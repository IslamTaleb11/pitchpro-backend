using Chargily.Pay;
using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer;
using BusinessLayer.Services;
using System.Text.Json;
using Chargily;
using DataAccessLayer.DTOs.Chargily;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DataAccessLayer.DTOs.User;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;


using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [ApiController]
    [Route("api/payment")]
    [EmailVerified]
    public class PaymentController : ControllerBase
    {
        private readonly IChargilyPayClient _chargilyClient;
        private readonly IConfiguration _configuration;
        private readonly string _frontendUrl;
        private readonly ILogger<PaymentController> _logger;
        public PaymentController(IChargilyPayClient chargilyClient, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _chargilyClient = chargilyClient;
            _configuration = configuration;
            _frontendUrl = configuration["FrontendUrl"];
            _logger = logger;
        }



        [HttpGet("test")]
        [AllowAnonymous]
        [DisableRateLimiting]
        public IActionResult test()
        {
            Console.WriteLine("!!! METRIC TRIGGERED: ROUTE IS ALIVE !!!");
            return Ok("System online");
        }

        /// <summary>
        /// Creates a fast checkout without needing pre-registered product/price items
        /// Route: POST https://localhost:7057/api/payment/checkout
        /// Body: { "clubId": 1 }
        /// </summary>


        [Authorize(Roles = "President")]
        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckout()
        {
            try
            {
                var backendURL = _configuration["Dev:BackendUrl"];
                int clubID = GeneralSettings.ClubID;

                decimal subscriptionTypePrice = await Payment.GetSubscriptionPriceByName("Premium");

                // Using the official amount + currency constructor form
                var createCheckout = new Checkout(amount: subscriptionTypePrice, Currency.DZD)
                {
                    Description = "PitchPro Premium Plan Subscription",
                    Language = LocaleType.Arabic,
                    PassFeesToCustomer = true,
                    CollectShippingAddress = false,
                    WebhookEndpointUrl = new Uri($"{backendURL}/api/payment/webhook"),
                    OnSuccessRedirectUrl = new Uri($"{_frontendUrl}/payment-success"),
                    OnFailureRedirectUrl = new Uri($"{_frontendUrl}/payment-failure"),
                    Metadata = new List<string> { $"clubId:{clubID}" }

                    // Metadata removed as clubId is no longer sent from frontend
                };

                // Call the correct SDK execution method name
                var response = await _chargilyClient.CreateCheckout(createCheckout);

                if (response != null && !string.IsNullOrEmpty(response.Value.CheckoutUrl.ToString()))
                {
                    // Deliver the session payment screen URL back to your client runner
                    return Ok(new { paymentUrl = response.Value.CheckoutUrl });
                }

                return BadRequest("Failed to generate Chargily payment session.");

            }

            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "An unexpected error occurred while finalizing the payment transaction profile."
                });
            }

        }


        /// <summary>
        /// Handles Chargily payment webhook
        /// Route: POST https://localhost:7057/api/payment/webhook
        /// </summary>






        [Authorize(Roles = "President")]
        [HttpPost("webhook")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[DisableRequestSizeLimit]
        [AllowAnonymous]
        [DisableRateLimiting]

        public async Task<IActionResult> HandleWebhook()
        {
            Request.EnableBuffering();

            try
            {
                using var reader = new StreamReader(Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                // 🚀 Deserialize explicitly into your DTO wrapper
                var webhookRequest = JsonSerializer.Deserialize<ChargilyWebhookDTO>(body, options);

                if (webhookRequest != null && webhookRequest.Data != null)
                {
                    // Forward the validated data payload down to your business logic service layer
                    PaymentWebhookService.ProcessChargilyWebhook(webhookRequest);
                }
                else
                {
                    return StatusCode(500);
                }

                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "An unexpected error occurred." });
            }
        }

        [Authorize]
        [HttpPost("upgrade-token")]
        public async Task<IActionResult> UpgradeJWT()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("User ID claim missing in token.");

                if (!int.TryParse(userIdClaim.Value, out int userID))
                    return Unauthorized("Invalid user ID in token.");

                var user = BusinessLayer.User.Find(userID);
                if (user == null)
                    return Unauthorized("User not found.");

                int? clubIDNullable = await BusinessLayer.User.GetClubId(userID);
                if (clubIDNullable == null)
                    return BadRequest("Club ID not found for user.");
                int clubID = clubIDNullable.Value;

                string currentPlanName = await ClubSubscription.GetCurrentSubscriptionName(clubID);
                if (string.IsNullOrEmpty(currentPlanName))
                    return BadRequest("Current plan not found for club.");

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("clubId", clubID.ToString()),
                    new Claim(ClaimTypes.Role, user.Role?.ToString() ?? string.Empty),
                    new Claim("plan", currentPlanName),
                    new Claim("email_verified", (user.EmailVerifiedAt != null).ToString())
                };

                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(jwtKey))
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "JWT signing key is not configured." });

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiresAt = token.ValidTo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal error occurred.", detail = ex.Message });
            }
        }








        //[Authorize]
        //[HttpPost("upgrade-token")]
        //public async Task<IActionResult> UpgradeJWT()
        //{
        //    int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        //    UserFindResponseDTO user = BusinessLayer.User.Find(userID);

        //    if (user == null)
        //    {
        //        return Unauthorized();
        //    }


        //    int clubID = await BusinessLayer.User.GetClubId(userID) ?? -1;
        //    string currentPlanName = await ClubSubscription.GetCurrentSubscriptionName(clubID);


        //    var claims = new[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
        //        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        //        new Claim("clubId", clubID.ToString()),
        //        new Claim(ClaimTypes.Role, user.Role.ToString()),
        //        new Claim("plan", currentPlanName)
        //    };

        //    var jwtKey = _configuration["Jwt:Key"];
        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //    var token = new JwtSecurityToken(
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddDays(1),
        //        signingCredentials: creds
        //    );

        //    return Ok(new
        //    {
        //        token = new JwtSecurityTokenHandler().WriteToken(token),
        //        expiresAt = token.ValidTo
        //    });

        //}
    }


}
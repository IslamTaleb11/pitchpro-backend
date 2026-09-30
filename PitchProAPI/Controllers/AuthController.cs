using DataAccessLayer.DTOs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessLayer;
using DataAccessLayer;
using DataAccessLayer.DTOs.Auth;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.RefreshToken;
using BusinessLayer.Services;
using PitchProAPI.Services;
using PitchProAPI.Helpers;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace PitchProAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("AuthLimiter")]

        public async Task<ActionResult<TokenResponseDTO>> Login([FromBody] LoginRequest request)
        {
            try
            {
                UserFindResponseDTO user = BusinessLayer.User.FindByEmail(request.Email);

                if (user == null)
                    return Unauthorized("Invalid credentials");

                int clubID = await BusinessLayer.User.GetClubId(user.ID) ?? -1;
                string currentPlanName = await ClubSubscription.GetCurrentSubscriptionName(clubID);


                bool isValidPassword =
                    BCrypt.Net.BCrypt.Verify(request.Password, user.Password);


                if (!isValidPassword)
                    return Unauthorized("Invalid credentials");

                string jwtKey = _configuration["Jwt:Key"];


                if (string.IsNullOrWhiteSpace(jwtKey))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        message = "JWT signing key is not configured."
                    });
                }


                // Create refresh token (random)
                var refreshToken = GeneralHelper.GenerateRefreshToken();

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("clubId", clubID.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("plan", currentPlanName),
                    new Claim("email_verified", (user.EmailVerifiedAt != null).ToString())
                };

                SecurityToken token = GeneralHelper.GenerateJWT(claims, jwtKey);

                string? ip = HttpRequestHelper.GetIpAddress(HttpContext);
                string? userAgent = HttpRequestHelper.GetDeviceName(HttpContext);

                RefreshToken refreshTokenObject = await RefreshTokenService.CreateRefreshTokenObject(user.ID, refreshToken, 
                    ip, userAgent);
                

                if (await refreshTokenObject.save())
                {
                    //Return the serialized JWT token to the client.
                    //The client will send this token with future requests.
                    return Ok(new TokenResponseDTO
                    {
                        AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                        RefreshToken = refreshToken
                    });
                }
                
                return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Failed to create refresh token.");

            }
            catch (Exception ex)
            {
                Console.WriteLine("The error is: " + ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }




        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("AuthLimiter")]

        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDTO request)
        {
            try
            {
                RefreshTokenFindResponseDTO refreshTokenFindResponseDTO = await RefreshToken.FindByHashedToken(GeneralHelper.ComputeSha256Hash(request.RefreshToken));

                if (refreshTokenFindResponseDTO == null)
                {
                    return Unauthorized("Invalid refresh token");
                }

                UserFindResponseDTO user = BusinessLayer.User.Find(refreshTokenFindResponseDTO.UserID);

                //Console.WriteLine("The User role is: " + user.Role);


                if (refreshTokenFindResponseDTO.RevokedAt != null)
                    return Unauthorized("Refresh token is revoked");

                if (refreshTokenFindResponseDTO.ExpiresAt <= DateTime.UtcNow)
                    return Unauthorized("Refresh token expired");


                // Derive the club from the refresh token's user, NOT from the
                // access-token claim (GeneralSettings.ClubID). On a cold start or
                // with an expired access token the claim is absent, which would
                // resolve ClubID to 0 and break the refresh. Using the refresh
                // token's user keeps refresh working independently of the access token.
                int clubID = await BusinessLayer.User.GetClubId(user.ID) ?? -1;
                string currentPlanName = await ClubSubscription.GetCurrentSubscriptionName(clubID);

                // If the club is on the Premium plan but it has expired, downgrade it to
                // Free here so the newly issued access token carries the correct plan.
                if (currentPlanName == "Premium")
                {
                    DateTime? expiryDate = await ClubSubscription.GetClubSubscriptionExpiryDate(clubID);

                    if (expiryDate.HasValue && expiryDate.Value <= DateTime.UtcNow)
                    {
                        await ClubSubscription.DowngradeToFreePlan(clubID);
                        currentPlanName = await ClubSubscription.GetCurrentSubscriptionName(clubID);
                    }
                }



                // Issue NEW access token (same claims & signing settings as login)
                var claims = new[]
                    {
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("clubId", clubID.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("plan", currentPlanName),
                    new Claim("email_verified", (user.EmailVerifiedAt != null).ToString())
                };

                string jwtKey = _configuration["Jwt:Key"];

                SecurityToken token = GeneralHelper.GenerateJWT(claims, jwtKey);

                var newAccessToken = new JwtSecurityTokenHandler().WriteToken(token);

                var newRefreshToken = GeneralHelper.GenerateRefreshToken();

                string? ip = HttpRequestHelper.GetIpAddress(HttpContext);
                string? deviceName = HttpRequestHelper.GetDeviceName(HttpContext);

                RefreshToken refreshTokenObject = await RefreshTokenService.CreateRefreshTokenObject(user.ID, newRefreshToken);

                RefreshTokenRevokeRequestDTO refreshTokenRevokeRequestDTO = new RefreshTokenRevokeRequestDTO();
                refreshTokenRevokeRequestDTO.TokenHash = refreshTokenFindResponseDTO.TokenHash;
                refreshTokenRevokeRequestDTO.RevokedAt = DateTime.UtcNow;
                refreshTokenRevokeRequestDTO.RevokedByIP = ip;
                refreshTokenRevokeRequestDTO.DeviceName = deviceName;
                refreshTokenRevokeRequestDTO.RevokedReason = Convert.ToInt32(
                    RefreshTokenRevocationReasons.enRefreshTokenRevocationReasons.RefreshTokenRotated);


                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!await refreshTokenObject.save())
                        return StatusCode(500);

                    refreshTokenRevokeRequestDTO.ReplacedByTokenID = refreshTokenObject.ID;

                    if (!await RefreshToken.RevokeRefreshToken(refreshTokenRevokeRequestDTO))
                        return StatusCode(500);

                    scope.Complete();
                }

                return Ok(new TokenResponseDTO
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            }

            catch(Exception)
            {
                return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
            }
        }



        // Lightweight, unauthenticated endpoint the SPA calls to measure the
        // clock skew between the client and the server. The client compares
        // this value against its own Date.now() to compute an offset, which it
        // then uses to validate JWT expiry WITHOUT trusting the (possibly
        // wrong) client clock. Returning the time in the JSON body avoids any
        // dependency on response-header exposure (CORS/proxies can strip the
        // HTTP `Date` header, which is what previously caused constant
        // unnecessary refreshes).
        [HttpGet("server-time")]
        [AllowAnonymous]
        public IActionResult ServerTime()
        {
            return Ok(new { serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenLogoutRequestDTO request)
        {
            try { 
                RefreshTokenFindResponseDTO refreshTokenFindResponseDTO = await RefreshToken.FindByHashedToken(GeneralHelper.ComputeSha256Hash(request.RefreshToken));

                if (refreshTokenFindResponseDTO == null)
                {
                    return Unauthorized("Invalid refresh token.");
                }

                if (refreshTokenFindResponseDTO.RevokedAt != null)
                {
                    return BadRequest("Refresh token has already been revoked.");
                }

                if (refreshTokenFindResponseDTO.ExpiresAt <= DateTime.UtcNow)
                {
                    return BadRequest("Refresh token has expired.");
                }

                UserFindResponseDTO user = BusinessLayer.User.Find(refreshTokenFindResponseDTO.UserID);

                RefreshTokenRevokeRequestDTO refreshTokenRevokeRequestDTO = new RefreshTokenRevokeRequestDTO();
                refreshTokenRevokeRequestDTO.TokenHash = refreshTokenFindResponseDTO.TokenHash;
                refreshTokenRevokeRequestDTO.RevokedAt = DateTime.UtcNow;
                refreshTokenRevokeRequestDTO.RevokedByIP = HttpRequestHelper.GetIpAddress(HttpContext);
                refreshTokenRevokeRequestDTO.DeviceName = HttpRequestHelper.GetDeviceName(HttpContext); ;
                refreshTokenRevokeRequestDTO.RevokedReason = Convert.ToInt32(
                    RefreshTokenRevocationReasons.enRefreshTokenRevocationReasons.Logout);

                if (!await RefreshToken.RevokeRefreshToken(refreshTokenRevokeRequestDTO))
                    return StatusCode(500);

                return Ok("Logged out successfully");
            }
            catch(Exception)
            {
                return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
            }
            
        }
    }
}

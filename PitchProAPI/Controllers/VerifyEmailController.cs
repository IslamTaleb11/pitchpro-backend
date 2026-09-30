using BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace PitchProAPI.Controllers
{
    [Route("api/verify-email")]
    [ApiController]
    public class VerifyEmailController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public ActionResult Verify([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Verification token is missing." });
            }

            bool verified = BusinessLayer.User.VerifyEmail(token);

            if (verified)
            {
                return Ok(new { message = "Email verified successfully." });
            }

            return BadRequest(new { message = "Invalid or expired verification link." });
        }
    }
}
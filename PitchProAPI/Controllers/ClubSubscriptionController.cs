using BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/club/subscription")]
    [ApiController]
    [EmailVerified]
    public class ClubSubscriptionController : ControllerBase
    {


        [HttpGet("remaining")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> GetSubscriptionRemainingDays()
        {
            try
            {
                int clubId = GeneralSettings.ClubID;
                if (clubId <= 0)
                {
                    return BadRequest(new { message = "Invalid or uninitialized Club ID context." });
                }

                int remainingDays = await ClubSubscription.GetClubSubscriptionRemainingDays(clubId);

                return Ok(remainingDays);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal error occurred while processing the request." });
            }
        }



    }
}

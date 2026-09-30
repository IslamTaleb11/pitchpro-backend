using BusinessLayer;
using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.MatchEvent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/match-events")]
    [ApiController]
    [EmailVerified]
    public class MatchEventController : ControllerBase
    {
        private readonly ILogger<MatchEventController> _logger;

        public MatchEventController(ILogger<MatchEventController> logger)
        {
            _logger = logger;
        }

        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Save(MatchEventRequestSaveDTO request)
        {
            try
            {
                if (request.MatchAttendanceID <= 0)
                {
                    return BadRequest(new { message = "Invalid match attendance ID." });
                }

                if (request.EventTypeID <= 0)
                {
                    return BadRequest(new { message = "Invalid event type ID." });
                }

                int newId = await MatchEvent.Save(request);

                return Ok(new { message = "Match event saved successfully.", id = newId });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Match event saving failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while saving match event.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [Authorize(Roles = "President")]
        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete([FromQuery] MatchEventDeleteRequestDTO request)
        {
            try
            {
                await MatchEvent.Delete(request);

                return Ok(new { message = "Match event deleted successfully." });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Match event deletion failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting match event.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("by-match/{matchId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetByMatch(int matchId)
        {
            try
            {
                var events = await MatchEvent.GetByMatch(matchId);

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving match events.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
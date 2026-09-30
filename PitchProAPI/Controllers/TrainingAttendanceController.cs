using BusinessLayer;
using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.TrainingAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/training-attendance")]
    [ApiController]
    [EmailVerified]
    public class TrainingAttendanceController : ControllerBase
    {
        private readonly ILogger<TrainingAttendanceController> _logger;

        public TrainingAttendanceController(ILogger<TrainingAttendanceController> logger)
        {
            _logger = logger;
        }

        // Records attendance for players in a training session. Accepts a
        // training session ID and a dictionary of playerId -> bool (attended).
        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Mark([FromBody] TrainingAttendanceSaveRequestDTO request)
        {
            try
            {
                if (request.TrainingSessionID <= 0)
                    return BadRequest(new { message = "Invalid training session ID." });

                if (request.PlayersAttendance == null || request.PlayersAttendance.Count == 0)
                    return BadRequest(new { message = "No players provided for attendance marking." });

                var attendanceIds = await TrainingAttendance.Save(request);

                return Ok(new
                {
                    message = "Training attendance marked successfully.",
                    attendanceIds
                });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Training attendance marking failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while marking training attendance.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}

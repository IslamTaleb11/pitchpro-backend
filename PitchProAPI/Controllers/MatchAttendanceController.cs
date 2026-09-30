using BusinessLayer;
using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.MatchAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/match-attendance")]
    [ApiController]
    [EmailVerified]
    public class MatchAttendanceController : ControllerBase
    {
        private readonly ILogger<MatchAttendanceController> _logger;

        public MatchAttendanceController(ILogger<MatchAttendanceController> logger)
        {
            _logger = logger;
        }

        // Records a player's attendance for a match. Receives the match_callup_players
        // id and a status bit (true = attended, false = absent). Re-marking the same
        // player updates the existing row rather than duplicating it. Club scoping is
        // enforced in the business layer, so a club can only record attendance for its
        // own called-up players.
        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Mark(MatchAttendanceMarkRequestDTO request)
        {
            try
            {
                if (request.MatchID <= 0)
                {
                    return BadRequest(new { message = "Invalid match ID." });
                }

                if (request.PlayersAttendance == null || request.PlayersAttendance.Count == 0)
                {
                    return BadRequest(new { message = "No players provided for attendance marking." });
                }

                Dictionary<int, int?> PlayersIds_MatchCallUpIds = await MatchAttendance.GetCallUpPlayerId(request.PlayersAttendance, request.MatchID);

                Dictionary<int, int> attendanceIds = await MatchAttendance.Mark(PlayersIds_MatchCallUpIds, request.PlayersAttendance);

                return Ok(new { message = "Match attendance marked successfully.", attendanceIds });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Match attendance marking failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while marking match attendance.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        // Resets attendance for specific players in a match. Accepts a match ID and
        // a list of player IDs, resolves each to its match_callup_players_id, then
        // deletes all attendance rows for those call-up players in a single transaction.
        // Club scoping is enforced through the call-up player lookup, so only the
        // current club's attendance data is affected.
        [Authorize(Roles = "President")]
        [HttpPost("reset")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Reset(MatchAttendanceResetRequestDTO request)
        {
            try
            {
                if (request.MatchID <= 0)
                {
                    return BadRequest(new { message = "Invalid match ID." });
                }

                if (request.PlayerIDs == null || request.PlayerIDs.Count == 0)
                {
                    return BadRequest(new { message = "No players provided." });
                }

                int deleted = await MatchAttendance.Reset(request.MatchID, request.PlayerIDs);

                return Ok(new
                {
                    deleted,
                    message = deleted > 0
                        ? "Match attendance records reset successfully."
                        : "No attendance records were found for the specified players."
                });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Match attendance reset failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while resetting match attendance.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }


        // Returns all called-up players for a match with their attendance status
        // (attended, absent, or null if not marked yet). Filtered by match,
        // category, and club (from auth token).
        [Authorize(Roles = "President")]
        [HttpGet("{matchId}/players")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAttendanceByMatch(
            int matchId, [FromQuery] int categoryId)
        {
            try
            {
                if (matchId <= 0)
                    return BadRequest(new { message = "Invalid match ID." });
                if (categoryId <= 0)
                    return BadRequest(new { message = "Invalid category ID." });

                var players = await MatchAttendance.GetAttendanceByMatchAsync(matchId, categoryId);

                return Ok(new { data = players });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching match attendance data.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}

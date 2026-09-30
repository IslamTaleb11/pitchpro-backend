using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.MatchCallUpPlayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/match-callup-players")]
    [ApiController]
    [EmailVerified]
    public class MatchCallUpPlayerController : ControllerBase
    {
        private readonly ILogger<MatchCallUpPlayerController> _logger;
        private readonly PlanLimitService _planLimitService;

        public MatchCallUpPlayerController(ILogger<MatchCallUpPlayerController> logger, PlanLimitService planLimitService)
        {
            _logger = logger;
            _planLimitService = planLimitService;
        }

        // Adds one or more players to a match call-up after verifying that the
        // match (and every player) belong to the supplied category within the
        // current club. Returns which players were added and which were rejected.
        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddNew([FromBody] MatchCallUpPlayerRegistrationRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (await _planLimitService.CanAddCallUpPlayers(request.MatchID, request.PlayerIDs.Count) != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 20 players per match call-up allowed on the Free Plan. Upgrade to the Premium Plan for a larger squad."
                    });
                }

                int added = await MatchCallUpPlayer.AddRange(
                    request.MatchID, request.CategoryID, request.PlayerIDs);

                return Ok(new
                {
                    added,
                    message = added > 0
                        ? "Players added to the match call-up successfully."
                        : "No new players were added to the call-up."
                });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Match call-up creation failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during match call-up creation.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        // Removes every player from a match's call-up within the current club and
        // returns how many were removed. A match the club doesn't own removes
        // nothing (club scoping is enforced in the provider's SQL join).
        [Authorize(Roles = "President")]
        [HttpDelete("{matchId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetByMatch(int matchId)
        {
            try
            {
                if (matchId <= 0)
                    return BadRequest(new { message = "Invalid match id." });

                int removed = await MatchCallUpPlayer.ResetByMatch(matchId);

                return Ok(new
                {
                    matchId,
                    removed,
                    message = removed > 0
                        ? "All players were removed from the match call-up."
                        : "No players were found for this match's call-up."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while resetting the match call-up.");
                return StatusCode(500, new { message = "An unexpected error occurred while resetting the call-up." });
            }
        }

        // Returns how many players are called up for a specific match, scoped to
        // the current club. A match the club doesn't own returns a count of 0.
        [HttpGet("count/{matchId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CountByMatch(int matchId)
        {
            try
            {
                int count = await MatchCallUpPlayer.CountByMatch(matchId);

                return Ok(new
                {
                    matchId,
                    count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while counting call-up players.");
                return StatusCode(500, new { message = "An unexpected error occurred while counting the call-up." });
            }
        }

        // Returns every player in the given category that has no active injury
        // (either no injury records or all have is_active = 0), along with whether
        // each player is already called up for the specified match.
        [HttpGet("available/{categoryId}/{matchId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAvailablePlayersByCategory(int categoryId, int matchId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest(new { message = "Invalid category ID." });
                if (matchId <= 0)
                    return BadRequest(new { message = "Invalid match ID." });

                var players = await MatchCallUpPlayer.GetAvailablePlayersByCategory(categoryId, matchId);

                return Ok(players);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching available players by category.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}

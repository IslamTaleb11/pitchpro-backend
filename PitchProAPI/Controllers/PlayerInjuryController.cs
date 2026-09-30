using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.PlayerInjury;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/player-injuries")]
    [ApiController]
    [EmailVerified]
    public class PlayerInjuryController : ControllerBase
    {
        private readonly ILogger<PlayerInjuryController> _logger;
        private readonly PlanLimitService _planLimitService;

        public PlayerInjuryController(ILogger<PlayerInjuryController> logger, PlanLimitService planLimitService)
        {
            _logger = logger;
            _planLimitService = planLimitService;
        }

        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddNew([FromBody] PlayerInjuryRegistrationRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (await _planLimitService.CanCreateInjury(request.PlayerMedicalDossierID) != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 3 active injuries per player allowed on the Free Plan. Upgrade to the Premium Plan for unlimited medical logs."
                    });
                }

                PlayerInjury newInjury = new PlayerInjury(request);

                bool success = await newInjury.Save();

                if (success)
                {
                    return Ok(new
                    {
                        id = newInjury.ID,
                        data = newInjury.playerInjuryRegistrationResponseDTO,
                        message = "Player injury recorded successfully"
                    });
                }

                return StatusCode(500, new { message = "The database failed to persist the record." });
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Player injury creation failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during player injury creation.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [Authorize(Roles = "President")]
        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<InjuryByCategoryResponseDTO>>> GetInjuriesByCategory(int categoryId)
        {
            try
            {
                IEnumerable<InjuryByCategoryResponseDTO> injuries = await PlayerInjury.GetInjuriesByCategory(categoryId);

                return Ok(new { data = injuries });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching injuries by category.");
                return StatusCode(500, new { message = "An error occurred while fetching injuries by category." });
            }
        }

        // Marks an injury as recovered (is_active = 0) — the player is no longer injured.
        [Authorize(Roles = "President")]
        [HttpPatch("{injuryId}/recover")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MarkAsRecovered(int injuryId)
        {
            try
            {
                bool success = await PlayerInjury.MarkAsRecovered(injuryId);

                if (success)
                {
                    return Ok(new { message = "Injury marked as recovered." });
                }

                return NotFound(new { message = "Injury not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while marking the injury as recovered.");
                return StatusCode(500, new { message = "An error occurred while updating the injury status." });
            }
        }
    }
}

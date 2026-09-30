using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.Player;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/players")]
    [ApiController]
    [EmailVerified]
    public class PlayerController : ControllerBase
    {
        private readonly BlobService _blobService;
        private readonly ILogger<PlayerController> _logger;
        private readonly PlanLimitService _planLimitService;

        public PlayerController(BlobService blobService, ILogger<PlayerController> logger, PlanLimitService planLimitService)
        {
            _blobService = blobService;
            _logger = logger;
            _planLimitService = planLimitService;
        }


        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> AddNew([FromForm] PlayerRegistrationRequestDTO request)
        {
            try
            {
                if (!GeneralHelper.IsValidImage(request.Photo))
                    return BadRequest(new { message = "Invalid image file." });

                if (await _planLimitService.CanCreatePlayer() != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 20 players allowed on the Free Plan. Upgrade to the Premium Plan to add unlimited players."
                    });
                }

                Player newPlayer = new Player(request);

                bool success = await newPlayer.Save(_blobService);

                if (success)
                {
                    return Ok(new { id = newPlayer.ID, message = "Player Registration successful" });
                }
                else
                {
                    return StatusCode(500, new { message = "The database failed to persist the record." });
                }
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Player creation failed (client-side error).");
                return BadRequest(new { message = "An error occurred while creating the player." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during player creation.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [Authorize(Roles = "President")]
        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<PlayerByCategoryResponseDTO>>> GetPlayersByCategory(int categoryId)
        {
            try
            {
                IEnumerable<PlayerByCategoryResponseDTO> players = await Player.GetPlayersByCategory(categoryId);

                return Ok(new { data = players });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching players by category.");
                return StatusCode(500, new { message = "An error occurred while fetching players by category." });
            }
        }

        // Returns every player in the given category that currently has no active
        // injury — i.e. available for selection in a call-up / lineup. The result
        // is scoped to the current club via the auth token.
        [HttpGet("match-call-up/{categoryId}/{matchID}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<MatchCallUpPlayerByCategoryResponseDTO>>> GetMatchCallUpPlayersByCategory(int categoryId, int matchID)
        {
            try
            {
                IEnumerable<MatchCallUpPlayerByCategoryResponseDTO> players = await Player.GetMatchCallUpPlayersByCategory(categoryId, matchID);

                return Ok(new { data = players });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching match call-up players by category.");
                return StatusCode(500, new { message = "An error occurred while fetching match call-up players by category." });
            }
        }

        // Returns the full detail of a single player, used to prefill the update
        // form. Scoped to the current club via the auth token.
        [Authorize(Roles = "President")]
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PlayerGetByIdResponseDTO>> GetById(int id)
        {
            try
            {
                PlayerGetByIdResponseDTO player = await Player.GetById(id);

                if (player == null)
                {
                    return NotFound(new { message = "Player not found." });
                }

                return Ok(new { data = player });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching a player.");
                return StatusCode(500, new { message = "An error occurred while fetching the player." });
            }
        }

        // Updates a player's full dossier. The photo is optional — when omitted
        // the existing picture is kept. Scoped to the current club via the token.
        [Authorize(Roles = "President")]
        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> Update(int id, [FromForm] PlayerUpdateRequestDTO request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid player id." });

                request.ID = id;

                Player player = new Player(request);

                bool success = await player.Save(_blobService);

                if (success)
                {
                    return Ok(new { message = "Player updated successfully." });
                }
                else
                {
                    return NotFound(new { message = "Player not found." });
                }
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Player update failed (client-side error).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during player update.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        // Soft-deletes a player (is_active = 0). Scoped to the current club.
        [Authorize(Roles = "President")]
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool success = await Player.Delete(id);

                if (success)
                {
                    return Ok(new { message = "Player deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = "Player not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during player delete.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}

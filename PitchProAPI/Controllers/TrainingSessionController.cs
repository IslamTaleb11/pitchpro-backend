using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.TrainingSession;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/training-sessions")]
    [ApiController]
    [EmailVerified]
    public class TrainingSessionController : ControllerBase
    {
        private readonly PlanLimitService _planLimitService;

        public TrainingSessionController(PlanLimitService planLimitService)
        {
            _planLimitService = planLimitService;
        }

        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddNew([FromBody] TrainingSessionRegistrationRequestDTO request)
        {
            try
            {
                if (await _planLimitService.CanCreateTrainingSession() != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 3 training sessions allowed on the Free Plan. Upgrade to the Premium Plan to schedule unlimited training sessions."
                    });
                }

                TrainingSession newSession = new TrainingSession(request);

                bool success = await newSession.SaveAsync();

                if (success)
                {
                    return Ok(new { id = newSession.ID, message = "Training session created successfully." });
                }
                else
                {
                    return StatusCode(500, new { message = "The database failed to persist the record." });
                }
            }
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [Authorize(Roles = "President")]
        [HttpPut]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update([FromBody] TrainingSessionEditRequestDTO request)
        {
            try
            {
                TrainingSession session = new TrainingSession(request);
                bool success = await session.SaveAsync();

                if (success)
                {
                    return Ok(new { message = "Training session updated successfully." });
                }
                else
                {
                    return NotFound(new { message = "Training session not found or you don't have permission to edit it." });
                }
            }
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        // Returns all players in a category who have no active injury.
        [Authorize(Roles = "President")]
        [HttpGet("players/{categoryId}/{trainingSessionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPlayersByCategory(int categoryId, int trainingSessionId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest(new { message = "Invalid category id." });
                if (trainingSessionId <= 0)
                    return BadRequest(new { message = "Invalid training session id." });

                var players = await TrainingSession.GetPlayersByCategoryAsync(categoryId, trainingSessionId);

                return Ok(new { data = players });
            }
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Returns paginated completed training sessions for a category within the
        // current club. Club is resolved from the auth token.
        [Authorize(Roles = "President")]
        [HttpGet("completed/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCompletedByCategory(
            int categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest(new { message = "Invalid category id." });
                if (page < 1)
                    return BadRequest(new { message = "Page must be 1 or greater." });
                if (pageSize < 1)
                    return BadRequest(new { message = "Page size must be 1 or greater." });

                var (data, totalCount) = await TrainingSession.GetCompletedByCategoryAsync(categoryId, page, pageSize);

                return Ok(new
                {
                    data,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Marks a training session as completed.
        [Authorize(Roles = "President")]
        [HttpPut("complete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Complete([FromBody] TrainingSessionCompleteRequestDTO request)
        {
            try
            {
                await TrainingSession.CompleteAsync(request.TrainingSessionID);
                return Ok(new { message = "Training session completed successfully." });
            }
            catch (BaseException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Returns all non-completed training sessions for a category within the
        // current club.
        [Authorize(Roles = "President")]
        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest(new { message = "Invalid category id." });

                var sessions = await TrainingSession.GetByCategoryAsync(categoryId);

                return Ok(new { data = sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

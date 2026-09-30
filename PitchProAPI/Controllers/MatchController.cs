using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.Match;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/matches")]
    [ApiController]
    [EmailVerified]
    public class MatchController : ControllerBase
    {
        private readonly PlanLimitService _planLimitService;

        public MatchController(PlanLimitService planLimitService)
        {
            _planLimitService = planLimitService;
        }

        [Authorize(Roles = "President")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddNew([FromBody] MatchRegistrationRequestDTO request)
        {
            try
            {
                if (await _planLimitService.CanCreateMatch() != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 3 matches allowed on the Free Plan. Upgrade to the Premium Plan to schedule unlimited matches."
                    });
                }

                Match newMatch = new Match(request);

                bool success = await newMatch.SaveAsync();

                if (success)
                {
                    return Ok(new { id = newMatch.ID, message = "Match created successfully." });
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
        public async Task<IActionResult> Update([FromBody] MatchEditRequestDTO request)
        {
            try
            {
                Match match = new Match(request);
                bool success = await match.SaveAsync();

                if (success)
                {
                    return Ok(new { message = "Match updated successfully." });
                }
                else
                {
                    return NotFound(new { message = "Match not found or you don't have permission to edit it." });
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

        // Sets the result (club score and opponent score) for a match. Club is
        // resolved from the auth token.
        [Authorize(Roles = "President")]
        [HttpPut("{matchId}/result")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetResult(int matchId, [FromBody] MatchResultSaveDTO dto)
        {
            try
            {
                if (matchId <= 0)
                    return BadRequest(new { message = "Invalid match id." });
                if (dto.ClubScore < 0)
                    return BadRequest(new { message = "Club score must be 0 or greater." });
                if (dto.OpponentScore < 0)
                    return BadRequest(new { message = "Opponent score must be 0 or greater." });

                await Match.SetResultAsync(matchId, dto);

                return Ok(new { message = "Match result updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Marks a match as completed by match ID. Club is resolved from the
        // auth token.
        [Authorize(Roles = "President")]
        [HttpPost("{matchId}/complete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetCompleted(int matchId)
        {
            try
            {
                if (matchId <= 0)
                    return BadRequest(new { message = "Invalid match id." });

                await Match.SetCompletedAsync(matchId);

                return Ok(new { message = "Match marked as completed." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Returns paginated completed matches for a category within the current
        // club. Club is resolved from the auth token.
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

                var (data, totalCount) = await MatchCallUp.GetCompletedByCategoryAsync(categoryId, page, pageSize);

                return Ok(new
                {
                    data,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize(Roles = "President")]
        [HttpGet("call-up/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCallUpByCategory(int categoryId)
        {
            try
            {
                MatchCallUp match = await MatchCallUp.GetUpcomingByCategoryAsync(categoryId);

                if (match == null || !match.HasUpcomingMatch)
                {
                    return Ok(new { hasUpcomingMatch = false, match = (object)null });
                }

                return Ok(new
                {
                    hasUpcomingMatch = true,
                    match = new
                    {
                        id            = match.ID,
                        clubId        = match.ClubID,
                        categoryId    = match.CategoryID,
                        opponentName  = match.OpponentName,
                        date          = match.Date,
                        kickoffTime   = match.KickoffTime,
                        endTime       = match.EndTime,
                        isCompleted   = match.IsCompleted,
                        isHome        = match.IsHome,
                        stadiumName   = match.StadiumName,
                        clubName      = match.ClubName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Returns all non-completed matches for a category within the current club.
        [HttpGet("incomplete/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetIncompleteByCategory(int categoryId)
        {
            try
            {
                IEnumerable<MatchIncompleteByCategoryResponseDTO> matches = await MatchCallUp.GetIncompleteByCategoryAsync(categoryId);

                return Ok(new { data = matches });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Returns the first upcoming match for a category (nearest not-completed
        // match dated today or later) within the current club. The club is always
        // resolved from the auth token, so only the category id is supplied.
        // 404 when the category has no upcoming match.
        [HttpGet("upcoming/{categoryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUpcomingMatchByCategory(int categoryId)
        {
            try
            {
                MatchCallUp match = await MatchCallUp.GetUpcomingByCategoryAsync(categoryId);

                if (match == null || !match.HasUpcomingMatch)
                {
                    return NotFound(new { message = "No upcoming match found for this category." });
                }

                return Ok(new
                {
                    id            = match.ID,
                    clubId        = match.ClubID,
                    categoryId    = match.CategoryID,
                    opponentName  = match.OpponentName,
                    date          = match.Date,
                    kickoffTime   = match.KickoffTime,
                    endTime       = match.EndTime,
                    isCompleted   = match.IsCompleted,
                    isHome        = match.IsHome,
                    stadiumName   = match.StadiumName,
                    clubName      = match.ClubName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

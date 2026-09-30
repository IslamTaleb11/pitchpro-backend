using BusinessLayer;
using DataAccessLayer.DTOs.EventType;
using DataAccessLayer.DTOs.BloodType;
using DataAccessLayer.DTOs.Category;
using DataAccessLayer.DTOs.PrimaryRole;
using DataAccessLayer.DTOs.RoleClassification;
using DataAccessLayer.DTOs.StaffCategory;
using DataAccessLayer.DTOs.PrimaryPosition;
using DataAccessLayer.DTOs.FootPreference;
using DataAccessLayer.DTOs.TrainingSessionType;
using DataAccessLayer.DTOs.Schedule;
using DataAccessLayer.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/lookups")]
    [ApiController]
    [EmailVerified]
    public class LookupsController : ControllerBase
    {
        private readonly IMemoryCache _cache;

        public LookupsController(IMemoryCache cache)
        {
            _cache = cache;
        }

        // 1. Blood Types
        [HttpGet("bloodtypes")]
        public async Task<ActionResult<IEnumerable<BloodTypeGetAllResponseDTO>>> GetBloodTypes()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("bloodTypesList", async entry =>
                {
                    return await BloodTypeProvider.GetAllBloodTypes();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 2. Categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryGetAllLookupResponse>>> GetCategories()
        {
            try
            {
                return Ok(await Category.GetAllCategories());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 3. Primary Roles
        [HttpGet("primaryroles")]
        public async Task<ActionResult<IEnumerable<PrimaryRoleGetAllResponseDTO>>> GetPrimaryRoles()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("primaryRolesList", async entry =>
                {
                    return await PrimaryRoleProvider.GetAllPrimaryRoles();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 4. Role Classifications
        [HttpGet("classifications")]
        public async Task<ActionResult<IEnumerable<RoleClassificationGetAllResponseDTO>>> GetRoleClassifications()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("classificationsList", async entry =>
                {
                    return await RoleClassificationProvider.GetAllRoleClassifications();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 5. Positions (Primary and Secondary Positions)
        [HttpGet("positions")]
        public async Task<ActionResult<IEnumerable<PrimaryPositionGetAllResponseDTO>>> GetPositions()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("positionsList", async entry =>
                {
                    return await PrimaryPositionProvider.GetAllPrimaryPositions();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 6. Preferred Feet
        [HttpGet("feet")]
        public async Task<ActionResult<IEnumerable<FootPreferenceGetAllResponseDTO>>> GetPreferredFeet()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("footPreferencesList", async entry =>
                {
                    return await FootPreferenceProvider.GetAllFootPreferences();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 7. Training Session Types
        [HttpGet("sessiontypes")]
        public async Task<ActionResult<IEnumerable<TrainingSessionTypeGetAllResponseDTO>>> GetSessionTypes()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("sessionTypesList", async entry =>
                {
                    return await TrainingSessionTypeProvider.GetAllSessionTypes();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 8. Event Types
        [HttpGet("eventtypes")]
        public async Task<ActionResult<IEnumerable<EventTypeGetAllResponseDTO>>> GetEventTypes()
        {
            try
            {
                return Ok(await _cache.GetOrCreateAsync("eventTypesList", async entry =>
                {
                    return await EventTypeProvider.GetAllEventTypes();
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 9. Upcoming Schedule
        [HttpGet("upcoming-schedule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUpcomingSchedule(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize   = 10,
            [FromQuery] string eventClassification = null,
            [FromQuery] string category = null)
        {
            try
            {
                List<UpcomingScheduleItemDTO> items = await Schedule.GetUpcomingSchedule(pageNumber, pageSize, eventClassification, category);

                return Ok(new
                {
                    data       = items,
                    pageNumber,
                    pageSize,
                    hasMore    = items.Count == pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

using BusinessLayer;
using DataAccessLayer.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/dashboard")]
    [ApiController]
    [EmailVerified]
    public class DashboardController : ControllerBase
    {
        [Authorize(Roles = "President")]
        [HttpGet("counts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardCountsResponseDTO>> GetCounts()
        {
            try
            {
                DashboardCountsResponseDTO counts = await Dashboard.GetCounts();
                return Ok(new { data = counts });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while fetching dashboard counts." });
            }
        }
    }
}

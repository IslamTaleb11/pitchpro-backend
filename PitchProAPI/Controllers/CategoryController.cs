using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using DataAccessLayer.DTOs.Category;
using DataAccessLayer.DTOs.StaffCategory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/dashboard/category")]
    [ApiController]
    [EmailVerified]
    public class CategoryController : ControllerBase
    {
        private readonly PlanLimitService _planLimitService;

        public CategoryController(PlanLimitService planLimitService)
        {
            _planLimitService = planLimitService;
        }

        [Authorize(Roles = "President")]
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<CategoryGetAllResponseDTO>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var list = await Category.GetAllCategories(pageNumber, pageSize);
                return Ok(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        [Authorize(Roles = "President")]
        [HttpGet("filter")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<CategoryFilterResponseDTO>>> GetCategoriesWithFilter(
            [FromQuery] int minAge,
            [FromQuery] int maxAge,
            [FromQuery] int? capacity,
            [FromQuery] int registrationFeeMin,
            [FromQuery] int registrationFeeMax,
            [FromQuery] int minPlayers,
            [FromQuery] int maxPlayers,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var list = await Category.GetCategoriesWithFilter(
                    minAge, maxAge, capacity,
                    registrationFeeMin, registrationFeeMax,
                    minPlayers, maxPlayers,
                    pageNumber, pageSize);
                return Ok(new { data = list });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the categories." });
            }
        }


        [Authorize(Roles = "President")]
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                bool result = await Category.Delete(id);

                if (result)
                {
                    return Ok(new { message = "Category deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = "Category not found." });
                }
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
        public async Task<ActionResult<CategoryEditRequestDTO>> Edit(CategoryEditRequestDTO request)
        {
            try
            {
                Category category = new Category(request);

                bool result = await category.Save();
                if (result)
                {
                    return Ok(new { message = "Category updated successfully." });
                }
                else
                {
                    return NotFound(new { message = "Category not found." });
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
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CategoryAddResponseDTO>> AddNew(CategoryAddRequestDTO request)
        {
            try
            {
                if (await _planLimitService.CanCreateCategory() != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 3 squad categories allowed on the Free Plan. Upgrade to the Premium Plan to add unlimited categories."
                    });
                }

                Category category = new Category(request);

                bool result = await category.Save();
                if (result)
                {
                    return Ok(new { id = category.categoryAddResponseDTO.ID, data = category.categoryAddResponseDTO, message = "Category created successfully." });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to create the category." });
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
    }
}

using BusinessLayer;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.Staff;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Exceptions;
using BusinessLayer.Services;
using BusinessLayer.Services.Azure;
using BusinessLayer.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;


using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/staff")]
    [ApiController]
    [EmailVerified]
    public class StaffController : ControllerBase
    {

        private readonly BlobService _blobService;

        private readonly PlanLimitService _planLimitService;

        // .NET provides the BlobService here automatically
        public StaffController(BlobService blobService, PlanLimitService planLimitService)
        {
            _blobService = blobService;
            _planLimitService = planLimitService;

        }

        [Authorize(Roles = "President")]
        [HttpPost]
        //[ProducesResponseType(typeof(PersonRegistrationResponseDTO), 201)]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2 * 1024 * 1024)] // 2MB total request limit

        public async Task<IActionResult> AddNew(StaffRegistrationRequestDTO request)
        {
            try
            {
                string clubCurrentPlan = User.FindFirst("plan").ToString();
                clubCurrentPlan = clubCurrentPlan.Replace("plan: ", "");


                if (await _planLimitService.CanCreateStaff() != true)
                {
                    return StatusCode(403, new
                    {
                        status = "limit_reached",
                        message = "You have reached the maximum limit of 3 active staff members allowed on the Free Plan. Upgrade to the Premium Plan to add unlimited staff."
                    });
                }

                if (!GeneralHelper.IsValidImage(request.Photo)) return BadRequest(new { message = "Invalid image file." });

                Staff newStaff = new Staff(request);

                // 3. This calls your async Save method
                bool success = await newStaff.Save(_blobService);

                if (success)
                {
                    // Return the ID and a clear message
                    return Ok(new { id = newStaff.ID, message = "Staff Member Registration successful" });
                }
                else
                {
                    return StatusCode(500, "The database failed to persist the record.");
                }
            }
            // 4. Specific exception handling
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // This captures the error message if you used 'throw' in your Staff class
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [Authorize(Roles = "President")]
        [HttpGet("counts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StaffDashboardCountsResponseDTO>> GetStaffDashboardCount()
        {
            try
            {
                StaffDashboardCountsResponseDTO responseDTO = await Staff.GetStaffDashboardCount();
                return Ok(new
                {
                    data = responseDTO
                });
            }
            catch(Exception)
            {
                return StatusCode(500, new { message = "An error occurred while calculating staff counts." });
            }
        }


        [Authorize(Roles = "President")]
        [HttpGet("all")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StaffGetAllResponse>>> GetAllStaff([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            try
            {
                List<StaffGetAllResponse> list = new List<StaffGetAllResponse>();
                list = await Staff.GetAllStaff(pageNumber, pageSize);
                return Ok(new
                {
                    data = list
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while calculating staff counts." });
            }
        }


        [Authorize(Roles = "President")]
        [HttpGet("filter")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StaffGetAllResponse>>> GetStaffByFilters(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] int? primaryRoleId,
            [FromQuery] int? roleClassificationId,
            [FromQuery] int[] categoryIds)
        {
            try
            {
                List<StaffGetAllResponse> list = await Staff.GetStaffByFilters(pageNumber, pageSize, primaryRoleId, roleClassificationId, categoryIds);
                return Ok(new
                {
                    data = list
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while fetching filtered staff." });
            }
        }

        [Authorize(Roles = "President")]
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid staff ID." });
                }

                StaffGetByIdResponse staff = await Staff.GetById(id);

                if (staff == null || staff.ID <= 0)
                {
                    return NotFound(new { message = "Staff member not found." });
                }

                return Ok(new { data = staff });
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
        [RequestSizeLimit(2 * 1024 * 1024)] // 2MB total request limit
        public async Task<IActionResult> UpdateStaff([FromForm] StaffEditRequestDTO request)
        {
            try
            {
                if (request == null || request.ID <= 0)
                {
                    return BadRequest(new { message = "Invalid staff ID." });
                }

                // Reject the update if the email is already used by another account.
                // Done before the (irreversible) blob upload below to avoid orphaned blobs.
                if (await Staff.IsEmailTakenByOtherAsync(request.ID, request.Email))
                {
                    return BadRequest(new { message = "This email address is already registered." });
                }

                // Only upload a new photo if one was provided; otherwise the existing photo is kept.
                string photoUrl = null;
                if (request.Photo != null)
                {
                    if (!GeneralHelper.IsValidImage(request.Photo))
                        return BadRequest(new { message = "Invalid image file." });

                    photoUrl = await _blobService.UploadAndResizeImageAsync(request.Photo, AzureContainers.Staff, 800, 800);
                }

                StaffEditSaveDTO saveDTO = new StaffEditSaveDTO
                {
                    ID = request.ID,
                    FirstName = request.FirstName,
                    SecondName = request.SecondName,
                    LastName = request.LastName,
                    Gender = request.Gender,
                    BirthDate = request.BirthDate,
                    Email = request.Email,
                    Photo = photoUrl,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    PrimaryRoleID = request.PrimaryRoleID,
                    RoleClassificationID = request.RoleClassificationID,
                    CategoriesIDs = request.CategoriesIDs,
                    BloodTypeID = request.BloodTypeID,
                    Allergies = request.Allergies,
                    MedicalNotes = request.MedicalNotes
                };

                bool success = await Staff.Update(saveDTO);

                if (success)
                {
                    return Ok(new { message = "Staff member updated successfully." });
                }

                return NotFound(new { message = "Staff member not found." });
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
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool response = await Staff.Delete(id);

                if (response)
                {
                    return Ok(new { data = response });
                }

                return NotFound(new { message = "Staff not found" });
            }
            catch (ArgumentOutOfRangeException ex)
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

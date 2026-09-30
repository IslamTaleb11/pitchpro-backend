using BusinessLayer;
using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/user")]
    [ApiController]
    [EmailVerified]
    public class UserController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(UserRegistrationResponseDTO), 201)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]

        public async Task<ActionResult<UserRegistrationResponseDTO>> AddNew(UserRegistrationRequestDTO userRegistrationRequestDTO)
        {
            try
            {
                User newUser = new User(userRegistrationRequestDTO);
                
                if (await newUser.Save())
                {
                    UserRegistrationResponseDTO responseDTO = newUser.userRegistrationResponseDTO;
                    return CreatedAtRoute("findUser", new { id = responseDTO.ID }, responseDTO);
                }
                else
                {
                    return StatusCode(500, "The database failed to persist the record.");
                }
            }
            catch(DuplicateEmailException ex)
            {
                return BadRequest(new { message = ex.Message, email = ex.Email });
            }
            catch (PersonNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message, id = ex.ID });
            }
            catch (RoleNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message, id = ex.ID });
            }
            catch (PersonAlreadyLinkedException ex)
            {
                return BadRequest(new { message = ex.Message, id = ex.ID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}", Name = "findUser")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult<UserFindResponseDTO> Find(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("The ID is invalid.");
                }

                UserFindResponseDTO dto = BusinessLayer.User.Find(id);
                if (dto != null)
                {
                    return Ok(dto);
                }
                return NotFound("Club with ID " + id + " not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An internal error occurred.");
            }
        }


    }
}

using BusinessLayer;
using DataAccessLayer.DTOs.Club;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DataAccessLayer.DTOs.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;


using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/person")]
    [ApiController]
    [EmailVerified]
    public class PersonController : ControllerBase
    {
        [HttpPost]
        //[ProducesResponseType(typeof(PersonRegistrationResponseDTO), 201)]
        [ProducesResponseType(typeof(PersonRegistrationResponseDTO), 200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]


        public ActionResult<PersonRegistrationResponseDTO> AddNew(PersonRegistrationRequestDTO personRegistrationRequestDTO)
        {
            try
            {
                Person newPerson = new Person(personRegistrationRequestDTO);

                if (newPerson.Save())
                {
                    PersonRegistrationResponseDTO responseDTO = newPerson.personRegistrationResponseDTO;
                    //return CreatedAtAction("find", new { id = responseDTO.ID }, responseDTO);
                    return Ok(new { id = newPerson.ID, message = "Person Registration successful" });
                }
                else
                {
                    return StatusCode(500, "The database failed to persist the record.");
                }
            }
            catch (ClubNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

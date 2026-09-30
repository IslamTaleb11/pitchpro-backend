using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DataAccessLayer.DTOs.Club;
using BusinessLayer;
using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/club")]
    [ApiController]
    [EmailVerified]
    public class ClubController : ControllerBase
    {


        private readonly BlobService _blobService;

        // .NET provides the BlobService here automatically
        public ClubController(BlobService blobService)
        {
            _blobService = blobService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ClubRegistrationResponseDTO), 201)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2 * 1024 * 1024)] // 2MB total request limit
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<ActionResult<ClubRegistrationResponseDTO>> AddNew([FromForm] ClubRegistrationRequest clubRegistrationRequestDTO)
        {
            try
            {
                if (!GeneralHelper.IsValidImage(clubRegistrationRequestDTO.Crest)) return BadRequest(new { message = "Invalid image file." });

                Club newClub = new Club(clubRegistrationRequestDTO);

                if (await newClub.Save(_blobService))
                {
                    ClubRegistrationResponseDTO responseDTO = newClub.clubRegistrationResponseDTO;
                    return CreatedAtAction("find", new { id = responseDTO.ID }, responseDTO);
                }
                else
                {
                    return StatusCode(500, "The database failed to persist the record.");
                }
            }
            catch (BaseException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(ClubGetAllResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]

        public ActionResult<IEnumerable<ClubGetAllResponseDTO>> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                List<ClubGetAllResponseDTO> clubs = Club.GetAll(pageNumber, pageSize);

                if (clubs.Count > 0)
                {
                    return Ok(clubs);
                }
                return NotFound("No Clubs found!");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An internal error occurred.");
            }
        }


        [HttpGet("{id}", Name = "find")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]

        public ActionResult<ClubFindByIDResponseDTO> Find(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("The ID is invalid.");
                }

                ClubFindByIDResponseDTO dto = Club.Find(id);
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



        //[HttpPut]
        //[ProducesResponseType(typeof(ClubUpdateResponseDTO), 200)]
        //[ProducesResponseType(404)]
        //[ProducesResponseType(400)]
        //[ProducesResponseType(500)]

        //public ActionResult<ClubUpdateResponseDTO> Update(ClubUpdateRequestDTO clubUpdateRequestDTO)
        //{
        //    try
        //    {
                
        //        Club club = new Club(clubUpdateRequestDTO);
        //        if (club.Save())
        //        {
        //            return Ok(club.clubUpdateResponseDTO);
        //        }
                
        //        return NotFound("Club with ID " + clubUpdateRequestDTO.ID + " not found.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "An internal error occurred.");
        //    }
        //}

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ClubUpdateResponseDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("The ID is invalid.");
                }
    
                if (Club.Delete(id))
                {
                   return NoContent();
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

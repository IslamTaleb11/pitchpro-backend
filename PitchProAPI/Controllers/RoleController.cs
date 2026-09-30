using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.Role;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

using PitchProAPI.Middleware;

namespace PitchProAPI.Controllers
{
    [Authorize]
    [EnableRateLimiting("UserLimiter")]
    [Route("api/role")]
    [ApiController]
    [EmailVerified]
    public class RoleController : ControllerBase
    {

        //[HttpPost]
        //[ProducesResponseType(typeof(RoleRegistrationResponseDTO), 200)]
        //[ProducesResponseType(500)]
        //public ActionResult<RoleRegistrationResponseDTO> AddNew(RoleRegistrationRequestDTO roleRegistrationRequestDTO)
        //{
        //    try
        //    {

        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //}
    }
}

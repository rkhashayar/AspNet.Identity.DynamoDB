using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers
{
    [Route("sample")]
    public class SampleController : ControllerBase
    {
        /// <summary>
        /// no authorization endpoint
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult NoAuthAction()
        {
            return Ok("no login page");
        }

        /// <summary>
        /// no authorization endpoint
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("secure")]
        public IActionResult AuthAction()
        {
            return Ok(HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.Email).Value);
        }
    }
}

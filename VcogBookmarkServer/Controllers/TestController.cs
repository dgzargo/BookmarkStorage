using Microsoft.AspNetCore.Mvc;

namespace VcogBookmarkServer.Controllers
{
    [ApiController]
    public class TestController : ControllerBase
    {
        [Route("/")]
        public IActionResult DoesServerWork()
        {
            return Ok("It works!");
        }
    }
}
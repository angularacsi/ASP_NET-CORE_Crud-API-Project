using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    //i want to make this controller version nuetral, so i will add the ApiVersionNeutral attribute to the controller
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersionNeutral]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult CheckHealth()
        {
            return Ok(
                new
                {
                    Status = "Healthy",
                    Message = "API is running successfully.",
                    CheckedAt = DateTime.UtcNow,
                }
            );
        }
    }
}

using Crud.Dtos;
using Crud.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterUserController : ControllerBase
    {
        private readonly ILogger<RegisterUserController> _logger;
        private readonly IAuthService _authService;
        public RegisterUserController(IAuthService authService)
        {
            _authService = authService;

        }
        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDto registerDto)
        {
            if (registerDto == null)
            {
                return BadRequest("Register data is null.");
            }

            // Here you would typically call a service to handle the registration logic
            // For example:
            // await _authService.RegisterAsync(registerDto);
            await _authService.RegisterAsync(registerDto);
            return Ok("User registered successfully.");
        }
        [HttpPost("Login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return BadRequest("Login data is null.");
            }
            // Here you would typically call a service to handle the login logic
            // For example:
            //var user = await _authService.LoginAsync(loginDto);
            //if (user == null)
            //{
            //    return Unauthorized("Invalid username or password.");
            //}
            //return Ok(user);
            // change into token return 
            var token = await _authService.LoginAsync(loginDto);
            if (token == null)
            {
                return Unauthorized("Invalid username or password.");
            }
            // make it formated response 
            return Ok(token);


        }
        [HttpPost("Refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
        {
            var token = await _authService.RefreshAsync(request);
            if (token == null)
            {
                return Unauthorized("Invalid refresh token.");
            }
            return Ok(token);
        }
    }
}

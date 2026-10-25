using Crud.Dtos;
using Crud.Models;

namespace Crud.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto registerDto);
        // Task<User?> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto); // Now i want to return the token instead of user object
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> UserExistsAsync(string username);
    }
}

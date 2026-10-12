using Crud.Dtos;
using Crud.Models;

namespace Crud.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> UserExistsAsync(string username);

        Task RegisterAsync(User users);

        Task<User?> GetUserByUsernameAsync(string username);
    }
}

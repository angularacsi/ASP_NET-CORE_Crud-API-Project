using Crud.Data;
using Crud.Dtos;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.EntityFrameworkCore;

namespace Crud.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ILogger<AuthRepository> _logger;
        private readonly APiDbContext _aPiDb;

        public AuthRepository(APiDbContext aPiDbContext, ILogger<AuthRepository> logger)
        {
            _aPiDb = aPiDbContext;
            _logger = logger;
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {

            return _aPiDb.User.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _aPiDb.User.FirstOrDefaultAsync(u => u.Username == username);
        }
        public async Task RegisterAsync(User users)
        {
            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }
            await _aPiDb.User.AddAsync(users);
            await _aPiDb.SaveChangesAsync();

        }
        public async Task<bool> UserExistsAsync(string username)
        {
            return await _aPiDb.User.AnyAsync(u => u.Username == username);

        }
    }
}

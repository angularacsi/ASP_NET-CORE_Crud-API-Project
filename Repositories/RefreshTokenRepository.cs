using Crud.Dtos;
using Crud.Data;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.EntityFrameworkCore;

namespace Crud.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly APiDbContext _context;
        public RefreshTokenRepository(APiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }
}


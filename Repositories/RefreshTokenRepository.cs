using Crud.Data;
using Crud.Dtos;
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
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt =>
                rt.TokenHash == tokenHash
            );
        }

        public async Task<bool> RotateAsync(
            int oldTokenId,
            string oldTokenHash,
            DateTime now,
            string replacementHash,
            RefreshToken newToken
        )
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var affectedRows = await _context
                .RefreshTokens.Where(t =>
                    t.Id == oldTokenId
                    && t.TokenHash == oldTokenHash
                    && t.RevokedAt == null
                    && t.ExpiresAt > now
                )
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(t => t.RevokedAt, (DateTime?)now)
                        .SetProperty(t => t.ReplacedByTokenHash, replacementHash)
                );

            // Another request may have already consumed this token
            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _context.RefreshTokens.AddAsync(newToken);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return true;
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }
}

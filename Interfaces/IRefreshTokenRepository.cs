using Crud.Models;

namespace Crud.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken);

        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

        Task UpdateAsync(RefreshToken refreshToken);
    }
}
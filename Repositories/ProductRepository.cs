using Crud.Data;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.EntityFrameworkCore;

namespace Crud.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly APiDbContext _aPiDb;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(APiDbContext aPiDb, ILogger<ProductRepository> logger)
        {
            _aPiDb = aPiDb;
            _logger = logger;
        }

        public async Task AddAsync(Product product)
        {
            try
            {
                _logger.LogInformation("Adding product to database");

                await _aPiDb.Product.AddAsync(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                throw;
            }
        }

        public void Update(Product product)
        {
            try
            {
                _aPiDb.Product.Update(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        public void Delete(Product product)
        {
            try
            {
                _aPiDb.Product.Remove(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                throw;
            }
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _aPiDb.Product.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _aPiDb.Product.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task SaveAsync()
        {
            await _aPiDb.SaveChangesAsync();
        }
    }
}
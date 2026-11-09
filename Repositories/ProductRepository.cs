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

        public async Task<IEnumerable<Product>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _aPiDb
                .Product.AsNoTracking()
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _aPiDb.Product.CountAsync();
        }

        public async Task<IEnumerable<Product>> GetNextPageAsync(int lastId, int pageSize)
        {
            //keyset pagination implementation, this method will return the next page of products based on the last product Id and the given page size
            return await _aPiDb
                .Product.AsNoTracking()
                .OrderBy(p => p.Id)
                .Skip((lastId) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}

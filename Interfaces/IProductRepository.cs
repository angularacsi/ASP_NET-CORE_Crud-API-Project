using Crud.Models;

namespace Crud.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();

        // Returns a single product based on the given Id
        Task<Product?> GetByIdAsync(int id);

        // Returns a paginated list of products based on the given page number and page size
        Task<IEnumerable<Product>> GetPagedAsync(int pageNumber, int pageSize);

        // Returns the next page of products based on the last product Id and the given page size
        // This method is useful for implementing infinite scrolling or "load more" functionality in a client application.
        Task<IEnumerable<Product>> GetNextPageAsync(int lastId, int pageSize);

        //Add method for displaying the count of products in the database
        Task<int> GetCountAsync();

        // Adds a new product to the DbContext
        Task AddAsync(Product product);

        // Marks an existing product as modified
        void Update(Product product);

        // Marks a product for deletion
        void Delete(Product product);

        // Saves all pending changes to the database
        Task SaveAsync();
    }
}

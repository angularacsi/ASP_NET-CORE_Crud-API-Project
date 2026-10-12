using Crud.Data;
using Crud.Interfaces;
using Crud.Models;

namespace Crud.Repositories
{
    public class PostDetailRepository : IPostDetailRepository
    {
        private readonly APiDbContext _dbContext;

        public PostDetailRepository(APiDbContext dbContext ) {
          _dbContext = dbContext;
        }
        public async Task  AddPostDetails(Postdetails postdetails)
        {
            try
            {
                if (postdetails == null)
                {
                    throw new ArgumentNullException(nameof(postdetails));
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; // Re-throw the exception after logging
            }
            _dbContext.Postdetails.Add(postdetails);
            await _dbContext.SaveChangesAsync();

        }

        public Task DeletePostDetails(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<Postdetails>> GetAllPostDetails()
        {
            throw new NotImplementedException();
        }

        public Task<Postdetails> GetPostDetailsById(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePostDetails(Postdetails postdetails)
        {
            throw new NotImplementedException();
        }
    }
}

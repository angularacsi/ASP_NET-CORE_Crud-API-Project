using Crud.Models;

namespace Crud.Interfaces
{
    public interface IPostDetailRepository
    {
        Task AddPostDetails(Postdetails postdetails);
        Task<Postdetails> GetPostDetailsById(int id);
        Task UpdatePostDetails(Postdetails postdetails);
        Task DeletePostDetails(int id);
        Task<List<Postdetails>> GetAllPostDetails();
    }
}

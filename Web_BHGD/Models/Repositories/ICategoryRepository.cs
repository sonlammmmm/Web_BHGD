using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAll();
        Task<Category?> GetById(int id); // Đổi sang Category?
        Task Create(Category category);
        Task Update(Category category);
        Task Delete(int id);
    }
}
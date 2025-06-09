using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAll();
        Task<Product?> GetById(int id); // Đổi sang Product?
        Task Create(Product product);
        Task Update(Product product);
        Task Delete(int id);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAll();
        Task<Order?> GetById(int id); // Đổi sang Order?
        Task Create(Order order);
        Task Update(Order order);
        Task Delete(int id);
        Task<IEnumerable<Order>> GetOrdersByUserId(string userId);
    }
}
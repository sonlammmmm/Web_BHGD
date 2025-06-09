using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItem>> GetAll();
        Task<OrderItem?> GetById(int id); // Đổi từ Task<OrderItem> sang Task<OrderItem?>
        Task Create(OrderItem orderItem);
        Task Update(OrderItem orderItem);
        Task Delete(int id);
        Task CreateRange(IEnumerable<OrderItem> orderItems);
    }
}
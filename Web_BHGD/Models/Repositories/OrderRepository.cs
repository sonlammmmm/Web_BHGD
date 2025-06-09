using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            // Sử dụng ! sau OrderItems để báo hiệu compiler rằng nó sẽ không null khi truy cập
            return await _context.Orders.Include(o => o.OrderItems!).ThenInclude(oi => oi.Product).ToListAsync();
        }

        public async Task<Order?> GetById(int id) // Đổi sang Order?
        {
            return await _context.Orders
                .Include(o => o.OrderItems!).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task Create(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var _order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (_order != null)
            {
                _context.Orders.Remove(_order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserId(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems!).ThenInclude(oi => oi.Product)
                .ToListAsync();
        }
    }
}
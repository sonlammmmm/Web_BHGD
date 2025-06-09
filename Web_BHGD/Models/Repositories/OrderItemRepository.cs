using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderItem>> GetAll()
        {
            return await _context.OrderItems.ToListAsync();
        }

        public async Task<OrderItem?> GetById(int id) // Đổi sang OrderItem?
        {
            return await _context.OrderItems.FirstOrDefaultAsync(oi => oi.Id == id);
        }

        public async Task Create(OrderItem orderItem)
        {
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();
        }

        public async Task Update(OrderItem orderItem)
        {
            _context.OrderItems.Update(orderItem);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var _orderItem = await _context.OrderItems.FirstOrDefaultAsync(oi => oi.Id == id);
            if (_orderItem != null)
            {
                _context.OrderItems.Remove(_orderItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateRange(IEnumerable<OrderItem> orderItems)
        {
            _context.OrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();
        }
    }
}
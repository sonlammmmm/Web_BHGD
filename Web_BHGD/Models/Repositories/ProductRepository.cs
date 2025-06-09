using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            return await _context.Products
                .Include(n => n.Category).ToListAsync();
        }

        public async Task<Product?> GetById(int id) // Đổi sang Product?
        {
            return await _context.Products
                .Include(n => n.Category).FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task Create(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var _product = await _context.Products.FirstOrDefaultAsync(n => n.Id == id);
            if (_product != null)
            {
                _context.Products.Remove(_product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
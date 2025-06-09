using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web_BHGD.Models.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetById(int id) // Đổi sang Category?
        {
            return await _context.Categories
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task Create(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var _category = await _context.Categories
                .FirstOrDefaultAsync(n => n.Id == id);
            if (_category != null)
            {
                _context.Categories.Remove(_category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
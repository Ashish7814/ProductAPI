using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Infrastructure.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _set;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _set.FindAsync(new object[] { id }, ct);

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
            => await _set.AsNoTracking().ToListAsync(ct);

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _set.AddAsync(entity, ct);
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _set.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            _set.Remove(entity);
            return Task.CompletedTask;
        }
    }

    public class ProductRepository : Repository<Products>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Products?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
            => await _context.Products
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<(IEnumerable<Products> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken ct = default)
        {
            var query = _context.Products.Include(p => p.Items).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search));

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }

    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Item>> GetByProductIdAsync(int productId, CancellationToken ct = default)
            => await _context.Items.AsNoTracking()
                .Where(i => i.ProductId == productId)
                .ToListAsync(ct);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) => _context = context;

        public async Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => await _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        public async Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
            => await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

        public async Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken ct = default)
        {
            await _context.Users.AddAsync(user, ct);
            return user;
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken ct = default)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default)
            => await _context.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);
    }
}

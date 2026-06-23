using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Product.Domain.Entities;

namespace Product.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<T> AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);
    }

    public interface IProductRepository : IRepository<Products>
    {
        Task<Products?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);
        Task<(IEnumerable<Products> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken ct = default);
    }

    public interface IItemRepository : IRepository<Item>
    {
        Task<IEnumerable<Item>> GetByProductIdAsync(int productId, CancellationToken ct = default);
    }

    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
        Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken ct = default);
        Task UpdateAsync(ApplicationUser user, CancellationToken ct = default);
        Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default);
    }

    public interface IUnitOfWork
    {
        IProductRepository Products { get; }
        IItemRepository Items { get; }
        IUserRepository Users { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}

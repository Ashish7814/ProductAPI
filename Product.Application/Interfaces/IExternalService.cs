using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendProductCreatedNotificationAsync(string recipientEmail, string productName, string createdBy, CancellationToken ct = default);
        Task SendProductDeletedNotificationAsync(string recipientEmail, string productName, CancellationToken ct = default);
    }

    /// <summary>Abstraction for password hashing. Implemented in Infrastructure.</summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    /// <summary>Abstraction for caching. Implemented in Infrastructure.</summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
        Task RemoveAsync(string key, CancellationToken ct = default);
        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    }

}

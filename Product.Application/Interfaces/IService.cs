using Product.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default);
        Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProductDto> CreateAsync(CreateProductRequest request, string createdBy, CancellationToken ct = default);
        Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, string modifiedBy, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        Task<IEnumerable<ItemDto>> GetItemsAsync(int productId, CancellationToken ct = default);
        Task<ItemDto> AddItemAsync(int productId, AddItemRequest request, CancellationToken ct = default);
        Task<ItemDto> UpdateItemAsync(int productId, int itemId, UpdateItemRequest request, CancellationToken ct = default);
        Task DeleteItemAsync(int productId, int itemId, CancellationToken ct = default);
    }

    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
        Task RevokeAsync(string username, CancellationToken ct = default);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(Domain.Entities.ApplicationUser user);
        string GenerateRefreshToken();
        System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}

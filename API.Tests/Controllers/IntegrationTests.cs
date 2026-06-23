using FluentAssertions;
using Microsoft.AspNetCore.Identity.Data;
using Product.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace API.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<ProductApiFactory>
    {
        private readonly HttpClient _client;

        public AuthControllerTests(ProductApiFactory factory)
            => _client = factory.CreateClient();

        [Fact]
        public async Task Register_WithValidRequest_Returns201()
        {
            var request = new Product.Application.DTOs.RegisterRequest("testuser_" + Guid.NewGuid().ToString("N")[..6],
                $"test_{Guid.NewGuid():N}@test.com", "StrongPass1");

            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
            body!.AccessToken.Should().NotBeNullOrEmpty();
            body.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithDuplicateUser_Returns409()
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var request = new Product.Application.DTOs.RegisterRequest($"dup_{unique}", $"dup_{unique}@test.com", "StrongPass1");

            await _client.PostAsJsonAsync("/api/v1/auth/register", request);
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200()
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var request = new Product.Application.DTOs.RegisterRequest($"login_{unique}", $"login_{unique}@test.com", "StrongPass1");
            await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
                new Product.Application.DTOs.LoginRequest(request.Username, "StrongPass1"));

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
                new Product.Application.DTOs.LoginRequest("nobody", "WrongPass1"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    public class ProductsControllerTests : IClassFixture<ProductApiFactory>
    {
        private readonly HttpClient _client;
        private readonly ProductApiFactory _factory;

        public ProductsControllerTests(ProductApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<string> GetTokenAsync()
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var reg = new Product.Application.DTOs.RegisterRequest($"prod_{unique}", $"prod_{unique}@test.com", "StrongPass1");
            var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
            var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
            return auth!.AccessToken;
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var reg = new Product.Application.DTOs.RegisterRequest($"admin_{unique}", $"admin_{unique}@test.com", "StrongPass1", "Admin");
            var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
            var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
            return auth!.AccessToken;
        }

        [Fact]
        public async Task GetAll_WithoutAuth_Returns401()
        {
            var response = await _client.GetAsync("/api/v1/products");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WithAuth_Returns200()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/v1/products");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_WithValidRequest_Returns201()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new CreateProductRequest("Test Product", 5);
            var response = await _client.PostAsJsonAsync("/api/v1/products", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            product!.ProductName.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetById_WithExistingProduct_Returns200()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResp = await _client.PostAsJsonAsync("/api/v1/products",
                new CreateProductRequest("GetById Product", 1));
            var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            var response = await _client.GetAsync($"/api/v1/products/{created!.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_WithNonExistentId_Returns404()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/v1/products/999999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithValidRequest_Returns200()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResp = await _client.PostAsJsonAsync("/api/v1/products",
                new CreateProductRequest("Original", 1));
            var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            var updateResp = await _client.PutAsJsonAsync($"/api/v1/products/{created!.Id}",
                new UpdateProductRequest("Updated Name"));

            updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await updateResp.Content.ReadFromJsonAsync<ProductDto>();
            updated!.ProductName.Should().Be("Updated Name");
        }

        [Fact]
        public async Task Delete_WithAdminRole_Returns204()
        {
            var adminToken = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var createResp = await _client.PostAsJsonAsync("/api/v1/products",
                new CreateProductRequest("To Delete", 1));
            var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            var deleteResp = await _client.DeleteAsync($"/api/v1/products/{created!.Id}");
            deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_WithUserRole_Returns403()
        {
            var userToken = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            // Create a product as user, then try to delete
            var createResp = await _client.PostAsJsonAsync("/api/v1/products",
                new CreateProductRequest("Cannot Delete", 1));
            var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            var deleteResp = await _client.DeleteAsync($"/api/v1/products/{created!.Id}");
            deleteResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Health_Returns200()
        {
            var response = await _client.GetAsync("/api/v1/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}

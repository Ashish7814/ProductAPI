using FluentAssertions;
using Product.Domain.Entities;
using Product.Domain.Enums;
using Product.Infrastructure.Data;
using Product.Infrastructure.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Repositories
{
    public class ItemRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ItemRepository _sut;

        public ItemRepositoryTests()
        {
            _context = DbContextFactory.Create();
            _sut = new ItemRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        private async Task<Products> SeedProductAsync()
        {
            var p = new Products
            {
                ProductName = "Parent Product",
                CreatedBy = "seed",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };
            await _context.Products.AddAsync(p);
            await _context.SaveChangesAsync();
            return p;
        }

        [Fact]
        public async Task GetByProductIdAsync_ReturnsOnlyMatchingItems()
        {
            var p1 = await SeedProductAsync();
            var p2 = await SeedProductAsync();

            await _context.Items.AddRangeAsync(
                new Item { ProductId = p1.Id, Quantity = 10 },
                new Item { ProductId = p1.Id, Quantity = 20 },
                new Item { ProductId = p2.Id, Quantity = 30 }
            );
            await _context.SaveChangesAsync();

            var items = await _sut.GetByProductIdAsync(p1.Id);

            items.Should().HaveCount(2);
            items.All(i => i.ProductId == p1.Id).Should().BeTrue();
        }

        [Fact]
        public async Task GetByProductIdAsync_WhenNoItems_ReturnsEmpty()
        {
            var p = await SeedProductAsync();
            var items = await _sut.GetByProductIdAsync(p.Id);
            items.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_PersistsItem()
        {
            var p = await SeedProductAsync();
            var item = new Item { ProductId = p.Id, Quantity = 42 };

            await _sut.AddAsync(item);
            await _context.SaveChangesAsync();

            var saved = await _sut.GetByIdAsync(item.Id);
            saved.Should().NotBeNull();
            saved!.Quantity.Should().Be(42);
        }

        [Fact]
        public async Task UpdateAsync_ChangesQuantity()
        {
            var p = await SeedProductAsync();
            var item = new Item { ProductId = p.Id, Quantity = 5 };
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            item.Quantity = 99;
            await _sut.UpdateAsync(item);
            await _context.SaveChangesAsync();

            var updated = await _sut.GetByIdAsync(item.Id);
            updated!.Quantity.Should().Be(99);
        }
    }

    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _sut;

        public UserRepositoryTests()
        {
            _context = DbContextFactory.Create();
            _sut = new UserRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        private ApplicationUser BuildUser(string suffix = "")
        {
            var uid = Guid.NewGuid().ToString("N")[..6] + suffix;
            return new ApplicationUser
            {
                Username = $"user_{uid}",
                Email = $"{uid}@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1"),
                Role = "User",
                CreatedOn = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task AddAsync_ThenGetByUsername_ReturnsUser()
        {
            var user = BuildUser();
            await _sut.AddAsync(user);
            await _context.SaveChangesAsync();

            var found = await _sut.GetByUsernameAsync(user.Username);
            found.Should().NotBeNull();
            found!.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetByUsernameAsync_WhenNotFound_ReturnsNull()
        {
            var result = await _sut.GetByUsernameAsync("ghost_user_xyz");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_WhenUsernameMatches_ReturnsTrue()
        {
            var user = BuildUser();
            await _sut.AddAsync(user);
            await _context.SaveChangesAsync();

            var exists = await _sut.ExistsAsync(user.Username, "other@email.com");
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenEmailMatches_ReturnsTrue()
        {
            var user = BuildUser();
            await _sut.AddAsync(user);
            await _context.SaveChangesAsync();

            var exists = await _sut.ExistsAsync("other_username", user.Email);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenNeitherMatches_ReturnsFalse()
        {
            var exists = await _sut.ExistsAsync("nobody", "nobody@nowhere.com");
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_PersistsRefreshToken()
        {
            var user = BuildUser();
            await _sut.AddAsync(user);
            await _context.SaveChangesAsync();

            user.RefreshToken = "my-refresh-token";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _sut.UpdateAsync(user);
            await _context.SaveChangesAsync();

            var updated = await _sut.GetByUsernameAsync(user.Username);
            updated!.RefreshToken.Should().Be("my-refresh-token");
        }

        [Fact]
        public async Task GetByRefreshTokenAsync_ReturnsCorrectUser()
        {
            var user = BuildUser();
            user.RefreshToken = "unique-rt-value";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);
            await _sut.AddAsync(user);
            await _context.SaveChangesAsync();

            var found = await _sut.GetByRefreshTokenAsync("unique-rt-value");
            found.Should().NotBeNull();
            found!.Username.Should().Be(user.Username);
        }
    }
}

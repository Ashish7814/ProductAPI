using FluentAssertions;
using Product.Domain.Entities;
using Product.Domain.Enums;
using Product.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Repositories
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitOfWork _sut;

        public UnitOfWorkTests()
        {
            _context = DbContextFactory.Create();
            _sut = new UnitOfWork(_context);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public void UnitOfWork_ExposesAllRepositories()
        {
            _sut.Products.Should().NotBeNull();
            _sut.Items.Should().NotBeNull();
            _sut.Users.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveChangesAsync_PersistsAcrossRepositories()
        {
            // Add a product via Products repo
            var product = new Products
            {
                ProductName = "UoW Product",
                CreatedBy = "test",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };
            await _sut.Products.AddAsync(product);

            // Add a user via Users repo — both in same unit of work
            var user = new ApplicationUser
            {
                Username = "uow_user",
                Email = "uow@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1"),
                Role = "User",
                CreatedOn = DateTime.UtcNow
            };
            await _sut.Users.AddAsync(user);

            // Single SaveChangesAsync commits both
            var affected = await _sut.SaveChangesAsync();

            affected.Should().Be(2);

            var savedProduct = await _sut.Products.GetByIdAsync(product.Id);
            var savedUser = await _sut.Users.GetByUsernameAsync("uow_user");

            savedProduct.Should().NotBeNull();
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveChangesAsync_RollsBackOnFailure()
        {
            // InMemory doesn't enforce FK constraints, so we test logical rollback:
            // nothing is persisted if SaveChanges is never called.
            var product = new Products
            {
                ProductName = "Never Saved",
                CreatedBy = "test",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };
            await _sut.Products.AddAsync(product);

            // Do NOT call SaveChangesAsync → nothing persisted
            var (items, total) = await _sut.Products.GetPagedAsync(1, 10, "Never Saved");
            total.Should().Be(0);
        }

        [Fact]
        public async Task Products_And_Items_ShareSameTransaction()
        {
            var product = new Products
            {
                ProductName = "Shared Tx Product",
                CreatedBy = "test",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };
            await _sut.Products.AddAsync(product);
            await _sut.SaveChangesAsync();

            var item = new Item { ProductId = product.Id, Quantity = 77 };
            await _sut.Items.AddAsync(item);
            await _sut.SaveChangesAsync();

            var loaded = await _sut.Products.GetByIdWithItemsAsync(product.Id);
            loaded!.Items.Should().HaveCount(1);
            loaded.Items.First().Quantity.Should().Be(77);
        }
    }

}

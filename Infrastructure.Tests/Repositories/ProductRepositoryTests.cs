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
    public class ProductRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductRepository _sut;

        public ProductRepositoryTests()
        {
            _context = DbContextFactory.Create();
            _sut = new ProductRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        // ── Seed helper ─────────────────────────────────────────────────────────

        private async Task<Products> SeedProductAsync(string name = "Test Product", int itemQty = 0)
        {
            var product = new Products
            {
                ProductName = name,
                CreatedBy = "seed_user",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };
            await _context.Products.AddAsync(product);

            if (itemQty > 0)
                await _context.Items.AddAsync(new Item { Product = product, Quantity = itemQty });

            await _context.SaveChangesAsync();
            return product;
        }

        // ── GetByIdAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WhenExists_ReturnsProduct()
        {
            var seeded = await SeedProductAsync("Widget A");
            var result = await _sut.GetByIdAsync(seeded.Id);
            result.Should().NotBeNull();
            result!.ProductName.Should().Be("Widget A");
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            var result = await _sut.GetByIdAsync(99999);
            result.Should().BeNull();
        }

        // ── GetByIdWithItemsAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByIdWithItemsAsync_IncludesItems()
        {
            var seeded = await SeedProductAsync("With Items", itemQty: 15);
            var result = await _sut.GetByIdWithItemsAsync(seeded.Id);

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(1);
            result.Items.First().Quantity.Should().Be(15);
        }

        [Fact]
        public async Task GetByIdWithItemsAsync_WhenNotFound_ReturnsNull()
        {
            var result = await _sut.GetByIdWithItemsAsync(99999);
            result.Should().BeNull();
        }

        // ── GetPagedAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_ReturnsCorrectPage()
        {
            for (int i = 1; i <= 15; i++)
                await SeedProductAsync($"Product {i:D2}");

            var (items, total) = await _sut.GetPagedAsync(page: 1, pageSize: 10, search: null);

            total.Should().Be(15);
            items.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_ReturnsRemainder()
        {
            for (int i = 1; i <= 15; i++)
                await SeedProductAsync($"Paged {i:D2}");

            var (items, total) = await _sut.GetPagedAsync(page: 2, pageSize: 10, search: null);

            total.Should().Be(15);
            items.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearch_FiltersResults()
        {
            await SeedProductAsync("Alpha Widget");
            await SeedProductAsync("Beta Gadget");
            await SeedProductAsync("Alpha Gizmo");

            var (items, total) = await _sut.GetPagedAsync(1, 10, search: "Alpha");

            total.Should().Be(2);
            items.All(p => p.ProductName.Contains("Alpha")).Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedAsync_WithNoMatch_ReturnsEmpty()
        {
            await SeedProductAsync("Visible Product");

            var (items, total) = await _sut.GetPagedAsync(1, 10, search: "ZZZNonExistent");

            total.Should().Be(0);
            items.Should().BeEmpty();
        }

        // ── AddAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_PersistsProduct()
        {
            var product = new Products
            {
                ProductName = "New Product",
                CreatedBy = "user1",
                CreatedOn = DateTime.UtcNow,
                Status = ProductStatus.Active
            };

            await _sut.AddAsync(product);
            await _context.SaveChangesAsync();

            var saved = await _sut.GetByIdAsync(product.Id);
            saved.Should().NotBeNull();
            saved!.ProductName.Should().Be("New Product");
        }

        // ── UpdateAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_PersistsChanges()
        {
            var product = await SeedProductAsync("Original");
            product.ProductName = "Renamed";
            product.ModifiedBy = "editor";
            product.ModifiedOn = DateTime.UtcNow;

            await _sut.UpdateAsync(product);
            await _context.SaveChangesAsync();

            var updated = await _sut.GetByIdAsync(product.Id);
            updated!.ProductName.Should().Be("Renamed");
            updated.ModifiedBy.Should().Be("editor");
        }

        // ── DeleteAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_RemovesProduct()
        {
            var product = await SeedProductAsync("To Delete");

            await _sut.DeleteAsync(product);
            await _context.SaveChangesAsync();

            var deleted = await _sut.GetByIdAsync(product.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_CascadesItems()
        {
            var product = await SeedProductAsync("With Items", itemQty: 5);
            var productId = product.Id;

            await _sut.DeleteAsync(product);
            await _context.SaveChangesAsync();

            var orphanItems = _context.Items.Where(i => i.ProductId == productId).ToList();
            orphanItems.Should().BeEmpty();
        }
    }

}

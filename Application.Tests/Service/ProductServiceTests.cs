using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Application.Mapping;
using Product.Application.Services;
using Product.Domain.Entities;
using Product.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Service
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IProductRepository> _productRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ProductService>> _loggerMock = new();
        private readonly ProductService _sut;

        public ProductServiceTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
            _uowMock.Setup(u => u.Items).Returns(_itemRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _sut = new ProductService(_uowMock.Object, _mapper, _loggerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductExists_ReturnsProductDto()
        {
            // Arrange
            var product = new Products
            {
                Id = 1,
                ProductName = "Test Product",
                CreatedBy = "admin",
                CreatedOn = DateTime.UtcNow,
                Items = new List<Item> { new Item { Id = 1, ProductId = 1, Quantity = 10 } }
            };
            _productRepoMock
                .Setup(r => r.GetByIdWithItemsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // Act
            var result = await _sut.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.ProductName.Should().Be("Test Product");
            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _productRepoMock
                .Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Products?)null);

            // Act & Assert
            await _sut.Invoking(s => s.GetByIdAsync(99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ReturnsProductDto()
        {
            // Arrange
            var request = new CreateProductRequest("New Product", 5);
            var createdProduct = new Products
            {
                Id = 1,
                ProductName = "New Product",
                CreatedBy = "user1",
                CreatedOn = DateTime.UtcNow,
                Items = new List<Item> { new Item { Id = 1, ProductId = 1, Quantity = 5 } }
            };

            _productRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Products>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Products p, CancellationToken _) => p);

            _itemRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item i, CancellationToken _) => i);

            _productRepoMock
                .Setup(r => r.GetByIdWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdProduct);

            // Act
            var result = await _sut.CreateAsync(request, "user1");

            // Assert
            result.Should().NotBeNull();
            result.ProductName.Should().Be("New Product");
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductExists_UpdatesAndReturns()
        {
            // Arrange
            var product = new Products
            {
                Id = 1,
                ProductName = "Old Name",
                CreatedBy = "admin",
                CreatedOn = DateTime.UtcNow,
                Items = new List<Item>()
            };
            var request = new UpdateProductRequest("New Name");

            _productRepoMock
                .Setup(r => r.GetByIdWithItemsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _productRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Products>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateAsync(1, request, "editor");

            // Assert
            result.ProductName.Should().Be("New Name");
            result.ModifiedBy.Should().Be("editor");
        }

        [Fact]
        public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _productRepoMock
                .Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Products?)null);

            // Act & Assert
            await _sut.Invoking(s => s.UpdateAsync(99, new UpdateProductRequest("X"), "user"))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_WhenProductExists_DeletesSuccessfully()
        {
            // Arrange
            var product = new Products { Id = 1, ProductName = "P", CreatedBy = "admin", CreatedOn = DateTime.UtcNow };
            _productRepoMock
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _productRepoMock
                .Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteAsync(1);

            // Assert
            _productRepoMock.Verify(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsPaginatedResults()
        {
            // Arrange
            var products = new List<Products>
        {
            new() { Id = 1, ProductName = "A", CreatedBy = "u", CreatedOn = DateTime.UtcNow, Items = new List<Item>() },
            new() { Id = 2, ProductName = "B", CreatedBy = "u", CreatedOn = DateTime.UtcNow, Items = new List<Item>() }
        };

            _productRepoMock
                .Setup(r => r.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((products, 2));

            // Act
            var result = await _sut.GetAllAsync(1, 10, null);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.TotalPages.Should().Be(1);
        }
    }

}

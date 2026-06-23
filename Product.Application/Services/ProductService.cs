using AutoMapper;
using Microsoft.Extensions.Logging;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize, string? search, CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching products: page={Page}, pageSize={PageSize}, search={Search}", page, pageSize, search);
            var (items, totalCount) = await _uow.Products.GetPagedAsync(page, pageSize, search, ct);
            var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return new PagedResult<ProductDto>(dtos, totalCount, page, pageSize, totalPages);
        }

        public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var product = await _uow.Products.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateAsync(CreateProductRequest request, string createdBy, CancellationToken ct = default)
        {
            _logger.LogInformation("Creating product '{ProductName}' by {CreatedBy}", request.ProductName, createdBy);

            var product = new Products
            {
                ProductName = request.ProductName,
                CreatedBy = createdBy,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Products.AddAsync(product, ct);
            await _uow.SaveChangesAsync(ct);

            if (request.InitialQuantity > 0)
            {
                var item = new Item { ProductId = product.Id, Quantity = request.InitialQuantity };
                await _uow.Items.AddAsync(item, ct);
                await _uow.SaveChangesAsync(ct);
            }

            var created = await _uow.Products.GetByIdWithItemsAsync(product.Id, ct);
            return _mapper.Map<ProductDto>(created!);
        }

        public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, string modifiedBy, CancellationToken ct = default)
        {
            var product = await _uow.Products.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

            product.ProductName = request.ProductName;
            product.ModifiedBy = modifiedBy;
            product.ModifiedOn = DateTime.UtcNow;

            if (request.Status.HasValue)
                product.Status = request.Status.Value;

            await _uow.Products.UpdateAsync(product, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product {Id} updated by {ModifiedBy}", id, modifiedBy);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var product = await _uow.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

            await _uow.Products.DeleteAsync(product, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Product {Id} deleted", id);
        }

        public async Task<IEnumerable<ItemDto>> GetItemsAsync(int productId, CancellationToken ct = default)
        {
            _ = await _uow.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException(nameof(Product), productId);

            var items = await _uow.Items.GetByProductIdAsync(productId, ct);
            return _mapper.Map<IEnumerable<ItemDto>>(items);
        }

        public async Task<ItemDto> AddItemAsync(int productId, AddItemRequest request, CancellationToken ct = default)
        {
            _ = await _uow.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException(nameof(Product), productId);

            var item = new Item { ProductId = productId, Quantity = request.Quantity };
            await _uow.Items.AddAsync(item, ct);
            await _uow.SaveChangesAsync(ct);

            return _mapper.Map<ItemDto>(item);
        }

        public async Task<ItemDto> UpdateItemAsync(int productId, int itemId, UpdateItemRequest request, CancellationToken ct = default)
        {
            _ = await _uow.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException(nameof(Product), productId);

            var item = await _uow.Items.GetByIdAsync(itemId, ct)
                ?? throw new NotFoundException(nameof(Item), itemId);

            if (item.ProductId != productId)
                throw new NotFoundException(nameof(Item), itemId);

            item.Quantity = request.Quantity;
            await _uow.Items.UpdateAsync(item, ct);
            await _uow.SaveChangesAsync(ct);

            return _mapper.Map<ItemDto>(item);
        }

        public async Task DeleteItemAsync(int productId, int itemId, CancellationToken ct = default)
        {
            _ = await _uow.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException(nameof(Product), productId);

            var item = await _uow.Items.GetByIdAsync(itemId, ct)
                ?? throw new NotFoundException(nameof(Item), itemId);

            if (item.ProductId != productId)
                throw new NotFoundException(nameof(Item), itemId);

            await _uow.Items.DeleteAsync(item, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }
}

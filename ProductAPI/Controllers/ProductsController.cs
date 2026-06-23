using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using System.Security.Claims;

namespace ProductAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _productService.GetAllAsync(page, pageSize, search, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var product = await _productService.GetByIdAsync(id, ct);
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Policy = "UserOrAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "system";
            var product = await _productService.CreateAsync(request, username, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "UserOrAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "system";
            var product = await _productService.UpdateAsync(id, request, username, ct);
            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _productService.DeleteAsync(id, ct);
            return NoContent();
        }

        [HttpGet("{productId:int}/items")]
        public async Task<IActionResult> GetItems(int productId, CancellationToken ct)
        {
            var items = await _productService.GetItemsAsync(productId, ct);
            return Ok(items);
        }

        [HttpPost("{productId:int}/items")]
        [Authorize(Policy = "UserOrAdmin")]
        public async Task<IActionResult> AddItem(int productId, [FromBody] AddItemRequest request, CancellationToken ct)
        {
            var item = await _productService.AddItemAsync(productId, request, ct);
            return CreatedAtAction(nameof(GetItems), new { productId }, item);
        }

        [HttpPut("{productId:int}/items/{itemId:int}")]
        [Authorize(Policy = "UserOrAdmin")]
        public async Task<IActionResult> UpdateItem(int productId, int itemId, [FromBody] UpdateItemRequest request, CancellationToken ct)
        {
            var item = await _productService.UpdateItemAsync(productId, itemId, request, ct);
            return Ok(item);
        }

        [HttpDelete("{productId:int}/items/{itemId:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteItem(int productId, int itemId, CancellationToken ct)
        {
            await _productService.DeleteItemAsync(productId, itemId, ct);
            return NoContent();
        }
    }
}

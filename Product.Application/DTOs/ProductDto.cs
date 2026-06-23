using Product.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.DTOs
{
    public record ProductDto(
    int Id,
    string ProductName,
    string CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn,
    string Status,
    IEnumerable<ItemDto> Items
);

    public record ItemDto(
        int Id,
        int ProductId,
        int Quantity
    );

    public record CreateProductRequest(
        string ProductName,
        int InitialQuantity
    );

    public record UpdateProductRequest(
        string ProductName,
        ProductStatus? Status = null
    );

    public record AddItemRequest(
        int Quantity
    );

    public record UpdateItemRequest(
        int Quantity
    );

    public record PagedResult<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );
}

using AutoMapper;
using Product.Application.DTOs;
using Product.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Product.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Products, ProductDto>()
                .ConstructUsing(src => new ProductDto(
                    src.Id,
                    src.ProductName,
                    src.CreatedBy,
                    src.CreatedOn,
                    src.ModifiedBy,
                    src.ModifiedOn,
                    src.Status.ToString(),
                    src.Items.Select(i => new ItemDto(i.Id, i.ProductId, i.Quantity))
                ));

            CreateMap<Item, ItemDto>()
                .ConstructUsing(src => new ItemDto(src.Id, src.ProductId, src.Quantity));
        }
    }
}

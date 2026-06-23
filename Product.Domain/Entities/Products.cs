using Product.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Domain.Entities
{
    public class Products
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}

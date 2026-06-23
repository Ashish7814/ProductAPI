using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Domain.Events
{
    public abstract class DomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public class ProductCreatedEvent : DomainEvent
    {
        public int ProductId { get; }
        public string ProductName { get; }
        public string CreatedBy { get; }

        public ProductCreatedEvent(int productId, string productName, string createdBy)
        {
            ProductId = productId;
            ProductName = productName;
            CreatedBy = createdBy;
        }
    }

    public class ProductUpdatedEvent : DomainEvent
    {
        public int ProductId { get; }
        public string ProductName { get; }
        public string ModifiedBy { get; }

        public ProductUpdatedEvent(int productId, string productName, string modifiedBy)
        {
            ProductId = productId;
            ProductName = productName;
            ModifiedBy = modifiedBy;
        }
    }

    public class ProductDeletedEvent : DomainEvent
    {
        public int ProductId { get; }

        public ProductDeletedEvent(int productId)
        {
            ProductId = productId;
        }
    }
}

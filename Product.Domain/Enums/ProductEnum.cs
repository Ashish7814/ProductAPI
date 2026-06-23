using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Domain.Enums
{
    public enum ProductStatus
    {
        Active = 1,
        Inactive = 2,
        Discontinued = 3,
        PendingReview = 4
    }

    public enum UserRole
    {
        User = 1,
        Admin = 2
    }

    public enum SortDirection
    {
        Ascending = 1,
        Descending = 2
    }

    public enum ProductSortField
    {
        CreatedOn = 1,
        ProductName = 2,
        ModifiedOn = 3
    }
}

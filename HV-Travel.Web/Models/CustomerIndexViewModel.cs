using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models
{
    public class CustomerIndexViewModel
    {
        public IEnumerable<Customer> Customers { get; set; }
        public PaginationMetadata Pagination { get; set; }
        public CustomerStatsSummary Stats { get; set; }
    }

    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    public class CustomerStatsSummary
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersCount { get; set; } // Customers added recently or with "New" segment
        public Dictionary<string, double> SegmentPercentages { get; set; }
    }
}

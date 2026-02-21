using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models
{
    public class UserIndexViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
        public UserStatsSummary Stats { get; set; } = new UserStatsSummary();
    }

    public class UserStatsSummary
    {
        public int TotalUsers { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public Dictionary<string, int> RoleCounts { get; set; } = new Dictionary<string, int>();
    }
}

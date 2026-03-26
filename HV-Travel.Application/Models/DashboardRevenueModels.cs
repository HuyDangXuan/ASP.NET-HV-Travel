namespace HVTravel.Application.Models
{
    public enum DashboardRevenueRange
    {
        Week,
        Month,
        Year
    }

    public class DashboardRevenuePoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class DashboardRevenueStatsResult
    {
        public DashboardRevenueRange Range { get; set; }
        public string RangeKey { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public double? GrowthPercentage { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string PreviousPeriodLabel { get; set; } = string.Empty;
        public List<DashboardRevenuePoint> Points { get; set; } = new();
    }

    public class DashboardRevenueOverviewResult
    {
        public decimal AllTimeRevenue { get; set; }
        public decimal ChartTotalRevenue { get; set; }
        public double? RevenueGrowthPercentage { get; set; }
        public DashboardRevenueStatsResult DefaultChart { get; set; } = new();
    }
}

using System.Collections;
using System.Linq.Expressions;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;

namespace HV_Travel.Web.Tests;

public class DashboardRevenueServiceTests
{
    [Fact]
    public async Task GetRevenueStatsAsync_WeekRange_UsesRealizedRevenueAndAsiaSaigonGrouping()
    {
        var service = CreateService(
            nowUtc: new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero),
            bookings:
            [
                CreateBooking(1_000_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 23, 1, 0, 0, DateTimeKind.Utc)),
                CreateBooking(2_500_000m, "Completed", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 24, 17, 30, 0, DateTimeKind.Utc)),
                CreateBooking(9_900_000m, "Confirmed", paymentStatus: "Unpaid", bookingDateUtc: new DateTime(2026, 3, 25, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(8_800_000m, "Pending", paymentStatus: "Pending", bookingDateUtc: new DateTime(2026, 3, 26, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(7_700_000m, "Cancelled", paymentStatus: "Unpaid", bookingDateUtc: new DateTime(2026, 3, 27, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(6_600_000m, "Refunded", paymentStatus: "Refunded", bookingDateUtc: new DateTime(2026, 3, 28, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(5_500_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 29, 2, 0, 0, DateTimeKind.Utc), isDeleted: true)
            ]);

        var result = await InvokeStatsAsync(service, "Week");

        Assert.Equal(3_500_000m, GetProperty<decimal>(result, "TotalRevenue"));

        var points = GetPoints(result, "Points");
        Assert.Equal(new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" }, points.Select(p => p.Label));
        Assert.Equal(new decimal[] { 1_000_000m, 0m, 2_500_000m, 0m, 0m, 0m, 0m }, points.Select(p => p.Value));
    }

    [Fact]
    public async Task GetRevenueStatsAsync_MonthRange_ZeroFillsEveryDayOfCurrentMonth()
    {
        var service = CreateService(
            nowUtc: new DateTimeOffset(2026, 3, 15, 8, 0, 0, TimeSpan.Zero),
            bookings:
            [
                CreateBooking(400_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 2, 28, 17, 30, 0, DateTimeKind.Utc)),
                CreateBooking(900_000m, "Completed", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 14, 18, 30, 0, DateTimeKind.Utc))
            ]);

        var result = await InvokeStatsAsync(service, "Month");
        var points = GetPoints(result, "Points");

        Assert.Equal(31, points.Count);
        Assert.Equal("01/03", points[0].Label);
        Assert.Equal(400_000m, points[0].Value);
        Assert.Equal("15/03", points[14].Label);
        Assert.Equal(900_000m, points[14].Value);
        Assert.Equal(0m, points[13].Value);
        Assert.Equal(1_300_000m, GetProperty<decimal>(result, "TotalRevenue"));
    }

    [Fact]
    public async Task GetRevenueStatsAsync_YearRange_ZeroFillsEveryMonthOfCurrentYear()
    {
        var service = CreateService(
            nowUtc: new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
            bookings:
            [
                CreateBooking(700_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 1, 4, 3, 0, 0, DateTimeKind.Utc)),
                CreateBooking(800_000m, "Completed", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 7, 9, 23, 0, 0, DateTimeKind.Utc)),
                CreateBooking(1_200_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc))
            ]);

        var result = await InvokeStatsAsync(service, "Year");
        var points = GetPoints(result, "Points");

        Assert.Equal(12, points.Count);
        Assert.Equal("Th1", points[0].Label);
        Assert.Equal(1_900_000m, points[0].Value);
        Assert.Equal("Th7", points[6].Label);
        Assert.Equal(800_000m, points[6].Value);
        Assert.All(points.Where((_, index) => index is not 0 and not 6), point => Assert.Equal(0m, point.Value));
        Assert.Equal(2_700_000m, GetProperty<decimal>(result, "TotalRevenue"));
    }

    [Fact]
    public async Task GetRevenueStatsAsync_ReturnsNullGrowthWhenPreviousPeriodHasNoRevenue()
    {
        var service = CreateService(
            nowUtc: new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero),
            bookings:
            [
                CreateBooking(2_000_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 24, 3, 0, 0, DateTimeKind.Utc))
            ]);

        var result = await InvokeStatsAsync(service, "Week");

        Assert.Equal(0m, GetProperty<decimal>(result, "PreviousPeriodRevenue"));
        Assert.Null(GetNullableDoubleProperty(result, "GrowthPercentage"));
    }

    [Fact]
    public async Task GetRevenueOverviewAsync_ReturnsAllTimeRealizedRevenueAndCurrentMonthGrowth()
    {
        var service = CreateService(
            nowUtc: new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero),
            bookings:
            [
                CreateBooking(3_000_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 5, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(2_000_000m, "Completed", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 2, 12, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(9_000_000m, "Confirmed", paymentStatus: "Unpaid", bookingDateUtc: new DateTime(2026, 3, 9, 2, 0, 0, DateTimeKind.Utc)),
                CreateBooking(4_000_000m, "Paid", paymentStatus: "Full", bookingDateUtc: new DateTime(2026, 3, 18, 2, 0, 0, DateTimeKind.Utc), isDeleted: true)
            ]);

        var result = await InvokeOverviewAsync(service, "Week");

        Assert.Equal(5_000_000m, GetProperty<decimal>(result, "AllTimeRevenue"));
        Assert.Equal(0m, GetProperty<decimal>(result, "ChartTotalRevenue"));
        Assert.Equal(50d, GetNullableDoubleProperty(result, "RevenueGrowthPercentage"));

        var chart = GetProperty<object>(result, "DefaultChart");
        Assert.Equal("Week", GetProperty<object>(chart, "Range").ToString());
    }

    private static DashboardService CreateService(DateTimeOffset nowUtc, IEnumerable<Booking> bookings)
    {
        return new DashboardService(
            new InMemoryRepository<Booking>(bookings),
            new InMemoryRepository<Tour>([]),
            new FixedTimeProvider(nowUtc));
    }

    private static async Task<object> InvokeStatsAsync(DashboardService service, string rangeName)
    {
        var assembly = typeof(DashboardService).Assembly;
        var rangeType = assembly.GetTypes().FirstOrDefault(type => type.Name == "DashboardRevenueRange");

        Assert.NotNull(rangeType);

        var method = typeof(DashboardService).GetMethod("GetRevenueStatsAsync", [rangeType!]);

        Assert.NotNull(method);

        var rangeValue = Enum.Parse(rangeType!, rangeName);
        var task = method!.Invoke(service, [rangeValue]) as Task;

        Assert.NotNull(task);

        await task!;

        var resultProperty = task!.GetType().GetProperty("Result");
        var result = resultProperty!.GetValue(task);
        Assert.NotNull(result);
        return result!;
    }

    private static async Task<object> InvokeOverviewAsync(DashboardService service, string rangeName)
    {
        var assembly = typeof(DashboardService).Assembly;
        var rangeType = assembly.GetTypes().FirstOrDefault(type => type.Name == "DashboardRevenueRange");

        Assert.NotNull(rangeType);

        var method = typeof(DashboardService).GetMethod("GetRevenueOverviewAsync", [rangeType!]);

        Assert.NotNull(method);

        var rangeValue = Enum.Parse(rangeType!, rangeName);
        var task = method!.Invoke(service, [rangeValue]) as Task;

        Assert.NotNull(task);

        await task!;

        var resultProperty = task!.GetType().GetProperty("Result");
        var result = resultProperty!.GetValue(task);
        Assert.NotNull(result);
        return result!;
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        var value = property!.GetValue(target);
        Assert.NotNull(value);

        if (typeof(T) == typeof(object))
        {
            return (T)value!;
        }

        return Assert.IsType<T>(value);
    }

    private static double? GetNullableDoubleProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (double?)property!.GetValue(target);
    }

    private static List<RevenuePointAssertion> GetPoints(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        var points = Assert.IsAssignableFrom<IEnumerable>(property!.GetValue(target))
            .Cast<object>()
            .Select(point => new RevenuePointAssertion(
                GetProperty<string>(point, "Label"),
                GetProperty<decimal>(point, "Value")))
            .ToList();

        return points;
    }

    private static Booking CreateBooking(
        decimal amount,
        string status,
        string paymentStatus,
        DateTime bookingDateUtc,
        bool isDeleted = false)
    {
        return new Booking
        {
            Id = Guid.NewGuid().ToString("N"),
            BookingCode = $"BK-{Guid.NewGuid():N}"[..10],
            BookingDate = bookingDateUtc,
            CreatedAt = bookingDateUtc,
            UpdatedAt = bookingDateUtc,
            TotalAmount = amount,
            Status = status,
            PaymentStatus = paymentStatus,
            ParticipantsCount = 1,
            IsDeleted = isDeleted,
            TourId = Guid.NewGuid().ToString("N"),
            CustomerId = Guid.NewGuid().ToString("N"),
            TourSnapshot = new TourSnapshot
            {
                Code = "TOUR-01",
                Name = "Ha Giang Loop",
                StartDate = bookingDateUtc.Date,
                Duration = "3 ngay 2 dem"
            },
            ContactInfo = new ContactInfo
            {
                Name = "Nguyen Van A",
                Email = "a@example.com",
                Phone = "0123456789"
            }
        };
    }

    private sealed class InMemoryRepository<T>(IEnumerable<T> items) : IRepository<T> where T : class
    {
        private readonly List<T> _items = items.ToList();

        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(_items);

        public Task<T> GetByIdAsync(string id) => Task.FromResult(_items.First());

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult<IEnumerable<T>>(_items.AsQueryable().Where(predicate).ToList());
        }

        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity) => Task.CompletedTask;

        public Task DeleteAsync(string id) => Task.CompletedTask;

        public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            var query = _items.AsQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }

            var page = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<T>(page, query.Count(), pageIndex, pageSize));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }

    private sealed record RevenuePointAssertion(string Label, decimal Value);
}



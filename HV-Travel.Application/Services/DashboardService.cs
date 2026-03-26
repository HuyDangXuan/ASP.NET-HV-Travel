using System.Globalization;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;

namespace HVTravel.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private static readonly TimeZoneInfo RevenueTimeZone = ResolveRevenueTimeZone();
        private readonly IRepository<Booking> _bookingRepository;
        private readonly TimeProvider _timeProvider;

        public DashboardService(IRepository<Booking> bookingRepository, IRepository<Tour> tourRepository)
            : this(bookingRepository, tourRepository, TimeProvider.System)
        {
        }

        public DashboardService(
            IRepository<Booking> bookingRepository,
            IRepository<Tour> tourRepository,
            TimeProvider timeProvider)
        {
            _bookingRepository = bookingRepository;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<DashboardRevenueOverviewResult> GetRevenueOverviewAsync(DashboardRevenueRange defaultRange)
        {
            var realizedBookings = await GetRealizedRevenueBookingsAsync();
            var currentLocalDate = GetCurrentLocalDate();
            var defaultChart = BuildRevenueStats(realizedBookings, defaultRange, currentLocalDate);
            var monthStats = BuildRevenueStats(realizedBookings, DashboardRevenueRange.Month, currentLocalDate);

            return new DashboardRevenueOverviewResult
            {
                AllTimeRevenue = realizedBookings.Sum(booking => booking.TotalAmount),
                ChartTotalRevenue = defaultChart.TotalRevenue,
                RevenueGrowthPercentage = monthStats.GrowthPercentage,
                DefaultChart = defaultChart
            };
        }

        public async Task<DashboardRevenueStatsResult> GetRevenueStatsAsync(DashboardRevenueRange range)
        {
            var realizedBookings = await GetRealizedRevenueBookingsAsync();
            return BuildRevenueStats(realizedBookings, range, GetCurrentLocalDate());
        }

        public async Task<IEnumerable<Booking>> GetRecentBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return bookings.OrderByDescending(booking => booking.BookingDate).Take(5);
        }

        private async Task<List<RealizedRevenueBooking>> GetRealizedRevenueBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();

            return bookings
                .Where(IsRealizedRevenueBooking)
                .Select(booking => new RealizedRevenueBooking(
                    booking.TotalAmount,
                    ConvertToLocalDateTime(booking.BookingDate)))
                .ToList();
        }

        private DashboardRevenueStatsResult BuildRevenueStats(
            IReadOnlyCollection<RealizedRevenueBooking> bookings,
            DashboardRevenueRange range,
            DateTime currentLocalDate)
        {
            return range switch
            {
                DashboardRevenueRange.Month => BuildMonthStats(bookings, currentLocalDate),
                DashboardRevenueRange.Year => BuildYearStats(bookings, currentLocalDate),
                _ => BuildWeekStats(bookings, currentLocalDate)
            };
        }

        private static DashboardRevenueStatsResult BuildWeekStats(
            IReadOnlyCollection<RealizedRevenueBooking> bookings,
            DateTime currentLocalDate)
        {
            var weekStart = StartOfWeek(currentLocalDate, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(6);
            var previousWeekStart = weekStart.AddDays(-7);
            var previousWeekEnd = weekStart.AddDays(-1);

            var points = Enumerable.Range(0, 7)
                .Select(offset => weekStart.AddDays(offset))
                .Select(day => new DashboardRevenuePoint
                {
                    Label = GetWeekLabel(day.DayOfWeek),
                    Value = bookings
                        .Where(booking => booking.LocalDate.Date == day.Date)
                        .Sum(booking => booking.TotalAmount)
                })
                .ToList();

            var totalRevenue = SumRevenue(bookings, weekStart, weekEnd);
            var previousPeriodRevenue = SumRevenue(bookings, previousWeekStart, previousWeekEnd);

            return new DashboardRevenueStatsResult
            {
                Range = DashboardRevenueRange.Week,
                RangeKey = "week",
                TotalRevenue = totalRevenue,
                PreviousPeriodRevenue = previousPeriodRevenue,
                GrowthPercentage = CalculateGrowth(totalRevenue, previousPeriodRevenue),
                PeriodLabel = $"{weekStart:dd/MM} - {weekEnd:dd/MM}",
                PreviousPeriodLabel = $"{previousWeekStart:dd/MM} - {previousWeekEnd:dd/MM}",
                Points = points
            };
        }

        private static DashboardRevenueStatsResult BuildMonthStats(
            IReadOnlyCollection<RealizedRevenueBooking> bookings,
            DateTime currentLocalDate)
        {
            var monthStart = new DateTime(currentLocalDate.Year, currentLocalDate.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(currentLocalDate.Year, currentLocalDate.Month);
            var monthEnd = monthStart.AddDays(daysInMonth - 1);
            var previousMonthStart = monthStart.AddMonths(-1);
            var previousMonthEnd = monthStart.AddDays(-1);

            var points = Enumerable.Range(0, daysInMonth)
                .Select(offset => monthStart.AddDays(offset))
                .Select(day => new DashboardRevenuePoint
                {
                    Label = day.ToString("dd/MM", CultureInfo.InvariantCulture),
                    Value = bookings
                        .Where(booking => booking.LocalDate.Date == day.Date)
                        .Sum(booking => booking.TotalAmount)
                })
                .ToList();

            var totalRevenue = SumRevenue(bookings, monthStart, monthEnd);
            var previousPeriodRevenue = SumRevenue(bookings, previousMonthStart, previousMonthEnd);

            return new DashboardRevenueStatsResult
            {
                Range = DashboardRevenueRange.Month,
                RangeKey = "month",
                TotalRevenue = totalRevenue,
                PreviousPeriodRevenue = previousPeriodRevenue,
                GrowthPercentage = CalculateGrowth(totalRevenue, previousPeriodRevenue),
                PeriodLabel = $"Tháng {monthStart:MM/yyyy}",
                PreviousPeriodLabel = $"Tháng {previousMonthStart:MM/yyyy}",
                Points = points
            };
        }

        private static DashboardRevenueStatsResult BuildYearStats(
            IReadOnlyCollection<RealizedRevenueBooking> bookings,
            DateTime currentLocalDate)
        {
            var yearStart = new DateTime(currentLocalDate.Year, 1, 1);
            var yearEnd = new DateTime(currentLocalDate.Year, 12, 31);
            var previousYearStart = yearStart.AddYears(-1);
            var previousYearEnd = yearEnd.AddYears(-1);

            var points = Enumerable.Range(1, 12)
                .Select(month => new DashboardRevenuePoint
                {
                    Label = $"Th{month}",
                    Value = bookings
                        .Where(booking => booking.LocalDate.Year == currentLocalDate.Year && booking.LocalDate.Month == month)
                        .Sum(booking => booking.TotalAmount)
                })
                .ToList();

            var totalRevenue = SumRevenue(bookings, yearStart, yearEnd);
            var previousPeriodRevenue = SumRevenue(bookings, previousYearStart, previousYearEnd);

            return new DashboardRevenueStatsResult
            {
                Range = DashboardRevenueRange.Year,
                RangeKey = "year",
                TotalRevenue = totalRevenue,
                PreviousPeriodRevenue = previousPeriodRevenue,
                GrowthPercentage = CalculateGrowth(totalRevenue, previousPeriodRevenue),
                PeriodLabel = $"Năm {yearStart:yyyy}",
                PreviousPeriodLabel = $"Năm {previousYearStart:yyyy}",
                Points = points
            };
        }

        private DateTime GetCurrentLocalDate()
        {
            return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), RevenueTimeZone).DateTime.Date;
        }

        private static decimal SumRevenue(
            IEnumerable<RealizedRevenueBooking> bookings,
            DateTime rangeStart,
            DateTime rangeEnd)
        {
            return bookings
                .Where(booking => booking.LocalDate.Date >= rangeStart.Date && booking.LocalDate.Date <= rangeEnd.Date)
                .Sum(booking => booking.TotalAmount);
        }

        private static bool IsRealizedRevenueBooking(Booking booking)
        {
            if (booking.IsDeleted)
            {
                return false;
            }

            return booking.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)
                || booking.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime ConvertToLocalDateTime(DateTime bookingDate)
        {
            var utcDate = bookingDate.Kind switch
            {
                DateTimeKind.Utc => bookingDate,
                DateTimeKind.Local => bookingDate.ToUniversalTime(),
                _ => DateTime.SpecifyKind(bookingDate, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, RevenueTimeZone);
        }

        private static TimeZoneInfo ResolveRevenueTimeZone()
        {
            var timeZoneIds = new[] { "Asia/Saigon", "Asia/Ho_Chi_Minh", "SE Asia Standard Time" };

            foreach (var id in timeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }

        private static string GetWeekLabel(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                _ => "CN"
            };
        }

        private static double? CalculateGrowth(decimal currentRevenue, decimal previousRevenue)
        {
            if (previousRevenue == 0m)
            {
                return currentRevenue > 0m ? null : 0d;
            }

            var growth = ((currentRevenue - previousRevenue) / previousRevenue) * 100m;
            return Math.Round((double)growth, 1, MidpointRounding.AwayFromZero);
        }

        private sealed record RealizedRevenueBooking(decimal TotalAmount, DateTime LocalDate);
    }
}

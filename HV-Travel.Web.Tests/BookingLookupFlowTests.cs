using System.Reflection;
using HVTravel.Domain.Entities;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace HV_Travel.Web.Tests;

public class BookingLookupFlowTests
{
    [Fact]
    public async Task LookupPost_ReturnsBooking_WhenCodeAndEmailMatch()
    {
        var controller = CreateController(new Booking
        {
            Id = "booking-1",
            BookingCode = "HV20260328001",
            TourId = "tour-1",
            CustomerId = "customer-1",
            Status = "Confirmed",
            PaymentStatus = "Paid",
            TotalAmount = 3500000m,
            ParticipantsCount = 2,
            ContactInfo = new ContactInfo
            {
                Name = "Nguyen Van A",
                Email = "guest@example.com",
                Phone = "0901234567"
            },
            TourSnapshot = new TourSnapshot
            {
                Name = "Ha Noi - Ha Giang",
                Code = "HG-01",
                Duration = "4 ngày 3 đêm",
                StartDate = new DateTime(2026, 4, 2)
            }
        });

        var result = await InvokeLookupAsync(controller, "HV20260328001", "guest@example.com", "");

        var view = Assert.IsType<ViewResult>(result);
        var model = view.Model ?? throw new Xunit.Sdk.XunitException("Expected booking lookup view model.");
        Assert.Equal("HV20260328001", GetStringProperty(model, "BookingCode"));
        Assert.Equal("Confirmed", GetStringProperty(model, "BookingStatus"));
        Assert.Equal("Paid", GetStringProperty(model, "PaymentStatus"));
    }

    [Fact]
    public async Task LookupPost_HidesBooking_WhenContactDoesNotMatch()
    {
        var controller = CreateController(new Booking
        {
            Id = "booking-2",
            BookingCode = "HV20260328002",
            TourId = "tour-2",
            CustomerId = "customer-2",
            Status = "Pending",
            PaymentStatus = "Unpaid",
            TotalAmount = 4200000m,
            ParticipantsCount = 3,
            ContactInfo = new ContactInfo
            {
                Name = "Tran Thi B",
                Email = "tran@example.com",
                Phone = "0912345678"
            },
            TourSnapshot = new TourSnapshot
            {
                Name = "Da Nang Retreat",
                Code = "DN-01",
                Duration = "3 ngày 2 đêm",
                StartDate = new DateTime(2026, 4, 12)
            }
        });

        var result = await InvokeLookupAsync(controller, "HV20260328002", "other@example.com", "");

        var view = Assert.IsType<ViewResult>(result);
        var modelState = view.ViewData.ModelState[string.Empty];
        Assert.NotNull(modelState);
        Assert.Contains(modelState!.Errors, error => error.ErrorMessage.Contains("Không tìm thấy", StringComparison.OrdinalIgnoreCase));
    }

    private static Controller CreateController(params Booking[] bookings)
    {
        var controllerType = GetWebAssembly().GetType("HVTravel.Web.Controllers.BookingLookupController")
            ?? throw new Xunit.Sdk.XunitException("Could not find BookingLookupController in HV-Travel.Web assembly.");

        var constructor = controllerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var bookingRepositoryType = constructor.GetParameters().Single().ParameterType;
        var bookingEntityType = bookingRepositoryType.GetGenericArguments().Single();
        var repositoryType = typeof(InMemoryRepository<>).MakeGenericType(bookingEntityType);
        var repository = Activator.CreateInstance(repositoryType, new object?[] { bookings })!;

        return (Controller)(Activator.CreateInstance(controllerType, repository)
            ?? throw new Xunit.Sdk.XunitException("Could not create BookingLookupController."));
    }

    private static async Task<IActionResult> InvokeLookupAsync(Controller controller, string bookingCode, string email, string phone)
    {
        var action = controller.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method => method.Name == "Lookup" && method.GetParameters().Length == 3)
            ?? throw new Xunit.Sdk.XunitException("Expected BookingLookupController.Lookup action with three parameters.");

        var resultTask = action.Invoke(controller, new object?[] { bookingCode, email, phone }) as Task<IActionResult>;
        if (resultTask == null)
        {
            throw new Xunit.Sdk.XunitException("Booking lookup action did not return Task<IActionResult>.");
        }

        return await resultTask;
    }

    private static Assembly GetWebAssembly()
    {
        return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "HV-Travel.Web")
               ?? Assembly.Load("HV-Travel.Web");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        return instance.GetType().GetProperty(propertyName)?.GetValue(instance)?.ToString() ?? string.Empty;
    }
}

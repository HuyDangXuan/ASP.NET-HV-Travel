using System.Linq.Expressions;
using System.Reflection;
using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace HV_Travel.Web.Tests;

public class HomeContactFlowTests
{
    [Fact]
    public async Task ContactPost_InvalidModel_DoesNotPersistOrSendEmail()
    {
        var context = CreateTestContext();
        var controller = CreateHomeController(context);

        controller.ModelState.AddModelError("FullName", "required");

        var model = CreateContactFormModel(
            fullName: "",
            phoneNumber: "0901234567",
            email: "guest@example.com",
            subject: "Khac",
            message: "Can ho tro");

        var result = await InvokeContactPostAsync(controller, model);

        Assert.IsType<ViewResult>(result);
        Assert.Empty(context.ContactMessages);
        Assert.Equal(0, context.EmailService.SendCount);
    }

    [Fact]
    public async Task ContactPost_ValidModel_PersistsAndSendsEmailToMailTo()
    {
        Environment.SetEnvironmentVariable("MAIL_TO", "ops@example.com");

        var context = CreateTestContext();
        var controller = CreateHomeController(context);
        var model = CreateContactFormModel(
            fullName: "Nguyen Van A",
            phoneNumber: "0901234567",
            email: "guest@example.com",
            subject: "Dat tour",
            message: "Toi can tu van");

        var result = await InvokeContactPostAsync(controller, model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Contact", redirect.ActionName);
        Assert.Single(context.ContactMessages);
        Assert.Equal(1, context.EmailService.SendCount);
        Assert.Equal("ops@example.com", context.EmailService.LastToEmail);

        var savedMessage = context.ContactMessages.Single();
        Assert.Equal("ops@example.com", GetStringProperty(savedMessage, "NotificationEmail"));
        Assert.True(GetBoolProperty(savedMessage, "EmailSent"));
        Assert.False(string.IsNullOrWhiteSpace(GetStringProperty(savedMessage, "Id")));
    }

    [Fact]
    public async Task ContactPost_EmailFailure_PersistsMessageAndReturnsErrorView()
    {
        Environment.SetEnvironmentVariable("MAIL_TO", "ops@example.com");

        var context = CreateTestContext();
        context.EmailService.ShouldThrow = true;

        var controller = CreateHomeController(context);
        var model = CreateContactFormModel(
            fullName: "Tran Thi B",
            phoneNumber: "0912345678",
            email: "tran@example.com",
            subject: "Khieu nai",
            message: "Noi dung lien he");

        var result = await InvokeContactPostAsync(controller, model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.ViewData.ModelState[string.Empty]);
        Assert.Single(context.ContactMessages);

        var savedMessage = context.ContactMessages.Single();
        Assert.False(GetBoolProperty(savedMessage, "EmailSent"));
        Assert.False(string.IsNullOrWhiteSpace(GetStringProperty(savedMessage, "EmailError")));
    }

    private static TestContext CreateTestContext()
    {
        return new TestContext
        {
            Tours = new List<object>(),
            ContactMessages = new List<object>(),
            EmailService = new FakeEmailService()
        };
    }

    private static Controller CreateHomeController(TestContext context)
    {
        var controllerType = FindHomeControllerType();
        var constructor = controllerType.GetConstructors().Single();
        var arguments = constructor.GetParameters()
            .Select(parameter => ResolveConstructorArgument(parameter.ParameterType, context))
            .ToArray();

        var controller = (Controller)Activator.CreateInstance(controllerType, arguments)!;
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
        return controller;
    }

    private static object ResolveConstructorArgument(Type parameterType, TestContext context)
    {
        if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(IRepository<>))
        {
            var entityName = parameterType.GetGenericArguments()[0].Name;
            var items = entityName switch
            {
                "Tour" => context.Tours,
                "ContactMessage" => context.ContactMessages,
                _ => throw new Xunit.Sdk.XunitException($"Unsupported repository dependency: {entityName}")
            };

            var adapterType = typeof(RepositoryAdapter<>).MakeGenericType(parameterType.GetGenericArguments()[0]);
            return Activator.CreateInstance(adapterType, items)!;
        }

        if (parameterType == typeof(IEmailService))
        {
            return context.EmailService;
        }

        if (parameterType == typeof(IConfiguration))
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MAIL_TO"] = "config@example.com"
                })
                .Build();
        }

        throw new Xunit.Sdk.XunitException($"Unsupported constructor dependency: {parameterType.FullName}");
    }

    private static object CreateContactFormModel(string fullName, string phoneNumber, string email, string subject, string message)
    {
        var modelType = FindContactModelType();
        var model = Activator.CreateInstance(modelType)
            ?? throw new Xunit.Sdk.XunitException("Could not instantiate contact form model.");

        modelType.GetProperty("FullName")?.SetValue(model, fullName);
        modelType.GetProperty("PhoneNumber")?.SetValue(model, phoneNumber);
        modelType.GetProperty("Email")?.SetValue(model, email);
        modelType.GetProperty("Subject")?.SetValue(model, subject);
        modelType.GetProperty("Message")?.SetValue(model, message);
        return model;
    }

    private static async Task<IActionResult> InvokeContactPostAsync(Controller controller, object model)
    {
        var action = controller.GetType().GetMethods()
            .SingleOrDefault(method => method.Name == "Contact" && method.GetParameters().Length == 1)
            ?? throw new Xunit.Sdk.XunitException("Expected HomeController.Contact POST action with one parameter.");

        var resultTask = action.Invoke(controller, new[] { model }) as Task<IActionResult>;
        if (resultTask == null)
        {
            throw new Xunit.Sdk.XunitException("HomeController.Contact POST action did not return Task<IActionResult>.");
        }

        return await resultTask;
    }

    private static Type FindHomeControllerType()
    {
        var type = GetWebAssembly().GetType("HVTravel.Web.Controllers.HomeController");
        return type ?? throw new Xunit.Sdk.XunitException("Could not find HomeController in HV-Travel.Web assembly.");
    }

    private static Type FindContactModelType()
    {
        foreach (var candidate in new[] { "HVTravel.Web.Models.ContactViewModel", "HVTravel.Web.Models.ContactFormViewModel" })
        {
            var type = GetWebAssembly().GetType(candidate);
            if (type != null)
            {
                return type;
            }
        }

        throw new Xunit.Sdk.XunitException("Could not find contact form view model type in HV-Travel.Web assembly.");
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

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        return (bool)(instance.GetType().GetProperty(propertyName)?.GetValue(instance)
            ?? throw new Xunit.Sdk.XunitException($"Missing bool property {propertyName}."));
    }

    private sealed class TestContext
    {
        public required List<object> Tours { get; init; }
        public required List<object> ContactMessages { get; init; }
        public required FakeEmailService EmailService { get; init; }
    }

    private sealed class RepositoryAdapter<T> : IRepository<T> where T : class
    {
        private readonly List<object> _items;

        public RepositoryAdapter(List<object> items)
        {
            _items = items;
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<T>>(_items.Cast<T>().ToList());
        }

        public Task<T> GetByIdAsync(string id)
        {
            var item = _items.Cast<T>().FirstOrDefault(entity => string.Equals(GetId(entity), id, StringComparison.Ordinal));
            return Task.FromResult(item)!;
        }

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<T>>(_items.Cast<T>().Where(compiled).ToList());
        }

        public Task AddAsync(T entity)
        {
            EnsureId(entity);
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            var query = _items.Cast<T>().AsEnumerable();
            if (filter != null)
            {
                query = query.Where(filter.Compile());
            }

            var pageItems = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<T>(pageItems, query.Count(), pageIndex, pageSize));
        }

        private static string? GetId(object entity)
        {
            return entity.GetType().GetProperty("Id")?.GetValue(entity) as string;
        }

        private static void EnsureId(object entity)
        {
            var property = entity.GetType().GetProperty("Id");
            if (property == null || property.PropertyType != typeof(string))
            {
                return;
            }

            var currentValue = property.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                return;
            }

            property.SetValue(entity, Guid.NewGuid().ToString("N"));
        }
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool ShouldThrow { get; set; }
        public int SendCount { get; private set; }
        public string LastToEmail { get; private set; } = string.Empty;

        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            SendCount++;
            LastToEmail = toEmail;

            if (ShouldThrow)
            {
                throw new InvalidOperationException("SMTP unavailable");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}

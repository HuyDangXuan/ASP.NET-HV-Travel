using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HV_Travel.Web.Tests;

public class CloudinaryAssetBrowserBackendTests
{
    [Fact]
    public async Task CloudinaryAssetBrowserService_UsesSearchApiAndMapsAssets()
    {
        var serviceType = GetRequiredType("HVTravel.Infrastructure.Services.CloudinaryAssetBrowserService, HV-Travel.Infrastructure");
        var requestType = GetRequiredType("HVTravel.Application.Models.CloudinaryAssetBrowseRequest, HV-Travel.Application");

        var handler = new RecordingHandler(_ =>
        {
            var responseJson = """
                {
                  "resources": [
                    {
                      "public_id": "HV-Travel ASP.NET/ha-long",
                      "secure_url": "https://res.cloudinary.com/demo/image/upload/v1/HV-Travel ASP.NET/ha-long.jpg",
                      "format": "jpg",
                      "bytes": 153600,
                      "width": 1200,
                      "height": 800,
                      "created_at": "2026-03-27T10:00:00Z",
                      "asset_folder": "HV-Travel ASP.NET"
                    }
                  ],
                  "next_cursor": "cursor-2"
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "demo",
                ["Cloudinary:ApiKey"] = "public-key",
                ["Cloudinary:ApiSecret"] = "secret-key",
                ["Cloudinary:UploadPreset"] = "preset"
            })
            .Build();

        var service = Activator.CreateInstance(serviceType, httpClient, configuration);
        Assert.NotNull(service);

        var request = Activator.CreateInstance(requestType);
        Assert.NotNull(request);
        requestType.GetProperty("Folder")!.SetValue(request, "HV-Travel ASP.NET");
        requestType.GetProperty("Search")!.SetValue(request, "ha long");
        requestType.GetProperty("Cursor")!.SetValue(request, "cursor-1");
        requestType.GetProperty("MaxResults")!.SetValue(request, 25);

        var method = serviceType.GetMethod("GetAssetsAsync");
        Assert.NotNull(method);

        var task = method!.Invoke(service, [request!, CancellationToken.None]) as Task;
        Assert.NotNull(task);
        await task!;

        var result = task!.GetType().GetProperty("Result")!.GetValue(task);
        Assert.NotNull(result);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.cloudinary.com/v1_1/demo/resources/search", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Basic", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes("public-key:secret-key")),
            handler.LastRequest.Headers.Authorization?.Parameter);

        var requestBody = handler.LastRequestBody;
        Assert.Contains("\"expression\":\"", requestBody);
        Assert.Contains("resource_type:image", requestBody);
        Assert.Contains("asset_folder=", requestBody);
        Assert.Contains("HV-Travel ASP.NET", requestBody);
        Assert.Contains("public_id:*ha long*", requestBody);
        Assert.Contains("filename:*ha long*", requestBody);
        Assert.Contains("\"next_cursor\":\"cursor-1\"", requestBody);
        Assert.Contains("\"max_results\":25", requestBody);

        var nextCursor = result.GetType().GetProperty("NextCursor")!.GetValue(result) as string;
        Assert.Equal("cursor-2", nextCursor);

        var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.GetType().GetProperty("Items")!.GetValue(result))
            .Cast<object>()
            .ToList();

        Assert.Single(items);
        var item = items[0];
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/v1/HV-Travel ASP.NET/ha-long.jpg", item.GetType().GetProperty("SecureUrl")!.GetValue(item));
        Assert.Equal("HV-Travel ASP.NET/ha-long", item.GetType().GetProperty("PublicId")!.GetValue(item));
        Assert.Equal("150 KB", item.GetType().GetProperty("SizeLabel")!.GetValue(item));
    }

    [Fact]
    public async Task CloudinaryAssetBrowserService_FallsBackToFolderExpression_WhenAssetFolderIsUnsupported()
    {
        var serviceType = GetRequiredType("HVTravel.Infrastructure.Services.CloudinaryAssetBrowserService, HV-Travel.Infrastructure");
        var requestType = GetRequiredType("HVTravel.Application.Models.CloudinaryAssetBrowseRequest, HV-Travel.Application");

        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""
                    {
                      "error": {
                        "message": "Unknown field asset_folder"
                      }
                    }
                    """, Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "resources": [
                        {
                          "public_id": "HV-Travel ASP.NET/ha-noi",
                          "secure_url": "https://res.cloudinary.com/demo/image/upload/v1/HV-Travel ASP.NET/ha-noi.jpg",
                          "format": "jpg",
                          "bytes": 204800,
                          "width": 1400,
                          "height": 900,
                          "created_at": "2026-03-27T11:00:00Z",
                          "folder": "HV-Travel ASP.NET"
                        }
                      ]
                    }
                    """, Encoding.UTF8, "application/json")
            }
        });

        var handler = new RecordingHandler(_ => responses.Dequeue());
        var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "demo",
                ["Cloudinary:ApiKey"] = "public-key",
                ["Cloudinary:ApiSecret"] = "secret-key"
            })
            .Build();

        var service = Activator.CreateInstance(serviceType, httpClient, configuration);
        Assert.NotNull(service);

        var request = Activator.CreateInstance(requestType);
        Assert.NotNull(request);
        requestType.GetProperty("Folder")!.SetValue(request, "HV-Travel ASP.NET");

        var method = serviceType.GetMethod("GetAssetsAsync");
        Assert.NotNull(method);

        var task = method!.Invoke(service, [request!, CancellationToken.None]) as Task;
        Assert.NotNull(task);
        await task!;

        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Contains("asset_folder=", handler.RequestBodies[0]);
        Assert.Contains("\"expression\":\"resource_type:image AND folder=", handler.RequestBodies[1]);
        Assert.DoesNotContain("asset_folder=", handler.RequestBodies[1]);

        var result = task!.GetType().GetProperty("Result")!.GetValue(task);
        Assert.NotNull(result);

        var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.GetType().GetProperty("Items")!.GetValue(result))
            .Cast<object>()
            .ToList();

        Assert.Single(items);
        Assert.Equal("HV-Travel ASP.NET", items[0].GetType().GetProperty("Folder")!.GetValue(items[0]));
    }

    [Fact]
    public void CloudinaryController_DefinesAdminProtectedAssetsEndpoint()
    {
        var controllerType = GetRequiredType("HVTravel.Web.Areas.Admin.Controllers.CloudinaryController, HV-Travel.Web");
        var classAuthorize = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(classAuthorize);
        Assert.Contains("AdminScheme", classAuthorize!.AuthenticationSchemes);

        var assetsMethod = controllerType.GetMethod("Assets");
        Assert.NotNull(assetsMethod);

        var httpGet = assetsMethod!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .OfType<HttpGetAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpGet);
        Assert.Equal("Assets", httpGet!.Template);
    }

    private static Type GetRequiredType(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            RequestBodies.Add(LastRequestBody);
            return responder(request);
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        public List<string> RequestBodies { get; } = [];
    }
}



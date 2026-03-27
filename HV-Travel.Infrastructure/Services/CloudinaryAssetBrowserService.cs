using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using Microsoft.Extensions.Configuration;

namespace HVTravel.Infrastructure.Services;

public class CloudinaryAssetBrowserService : ICloudinaryAssetBrowserService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CloudinaryAssetBrowserService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<CloudinaryAssetBrowseResult> GetAssetsAsync(CloudinaryAssetBrowseRequest request, CancellationToken cancellationToken = default)
    {
        var cloudName = GetRequiredConfig("Cloudinary:CloudName");
        var apiKey = GetRequiredConfig("Cloudinary:ApiKey");
        var apiSecret = GetRequiredConfig("Cloudinary:ApiSecret");
        var safeRequest = request ?? new CloudinaryAssetBrowseRequest();
        var safeMaxResults = Math.Clamp(safeRequest.MaxResults <= 0 ? 24 : safeRequest.MaxResults, 1, 50);

        var responsePayload = await ExecuteSearchAsync(cloudName, apiKey, apiSecret, safeRequest, safeMaxResults, useAssetFolder: true, cancellationToken);
        if (responsePayload.StatusCode == HttpStatusCode.BadRequest && IsUnsupportedAssetFolderError(responsePayload.Content))
        {
            responsePayload = await ExecuteSearchAsync(cloudName, apiKey, apiSecret, safeRequest, safeMaxResults, useAssetFolder: false, cancellationToken);
        }

        if (!responsePayload.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetCloudinaryError(responsePayload.Content));
        }

        var searchResponse = JsonSerializer.Deserialize<CloudinarySearchResponse>(responsePayload.Content, JsonOptions) ?? new CloudinarySearchResponse();
        var items = (searchResponse.Resources ?? [])
            .Select(MapItem)
            .Where(item => item is not null)
            .Cast<CloudinaryAssetBrowseItem>()
            .ToList();

        return new CloudinaryAssetBrowseResult
        {
            Folder = safeRequest.Folder?.Trim() ?? string.Empty,
            NextCursor = searchResponse.NextCursor ?? string.Empty,
            Items = items
        };
    }

    private async Task<CloudinarySearchPayload> ExecuteSearchAsync(
        string cloudName,
        string apiKey,
        string apiSecret,
        CloudinaryAssetBrowseRequest request,
        int maxResults,
        bool useAssetFolder,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["expression"] = BuildExpression(request, useAssetFolder),
            ["max_results"] = maxResults,
            ["sort_by"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["created_at"] = "desc"
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            payload["next_cursor"] = request.Cursor.Trim();
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/resources/search");
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}")));
        message.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new CloudinarySearchPayload
        {
            StatusCode = response.StatusCode,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            Content = content
        };
    }

    private string GetRequiredConfig(string key)
    {
        var value = _configuration[key] ?? Environment.GetEnvironmentVariable(key.Replace(':', '_').Replace("__", "_"));
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Thiếu cấu hình {key}.");
    }

    private static string BuildExpression(CloudinaryAssetBrowseRequest request, bool useAssetFolder)
    {
        var expressions = new List<string> { "resource_type:image" };

        if (!string.IsNullOrWhiteSpace(request.Folder))
        {
            var fieldName = useAssetFolder ? "asset_folder" : "folder";
            expressions.Add($"{fieldName}=\"{EscapeSearchValue(request.Folder.Trim())}\"");
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = EscapeSearchValue(request.Search.Trim());
            expressions.Add($"(public_id:*{search}* OR filename:*{search}*)");
        }

        return string.Join(" AND ", expressions);
    }

    private static bool IsUnsupportedAssetFolderError(string content)
    {
        var message = GetCloudinaryError(content);
        return message.Contains("asset_folder", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("unknown", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unsupported", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    private static string EscapeSearchValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static CloudinaryAssetBrowseItem? MapItem(CloudinaryResource resource)
    {
        var secureUrl = resource.SecureUrl ?? resource.Url;
        if (string.IsNullOrWhiteSpace(secureUrl))
        {
            return null;
        }

        return new CloudinaryAssetBrowseItem
        {
            SecureUrl = secureUrl,
            ThumbnailUrl = secureUrl,
            PublicId = resource.PublicId ?? string.Empty,
            Format = resource.Format ?? string.Empty,
            SizeLabel = FormatBytes(resource.Bytes),
            Width = resource.Width,
            Height = resource.Height,
            CreatedAt = resource.CreatedAt ?? string.Empty,
            Folder = resource.AssetFolder ?? resource.Folder ?? string.Empty
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        var units = new[] { "B", "KB", "MB", "GB" };
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 || Math.Abs(size - Math.Round(size)) < 0.05 ? "0" : "0.0";
        return $"{size.ToString(format)} {units[unitIndex]}";
    }

    private static string GetCloudinaryError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Cloudinary không trả về nội dung lỗi.";
        }

        try
        {
            var error = JsonSerializer.Deserialize<CloudinaryErrorResponse>(content, JsonOptions);
            return error?.Error?.Message ?? "Cloudinary trả về lỗi không xác định.";
        }
        catch (JsonException)
        {
            return "Cloudinary trả về lỗi không xác định.";
        }
    }

    private sealed class CloudinarySearchPayload
    {
        public HttpStatusCode StatusCode { get; set; }

        public bool IsSuccessStatusCode { get; set; }

        public string Content { get; set; } = string.Empty;
    }

    private sealed class CloudinarySearchResponse
    {
        [JsonPropertyName("resources")]
        public List<CloudinaryResource>? Resources { get; set; }

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }
    }

    private sealed class CloudinaryResource
    {
        [JsonPropertyName("public_id")]
        public string? PublicId { get; set; }

        [JsonPropertyName("secure_url")]
        public string? SecureUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("bytes")]
        public long Bytes { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("asset_folder")]
        public string? AssetFolder { get; set; }

        [JsonPropertyName("folder")]
        public string? Folder { get; set; }
    }

    private sealed class CloudinaryErrorResponse
    {
        [JsonPropertyName("error")]
        public CloudinaryErrorDetail? Error { get; set; }
    }

    private sealed class CloudinaryErrorDetail
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}

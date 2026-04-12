using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace HVTravel.Web.Services;

public sealed class GroqChatClient : IGroqChatClient
{
    private const string DefaultBaseUrl = "https://api.groq.com/openai/v1";
    private const string DefaultModel = "llama-3.3-70b-versatile";
    private const double DefaultTemperature = 0.2d;
    private const int DefaultMaxTokens = 600;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqChatClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> CompleteChatAsync(IReadOnlyList<GroqChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            throw new ArgumentException("At least one message is required.", nameof(messages));
        }

        var apiKey = ReadSetting("ApiKey", "GROQ_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured.");
        }

        var baseUrl = ReadSetting("BaseUrl") ?? DefaultBaseUrl;
        var model = ReadSetting("Model", "GROQ_MODEL") ?? DefaultModel;
        var temperature = ReadDoubleSetting("Temperature", DefaultTemperature);
        var maxTokens = ReadIntSetting("MaxTokens", DefaultMaxTokens);

        var payload = new
        {
            model,
            messages = messages.Select(message => new
            {
                role = message.Role,
                content = message.Content
            }),
            temperature,
            max_completion_tokens = maxTokens
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Groq request failed with status {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Groq response did not include any choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.GetProperty("content").GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Groq response content was empty.");
        }

        return content;
    }

    private string? ReadSetting(string key, string? legacyKey = null)
    {
        var configuredValue = _configuration[$"Groq:{key}"];
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue;
        }

        if (string.IsNullOrWhiteSpace(legacyKey))
        {
            return null;
        }

        var legacyValue = _configuration[legacyKey];
        return string.IsNullOrWhiteSpace(legacyValue) ? null : legacyValue;
    }

    private double ReadDoubleSetting(string key, double fallback)
    {
        var rawValue = ReadSetting(key);
        return double.TryParse(rawValue, out var value) ? value : fallback;
    }

    private int ReadIntSetting(string key, int fallback)
    {
        var rawValue = ReadSetting(key);
        return int.TryParse(rawValue, out var value) ? value : fallback;
    }
}

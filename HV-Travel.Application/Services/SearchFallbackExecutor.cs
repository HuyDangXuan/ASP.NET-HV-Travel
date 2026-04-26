using Microsoft.Extensions.Logging;

namespace HVTravel.Application.Services;

internal static class SearchFallbackExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<bool>> isAvailableAsync,
        Func<CancellationToken, Task<T>> preferredAsync,
        Func<Task<T>> fallbackAsync,
        ILogger logger,
        string scope,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await isAvailableAsync(cancellationToken))
            {
                return await fallbackAsync();
            }

            return await preferredAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{SearchScope} Meilisearch path failed. Falling back to repository path.", scope);
            return await fallbackAsync();
        }
    }
}

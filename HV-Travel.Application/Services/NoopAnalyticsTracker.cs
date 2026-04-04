using HVTravel.Application.Interfaces;

namespace HVTravel.Application.Services;

public class NoopAnalyticsTracker : IAnalyticsTracker
{
    public Task TrackAsync(string eventName, IReadOnlyDictionary<string, string?> properties)
    {
        return Task.CompletedTask;
    }
}

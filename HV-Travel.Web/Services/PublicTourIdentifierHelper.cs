using HVTravel.Domain.Entities;
using MongoDB.Bson;

namespace HVTravel.Web.Services;

public static class PublicTourIdentifierHelper
{
    public static string GetDetailIdentifier(Tour? tour)
    {
        return string.IsNullOrWhiteSpace(tour?.Slug) ? tour?.Id ?? string.Empty : tour.Slug;
    }

    public static bool IsObjectId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && ObjectId.TryParse(value, out _);
    }
}

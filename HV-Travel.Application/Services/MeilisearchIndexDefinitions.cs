using HVTravel.Application.Models;

namespace HVTravel.Application.Services;

public static class MeilisearchIndexDefinitions
{
    public static MeilisearchIndexDefinition Tours(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.ResolveToursIndexName(),
            SearchableAttributes =
            [
                "id",
                "slug",
                "name",
                "destination",
                "region",
                "country",
                "shortDescriptionText",
                "descriptionText",
                "highlightsText",
                "routingSummary"
            ],
            FilterableAttributes =
            [
                "status",
                "region",
                "destination",
                "startingAdultPrice",
                "durationDays",
                "departureMonths",
                "maxRemainingCapacity",
                "confirmationTypes",
                "isFreeCancellation",
                "hasPromotion",
                "effectiveDiscount",
                "isDomestic",
                "isInternational",
                "isPremium",
                "isBudget",
                "isDeal",
                "cancellationType"
            ],
            SortableAttributes =
            [
                "name",
                "status",
                "startingAdultPrice",
                "rating",
                "createdAt",
                "nextDepartureDate",
                "effectiveDiscount"
            ]
        };
    }

    public static MeilisearchIndexDefinition Bookings(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.Bookings,
            SearchableAttributes =
            [
                "id",
                "bookingCode",
                "contactName",
                "contactEmail",
                "contactPhone",
                "contactPhoneNormalized",
                "tourName"
            ],
            FilterableAttributes =
            [
                "bookingCode",
                "contactEmail",
                "contactPhoneNormalized",
                "bookingStatus",
                "paymentStatus",
                "createdAt",
                "departureDate",
                "totalAmount",
                "isDeleted",
                "publicLookupEnabled"
            ],
            SortableAttributes =
            [
                "createdAt",
                "departureDate",
                "totalAmount",
                "bookingStatus",
                "paymentStatus",
                "bookingCode"
            ]
        };
    }

    public static MeilisearchIndexDefinition Users(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.Users,
            SearchableAttributes =
            [
                "id",
                "fullName",
                "email",
                "role"
            ],
            FilterableAttributes =
            [
                "status",
                "role"
            ],
            SortableAttributes =
            [
                "fullName",
                "email",
                "role",
                "status",
                "lastLogin",
                "createdAt"
            ]
        };
    }

    public static MeilisearchIndexDefinition Reviews(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.Reviews,
            SearchableAttributes =
            [
                "id",
                "bookingId",
                "tourName",
                "customerEmail",
                "displayName",
                "comment",
                "moderatorName"
            ],
            FilterableAttributes =
            [
                "moderationStatus",
                "moderationStatusRank",
                "isVerifiedBooking",
                "createdAt",
                "rating"
            ],
            SortableAttributes =
            [
                "createdAt",
                "rating",
                "moderationStatusRank"
            ]
        };
    }

    public static MeilisearchIndexDefinition ServiceLeads(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.ServiceLeads,
            SearchableAttributes =
            [
                "id",
                "fullName",
                "email",
                "phoneNumber",
                "destination",
                "serviceType"
            ],
            FilterableAttributes =
            [
                "status",
                "serviceType",
                "createdAt"
            ],
            SortableAttributes =
            [
                "createdAt",
                "fullName",
                "status"
            ]
        };
    }

    public static MeilisearchIndexDefinition Customers(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.Customers,
            SearchableAttributes =
            [
                "id",
                "customerCode",
                "fullName",
                "email",
                "phoneNumber",
                "segment"
            ],
            FilterableAttributes =
            [
                "segment",
                "status",
                "totalOrders",
                "totalSpending",
                "lastBookingDate",
                "createdAt"
            ],
            SortableAttributes =
            [
                "fullName",
                "totalOrders",
                "totalSpending",
                "lastBookingDate",
                "createdAt"
            ]
        };
    }

    public static MeilisearchIndexDefinition Payments(MeilisearchOptions options)
    {
        return new MeilisearchIndexDefinition
        {
            Name = options.Indexes.Payments,
            SearchableAttributes =
            [
                "id",
                "bookingCode",
                "customerId",
                "customerName"
            ],
            FilterableAttributes =
            [
                "paymentStatus",
                "bookingStatus",
                "amount",
                "createdAt",
                "paidAt"
            ],
            SortableAttributes =
            [
                "id",
                "bookingCode",
                "paymentStatus",
                "bookingStatus",
                "amount",
                "createdAt",
                "paidAt"
            ]
        };
    }
}

from __future__ import annotations

from collections import OrderedDict

from mockdata_generator.catalog import collection_alias_map, get_collection_specs


PROFILE_DEFAULTS = {
    "small": OrderedDict(
        {
            "Users": 4,
            "Tours": 10,
            "Customers": 18,
            "Promotions": 12,
            "TravelArticles": 6,
            "siteSettings": 1,
            "contentSections": 12,
            "Bookings": 36,
            "Payments": 24,
            "Reviews": 18,
            "Notifications": 45,
            "AncillaryLeads": 8,
            "SavedTravellerProfiles": 16,
            "LoyaltyLedgerEntrys": 20,
            "VoucherWalletItems": 12,
            "chatConversations": 8,
            "chatMessages": 40,
            "ContactMessages": 8,
        }
    ),
    "medium": OrderedDict(
        {
            "Users": 12,
            "Tours": 24,
            "Customers": 60,
            "Promotions": 24,
            "TravelArticles": 12,
            "siteSettings": 1,
            "contentSections": 24,
            "Bookings": 180,
            "Payments": 130,
            "Reviews": 70,
            "Notifications": 220,
            "AncillaryLeads": 35,
            "SavedTravellerProfiles": 90,
            "LoyaltyLedgerEntrys": 120,
            "VoucherWalletItems": 70,
            "chatConversations": 40,
            "chatMessages": 260,
            "ContactMessages": 30,
        }
    ),
    "large": OrderedDict(
        {
            "Users": 30,
            "Tours": 60,
            "Customers": 200,
            "Promotions": 60,
            "TravelArticles": 24,
            "siteSettings": 1,
            "contentSections": 40,
            "Bookings": 900,
            "Payments": 650,
            "Reviews": 320,
            "Notifications": 1100,
            "AncillaryLeads": 120,
            "SavedTravellerProfiles": 300,
            "LoyaltyLedgerEntrys": 700,
            "VoucherWalletItems": 260,
            "chatConversations": 180,
            "chatMessages": 2200,
            "ContactMessages": 120,
        }
    ),
}


def resolve_profile_counts(profile: str, overrides: dict[str, int] | None = None) -> OrderedDict[str, int]:
    if profile not in PROFILE_DEFAULTS:
        raise ValueError(f"Unknown profile '{profile}'.")

    counts = OrderedDict(PROFILE_DEFAULTS[profile])
    aliases = collection_alias_map()

    for key, value in (overrides or {}).items():
        normalized = aliases.get(key)
        if normalized is None:
            raise ValueError(f"Unknown collection override '{key}'.")
        if value < 0:
            raise ValueError(f"Count for '{normalized}' cannot be negative.")
        counts[normalized] = value

    ordered_counts = OrderedDict()
    for spec in get_collection_specs():
        ordered_counts[spec.collection_name] = counts[spec.collection_name]
    return ordered_counts

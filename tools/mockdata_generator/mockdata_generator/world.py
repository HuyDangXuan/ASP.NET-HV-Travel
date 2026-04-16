from __future__ import annotations

from collections import OrderedDict
from dataclasses import dataclass, field

from mockdata_generator.builders import (
    build_ancillary_leads,
    build_bookings,
    build_chat_conversations,
    build_chat_messages,
    build_contact_messages,
    build_content_sections,
    build_customers,
    build_loyalty_ledger_entries,
    build_notifications,
    build_payments,
    build_promotions,
    build_reviews,
    build_saved_traveller_profiles,
    build_site_settings,
    build_tours,
    build_travel_articles,
    build_users,
    build_voucher_wallet_items,
)
from mockdata_generator.catalog import CollectionSpec, get_collection_specs, resolve_selected_collections
from mockdata_generator.profiles import resolve_profile_counts
from mockdata_generator.randoms import SeededFactory


@dataclass
class World:
    collections: OrderedDict[str, list[dict]]
    manifest: dict


@dataclass
class GenerationContext:
    factory: SeededFactory
    counts: OrderedDict[str, int]
    specs: tuple[CollectionSpec, ...]
    collections: OrderedDict[str, list[dict]] = field(default_factory=OrderedDict)
    state: dict = field(default_factory=dict)


def generate_world(
    profile: str,
    seed: int,
    count_overrides: dict[str, int] | None = None,
    selected_collections: list[str] | None = None,
) -> World:
    specs = get_collection_specs()
    counts = resolve_profile_counts(profile, count_overrides or {})
    enabled_collections = resolve_selected_collections(selected_collections)
    ctx = GenerationContext(
        factory=SeededFactory(seed),
        counts=counts,
        specs=specs,
        collections=OrderedDict((spec.collection_name, []) for spec in specs),
    )

    _build_if_enabled(ctx, enabled_collections, "Users", build_users)
    _build_if_enabled(ctx, enabled_collections, "Tours", build_tours)
    _build_if_enabled(ctx, enabled_collections, "Customers", build_customers)
    _build_if_enabled(ctx, enabled_collections, "Promotions", build_promotions)
    _build_if_enabled(ctx, enabled_collections, "TravelArticles", build_travel_articles)
    _build_if_enabled(ctx, enabled_collections, "siteSettings", build_site_settings)
    _build_if_enabled(ctx, enabled_collections, "contentSections", build_content_sections)
    _build_if_enabled(ctx, enabled_collections, "Bookings", build_bookings)
    _build_if_enabled(ctx, enabled_collections, "Payments", build_payments)
    _build_if_enabled(ctx, enabled_collections, "Reviews", build_reviews)
    _build_if_enabled(ctx, enabled_collections, "Notifications", build_notifications)
    _build_if_enabled(ctx, enabled_collections, "AncillaryLeads", build_ancillary_leads)
    _build_if_enabled(ctx, enabled_collections, "SavedTravellerProfiles", build_saved_traveller_profiles)
    _build_if_enabled(ctx, enabled_collections, "LoyaltyLedgerEntrys", build_loyalty_ledger_entries)
    _build_if_enabled(ctx, enabled_collections, "VoucherWalletItems", build_voucher_wallet_items)
    _build_if_enabled(ctx, enabled_collections, "chatConversations", build_chat_conversations)
    _build_if_enabled(ctx, enabled_collections, "chatMessages", build_chat_messages)
    _build_if_enabled(ctx, enabled_collections, "ContactMessages", build_contact_messages)

    manifest = _build_manifest(profile, seed, specs, ctx.collections)
    return World(collections=ctx.collections, manifest=manifest)


def _build_if_enabled(ctx: GenerationContext, enabled_collections: set[str], collection_name: str, builder) -> None:
    if collection_name in enabled_collections:
        builder(ctx)


def _build_manifest(profile: str, seed: int, specs: tuple[CollectionSpec, ...], collections: OrderedDict[str, list[dict]]) -> dict:
    counts = OrderedDict((name, len(items)) for name, items in collections.items())
    customers = {item["_id"] for item in collections["Customers"]}
    tours = {item["_id"] for item in collections["Tours"]}
    bookings = {item["_id"] for item in collections["Bookings"]}
    promotions = {item["_id"] for item in collections["Promotions"]}
    conversations = {item["_id"] for item in collections["chatConversations"]}

    return {
        "generatorVersion": "0.1.0",
        "profile": profile,
        "seed": seed,
        "counts": counts,
        "totalRecords": sum(counts.values()),
        "collectionMapping": OrderedDict((spec.type_name, spec.collection_name) for spec in specs),
        "relationshipIntegrity": {
            "bookingsLinkedToCustomers": sum(1 for item in collections["Bookings"] if item["customerId"] in customers),
            "bookingsLinkedToTours": sum(1 for item in collections["Bookings"] if item["tourId"] in tours),
            "paymentsLinkedToBookings": sum(1 for item in collections["Payments"] if item["bookingId"] in bookings),
            "reviewsLinkedToBookings": sum(1 for item in collections["Reviews"] if item["bookingId"] in bookings),
            "walletItemsLinkedToPromotions": sum(
                1 for item in collections["VoucherWalletItems"] if item["promotionId"] in promotions
            ),
            "messagesLinkedToConversations": sum(
                1 for item in collections["chatMessages"] if item["conversationId"] in conversations
            ),
        },
        "files": [],
    }

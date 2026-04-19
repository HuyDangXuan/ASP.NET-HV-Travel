from __future__ import annotations

from dataclasses import dataclass
from functools import lru_cache


@dataclass(frozen=True)
class CollectionSpec:
    type_name: str
    collection_name: str
    dependencies: tuple[str, ...]
    builder_key: str
    output_filename: str


_COLLECTION_NAME_OVERRIDES = {
    "User": "Users",
    "Tour": "Tours",
    "Booking": "Bookings",
    "Customer": "Customers",
    "Payment": "Payments",
    "Promotion": "Promotions",
    "Review": "Reviews",
    "Notification": "Notifications",
    "SiteSettings": "siteSettings",
    "ContentSection": "contentSections",
    "ChatConversation": "chatConversations",
    "ChatMessage": "chatMessages",
    "ContactMessage": "ContactMessages",
}

_GENERATION_DEFINITION = (
    ("User", (), "users"),
    ("Tour", (), "tours"),
    ("Customer", (), "customers"),
    ("Promotion", (), "promotions"),
    ("TravelArticle", (), "travel_articles"),
    ("SiteSettings", (), "site_settings"),
    ("ContentSection", (), "content_sections"),
    ("Booking", ("Customers", "Tours"), "bookings"),
    ("Payment", ("Bookings",), "payments"),
    ("Review", ("Bookings", "Customers", "Tours"), "reviews"),
    ("Notification", ("Users", "Customers", "Bookings"), "notifications"),
    ("AncillaryLead", ("Customers",), "ancillary_leads"),
    ("SavedTravellerProfile", ("Customers", "Bookings"), "saved_traveller_profiles"),
    ("LoyaltyLedgerEntry", ("Customers", "Bookings"), "loyalty_ledger_entries"),
    ("VoucherWalletItem", ("Customers", "Promotions"), "voucher_wallet_items"),
    ("ChatConversation", ("Users", "Customers"), "chat_conversations"),
    ("ChatMessage", ("chatConversations", "Users", "Customers"), "chat_messages"),
    ("ContactMessage", (), "contact_messages"),
)


def collection_name_for_type(type_name: str) -> str:
    return _COLLECTION_NAME_OVERRIDES.get(type_name, f"{type_name}s")


@lru_cache(maxsize=1)
def get_collection_specs() -> tuple[CollectionSpec, ...]:
    return tuple(
        CollectionSpec(
            type_name=type_name,
            collection_name=collection_name_for_type(type_name),
            dependencies=tuple(dependencies),
            builder_key=builder_key,
            output_filename=f"{collection_name_for_type(type_name)}.json",
        )
        for type_name, dependencies, builder_key in _GENERATION_DEFINITION
    )


def collection_alias_map() -> dict[str, str]:
    aliases: dict[str, str] = {}
    for spec in get_collection_specs():
        aliases[spec.collection_name] = spec.collection_name
        aliases[spec.type_name] = spec.collection_name
    return aliases


def collection_spec_map() -> dict[str, CollectionSpec]:
    return {spec.collection_name: spec for spec in get_collection_specs()}


def resolve_selected_collections(selected_collections: list[str] | None) -> set[str]:
    if not selected_collections:
        return {spec.collection_name for spec in get_collection_specs()}

    aliases = collection_alias_map()
    specs = collection_spec_map()
    resolved: set[str] = set()
    pending = [aliases[name] for name in selected_collections if name in aliases]
    if len(pending) != len(selected_collections):
        unknown = [name for name in selected_collections if name not in aliases]
        raise ValueError(f"Unknown selected collections: {', '.join(unknown)}")

    while pending:
        collection_name = pending.pop()
        if collection_name in resolved:
            continue
        resolved.add(collection_name)
        pending.extend(specs[collection_name].dependencies)

    return resolved

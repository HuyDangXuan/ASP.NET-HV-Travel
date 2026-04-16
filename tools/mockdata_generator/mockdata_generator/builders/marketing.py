from __future__ import annotations


_PROMOTION_CATALOG = (
    {
        "code": "WELCOME",
        "title": "Welcome savings for first booking",
        "campaignType": "Voucher",
        "badgeText": "New guest",
        "campaignScope": "Web",
        "destinations": ["Vietnam", "Thailand"],
        "segments": ["New"],
        "minimumSpend": 2_000_000,
        "description": "Designed for newly registered travelers comparing their first departure options.",
    },
    {
        "code": "SUMMER",
        "title": "Summer beach departure offer",
        "campaignType": "FlashSale",
        "badgeText": "Hot seats",
        "campaignScope": "CRM",
        "destinations": ["Da Nang", "Nha Trang", "Phu Quoc"],
        "segments": ["Standard", "VIP"],
        "minimumSpend": 4_000_000,
        "description": "Used for short-notice coastal departures with strong price sensitivity.",
    },
    {
        "code": "FAMILY",
        "title": "Family rooming upgrade package",
        "campaignType": "Seasonal",
        "badgeText": "Family pick",
        "campaignScope": "Admin",
        "destinations": ["Phu Quoc", "Da Nang", "Singapore"],
        "segments": ["New", "Standard"],
        "minimumSpend": 8_000_000,
        "description": "Targets households traveling with children and requiring flexible hotel support.",
    },
    {
        "code": "LOYAL",
        "title": "Repeat traveler loyalty voucher",
        "campaignType": "Loyalty",
        "badgeText": "Member perk",
        "campaignScope": "CRM",
        "destinations": ["Ha Giang", "Da Lat", "Japan"],
        "segments": ["VIP", "Standard"],
        "minimumSpend": 6_000_000,
        "description": "Reserved for returning guests with recent spending or loyalty redemptions.",
    },
)

_ARTICLE_CATALOG = (
    ("Best time to visit Da Nang and Hoi An", "Destination Guide", "Da Nang", ["da-nang", "hoi-an", "season"]),
    ("How much should you budget for a Ha Giang loop", "Budget Planning", "Ha Giang", ["ha-giang", "budget", "north-vietnam"]),
    ("Documents to prepare before a family trip to Singapore", "Visa Tips", "Singapore", ["singapore", "family", "documents"]),
    ("Where to stay in Da Lat for a short weekend trip", "Hotel Guide", "Da Lat", ["da-lat", "hotel", "weekend"]),
    ("What to know before booking a Ha Long Bay cruise", "Planning Guide", "Quang Ninh", ["ha-long", "cruise", "planning"]),
    ("Rainy season travel tips for central Vietnam departures", "Seasonal Advice", "Hue", ["hue", "da-nang", "weather"]),
)

_SECTION_TEMPLATES = (
    ("home", "hero", "Plan trips with clear logistics", "Curated departures, transparent pricing, and support that stays responsive after booking."),
    ("home", "featured-tours", "Popular departures this month", "Highlight the routes with the strongest conversion and the healthiest seat pacing."),
    ("about", "service-story", "How our operations team supports each departure", "Explain how sales, operations, and guides hand off information across the booking journey."),
    ("tours", "trust-bar", "Why guests book with HV Travel", "Show verified reviews, seat availability, and destination-specific planning help."),
    ("contact", "support-options", "Reach support in the way that fits your timing", "Surface phone, chat, and email coverage for urgent and non-urgent requests."),
    ("blog", "editor-picks", "Planning notes from recent customer questions", "Turn recurring support questions into practical travel content."),
)


def _style(index: int) -> dict:
    return {
        "align": ("left", "center", "right")[index % 3],
        "fontPreset": ("display", "body", "accent")[index % 3],
        "customFontFamily": "" if index % 4 else "Merriweather",
        "sizePreset": ("sm", "md", "lg", "xl")[index % 4],
        "customSizeValue": None if index % 5 else 18,
        "customSizeUnit": "" if index % 5 else "px",
        "colorPreset": ("default", "muted", "accent")[index % 3],
        "customColorHex": "" if index % 6 else "#0B3D2E",
    }


def _field(key: str, label: str, value: str, index: int, field_type: str = "text") -> dict:
    return {
        "key": key,
        "label": label,
        "fieldType": field_type,
        "value": value,
        "placeholder": f"Enter {label.lower()}",
        "style": _style(index),
    }


def build_promotions(ctx) -> None:
    items = []
    for index in range(ctx.counts["Promotions"]):
        template = _PROMOTION_CATALOG[index % len(_PROMOTION_CATALOG)]
        active = index % 5 != 4
        discount = float(5 + ((index + 1) % 5) * 5)
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "code": f"{template['code']}{2026 + (index % 2)}{index + 1:02d}",
                "title": template["title"],
                "campaignType": template["campaignType"],
                "discountPercentage": discount,
                "discountValue": 0,
                "description": f"{template['description']} {ctx.factory.paragraph(1)}",
                "badgeText": template["badgeText"],
                "campaignScope": template["campaignScope"],
                "minimumSpend": template["minimumSpend"] + (index % 3) * 500_000,
                "usageLimit": 80 + index * 8,
                "usageCount": 12 + index * 3,
                "eligibleSegments": list(template["segments"]),
                "applicableDestinations": list(template["destinations"]),
                "terms": "Cannot be combined with group contract pricing or manual finance adjustments.",
                "imageUrl": ctx.factory.image_url("promotion-banner", index),
                "priority": index % 10,
                "highlightPriceText": f"Save {int(discount)}% on selected departures",
                "isFlashSale": template["campaignType"] == "FlashSale",
                "validFrom": ctx.factory.iso_timestamp(days_back=30),
                "validTo": ctx.factory.future_date(5, 120) if active else ctx.factory.iso_timestamp(days_back=2),
                "isActive": active,
            }
        )
    ctx.collections["Promotions"] = items


def build_travel_articles(ctx) -> None:
    items = []
    for index in range(ctx.counts["TravelArticles"]):
        title, category, destination, tags = _ARTICLE_CATALOG[index % len(_ARTICLE_CATALOG)]
        published = index % 5 != 0
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "slug": ctx.factory.slug(title),
                "title": title,
                "summary": f"{destination} planning note based on common customer questions and recent booking patterns.",
                "body": f"<p>{ctx.factory.paragraph(2)}</p><p>{ctx.factory.paragraph(2)}</p>",
                "category": category,
                "destination": destination,
                "heroImageUrl": ctx.factory.image_url("article-hero", index),
                "tags": list(tags),
                "featured": index % 4 == 0,
                "isPublished": published,
                "publishedAt": ctx.factory.iso_timestamp(days_back=40),
                "createdAt": ctx.factory.iso_timestamp(days_back=60),
                "updatedAt": ctx.factory.iso_timestamp(days_back=5),
            }
        )
    ctx.collections["TravelArticles"] = items


def build_site_settings(ctx) -> None:
    items = []
    for index in range(ctx.counts["siteSettings"]):
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "settingsKey": "default" if index == 0 else f"seasonal-{index}",
                "groups": [
                    {
                        "groupKey": "branding",
                        "title": "Branding",
                        "description": "Core public brand settings",
                        "isEnabled": True,
                        "displayOrder": 1,
                        "fields": [
                            _field("siteName", "Site Name", "HV Travel", index),
                            _field("tagline", "Tagline", "Curated departures with clear support from quote to return", index + 1),
                        ],
                    },
                    {
                        "groupKey": "contact",
                        "title": "Contact",
                        "description": "Default support information",
                        "isEnabled": True,
                        "displayOrder": 2,
                        "fields": [
                            _field("supportEmail", "Support Email", "support@hvtravel.vn", index + 2),
                            _field("supportPhone", "Support Phone", "0912345678", index + 3),
                        ],
                    },
                    {
                        "groupKey": "social",
                        "title": "Social",
                        "description": "Public social links",
                        "isEnabled": True,
                        "displayOrder": 3,
                        "fields": [
                            _field("facebook", "Facebook", "https://facebook.com/hvtravel.vn", index + 4),
                            _field("youtube", "YouTube", "https://youtube.com/@hvtravel", index + 5),
                        ],
                    },
                ],
                "updatedAt": ctx.factory.iso_timestamp(days_back=7),
            }
        )
    ctx.collections["siteSettings"] = items


def build_content_sections(ctx) -> None:
    items = []
    for index in range(ctx.counts["contentSections"]):
        page_key, section_key, title, description = _SECTION_TEMPLATES[index % len(_SECTION_TEMPLATES)]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "pageKey": page_key,
                "sectionKey": f"{section_key}-{index + 1}",
                "title": title,
                "description": description,
                "isEnabled": index % 6 != 0,
                "isPublished": index % 5 != 0,
                "displayOrder": index + 1,
                "fields": [
                    _field("eyebrow", "Eyebrow", "Editorial pick" if index % 2 == 0 else "Operations insight", index),
                    _field("headline", "Headline", title, index + 1),
                    _field("body", "Body", f"{description} {ctx.factory.paragraph(1)}", index + 2, field_type="textarea"),
                    _field("ctaLabel", "CTA Label", "Explore departures" if page_key in {"home", "tours"} else "Contact support", index + 3),
                ],
                "presentation": {
                    "container": {
                        "align": ("left", "center")[index % 2],
                        "backgroundPreset": ("default", "warm", "cool")[index % 3],
                        "customBackgroundHex": "" if index % 4 else "#F4F1EA",
                    },
                    "eyebrowText": _style(index),
                    "titleText": _style(index + 1),
                    "descriptionText": _style(index + 2),
                },
                "updatedAt": ctx.factory.iso_timestamp(days_back=20),
            }
        )
    ctx.collections["contentSections"] = items

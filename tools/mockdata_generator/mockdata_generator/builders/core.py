from __future__ import annotations

import math
from datetime import datetime, timedelta, timezone


_TOUR_CATALOG = (
    {
        "city": "Ha Giang",
        "country": "Vietnam",
        "region": "North",
        "name": "Ha Giang Loop Explorer",
        "summary": "Mountain passes, small villages, and a compact 3-day route through the far north.",
        "base_price": 3_500_000,
        "days": 3,
        "nights": 2,
        "inclusions": ["Sleeper bus", "Homestay", "Local guide", "Breakfast"],
        "exclusions": ["Personal drinks", "Travel insurance"],
        "schedule": (
            ("Ha Giang - Quan Ba - Yen Minh", ("Twin Mountains lookout", "Evening market walk")),
            ("Yen Minh - Dong Van - Ma Pi Leng", ("Ma Pi Leng pass stop", "Nho Que boat ride")),
            ("Dong Van - Meo Vac - Ha Giang", ("Hmong King Palace", "Return to city center")),
        ),
    },
    {
        "city": "Da Nang",
        "country": "Vietnam",
        "region": "Central",
        "name": "Da Nang & Hoi An Discovery",
        "summary": "A relaxed central Vietnam plan with beach time, old town visits, and one hill-top day trip.",
        "base_price": 4_200_000,
        "days": 4,
        "nights": 3,
        "inclusions": ["Hotel 4 star", "Airport transfer", "Breakfast", "Ba Na ticket"],
        "exclusions": ["Dinner on free night", "Personal shopping"],
        "schedule": (
            ("Arrival and My Khe beachfront", ("Airport pickup", "Sunset by the beach")),
            ("Ba Na Hills and Golden Bridge", ("Cable car ride", "Free time in French Village")),
            ("Hoi An old quarter", ("Lantern street walk", "Local food tasting")),
            ("Coffee stop before departure", ("Hotel checkout", "Transfer to airport")),
        ),
    },
    {
        "city": "Quang Ninh",
        "country": "Vietnam",
        "region": "North",
        "name": "Ha Long Bay Heritage Cruise",
        "summary": "Short luxury cruise with cave visits, kayaking, and seafood-focused meals.",
        "base_price": 3_900_000,
        "days": 2,
        "nights": 1,
        "inclusions": ["Cruise cabin", "All main meals", "Kayak service", "Sightseeing tickets"],
        "exclusions": ["Spa package", "Premium drinks"],
        "schedule": (
            ("Boarding and afternoon cruise", ("Welcome lunch", "Kayak near limestone islands")),
            ("Tai chi and return to harbor", ("Sunrise deck session", "Brunch before disembarkation")),
        ),
    },
    {
        "city": "Hue",
        "country": "Vietnam",
        "region": "Central",
        "name": "Hue Royal Heritage Journey",
        "summary": "Historic sites, river views, and a slower itinerary suited to couples and family groups.",
        "base_price": 3_200_000,
        "days": 3,
        "nights": 2,
        "inclusions": ["Boutique hotel", "Boat ticket", "Lunch set", "Guide service"],
        "exclusions": ["Evening snacks", "Optional art workshop"],
        "schedule": (
            ("Imperial city landmarks", ("Citadel entry", "Royal court museum")),
            ("Pagoda and perfume river", ("Thien Mu visit", "Dragon boat sunset ride")),
            ("Garden house and local market", ("Traditional tea stop", "Airport transfer")),
        ),
    },
    {
        "city": "Phu Quoc",
        "country": "Vietnam",
        "region": "South",
        "name": "Phu Quoc Family Beach Escape",
        "summary": "Comfort-focused beach stay with light sightseeing and enough downtime for young children.",
        "base_price": 5_800_000,
        "days": 4,
        "nights": 3,
        "inclusions": ["Resort stay", "Airport transfer", "Breakfast", "Island shuttle"],
        "exclusions": ["Water sport upgrades", "Private dining"],
        "schedule": (
            ("Check-in and sunset coast", ("Resort arrival", "Night market dinner block")),
            ("South island leisure day", ("Cable car option", "Beach club break")),
            ("Family free day", ("Pool time", "Optional safari or grand world visit")),
            ("Late checkout support", ("Souvenir stop", "Airport drop-off")),
        ),
    },
    {
        "city": "Nha Trang",
        "country": "Vietnam",
        "region": "South",
        "name": "Nha Trang Island Leisure Break",
        "summary": "A short coastal holiday with island hopping, seafood meals, and a flexible final day.",
        "base_price": 4_600_000,
        "days": 3,
        "nights": 2,
        "inclusions": ["Hotel stay", "Island boat", "Lunch", "Round-trip transfer"],
        "exclusions": ["Scuba add-on", "Spa service"],
        "schedule": (
            ("Arrival and coastal walk", ("Hotel check-in", "Evening seafood district visit")),
            ("Island hopping day", ("Snorkeling stop", "Floating bar photo break")),
            ("Mud bath or leisure morning", ("Optional mud bath", "Airport transfer")),
        ),
    },
    {
        "city": "Da Lat",
        "country": "Vietnam",
        "region": "Highlands",
        "name": "Da Lat Pine Forest Weekend",
        "summary": "Cool weather, soft adventure, and cafe-heavy pacing for small groups and couples.",
        "base_price": 3_400_000,
        "days": 3,
        "nights": 2,
        "inclusions": ["Central hotel", "City transfer", "Breakfast", "Guide support"],
        "exclusions": ["Jeep rental", "Private photo set"],
        "schedule": (
            ("City highlights and gardens", ("Railway station check-in", "Flower garden walk")),
            ("Outskirts and pine hills", ("Lang Biang viewpoint", "Farm coffee tasting")),
            ("Slow morning before return", ("Market stop", "Departure support")),
        ),
    },
    {
        "city": "Can Tho",
        "country": "Vietnam",
        "region": "Mekong",
        "name": "Can Tho Early Market Getaway",
        "summary": "A compact Mekong experience built around floating market timing and easy local food stops.",
        "base_price": 2_900_000,
        "days": 2,
        "nights": 1,
        "inclusions": ["Hotel room", "Boat ticket", "Breakfast", "Guide support"],
        "exclusions": ["Premium snacks", "Private transfer upgrade"],
        "schedule": (
            ("Arrival and riverside dinner", ("Check-in support", "Short riverside walk")),
            ("Cai Rang floating market", ("Early boat ride", "Fruit garden visit")),
        ),
    },
)

_BOOKING_STATUS_FLOW = ("Completed", "Paid", "Pending", "Confirmed", "Cancelled", "Refunded", "Paid", "Completed")
_MEETING_POINTS_BY_CITY = {
    "Ha Giang": "HV Travel office, 18 Nguyen Trai, Ha Giang City. Meet 30 minutes before departure.",
    "Da Nang": "Da Nang International Airport arrival gate or 32 Bach Dang, Hai Chau, Da Nang.",
    "Quang Ninh": "Tuan Chau International Marina lobby, Quang Ninh.",
    "Hue": "Hue railway station front gate or 12 Le Loi, Hue City.",
    "Phu Quoc": "Phu Quoc Airport arrival hall or resort shuttle desk in Duong Dong.",
    "Nha Trang": "Nha Trang Center pickup bay, 20 Tran Phu, Nha Trang.",
    "Da Lat": "Da Lat night market fountain, Le Dai Hanh street.",
    "Can Tho": "Ninh Kieu Wharf welcome desk, Can Tho City.",
}
_ROUTING_STOP_TEMPLATES = {
    "Ha Giang Loop Explorer": (
        {
            "day": 1,
            "order": 1,
            "name": "Twin Mountains lookout",
            "type": "viewpoint",
            "coordinates": {"lat": 23.0324, "lng": 104.8856},
            "visitMinutes": 35,
            "attractionScore": 8.4,
            "note": "Photo stop with a short uphill walk and a wide valley view.",
        },
        {
            "day": 1,
            "order": 2,
            "name": "Quan Ba town lunch stop",
            "type": "rest-stop",
            "coordinates": {"lat": 23.0461, "lng": 104.9538},
            "visitMinutes": 60,
            "attractionScore": 6.8,
            "note": "Refuel and regroup before continuing toward Yen Minh.",
        },
        {
            "day": 2,
            "order": 1,
            "name": "Ma Pi Leng pass stop",
            "type": "viewpoint",
            "coordinates": {"lat": 23.1603, "lng": 105.2748},
            "visitMinutes": 40,
            "attractionScore": 9.5,
            "note": "Main scenic stop with strong photo value and short walking segments.",
        },
        {
            "day": 2,
            "order": 2,
            "name": "Nho Que boat ride pier",
            "type": "activity",
            "coordinates": {"lat": 23.1567, "lng": 105.2864},
            "visitMinutes": 95,
            "attractionScore": 9.0,
            "note": "Longer stop for boarding, river activity, and return transfer.",
        },
        {
            "day": 3,
            "order": 1,
            "name": "Hmong King Palace",
            "type": "heritage",
            "coordinates": {"lat": 23.1355, "lng": 105.2388},
            "visitMinutes": 50,
            "attractionScore": 8.1,
            "note": "Culture-heavy stop before the return leg to Ha Giang City.",
        },
    ),
    "Da Nang & Hoi An Discovery": (
        {
            "day": 1,
            "order": 1,
            "name": "My Khe beachfront",
            "type": "beach",
            "coordinates": {"lat": 16.0678, "lng": 108.2458},
            "visitMinutes": 70,
            "attractionScore": 8.2,
            "note": "Soft-arrival stop that works well after airport pickup.",
        },
        {
            "day": 2,
            "order": 1,
            "name": "Ba Na Hills cable car",
            "type": "activity",
            "coordinates": {"lat": 15.9951, "lng": 107.9967},
            "visitMinutes": 120,
            "attractionScore": 8.8,
            "note": "High dwell-time stop that anchors the hill-top day.",
        },
        {
            "day": 2,
            "order": 2,
            "name": "Golden Bridge",
            "type": "landmark",
            "coordinates": {"lat": 15.9986, "lng": 107.9963},
            "visitMinutes": 45,
            "attractionScore": 9.1,
            "note": "Shorter but very high-interest landmark inside the Ba Na complex.",
        },
        {
            "day": 3,
            "order": 1,
            "name": "Hoi An old quarter",
            "type": "heritage",
            "coordinates": {"lat": 15.8794, "lng": 108.3380},
            "visitMinutes": 140,
            "attractionScore": 9.2,
            "note": "Main walking block for lantern streets, shops, and food tasting.",
        },
        {
            "day": 4,
            "order": 1,
            "name": "Airport coffee stop",
            "type": "rest-stop",
            "coordinates": {"lat": 16.0439, "lng": 108.1997},
            "visitMinutes": 30,
            "attractionScore": 5.8,
            "note": "Buffer stop before transfer to the terminal.",
        },
    ),
    "Hue Royal Heritage Journey": (
        {
            "day": 1,
            "order": 1,
            "name": "Imperial City",
            "type": "heritage",
            "coordinates": {"lat": 16.4696, "lng": 107.5784},
            "visitMinutes": 110,
            "attractionScore": 9.0,
            "note": "Primary heritage anchor for the first day of the itinerary.",
        },
        {
            "day": 2,
            "order": 1,
            "name": "Thien Mu Pagoda",
            "type": "heritage",
            "coordinates": {"lat": 16.4524, "lng": 107.5450},
            "visitMinutes": 55,
            "attractionScore": 8.3,
            "note": "Short cultural stop before the river segment.",
        },
        {
            "day": 2,
            "order": 2,
            "name": "Perfume River boat pier",
            "type": "activity",
            "coordinates": {"lat": 16.4669, "lng": 107.5882},
            "visitMinutes": 80,
            "attractionScore": 7.7,
            "note": "Boarding and sunset cruise buffer grouped into one route stop.",
        },
        {
            "day": 3,
            "order": 1,
            "name": "Garden house tea stop",
            "type": "culture",
            "coordinates": {"lat": 16.4635, "lng": 107.6024},
            "visitMinutes": 45,
            "attractionScore": 6.9,
            "note": "Lower-intensity final-day stop suited to slower pacing.",
        },
    ),
    "Da Lat Pine Forest Weekend": (
        {
            "day": 1,
            "order": 1,
            "name": "Da Lat railway station",
            "type": "landmark",
            "coordinates": {"lat": 11.9416, "lng": 108.4583},
            "visitMinutes": 40,
            "attractionScore": 7.5,
            "note": "Compact first stop before entering the garden circuit.",
        },
        {
            "day": 1,
            "order": 2,
            "name": "Flower garden walk",
            "type": "garden",
            "coordinates": {"lat": 11.9536, "lng": 108.4448},
            "visitMinutes": 75,
            "attractionScore": 8.0,
            "note": "Gentle walking stop with low logistics overhead.",
        },
        {
            "day": 2,
            "order": 1,
            "name": "Lang Biang viewpoint",
            "type": "viewpoint",
            "coordinates": {"lat": 12.0254, "lng": 108.4381},
            "visitMinutes": 65,
            "attractionScore": 8.7,
            "note": "Main scenic anchor for the outskirts day.",
        },
        {
            "day": 2,
            "order": 2,
            "name": "Farm coffee tasting",
            "type": "culture",
            "coordinates": {"lat": 11.9996, "lng": 108.4065},
            "visitMinutes": 55,
            "attractionScore": 7.4,
            "note": "Mid-length stop that balances photo time and rest time.",
        },
        {
            "day": 3,
            "order": 1,
            "name": "Da Lat market stop",
            "type": "market",
            "coordinates": {"lat": 11.9409, "lng": 108.4382},
            "visitMinutes": 35,
            "attractionScore": 6.6,
            "note": "Short final stop before departure support.",
        },
    ),
    "Can Tho Early Market Getaway": (
        {
            "day": 1,
            "order": 1,
            "name": "Ninh Kieu riverside walk",
            "type": "riverside",
            "coordinates": {"lat": 10.0343, "lng": 105.7871},
            "visitMinutes": 40,
            "attractionScore": 7.0,
            "note": "Light evening stop with low transfer complexity.",
        },
        {
            "day": 2,
            "order": 1,
            "name": "Cai Rang floating market",
            "type": "market",
            "coordinates": {"lat": 10.0017, "lng": 105.7837},
            "visitMinutes": 90,
            "attractionScore": 8.9,
            "note": "Core early-morning activity that drives the route timing.",
        },
        {
            "day": 2,
            "order": 2,
            "name": "Fruit garden visit",
            "type": "garden",
            "coordinates": {"lat": 10.0118, "lng": 105.7619},
            "visitMinutes": 55,
            "attractionScore": 7.3,
            "note": "Follow-on stop that pairs naturally with the market run.",
        },
    ),
}


def _iso(value: datetime) -> str:
    return value.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _build_party_types(participants: int, allow_infant: bool) -> list[str]:
    if participants <= 1:
        return ["Adult"]
    if participants == 2:
        return ["Adult", "Adult"]
    if participants == 3:
        return ["Adult", "Adult", "Child"]
    if allow_infant:
        return ["Adult", "Adult", "Child", "Infant"]
    return ["Adult", "Adult", "Child", "Child"]


def _related_name(ctx, primary_name: str, passenger_index: int) -> str:
    if passenger_index == 0:
        return primary_name
    primary_last_name = primary_name.split()[0]
    generated_name = ctx.factory.person_name()
    name_parts = generated_name.split()
    if len(name_parts) == 3:
        name_parts[0] = primary_last_name
    return " ".join(name_parts)


def _passenger_birth_date(ctx, passenger_type: str) -> str:
    if passenger_type == "Adult":
        return ctx.factory.date_of_birth(23, 65)
    if passenger_type == "Child":
        return ctx.factory.date_of_birth(6, 15)
    return ctx.factory.date_of_birth(0, 2)


def _booking_actor_name(user_role: str, users: list[dict]) -> str:
    candidates = [user["fullName"] for user in users if user["role"] == user_role]
    return candidates[0] if candidates else "HV Travel Team"


def _seo_metadata(name: str, short_description: str, slug: str, images: list[str]) -> dict:
    return {
        "title": f"{name} | HV Travel",
        "description": short_description,
        "canonicalPath": f"/PublicTours/{slug}",
        "openGraphImageUrl": images[0] if images else "",
    }


def _cancellation_policy(days: int, discount: float) -> dict:
    is_free_cancellation = days >= 3 or discount <= 5
    free_cancellation_before_hours = 48 if is_free_cancellation else 24
    summary = (
        f"Free cancellation up to {free_cancellation_before_hours} hours before departure."
        if is_free_cancellation
        else f"Cancellation fees may apply within {free_cancellation_before_hours} hours of departure."
    )
    return {
        "summary": summary,
        "isFreeCancellation": is_free_cancellation,
        "freeCancellationBeforeHours": free_cancellation_before_hours,
    }


def _tour_highlights(catalog: dict) -> list[str]:
    highlights: list[str] = []
    for _, activities in catalog["schedule"]:
        highlights.extend(list(activities))
    highlights.extend(list(catalog["inclusions"][:2]))

    deduplicated: list[str] = []
    for item in highlights:
        if item not in deduplicated:
            deduplicated.append(item)
    return deduplicated[:4]


def _badge_set(base_price: int, discount: float, status: str, current_participants: int, max_participants: int) -> list[str]:
    badges: list[str] = []
    if discount > 0:
        badges.append("deal")
    if status == "ComingSoon":
        badges.append("coming-soon")
    elif status == "SoldOut":
        badges.append("sold-out")
    elif current_participants / max(max_participants, 1) >= 0.75:
        badges.append("limited")
    if base_price >= 5_000_000:
        badges.append("premium")
    return badges


def _departure_booked_counts(current_participants: int, departure_count: int, capacity: int, status: str) -> list[int]:
    if departure_count <= 0:
        return []
    if status == "SoldOut":
        return [capacity for _ in range(departure_count)]
    if status == "ComingSoon":
        return [0 for _ in range(departure_count)]

    base = current_participants // departure_count
    remainder = current_participants % departure_count
    return [min(capacity, base + (1 if index < remainder else 0)) for index in range(departure_count)]


def _build_departures(
    slug: str,
    start_dates: list[str],
    price: dict,
    max_participants: int,
    current_participants: int,
    confirmation_type: str,
    status: str,
) -> list[dict]:
    departure_status = "SoldOut" if status == "SoldOut" else "Scheduled"
    booked_counts = _departure_booked_counts(current_participants, len(start_dates), max_participants, status)
    departures = []
    for index, start_date in enumerate(start_dates):
        departures.append(
            {
                "id": f"{slug}-departure-{index + 1}",
                "startDate": start_date,
                "adultPrice": price["adult"],
                "childPrice": price["child"],
                "infantPrice": price["infant"],
                "discountPercentage": price["discount"],
                "capacity": max_participants,
                "bookedCount": booked_counts[index],
                "confirmationType": confirmation_type,
                "status": departure_status,
                "cutoffHours": 24,
            }
        )
    return departures


def _build_routing(name: str, slug: str) -> dict | None:
    templates = _ROUTING_STOP_TEMPLATES.get(name)
    if not templates:
        return None

    return {
        "schemaVersion": 1,
        "stops": [
            {
                "id": f"{slug}-day{item['day']}-stop{item['order']}",
                "day": item["day"],
                "order": item["order"],
                "name": item["name"],
                "type": item["type"],
                "coordinates": dict(item["coordinates"]),
                "visitMinutes": item["visitMinutes"],
                "attractionScore": item["attractionScore"],
                "note": item["note"],
            }
            for item in templates
        ],
    }


def build_users(ctx) -> None:
    roles = ("Admin", "Staff", "Manager", "Guide", "Staff", "Client")
    statuses = ("Active", "Active", "Active", "Inactive")
    permission_map = {
        "Admin": ["dashboard:view", "bookings:edit", "users:edit", "content:edit"],
        "Manager": ["dashboard:view", "bookings:view", "payments:view"],
        "Staff": ["bookings:view", "customers:view", "chat:reply"],
        "Guide": ["tours:view", "bookings:view"],
        "Client": [],
    }
    items = []
    for index in range(ctx.counts["Users"]):
        full_name = ctx.factory.person_name()
        role = roles[index % len(roles)]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "email": ctx.factory.email(full_name, index),
                "passwordHash": ctx.factory.password_hash(f"user:{role}:{full_name}", index),
                "role": role,
                "fullName": full_name,
                "avatarUrl": ctx.factory.image_url(f"{role.lower()}-avatar", index),
                "status": statuses[index % len(statuses)],
                "lastLogin": ctx.factory.iso_timestamp(days_back=45) if role != "Client" and index % 4 != 0 else None,
                "permissions": permission_map[role],
                "createdAt": ctx.factory.iso_timestamp(days_back=720),
            }
        )
    ctx.collections["Users"] = items


def build_tours(ctx) -> None:
    items = []
    for index in range(ctx.counts["Tours"]):
        catalog = _TOUR_CATALOG[index % len(_TOUR_CATALOG)]
        adult_price = catalog["base_price"] + (ctx.factory.integer(0, 4) * 100_000)
        discount = float((index % 4) * 5)
        max_participants = ctx.factory.integer(14, 28)
        status = ("Active", "Active", "Active", "ComingSoon", "SoldOut")[index % 5]
        if status == "SoldOut":
            current_participants = max_participants
        elif status == "ComingSoon":
            current_participants = ctx.factory.integer(0, max(1, max_participants // 3))
        else:
            current_participants = ctx.factory.integer(max(2, max_participants // 4), max_participants - 1)
        start_dates = [
            ctx.factory.future_date(3 + (step * 7), 50 + (step * 12))
            for step in range(3 if catalog["days"] >= 3 else 2)
        ]
        slug = f"{ctx.factory.slug(catalog['name'])}-{index + 1:05d}"
        images = [
            ctx.factory.image_url(catalog["city"], index),
            ctx.factory.image_url(f"{catalog['city']}-detail", index),
        ]
        confirmation_type = "Instant"
        price = {
            "adult": adult_price,
            "child": int(adult_price * 0.72),
            "infant": int(adult_price * 0.12),
            "currency": "VND",
            "discount": discount,
        }
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "code": f"TOUR-{index + 1:05d}",
                "name": catalog["name"],
                "slug": slug,
                "description": f"<p>{catalog['summary']}</p><p>{ctx.factory.paragraph(2)}</p>",
                "shortDescription": catalog["summary"],
                "seo": _seo_metadata(catalog["name"], catalog["summary"], slug, images),
                "destination": {
                    "city": catalog["city"],
                    "country": catalog["country"],
                    "region": catalog["region"],
                },
                "images": images,
                "price": price,
                "duration": {
                    "days": catalog["days"],
                    "nights": catalog["nights"],
                    "text": f"{catalog['days']} Days {catalog['nights']} Nights",
                },
                "startDates": start_dates,
                "departures": _build_departures(
                    slug,
                    start_dates,
                    price,
                    max_participants,
                    current_participants,
                    confirmation_type,
                    status,
                ),
                "schedule": [
                    {
                        "day": day_index + 1,
                        "title": title,
                        "description": ctx.factory.paragraph(2),
                        "activities": list(activities),
                    }
                    for day_index, (title, activities) in enumerate(catalog["schedule"])
                ],
                "highlights": _tour_highlights(catalog),
                "generatedInclusions": list(catalog["inclusions"]),
                "generatedExclusions": list(catalog["exclusions"]),
                "cancellationPolicy": _cancellation_policy(catalog["days"], discount),
                "confirmationType": confirmation_type,
                "meetingPoint": _MEETING_POINTS_BY_CITY.get(catalog["city"], "HV Travel service desk. Meet 30 minutes before departure."),
                "badgeSet": _badge_set(catalog["base_price"], discount, status, current_participants, max_participants),
                "routing": _build_routing(catalog["name"], slug),
                "maxParticipants": max_participants,
                "currentParticipants": current_participants,
                "rating": round(4.4 + ((index % 5) * 0.1), 1),
                "reviewCount": 18 + (index * 7),
                "createdAt": ctx.factory.iso_timestamp(days_back=540),
                "updatedAt": ctx.factory.iso_timestamp(days_back=45),
                "version": index % 4,
                "status": status,
            }
        )
    ctx.collections["Tours"] = items


def build_customers(ctx) -> None:
    items = []
    for index in range(ctx.counts["Customers"]):
        full_name = ctx.factory.person_name()
        trip_count = (index * 3) % 8
        lifetime_spend = trip_count * (2_500_000 + (index % 5) * 1_200_000)
        if lifetime_spend >= 35_000_000:
            segment = "VIP"
        elif trip_count == 0 and index % 4 == 0:
            segment = "New"
        elif index % 7 == 0:
            segment = "ChurnRisk"
        elif index % 11 == 0:
            segment = "Inactive"
        else:
            segment = "Standard"
        if lifetime_spend >= 40_000_000:
            tier = "Platinum"
        elif lifetime_spend >= 20_000_000:
            tier = "Gold"
        elif lifetime_spend >= 8_000_000:
            tier = "Silver"
        else:
            tier = "Explorer"
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "customerCode": f"CUS-{7000 + index:04d}",
                "fullName": full_name,
                "email": ctx.factory.email(full_name, 1000 + index),
                "password": ctx.factory.password_hash(f"customer:{full_name}", index),
                "phoneNumber": ctx.factory.phone(),
                "avatarUrl": ctx.factory.image_url("customer-avatar", index),
                "address": {
                    "street": ctx.factory.street(),
                    "city": ctx.factory.city(),
                    "country": "Vietnam",
                },
                "notes": ctx.factory.paragraph(1) if segment in {"VIP", "ChurnRisk"} else "",
                "segment": segment,
                "status": "Banned" if segment == "Inactive" and index % 13 == 0 else "Active",
                "emailVerified": index % 5 != 0,
                "tokenVersion": index % 4,
                "stats": {
                    "lifetimeSpend": lifetime_spend,
                    "tripCount": trip_count,
                    "loyaltyPoints": trip_count * 180,
                    "pendingPoints": 60 if trip_count and index % 6 == 0 else 0,
                    "tier": tier,
                    "referralCode": f"REF-{index + 1:04d}",
                    "voucherBalance": index % 4,
                    "lastActivity": ctx.factory.iso_timestamp(days_back=45 if trip_count else 10),
                    "lastCompletedTripAt": ctx.factory.iso_timestamp(days_back=120) if trip_count else None,
                },
                "tags": ["vip"] if segment == "VIP" else (["churn-watch"] if segment == "ChurnRisk" else ["returning"] if trip_count > 1 else ["lead"]),
                "createdAt": ctx.factory.iso_timestamp(days_back=900),
                "updatedAt": ctx.factory.iso_timestamp(days_back=20),
            }
        )
    ctx.collections["Customers"] = items


def build_bookings(ctx) -> None:
    customers = ctx.collections["Customers"]
    tours = ctx.collections["Tours"]
    booking_count = ctx.counts["Bookings"]
    if not customers or not tours:
        ctx.collections["Bookings"] = []
        return

    minimum_active_customers = math.ceil(booking_count / 5)
    target_active_customers = max(minimum_active_customers, int(len(customers) * 0.8))
    active_customer_count = 1 if len(customers) == 1 else min(len(customers) - 1, max(1, target_active_customers))
    customer_pool = [customer for customer in customers if customer["status"] == "Active"] or customers
    active_customers = ctx.factory.shuffled(customer_pool)[:active_customer_count]
    users = ctx.collections["Users"]
    items = []
    customer_booking_map = {customer["_id"]: [] for customer in customers}

    for index in range(booking_count):
        customer = active_customers[index % len(active_customers)]
        tour = tours[(index * 3 + ctx.factory.integer(0, len(tours) - 1)) % len(tours)]
        status = _BOOKING_STATUS_FLOW[index % len(_BOOKING_STATUS_FLOW)]
        tour_start = tour["startDates"][index % len(tour["startDates"])]
        tour_start_dt = ctx.factory.parse_utc(tour_start)
        created_dt = tour_start_dt - timedelta(days=ctx.factory.integer(7, 110), hours=ctx.factory.integer(1, 20))
        created_at = _iso(created_dt)
        payment_session_at = _iso(created_dt + timedelta(minutes=35))
        payment_method = ("CreditCard", "BankTransfer", "EWallet")[index % 3]
        provider = {"CreditCard": "HVPay", "BankTransfer": "ManualTransfer", "EWallet": "MoMo"}[payment_method]
        passenger_types = _build_party_types(1 + (index % 4), allow_infant=index % 5 == 0)
        passengers = []
        for passenger_index, passenger_type in enumerate(passenger_types):
            passengers.append(
                {
                    "fullName": _related_name(ctx, customer["fullName"], passenger_index),
                    "birthDate": _passenger_birth_date(ctx, passenger_type),
                    "type": passenger_type,
                    "gender": ("Female", "Male")[passenger_index % 2],
                    "passportNumber": ctx.factory.passport_number(index, passenger_index),
                }
            )

        adult_count = sum(1 for passenger in passengers if passenger["type"] == "Adult")
        child_count = sum(1 for passenger in passengers if passenger["type"] == "Child")
        infant_count = sum(1 for passenger in passengers if passenger["type"] == "Infant")
        total_amount = (
            adult_count * int(tour["price"]["adult"])
            + child_count * int(tour["price"]["child"])
            + infant_count * int(tour["price"]["infant"])
        )
        payment_status = {
            "Pending": "Pending",
            "Paid": "Full",
            "Confirmed": "Full",
            "Completed": "Full",
            "Cancelled": "Unpaid",
            "Refunded": "Refunded",
        }[status]
        payment_amount = total_amount if payment_status in {"Full", "Refunded"} else max(int(total_amount * 0.5), 1_000_000)
        processed_at = None
        confirmed_at = None
        completed_at = None
        refunded_at = None
        if payment_status in {"Full", "Refunded"}:
            processed_at = _iso(created_dt + timedelta(hours=ctx.factory.integer(1, 30)))
            confirmed_at = _iso(created_dt + timedelta(hours=ctx.factory.integer(6, 60))) if status in {"Confirmed", "Completed"} else None
            completed_at = _iso(tour_start_dt + timedelta(days=tour["duration"]["days"] - 1, hours=6)) if status == "Completed" else None
            refunded_at = _iso(created_dt + timedelta(days=2, hours=3)) if status == "Refunded" else None
        updated_at = refunded_at or completed_at or confirmed_at or processed_at or payment_session_at
        booking_code = ctx.factory.booking_code(created_at, index)
        transaction_id = ctx.factory.transaction_code(booking_code, processed_at or payment_session_at)
        receipt_number = ctx.factory.receipt_number(booking_code) if payment_status in {"Full", "Refunded"} else None
        transfer_proof = payment_method == "BankTransfer" and payment_status in {"Pending", "Full", "Refunded"}
        coordinator_name = _booking_actor_name("Staff", users)
        finance_name = _booking_actor_name("Manager", users)

        history_log = [
            {
                "action": "Booking created",
                "timestamp": created_at,
                "user": customer["fullName"],
                "note": f"Reserved {len(passengers)} seats on {tour['name']}.",
            },
            {
                "action": "Payment session opened",
                "timestamp": payment_session_at,
                "user": coordinator_name,
                "note": f"{payment_method} instructions were sent to {customer['email']}.",
            },
        ]
        events = [
            {
                "type": "booking-created",
                "title": "Booking created",
                "description": f"Booking {booking_code} was created for {tour['name']}.",
                "occurredAt": created_at,
                "actor": customer["fullName"],
                "visibleToCustomer": True,
            },
            {
                "type": "payment",
                "title": "Payment session opened",
                "description": f"{payment_method} instructions were shared with the guest.",
                "occurredAt": payment_session_at,
                "actor": coordinator_name,
                "visibleToCustomer": True,
            },
        ]

        if processed_at:
            history_log.append(
                {
                    "action": "Payment received",
                    "timestamp": processed_at,
                    "user": finance_name if payment_method == "BankTransfer" else provider,
                    "note": f"Recorded {payment_amount:,} VND via {payment_method}.",
                }
            )
            events.append(
                {
                    "type": "payment",
                    "title": "Payment received",
                    "description": f"{provider} confirmed the transaction successfully.",
                    "occurredAt": processed_at,
                    "actor": provider,
                    "visibleToCustomer": True,
                }
            )
        if confirmed_at:
            history_log.append(
                {
                    "action": "Booking confirmed",
                    "timestamp": confirmed_at,
                    "user": coordinator_name,
                    "note": "Operations confirmed rooming and departure details.",
                }
            )
            events.append(
                {
                    "type": "booking-confirmed",
                    "title": "Booking confirmed",
                    "description": "The service team confirmed logistics for this booking.",
                    "occurredAt": confirmed_at,
                    "actor": coordinator_name,
                    "visibleToCustomer": True,
                }
            )
        if completed_at:
            history_log.append(
                {
                    "action": "Trip completed",
                    "timestamp": completed_at,
                    "user": finance_name,
                    "note": "The itinerary was marked complete after guide handoff.",
                }
            )
            events.append(
                {
                    "type": "trip-completed",
                    "title": "Trip completed",
                    "description": "The trip finished successfully and post-trip follow-up was queued.",
                    "occurredAt": completed_at,
                    "actor": finance_name,
                    "visibleToCustomer": True,
                }
            )
        cancellation_request = None
        if status in {"Cancelled", "Refunded"}:
            requested_at = _iso(created_dt + timedelta(days=1, hours=2))
            processed_cancel_at = refunded_at or _iso(created_dt + timedelta(days=1, hours=8))
            cancellation_request = {
                "status": "Approved",
                "reason": "Date no longer matches the travel party schedule.",
                "requestedAt": requested_at,
                "requestedBy": customer["fullName"],
                "processedAt": processed_cancel_at,
                "resolutionNote": "Approved after operations reviewed inventory and fare rules.",
            }
            history_log.append(
                {
                    "action": "Cancellation approved",
                    "timestamp": processed_cancel_at,
                    "user": coordinator_name,
                    "note": cancellation_request["resolutionNote"],
                }
            )
            events.append(
                {
                    "type": "booking-cancelled",
                    "title": "Cancellation approved",
                    "description": cancellation_request["reason"],
                    "occurredAt": processed_cancel_at,
                    "actor": coordinator_name,
                    "visibleToCustomer": True,
                }
            )
        if refunded_at:
            history_log.append(
                {
                    "action": "Refund settled",
                    "timestamp": refunded_at,
                    "user": finance_name,
                    "note": "Finance completed the refund back to the original payment channel.",
                }
            )
            events.append(
                {
                    "type": "refund",
                    "title": "Refund settled",
                    "description": "Funds were returned after the approved cancellation workflow finished.",
                    "occurredAt": refunded_at,
                    "actor": finance_name,
                    "visibleToCustomer": True,
                }
            )

        booking = {
            "_id": ctx.factory.object_id(),
            "bookingCode": booking_code,
            "tourId": tour["_id"],
            "tourSnapshot": {
                "code": tour["code"],
                "name": tour["name"],
                "startDate": tour_start,
                "duration": tour["duration"]["text"],
            },
            "customerId": customer["_id"],
            "bookingDate": created_at,
            "totalAmount": total_amount,
            "status": status,
            "paymentStatus": payment_status,
            "participantsCount": len(passengers),
            "passengers": passengers,
            "contactInfo": {
                "name": customer["fullName"],
                "email": customer["email"],
                "phone": customer["phoneNumber"],
            },
            "notes": ctx.factory.paragraph(1) if len(passengers) > 2 or payment_method == "BankTransfer" else "",
            "historyLog": history_log,
            "events": events,
            "paymentTransactions": [
                {
                    "provider": provider,
                    "method": payment_method,
                    "transactionId": transaction_id,
                    "reference": f"PAY-{booking_code}",
                    "amount": payment_amount,
                    "status": {
                        "Pending": "Pending",
                        "Full": "Completed",
                        "Unpaid": "Rejected",
                        "Refunded": "Refunded",
                    }[payment_status],
                    "receivedFromWebhook": payment_method != "BankTransfer",
                    "payloadHash": f"{booking_code}|{transaction_id}|{payment_status}|{payment_amount}",
                    "createdAt": payment_session_at,
                    "processedAt": processed_at or refunded_at,
                }
            ],
            "cancellationRequest": cancellation_request,
            "publicLookupEnabled": index % 7 != 0,
            "receiptNumber": receipt_number,
            "transferProofFileName": f"{booking_code.lower()}-proof.png" if transfer_proof else None,
            "transferProofContentType": "image/png" if transfer_proof else None,
            "transferProofBase64": "cHJvb2Y=" if transfer_proof else None,
            "confirmedAt": confirmed_at,
            "completedAt": completed_at,
            "createdAt": created_at,
            "updatedAt": updated_at,
            "isDeleted": False,
            "deletedBy": None,
            "deletedAt": None,
        }
        items.append(booking)
        customer_booking_map[customer["_id"]].append(booking)

    ctx.collections["Bookings"] = items
    ctx.state["customer_bookings"] = customer_booking_map
    ctx.state["reviewable_bookings"] = [item for item in items if item["status"] in {"Paid", "Confirmed", "Completed"}]
    ctx.state["completed_bookings"] = [item for item in items if item["status"] == "Completed"]
    ctx.state["payment_eligible_bookings"] = list(items)


def build_payments(ctx) -> None:
    bookings = ctx.state.get("payment_eligible_bookings", [])
    items = []
    if not bookings:
        ctx.collections["Payments"] = items
        return
    for index in range(ctx.counts["Payments"]):
        booking = bookings[index % len(bookings)]
        transaction = booking["paymentTransactions"][0]
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "bookingId": booking["_id"],
                "amount": transaction["amount"],
                "paymentMethod": transaction["method"],
                "transactionId": f"PAY-{index + 1:06d}",
                "paymentDate": transaction["processedAt"] or transaction["createdAt"],
                "status": {"Pending": "Pending", "Completed": "Success", "Rejected": "Failed", "Refunded": "Success"}[
                    transaction["status"]
                ],
            }
        )
    ctx.collections["Payments"] = items


def build_reviews(ctx) -> None:
    bookings = ctx.state.get("reviewable_bookings", [])
    customers = {item["_id"]: item for item in ctx.collections["Customers"]}
    tours = {item["_id"]: item for item in ctx.collections["Tours"]}
    staff_names = [user["fullName"] for user in ctx.collections["Users"] if user["role"] in {"Admin", "Manager", "Staff"}]
    items = []
    shuffled = ctx.factory.shuffled(bookings)
    for index in range(min(ctx.counts["Reviews"], len(shuffled))):
        booking = shuffled[index]
        customer = customers[booking["customerId"]]
        tour = tours[booking["tourId"]]
        approved = index % 4 != 0
        items.append(
            {
                "_id": ctx.factory.object_id(),
                "tourId": tour["_id"],
                "customerId": customer["_id"],
                "bookingId": booking["_id"],
                "rating": 4 + (index % 2),
                "comment": f"{tour['name']} matched the booking expectations. {ctx.factory.paragraph(1)}",
                "displayName": customer["fullName"],
                "createdAt": ctx.factory.iso_timestamp(days_back=85),
                "isApproved": approved,
                "isVerifiedBooking": True,
                "moderationStatus": "Approved" if approved else "Pending",
                "moderatedAt": ctx.factory.iso_timestamp(days_back=80) if approved else None,
                "moderatorName": staff_names[index % len(staff_names)] if approved and staff_names else "",
            }
        )
    ctx.collections["Reviews"] = items

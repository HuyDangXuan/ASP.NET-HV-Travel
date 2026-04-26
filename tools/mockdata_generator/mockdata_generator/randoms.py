from __future__ import annotations

import hashlib
import random
import re
from datetime import datetime, timedelta, timezone


BASE_NOW = datetime(2026, 3, 29, 12, 0, 0, tzinfo=timezone.utc)


class SeededFactory:
    _FIRST_NAMES = (
        "Anh",
        "Bao",
        "Binh",
        "Chi",
        "Dung",
        "Giang",
        "Ha",
        "Hanh",
        "Hoa",
        "Huy",
        "Khanh",
        "Lam",
        "Lan",
        "Linh",
        "Long",
        "Mai",
        "Minh",
        "Nam",
        "Ngoc",
        "Phuc",
        "Phuong",
        "Quang",
        "Trang",
        "Tuan",
        "Vy",
    )
    _LAST_NAMES = (
        "Nguyen",
        "Tran",
        "Le",
        "Pham",
        "Hoang",
        "Phan",
        "Vu",
        "Vo",
        "Dang",
        "Bui",
        "Do",
        "Ho",
    )
    _CITIES = (
        "Ha Noi",
        "Da Nang",
        "Ho Chi Minh City",
        "Hai Phong",
        "Nha Trang",
        "Hue",
        "Can Tho",
        "Quy Nhon",
        "Da Lat",
        "Vung Tau",
    )
    _STREETS = (
        "Le Loi",
        "Tran Phu",
        "Hai Ba Trung",
        "Nguyen Hue",
        "Vo Nguyen Giap",
        "Pham Van Dong",
        "Bach Dang",
        "Ly Thuong Kiet",
    )
    _DOMAINS = ("gmail.com", "outlook.com", "yahoo.com", "icloud.com", "company.vn", "fpt.vn")
    _SENTENCE_TEMPLATES = (
        "Guests requested a balanced itinerary with enough free time in the evening.",
        "Our operations team confirmed pickup details before the final departure note went out.",
        "The route works well for first-time visitors who want food, scenery, and easy transfers.",
        "Sales marked this lead as high intent after a same-day follow-up call.",
        "The family asked for child-friendly pacing and a hotel close to the city center.",
        "The customer preferred a later departure so the group could avoid morning traffic.",
        "The guide confirmed weather conditions were suitable for the outdoor segment.",
        "This package performs well with returning guests who value comfort and clear logistics.",
        "The article focuses on visa timing, budget planning, and the best travel window.",
        "The promotion was created for guests comparing multiple departures in the same month.",
        "The support team shared check-in guidance and baggage notes after payment cleared.",
        "The guest asked for vegetarian meals and one room with a sea-facing view.",
    )

    def __init__(self, seed: int) -> None:
        self.seed = seed
        self.random = random.Random(seed)

    def object_id(self) -> str:
        return f"{self.random.getrandbits(96):024x}"

    def choice(self, items):
        return items[self.random.randrange(len(items))]

    def shuffled(self, items):
        copied = list(items)
        self.random.shuffle(copied)
        return copied

    def bool(self, probability: float = 0.5) -> bool:
        return self.random.random() < probability

    def integer(self, minimum: int, maximum: int) -> int:
        return self.random.randint(minimum, maximum)

    def money(self, minimum: int, maximum: int, step: int = 100_000) -> int:
        slots = (maximum - minimum) // step
        return minimum + step * self.random.randint(0, max(slots, 0))

    def iso_timestamp(self, *, days_back: int = 365, days_forward: int = 0) -> str:
        earliest = BASE_NOW - timedelta(days=days_back)
        latest = BASE_NOW + timedelta(days=days_forward)
        total_seconds = max(int((latest - earliest).total_seconds()), 1)
        return self._format_dt(earliest + timedelta(seconds=self.random.randint(0, total_seconds)))

    def future_date(self, minimum_days: int = 3, maximum_days: int = 120) -> str:
        delta_days = self.random.randint(minimum_days, maximum_days)
        return self._format_dt(BASE_NOW + timedelta(days=delta_days))

    def person_name(self) -> str:
        return f"{self.choice(self._LAST_NAMES)} {self.choice(self._FIRST_NAMES)} {self.choice(self._FIRST_NAMES)}"

    def email(self, full_name: str, index: int) -> str:
        slug = self.slug(full_name)
        return f"{slug}.{index}@{self.choice(self._DOMAINS)}"

    def phone(self) -> str:
        prefix = self.choice(("090", "091", "093", "096", "097", "098"))
        suffix = "".join(str(self.random.randint(0, 9)) for _ in range(7))
        return f"{prefix}{suffix}"

    def password_hash(self, label: str, index: int) -> str:
        digest = hashlib.sha256(f"{self.seed}:{label}:{index}".encode("utf-8")).hexdigest()
        return f"pbkdf2_sha256$600000${digest[:16]}${digest[16:48]}"

    def slug(self, value: str) -> str:
        normalized = re.sub(r"[^a-z0-9]+", "-", value.lower())
        return normalized.strip("-") or "item"

    def street(self) -> str:
        return f"{self.integer(1, 999)} {self.choice(self._STREETS)}"

    def city(self) -> str:
        return self.choice(self._CITIES)

    def image_url(self, category: str, index: int) -> str:
        category_slug = self.slug(category)
        return f"https://cdn.hvtravel.vn/mock/{category_slug}/{index:03d}.jpg"

    def sentence(self) -> str:
        return self.choice(self._SENTENCE_TEMPLATES)

    def paragraph(self, sentence_count: int = 2) -> str:
        return " ".join(self.sentence() for _ in range(sentence_count))

    def date_of_birth(self, minimum_age: int = 18, maximum_age: int = 65) -> str:
        age = self.integer(minimum_age, maximum_age)
        extra_days = self.integer(0, 364)
        return self._format_dt(BASE_NOW - timedelta(days=(age * 365) + extra_days))

    def passport_number(self, index: int, suffix: int = 0) -> str:
        prefix = self.choice(("C", "D", "E", "N", "P"))
        return f"{prefix}{index + 1:07d}{suffix:02d}"

    def booking_code(self, timestamp: str, index: int) -> str:
        dt = self.parse_utc(timestamp)
        return f"HV{dt.strftime('%Y%m%d%H%M%S')}{index:03d}"

    def receipt_number(self, booking_code: str) -> str:
        return f"HVT-{booking_code}"

    def transaction_code(self, booking_code: str, timestamp: str) -> str:
        dt = self.parse_utc(timestamp)
        return f"TXN-{booking_code}-{dt.strftime('%H%M%S')}"

    @staticmethod
    def _format_dt(value: datetime) -> str:
        return value.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    @staticmethod
    def parse_utc(value: str | None) -> datetime:
        if value is None:
            return BASE_NOW
        text = value
        if text.endswith("Z"):
            text = text[:-1] + "+00:00"
        return datetime.fromisoformat(text).astimezone(timezone.utc)

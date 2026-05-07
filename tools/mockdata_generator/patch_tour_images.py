"""
patch_tour_images.py
====================
Patches all Tours.json mock-data output files:
  1. Removes tours with destination.city == "Can Tho"
  2. Replaces fake CDN images with real Unsplash URLs from 'images by locations.md'
  3. Cascade-deletes related records in Bookings, Reviews, Payments

Usage:
    python patch_tour_images.py
"""

import json
import os
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
IMAGES_MD = SCRIPT_DIR / "images by locations.md"
OUTPUT_DIR = SCRIPT_DIR / "output"

CITY_TO_REMOVE = "Can Tho"

OUTPUT_FOLDERS = [
    "small",
    "medium",
    "large",
    "phase1-sample",
    "sample-refresh",
    "ui-smoke",
    "verification",
]


def parse_images_md(filepath: Path) -> dict[str, list[str]]:
    """Parse the markdown file and return {city: [url, ...]}."""
    city_images: dict[str, list[str]] = {}
    current_city = None

    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()

            # Detect city header: **Ha Giang:** or Ha Giang: or **Da Nang:**
            header_match = re.match(r"^\*{0,2}([A-Za-z ]+?):?\*{0,2}\s*$", line)
            if header_match and "unsplash" not in line:
                candidate = header_match.group(1).strip().strip("*").strip()
                if candidate and len(candidate) < 30:
                    current_city = candidate
                    if current_city not in city_images:
                        city_images[current_city] = []
                    continue

            # Detect image URL line: \- https://images.unsplash.com/...
            if current_city and "images.unsplash.com" in line:
                # Clean up escaped markdown characters
                url = line.lstrip("\\- ").strip()
                url = url.replace("\\&", "&")
                url = url.replace("\\%", "%")
                city_images[current_city].append(url)

    return city_images


def load_json(filepath: Path) -> list[dict]:
    """Load a JSON array from file."""
    with open(filepath, "r", encoding="utf-8") as f:
        return json.load(f)


def save_json(filepath: Path, data: list[dict]) -> None:
    """Save a JSON array to file with consistent formatting."""
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


def get_oid(obj: dict) -> str:
    """Extract $oid string from a MongoDB-style ObjectId dict."""
    if isinstance(obj, dict) and "$oid" in obj:
        return obj["$oid"]
    return str(obj)


def patch_folder(folder_path: Path, city_images: dict[str, list[str]]) -> dict:
    """Patch all JSON files in a single output folder. Returns stats."""
    stats = {
        "tours_removed": 0,
        "tours_patched": 0,
        "bookings_removed": 0,
        "reviews_removed": 0,
        "payments_removed": 0,
    }

    tours_file = folder_path / "Tours.json"
    if not tours_file.exists():
        return stats

    tours = load_json(tours_file)

    # --- Step 1: Identify Can Tho tour IDs ---
    removed_tour_ids = set()
    kept_tours = []
    for tour in tours:
        city = tour.get("destination", {}).get("city", "")
        if city == CITY_TO_REMOVE:
            removed_tour_ids.add(get_oid(tour["_id"]))
            stats["tours_removed"] += 1
        else:
            kept_tours.append(tour)

    # --- Step 2: Replace images with round-robin Unsplash URLs ---
    city_counters: dict[str, int] = {}
    for tour in kept_tours:
        city = tour.get("destination", {}).get("city", "")
        if city in city_images and city_images[city]:
            idx = city_counters.get(city, 0)
            url = city_images[city][idx % len(city_images[city])]
            tour["images"] = [url]
            city_counters[city] = idx + 1
            stats["tours_patched"] += 1

    save_json(tours_file, kept_tours)

    # --- Step 3: Cascade delete Bookings ---
    bookings_file = folder_path / "Bookings.json"
    removed_booking_ids = set()
    if bookings_file.exists() and removed_tour_ids:
        bookings = load_json(bookings_file)
        kept_bookings = []
        for b in bookings:
            tour_id = get_oid(b.get("tourId", {}))
            if tour_id in removed_tour_ids:
                removed_booking_ids.add(get_oid(b["_id"]))
                stats["bookings_removed"] += 1
            else:
                kept_bookings.append(b)
        save_json(bookings_file, kept_bookings)

    # --- Step 4: Cascade delete Reviews ---
    reviews_file = folder_path / "Reviews.json"
    if reviews_file.exists() and removed_tour_ids:
        reviews = load_json(reviews_file)
        kept_reviews = []
        for r in reviews:
            tour_id = get_oid(r.get("tourId", {}))
            if tour_id in removed_tour_ids:
                stats["reviews_removed"] += 1
            else:
                kept_reviews.append(r)
        save_json(reviews_file, kept_reviews)

    # --- Step 5: Cascade delete Payments ---
    payments_file = folder_path / "Payments.json"
    if payments_file.exists() and removed_booking_ids:
        payments = load_json(payments_file)
        kept_payments = []
        for p in payments:
            booking_id = get_oid(p.get("bookingId", {}))
            if booking_id in removed_booking_ids:
                stats["payments_removed"] += 1
            else:
                kept_payments.append(p)
        save_json(payments_file, kept_payments)

    return stats


def main():
    if not IMAGES_MD.exists():
        print(f"ERROR: Images file not found: {IMAGES_MD}")
        sys.exit(1)

    if not OUTPUT_DIR.exists():
        print(f"ERROR: Output directory not found: {OUTPUT_DIR}")
        sys.exit(1)

    # Parse image URLs from markdown
    city_images = parse_images_md(IMAGES_MD)
    print("=== Parsed image URLs from MD ===")
    for city, urls in sorted(city_images.items()):
        print(f"  {city}: {len(urls)} images")
    print()

    # Process each output folder
    total_stats = {
        "tours_removed": 0,
        "tours_patched": 0,
        "bookings_removed": 0,
        "reviews_removed": 0,
        "payments_removed": 0,
    }

    for folder_name in OUTPUT_FOLDERS:
        folder_path = OUTPUT_DIR / folder_name
        if not folder_path.exists():
            print(f"SKIP: {folder_name} (not found)")
            continue

        stats = patch_folder(folder_path, city_images)
        print(f"--- {folder_name} ---")
        print(f"  Tours removed (Can Tho):  {stats['tours_removed']}")
        print(f"  Tours patched (images):   {stats['tours_patched']}")
        print(f"  Bookings removed:         {stats['bookings_removed']}")
        print(f"  Reviews removed:          {stats['reviews_removed']}")
        print(f"  Payments removed:         {stats['payments_removed']}")

        for key in total_stats:
            total_stats[key] += stats[key]

    print()
    print("=== TOTAL ===")
    for key, val in total_stats.items():
        print(f"  {key}: {val}")
    print()
    print("Done!")


if __name__ == "__main__":
    main()

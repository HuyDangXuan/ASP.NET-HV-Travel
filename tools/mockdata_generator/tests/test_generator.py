import json
import tempfile
import unittest
from pathlib import Path

from mockdata_generator.importer import build_import_plan, load_manifest
from mockdata_generator import ui
from mockdata_generator.catalog import get_collection_specs
from mockdata_generator.cli import main
from mockdata_generator.profiles import resolve_profile_counts
from mockdata_generator.ui import build_generation_targets
from mockdata_generator.world import generate_world
from mockdata_generator.writer import write_dataset

SAMPLE_DATA_DIR = Path(__file__).resolve().parents[2] / "sample_data"
ROUTE_ENABLED_TOURS = {
    "Ha Giang Loop Explorer",
    "Da Nang & Hoi An Discovery",
    "Hue Royal Heritage Journey",
    "Da Lat Pine Forest Weekend",
    "Can Tho Early Market Getaway",
}


class ProfileTests(unittest.TestCase):
    def test_profile_defaults_and_overrides(self) -> None:
        counts = resolve_profile_counts("small", {"Bookings": 5, "chatMessages": 7})

        self.assertEqual(counts["Users"], 4)
        self.assertEqual(counts["Bookings"], 5)
        self.assertEqual(counts["chatMessages"], 7)
        self.assertEqual(counts["TravelArticles"], 6)


class CatalogTests(unittest.TestCase):
    def test_collection_names_match_expected_config_and_fallbacks(self) -> None:
        specs = {spec.type_name: spec for spec in get_collection_specs()}

        self.assertEqual(specs["User"].collection_name, "Users")
        self.assertEqual(specs["SiteSettings"].collection_name, "siteSettings")
        self.assertEqual(specs["ContentSection"].collection_name, "contentSections")
        self.assertEqual(specs["ChatConversation"].collection_name, "chatConversations")
        self.assertEqual(specs["ContactMessage"].collection_name, "ContactMessages")
        self.assertEqual(specs["LoyaltyLedgerEntry"].collection_name, "LoyaltyLedgerEntrys")

    def test_ui_targets_include_run_all_and_collection_targets(self) -> None:
        targets = build_generation_targets()

        self.assertIn("Run all", targets)
        self.assertIn("Payments", targets)
        self.assertIn("chatMessages", targets)
        self.assertEqual(targets["Run all"], [])


class UiTests(unittest.TestCase):
    def test_ui_module_exposes_launch_entrypoint(self) -> None:
        self.assertTrue(callable(ui.launch_ui))


class ImporterTests(unittest.TestCase):
    def test_load_manifest_and_build_import_plan(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = Path(temp_dir)
            manifest_path = output_dir / "manifest.json"
            manifest_path.write_text(
                json.dumps(
                    {
                        "files": ["Users.json", "Tours.json"],
                        "counts": {"Users": 4, "Tours": 10},
                    }
                ),
                encoding="utf-8",
            )

            manifest = load_manifest(output_dir)
            plan = build_import_plan(output_dir, manifest, database_name="HV-Travel", mongo_uri="mongodb://localhost:27017")

            self.assertEqual(manifest["files"][0], "Users.json")
            self.assertEqual(plan[0]["collection"], "Users")
            self.assertTrue(plan[0]["file"].endswith("Users.json"))
            self.assertEqual(plan[0]["database"], "HV-Travel")
            self.assertEqual(plan[0]["uri"], "mongodb://localhost:27017")


class WorldGenerationTests(unittest.TestCase):
    def test_generate_world_produces_related_records(self) -> None:
        world = generate_world(profile="small", seed=42, count_overrides={})

        customers = {item["_id"] for item in world.collections["Customers"]}
        tours = {item["_id"] for item in world.collections["Tours"]}
        bookings = {item["_id"] for item in world.collections["Bookings"]}
        promotions = {item["_id"] for item in world.collections["Promotions"]}
        conversations = {item["_id"] for item in world.collections["chatConversations"]}

        self.assertEqual(world.manifest["profile"], "small")
        self.assertEqual(world.manifest["seed"], 42)
        self.assertEqual(len(world.collections["Users"]), 4)
        self.assertEqual(len(world.collections["TravelArticles"]), 6)
        self.assertTrue(any(item["status"] == "Completed" for item in world.collections["Bookings"]))
        self.assertTrue(any(item["participantType"] == "guest" for item in world.collections["chatConversations"]))
        self.assertTrue(any(item["participantType"] == "customer" for item in world.collections["chatConversations"]))

        for booking in world.collections["Bookings"]:
            self.assertIn(booking["customerId"], customers)
            self.assertIn(booking["tourId"], tours)

        for payment in world.collections["Payments"]:
            self.assertIn(payment["bookingId"], bookings)

        for review in world.collections["Reviews"]:
            self.assertIn(review["bookingId"], bookings)
            self.assertIn(review["customerId"], customers)
            self.assertIn(review["tourId"], tours)

        for wallet_item in world.collections["VoucherWalletItems"]:
            self.assertIn(wallet_item["customerId"], customers)
            self.assertIn(wallet_item["promotionId"], promotions)

        for message in world.collections["chatMessages"]:
            self.assertIn(message["conversationId"], conversations)

        for profile in world.collections["SavedTravellerProfiles"]:
            self.assertIn(profile["customerId"], customers)

        for entry in world.collections["LoyaltyLedgerEntrys"]:
            self.assertIn(entry["bookingId"], bookings)
            self.assertIn(entry["customerId"], customers)

    def test_chat_messages_match_conversation_participants(self) -> None:
        world = generate_world(profile="small", seed=21, count_overrides={})
        conversations = {item["_id"]: item for item in world.collections["chatConversations"]}

        for message in world.collections["chatMessages"]:
            conversation = conversations[message["conversationId"]]
            if conversation["participantType"] == "guest":
                self.assertIn(message["senderType"], {"guest", "staff"})
            else:
                self.assertIn(message["senderType"], {"customer", "staff"})

    def test_content_and_site_settings_have_nested_styles(self) -> None:
        world = generate_world(profile="small", seed=5, count_overrides={})

        section = world.collections["contentSections"][0]
        settings = world.collections["siteSettings"][0]

        self.assertIn("presentation", section)
        self.assertIn("container", section["presentation"])
        self.assertIn("titleText", section["presentation"])
        self.assertIn("style", section["fields"][0])
        self.assertIn("groups", settings)
        self.assertIn("fields", settings["groups"][0])
        self.assertIn("style", settings["groups"][0]["fields"][0])

    def test_travel_articles_always_have_published_at_value(self) -> None:
        world = generate_world(profile="small", seed=17, count_overrides={})

        for article in world.collections["TravelArticles"]:
            self.assertIsNotNone(article["publishedAt"])

    def test_large_profile_smoke_counts(self) -> None:
        world = generate_world(profile="large", seed=11, count_overrides={})

        self.assertEqual(len(world.collections["Bookings"]), 900)
        self.assertEqual(len(world.collections["chatMessages"]), 2200)
        self.assertEqual(world.manifest["relationshipIntegrity"]["paymentsLinkedToBookings"], 650)

    def test_generate_world_for_payments_includes_dependencies_only(self) -> None:
        world = generate_world(profile="small", seed=31, count_overrides={}, selected_collections=["Payments"])

        self.assertGreater(len(world.collections["Customers"]), 0)
        self.assertGreater(len(world.collections["Tours"]), 0)
        self.assertGreater(len(world.collections["Bookings"]), 0)
        self.assertGreater(len(world.collections["Payments"]), 0)
        self.assertEqual(world.collections["Reviews"], [])
        self.assertEqual(world.collections["chatMessages"], [])

    def test_generate_world_for_chat_messages_includes_conversation_dependencies(self) -> None:
        world = generate_world(profile="small", seed=44, count_overrides={}, selected_collections=["chatMessages"])

        self.assertGreater(len(world.collections["Users"]), 0)
        self.assertGreater(len(world.collections["Customers"]), 0)
        self.assertGreater(len(world.collections["chatConversations"]), 0)
        self.assertGreater(len(world.collections["chatMessages"]), 0)
        self.assertEqual(world.collections["Bookings"], [])

    def test_curated_tours_include_routing_and_commerce_fields(self) -> None:
        world = generate_world(profile="small", seed=42, count_overrides={"Tours": 8})

        for tour in world.collections["Tours"]:
            self.assertIn("slug", tour)
            self.assertIn("seo", tour)
            self.assertIn("cancellationPolicy", tour)
            self.assertIn("confirmationType", tour)
            self.assertIn("highlights", tour)
            self.assertIn("meetingPoint", tour)
            self.assertIn("badgeSet", tour)
            self.assertIn("departures", tour)
            self.assertIn("routing", tour)

            self.assertTrue(tour["slug"])
            self.assertTrue(tour["seo"]["title"])
            self.assertTrue(tour["seo"]["canonicalPath"])
            self.assertEqual(tour["confirmationType"], "Instant")
            self.assertGreater(len(tour["departures"]), 0)

            schedule_days = {item["day"] for item in tour["schedule"]}
            if tour["name"] in ROUTE_ENABLED_TOURS:
                self.assertIsNotNone(tour["routing"])
                self.assertEqual(tour["routing"]["schemaVersion"], 1)
                self.assertGreater(len(tour["routing"]["stops"]), 0)

                previous_orders_by_day: dict[int, int] = {}
                for stop in tour["routing"]["stops"]:
                    self.assertIn(stop["day"], schedule_days)
                    self.assertGreater(stop["visitMinutes"], 0)
                    self.assertGreater(stop["attractionScore"], 0)
                    self.assertIn("lat", stop["coordinates"])
                    self.assertIn("lng", stop["coordinates"])

                    previous_order = previous_orders_by_day.get(stop["day"], 0)
                    self.assertGreater(stop["order"], previous_order)
                    previous_orders_by_day[stop["day"]] = stop["order"]
            else:
                self.assertIsNone(tour["routing"])


class OutputTests(unittest.TestCase):
    def _write_world(self, world) -> Path:
        temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(temp_dir.cleanup)
        output_dir = Path(temp_dir.name)
        write_dataset(world, output_dir, pretty=True)
        return output_dir

    def test_writer_creates_collection_files_and_manifest(self) -> None:
        world = generate_world(profile="small", seed=7, count_overrides={"Bookings": 5})

        output_dir = self._write_world(world)

        manifest_path = output_dir / "manifest.json"
        self.assertTrue(manifest_path.exists())

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["counts"]["Bookings"], 5)
        self.assertIn("Users.json", manifest["files"])
        self.assertTrue((output_dir / "Users.json").exists())
        self.assertTrue((output_dir / "chatMessages.json").exists())

    def test_writer_uses_mongo_extended_json_for_collection_files(self) -> None:
        world = generate_world(profile="small", seed=7, count_overrides={"Bookings": 1})

        output_dir = self._write_world(world)

        users = json.loads((output_dir / "Users.json").read_text(encoding="utf-8"))
        bookings = json.loads((output_dir / "Bookings.json").read_text(encoding="utf-8"))

        self.assertIn("$oid", users[0]["_id"])
        self.assertIn("$date", users[0]["createdAt"])
        self.assertIn("$date", bookings[0]["bookingDate"])
        self.assertIn("$date", bookings[0]["tourSnapshot"]["startDate"])

    def test_writer_preserves_generated_business_content(self) -> None:
        world = generate_world(profile="small", seed=7, count_overrides={"Tours": 1, "Bookings": 1})
        output_dir = self._write_world(world)

        exported_tour = json.loads((output_dir / "Tours.json").read_text(encoding="utf-8"))[0]
        exported_booking = json.loads((output_dir / "Bookings.json").read_text(encoding="utf-8"))[0]
        source_tour = world.collections["Tours"][0]
        source_booking = world.collections["Bookings"][0]

        self.assertEqual(exported_tour["name"], source_tour["name"])
        self.assertEqual(exported_tour["shortDescription"], source_tour["shortDescription"])
        self.assertEqual(exported_tour["maxParticipants"], source_tour["maxParticipants"])
        self.assertEqual(exported_tour["rating"], source_tour["rating"])
        self.assertEqual(exported_booking["notes"], source_booking["notes"])
        self.assertEqual(exported_booking["passengers"][0]["fullName"], source_booking["passengers"][0]["fullName"])

    def test_writer_output_avoids_known_placeholder_strings(self) -> None:
        world = generate_world(profile="small", seed=7, count_overrides={})
        output_dir = self._write_world(world)

        placeholder_fragments = (
            "Tour test",
            "Campaign ",
            "Contact request ",
            "Companion ",
            "$mock$",
            "Mock Moderator",
            "<p>123</p>",
        )

        for file_name in ("Users.json", "Tours.json", "Bookings.json", "Promotions.json", "ContactMessages.json", "Reviews.json"):
            payload = (output_dir / file_name).read_text(encoding="utf-8")
            for fragment in placeholder_fragments:
                self.assertNotIn(fragment, payload, f"{file_name} unexpectedly contains placeholder fragment {fragment!r}")

    def test_tours_output_matches_sample_contract(self) -> None:
        sample = json.loads((SAMPLE_DATA_DIR / "HV-Travel.Tours.json").read_text(encoding="utf-8"))[0]
        world = generate_world(profile="small", seed=7, count_overrides={"Tours": 1})

        output_dir = self._write_world(world)
        generated = json.loads((output_dir / "Tours.json").read_text(encoding="utf-8"))[0]

        self.assertEqual(list(generated.keys()), list(sample.keys()))
        self.assertEqual(list(generated["price"].keys()), list(sample["price"].keys()))
        self.assertEqual(list(generated["duration"].keys()), list(sample["duration"].keys()))
        self.assertEqual(list(generated["schedule"][0].keys()), list(sample["schedule"][0].keys()))
        self.assertEqual(list(generated["seo"].keys()), list(sample["seo"].keys()))
        self.assertEqual(list(generated["cancellationPolicy"].keys()), list(sample["cancellationPolicy"].keys()))
        self.assertEqual(list(generated["departures"][0].keys()), list(sample["departures"][0].keys()))
        self.assertEqual(list(generated["routing"].keys()), list(sample["routing"].keys()))
        self.assertEqual(list(generated["routing"]["stops"][0].keys()), list(sample["routing"]["stops"][0].keys()))
        self.assertEqual(
            list(generated["routing"]["stops"][0]["coordinates"].keys()),
            list(sample["routing"]["stops"][0]["coordinates"].keys()),
        )
        self.assertIn("$oid", generated["_id"])
        self.assertIn("$numberDecimal", generated["price"]["adult"])
        self.assertIn("$numberDecimal", generated["price"]["child"])
        self.assertIn("$numberDecimal", generated["price"]["infant"])
        self.assertIn("$date", generated["startDates"][0])

    def test_tours_output_preserves_null_routing_for_non_curated_templates(self) -> None:
        world = generate_world(profile="small", seed=7, count_overrides={"Tours": 8})

        output_dir = self._write_world(world)
        generated = json.loads((output_dir / "Tours.json").read_text(encoding="utf-8"))
        by_name = {item["name"]: item for item in generated}

        self.assertIsNone(by_name["Ha Long Bay Heritage Cruise"]["routing"])
        self.assertIsNone(by_name["Phu Quoc Family Beach Escape"]["routing"])
        self.assertIsNone(by_name["Nha Trang Island Leisure Break"]["routing"])

    def test_customers_output_matches_sample_contract(self) -> None:
        sample = json.loads((SAMPLE_DATA_DIR / "HV-Travel.Customers.json").read_text(encoding="utf-8"))[0]
        world = generate_world(profile="small", seed=7, count_overrides={"Customers": 1})

        output_dir = self._write_world(world)
        generated = json.loads((output_dir / "Customers.json").read_text(encoding="utf-8"))[0]

        self.assertEqual(list(generated.keys()), list(sample.keys()))
        self.assertEqual(list(generated["address"].keys()), list(sample["address"].keys()))
        self.assertEqual(list(generated["stats"].keys()), list(sample["stats"].keys()))
        self.assertIn("$oid", generated["_id"])
        self.assertIn("$date", generated["createdAt"])
        self.assertIn("$date", generated["stats"]["lastActivity"])

    def test_bookings_output_matches_sample_contract(self) -> None:
        sample = json.loads((SAMPLE_DATA_DIR / "HV-Travel.Bookings.json").read_text(encoding="utf-8"))[0]
        world = generate_world(profile="small", seed=7, count_overrides={"Bookings": 1})

        output_dir = self._write_world(world)
        generated = json.loads((output_dir / "Bookings.json").read_text(encoding="utf-8"))[0]

        self.assertEqual(list(generated.keys()), list(sample.keys()))
        self.assertEqual(list(generated["tourSnapshot"].keys()), list(sample["tourSnapshot"].keys()))
        self.assertEqual(list(generated["passengers"][0].keys()), list(sample["passengers"][0].keys()))
        self.assertEqual(list(generated["historyLog"][0].keys()), list(sample["historyLog"][0].keys()))
        self.assertEqual(list(generated["events"][0].keys()), list(sample["events"][0].keys()))
        self.assertEqual(list(generated["paymentTransactions"][0].keys()), list(sample["paymentTransactions"][0].keys()))
        self.assertIn("$oid", generated["_id"])
        self.assertIn("$oid", generated["tourId"])
        self.assertIn("$numberDecimal", generated["totalAmount"])
        self.assertIn("$date", generated["bookingDate"])
        self.assertIn("$date", generated["tourSnapshot"]["startDate"])
        self.assertRegex(generated["bookingCode"], r"^HV\d{17}$")


class CliTests(unittest.TestCase):
    def test_cli_generate_command_writes_output(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            exit_code = main(
                [
                    "generate",
                    "--profile",
                    "small",
                    "--seed",
                    "99",
                    "--out",
                    temp_dir,
                    "--count",
                    "Bookings=4",
                ]
            )

            self.assertEqual(exit_code, 0)
            manifest = json.loads(Path(temp_dir, "manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(manifest["counts"]["Bookings"], 4)
            self.assertEqual(manifest["seed"], 99)

    def test_cli_ui_command_returns_zero(self) -> None:
        exit_code = main(["ui", "--dry-run"])

        self.assertEqual(exit_code, 0)


if __name__ == "__main__":
    unittest.main()

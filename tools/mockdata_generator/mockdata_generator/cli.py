from __future__ import annotations

import argparse
from pathlib import Path

from mockdata_generator.profiles import PROFILE_DEFAULTS
from mockdata_generator.ui import launch_ui
from mockdata_generator.writer import clean_output_directory, write_dataset
from mockdata_generator.world import generate_world


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="mockdata_generator")
    subparsers = parser.add_subparsers(dest="command", required=True)
    generate_parser = subparsers.add_parser("generate", help="Generate related JSON mockdata.")
    generate_parser.add_argument("--profile", choices=tuple(PROFILE_DEFAULTS), default="medium")
    generate_parser.add_argument("--out", type=Path, default=None)
    generate_parser.add_argument("--seed", type=int, default=42)
    generate_parser.add_argument("--count", action="append", default=[], metavar="COLLECTION=VALUE")
    generate_parser.add_argument("--pretty", action="store_true")
    generate_parser.add_argument("--clean-output", action="store_true")
    ui_parser = subparsers.add_parser("ui", help="Launch desktop UI.")
    ui_parser.add_argument("--dry-run", action="store_true")
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if args.command == "generate":
        overrides = _parse_count_overrides(args.count)
        output_dir = args.out or _default_output_dir(args.profile)
        world = generate_world(profile=args.profile, seed=args.seed, count_overrides=overrides)
        if args.clean_output:
            clean_output_directory(output_dir)
        write_dataset(world, output_dir, pretty=args.pretty)
        return 0
    if args.command == "ui":
        if args.dry_run:
            return 0
        launch_ui()
        return 0
    parser.error(f"Unsupported command '{args.command}'.")
    return 2


def entrypoint() -> int:
    return main()


def _parse_count_overrides(raw_values: list[str]) -> dict[str, int]:
    overrides: dict[str, int] = {}
    for raw_value in raw_values:
        if "=" not in raw_value:
            raise ValueError(f"Invalid --count value '{raw_value}'. Expected COLLECTION=VALUE.")
        key, value = raw_value.split("=", 1)
        overrides[key.strip()] = int(value.strip())
    return overrides


def _default_output_dir(profile: str) -> Path:
    tool_root = Path(__file__).resolve().parent.parent
    return tool_root / "output" / profile

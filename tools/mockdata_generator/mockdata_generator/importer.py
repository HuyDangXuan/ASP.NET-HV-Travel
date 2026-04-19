from __future__ import annotations

import json
from pathlib import Path


def load_manifest(output_dir: Path) -> dict:
    manifest_path = output_dir / "manifest.json"
    return json.loads(manifest_path.read_text(encoding="utf-8"))


def build_import_plan(output_dir: Path, manifest: dict, *, database_name: str, mongo_uri: str) -> list[dict]:
    plan: list[dict] = []
    for file_name in manifest.get("files", []):
        if file_name == "manifest.json":
            continue
        collection_name = Path(file_name).stem
        plan.append(
            {
                "collection": collection_name,
                "file": str((output_dir / file_name).resolve()),
                "database": database_name,
                "uri": mongo_uri,
            }
        )
    return plan

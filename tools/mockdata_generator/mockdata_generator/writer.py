from __future__ import annotations

import json
import re
import shutil
from datetime import datetime, timezone
from pathlib import Path

from mockdata_generator.sample_contract import is_wrapper_dict, prepare_collections_for_export
from mockdata_generator.world import World

_OBJECT_ID_RE = re.compile(r"^[0-9a-f]{24}$")
_ISO_UTC_RE = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")


def clean_output_directory(output_dir: Path) -> None:
    if not output_dir.exists():
        return
    for child in output_dir.iterdir():
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()


def write_dataset(world: World, output_dir: Path, *, pretty: bool = False) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    files: list[str] = []
    export_collections = prepare_collections_for_export(world.collections)
    for collection_name, items in export_collections.items():
        file_name = f"{collection_name}.json"
        mongo_payload = _to_mongo_extended_json(items)
        (output_dir / file_name).write_text(_serialize(mongo_payload, pretty=pretty), encoding="utf-8")
        files.append(file_name)

    manifest = dict(world.manifest)
    manifest["files"] = files
    manifest["generatedAt"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    (output_dir / "manifest.json").write_text(_serialize(manifest, pretty=True), encoding="utf-8")


def _serialize(payload, *, pretty: bool) -> str:
    if pretty:
        return json.dumps(payload, indent=2, ensure_ascii=False) + "\n"
    return json.dumps(payload, separators=(",", ":"), ensure_ascii=False)


def _to_mongo_extended_json(value, key: str | None = None):
    if isinstance(value, list):
        return [_to_mongo_extended_json(item, key=key) for item in value]
    if isinstance(value, dict):
        if is_wrapper_dict(value):
            return value
        return {child_key: _to_mongo_extended_json(child_value, key=child_key) for child_key, child_value in value.items()}
    if isinstance(value, str):
        if key == "_id" and _OBJECT_ID_RE.fullmatch(value):
            return {"$oid": value}
        if key not in {"$date", "$oid", "$numberDecimal"} and _ISO_UTC_RE.fullmatch(value):
            return {"$date": value}
    return value

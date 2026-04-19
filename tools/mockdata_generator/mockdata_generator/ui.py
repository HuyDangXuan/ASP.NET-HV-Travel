from __future__ import annotations

import sys
import threading
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, ttk

if __package__ in {None, ""}:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from mockdata_generator.catalog import get_collection_specs
from mockdata_generator.writer import clean_output_directory, write_dataset
from mockdata_generator.world import generate_world


def build_generation_targets() -> dict[str, list[str]]:
    targets = {"Run all": []}
    for spec in get_collection_specs():
        targets[spec.collection_name] = [spec.collection_name]
    return targets


def launch_ui() -> None:
    root = tk.Tk()
    root.title("HV Travel Mockdata Generator")
    root.geometry("920x680")

    targets = build_generation_targets()
    profile_var = tk.StringVar(value="medium")
    seed_var = tk.StringVar(value="42")
    output_var = tk.StringVar(value=str(_default_output_dir("medium")))
    pretty_var = tk.BooleanVar(value=True)
    clean_var = tk.BooleanVar(value=True)
    status_var = tk.StringVar(value="Ready")

    root.columnconfigure(0, weight=1)
    root.rowconfigure(2, weight=1)

    header = ttk.Frame(root, padding=16)
    header.grid(row=0, column=0, sticky="ew")
    header.columnconfigure(3, weight=1)

    ttk.Label(header, text="Profile").grid(row=0, column=0, sticky="w")
    ttk.Combobox(header, textvariable=profile_var, values=["small", "medium", "large"], state="readonly", width=10).grid(row=0, column=1, padx=(8, 16), sticky="w")
    ttk.Label(header, text="Seed").grid(row=0, column=2, sticky="w")
    ttk.Entry(header, textvariable=seed_var, width=12).grid(row=0, column=3, sticky="w")

    ttk.Label(header, text="Output").grid(row=1, column=0, sticky="w", pady=(12, 0))
    ttk.Entry(header, textvariable=output_var).grid(row=1, column=1, columnspan=3, sticky="ew", pady=(12, 0))
    ttk.Button(header, text="Browse", command=lambda: _pick_output(output_var)).grid(row=1, column=4, padx=(8, 0), pady=(12, 0))

    options = ttk.Frame(root, padding=(16, 0, 16, 12))
    options.grid(row=1, column=0, sticky="ew")
    ttk.Checkbutton(options, text="Pretty JSON", variable=pretty_var).pack(side="left")
    ttk.Checkbutton(options, text="Clean output first", variable=clean_var).pack(side="left", padx=(12, 0))
    ttk.Label(options, textvariable=status_var).pack(side="right")

    body = ttk.Frame(root, padding=(16, 0, 16, 16))
    body.grid(row=2, column=0, sticky="nsew")
    body.columnconfigure(0, weight=0)
    body.columnconfigure(1, weight=1)
    body.rowconfigure(1, weight=1)

    ttk.Label(body, text="Generate mockdata").grid(row=0, column=0, sticky="w", pady=(0, 8))

    button_frame = ttk.Frame(body)
    button_frame.grid(row=1, column=0, sticky="ns")

    log = tk.Text(body, wrap="word", height=24)
    log.grid(row=1, column=1, sticky="nsew", padx=(16, 0))

    for index, (label, selected) in enumerate(targets.items()):
        ttk.Button(
            button_frame,
            text=label,
            width=24,
            command=lambda selected=selected, label=label: _run_generation(
                root,
                log,
                status_var,
                label,
                selected,
                profile_var.get,
                seed_var.get,
                output_var.get,
                pretty_var.get,
                clean_var.get,
            ),
        ).grid(row=index, column=0, sticky="ew", pady=4)

    root.mainloop()


def _run_generation(root, log, status_var, label, selected, profile_getter, seed_getter, output_getter, pretty_getter, clean_getter) -> None:
    def worker() -> None:
        try:
            profile = profile_getter()
            seed = int(seed_getter())
            output_dir = Path(output_getter())
            root.after(0, lambda: status_var.set(f"Running {label}..."))
            world = generate_world(profile=profile, seed=seed, count_overrides={}, selected_collections=selected)
            if clean_getter():
                clean_output_directory(output_dir)
            write_dataset(world, output_dir, pretty=pretty_getter())
            root.after(0, lambda: _append_log(log, f"[OK] {label} -> {output_dir}"))
            root.after(0, lambda: status_var.set(f"Completed {label}"))
        except Exception as exc:  # pragma: no cover - UI side path
            root.after(0, lambda: _append_log(log, f"[ERROR] {label}: {exc}"))
            root.after(0, lambda: status_var.set(f"Failed {label}"))

    threading.Thread(target=worker, daemon=True).start()


def _append_log(log, message: str) -> None:
    log.insert("end", message + "\n")
    log.see("end")


def _pick_output(output_var: tk.StringVar) -> None:
    selected = filedialog.askdirectory()
    if selected:
        output_var.set(selected)


def _default_output_dir(profile: str) -> Path:
    tool_root = Path(__file__).resolve().parent.parent
    return tool_root / "output" / profile


if __name__ == "__main__":
    launch_ui()

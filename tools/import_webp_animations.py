#!/usr/bin/env python3
r"""Convert user-provided animated WebP files into PNG + timing sequences.

Requires: python -m pip install Pillow
Usage: python tools/import_webp_animations.py --input "C:\path\to\webps"
The default output is %LOCALAPPDATA%\CapriDesktop\character on Windows.
"""
import argparse
import json
import os
from pathlib import Path

from PIL import Image

STATES = ("idle", "talk", "glitch", "tossed", "sleep", "perch", "perch-music")


def convert_file(source: Path, destination: Path) -> tuple[int, int]:
    destination.mkdir(parents=True, exist_ok=True)
    delays: list[int | None] = []
    with Image.open(source) as image:
        frames = getattr(image, "n_frames", 1)
        for index in range(frames):
            image.seek(index)
            duration = image.info.get("duration")
            delays.append(int(duration) if isinstance(duration, (int, float)) and duration >= 20 else None)
            image.convert("RGBA").save(destination / f"{index:04d}.png", format="PNG", compress_level=6)

    fallback = next((value for value in delays if value is not None), 100)
    durations = [value if value is not None else fallback for value in delays]
    (destination / "timing.json").write_text(
        json.dumps({"frameDurationsMs": durations}, separators=(",", ":")) + "\n", encoding="utf-8"
    )
    return frames, sum(durations)


def main() -> None:
    default_target = Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "CapriDesktop" / "character"
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path, help="Folder containing the animated WebP files")
    parser.add_argument("--output", type=Path, default=default_target, help="Target character folder")
    args = parser.parse_args()

    total = 0
    for state in STATES:
        source = args.input / f"{state}.webp"
        if not source.is_file():
            print(f"SKIP  {state}.webp (missing)")
            continue
        count, milliseconds = convert_file(source, args.output / state)
        total += count
        print(f"READY {state}: {count} frames / {milliseconds / 1000:.2f}s")
    if total == 0:
        raise SystemExit("No matching animated WebP files found.")
    print(f"Imported {total} frames. Choose Reload character art in Capri Desktop.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICONSET = ROOT / "ios" / "App" / "App" / "Assets.xcassets" / "AppIcon.appiconset"
CONTENTS = ICONSET / "Contents.json"


def main() -> int:
    if not CONTENTS.exists():
        print(f"ERROR: Contents.json not found: {CONTENTS}")
        return 1

    data = json.loads(CONTENTS.read_text(encoding="utf-8"))
    images = data.get("images", [])
    required = [img.get("filename") for img in images if img.get("filename")]

    missing = [name for name in required if not (ICONSET / name).exists()]
    if missing:
        print("MISSING ICON FILES:")
        for name in missing:
            print(f"- {name}")
        return 2

    print(f"OK: {len(required)} / {len(required)} iOS app icon files are present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

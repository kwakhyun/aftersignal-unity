"""Remove orphaned generated Unity meshes without touching authored art or audio.

Default: write a dry-run report. Pass --apply to remove only meshes whose GUIDs
have no incoming references in Assets or ProjectSettings, together with metadata.
"""
import argparse
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
GEOMETRY = ROOT / "Assets/AfterSignal/Resources/Geometry"
GUID = re.compile(rb"guid:\s*([0-9a-f]{32})")
SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".meta", ".controller",
              ".overridecontroller", ".playable", ".lighting", ".fontsettings",
              ".rendertexture", ".shadervariants"}
GENERATED = re.compile(r"(?:.+_batch_\d+|CityLife-(?:roof|town)-\d+-\d+)\.asset$")


def checked(path):
    resolved = path.resolve()
    if not resolved.is_relative_to(GEOMETRY.resolve()) or path.is_symlink():
        raise ValueError(f"Generated asset is outside its allowed directory: {path}")
    return resolved


def audit():
    candidates = {}
    for path in GEOMETRY.glob("*.asset"):
        if not GENERATED.fullmatch(path.name):
            continue
        meta = path.with_name(path.name + ".meta")
        match = GUID.search(meta.read_bytes()) if meta.exists() else None
        if match:
            candidates[match[1]] = (path, meta)
    referenced = set()
    for directory in (ROOT / "Assets", ROOT / "ProjectSettings"):
        for path in directory.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in SERIALIZED:
                continue
            data = path.read_bytes()
            for guid in GUID.findall(data):
                pair = candidates.get(guid)
                if pair and path != pair[1]:
                    referenced.add(guid)
    return sorted((pair for guid, pair in candidates.items() if guid not in referenced),
                  key=lambda pair: pair[0].name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    unused = audit()
    report = {"applied": args.apply, "assets": len(unused), "files": len(unused) * 2,
              "bytes": sum(p.stat().st_size for pair in unused for p in pair),
              "removed": [str(p.relative_to(ROOT)).replace("\\", "/")
                          for pair in unused for p in pair]}
    # Validate the entire list before mutating any file.
    for pair in unused:
        for path in pair:
            checked(path)
    if args.apply:
        for pair in unused:
            for path in pair:
                path.unlink()
    output = ROOT / "Artifacts/Maintenance"
    output.mkdir(parents=True, exist_ok=True)
    destination = output / ("cleanup-applied.json" if args.apply else "cleanup-audit.json")
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "removed"}))
    print(destination)


if __name__ == "__main__":
    main()

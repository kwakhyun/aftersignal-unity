"""Project-local ArtCraft export and inventory. No account or cloud calls.

Keep original artwork in Artifacts/ArtCraft; export reviewed game assets to
Assets/AfterSignal/Resources/Art/ArtCraft. Source art is never overwritten.
"""
import argparse
import hashlib
import json
import shutil
import struct
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRIVATE = ROOT / "Artifacts/ArtCraft"
VERSIONS = {"photocraft": "0.9.0", "vectorcraft": "0.11.0"}
VECTOR_EXTENSIONS = {".svg", ".svgz", ".vectorcraft", ".drawcraft"}


def executable(tool):
    version = VERSIONS[tool]
    folder = f"{tool}-{version}"
    path = ROOT / "Artifacts/Tools/ArtCraft" / folder / f"{folder}-windows-x64-portable" / f"{tool}-cli.exe"
    if not path.is_file():
        raise ValueError("Run Tools/ArtCraft/Install-ArtCraft.ps1 first.")
    return str(path)


def command(tool, *args):
    result = subprocess.run([executable(tool), *map(str, args)], capture_output=True, text=True,
                            encoding="utf-8", errors="replace", timeout=180)
    if result.returncode:
        raise RuntimeError(f"{tool}: {(result.stderr or result.stdout).strip()}")
    return result.stdout.strip()


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def png_size(path):
    with path.open("rb") as stream:
        header = stream.read(24)
    if len(header) < 24 or header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise ValueError(f"Invalid PNG: {path}")
    return struct.unpack(">II", header[16:24])


def export_asset(source, output, max_size=None, nearest=False, force=False):
    source, output = source.resolve(strict=True), output.resolve()
    allowed = (PRIVATE.resolve(), (ROOT / "Assets/AfterSignal/Resources/Art/ArtCraft").resolve())
    if not any(output.is_relative_to(folder) for folder in allowed):
        raise ValueError("Export only to Artifacts/ArtCraft or Resources/Art/ArtCraft.")
    if source == output or output.suffix.lower() != ".png":
        raise ValueError("Export requires a separate .png output, preserving source artwork.")
    if output.exists() and not force:
        raise ValueError("Output already exists; use --force to back it up and replace it.")
    output.parent.mkdir(parents=True, exist_ok=True)
    pending = output.with_name(output.stem + ".pending.png")
    resized = output.with_name(output.stem + ".resized.png")
    if source in (pending, resized):
        raise ValueError("Source cannot use a reserved temporary export path.")
    tool = "vectorcraft" if source.suffix.lower() in VECTOR_EXTENSIONS else "photocraft"
    try:
        result = command(tool, "convert", source, pending)
        width, height = png_size(pending)
        if max_size and max(width, height) > max_size:
            ratio = max_size / max(width, height)
            params = {"width": max(1, round(width * ratio)), "height": max(1, round(height * ratio)),
                      "resample": "nearest" if nearest else "lanczos"}
            result += "\n" + command("photocraft", "run", pending, "--cmd", "image.imageSize",
                                      "--params", json.dumps(params), "--out", resized)
            png_size(resized)
            resized.replace(pending)
        width, height = png_size(pending)
        if output.exists():
            backups = PRIVATE / "Backups"
            backups.mkdir(parents=True, exist_ok=True)
            backup = backups / f"{output.stem}-{digest(output)[:16]}.png"
            if not backup.exists():
                shutil.copy2(output, backup)
        pending.replace(output)
    finally:
        for temporary in (pending, resized):
            if temporary.exists():
                temporary.unlink()
    report = {"created_utc": datetime.now(timezone.utc).isoformat(), "source": str(source),
              "source_sha256": digest(source), "output": str(output), "output_sha256": digest(output),
              "size": [width, height], "tools": VERSIONS, "engine_output": result,
              "cloud_requests": 0, "visual_approval": False}
    reports = PRIVATE / "Exports"
    reports.mkdir(parents=True, exist_ok=True)
    report_path = reports / (output.stem + "-" + report["output_sha256"][:12] + ".json")
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "size": [width, height], "report": str(report_path)}, ensure_ascii=False))


def audit():
    """Metadata inventory only: disk size is not live Unity VRAM or measured FPS."""
    rows = []
    for path in sorted((ROOT / "Assets/AfterSignal/Resources").rglob("*.png")):
        width, height = png_size(path)
        meta = path.with_suffix(path.suffix + ".meta")
        metadata = meta.read_text(encoding="utf-8") if meta.exists() else ""
        rows.append({"path": path.relative_to(ROOT).as_posix(), "size": [width, height],
                     "disk_bytes": path.stat().st_size, "source_rgba_bytes": width * height * 4,
                     "readable": "  isReadable: 1" in metadata})
    PRIVATE.mkdir(parents=True, exist_ok=True)
    report = PRIVATE / "asset-inventory.json"
    report.write_text(json.dumps({"note": "Source inventory; NOT loaded VRAM or FPS. Do not resize sprite sheets without updating their slicing metadata.",
                                  "textures": rows}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"textures": len(rows), "report": str(report)}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="action", required=True)
    sub.add_parser("doctor")
    sub.add_parser("audit")
    export = sub.add_parser("export")
    export.add_argument("source", type=Path)
    export.add_argument("output", type=Path)
    export.add_argument("--max-size", type=int)
    export.add_argument("--nearest", action="store_true", help="Only for intentionally pixel-art assets.")
    export.add_argument("--force", action="store_true")
    args = parser.parse_args()
    if args.action == "doctor":
        for tool in VERSIONS:
            command(tool, "--help")
            print(f"{tool} {VERSIONS[tool]}: ready")
    elif args.action == "audit":
        audit()
    else:
        if args.max_size is not None and args.max_size < 1:
            parser.error("--max-size must be positive")
        export_asset(args.source, args.output, args.max_size, args.nearest, args.force)


if __name__ == "__main__":
    main()

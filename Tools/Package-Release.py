"""Package the self-contained Unity project and Windows player; verify every ZIP entry."""
import hashlib
import json
import pathlib
import sys
import zipfile

project = pathlib.Path(__file__).resolve().parents[1]
verification=(project/'Documentation/Expansion/VERIFICATION.md').read_text(encoding='utf-8')
assert '<!-- FINAL_' not in verification,'Finish the native verification report before packaging.'
residence=(project/'Documentation/Residence/VERIFICATION.md').read_text(encoding='utf-8')
assert '<!-- FINAL_' not in residence,'Finish current release measurements before packaging.'
current=json.loads((project/'Documentation/Residence/verification.json').read_text(encoding='utf-8'))
assert all(v.get('completed',v.get('failed')==0) for v in current.values()),'Current verification must pass.'
urban=json.loads((project/'Documentation/Urban/verification.json').read_text(encoding='utf-8'))
assert all(v.get('completed',False) for v in urban.values()),'Native city, interiors and regression verification must pass.'
destination = pathlib.Path(sys.argv[1]).resolve()
if destination.exists():
    raise SystemExit(f"Refusing to overwrite existing release: {destination}")
roots = [project / name for name in ("Assets", "Packages", "ProjectSettings", "Tools", "Documentation", "Builds")]
files = [file for root in roots for file in root.rglob("*") if file.is_file() and not any(part == ".python-deps" or "DoNotShip" in part or part == "__pycache__" for part in file.parts)]
files.extend(file for file in project.iterdir() if file.is_file() and (file.suffix in (".cmd", ".md") or file.name == ".gitignore"))
files = sorted(files)
destination.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED, compresslevel=5, allowZip64=True) as archive:
    for file in files:
        archive.write(file, "AFTERSIGNAL-Unity/" + file.relative_to(project).as_posix())

with zipfile.ZipFile(destination) as archive:
    assert len(archive.namelist()) == len(set(archive.namelist())) == len(files)
    for file in files:
        name = "AFTERSIGNAL-Unity/" + file.relative_to(project).as_posix()
        assert not pathlib.PurePosixPath(name).is_absolute() and ".." not in pathlib.PurePosixPath(name).parts
        assert hashlib.sha256(archive.read(name)).digest() == hashlib.sha256(file.read_bytes()).digest(), name
    required = ["PLAY.cmd", "OPEN_UNITY.cmd", "Packages/manifest.json", "ProjectSettings/ProjectVersion.txt", "Assets/AfterSignal/Runtime/Player/PlayerMotor.cs", "Assets/AfterSignal/Runtime/Player/RopeMotor.cs", "Assets/AfterSignal/Scenes/01_NeonStation.unity", "Builds/Windows/AFTERSIGNAL.exe", "Documentation/VERIFICATION.md"]
    assert all("AFTERSIGNAL-Unity/" + name in archive.namelist() for name in required)
    assert all("AFTERSIGNAL-Unity/"+name in archive.namelist() for name in ["Assets/AfterSignal/Scenes/17_Origin.unity","Assets/AfterSignal/Scenes/18_Harbor.unity","Assets/AfterSignal/Runtime/Campaign/CampaignCatalog.cs","Documentation/Expansion/VERIFICATION.md","Documentation/Expansion/verification.json"])
    assert all("AFTERSIGNAL-Unity/"+name in archive.namelist() for name in ["Assets/AfterSignal/Scenes/19_SeoResidence.unity","Assets/AfterSignal/Scenes/23_FacadeBreach.unity","Assets/AfterSignal/Shaders/PixelActor.shader","Documentation/Residence/review.html","Documentation/Residence/verification.json"])
    assert all("AFTERSIGNAL-Unity/"+name in archive.namelist() for name in ["Assets/AfterSignal/Scenes/24_OpenCity.unity","Assets/AfterSignal/Scenes/25_CityInterior.unity","Assets/AfterSignal/Prefabs/CityTaxi.prefab","Assets/AfterSignal/Prefabs/CityBus.prefab","Assets/AfterSignal/Prefabs/CityTruck.prefab","Assets/AfterSignal/Runtime/City/Traffic/CityRoadNetwork.cs","Documentation/Urban/verification.json","Documentation/Urban/review.html"])

report = {"file": str(destination), "bytes": destination.stat().st_size, "files": len(files), "sha256": hashlib.file_digest(destination.open("rb"), "sha256").hexdigest(), "everyEntryVerified": True}
destination.with_suffix(".manifest.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2))

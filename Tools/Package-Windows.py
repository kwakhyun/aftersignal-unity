"""Package the verified release player and its notices; validate each archived byte."""
from pathlib import Path
import zipfile, sys, hashlib, json
root=Path(__file__).resolve().parents[1];source=root/'Builds/Windows';dest=Path(sys.argv[1]).resolve()
assert not dest.exists(),'Preserve existing packages; use a new version name.'
files=[p for p in source.rglob('*') if p.is_file() and not any('DoNotShip' in part for part in p.parts)]
evidence=json.loads((root/'Documentation/Urban/verification.json').read_text(encoding='utf-8'))
assert all(item.get('completed',False) for item in evidence.values()),'Finish native release verification first.'
extras={name:(root/path).read_bytes() for name,path in {
    'README_KO.md':'Documentation/Urban/URBAN-15.md',
    'VERIFICATION.md':'Documentation/Urban/VERIFICATION.md',
    'NotoSansKR-LICENSE.txt':'Documentation/NotoSansKR-LICENSE.txt',
    'ART_SOURCES.json':'Documentation/Urban/sources.json'
}.items()}
notes="AFTERSIGNAL Night Line 1.5\n\nRun AFTERSIGNAL.exe. Windows x64 / Unity 6000.4.0f1 / URP 17.4.0.\nWASD move, LMB attack, RMB hold grapple, Space jump, Shift dash, Ctrl guard, Q weapon skill, R reload, E interact, wheel or 1/2/3 weapon, Esc menu.\nCity: E enter/take a car; W/S accelerate/reverse, A/D steer, Space brake. E exit when stopped; F exits at a fuel pump. E starts/stops fueling while stationary at a station. M opens the city map. Main quest guidance stays visible.\n\n25 scenes. Begin in Seoha's apartment on floor 6. Open the door and use the elevator or emergency stairs to reach Afterlight. The east town exit leads to the 790 by 660 metre city: forty facility entrances with sixteen interior themes, dense high-rises, coordinated signals, pedestrians, sedan/taxi/bus/truck traffic, fuel, collision damage and wrecks. The first rail investigation and the following restoration chapters remain available through headquarters and Noa. Existing campaign saves retain progress.\n\nOffline game. 60 FPS cap / D3D11 BitBlt. Measured on Ryzen 5 7500F + RTX 4060 Ti at 1600x900. See the source project's Documentation/Urban/VERIFICATION.md for actual run results and limits. This is not a minimum-device certification or a claim of commercial-reference art parity.\n"
with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
    for p in files:z.write(p,'AFTERSIGNAL-Windows/'+p.relative_to(source).as_posix())
    z.writestr('AFTERSIGNAL-Windows/READ_ME.txt',notes)
    for name,data in extras.items():z.writestr('AFTERSIGNAL-Windows/'+name,data)
with zipfile.ZipFile(dest) as z:
    assert len(z.namelist())==len(set(z.namelist()))==len(files)+1+len(extras)
    for p in files:assert hashlib.sha256(z.read('AFTERSIGNAL-Windows/'+p.relative_to(source).as_posix())).digest()==hashlib.sha256(p.read_bytes()).digest()
    for name,data in extras.items():assert z.read('AFTERSIGNAL-Windows/'+name)==data
    assert z.read('AFTERSIGNAL-Windows/READ_ME.txt').decode('utf-8')==notes
manifest={'file':str(dest),'bytes':dest.stat().st_size,'files':len(files)+1+len(extras),'sha256':hashlib.file_digest(dest.open('rb'),'sha256').hexdigest(),'everyEntryVerified':True}
dest.with_suffix('.manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8');print(json.dumps(manifest,indent=2))

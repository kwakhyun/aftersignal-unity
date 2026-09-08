"""Read-only audit of this expansion's untracked generated meshes after batching."""
from pathlib import Path
import json,re,subprocess
root=Path(__file__).resolve().parents[2]
folder=root/'Assets/AfterSignal/Resources/WorldAssets/Generated'
tracked=set(subprocess.check_output(['git','ls-files'],cwd=root,text=True).splitlines())
refs=set()
for prefab in (root/'Assets/AfterSignal/Resources/WorldAssets').glob('*.prefab'):
 refs.update(re.findall(r'guid: ([a-f0-9]{32})',prefab.read_text(encoding='utf-8-sig')))
unused=[]
for mesh in folder.glob('Mesh*.asset'):
 if int(mesh.stem[4:])<18000 or mesh.relative_to(root).as_posix() in tracked:continue
 meta=mesh.with_suffix('.asset.meta')
 if not meta.exists():continue
 guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
 if guid not in refs:unused.append(dict(path=str(mesh.resolve()),meta=str(meta.resolve()),bytes=mesh.stat().st_size))
(root/'Artifacts/neon-unused-meshes.json').write_text(json.dumps(unused,indent=2),encoding='utf-8')
print(json.dumps(dict(unused=len(unused),megabytes=round(sum(x['bytes'] for x in unused)/1024/1024,2))))

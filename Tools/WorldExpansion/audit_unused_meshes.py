"""Read-only GUID audit; the JSON report is reviewed before deleting generated assets."""
from pathlib import Path
import re,json,subprocess
root=Path(__file__).resolve().parents[2]
assets=root/'Assets'; generated=assets/'AfterSignal/Resources/WorldAssets/Generated'
command=['rg','--no-heading','--no-line-number','--no-filename','-o','guid: [a-f0-9]{32}','Assets']
for pattern in ['*.unity','*.prefab','*.asset','*.mat','*.controller','*.overrideController','*.playable','*.anim','*.meta','!**/Generated/Mesh*.asset','!**/Generated/Mesh*.asset.meta']:command.extend(['-g',pattern])
scan=subprocess.run(command,cwd=root,capture_output=True,check=True)
refs=set(re.findall(rb'guid: ([a-f0-9]{32})',scan.stdout))
scan=subprocess.run(['rg','--no-heading','--no-line-number','-o','guid: [a-f0-9]{32}',str(generated),'-g','Mesh*.asset.meta'],cwd=root,capture_output=True,check=True)
unused=[];size=0
for line in scan.stdout.splitlines():
    raw,guid=line.rsplit(b':guid: ',1);meta=Path(raw.decode());p=meta.with_suffix('')
    if guid not in refs:
        unused.extend([str(p),str(meta)]);size+=p.stat().st_size+meta.stat().st_size
out=root/'Artifacts/unused-generated-meshes.json'
out.write_text(json.dumps({'meshes':len(unused)//2,'bytes':size,'files':unused},indent=2),encoding='utf-8')
print(json.dumps({'meshes':len(unused)//2,'bytes':size,'report':str(out)}))

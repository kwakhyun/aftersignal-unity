from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[1]
baseline=root.parent/'QA-Baseline';baseline.mkdir(parents=True,exist_ok=True)
assert not (baseline/'Assets').exists(),'Baseline already prepared; preserve it.'
backup=root/'Artifacts/Quality/Backup'
shutil.copytree(root/'Packages',baseline/'Packages')
shutil.copytree(backup/'ProjectSettings',baseline/'ProjectSettings')
shutil.copytree(backup/'Settings',baseline/'Assets/Settings')
shutil.copytree(backup/'AfterSignal',baseline/'Assets/AfterSignal')
current=root/'Assets/AfterSignal/Runtime';dest=baseline/'Assets/AfterSignal/Runtime'
for name in ['QualitySession.cs','QualityAudioCapture.cs','QualityVideoCapture.cs','RuntimeSmoke.cs']:
    source=next(current.rglob(name))
    shutil.copy2(source,dest/name)
game=dest/'GameDirector.cs';text=game.read_text(encoding='utf-8')
text=text.replace('Ready=true;','Ready=true;\n            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-quality-slice")>=0)gameObject.AddComponent<QualitySession>();')
game.write_text(text,encoding='utf-8')
print(baseline)

"""Fifteen-second stationary diagnostic; not a gameplay benchmark."""
from pathlib import Path
import sys, subprocess, json
root=Path(__file__).resolve().parents[1]
name=sys.argv[1];out=root/'Artifacts/Expansion/Probes'/name;out.mkdir(parents=True,exist_ok=True)
args=[str(root/'Builds/Windows/AFTERSIGNAL.exe'),'-quality-probe','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-quality-output',str(out),'-logFile',str(out/'player.log')]+sys.argv[2:]
result=subprocess.run(args,cwd=root,timeout=40)
print('exit',result.returncode)
if (out/'metrics.json').exists(): print((out/'metrics.json').read_text())

"""Mux the native listener track without cutting or reconstructing gameplay frames."""
from pathlib import Path
import subprocess,wave,json,array,re
root=Path(__file__).resolve().parents[1]
ffmpeg=root/'Tools/.python-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
folders=[root/'Artifacts/Residence/Iteration5/Residence-0',root/'Artifacts/Residence/Iteration5/Breach-5',root/'Artifacts/Residence/RailVisual/Roof']
audit=[]
for folder in folders:
    video=folder/'gameplay-unedited.mp4';sound=folder/'game-audio.wav'
    if folder.name=='Roof' and not video.exists():video=folder.parent/'gameplay-unedited.mp4'
    if not video.exists() or not sound.exists():continue
    with wave.open(str(sound)) as w:
        samples=array.array('h',w.readframes(w.getnframes()))
        peak=max(abs(x) for x in samples)/32768
        audit.append({'source':str(folder.relative_to(root)),'audioSeconds':len(samples)/w.getframerate()/w.getnchannels(),'peak':peak,'clippedSamples':sum(abs(x)>=32766 for x in samples),'channels':w.getnchannels(),'sampleRate':w.getframerate()})
    assert peak>0,'Silent output is not verified sound.'
    probe=subprocess.run([str(ffmpeg),'-hide_banner','-i',str(video)],capture_output=True,text=True)
    match=re.search(r'Duration: (\d+):(\d+):([\d.]+)',probe.stderr);assert match,video
    duration=int(match[1])*3600+int(match[2])*60+float(match[3])
    subprocess.run([str(ffmpeg),'-y','-i',str(video),'-i',str(sound),'-c:v','copy','-c:a','aac','-b:a','192k','-af','apad=whole_dur='+str(duration),'-t',str(duration),'-movflags','+faststart',str(folder/'gameplay-with-audio.mp4')],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=True,timeout=45)
(root/'Documentation/Residence/native-audio-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print(json.dumps(audit,indent=2))

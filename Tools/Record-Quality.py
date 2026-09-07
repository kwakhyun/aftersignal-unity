"""Record only the game's own window, with the opt-in normal-controller QA route."""
import argparse, pathlib, subprocess, time, sys
p=argparse.ArgumentParser();p.add_argument('label');p.add_argument('--no-video',action='store_true');p.add_argument('--project');p.add_argument('--effects-off',action='store_true');p.add_argument('--fps',type=int,default=60);p.add_argument('--d3d11',action='store_true');p.add_argument('--vsync',type=int);p.add_argument('--no-captures',action='store_true');a=p.parse_args()
workspace=pathlib.Path(__file__).resolve().parents[1];root=pathlib.Path(a.project) if a.project else workspace;sys.path.insert(0,str(workspace/'Tools/.python-deps'))
if not a.no_video:
    import imageio_ffmpeg
out=workspace/'Artifacts/Quality'/a.label;out.mkdir(parents=True,exist_ok=True)
args=[str(root/'Builds/Windows/AFTERSIGNAL.exe'),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-aftersignal-smoke','-quality-slice','-quality-output',str(out),'-logFile',str(out/'player.log')]
if not a.no_video:args.extend(['-quality-ffmpeg',imageio_ffmpeg.get_ffmpeg_exe()])
if a.effects_off:args+=['-quality-effects-off']
args+=['-quality-fps',str(a.fps)]
if a.d3d11:args+=['-force-d3d11']
if a.vsync is not None:args+=['-quality-vsync',str(a.vsync)]
if a.no_captures:args+=['-quality-no-captures']
game=subprocess.Popen(args,cwd=root)
try: code=game.wait(timeout=120)
except subprocess.TimeoutExpired: game.terminate();code=2
video=out/'gameplay-unedited.mp4';sound=out/'game-audio.wav'
if not a.no_video and video.exists() and sound.exists():subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-i',str(video),'-i',str(sound),'-c:v','copy','-c:a','aac','-b:a','192k','-shortest',str(out/'gameplay-with-audio.mp4')],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=True)
print('Game exit:',code,'Artifacts:',out,flush=True)
sys.exit(code)

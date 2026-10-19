"""Prepare local Mirelo WAVs for game timing and mix, preserving every raw file."""
from pathlib import Path
import argparse, hashlib, json, math, wave
import numpy as np
from mirelo_generate import ROOT, OUT, PLAN, read, save


def load(path, reference=False):
    with wave.open(str(path),'rb') as w:
        channels,rate=w.getnchannels(),w.getframerate()
        if w.getsampwidth()!=2 or (not reference and (channels!=1 or rate!=48000)):
            raise ValueError('Expected PCM16 mono 48 kHz: '+path.name)
        data=w.readframes(w.getnframes())
        if len(data)!=w.getnframes()*channels*2: raise ValueError('Truncated WAV')
    a=np.frombuffer(data,dtype='<i2').astype(np.float64).reshape(-1,channels).mean(axis=1)/32768
    if rate!=48000:
        a=np.interp(np.arange(round(len(a)*48000/rate))*rate/48000,np.arange(len(a)),a)
    return a


def active_rms(a):
    frames=len(a)//240
    if not frames: return 0
    blocks=a[:frames*240].reshape(-1,240)
    energy=np.mean(blocks*blocks,axis=1)
    active=energy>max(float(np.max(energy))*.0036,1e-8)
    return float(np.sqrt(np.mean(energy[active]))) if np.any(active) else 0


def prepare(a,loop,target):
    if not np.isfinite(a).all() or np.max(np.abs(a))<.005: raise ValueError('Empty or nonfinite audio')
    peak=float(np.max(np.abs(a))); active=np.flatnonzero(np.abs(a)>max(.0005,peak*.015))
    before={'seconds':len(a)/48000,'peak':peak,'active_rms':active_rms(a),'onset_seconds':int(active[0])/48000}
    if loop:
        if len(a)<48000*2: raise ValueError('Loop raw material is too short')
        k=12000
        theta=np.linspace(0,np.pi/2,k)
        join=a[-k:]*np.cos(theta)+a[:k]*np.sin(theta)
        a=np.concatenate((a[k:-k],join))
        # A sub-millisecond boundary taper prevents a discontinuity after quantization.
        k2=48
        a[:k2]*=np.linspace(0,1,k2);a[-k2:]*=np.linspace(1,0,k2)
        trimmed=0
    else:
        start=max(0,int(active[0])-144);end=min(len(a),int(active[-1])+1200)
        a=a[start:end].copy();trimmed=start/48000
        a[:min(48,len(a))]*=np.linspace(0,1,min(48,len(a)))
        k=min(720,len(a)//5);a[-k:]*=np.linspace(1,0,k)
    measured=active_rms(a)
    if measured<.0001: raise ValueError('No audible material after editing')
    gain=min(target/measured,.80/float(np.max(np.abs(a))))
    a*=gain
    after={'seconds':round(len(a)/48000,4),'peak':round(float(np.max(np.abs(a))),5),
           'active_rms':round(active_rms(a),5),'target_active_rms':round(target,5),
           'trimmed_lead_seconds':round(trimmed,4),'gain_db':round(20*math.log10(gain),3),
           'boundary_delta':round(float(abs(a[-1]-a[0])),7)}
    if after['peak']>.801 or not .02<=after['seconds']<=(20 if loop else 5): raise ValueError('Prepared audio outside game limits')
    return np.clip(np.rint(a*32768),-32768,32767).astype('<i2'),before,after


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scope',choices=['pilot','all'],default='pilot')
    args=parser.parse_args()
    plan=read(PLAN);ledger=read(OUT/'generation-ledger.json')
    folder=OUT/'prepared';folder.mkdir(exist_ok=True)
    assets=[];metrics=[]
    for row in plan['requests']:
        if args.scope=='pilot' and row['priority']!='pilot': continue
        cue=row['cue'];job=ledger['jobs'].get(cue,{})
        if job.get('status')!='succeeded': raise ValueError('Generation incomplete: '+cue)
        loop=row['kind']=='loop'
        if row['existing_sources']:
            reference=load(ROOT/row['existing_sources'][-1]['path'],reference=True)
            target=min(.22,max(.01,active_rms(reference)))
        else: target=.18 if cue=='police_siren' else .13
        for entry in job['files']:
            raw=OUT/entry['path'];name=raw.stem;path=folder/raw.name
            if hashlib.sha256(raw.read_bytes()).hexdigest()!=entry['sha256']: raise ValueError('Raw source hash changed')
            data,before,after=prepare(load(raw),loop,target)
            if path.exists():
                # Deterministic reruns only: never silently replace an edited output.
                with wave.open(str(path),'rb') as old: same=old.readframes(old.getnframes())==data.tobytes()
                if not same: raise ValueError('Prepared file changed; preserve it before rerunning: '+name)
            else:
                with wave.open(str(path),'wb') as wav:
                    wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(48000);wav.writeframes(data.tobytes())
            assets.append({'kind':'loop' if loop else 'sfx','id':name,'cue':cue,
                'source_file':raw.name,'provider':'Mirelo','model':'sfx-2.0',
                'source_url':('https://api.mirelo.ai/v3/text-to-sfx/generations/'+job['job_id']) if job.get('job_id') else job['source_url'],
                'license_url':'https://mirelo.ai/terms','license_plan':'Free','commercial_use':False,
                'reviewed':False,'loop_seam_reviewed':False if loop else None,
                'technical_checks':after,'human_listening_reviewed':False})
            metrics.append({'id':name,'before':before,'after':after})
    save(folder/'pack.json',{'schema':1,'status':'technical_preparation_complete_pending_playback_review','assets':assets})
    save(OUT/'preparation-metrics.json',metrics)
    print(json.dumps({'prepared_files':len(assets),'onsets_trimmed':sum(m['after']['trimmed_lead_seconds']>.01 for m in metrics),'clipped_files':0},ensure_ascii=False))

if __name__=='__main__': main()

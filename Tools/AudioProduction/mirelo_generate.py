"""Bounded Mirelo SFX 2.0 generation with preflight and a durable idempotency ledger."""
import argparse, datetime, hashlib, json, os, time, urllib.request, urllib.error, uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE='https://api.mirelo.ai/v3'
OUT=ROOT/'Artifacts/AudioProduction/Mirelo'
PLAN=ROOT/'Documentation/Audio/mirelo-sfx-plan.json'


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    tmp=path.with_suffix(path.suffix+'.tmp')
    tmp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    tmp.replace(path)

def key():
    value=os.environ.get('MIRELO_API_KEY','')
    if not value:
        for line in (ROOT/'.env.mirelo.local').read_text(encoding='utf-8-sig').splitlines():
            if line.startswith('MIRELO_API_KEY='): value=line.split('=',1)[1].strip().strip('\"').strip("'")
    if len(value)<24 or any(c.isspace() for c in value): raise ValueError('Configure the local MIRELO_API_KEY; never pass it on the command line')
    return value

class Client:
    def __init__(self): self.token=key()
    def call(self,path,body=None,idempotency=None):
        headers={'Authorization':'Bearer '+self.token,'Mirelo-Version':'2026-08-28'}
        if body is not None: headers['Content-Type']='application/json'
        if idempotency: headers['Idempotency-Key']=idempotency
        req=urllib.request.Request(BASE+path,data=None if body is None else json.dumps(body,separators=(',',':')).encode(),headers=headers)
        try:
            with urllib.request.urlopen(req,timeout=35) as response: return json.load(response)
        except urllib.error.HTTPError as error:
            raise RuntimeError(f'Mirelo HTTP {error.code} on {path.split("?")[0]}; request halted without automatic resubmission') from None


def download(url,path):
    if not url.startswith('https://'): raise ValueError('Expected an HTTPS audio download')
    # No API Authorization header is sent to the audio storage host.
    with urllib.request.urlopen(url,timeout=35) as response:
        data=response.read(16*1024*1024+1)
    if len(data)>16*1024*1024 or not data.startswith(b'RIFF') or int.from_bytes(data[4:8],'little')+8!=len(data):
        raise ValueError('Unexpected or truncated WAV download')
    temporary=path.with_suffix('.wav.part')
    with temporary.open('xb') as f: f.write(data)
    # Windows rename refuses an existing destination; never overwrite a completed take.
    temporary.rename(path)
    return hashlib.sha256(data).hexdigest()


def run(scope,limit,max_jobs):
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'raw').mkdir(exist_ok=True)
    client=Client()
    model=client.call('/models/sfx-2.0')
    if model.get('id')!='sfx-2.0': raise ValueError('Wrong model')
    save(OUT/'model-contract.json',model)
    plan=read(PLAN)
    ledger_path=OUT/'generation-ledger.json'
    if ledger_path.exists(): ledger=read(ledger_path)
    else:
        files=[OUT/'raw'/f'blade_hit_{i}.wav' for i in range(2)]
        if not all(p.is_file() for p in files): raise ValueError('Preserve the initial browser generation before starting the batch')
        ledger={'model':'sfx-2.0','plan':'Free','commercial_use':False,'credit_limit':limit,'jobs':{
            'blade_hit':{'status':'succeeded','origin':'Mirelo Studio Playground','credits':16,'duration_ms':800,'num_variants':2,
                         'source_url':'https://mirelo.ai/studio/playground#text-to-sfx','files':[{'path':p.relative_to(OUT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files]}}}
        save(ledger_path,ledger)
    if ledger['credit_limit']!=limit: raise ValueError('Existing ledger budget differs; do not silently change authorization')
    rows=[r for r in plan['requests'] if scope=='all' or r['priority']=='pilot']
    count=0
    for row in rows:
        cue=row['cue'];previous=ledger['jobs'].get(cue)
        if previous and previous.get('status')=='succeeded':
            files=previous.get('files',[])
            if len(files)!=len(row['planned_files']) or any(not (OUT/f['path']).is_file() or hashlib.sha256((OUT/f['path']).read_bytes()).hexdigest()!=f['sha256'] for f in files):
                raise RuntimeError('Completed audio is missing or changed: '+cue)
            continue
        if count>=max_jobs: break
        body=row['request']
        if previous:
            # Only GET an already-known job; uncertain submissions require explicit recovery.
            if not previous.get('job_id'): raise RuntimeError('Uncertain prior submission for '+cue+'; recover its idempotency key before continuing')
            if previous['request']!=body: raise ValueError('Planned body changed for an existing job')
            entry=previous;job=client.call('/text-to-sfx/generations/'+entry['job_id'])
        else:
            quote=client.call('/text-to-sfx/generations/preflight',body)
            credits=quote['credits']
            recovery=quote.get('credit_recovery')
            if recovery and (recovery.get('provisioning_state')!='ready' or recovery.get('recovery_action') is not None or recovery.get('credit_shortfall',0)>0):
                raise RuntimeError('Free credits are not available for '+cue)
            committed=sum(x.get('credits',x.get('quoted_credits',0)) for x in ledger['jobs'].values())
            if not isinstance(credits,int) or credits<0 or committed+credits>limit: raise RuntimeError('Authorized free credit ceiling reached')
            entry={'status':'submitting','idempotency_key':str(uuid.uuid4()),'request':body,'quoted_credits':credits,
                   'submitted_at':datetime.datetime.now(datetime.timezone.utc).isoformat()}
            ledger['jobs'][cue]=entry;save(ledger_path,ledger)
            print(f'{cue}: preflight {credits} credits; total committed {committed+credits}/{limit}',flush=True)
            job=client.call('/text-to-sfx/generations?wait=20',body,entry['idempotency_key'])
            entry['job_id']=job['id'];entry['status']=job['status'];save(ledger_path,ledger)
        deadline=time.monotonic()+240
        while job['status'] in ('queued','running'):
            if time.monotonic()>deadline: raise RuntimeError('Generation still running; resume will poll existing job '+cue)
            time.sleep(5)
            job=client.call('/text-to-sfx/generations/'+entry['job_id'])
        save(OUT/'jobs'/(cue+'.json'),job)
        entry['status']='downloading' if job['status']=='succeeded' else job['status']
        entry['credits']=job.get('credits')
        if not isinstance(entry['credits'],int): raise RuntimeError('Unknown billed credit amount; stop for review')
        save(ledger_path,ledger)
        if job['status']!='succeeded': raise RuntimeError('Incomplete generation '+cue+'; inspect receipt before deciding on a retry')
        output=job['result']['output'];variants=output['variants']
        if output['status']!='succeeded' or len(variants)!=len(row['planned_files']): raise RuntimeError('Missing audio variant '+cue)
        entry['files']=[]
        for variant,name in zip(sorted(variants,key=lambda v:v['index']),row['planned_files']):
            if variant['status']!='succeeded': raise RuntimeError('Failed audio variant '+cue)
            audio=variant['files']['audio'];path=OUT/'raw'/name
            if audio['sample_rate']!=48000 or audio['channels']!=1: raise ValueError('Unexpected audio format')
            digest=hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else download(audio['url'],path)
            entry['files'].append({'path':path.relative_to(OUT).as_posix(),'sha256':digest,'variant_index':variant['index']})
        entry['status']='succeeded';entry['generation_id']=output.get('generation_id');save(ledger_path,ledger)
        count+=1
        print(f'{cue}: saved {len(entry["files"])} WAV files',flush=True)
    print(json.dumps({'finished_jobs':sum(x['status']=='succeeded' for x in ledger['jobs'].values()),'credits':sum(x.get('credits',x.get('quoted_credits',0)) for x in ledger['jobs'].values())}),flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--scope',choices=['pilot','all'],default='pilot')
    p.add_argument('--max-total-credits',type=int,required=True)
    p.add_argument('--max-jobs',type=int,default=71)
    args=p.parse_args()
    run(args.scope,args.max_total_credits,args.max_jobs)

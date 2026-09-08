"""Licensed public Freesound previews -> mono PCM loops with seamless overlap.
Source pages and exact downloaded bytes are retained in Artifacts; credits ship.
"""
import concurrent.futures, hashlib, io, json, re
from pathlib import Path
import numpy as np
import requests, soundfile as sf
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
from scipy.signal import resample_poly
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/AfterSignal/Resources/Audio/Transport'
CACHE=ROOT/'Artifacts/TransportAudio'
SOURCES=[
 ('sedan','Dmitry_mansurev64',748027,'CC0-1.0',0,12,True),
 ('taxi','RichieMcMullen',386793,'CC0-1.0',0,8,True),
 ('bus','ReadeOnly',186936,'CC0-1.0',0,7,True),
 ('truck','ReadeOnly',186936,'CC0-1.0',0,7,True),
 ('motorcycle','Dmitry_mansurev64',747916,'CC0-1.0',0,8,True),
 ('sportscar','FreeCarSoundsGaming',535040,'CC0-1.0',0,1.7,True),
 ('boat','morganpurkis',394287,'CC0-1.0',0,5.5,True),
 ('ship','LG',91533,'CC-BY-4.0',0,11,True),
 ('airliner','smidoid',128614,'CC-BY-4.0',0,5,True),
 ('fighter','MickBoere',269064,'CC0-1.0',5,12,True),
 ('combathelicopter','qubodup',187681,'CC0-1.0',0,6,True),
 ('tank','qubodup',200303,'CC-BY-4.0',0,5,True),
 ('ignition','prometheus888',458461,'CC0-1.0',0,3.5,False),
 ('bike_start','spenceomatic',119774,'CC0-1.0',0,3.5,False),
 ('underwater','Perel',173439,'CC0-1.0',0,5.5,True),
 ('waterstroke','felix.blume',208816,'CC0-1.0',6,1.25,False),
 ('cannon','unfa',231765,'CC0-1.0',0,4,False),
 ('turbine','qubodup',146770,'CC0-1.0',0,4.5,True),
]
def run(row):
 cue,author,ident,license,start,duration,loop=row
 page=f'https://freesound.org/people/{author}/sounds/{ident}/'
 html_path=CACHE/f'{ident}.html'
 if not html_path.exists():
  # Public, unauthenticated asset host currently serves an expired certificate.
  r=requests.get(page,timeout=60,verify=False);r.raise_for_status();html_path.write_text(r.text,encoding='utf-8')
 html=html_path.read_text(encoding='utf-8')
 matches=re.findall(r'https[^\s"<>]+?-hq\.(?:mp3|ogg)',html)
 if not matches: raise RuntimeError(f'No published HQ preview: {page}')
 url=matches[0].replace('&amp;','&');raw=CACHE/f'{ident}.{url.split(".")[-1]}'
 if not raw.exists():
  r=requests.get(url,timeout=90,verify=False);r.raise_for_status();raw.write_bytes(r.content)
 a,rate=sf.read(raw,always_2d=True);a=a.mean(axis=1)
 a=a[int(start*rate):int((start+duration)*rate)].copy();a-=a.mean()
 a=resample_poly(a,48000,rate);rate=48000
 if loop:
  n=min(int(.08*rate),len(a)//12);blend=np.linspace(0,1,n)
  a[:n]=a[-n:]*(1-blend)+a[:n]*blend;a=a[:-n]
 else:
  n=min(4800,len(a)//5);a[-n:]*=np.linspace(1,0,n);a[:96]*=np.linspace(0,1,96)
 a*=min(.86/max(.001,np.max(np.abs(a))),.15/max(.001,np.sqrt(np.mean(a*a))))
 sf.write(OUT/f'{cue}.wav',a,rate,subtype='PCM_16')
 result=dict(cue=cue,author=author,page=page,preview=url,license=license,source_sha256=hashlib.sha256(raw.read_bytes()).hexdigest(),seconds=len(a)/rate,loop=loop,start=start,processing='HQ preview decoded, mono downmix, DC removal, resampled 48 kHz, gain limited, loop boundary overlap or one-shot fades')
 print(cue+' ready',flush=True);return result
if __name__=='__main__':
 OUT.mkdir(parents=True,exist_ok=True);CACHE.mkdir(parents=True,exist_ok=True)
 with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:rows=list(pool.map(run,SOURCES))
 doc=ROOT/'Documentation/Audio/transport-sources.json';doc.write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 credits='# Transport and swimming audio\n\nEdits: mono conversion, resampling, gain and seamless loop crossfades. Original authors retain their rights.\n\n'
 for row in rows:
  lic='https://creativecommons.org/publicdomain/zero/1.0/' if row['license']=='CC0-1.0' else 'https://creativecommons.org/licenses/by/4.0/'
  credits+=f"- {row['cue']}: {row['author']} — {row['page']} — {row['license']} ({lic})\n"
 (ROOT/'Documentation/Audio/TRANSPORT-LICENSES.md').write_text(credits,encoding='utf-8')
 ship=ROOT/'Assets/StreamingAssets/ThirdParty';ship.mkdir(parents=True,exist_ok=True);(ship/'TransportAudio.txt').write_text(credits,encoding='utf-8')

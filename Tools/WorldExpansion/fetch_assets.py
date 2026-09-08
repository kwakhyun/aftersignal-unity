"""Download selected CC0 Poly Haven source models, preserving their texture paths and hashes."""
import concurrent.futures, hashlib, json, pathlib, urllib.request
opener=urllib.request.build_opener(); opener.addheaders=[('User-Agent','AfterSignal-asset-import/1.0 (local Unity project)')]; urllib.request.install_opener(opener)
ROOT=pathlib.Path(__file__).resolve().parents[2]
IDS=['street_lamp_01','modular_street_seating','concrete_road_barrier','modular_urban_apartments_facade','industrial_wall_lamp','painted_wooden_bench']
def fetch(asset):
    info=json.load(urllib.request.urlopen('https://api.polyhaven.com/files/'+asset))
    rec=info['blend']['1k']['blend']; folder=ROOT/'Artifacts/WorldExpansion/Source'/asset; folder.mkdir(parents=True,exist_ok=True)
    files=[(asset+'.blend',rec)]+list(rec.get('include',{}).items()); result=[]
    for name,item in files:
        dest=folder/name; dest.parent.mkdir(parents=True,exist_ok=True)
        if not dest.exists() or hashlib.md5(dest.read_bytes()).hexdigest()!=item['md5']:
            urllib.request.urlretrieve(item['url'],dest)
        assert hashlib.md5(dest.read_bytes()).hexdigest()==item['md5'],dest
        result.append({'path':str(dest.relative_to(ROOT)),'url':item['url'],'md5':item['md5']})
    print(asset,len(result),flush=True)
    return {'id':asset,'source':'https://polyhaven.com/a/'+asset,'license':'CC0-1.0','files':result}
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    records=list(pool.map(fetch,IDS))
(ROOT/'Documentation/WorldExpansion/polyhaven-sources.json').write_text(json.dumps(records,indent=2),encoding='utf-8')

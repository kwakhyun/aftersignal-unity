"""Fetch pinned CC0 production textures, verify hashes, retain provenance.
Powered by Poly Haven (https://polyhaven.com). No live asset API is used by the game.
"""
import hashlib, json, pathlib, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
DEST = ROOT / 'Artifacts/Fidelity/Materials'
ASSETS = ['asphalt_04', 'concrete_tiles_02', 'concrete_wall_006', 'blue_metal_plate']
def request(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent':'AfterSignalArtPipeline/1.0'}),timeout=90)
DEST.mkdir(parents=True, exist_ok=True)
manifest=[]
for asset in ASSETS:
    with request('https://api.polyhaven.com/files/'+asset) as response:
        files=json.load(response)
    for kind, label in [('Diffuse','albedo'),('nor_gl','normal'),('arm','arm')]:
        descriptor=files[kind]['2k']['jpg']
        target=DEST/(asset+'_'+label+'.jpg')
        if not target.exists() or hashlib.md5(target.read_bytes()).hexdigest()!=descriptor['md5']:
            with request(descriptor['url']) as response:
                data=response.read()
            if hashlib.md5(data).hexdigest()!=descriptor['md5']: raise ValueError('Hash mismatch: '+asset)
            target.write_bytes(data)
        manifest.append(dict(asset=asset,file=target.name,url=descriptor['url'],md5=descriptor['md5'],license='CC0-1.0',source='https://polyhaven.com/a/'+asset))
        print(target.name, target.stat().st_size,flush=True)
(DEST/'provenance.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')

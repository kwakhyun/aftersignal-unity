"""CC0 tree source, Powered by Poly Haven; hash verified and pinned offline."""
import pathlib,urllib.request,json,hashlib
root=pathlib.Path(__file__).resolve().parents[2];out=root/'Artifacts/Fidelity/TreeSource';out.mkdir(parents=True,exist_ok=True)
def request(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'AfterSignalArtPipeline/1.0'}),timeout=120)
with request('https://api.polyhaven.com/files/tree_small_02') as response:source=json.load(response)['gltf']['1k']['gltf']
manifest=[]
for name,item in [('tree_small_02.gltf',source)]+list(source['include'].items()):
    path=out/name
    if not path.resolve().is_relative_to(out.resolve()):raise ValueError('Unexpected asset path')
    path.parent.mkdir(parents=True,exist_ok=True)
    with request(item['url']) as response:data=response.read()
    if hashlib.md5(data).hexdigest()!=item['md5']:raise ValueError('Checksum mismatch')
    path.write_bytes(data);print(name,len(data),flush=True)
    manifest.append(dict(file=name,url=item['url'],md5=item['md5'],license='CC0-1.0',source='https://polyhaven.com/a/tree_small_02'))
(out/'provenance.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')

import concurrent.futures,urllib.request,pathlib,json,hashlib
root=pathlib.Path(__file__).resolve().parents[2]
opener=urllib.request.build_opener();opener.addheaders=[('User-Agent','AfterSignal-asset-import/1.0')];urllib.request.install_opener(opener)
def get(asset):
    info=json.load(urllib.request.urlopen('https://api.polyhaven.com/files/'+asset));out=root/'Artifacts/WorldExpansion/SourceTextures'/asset;out.mkdir(parents=True,exist_ok=True);records=[]
    for role in ['diff','nor_gl','rough']:
        key={'diff':'Diffuse','rough':'Rough'}.get(role,role)
        item=info.get(key,info.get(role,{})).get('1k',{});item=item.get('jpg',item.get('png',item.get('exr')))
        if not item:continue
        name=item['url'].rsplit('/',1)[-1];path=out/name
        if not path.exists():urllib.request.urlretrieve(item['url'],path)
        assert hashlib.md5(path.read_bytes()).hexdigest()==item['md5'];records.append({'role':role,'file':name,**item})
    print(asset,len(records),flush=True);return {'id':asset,'license':'CC0-1.0','source':'https://polyhaven.com/a/'+asset,'files':records}
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:records=list(pool.map(get,['asphalt_02','aerial_sand','aerial_grass_rock','concrete_pavement']))
(root/'Documentation/WorldExpansion/surface-sources.json').write_text(json.dumps(records,indent=2))

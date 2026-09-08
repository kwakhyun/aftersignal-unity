import pathlib,json
root=pathlib.Path(__file__).resolve().parents[2]
for folder in (root/'Assets/AfterSignal/Resources/WorldAssets').iterdir():
    meta=folder/'materials.json'
    if not meta.exists():continue
    data=json.loads(meta.read_text())
    for m in data['materials']:
        suffixes=('diff_','nor_gl_','rough_','metal_','emissive_','alpha_')
        m['images']=[p.name for p in folder.iterdir() if p.suffix in ('.png','.jpg','.exr') and any(p.name.startswith(m['name']+'_'+s) for s in suffixes)]
    meta.write_text(json.dumps(data,indent=2))
    print(folder.name,[(m['name'],len(m['images'])) for m in data['materials']])

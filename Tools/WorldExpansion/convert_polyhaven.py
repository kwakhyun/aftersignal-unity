import bpy, pathlib, json, re, shutil
ROOT=pathlib.Path(__file__).resolve().parents[2]
records=[]
for folder in (ROOT/'Artifacts/WorldExpansion/Source').iterdir():
    source=folder/(folder.name+'.blend')
    if not source.exists():continue
    bpy.ops.wm.open_mainfile(filepath=str(source))
    out=ROOT/'Assets/AfterSignal/Resources/WorldAssets'/folder.name;out.mkdir(parents=True,exist_ok=True)
    for texture in (folder/'textures').iterdir():shutil.copy2(texture,out/texture.name)
    bpy.ops.object.select_all(action='DESELECT')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.hide_render]
    mats=[]
    for m in bpy.data.materials:
        if not m.use_nodes:continue
        images=[]
        for n in m.node_tree.nodes:
            if n.type!='TEX_IMAGE' or not n.image:continue
            im=n.image; src=pathlib.Path(im.filepath.replace('\\','/'))
            if not src.name:continue
            stem=re.sub(r'_[1248]k$', '',src.stem)
            matches=list((folder/'textures').glob(stem+'_1k.*')) or list((folder/'textures').glob(src.name))
            if matches:im.filepath=str(matches[0]);images.append(matches[0].name)
        mats.append({'name':m.name,'images':images})
    for o in meshes:
        o.select_set(True)
        if len(o.data.vertices)>12000:
            dec=o.modifiers.new('Game budget','DECIMATE');dec.ratio=12000/len(o.data.vertices)
        for mod in o.modifiers:
            if mod.type=='SUBSURF':mod.levels=min(mod.levels,1);mod.render_levels=min(mod.render_levels,1)
    if meshes:bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.export_scene.fbx(filepath=str(out/(folder.name+'.fbx')),use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    record={'id':folder.name,'objects':[{'name':o.name,'size':list(o.dimensions),'location':list(o.location),'vertices':len(o.data.vertices)} for o in meshes],'materials':mats}
    (out/'materials.json').write_text(json.dumps(record,indent=2))
    records.append(record)
    print('EXPORTED',folder.name,len(meshes),flush=True)
(ROOT/'Artifacts/WorldExpansion/polyhaven-inspect.json').write_text(json.dumps(records,indent=2))

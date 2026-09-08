import bpy,bmesh,pathlib,collections,json
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(root/'Artifacts/Fidelity/TreeSource/tree_small_02.gltf'))
for obj in list(bpy.context.scene.objects):
    if obj.type!='MESH':continue
    print('OBJECT',obj.name,tuple(obj.dimensions),flush=True)
    bm=bmesh.new();bm.from_mesh(obj.data);seen=set();groups=collections.defaultdict(list)
    for v in bm.verts:
        if v in seen:continue
        stack=[v];seen.add(v);verts=[];faces=set()
        while stack:
            w=stack.pop();verts.append(w);faces.update(w.link_faces)
            for edge in w.link_edges:
                other=edge.other_vert(w)
                if other not in seen:seen.add(other);stack.append(other)
        mat=obj.data.materials[next(iter(faces)).material_index].name if faces else 'none'
        groups[mat].append((len(verts),sum(len(f.verts)-2 for f in faces)))
    for mat,counts in groups.items():print(mat,len(counts),'most common',collections.Counter(counts).most_common(12),flush=True)
    for mat in obj.data.materials:
        print('MATERIAL',mat.name,mat.diffuse_color[:],[(n.type,[(x.name,x.default_value) for x in n.inputs if x.name in ['Alpha','Base Color']]) for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'],flush=True)
    bm.free()

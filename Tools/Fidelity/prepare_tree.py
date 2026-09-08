"""Reduce CC0 botanical source into bounded, material-preserving game LODs."""
import bpy,bmesh,pathlib,json,random,math
root=pathlib.Path(__file__).resolve().parents[2];source=root/'Artifacts/Fidelity/TreeSource';out=root/'Artifacts/Fidelity/Botanical';out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source/'tree_small_02.gltf'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();original=bpy.context.object;original.name='BotanicalSource';original.select_set(False)
count=sum(len(p.vertices)-2 for p in original.data.polygons)
bpy.context.view_layer.objects.active=original;original.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
sources=list(bpy.context.selected_objects)
for obj in sources:obj.select_set(False)
stats=[]
for lod,keep in enumerate((.16,.05,.012)):
    parts=[]
    for source_object in sources:
        obj=source_object.copy();obj.data=source_object.data.copy();bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        material=obj.data.materials[obj.data.polygons[0].material_index].name
        if 'leaves' in material:
            # Keep whole botanical islands before simplifying. Global decimation had
            # collapsed the trunk and every leaf to slivers during visual review.
            bm=bmesh.new();bm.from_mesh(obj.data);seen=set();remove=[];rng=random.Random(712)
            for vertex in bm.verts:
                if vertex in seen:continue
                group=[];stack=[vertex];seen.add(vertex)
                while stack:
                    v=stack.pop();group.append(v)
                    for edge in v.link_edges:
                        other=edge.other_vert(v)
                        if other not in seen:seen.add(other);stack.append(other)
                if rng.random()>keep:remove.extend(group)
                else:
                    center=sum((v.co for v in group),group[0].co.copy()*0)/len(group)
                    for v in group:v.co=center+(v.co-center)*min(3.2,math.sqrt(1/keep)*.8)
            bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(obj.data);bm.free();ratio=.20
        elif 'branches' in material:ratio=(.12,.065,.025)[lod]
        else:ratio=(.40,.18,.08)[lod]
        modifier=obj.modifiers.new('Material-specific silhouette reduction','DECIMATE');modifier.ratio=ratio;modifier.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=modifier.name);parts.append(obj);obj.select_set(False)
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='BotanicalTree_LOD'+str(lod)
    bpy.ops.export_scene.fbx(filepath=str(out/(obj.name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',object_types={'MESH'},add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
    stats.append({'lod':lod,'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'sourceTriangles':count});bpy.data.objects.remove(obj,do_unlink=True)
(out/'lod-stats.json').write_text(json.dumps(stats,indent=2));print(stats,flush=True)

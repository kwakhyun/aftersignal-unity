"""Small structural audit; this does not certify artistic likeness or animation quality."""
import bpy, json, math
from pathlib import Path
R=Path(__file__).resolve().parents[2];out=R/'Artifacts/SeohaStudio'
bpy.ops.wm.open_mainfile(filepath=str(out/'Seoha-Likeness.blend'))
rig=bpy.data.objects['Seoha_Rig'];objects=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==rig]
issues=[];vertices=0;triangles=0
for o in objects:
    deform={g.index for g in o.vertex_groups if g.name in rig.data.bones}
    for v in o.data.vertices:
        if not all(math.isfinite(x) for x in v.co):issues.append(o.name+': non-finite vertex')
        if not any(g.group in deform and g.weight>0 for g in v.groups):issues.append(o.name+': unweighted vertex')
    vertices+=len(o.data.vertices)
    evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=evaluated.to_mesh();m.calc_loop_triangles();triangles+=len(m.loop_triangles);evaluated.to_mesh_clear()
for name in ['Face / sculpted anime topology','Eye / contoured illustrated lid -1','Eye / contoured illustrated lid 1']:
    o=bpy.data.objects[name]
    if not o.data.uv_layers:issues.append(name+': missing face UV')
    if not o.data.shape_keys or not o.data.shape_keys.key_blocks.get('Blink.L'):issues.append(name+': missing blink shape')
image=next((i for i in bpy.data.images if 'Seoha-Face-Albedo' in i.name),None)
if image is None or not image.packed_file:issues.append('Face albedo not packed into editable source')
report={'structural_errors':sorted(set(issues)),'weighted_meshes':len(objects),'base_vertices':vertices,'evaluated_triangles':triangles,'skeleton_bones':len(rig.data.bones),'face_texture_packed':bool(image and image.packed_file),'artistic_acceptance':False,'gameplay_replacement':False}
(out/'asset-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
if issues:raise RuntimeError('Seoha structural audit failed')

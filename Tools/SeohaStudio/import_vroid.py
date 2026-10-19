"""Import the locally authored, private VRoid source for Blender refinement."""
import bpy
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Artifacts/SeohaStudio/VRoid'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.preferences.addon_enable(module='VRM_Addon_for_Blender-release')
# This is our own newly authored file, exported with owner-only redistribution.
os.environ['BLENDER_VRM_AUTOMATIC_LICENSE_CONFIRMATION'] = 'true'
result = bpy.ops.import_scene.vrm(filepath=str(OUT / 'Seoha-Authoring.vrm'),
    extract_textures_into_folder=True, make_new_texture_folder=False)
assert 'FINISHED' in result, result
report = {'objects': [], 'materials': [], 'images': [], 'armature': []}
for obj in bpy.data.objects:
    info = {'name': obj.name, 'type': obj.type, 'location': list(obj.location)}
    if obj.type == 'MESH':
        info.update(vertices=len(obj.data.vertices), polygons=len(obj.data.polygons),
            materials=[m.name if m else '' for m in obj.data.materials],
            bounds=[list(v) for v in obj.bound_box],
            shape_keys=list(obj.data.shape_keys.key_blocks.keys()) if obj.data.shape_keys else [],
            groups=[v.name for v in obj.vertex_groups])
    elif obj.type == 'ARMATURE':
        report['armature'] = [{'name': b.name, 'head': list(b.head_local), 'tail': list(b.tail_local),
            'parent': b.parent.name if b.parent else None} for b in obj.data.bones]
    report['objects'].append(info)
for mat in bpy.data.materials:
    report['materials'].append({'name': mat.name, 'nodes': [{
        'name': n.name, 'type': n.type,
        'image': n.image.name if n.type == 'TEX_IMAGE' and n.image else None
    } for n in mat.node_tree.nodes] if mat.node_tree else []})
for img in bpy.data.images:
    report['images'].append({'name':img.name, 'size':list(img.size), 'path':img.filepath})
(OUT / 'import-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Seoha-Imported.blend'))
print('IMPORTED', len(report['armature']), 'bones', len(report['objects']), 'objects')

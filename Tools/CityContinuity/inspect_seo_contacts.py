from pathlib import Path
import bpy,json
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Artifacts/CharacterLab/Refined'
bpy.ops.wm.open_mainfile(filepath=str(path/'Seo-Actions-Review.blend'))
rig=bpy.data.objects['Seoha_Rig'];result={'matrix':[list(v) for v in rig.matrix_world],'frames':[]}
for frame in [1,13,25,37]:
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();row={'frame':frame,'bones':{}}
    for side in ['L','R']:
        for name in ['Review thigh.','Review shin.','upperleg01.','lowerleg01.','foot.']:
            bone=rig.pose.bones[name+side];row['bones'][name+side]={'head':list(bone.head),'tail':list(bone.tail)}
        row['bones']['target.'+side]=list(bpy.data.objects['Foot contact target '+side].location)
    result['frames'].append(row)
(path/'contact-audit.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))

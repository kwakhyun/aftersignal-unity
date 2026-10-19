"""Minimal rig safety checks and actual render review of the anatomy revision."""
from pathlib import Path
import bpy,math,json,sys
from mathutils import Vector,Quaternion
sys.path.insert(0,str(Path(__file__).parent))
from vroid_review import setup
OUT=Path(__file__).resolve().parents[2]/'Artifacts/SeohaStudio/VRoid/Anatomy'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Anatomy-Finish.blend'))
rig=bpy.data.objects['Armature'];face=bpy.data.objects['Face'];keys=face.data.shape_keys.key_blocks
rig.animation_data_clear()

def rotate(name,axis,deg):
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion();b.rotation_mode='QUATERNION'
    b.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(deg))@q
def relaxed():
    for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
    for k in keys:k.value=0
    for side,angle in [('L',67),('R',-67)]:
        rotate('J_Bip_'+side+'_UpperArm',(0,1,0),angle)
        rotate('J_Bip_'+side+'_LowerArm',(0,0,1),-8 if side=='L' else 8)
        for digit in ['Index','Middle','Ring','Little']:
            for n,angle in [(1,12),(2,18),(3,10)]:
                bone=rig.pose.bones.get('J_Bip_'+side+'_'+digit+str(n))
                if bone:bone.rotation_quaternion=Quaternion((1,0,0),math.radians(angle))

scene,cam,aim=setup();scene.view_settings.view_transform='AgX'
scene.render.resolution_x=1100;scene.render.resolution_y=1300
scene.eevee.taa_render_samples=128;scene.eevee.shadow_ray_count=4;scene.eevee.shadow_step_count=8
for name,power in [('Key',220),('Fill',95),('Rim',200)]:
    light=bpy.data.objects[name].data;light.energy=power;light.use_shadow_jitter=True

def render(label,pos,target,scale):
    cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/('Review-'+label+'.png'));bpy.ops.render.render(write_still=True)

errors=[];meshes=[o for o in scene.objects if o.type=='MESH']
for ob in meshes:
    for v in ob.data.vertices:
        if not all(math.isfinite(n) for n in v.co):errors.append(ob.name+': nonfinite vertex');break
        weight=sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name in rig.data.bones)
        if abs(weight-1)>.011:errors.append(ob.name+': invalid skin weight '+str(weight));break
    for mod in ob.modifiers:
        if mod.type=='ARMATURE' and mod.object!=rig:errors.append(ob.name+': wrong armature')
if len(keys)!=58 or any(len(k.data)!=len(face.data.vertices) for k in keys):errors.append('Facial expression topology mismatch')
if len(rig.data.bones)!=117:errors.append('Unexpected rig bone count')
report={'structural_errors':errors,'rig_bones':len(rig.data.bones),'expression_shapes':len(keys)-1,
    'mesh_objects':len(meshes),'base_vertices':sum(len(o.data.vertices) for o in meshes),
    'art_target_achieved':False,'gameplay_connected':False,'final_gameplay_animations':False,
    'limitations':['hair roots and strand grouping remain stylized study quality','garment side seams and equipment detail need more authoring',
    'extreme-pose cloth/hair collision and full action clips not completed']}
(OUT/'anatomy-review.json').write_text(json.dumps(report,indent=2),encoding='utf8')
assert not errors,errors

relaxed()
render('Neutral',(.65,-2.8,1.52),(0,0,1.49),.48)
render('Body',(.5,-3,1.02),(0,0,.83),1.88)
render('Back',(-.65,3,1.25),(0,0,1.26),.90)
keys['Fcl_EYE_Close'].value=1
render('Blink',(.65,-2.8,1.52),(0,0,1.49),.43)
keys['Fcl_EYE_Close'].value=0;keys['Fcl_MTH_A'].value=.6
render('Speech',(.65,-2.8,1.52),(0,0,1.49),.43)
relaxed()
rotate('J_Bip_L_UpperLeg',(1,0,0),-55);rotate('J_Bip_L_LowerLeg',(1,0,0),80)
rotate('J_Bip_R_UpperArm',(0,1,0),-35);rotate('J_Bip_R_LowerArm',(0,0,1),65)
render('Bend',(.85,-3,1.05),(0,0,.88),1.9)
relaxed()
# A separate natural stance for the editable inspection source; neutral views
# above remain available for proportion comparisons.
rotate('J_Bip_C_Head',(0,1,0),-4)
rotate('J_Bip_L_UpperArm',(0,1,0),61);rotate('J_Bip_L_LowerArm',(0,0,1),-16)
# Preserve the reference's warm skin in the presentation view. The neutral
# AgX views above remain available for geometry and deformation inspection.
scene.view_settings.view_transform='Standard'
render('Portrait',(.58,-2.8,1.51),(0,0,1.315),.93)
scene.frame_start=1;scene.frame_end=1
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
for image in bpy.data.images:
    if image.source=='FILE' and image.has_data:
        try:image.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Anatomy-Review.blend'))
print(json.dumps(report))

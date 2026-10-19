"""Actual articulated-pose and expression review for the private Seoha asset."""
from pathlib import Path
import sys, math, json
import bpy,bmesh
from mathutils import Quaternion,Vector
sys.path.insert(0,str(Path(__file__).parent))
from vroid_review import OUT,render_views
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Refined.blend'))
rig=bpy.data.objects['Armature'];face=bpy.data.objects['Face'];body=bpy.data.objects['Body']

# Tailor fingerless gloves directly from the hand topology and original skin weights.
for side in [-1,1]:
    ob=body.copy();ob.data=body.data.copy();ob.name='Seoha fingerless glove '+str(side)
    bpy.context.collection.objects.link(ob)
    bm=bmesh.new();bm.from_mesh(ob.data)
    discard=[]
    for f in bm.faces:
        xs=[side*(ob.matrix_world@v.co).x for v in f.verts]
        if f.material_index!=0 or min(xs)<.445 or max(xs)>.535:discard.append(f)
    bmesh.ops.delete(bm,geom=discard,context='FACES')
    isolated=[v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm,geom=isolated,context='VERTS')
    bm.normal_update()
    for v in bm.verts:v.co+=v.normal*.00075
    bm.to_mesh(ob.data);bm.free()
    ob.data.materials.clear();ob.data.materials.append(bpy.data.materials['Seoha | midnight leather'])
    for p in ob.data.polygons:p.material_index=0;p.use_smooth=True
    mod=ob.modifiers.new('Glove edge thickness','SOLIDIFY');mod.thickness=.0005

def rotate_world(name,axis,degrees):
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion()
    b.rotation_mode='QUATERNION';b.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q

def rest():
    for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
    for key in face.data.shape_keys.key_blocks:key.value=0

def relaxed():
    rest()
    for side,angle in [('L',67),('R',-67)]:
        rotate_world('J_Bip_'+side+'_UpperArm',(0,1,0),angle)
        # A slight elbow bend and relaxed, separated fingers.
        rotate_world('J_Bip_'+side+'_LowerArm',(0,0,1),-8 if side=='L' else 8)
        for digit in ['Index','Middle','Ring','Little']:
            for n,a in [(1,12),(2,18),(3,10)]:
                b=rig.pose.bones.get('J_Bip_'+side+'_'+digit+str(n))
                if b:b.rotation_quaternion=Quaternion((1,0,0),math.radians(a))

relaxed()
render_views('Pose', [('Portrait',(.60,-2.8,1.55),(0,0,1.475),.66),
    ('Body',(.5,-3,1.05),(0,0,.88),1.94),
    ('Back',(-.45,3,1.45),(0,0,1.46),.69)])

# Save a human-reviewable relaxed pose as the default; T pose stays available via Clear Pose.
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=48
face.data.shape_keys.key_blocks['Fcl_EYE_Close'].value=1
render_views('Pose',[('Blink',(.60,-2.8,1.55),(0,0,1.54),.43)])
face.data.shape_keys.key_blocks['Fcl_EYE_Close'].value=0
face.data.shape_keys.key_blocks['Fcl_MTH_A'].value=.6
render_views('Pose',[('Speech',(.60,-2.8,1.55),(0,0,1.54),.43)])
face.data.shape_keys.key_blocks['Fcl_MTH_A'].value=0

# Bend stress check, not a claim of a finished gameplay animation.
rotate_world('J_Bip_L_UpperLeg',(1,0,0),-55)
rotate_world('J_Bip_L_LowerLeg',(1,0,0),80)
rotate_world('J_Bip_R_UpperArm',(0,1,0),-35)
rotate_world('J_Bip_R_LowerArm',(0,0,1),65)
render_views('Pose',[('JointBend',(.9,-3,1.15),(0,0,.93),1.94)])
relaxed()
for b in rig.pose.bones:b.keyframe_insert(data_path='rotation_quaternion',frame=1)
if rig.animation_data and rig.animation_data.action:rig.animation_data.action.name='Seoha | relaxed inspection pose'

# One camera and three lights are enough in the editable deliverable.
active=bpy.context.scene.camera
for ob in list(bpy.data.objects):
    if ob.type=='CAMERA' and ob!=active:bpy.data.objects.remove(ob,do_unlink=True)
    elif ob.type=='LIGHT' and ob.name not in ['Key','Fill','Rim']:bpy.data.objects.remove(ob,do_unlink=True)
active.location=(.6,-2.8,1.55)
active.rotation_euler=(Vector((0,0,1.475))-active.location).to_track_quat('-Z','Y').to_euler()
active.data.ortho_scale=.66
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
for img in bpy.data.images:
    if img.source=='FILE' and img.has_data:
        try:img.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Refined-RigReview.blend'))

report={'game_ready':False,'rig_bones':len(rig.data.bones),'expression_shapes':len(face.data.shape_keys.key_blocks)-1,
    'review_views':['Portrait','Body','Back','Blink','Speech','JointBend'],
    'mesh_objects':sum(o.type=='MESH' for o in bpy.context.scene.objects),
    'base_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),
    'source':'Seoha-Refined-RigReview.blend','final_gameplay_animations':False}
(OUT/'rig-review.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

"""Anatomical refinement of the actual VRoid-derived Seoha mesh.

The body/garment cage and rest skeleton use one continuous deformation. Face
landmark edits are evaluated independently for every preserved expression key.
No rendered reference image is projected onto the character.
"""
from pathlib import Path
import sys,math,json
import bpy,bmesh
from mathutils import Vector,Quaternion
sys.path.insert(0,str(Path(__file__).parent))
from vroid_review import setup

BASE=Path(__file__).resolve().parents[2]/'Artifacts/SeohaStudio/VRoid'
OUT=BASE/'Anatomy';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'Seoha-Refined-RigReview.blend'))
rig=bpy.data.objects['Armature'];face=bpy.data.objects['Face'];body=bpy.data.objects['Body']
rig.animation_data_clear()
for b in rig.pose.bones:
    b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
for k in face.data.shape_keys.key_blocks:k.value=0

from seoha_deformation import clamp,sstep,g,face_sculpt,body_cage

def transform(ob,func):
    matrix=ob.matrix_world.copy();inv=matrix.inverted()
    layers=[k.data for k in ob.data.shape_keys.key_blocks] if ob.data.shape_keys else [ob.data.vertices]
    for layer in layers:
        for v in layer:v.co=inv@func(matrix@v.co)
    if ob.data.shape_keys:
        for vertex,point in zip(ob.data.vertices,ob.data.shape_keys.key_blocks[0].data):vertex.co=point.co
    ob.data.update()

def skin_material(old):
    texture=old.node_tree.nodes.get('Mtoon1BaseColorTexture.Image') if old.node_tree else None
    if not texture or not texture.image:return old
    mat=bpy.data.materials.new(old.name+' | sculpt lighting');mat.use_nodes=True
    mat.diffuse_color=(.8,.62,.53,1);nodes=mat.node_tree.nodes;links=mat.node_tree.links
    bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.64
    bs.inputs['Specular IOR Level'].default_value=.28
    bs.inputs['Subsurface Weight'].default_value=.045
    bs.inputs['Subsurface Radius'].default_value=(.65,.32,.20)
    tex=nodes.new('ShaderNodeTexImage');tex.image=texture.image
    links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if 'restored exposed' not in old.name and 'Face_00_SKIN' not in old.name:
        links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
        mat.surface_render_method='DITHERED'
    return mat

skin_cache={}
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':continue
    for slot in ob.material_slots:
        mat=slot.material
        if mat and ('SKIN' in mat.name or 'restored exposed arm skin' in mat.name):
            if mat.name not in skin_cache:skin_cache[mat.name]=skin_material(mat)
            slot.material=skin_cache[mat.name]

def rotate_world(name,axis,angle):
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion()
    b.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(angle))@q

def relax():
    for b in rig.pose.bones:b.rotation_quaternion=Quaternion()
    for side,angle in [('L',67),('R',-67)]:
        rotate_world('J_Bip_'+side+'_UpperArm',(0,1,0),angle)
        rotate_world('J_Bip_'+side+'_LowerArm',(0,0,1),-8 if side=='L' else 8)

def render(prefix,warped=False,full=True):
    scene,cam,aim=setup();scene.view_settings.view_transform='AgX'
    scene.render.resolution_x=1100;scene.render.resolution_y=1300
    # Same lighting in before and after comparisons.
    bpy.data.objects['Key'].data.energy=450;bpy.data.objects['Fill'].data.energy=110
    bpy.data.objects['Rim'].data.energy=350
    views=[('Face',(0,-3,1.57),(0,0,1.55),.40),
      ('Quarter',(.86,-2.7,1.57),(0,0,1.53),.49),
      ('Profile',(3,-.1,1.55),(0,0,1.535),.45)]
    if full:views += [('Body',(.5,-3,1.05),(0,0,.86),1.88),('Torso',(.95,-3,1.27),(0,0,1.22),.78)]
    for label,pos,target,scale in views:
        target=Vector(target);pos=Vector(pos)
        if warped:
            delta=body_cage(target.copy())-target;pos+=delta;target+=delta
        cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
        scene.render.filepath=str(OUT/(prefix+'-'+label+'.png'));bpy.ops.render.render(write_still=True)

# The exact source receives the same new lighting for an honest form comparison.
relax()
if '--before' in sys.argv:render('Before',False,True)
for b in rig.pose.bones:b.rotation_quaternion=Quaternion()
# A local topology pass gives the visible abdomen enough vertices for the navel
# and oblique transitions, while preserving interpolated UVs and bone weights.
bm=bmesh.new();bm.from_mesh(body.data)
local_faces=[f for f in bm.faces if f.material_index==0 and all(1.025<v.co.z<1.245 and abs(v.co.x)<.152 for v in f.verts)]
edges={e for f in local_faces for e in f.edges}
if edges:bmesh.ops.subdivide_edges(bm,edges=list(edges),cuts=2,use_grid_fill=True)
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()
transform(face,face_sculpt)
for ob in list(bpy.context.scene.objects):
    if ob.type=='MESH':transform(ob,body_cage)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:
    b.head=body_cage(b.head.copy());b.tail=body_cage(b.tail.copy())
bpy.ops.object.mode_set(mode='OBJECT')
relax()
for image in bpy.data.images:
    if image.source=='FILE' and image.has_data:
        try:image.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Anatomy.blend'))
if '--no-render' not in sys.argv:render('Sculpt',True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Anatomy.blend'))
(OUT/'sculpt-report.json').write_text(json.dumps({'source':'Seoha-Refined-RigReview.blend',
  'bones':len(rig.data.bones),'expressions':len(face.data.shape_keys.key_blocks)-1,
  'art_target_achieved':False,'gameplay_connected':False,
  'edits':['chin/jaw planes','malar and suborbital volume','bridge/alar/lip projection',
  'shoulder/ribcage/pelvis','neck/leg proportions','abdomen/limb volume','rest skeleton alignment']},indent=2),encoding='utf8')

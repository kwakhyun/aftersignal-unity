"""Direct rest-mesh modelling, curve-designed hair and tailored costume study.

This is a private authoring source, not a quality-approved gameplay asset.
"""
from pathlib import Path
import sys,math,json
import bpy,bmesh
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from atelier_common import Modeler,reset_pose,relaxed,rotate,g,sstep
from atelier_hair import build_hair
from vroid_review import setup

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/SeohaStudio/VRoid/Atelier';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Artifacts/SeohaStudio/VRoid/Anatomy/Seoha-Anatomy-Review.blend'))
rig=bpy.data.objects['Armature'];body=bpy.data.objects['Body'];face=bpy.data.objects['Face']
reset_pose(rig,face)
for ob in bpy.context.scene.objects:
    if ob.type=='MESH':
        for mod in ob.modifiers:
            if mod.type=='NODES':mod.show_viewport=False;mod.show_render=False

def studio():
    scene,cam,aim=setup();scene.view_settings.view_transform='Standard'
    scene.eevee.taa_render_samples=96;scene.eevee.shadow_ray_count=4;scene.eevee.shadow_step_count=8
    scene.render.resolution_x=1100;scene.render.resolution_y=1300
    for name,power in [('Key',175),('Fill',100),('Rim',170)]:
        light=bpy.data.objects[name].data;light.energy=power;light.use_shadow_jitter=True
    return scene,cam,aim

scene,cam,aim=studio()
def render(name,pos,target,scale):
    cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)

if '--before' in sys.argv:
    relaxed(rig,face)
    render('Before-Portrait',(.58,-2.8,1.51),(0,0,1.315),.93)
    render('Before-Face',(0,-3,1.51),(0,0,1.506),.36)
    reset_pose(rig,face)

# Sculpt the existing expression topology. The same continuous field is
# evaluated for EVERY expression, so eyelid/lash/iris surfaces stay coherent.
for key in face.data.shape_keys.key_blocks:
    for v in key.data:
        p=v.co;x,y,z=p;ax=abs(x);side=1 if x>=0 else -1
        front=sstep((.01-y)/.04)
        eye=g(ax-.039,.030)*g(z-1.504,.024)*front
        p.z+=(1.504-z)*.18*eye
        p.z+=.0009*sstep((ax-.032)/.033)*eye
        p.z+=.0035*g(ax-.024,.014)*g(z-1.528,.012)*front
        p.x-=side*.0016*g(ax-.044,.028)*g(z-1.493,.046)*front
        # Refine the jaw below the cheekbone, and lengthen the chin slightly.
        p.x-=side*.0030*g(ax-.044,.023)*g(z-1.443,.023)*front
        p.z-=.0038*g(x,.046)*g(z-1.430,.022)*front
        p.y-=.0018*g(ax-.048,.021)*g(z-1.484,.017)*front
        p.y+=.0010*g(ax-.043,.021)*g(z-1.454,.017)*front
        p.y+=.0018*g(x,.009)*g(z-1.478,.008)*front
        p.y-=.0011*g(x,.009)*g(z-1.498,.016)*front
        # A restrained cupid bow and fuller lower lip, not a painted mouth.
        p.y-=.00065*g(ax-.005,.004)*g(z-1.454,.0025)*front
        p.y-=.00065*g(x,.012)*g(z-1.448,.003)*front
        p.z+=.0007*g(ax-.021,.005)*g(z-1.451,.004)*front
for v,p in zip(face.data.vertices,face.data.shape_keys.key_blocks[0].data):v.co=p.co
face.data.update()
# Keep the reference's crisp upper lashes, but reduce the dense stock lower
# lash fringe. The rest-space mask follows expressions without swimming.
lashmask=face.data.color_attributes.new(name='Atelier lash weight',type='FLOAT_COLOR',domain='POINT')
for i,v in enumerate(face.data.shape_keys.key_blocks[0].data):
    w=.20+.80*sstep((v.co.z-1.501)/.010)
    lashmask.data[i].color=(1,1,1,w)
mat=bpy.data.materials.new('Atelier | crisp restrained lash ink');mat.use_nodes=True
n=mat.node_tree.nodes;l=mat.node_tree.links;n.clear()
tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images['_09']
attr=n.new('ShaderNodeAttribute');attr.attribute_name='Atelier lash weight'
mult=n.new('ShaderNodeMath');mult.operation='MULTIPLY';l.new(tex.outputs['Alpha'],mult.inputs[0]);l.new(attr.outputs['Alpha'],mult.inputs[1])
ink=n.new('ShaderNodeEmission');ink.inputs[0].default_value=(.012,.008,.018,1)
transparent=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');out=n.new('ShaderNodeOutputMaterial')
l.new(mult.outputs[0],mix.inputs[0]);l.new(transparent.outputs[0],mix.inputs[1]);l.new(ink.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs[0])
mat.surface_render_method='DITHERED';face.data.materials[5]=mat
for mod in face.modifiers:
    if mod.type=='SUBSURF':mod.levels=2;mod.render_levels=2

modeler=Modeler(rig,body)
build_hair(modeler,face)
if '--hair-only' not in sys.argv:
    from atelier_outfit import build_outfit
    build_outfit(modeler,face)

relaxed(rig,face)
render('Atelier-Face',(0,-3,1.51),(0,0,1.503),.37)
render('Atelier-Quarter',(.95,-2.8,1.53),(0,0,1.497),.42)
if '--quick' not in sys.argv:
    render('Atelier-Profile',(3,-.1,1.53),(0,0,1.486),.45)
    render('Atelier-Back',(-.7,3,1.48),(0,0,1.475),.55)
    render('Atelier-Body',(.45,-3,1.01),(0,0,.84),1.84)
rotate(rig,'J_Bip_C_Head',(0,1,0),-4)
render('Atelier-Portrait',(.58,-2.8,1.51),(0,0,1.315),.93)
for img in bpy.data.images:
    if img.source=='FILE' and img.has_data:
        try:img.pack()
        except RuntimeError:pass
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
scene['quality_status']='Private work in progress; illustration target not yet approved.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Atelier.blend'))
report={'source':'Seoha-Anatomy-Review.blend','bones':len(rig.data.bones),'expression_shapes':len(face.data.shape_keys.key_blocks)-1,
    'art_target_achieved':False,'gameplay_connected':False,'full_actions_complete':False,
    'hair_design_paths':len(modeler.guides.objects),'method':'direct mesh sculpt edits, designed quad hair masses, UV surfaces and tailored separate clothing'}
(OUT/'atelier-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')

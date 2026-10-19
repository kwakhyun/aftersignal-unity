"""Bake the authored garment folds and inspect real expressions and rig poses."""
from pathlib import Path
import sys,json,math
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from atelier_common import reset_pose,relaxed,rotate,collection

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Artifacts/SeohaStudio/VRoid/Atelier'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Atelier.blend'))
rig=bpy.data.objects['Armature'];face=bpy.data.objects['Face'];scene=bpy.context.scene;cam=scene.camera
reset_pose(rig,face)
low=bpy.data.objects['Outfit | technical crop top LOW'];high=bpy.data.objects['Outfit | technical crop top HIGH']
col=high.users_collection[0]
if '--reuse-bake' in sys.argv:
    image=bpy.data.images.load(str(OUT/'Seoha-TechnicalTop-Normal.png'),check_existing=True)
    image.colorspace_settings.name='Non-Color';image.pack()
    mat=low.data.materials[0];nodes=mat.node_tree.nodes;links=mat.node_tree.links
    tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.name='BAKED garment folds'
    normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65
    links.new(tex.outputs['Color'],normal.inputs['Color'])
    bump=next(n for n in nodes if n.type=='BUMP');links.new(normal.outputs[0],bump.inputs['Normal'])
elif '--skip-bake' not in sys.argv:
    visibility={o:o.hide_render for o in scene.objects}
    for o in scene.objects:
        if o.type=='MESH':o.hide_render=o not in [low,high]
    col.hide_viewport=False;col.hide_render=False;high.hide_render=False
    mods={m:m.show_render for m in low.modifiers}
    for mod in low.modifiers:
        if mod.type in ['SOLIDIFY','ARMATURE']:mod.show_viewport=False;mod.show_render=False
    image=bpy.data.images.new('Seoha technical top | tangent normal',width=2048,height=2048,alpha=False,float_buffer=False)
    image.colorspace_settings.name='Non-Color'
    mat=low.data.materials[0];nodes=mat.node_tree.nodes;links=mat.node_tree.links
    target=nodes.new('ShaderNodeTexImage');target.name='BAKED garment folds';target.image=image;nodes.active=target
    for n in nodes:n.select=n==target
    bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True);bpy.context.view_layer.objects.active=low
    scene.render.engine='CYCLES';scene.cycles.samples=8
    scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.010;scene.render.bake.max_ray_distance=.020
    scene.render.bake.margin=16;scene.render.bake.normal_space='TANGENT'
    bpy.ops.object.bake(type='NORMAL')
    image.filepath_raw=str(OUT/'Seoha-TechnicalTop-Normal.png');image.file_format='PNG';image.save();image.pack()
    normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65
    links.new(target.outputs['Color'],normal.inputs['Color'])
    bump=next((n for n in nodes if n.type=='BUMP'),None)
    if bump:links.new(normal.outputs['Normal'],bump.inputs['Normal'])
    else:links.new(normal.outputs['Normal'],nodes.get('Principled BSDF').inputs['Normal'])
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.bake.use_selected_to_active=False
    for o,visible in visibility.items():o.hide_render=visible
    for mod,state in mods.items():mod.show_render=state;mod.show_viewport=state
    col.hide_viewport=True;col.hide_render=True
    low['baked_normal']='Seoha-TechnicalTop-Normal.png, 2048 square, tangent space, 16px margin.'
    low['high_source_vertices']=len(high.data.vertices)

errors=[];active=[]
for ob in scene.objects:
    if ob.type!='MESH' or ob.hide_render or any(c.hide_render for c in ob.users_collection):continue
    active.append(ob)
    for v in ob.data.vertices:
        if not all(math.isfinite(n) for n in v.co):errors.append(ob.name+': nonfinite vertex');break
        weight=sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name in rig.data.bones)
        if abs(weight-1)>.012:errors.append(ob.name+': unnormalized weights '+str(weight));break
    for mod in ob.modifiers:
        if mod.type=='ARMATURE' and mod.object!=rig:errors.append(ob.name+': wrong armature')
keys=face.data.shape_keys.key_blocks
if len(keys)!=58 or any(len(k.data)!=len(face.data.vertices) for k in keys):errors.append('Facial expression topology changed unexpectedly')
assert not errors,errors

def render(name,pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    if name in ['Review-Front','Review-Profile','Review-Back','Review-Body']:
        data=cam.data.copy();view=bpy.data.objects.new('Inspect | '+name,data);scene.collection.objects.link(view)
        view.matrix_world=cam.matrix_world.copy()

relaxed(rig,face)
render('Review-Front',(0,-3,1.51),(0,0,1.502),.39)
render('Review-Profile',(3,-.15,1.53),(0,0,1.49),.48)
render('Review-Back',(-.8,3,1.50),(0,0,1.48),.55)
render('Review-Body',(.45,-3,1.01),(0,0,.84),1.84)
if '--surface-review' not in sys.argv:
    keys['Fcl_EYE_Close'].value=1
    render('Review-Blink',(.60,-2.8,1.52),(0,0,1.485),.41)
    keys['Fcl_EYE_Close'].value=0;keys['Fcl_MTH_A'].value=.6
    render('Review-Speech',(.60,-2.8,1.52),(0,0,1.485),.41)
    relaxed(rig,face)
    rotate(rig,'J_Bip_L_UpperLeg',(1,0,0),-55);rotate(rig,'J_Bip_L_LowerLeg',(1,0,0),80)
    rotate(rig,'J_Bip_R_UpperArm',(0,1,0),-35);rotate(rig,'J_Bip_R_LowerArm',(0,0,1),65)
    render('Review-Joints',(.8,-3,1.13),(0,0,.88),1.88)
relaxed(rig,face)
rotate(rig,'J_Bip_C_Head',(0,1,0),-4)
render('Review-Portrait',(.58,-2.8,1.51),(0,0,1.315),.93)
scene.frame_start=scene.frame_end=1
refs=collection('00 REFERENCE | original Seoha illustration',True)
original=bpy.data.images.load(str(ROOT/'Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png'),check_existing=True);original.pack()
ref=bpy.data.objects.new('REFERENCE ONLY | original illustration',None);refs.objects.link(ref)
ref.empty_display_type='IMAGE';ref.data=original;ref.empty_display_size=1.7
ref.location=(-.90,.16,.85);ref.rotation_euler=(math.pi/2,0,0)
notes=bpy.data.texts.new('READ ME | Seoha atelier')
notes.write('Private work in progress. Original illustration target NOT approved.\n'
    '01 HAIR / 02 OUTFIT / 03 DETAILS: rendered, weighted meshes.\n'
    '00 AUTHORING: editable design paths, retained before curve-to-mesh style conversion; not a live modifier link.\n'
    '90 SCULPT: 47,829-vertex garment normal-bake source, hidden by default.\n'
    'Reset pose transforms for the rest pose. Face has 57 expression shapes.\n'
    'Inspection cameras and actual Review images are provided. Full gameplay actions and hair physics are unfinished.\n')
report={'structural_errors':errors,'bones':len(rig.data.bones),'expression_shapes':len(keys)-1,
    'visible_meshes':len(active),'visible_base_vertices':sum(len(o.data.vertices) for o in active),
    'garment_sculpt_vertices':len(high.data.vertices),'garment_low_vertices':len(low.data.vertices),
    'garment_normal_map':'Seoha-TechnicalTop-Normal.png','gameplay_connected':False,'full_actions_complete':False,
    'art_target_achieved':False,'limited_validation':'neutral, profile, back, full body, blink, speech, bent elbow/knee; structural skinning checks',
    'surface_only_final_pass':'--surface-review' in sys.argv,
    'expression_joint_note':'Expression and joint renders from previous pass retained when only rear scalp fitting changed.' if '--surface-review' in sys.argv else 'Expressions and joint poses rendered in this pass.',
    'limitations':['remaining hair-root and silhouette refinement','reference likeness and garment fit not final','full action animation, hair physics, engine material conversion and performance validation unfinished']}
(OUT/'atelier-review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Atelier-Review.blend'))
print(json.dumps(report))

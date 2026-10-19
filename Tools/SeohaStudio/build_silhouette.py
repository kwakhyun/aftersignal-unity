"""Private likeness revision. Preserve the previous Atelier source unchanged."""
from pathlib import Path
import sys,math,json
import bpy,bmesh
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from atelier_common import Modeler,reset_pose,relaxed,rotate,g,sstep,body_cage
from silhouette_hair import build_hair
from vroid_review import setup

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/SeohaStudio/VRoid/Silhouette';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Artifacts/SeohaStudio/VRoid/Atelier/Seoha-Atelier-Review.blend'))
rig=bpy.data.objects['Armature'];body=bpy.data.objects['Body'];face=bpy.data.objects['Face']
reset_pose(rig,face)

# Keep expression deltas and all existing rig attachments. These are direct
# rest-mesh edits, not a new face retopology or completed game animation set.
if '--hair-only' not in sys.argv:
    # Return to the authored, clean expression topology before rebuilding the
    # face. Successive narrow deformation fields in earlier studies produced
    # ripples; adding further smoothing to them lost the intended anatomy.
    with bpy.data.libraries.load(str(ROOT/'Artifacts/SeohaStudio/VRoid/Seoha-Imported.blend'),link=False) as (available,loaded):
        loaded.objects=['Face']
    source=loaded.objects[0]
    assert len(source.data.vertices)==len(face.data.vertices)
    for key in face.data.shape_keys.key_blocks:
        original=source.data.shape_keys.key_blocks[key.name]
        for v,start in zip(key.data,original.data):
            v.co=body_cage(start.co.copy())
    bpy.data.objects.remove(source,do_unlink=True)
    for key in face.data.shape_keys.key_blocks:
        for v in key.data:
            p=v.co;x,y,z=p;ax=abs(x);side=1 if x>=0 else -1
            front=sstep((.012-y)/.040)
            orbital=g(ax-.044,.030)*g(z-1.505,.027)*front
            p.z+=(1.504-z)*.18*orbital
            p.z+=.0023*sstep((ax-.029)/.038)*orbital
            p.x+=side*.0010*g(ax-.046,.026)*g(z-1.501,.033)*front
            # A tapered lower jaw and fuller high cheek, avoiding a round doll
            # chin and a uniformly flat cheek-to-mouth surface.
            p.x-=side*.0015*g(ax-.040,.029)*g(z-1.444,.030)*front
            p.y-=.0014*g(ax-.044,.028)*g(z-1.485,.026)*front
            p.z+=.0008*g(x,.026)*g(z-1.422,.021)*front
            # Smaller nose wings, a more defined bridge/tip and soft lip corners.
            p.y-=.0012*g(x,.013)*g(z-1.493,.024)*front
            p.x*=1+.08*g(x,.027)*g(z-1.451,.015)*front
            p.y-=.0014*g(x,.023)*g(z-1.451,.014)*front
            ear=sstep((ax-.069)/.022)*sstep((y+.002)/.028)
            p.x+=(side*.075-x)*.14*ear
    for v,p in zip(face.data.vertices,face.data.shape_keys.key_blocks[0].data):v.co=p.co
    face.data.update()

    bpy.context.view_layer.objects.active=face
    if face.data.has_custom_normals:bpy.ops.mesh.customdata_custom_splitnormals_clear()
    for p in face.data.polygons:p.use_smooth=True
    for e in face.data.edges:e.use_edge_sharp=False
    bm=bmesh.new();bm.from_mesh(face.data)
    skin_faces=[f for f in bm.faces if f.material_index==2]
    bmesh.ops.join_triangles(bm,faces=skin_faces,angle_face_threshold=math.radians(40),
        angle_shape_threshold=math.radians(50),cmp_uvs=True,cmp_vcols=False,
        cmp_seam=True,cmp_sharp=False,cmp_materials=True)
    bm.to_mesh(face.data);bm.free()
    face.data.update()

    # Reduce the mottled, baked illustration texture in skin-only areas. A
    # continuous rest-space colour field is smooth under subdivision and moves
    # with expressions. Preserve a small amount of the existing UV detail.
    attr=face.data.color_attributes.new(name='Silhouette skin paint',type='FLOAT_COLOR',domain='POINT')
    for i,v in enumerate(face.data.shape_keys.key_blocks[0].data):
        x,y,z=v.co;front=sstep((.008-y)/.04)
        cheek=g(abs(x)-.047,.024)*g(z-1.487,.018)*front
        lid=g(abs(x)-.041,.025)*g(z-1.517,.005)*front
        lips=g(x,.018)*g(z-1.450,.004)*front
        rgb=(.91-.035*cheek-.07*lips,.685-.10*cheek-.12*lid-.24*lips,.59-.045*cheek-.10*lid-.16*lips)
        attr.data[i].color=(*rgb,1)
    mat=face.data.materials[2].copy();mat.name='Silhouette | clean face shading';face.data.materials[2]=mat
    nodes=mat.node_tree.nodes;links=mat.node_tree.links
    tex=next(n for n in nodes if n.type=='TEX_IMAGE')
    destinations=list(tex.outputs['Color'].links)
    paint=nodes.new('ShaderNodeAttribute');paint.attribute_name='Silhouette skin paint'
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[0].default_value=.22
    links.new(paint.outputs['Color'],mix.inputs[1]);links.new(tex.outputs['Color'],mix.inputs[2])
    for link in destinations:links.new(mix.outputs[0],link.to_socket)

modeler=Modeler(rig,body);build_hair(modeler,face)
from silhouette_tailoring import refine_neckline
refine_neckline(modeler,rebuild_front='--experimental-tailoring' in sys.argv)
scene,cam,aim=setup();scene.eevee.taa_render_samples=64
scene.eevee.shadow_ray_count=4;scene.eevee.shadow_step_count=8
scene.render.resolution_x=1000;scene.render.resolution_y=1200
for name,power in [('Key',175),('Fill',100),('Rim',170)]:
    light=bpy.data.objects[name].data;light.energy=power;light.use_shadow_jitter=True

def render(name,pos,target,scale):
    cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)

relaxed(rig,face)
render('Review-Front',(0,-3,1.51),(0,0,1.502),.40)
render('Review-Quarter',(.95,-2.8,1.53),(0,0,1.498),.43)
if '--quick' not in sys.argv:
    render('Review-Profile',(3,-.15,1.53),(0,0,1.49),.48)
    render('Review-Back',(-.8,3,1.50),(0,0,1.48),.55)
    render('Review-Body',(.45,-3,1.01),(0,0,.84),1.84)
    keys=face.data.shape_keys.key_blocks
    keys['Fcl_EYE_Close'].value=1
    render('Review-Blink',(.60,-2.8,1.52),(0,0,1.485),.41)
    keys['Fcl_EYE_Close'].value=0;keys['Fcl_MTH_A'].value=.6
    render('Review-Speech',(.60,-2.8,1.52),(0,0,1.485),.41)
relaxed(rig,face);rotate(rig,'J_Bip_C_Head',(0,1,0),-4)
render('Review-Portrait',(.58,-2.8,1.51),(0,0,1.315),.93)
scene['quality_status']='Private likeness work in progress; reference target not achieved. Not activated in Unity.'
errors=[];active=[]
for ob in scene.objects:
    if ob.type!='MESH' or ob.hide_render or any(c.hide_render for c in ob.users_collection):continue
    active.append(ob)
    for vertex in ob.data.vertices:
        if not all(math.isfinite(v) for v in vertex.co):errors.append(ob.name+': nonfinite position');break
        weight=sum(w.weight for w in vertex.groups if ob.vertex_groups[w.group].name in rig.data.bones)
        if abs(weight-1)>.012:errors.append(ob.name+': invalid skin weight '+str(weight));break
keys=face.data.shape_keys.key_blocks
if len(keys)!=58 or any(len(k.data)!=len(face.data.vertices) for k in keys):errors.append('Expression topology inconsistency')
assert not errors,errors
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
report={'source':'Atelier/Seoha-Atelier-Review.blend','bones':len(rig.data.bones),
        'expression_shapes':len(face.data.shape_keys.key_blocks)-1,
        'new_hair_meshes':len(modeler.hair.objects),'gameplay_connected':False,
        'reference_quality_achieved':False,'full_actions_complete':False,
        'materials':[(i,m.name) for i,m in enumerate(face.data.materials)],
        'actual_render_engine':'Blender EEVEE','quick_review':'--quick' in sys.argv,
        'structural_errors':errors,'visible_base_vertices':sum(len(o.data.vertices) for o in active),
        'face_quad_polygons':sum(len(p.vertices)==4 for p in face.data.polygons if p.material_index==2),
        'face_base_vertices':len(face.data.vertices),
        'experimental_jacket_fronts':'--experimental-tailoring' in sys.argv,
        'limitations':['likeness and complete outfit still below illustration target',
            'full game actions, long hair dynamics and Unity materials not finished'],
        'face_source':'Clean authored VRoid expression mesh; broad continuous shaping and partial triangle-to-quad conversion'}
(OUT/'review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Silhouette-Review.blend'))
print(json.dumps(report))

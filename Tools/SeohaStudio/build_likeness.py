"""Reference-pose likeness review of the real, private Seoha model.

Every PNG is rendered from this rigged mesh; no illustration is projected onto
the face. A successful structural check is deliberately not a likeness pass.
"""
from pathlib import Path
import sys, math, json
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
from atelier_common import Modeler, reset_pose, relaxed, rotate, g, sstep
from vroid_review import setup

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/SeohaStudio/VRoid/Likeness'; OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Artifacts/SeohaStudio/VRoid/Silhouette/Seoha-Silhouette-Review.blend'))
rig=bpy.data.objects['Armature']; body=bpy.data.objects['Body']; face=bpy.data.objects['Face']
reset_pose(rig,face)

if '--baseline' not in sys.argv:
    # One continuous field changes the skin, eyelid, iris and all expressions
    # together. The outer corner is preserved while the lower lid is lifted.
    for key in face.data.shape_keys.key_blocks:
        for v in key.data:
            p=v.co; x,y,z=p; ax=abs(x); side=1 if x>=0 else -1
            front=sstep((.008-y)/.040)
            orbit=g(ax-.043,.032)*g(z-1.503,.023)*front
            p.z+=(1.509-z)*.19*orbit
            p.x-=side*.0020*orbit
            # Narrow the low cheek without carving sharp local dents.
            p.x-=side*.0028*g(ax-.046,.030)*g(z-1.458,.026)*front
            # Small mouth with a relaxed slight smile at the corners.
            mouth=g(x,.025)*g(z-1.450,.014)*front
            p.x*=1-.12*mouth
            p.z+=.0008*sstep(ax/.016)*mouth
            p.y-=.0007*g(x,.015)*g(z-1.450,.009)*front
            # Less prominent ears; their inner sculpt remains intact.
            ear=sstep((ax-.069)/.018)*sstep((y+.012)/.023)
            p.x+=(side*.076-x)*.27*ear
            p.z+=(1.488-z)*.10*ear
    for v,k in zip(face.data.vertices,face.data.shape_keys.key_blocks[0].data):v.co=k.co
    face.data.update()
    iris_ids=set(i for p in face.data.polygons if p.material_index==1 for i in p.vertices)
    for key in face.data.shape_keys.key_blocks:
        for side in [-1,1]:
            ids=[i for i in iris_ids if key.data[i].co.x*side>0]
            mid=Vector(((min(key.data[i].co.x for i in ids)+max(key.data[i].co.x for i in ids))/2,
                0,(min(key.data[i].co.z for i in ids)+max(key.data[i].co.z for i in ids))/2))
            for i in ids:
                p=key.data[i].co;p.x=mid.x+(p.x-mid.x)*.86;p.z=mid.z+(p.z-mid.z)*.90
    for v,k in zip(face.data.vertices,face.data.shape_keys.key_blocks[0].data):v.co=k.co
    face.data.update()
    # Rebuild skin shading instead of multiplying several generations of
    # painted ambient shadows. Colour is attached to the deforming mesh.
    attr=face.data.color_attributes.new(name='Likeness skin',type='FLOAT_COLOR',domain='POINT')
    for i,v in enumerate(face.data.vertices):
        x,y,z=v.co;front=sstep((.008-y)/.040)
        blush=g(abs(x)-.045,.023)*g(z-1.491,.015)*front
        lids=g(abs(x)-.043,.024)*g(z-1.519,.006)*front
        lips=g(x,.014)*g(z-1.450,.003)*front
        attr.data[i].color=(.91-.075*lips,.70-.115*blush-.10*lids-.22*lips,.625-.065*blush-.07*lids-.14*lips,1)
    mat=bpy.data.materials.new('Likeness | unified soft face');mat.use_nodes=True
    ns=mat.node_tree.nodes;ls=mat.node_tree.links;ns.clear()
    diffuse=ns.new('ShaderNodeBsdfDiffuse');diffuse.inputs[0].default_value=(1,1,1,1)
    st=ns.new('ShaderNodeShaderToRGB');ls.new(diffuse.outputs[0],st.inputs[0])
    bw=ns.new('ShaderNodeRGBToBW');ls.new(st.outputs[0],bw.inputs[0])
    ramp=ns.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=(.70,.51,.53,1)
    ramp.color_ramp.elements[1].position=.80;ramp.color_ramp.elements[1].color=(1,.98,.975,1)
    ls.new(bw.outputs[0],ramp.inputs[0])
    colour=ns.new('ShaderNodeAttribute');colour.attribute_name='Likeness skin'
    mul=ns.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1
    ls.new(colour.outputs[0],mul.inputs[1]);ls.new(ramp.outputs[0],mul.inputs[2])
    em=ns.new('ShaderNodeEmission');ls.new(mul.outputs[0],em.inputs[0])
    out=ns.new('ShaderNodeOutputMaterial');ls.new(em.outputs[0],out.inputs[0]);face.data.materials[2]=mat
    # A shaded oral cavity should not glow orange when speaking.
    mouth=face.data.materials[0].copy();mouth.name='Likeness | shaded oral cavity'
    old=next((n.image for n in mouth.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None)
    if old:
        ns=mouth.node_tree.nodes;ls=mouth.node_tree.links;ns.clear()
        tex=ns.new('ShaderNodeTexImage');tex.image=old
        mul=ns.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1
        mul.inputs[2].default_value=(.32,.17,.19,1);ls.new(tex.outputs[0],mul.inputs[1])
        em=ns.new('ShaderNodeEmission');ls.new(mul.outputs[0],em.inputs[0])
        out=ns.new('ShaderNodeOutputMaterial');ls.new(em.outputs[0],out.inputs[0]);face.data.materials[0]=mouth
    from likeness_hair import build_hair
    modeler=Modeler(rig,body);build_hair(modeler,face)
    from likeness_tailoring import rebuild
    rebuild(modeler)
    # Match the illustration's adult shoulder span. Move the rest skeleton and
    # all actual garment/skin vertices through the same continuous field.
    def shoulder(p):
        p=p.copy();x,y,z=p
        p.x+=(1 if x>0 else -1)*.028*sstep(abs(x)/.105)*g(z-1.35,.10)*(1-sstep((z-1.40)/.04))
        p.x+=(1 if x>0 else -1)*.060*sstep((abs(x)-.30)/.19)*sstep((z-1.22)/.08)*(1-sstep((z-1.40)/.04))
        return p
    bpy.context.view_layer.update()
    for ob in list(scene_ob for scene_ob in bpy.context.scene.objects if scene_ob.type=='MESH'):
        if ob==face or ob.name.startswith('Hair |') or ob.hide_render or any(c.hide_render for c in ob.users_collection):continue
        world=ob.matrix_world.copy();inv=world.inverted()
        if ob.data.shape_keys:
            for key in ob.data.shape_keys.key_blocks:
                for v in key.data:v.co=inv@shoulder(world@v.co)
            for v,k in zip(ob.data.vertices,ob.data.shape_keys.key_blocks[0].data):v.co=k.co
        else:
            for v in ob.data.vertices:v.co=inv@shoulder(world@v.co)
        ob.data.update()
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    rest={b.name:(b.head.copy(),b.tail.copy()) for b in rig.data.edit_bones}
    # Connected endpoints can update neighbouring bones in edit mode. Read
    # every source endpoint first so no joint receives the field twice.
    for b in rig.data.edit_bones:b.head=shoulder(rest[b.name][0]);b.tail=shoulder(rest[b.name][1])
    bpy.ops.object.mode_set(mode='OBJECT')
    # Keep the entire section of a hanging lock outside the finished clothing.
    # Applying separate per-vertex snaps would flatten or tear the hair surface.
    from mathutils.bvhtree import BVHTree
    bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();cvs=[];cfs=[]
    for ob in bpy.context.scene.objects:
        if ob.type!='MESH' or not ob.name.startswith(('Tailoring |','Outfit | technical crop top LOW')):continue
        ev=ob.evaluated_get(deps);mesh=ev.to_mesh();offset=len(cvs)
        cvs.extend(ev.matrix_world@v.co for v in mesh.vertices)
        cfs.extend(tuple(offset+i for i in p.vertices) for p in mesh.polygons);ev.to_mesh_clear()
    clothing=BVHTree.FromPolygons(cvs,cfs)
    for ob in bpy.context.scene.objects:
        if ob.type!='MESH' or not ob.name.startswith(('Hair | long framing lock','Hair | long split end')):continue
        nr=54;nc=12;shifts=[0.0]*nr
        for row in range(nr):
            points=[ob.data.vertices[row*nc+j].co for j in range(nc)]
            centre=sum(points,Vector())/nc
            if centre.z>1.405:continue
            for p in [centre,points[0],points[nc//2]]:
                hit=clothing.ray_cast(Vector((p.x,-.55,p.z)),Vector((0,1,0)),.7)[0]
                if hit is not None:shifts[row]=min(shifts[row],hit.y-.012-p.y)
        smooth=[min(shifts[max(0,i-2):min(nr,i+3)]) for i in range(nr)]
        for i,shift in enumerate(smooth):
            for j in range(nc):ob.data.vertices[i*nc+j].co.y+=shift
        ob.data.update()

scene,cam,aim=setup(); scene.eevee.taa_render_samples=64
scene.eevee.shadow_ray_count=4;scene.eevee.shadow_step_count=8
scene.render.resolution_x=1000;scene.render.resolution_y=1200
for name,power in [('Key',175),('Fill',100),('Rim',170)]:
    light=bpy.data.objects[name].data;light.energy=power;light.use_shadow_jitter=True

rendered=[]
def render(name,pos,target,scale):
    cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    rendered.append(name+'.png')

def reference_pose(with_arms=False):
    relaxed(rig,face)
    rotate(rig,'J_Bip_C_Neck',(0,1,0),4)
    rotate(rig,'J_Bip_C_Head',(0,1,0),16)
    for name in ['J_Sec_Hair3_02','J_Sec_Hair3_05']:
        rotate(rig,name,(0,1,0),-14)
    if with_arms:
        from likeness_pose import portrait_arms
        portrait_arms(rig)

tag='Baseline' if '--baseline' in sys.argv else 'Review'
relaxed(rig,face)
render(tag+'-Front',(0,-3,1.515),(0,0,1.502),.40)
render(tag+'-Quarter',(-1.0,-2.8,1.54),(0,0,1.497),.43)
reference_pose()
render(tag+'-Reference-Angle',(-.85,-3,1.54),(.022,0,1.485),.43)
if '--baseline' not in sys.argv:
    relaxed(rig,face)
    if '--quick' not in sys.argv:
        render('Review-Profile',(3,-.15,1.53),(0,0,1.49),.48)
        render('Review-Back',(-.8,3,1.5),(0,0,1.48),.55)
        render('Review-Body',(-.45,-3,1.04),(0,0,.84),1.84)
        face.data.shape_keys.key_blocks['Fcl_EYE_Close'].value=1
        render('Review-Blink',(-.6,-2.8,1.53),(0,0,1.49),.41)
        face.data.shape_keys.key_blocks['Fcl_EYE_Close'].value=0
        face.data.shape_keys.key_blocks['Fcl_MTH_A'].value=.6
        render('Review-Speech',(-.6,-2.8,1.53),(0,0,1.49),.41)
    reference_pose(with_arms=True)
    # Portrait lighting follows the illustration's dark teal setting. The
    # other eight views remain neutral studio renders for shape inspection.
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.004,.012,.013,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.50
    bpy.data.objects['Key'].data.color=(1,.88,.82)
    bpy.data.objects['Fill'].data.color=(.65,.76,1)
    bpy.data.objects['Rim'].data.color=(.06,.65,1);bpy.data.objects['Rim'].data.energy=230
    rimdata=bpy.data.lights.new('Portrait cyan rim','AREA');rimdata.energy=85;rimdata.color=(.04,.75,.86);rimdata.shape='DISK';rimdata.size=1.15
    rim=bpy.data.objects.new('Portrait cyan rim',rimdata);scene.collection.objects.link(rim);rim.location=(-1.2,.8,1.5);aim(rim,(0,0,1.25))
    scene.eevee.taa_render_samples=96
    render('Review-Portrait',(-.85,-3,1.47),(0,0,1.25),.84)
    errors=[]
    for ob in scene.objects:
        if ob.type!='MESH' or ob.hide_render or any(c.hide_render for c in ob.users_collection):continue
        for v in ob.data.vertices:
            if not all(math.isfinite(x) for x in v.co):errors.append(ob.name+': nonfinite mesh');break
            w=sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name in rig.data.bones)
            if abs(w-1)>.012:errors.append(ob.name+': invalid weights');break
    assert len(face.data.shape_keys.key_blocks)==58
    assert not errors,errors
    scene['quality_status']='Reference likeness NOT passed; private model work in progress.'
    report={'reference':'Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png',
      'reference_quality_achieved':False,'gameplay_connected':False,'full_actions_complete':False,
      'bones':len(rig.data.bones),'expression_shapes':len(face.data.shape_keys.key_blocks)-1,
      'structural_errors':errors,'quick_review':'--quick' in sys.argv,
      'renders_in_this_build':rendered,'validation_scope':'Finite weighted geometry, expression topology, portrait wrist targets, actual rendered views; not all game actions.',
      'lighting':'Front/quarter/reference-angle/profile/back/body/blink/speech: neutral. Portrait: illustration-inspired teal rim lighting.',
      'render_engine':'Blender EEVEE; actual mesh','reference_pose':'20 degree combined neck/head roll; camera on character right; analytically solved portrait arms',
      'limitations':['Single illustration does not establish exact side/back anatomy.',
      'Model identity, outfit and animation still require artistic work; no automatic likeness pass.']}
    (OUT/'review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Likeness-Review.blend'))
    print(json.dumps(report))

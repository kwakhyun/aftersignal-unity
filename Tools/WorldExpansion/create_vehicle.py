"""Original bevelled five-door electric sedan; Blender metres, X is the forward axis."""
import bpy,math,pathlib,bmesh
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];out=ROOT/'Assets/AfterSignal/Resources/WorldAssets/DetailedSedan';out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metal=0,rough=.35):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=(*color,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough;return m
paint=mat('BodyPaint',(.22,.3,.34),.72,.21);black=mat('TrimRubber',(.012,.017,.02),0,.5);alloy=mat('BrushedAlloy',(.52,.57,.58),.85,.25);glass=mat('Glazing',(.16,.29,.32),.1,.12);light=mat('Headlamp',(.65,.95,1),.1,.2);red=mat('Taillamp',(.85,.035,.03),.1,.2)
def finish(o,name,m,bevel=0):
    o.name=name;o.data.materials.append(m)
    if bevel:mod=o.modifiers.new('Manufactured edge radius','BEVEL');mod.width=bevel;mod.segments=3
    for p in o.data.polygons:p.use_smooth=True
    mod=o.modifiers.new('Weighted panel normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    return o
def box(name,at,size,m,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,bevel)
def mesh(name,v,f,m):
    data=bpy.data.meshes.new(name);data.from_pydata(v,[],f);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);return finish(o,name,m,.015)
def rod(name,a,b,r,m):
    d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,name,m,.008)
xs=[-2.48,-2.35,-1.95,-1.3,-.4,.65,1.3,1.9,2.4,2.53];width=[.64,.86,.99,1.01,1.0,.99,.99,.94,.81,.62];top=[.76,.94,1.02,1.04,1.02,1.03,1.05,1.0,.89,.68]
# Dense longitudinal rings and round wheel cut-outs keep silhouettes smooth.
controls=list(zip(xs,width,top))
def section(x):
    for k in range(1,len(controls)):
        a,b=controls[k-1],controls[k]
        if x<=b[0]:
            t=(x-a[0])/(b[0]-a[0]);return a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t
    return width[-1],top[-1]
xs=[-2.48+i*(5.01/96) for i in range(97)]
v=[]
for x in xs:
    w,z=section(x)
    wheel_dx=min(abs(x-1.6),abs(x+1.6))
    arch=.44+math.sqrt(max(0,.48*.48-wheel_dx*wheel_dx)) if wheel_dx<.48 else .3
    for y,h in [(-.72,.32),(-.93,.39),(-1,.62),(-.99,.82),(-.85,1),(.85,1),(.99,.82),(1,.62),(.93,.39),(.72,.32)]:
        v.append((x,y*w,max(h*z,arch) if abs(y)>.8 else h*z))
f=[]
for i in range(len(xs)-1):
    for j in range(10):f.append((i*10+j,i*10+(j+1)%10,(i+1)*10+(j+1)%10,(i+1)*10+j))
f.extend([tuple(range(9,-1,-1)),tuple(range((len(xs)-1)*10,len(xs)*10))]);mesh('Paint sculpted monocoque',v,f,paint)
box('Underbody skid plate',(0,0,.34),(4.25,1.55,.15),black)
# Hood and rear hatch panel breaks follow the curved coachwork.
mesh('Paint hood',[(.93,-.81,1.058),(.93,.81,1.058),(2.16,.77,.989),(2.16,-.77,.989)],[(0,1,2,3)],paint)
mesh('Paint hatch',[(-2.2,-.79,1),(-2.2,.79,1),(-1.48,.9,1.08),(-1.48,-.9,1.08)],[(0,1,2,3)],paint)
mesh('Paint roof',[(-1.06,-.78,1.74),(-1.06,.78,1.74),(-.65,.83,1.83),(.4,.8,1.83),(.7,.73,1.73),(.7,-.73,1.73),(.4,-.8,1.83),(-.65,-.83,1.83)],[(0,1,2,3,4,5,6,7)],paint)
mesh('Windshield',[(.7,-.73,1.73),(.7,.73,1.73),(1.29,.87,1.065),(1.29,-.87,1.065)],[(0,1,2,3)],glass)
mesh('Rear windshield',[(-1.09,-.77,1.74),(-1.5,-.87,1.075),(-1.5,.87,1.075),(-1.09,.77,1.74)],[(0,1,2,3)],glass)
for s in [-1,1]:
    # Separate glass apertures, B-pillars, doors, handles and weatherstrips.
    mesh('Side window front',[(.65,s*.756,1.72),(-.28,s*.832,1.77),(-.29,s*.985,1.065),(1.23,s*.865,1.065)],[(0,1,2,3)],glass)
    mesh('Side window rear',[(-.38,s*.834,1.77),(-1.07,s*.79,1.72),(-1.48,s*.9,1.07),(-.38,s*.985,1.065)],[(0,1,2,3)],glass)
    for name,a,b in [('A pillar',(.68,s*.75,1.74),(1.29,s*.88,1.05)),('C pillar',(-1.08,s*.79,1.75),(-1.51,s*.9,1.05)),('B pillar',(-.33,s*.83,1.78),(-.33,s*.99,1.0))]:rod('Paint '+name,a,b,.047 if name!='B pillar' else .035,paint if name!='B pillar' else black)
    rod('Window sill',(-1.53,s*.93,1.06),(1.3,s*.91,1.06),.028,alloy)
    for x in [-1.24,-.31,1.16]:rod('Door panel seam',(x,s*.99,.52),(x,s*.99,1.02),.009,black)
    for x in [-.99,.07]:box('Flush door handle',(x,s*1.014,.95),(.25,.028,.035),alloy,.012)
    rod('Side sill',(-1.15,s*.95,.39),(1.2,s*.95,.39),.045,black)
    mirror=box('Paint rearview mirror',(.9,s*1.17,1.19),(.28,.31,.14),paint,.055);box('Mirror glass',(.759,s*1.18,1.19),(.012,.25,.09),alloy,.008);rod('Mirror stalk',(.93,s*.87,1.11),(.93,s*1.15,1.16),.025,black)
    for x in [-1.6,1.6]:
        bpy.ops.object.empty_add(location=(x,s*1.01,.44));wheel=bpy.context.object;wheel.name='Wheel assembly'
        def parent(o):o.parent=wheel;o.matrix_parent_inverse=wheel.matrix_world.inverted();return o
        bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=12,location=(x,s*1.01,.44),major_radius=.325,minor_radius=.107,rotation=(math.pi/2,0,0));parent(finish(bpy.context.object,'Tyre sidewall',black))
        bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.29,depth=.18,location=(x,s*1.015,.44),rotation=(math.pi/2,0,0));parent(finish(bpy.context.object,'Brake rotor',alloy,.006))
        for spoke in range(10):
            a=spoke*math.tau/10;parent(rod('Alloy wheel spoke',(x+math.cos(a)*.075,s*1.12,.44+math.sin(a)*.075),(x+math.cos(a+.12)*.285,s*1.12,.44+math.sin(a+.12)*.285),.022,alloy))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.07,location=(x,s*1.13,.44));parent(finish(bpy.context.object,'Hub cap',alloy))
        for tread in range(36):
            a=tread*math.tau/36;rub=box('Tread groove',(x+math.cos(a)*.423,s*1.012,.44+math.sin(a)*.423),(.024,.18,.012),black,.001);rub.rotation_euler.y=-a;parent(rub)
    # Lamp housings with an internal LED grid and rear reflectors.
    box('Headlight housing',(2.36,s*.65,.78),(.17,.43,.2),black,.05)
    for j in range(5):box('Headlamp LED',(2.456,s*(.49+j*.075),.79),(.019,.042,.075),light,.012)
    box('Rear lamp housing',(-2.36,s*.66,.84),(.12,.5,.16),black,.04);box('Taillamp LED',(-2.428,s*.66,.855),(.019,.43,.052),red,.02)
    for axle in [-1.6,1.6]:
        for arc in range(18):
            a=arc*math.pi/18;b=(arc+1)*math.pi/18
            rod('Paint wheel arch',(axle+math.cos(a)*.485,s*.996,.44+math.sin(a)*.485),(axle+math.cos(b)*.485,s*.996,.44+math.sin(b)*.485),.024,paint)
box('Lower front intake',(2.464,0,.48),(.12,1.33,.22),black,.07)
for j in range(12):box('Intake vertical vane',(2.54,-.6+j*.109,.48),(.016,.027,.16),alloy,.005)
box('Rear diffuser',(-2.39,0,.43),(.16,1.4,.18),black,.04)
for y in [-.7,-.3,.3,.7]:box('Rear diffuser fin',(-2.43,y,.38),(.29,.035,.14),black,.006)
box('Rear license recess',(-2.441,0,.7),(.035,.46,.15),black,.015)
box('Front license plate',(2.545,0,.67),(.022,.45,.125),alloy,.005)
for s in [-1,1]:rod('Wiper arm',(.97,s*.3,1.4),(1.2,s*.55,1.16),.012,black)
box('Dashboard',(.9,0,1.02),(.45,1.55,.18),black,.06)
for x in [.25,-.65]:
    for s in [-1,1]:box('Leather seat',(x,s*.43,.64),(.54,.46,.12),black,.07);seat=box('Seat back',(x-.25,s*.43,.98),(.13,.44,.64),black,.07);seat.rotation_euler.y=-.1;box('Headrest',(x-.25,s*.43,1.34),(.14,.29,.22),black,.06)
# Orient shell faces outwards before baking the bevelled normals.
for o in list(bpy.context.scene.objects):
    if o.type!='MESH':continue
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    if o.name in ['Paint sculpted monocoque','Paint hood','Paint hatch','Paint roof','Windshield','Rear windshield'] or o.name.startswith('Side window'):
        for f in bm.faces:
            center=f.calc_center_median()
            if f.normal.dot(center-Vector((0,0,.85)))<0:f.normal_flip()
    bm.to_mesh(o.data);bm.free()
# Join by material and wheel assembly so moving wheels stay articulated, with low draw count.
groups={}
for o in list(bpy.context.scene.objects):
    if o.type!='MESH':continue
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH')
    key=(o.parent.name if o.parent else 'body',o.data.materials[0].name);groups.setdefault(key,[]).append(o)
for (parent,name),objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=name
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Documentation/WorldExpansion/DetailedSedan.blend'))
bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(out/'DetailedSedan.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
print('AUTHORED SEDAN',len(bpy.data.objects),sum(len(m.vertices) for m in bpy.data.meshes),flush=True)

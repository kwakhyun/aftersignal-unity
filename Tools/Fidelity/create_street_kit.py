"""Original bevelled, material-separated street kit. Blender 4.5, metres, Z up."""
import bpy, math, pathlib, random
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/Fidelity/StreetKit';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
M={}
for name,c,metal,rough in [('Alloy',(.32,.39,.42),.8,.3),('Graphite',(.035,.045,.055),.5,.35),('Ceramic',(.61,.65,.64),.08,.36),('Copper',(.39,.19,.095),.7,.3),('Glass',(.035,.15,.18),.5,.16),('Wood',(.18,.08,.035),0,.58),('Leaf',(.035,.15,.075),0,.72),('Soil',(.035,.027,.018),0,.95),('Cyan',(.015,.65,.8),.1,.25),('Amber',(.9,.38,.045),.1,.25)]:
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if name in ('Cyan','Amber'):p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=3
    M[name]=m
def box(n,p,s,mat='Graphite',bevel=.035,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=n;o.dimensions=s
    if rot:o.rotation_euler=rot
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(M[mat])
    if bevel:
        m=o.modifiers.new('Manufactured rounded edges','BEVEL');m.width=min(bevel,min(s)*.35);m.segments=3
        bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');m.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=m.name)
    return o
def tube(n,a,b,r,mat='Alloy',vertices=16,r2=None):
    direction=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r if r2 is None else r2,depth=direction.length,location=(Vector(a)+Vector(b))*.5)
    o=bpy.context.object;o.name=n;o.rotation_euler=direction.to_track_quat('Z','Y').to_euler();o.data.materials.append(M[mat])
    for f in o.data.polygons:f.use_smooth=len(f.vertices)==4
    return o
def export(name):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for o in meshes:o.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,object_types={'MESH'},bake_anim=False,path_mode='AUTO')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def bench(at=(0,0,0)):
    x,y,z=at
    for xx in (-.9,.9):
        box('Cast bench leg',(x+xx,y,z+.24),(.15,.55,.48),'Graphite')
        tube('Armrest front',(x+xx,y-.2,z+.43),(x+xx,y-.2,z+.75),.035)
        tube('Armrest',(x+xx,y-.2,z+.75),(x+xx,y+.25,z+.75),.035)
    for i in range(7):box('Individual wood seat slat',(x,y-.28+i*.085,z+.49),(2.2,.065,.055),'Wood',.015)
    for i in range(4):box('Angled backrest slat',(x,y+.28+i*.025,z+.69+i*.10),(2.2,.055,.075),'Wood',.012,(-.15,0,0))
bench();export('PromenadeBench')
for x in (-2.3,2.3):
    tube('Shelter structural leg',(x,.75,0),(x,.75,2.8),.085)
    box('Ground anchor',(x,.75,.045),(.42,.4,.09),'Alloy')
box('Cantilever canopy',(0,.05,2.85),(5.6,2.8,.18),'Ceramic',.08)
box('Recessed canopy',(0,.05,2.74),(5.35,2.57,.09),'Graphite')
for x in (-2.57,2.57):box('Canopy edge light',(x,.05,2.83),(.055,2.72,.055),'Cyan',.01)
for x in (-1.8,0,1.8):box('Tinted windbreak',(x,1.12,1.5),(1.7,.035,2.35),'Glass',.01)
bench((-.7,.30,0));box('Live transit display',(1.84,.82,1.8),(.88,.22,1.35),'Graphite',.07)
box('Screen luminous backing',(1.84,.685,1.8),(.76,.035,1.19),'Cyan',.008)
for i in range(6):box('Departure row',(1.82,.661,2.17-i*.145),(.55,.012,.025),'Graphite',0)
export('TransitShelter')
box('Kiosk pedestal',(0,0,.10),(1.05,.72,.2),'Alloy',.06);box('Rounded equipment enclosure',(0,0,1.3),(.88,.48,2.4),'Graphite',.09)
box('Civic display bezel',(0,-.27,1.66),(.78,.10,1.25),'Alloy',.03);box('Civic display',(0,-.33,1.67),(.67,.025,1.09),'Cyan',.015)
for i in range(6):box('Display line',(-.02,-.35,2.0-i*.12),(.48,.012,.032),'Graphite',0)
box('Contactless reader',(.22,-.29,.82),(.18,.07,.16),'Amber',.02)
for i in range(9):box('Enclosure vent',(0,-.253,.3+i*.031),(.5,.014,.012),'Alloy',0)
export('CivicKiosk')
box('Charging foundation',(0,0,.06),(.72,.64,.12),'Ceramic');box('Charger curved housing',(0,0,.8),(.43,.38,1.5),'Graphite',.12)
box('Charger status',(0,-.21,1.24),(.31,.035,.40),'Cyan',.04)
for i in range(18):
    a=math.pi*i/18;b=math.pi*(i+1)/18
    tube('Braided charging cable',(.30+math.sin(a)*.23,-.05,.96-math.cos(a)*.65),(.30+math.sin(b)*.23,-.05,.96-math.cos(b)*.65),.018,'Graphite',8)
box('Charge plug',(.32,-.1,.93),(.12,.09,.25),'Alloy',.03);export('ChargePoint')
box('Bollard base',(0,0,.08),(.42,.42,.16),'Alloy');tube('Impact bollard',(0,0,.08),(0,0,.98),.10,'Graphite',20)
for z in (.7,.8):tube('Bollard marker',(0,0,z),(0,0,z+.035),.106,'Amber',20)
export('SmartBollard')
box('HVAC casing',(0,0,.65),(1.7,.85,1.3),'Ceramic',.07)
for x in (-.43,.43):
    tube('Fan surround',(x,-.43,.75),(x,-.47,.75),.34,'Graphite',32)
    for i in range(12):
        a=i*math.pi/6;tube('Fan safety grill',(x+math.sin(a)*.32,-.50,.75+math.cos(a)*.32),(x-math.sin(a)*.32,-.50,.75-math.cos(a)*.32),.008,'Alloy',6)
    tube('Fan hub',(x,-.51,.75),(x,-.54,.75),.07,'Alloy')
for x in (-.7,.7):box('Rubber isolation mount',(x,0,.06),(.17,.7,.12),'Graphite')
export('ClimateUnit')
box('Tree planter rim',(0,0,.3),(2.3,2.3,.6),'Ceramic',.13);box('Inlaid soil',(0,0,.62),(2.05,2.05,.035),'Soil',.06)
random.seed(711)
for i in range(6):tube('Palm fibrous trunk',(.10*math.sin(i*.35),0,.62+i*.62),(.10*math.sin((i+1)*.35),0,.62+(i+1)*.62),.14-i*.011,'Wood',14,r2=.14-(i+1)*.011)
for i in range(22):
    a=i*math.pi*2/22;radius=2.0+(i%3)*.22;top=4.5
    points=[]
    for j in range(12):
        t=j/11;points.append((math.cos(a)*radius*t,math.sin(a)*radius*t,top+.9*math.sin(t*math.pi)-.65*t))
    for j in range(11):
        tube('Palm frond stem',points[j],points[j+1],.019,'Leaf',6)
        p=Vector(points[j]);width=.37*math.sin((j+1)/12*math.pi)
        for side in (-1,1):
            tip=p+Vector((-math.sin(a)*width*side,math.cos(a)*width*side,-.18));back=Vector(points[j+1]);mesh=bpy.data.meshes.new('Palm leaf blade');mesh.from_pydata([p,tip,back],[],[(0,1,2),(2,1,0)]);mesh.materials.append(M['Leaf']);o=bpy.data.objects.new('Palm feather leaflet',mesh);bpy.context.collection.objects.link(o)
export('CoastalPalm')
print('STREET KIT COMPLETE',flush=True)

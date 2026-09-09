"""Original cybernetic security vehicles and articulated response robots. Blender 4.5."""
import bpy,math,pathlib
R=pathlib.Path(__file__).resolve().parents[2]
OUT=R/'Assets/AfterSignal/Resources/Security';OUT.mkdir(parents=True,exist_ok=True)
ART=R/'Artifacts/CyberConflict/Models';ART.mkdir(parents=True,exist_ok=True)
def mat(n,c,metal=.5,rough=.4):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
armor=mat('SecurityArmor',(.13,.18,.17));black=mat('SecurityCarbon',(.028,.036,.045));white=mat('SecurityCeramic',(.64,.72,.76),.25);steel=mat('SecuritySteel',(.32,.39,.43),.85,.3);red=mat('SecurityRed',(.44,.035,.065));cyan=mat('SecurityCyan',(.05,.65,.8));glass=mat('SecurityGlass',(.09,.18,.21),.25,.2);rubber=mat('SecurityRubber',(.018,.024,.028),.05,.85)
def pivot(n,at,p=None):
 o=bpy.data.objects.new(n,None);bpy.context.collection.objects.link(o);o.location=at;o.parent=p;return o
def cube(n,at,sz,m,p=None,rot=(0,0,0),bevel=.055):
 bpy.ops.mesh.primitive_cube_add();o=bpy.context.object;o.name=n;o.scale=tuple(v/2 for v in sz);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.location=at;o.rotation_euler=rot;o.parent=p;o.data.materials.append(m)
 if n in ('Faceted thorax','Thigh plate','Shin plate','Sensor shell','Shoulder pauldron','Forearm weapon mount'):
  for v in o.data.vertices:
   if n=='Faceted thorax' and v.co.z<0:v.co.x*=.64;v.co.y*=.76
   elif n=='Sensor shell':
    if v.co.z>0:v.co.x*=.74;v.co.y*=.83
   elif n!='Faceted thorax' and v.co.z<0:v.co.x*=.73
  bevel=min(bevel,.04)
 if bevel:
  mod=o.modifiers.new('Machined edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
def cyl(n,at,r,d,m,p=None,rot=(0,0,0),vertices=20):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d);o=bpy.context.object;o.name=n;o.location=at;o.rotation_euler=rot;o.parent=p;o.data.materials.append(m);mod=o.modifiers.new('Edge bevel','BEVEL');mod.width=.025;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name);return o
def clear():bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def vehicle(gang=False):
 root=pivot('VehicleRoot',(0,0,0));body=red if gang else armor
 cube('Ballistic lower hull',(0,0,.82),(7.4 if not gang else 5.6,2.6 if not gang else 2.1,.68),body,root,bevel=.2)
 cube('V hull belly',(-.3,0,.57),(5.8 if not gang else 4.8,1.8,.45),black,root,rot=(0,.08,0),bevel=.14)
 # Truck cab has individual windows and a short sloped nose; troop bay is behind it.
 cab=1.75 if not gang else .3
 cube('Sloped hood',(2.9 if not gang else 1.8,0,1.18),(1.65,2.38 if not gang else 1.95,.4),body,root,rot=(0,-.12,0),bevel=.12)
 cube('Cab roof',(cab,0,2.55 if not gang else 1.93),(1.8 if not gang else 2.5,2.45 if not gang else 1.95,.2),body,root)
 height=2.1 if not gang else 1.6
 for s in [-1,1]:
  y=s*(1.21 if not gang else .98)
  cube('Driver door armor',(cab,y,1.35 if not gang else 1),(1.65 if not gang else 2.2,.1,.68),body,root)
  cube('Ballistic side window',(cab,y,height),(1.25 if not gang else 1.8,.045,.54),glass,root,bevel=.015)
  for x in [cab-.75,cab+.75]:cube('Reinforced pillar',(x,y,height),(.13,.13,.83),steel,root)
  cube('Mirror housing',(cab+.9,y+s*.23,height),(.32,.18,.26),black,root)
  cube('Side running board',(.1,y+s*.22,.65),(5 if not gang else 3.6,.37,.12),steel,root)
  for x in [-2.7,-1.4,-.1]:
   if not gang:cube('Sloped side armor',(x,s*1.34,1.58),(1.2,.16,1.6),body,root,rot=(s*-.12,0,0));cube('Armor bolt',(x,s*1.44,1.45),(.12,.09,.12),steel,root)
  cube('Headlight cluster',(3.68 if not gang else 2.72,s*.79,1.13),(.06,.65,.15),white,root,bevel=.015)
  cube('Rear brake light',(-3.67 if not gang else -2.71,s*.84,1.25),(.07,.28,.15),red,root,bevel=.02)
  for x in ([-2.55,-.7,2.35] if not gang else [-1.75,1.8]):
   wheel=pivot('Wheel', (x,s*(1.24 if not gang else 1.06),.64 if not gang else .48),root)
   cyl('All terrain tire',(0,0,0),.65 if not gang else .49,.38,rubber,wheel,(math.pi/2,0,0),32)
   cyl('Forged hub',(0,s*.22,0),.37 if not gang else .31,.1,steel,wheel,(math.pi/2,0,0),24)
   cyl('Axle cap',(0,s*.3,0),.13,.12,black,wheel,(math.pi/2,0,0))
   for j in range(16):
    a=j*math.tau/16;rr=.63 if not gang else .48
    cube('Tread block',(math.cos(a)*rr,0,math.sin(a)*rr),(.14,.39,.065),black,wheel,rot=(0,-a+math.pi/2,0),bevel=.009)
  cube('Wheel arch rail',(-.1,s*1.28,1.22),(6.6 if not gang else 4.7,.12,.16),black,root)
 front=cab+.85
 cube('Front ballistic glass',(front,0,height),(.045,2.1 if not gang else 1.7,.59),glass,root,rot=(0,-.15,0),bevel=.02)
 cube('Windshield divider',(front+.04,0,height),(.08,.08,.7),steel,root)
 cube('Impact bumper',(3.8 if not gang else 2.85,0,.77),(.25,2.72 if not gang else 2.2,.29),steel,root)
 for z in [1.15,1.29,1.43]:cube('Radiator grill',(3.72 if not gang else 2.8,0,z),(.045,.9,.065),black,root,bevel=.01)
 if not gang:
  cube('Troop compartment',(-1.6,0,1.75),(4,2.55,1.9),body,root,bevel=.18)
  cube('Roof service rails',(-1.8,0,2.76),(3.7,2.38,.13),steel,root)
  ramp=pivot('RearRamp',(-3.65,0,.58),root);cube('Ramp armor',(0,0,1.04),(.18,2.3,2.03),body,ramp)
  for s in [-1,1]:cube('Ramp handrail',(-.13,s*.78,1.12),(.09,.075,.64),steel,ramp)
  for i in range(6):cube('Cooling louvre',(-2.8+i*.3,-1.3,2.18),(.16,.07,.27),black,root)
  cyl('Turret ring',(-.65,0,2.9),.64,.25,black,root)
  cube('Remote weapon station',(-.55,0,3.18),(.9,.68,.48),body,root)
  cyl('Remote barrel',(.25,0,3.23),.07,1.2,steel,root,(0,math.pi/2,0))
 else:
  cube('Rear sloping deck',(-1.9,0,1.15),(1.5,1.95,.43),red,root,rot=(0,.15,0))
  for s in [-1,1]:cube('External roll cage',(-.6,s*1.03,1.76),(3.7,.12,.14),steel,root)
  cube('Roof assault turret',(-.5,0,2.16),(.88,.67,.38),black,root)
  for s in [-1,1]:cyl('Twin smartgun',(0,s*.16,2.15),.055,1.3,steel,root,(0,math.pi/2,0))
  for i in range(4):cube('Illuminated grille blade',(2.85,(i-1.5)*.22,1.5),(.03,.08,.14),red,root,bevel=.005)
 for s in [-1,1]:cyl('Communications aerial',(-2.5,s*.83,3.2 if not gang else 2.45),.016,1,black,root)
 return root
def robot(military):
 root=pivot('RobotRoot',(0,0,0));m=armor if military else white;scale=1.22 if military else 1
 hip=pivot('Hip',(0,0,1.45),root);cube('Pelvic armor',(0,0,0),(.75,.51,.43),m,hip)
 torso=pivot('Torso',(0,0,2.07),root)
 cube('Faceted thorax',(0,0,0),(1.06,.68,.86),m,torso,bevel=.16)
 cube('Chest inset',(0,.355,.1),(.68,.065,.4),black,torso)
 for s in [-1,1]:
  cube('Overlapping chest lamella',(s*.25,.39,-.04),(.46,.105,.22),m,torso,rot=(0,s*.12,s*.19),bevel=.025)
  cube('Chest intake grille',(s*.4,.28,.25),(.12,.14,.27),black,torso,rot=(0,0,s*.22),bevel=.012)
  for j in range(4):cube('Intake cooling blade',(s*.4,.359,.16+j*.06),(.095,.018,.02),steel,torso,bevel=.003)
  cyl('Chest fastener',(s*.29,.427,-.09),.025,.026,steel,torso,(math.pi/2,0,0),10)
 for i in range(3):cube('Status light',((i-1)*.18,.405,.15),(.11,.03,.055),cyan if not military else red,torso,bevel=.008)
 for s in [-1,1]:
  cyl('Spine hydraulic actuator',(s*.3,-.19,-.42),.07,.65,steel,torso)
  thigh=pivot('Thigh_'+str(s),(s*.31,0,1.35),root)
  cyl('Hip bearing',(0,0,0),.19,.35,black,thigh,(0,math.pi/2,0))
  cube('Thigh plate',(0,0,-.28),(.38,.46,.64),m,thigh,rot=(0,s*.05,0),bevel=.09)
  cube('Thigh insert',(0,.246,-.27),(.15,.055,.36),black,thigh,bevel=.015)
  knee=pivot('Shin_'+str(s),(0,0,-.55),thigh)
  cyl('Knee bearing',(0,0,0),.15,.4,steel,knee,(0,math.pi/2,0))
  cube('Knee shield',(0,.25,.02),(.37,.16,.28),m,knee)
  cube('Shin plate',(0,.02,-.3),(.31,.36,.62),m,knee,bevel=.08)
  cube('Tibial raised ridge',(0,.234,-.31),(.09,.07,.44),steel,knee,bevel=.009)
  for j in range(3):cube('Shin articulation slot',(0,.214,-.48+j*.07),(.23,.025,.025),black,knee,bevel=.004)
  cyl('Leg actuator',(s*.17,-.08,-.26),.045,.53,steel,knee)
  cube('Articulated foot',(0,.15,-.67),(.4,.74,.22),black,knee,bevel=.07)
  arm=pivot('Arm_'+str(s),(s*.75,0,2.3),root)
  cube('Shoulder pauldron',(0,0,0),(.48,.67,.49),m,arm,bevel=.1)
  cyl('Exposed shoulder bearing',(s*.25,0,0),.16,.08,steel,arm,(0,math.pi/2,0),16)
  cube('Shoulder inlay',(0,.35,.04),(.31,.025,.06),cyan if not military else red,arm,bevel=.006)
  cyl('Arm joint',(0,0,-.27),.12,.4,steel,arm)
  cube('Forearm weapon mount',(0,.14,-.62),(.3,.48,.55),m,arm)
  cube('Weapon heat sink',(s*.18,.16,-.57),(.055,.29,.31),black,arm,bevel=.012)
  for j in range(4):cube('Heat sink rib',(s*.215,.16,-.69+j*.07),(.035,.27,.027),steel,arm,bevel=.004)
  for i in range(3 if military else 1):cyl('Weapon barrel',((i-1)*.07,.56,-.55),.045,.8,black,arm,(math.pi/2,0,0))
  if s==1 and not military:cube('Ballistic shield',(0,.44,-.5),(.64,.1,1.05),white,arm,bevel=.07);cube('Shield lens',(0,.505,-.3),(.43,.04,.14),cyan,arm)
 head=pivot('Sensor',(0,0,2.73),root);cube('Sensor shell',(0,0,0),(.51,.43,.41),m,head,bevel=.09)
 cyl('Neck swivel',(0,0,-.24),.12,.18,steel,head)
 for s in [-1,1]:
  cube('Angular cheek guard',(s*.2,.24,-.08),(.12,.12,.2),m,head,rot=(0,s*.32,0),bevel=.014)
  cyl('Temple sensor',(s*.25,-.02,0),.07,.04,steel,head,(0,math.pi/2,0),12)
 cube('Visor',(0,.232,.025),(.44,.045,.14),black,head)
 for i in range(3):cube('Optical sensor',((i-1)*.12,.263,.025),(.065,.028,.065),red if military else cyan,head,bevel=.018)
 cyl('Radar mast',(.16,-.1,.24),.02,.37,steel,head)
 cube('Power backpack',(0,-.47,2.07),(.64,.31,.7),black,root)
 for s in [-1,1]:cube('Back cooling fins',(s*.39,-.42,2.2),(.16,.29,.57),steel,root)
 if military:
  for s in [-1,1]:
   cube('Shoulder missile pod',(s*.7,-.17,2.64),(.5,.82,.36),armor,root)
   for i in range(3):cyl('Pod launch tube',(s*.7+(i-1)*.12,.26,2.64),.048,.08,black,root,(math.pi/2,0,0))
 root.scale=(scale,)*3;return root
def save(name,root):
 # Join static meshes per parent/material: preserve useful joints while reducing renderer count.
 groups={}
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH':groups.setdefault((o.parent,o.data.materials[0].name),[]).append(o)
 for (parent,_),objects in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();objects[0].name=(parent.name if parent else 'Body')+'_'+objects[0].data.materials[0].name
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL')
 bpy.ops.wm.save_as_mainfile(filepath=str(ART/(name+'.blend')))
 # Neutral studio thumbnail from an authored perspective.
 world=bpy.context.scene.world or bpy.data.worlds.new('Studio');bpy.context.scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.075,.095,.13,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
 from mathutils import Vector
 target=Vector((0,0,1.6));bpy.ops.object.camera_add(location=(10,-11,7) if 'Robot' not in name else (5,7,4));cam=bpy.context.object;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=10 if 'Robot' not in name else 5.6;bpy.context.scene.camera=cam
 for pos,energy,size in [((3,4,9),1800,7),((-5,-4,5),1400,6)]:
  bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1000;scene.render.resolution_y=750;scene.render.resolution_percentage=100;scene.render.filepath=str(ART/(name+'.png'));bpy.ops.render.render(write_still=True)
for name,kind in [('MilitaryCarrier',0),('GangInterceptor',1),('PoliceRobot',2),('MilitaryRobot',3)]:
 clear();root=vehicle(kind==1) if kind<2 else robot(kind==3);save(name,root)

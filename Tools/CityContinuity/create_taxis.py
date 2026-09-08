"""Original rounded hydrofoil and electric ducted-fan taxi models, metres, +X nose."""
import bpy,math
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def mat(name,color,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.3;return m
paint=mat('BodyPaint',(.71,.73,.68),.4);trim=mat('TrimRubber',(.035,.06,.07));alloy=mat('BrushedAlloy',(.3,.46,.48),.8);glass=mat('Glazing',(.08,.22,.28),.3);light=mat('Headlamp',(.15,.9,.8));red=mat('Taillamp',(.9,.08,.1))
def finish(o,name,m):
 o.name=name;o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 return o
def box(name,at,size,m,bevel=.08):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);finish(o,name,m)
 if bevel:
  mod=o.modifiers.new('Manufactured edge radius','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def loft(name,sections,m):
 verts=[];faces=[];n=24
 for x,y,z,ry,rz in sections:
  for j in range(n):
   a=math.tau*j/n;verts.append((x,y+math.cos(a)*ry,z+math.sin(a)*rz))
 for i in range(len(sections)-1):
  for j in range(n):a=i*n+j;b=i*n+(j+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(reversed(range(n))),tuple((len(sections)-1)*n+j for j in range(n))]);mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);finish(o,name,m);return o
def export(name):
 fans=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Electric fan rotor')]
 for fan in fans:
  bpy.ops.object.select_all(action='DESELECT');fan.select_set(True)
  for child in list(fan.children):child.select_set(True)
  bpy.context.view_layer.objects.active=fan;bpy.ops.object.join()
 for material in [paint,trim,alloy,glass,light,red]:
  pieces=[o for o in bpy.context.scene.objects if o.type=='MESH' and o not in fans and o.data.materials and o.data.materials[0]==material]
  if not pieces:continue
  bpy.ops.object.select_all(action='DESELECT')
  for o in pieces:o.select_set(True)
  bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();bpy.context.object.name=material.name+' coachwork'
 out=ROOT/'Assets/AfterSignal/Resources/WorldAssets'/name;out.mkdir(parents=True,exist_ok=True)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE')
def reset():bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
reset()
for side in [-1,1]:
 loft('Wave piercing pontoon',[(x,side*2, .1,ry,rz) for x,ry,rz in [(-7.7,.05,.05),(-6,.48,.52),(4,.55,.55),(6,.32,.42),(7.9,.015,.05)]],alloy)
 box('Hydrofoil strut',(0,side*2,-.6),(.23,.18,1.4),alloy)
box('Underwater lifting foil',(0,0,-1.2),(1.4,6,.13),alloy)
loft('Passenger deck',[(-6.7,0,.8,.2,.15),(-5.8,0,.8,2.3,.3),(4.8,0,.8,2.3,.3),(7,0,.8,.15,.12)],paint)
loft('Panoramic saloon',[(-5.2,0,2,.2,.15),(-4.5,0,2,2.02,1.17),(3.8,0,2,2.02,1.17),(5.2,0,1.9,.25,.3)],glass)
box('Floating saloon roof',(-.25,0,3.22),(9.5,3.95,.16),paint,.16)
for side in [-1,1]:
 for i in range(7):box('Window mullion',(-4.5+i*1.4,side*2,2.04),(.07,.08,2.18),alloy,.02)
 box('Cyan service stripe',(-.1,side*2.14,1.14),(10.2,.045,.11),light,.02)
 for n in range(2):box('Bow running light',(5.6,side*1.15,1.12),(.25,.7,.08),light,.03)
for i in range(12):
 at=(-4+(i//2)*1.25,(-1 if i%2 else 1)*1.2,1.36);box('Passenger seat',at,(.75,.72,.2),trim);box('Seat back',(at[0]-.32,at[1],1.85),(.15,.72,.92),trim)
box('Pilot console',(4,0,1.9),(.75,2,.9),trim);box('Service TAXI beacon',(0,0,3.45),(1.9,.55,.3),light)
export('WaterTaxi')
reset()
loft('Autonomous passenger capsule',[(-4.7,0,1.1,.1,.1),(-3.4,0,1.25,1.25,.65),(1.9,0,1.25,1.4,.65),(3.8,0,1.13,.1,.2)],paint)
loft('Continuous canopy',[(-2.7,0,1.9,.25,.1),(-1.9,0,2,1.18,.75),(1.8,0,2,1.18,.65),(3,0,1.8,.2,.1)],glass)
box('Floating canopy rail',(-.1,0,2.73),(4.2,1.1,.11),paint)
for x in [-2.6,2.3]:
 for side in [-1,1]:
  box('Swept outrigger',(x,side*1.9,1.15),(1.25,2,.17),paint)
  bpy.ops.mesh.primitive_torus_add(major_radius=.93,minor_radius=.19,major_segments=40,minor_segments=10,location=(x,side*3,1.16));finish(bpy.context.object,'Ducted fan nacelle',paint)
  bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=.22,depth=.32,location=(x,side*3,1.18));hub=finish(bpy.context.object,'Electric fan rotor',alloy)
  for blade in range(6):
   b=box('Rotor blade',(x,side*3,1.2),(.82,.13,.035),trim,.015);b.rotation_euler.z=blade*math.tau/6;b.location.x+=math.cos(b.rotation_euler.z)*.43;b.location.y+=math.sin(b.rotation_euler.z)*.43;b.parent=hub;b.matrix_parent_inverse=hub.matrix_world.inverted()
  box('Landing shoe',(x,side*1.6,.1),(1.8,.2,.2),alloy)
  box('Landing strut',(x,side*1.6,.58),(.12,.15,.9),alloy)
for side in [-1,1]:
 box('Door seal',(-.3,side*1.25,1.5),(.05,.07,1.4),trim,.01)
 box('Luminous route band',(-.3,side*1.35,.95),(5,.05,.07),light,.02)
 box('Navigation light',(2.5,side*3,1.22),(.28,.12,.09),light)
 for x in [-.7,1.7]:box('Cabin seat',(x,side*.55,1.3),(.8,.7,.22),trim);box('Seat back',(x-.35,side*.55,1.73),(.15,.7,1),trim)
box('Autonomy lidar',(0,0,2.93),(.5,.5,.3),trim);box('Front light signature',(3.2,0,1.2),(.12,1.5,.08),light)
export('AirTaxi')
print('TAXI_MODELS_READY')

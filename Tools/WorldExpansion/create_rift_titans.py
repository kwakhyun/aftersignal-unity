"""Original articulated creatures: morphology studies, no third-party geometry.
Blender +Y forward, Z up. All details join per animated pivot/material.
"""
import bpy, math, random, pathlib
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/AfterSignal/Resources/Creatures'
ART=ROOT/'Artifacts/IncursionCampaign/Models'
OUT.mkdir(parents=True,exist_ok=True);ART.mkdir(parents=True,exist_ok=True)
random.seed(3717)
def material(name,color,metal=.0,rough=.5,emit=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emit:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
 return m
hide=material('RiftHide',(.12,.095,.15),.15,.67)
plate=material('RiftPlate',(.17,.23,.27),.5,.4)
bone=material('RiftBone',(.5,.43,.35),.2,.5)
core=material('RiftCore',(.45,.035,.72),.1,.3,.85)
def pivot(name,at,parent=None):
 p=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(p);p.location=at;p.parent=parent;return p
def mesh(name,v,f,mat,parent=None):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(mat);o.parent=parent
 for p in m.polygons:p.use_smooth=True
 return o
def tube(name,points,radii,mat,parent=None,segments=14,ripple=0):
 # Smooth biological joints, preserving deliberate hard carapace ridges.
 if len(points)>2:
  pp=[];rr=[]
  for k in range(len(points)-1):
   a=Vector(points[max(0,k-1)]);b=Vector(points[k]);c=Vector(points[k+1]);d=Vector(points[min(len(points)-1,k+2)])
   for j in range(5):
    t=j/5;pp.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));rr.append(radii[k]*(1-t)+radii[k+1]*t)
  pp.append(points[-1]);rr.append(radii[-1]);points,radii=pp,rr
 v=[];f=[]
 for i,point in enumerate(points):
  p=Vector(point);d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);d.normalize();u=d.cross(Vector((0,0,1)))
  if u.length<.01:u=d.cross(Vector((0,1,0)))
  u.normalize();w=d.cross(u).normalized()
  for j in range(segments):
   a=j*math.tau/segments;r=radii[i]*(1+ripple*math.sin(j*4.3+i*1.73));v.append(p+(u*math.cos(a)+w*math.sin(a))*r)
 for i in range(len(points)-1):
  for j in range(segments):a=i*segments+j;b=i*segments+(j+1)%segments;f.append((a,b,b+segments,a+segments))
 f.extend([tuple(range(segments-1,-1,-1)),tuple(range((len(points)-1)*segments,len(points)*segments))]);return mesh(name,v,f,mat,parent)
def ellipsoid(name,at,scale,mat,parent=None,ripple=.03):
 v=[];f=[];n=32;h=18
 for i in range(h+1):
  a=math.pi*i/h
  for j in range(n):
   b=j*math.tau/n;r=1+ripple*math.sin(i*2.8+j*2.1);v.append((at[0]+math.sin(a)*math.cos(b)*scale[0]*r,at[1]+math.sin(a)*math.sin(b)*scale[1]*r,at[2]+math.cos(a)*scale[2]*r))
 for i in range(h):
  for j in range(n):a=i*n+j;b=i*n+(j+1)%n;f.append((a,b,b+n,a+n))
 return mesh(name,v,f,mat,parent)
def claw(at,direction,size,parent):
 a=Vector(at);d=Vector(direction);tube('Carved hooked talon',[a,a+d*size*.35+Vector((0,0,size*.3)),a+d*size*.8,a+d*size],[size*.23,size*.19,size*.09,.012],bone,parent,16)
def make(kind):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 root=pivot('TitanRoot',(0,0,0));height=[8.4,5.3,6.7][kind];thick=[1.75,2.4,1.8][kind]
 torso=pivot('Thorax',(0,0,height-1.4),root)
 ellipsoid('Fibrous thoracic muscle',(0,-.1,0),(thick,1.5,2.5 if kind==0 else 1.7),hide,torso,.07)
 for row in range(7):
  z=-1.7+row*.53;w=thick*(.72+math.sin(row*.44)*.22)
  for side in [-1,1]:
   tube('Overlapping rib armour',[(side*.25,.9,z+.12),(side*w*.7,1.14,z),(side*w, .45,z-.15),(side*w,-.65,z+.2)],[.14,.23,.29,.18],plate,torso,16,.1)
   tube('Exposed neural seam',[(side*.16,1.3,z),(side*w*.65,1.35,z-.17),(side*w*.9,.6,z-.24)],[.05,.04,.01],core,torso,8)
 for side in [-1,1]:
  ellipsoid('Shoulder carapace',(side*(thick-.15),-.05,1.35),(1.12,1.36,.78),plate,torso)
  for j in range(5):claw((side*(thick-.6+j*.3),-.2-j*.17,1.8),(side*.5,-.5,1),1.1+j*.2,torso)
 head=pivot('Cranium',(0,1, height+1.1 if kind==0 else height+.65),root)
 ellipsoid('Elongated skull',(0,.65,.1),(1.05,1.65,.72),plate,head)
 ellipsoid('Open maw',(0,1.77,-.25),(.74,.56,.48),hide,head)
 for side in [-1,1]:
  tube('Split mandibular jaw',[(side*.85,.7,-.15),(side*.83,1.9,-.42),(side*.33,2.28,-.38)],[.27,.3,.15],plate,head)
  claw((side*.68,-.2,.3),(side*.65,-1,.8),2.5 if kind==2 else 1.9,head)
  for j in range(8):claw((side*(.25+j*.075),1.35+j*.085,-.04),(0,.1,-1),.4+j*.04,head)
  for j in range(3):ellipsoid('Sensory eyes',(side*(.82+j*.12),.83-j*.3,.31),(.10,.17,.075),core,head)
 ellipsoid('Laser organ',(0,2.23,.15),(.26,.24,.18),core,head)
 ellipsoid('Exposed resonance heart',(0,1.43,-.1),(.42,.27,.88),core,torso)
 legs=2 if kind==0 else 4 if kind==1 else 6
 for i in range(legs):
  side=-1 if i%2==0 else 1;row=i//2;z=height-3 if kind==0 else height-1.6
  p=pivot('Limb_'+str(i),(side*(1.1 if kind==0 else 1.6),(-row+.5)*1.6,z),root)
  knee=(side*(.75 if kind==0 else 2.9),-.8, -z*.5);ankle=(side*(.5 if kind==0 else 4.1),1.1,-z+.55)
  tube('Load bearing limb',[(0,0,0),knee,ankle],[.72,.53,.25],hide,p,22,.1)
  ellipsoid('Knee armour',knee,(.69,.75,.61),plate,p)
  tube('Limb ridge',[(side*.3,0,.1),Vector(knee)+Vector((0,.35,.23)),Vector(ankle)+Vector((0,.25,.12))],[.29,.35,.19],plate,p)
  for j in [-1,0,1]:claw(Vector(ankle)+Vector((j*.22,.1,-.22)),(j*.32,1,-.18),1.2,p)
  for j in range(3):claw(Vector(knee)+Vector((side*.2,-j*.2,.1)),(side*.7,-.4,.6),.9+j*.2,p)
 if kind==0:
  for side in [-1,1]:
   p=pivot('Arm_'+str(side),(side*2,0,8.1),root)
   tube('Rending forearm',[(0,0,0),(side*1.2,.1,-2),(side*.8,1.15,-3.6)],[.77,.65,.38],hide,p,24,.08)
   ellipsoid('Bladed forearm',(side*1.1,.4,-2.5),(.67,.8,1.45),plate,p)
   for j in range(4):claw((side*.8+j*.22,1.3,-3.5),(side*.12,1,-.65),1.9,p)
 tail=pivot('Tail',(0,-1,height-2.2),root)
 for j in range(9):
  y=-j*.82;z=-j*.21
  ellipsoid('Tail vertebra',(math.sin(j*.6)*.3,y,z),(.8-j*.07,.65,.63-j*.035),plate,tail)
  claw((math.sin(j*.6)*.3,y,z+.4),(0,-.4,1),1.1-j*.06,tail)
 # Individually overlapping scales break up broad smooth surfaces and create real normal detail.
 for row in range(12):
  for col in range(16):
   a=col*math.tau/16;z=-1.9+row*.34;r=thick*(.75+.16*math.sin(row*.28))
   if math.sin(a)>.45:continue
   ellipsoid('Interlocking dermal scute',(math.cos(a)*r,math.sin(a)*1.45,z),(.24,.19,.31),plate,torso,.07)
 for side in [-1,1]:
  for j in range(12):
   y=-.5+j*.2;ellipsoid('Skull shield scute',(side*(.5+.18*math.sin(j)),y,.63),(.22,.28,.09),plate,head,.1)
  tube('Jaw tendon',[(side*.73,.35,-.22),(side*.78,.9,-.48),(side*.65,1.7,-.55)],[.13,.11,.05],hide,head)
  for j in range(7):claw((side*(.16+j*.085),1.65+j*.085,-.45),(0,.4,1),.3+j*.025,head)
 if kind==1:
  head.scale=(1.6,1.15,1.1)
  for j in range(7):ellipsoid('Bastion dorsal shield',(0,-.5-j*.42,1.8-j*.1),(1.6-j*.12,.45,.44),plate,torso)
 if kind==2:
  head.scale=(.9,1.4,1.3)
  for j in range(8):
   a=j*math.tau/8;claw((math.cos(a)*1.2,0,math.sin(a)*.7),(math.cos(a),-.7,math.sin(a)),2.3,head)
 # Join per articulation and material: a bounded draw count instead of hundreds of primitives.
 groups={}
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH':groups.setdefault((o.parent.name,o.data.materials[0].name),[]).append(o)
 for (pn,mn),objects in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=mn+'_'+pn
 name=['RiftReaver','RiftBehemoth','RiftOracle'][kind]
 source=ROOT/'Documentation/IncursionCampaign/Models';source.mkdir(parents=True,exist_ok=True)
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(source/(name+'.blend')))
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.data.materials.append(material('Ground',(.025,.04,.06)))
 bpy.ops.object.camera_add(location=(19,25,15));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,4.7))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=22;scene.camera=camera
 for at,color,power,size in [((8,10,18),(.66,.81,1),5000,10),((-9,4,10),(.5,.22,1),3500,8),((1,-12,13),(1,.45,.2),5500,7)]:
  bpy.ops.object.light_add(type='AREA',location=at);l=bpy.context.object;l.data.energy=power;l.data.color=color;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,5))-l.location).to_track_quat('-Z','Y').to_euler()
 scene.world.color=(.22,.22,.22);scene.render.filepath=str(ART/(name+'.png'));bpy.ops.render.render(write_still=True)
for k in range(3):make(k)

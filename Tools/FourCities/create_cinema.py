"""Render an original, redistributable short film for the in-world cinemas.
All geometry, camera motion and soundtrack are authored by this script.
Blender 4.5 LTS; no third-party movie, texture, font, or music assets.
"""
import bpy, math, random, wave, struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/StreamingAssets/Cinema';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE_NEXT'
scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100
scene.render.fps=24;scene.frame_start=1;scene.frame_end=1440
scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM';scene.render.ffmpeg.ffmpeg_preset='GOOD'
scene.render.ffmpeg.audio_codec='AAC';scene.render.filepath=str(OUT/'SignalTide.mp4')
scene.world.color=(.006,.018,.03)
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.025,.08,.12,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.view_settings.view_transform='AgX'
def mat(name,col,metal=0,emit=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.28
 if emit:p.inputs['Emission Color'].default_value=(*col,1);p.inputs['Emission Strength'].default_value=emit
 return m
white=mat('Pearl alloy',(.45,.62,.64),.65);black=mat('Pressure carbon',(.018,.065,.095),.7);blue=mat('Cyan biolight',(.03,.78,1),.2,4);amber=mat('Warm habitation',(.9,.42,.12),.15,3);coral=mat('Coral light',(.6,.04,.15),.2,2);ground=mat('Seabed',(.024,.06,.073))
def box(name,p,size,m,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 if bevel:mod=o.modifiers.new('Soft architectural edges','BEVEL');mod.width=bevel;mod.segments=2
 return o
def line(name,points,radius,m,closed=False):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=radius;c.bevel_resolution=2;s=c.splines.new('POLY');s.points.add(len(points)-1)
 for p,v in zip(s.points,points):p.co=(*v,1)
 s.use_cyclic_u=closed;o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);return o
box('Abyss plain',(0,0,-4),(650,650,5),ground)
random.seed(811)
for i in range(85):
 a=i*2.39996;r=26+(i%9)*12;x=math.cos(a)*r;y=math.sin(a)*r;h=random.uniform(7,39);w=random.uniform(5,10)
 box('Habitat', (x,y,h/2),(w,w*.85,h),black,.6)
 for f in range(1,int(h/3)):
  z=f*3;box('Overhanging terrace',(x,y,z),(w+1.2,w*.85+1.2,.28),white,.1)
  for side in [-1,1]:box('Light band',(x+side*(w/2+.03),y,z+1),(.08,w*.65,.4),amber if i%3 else blue)
 box('Solar crown',(x,y,h+.35),(w+.4,w+.4,.7),white,.2)
for radius in [22,58,98,133]:
 line('Connected habitat ring',[(math.cos(i*math.tau/128)*radius,math.sin(i*math.tau/128)*radius,.2) for i in range(128)],.55,white,True)
 line('Transit light',[(math.cos(i*math.tau/128)*radius,math.sin(i*math.tau/128)*radius,1.4) for i in range(128)],.09,blue,True)
for a in range(0,360,30):
 a=math.radians(a);pts=[]
 for t in range(41):
  f=t*math.pi/80;pts.append((math.cos(a)*155*math.cos(f),math.sin(a)*155*math.cos(f),math.sin(f)*65))
 line('Pressure dome rib',pts,.23,white)
for z in [18,38,55]:
 r=155*math.sqrt(1-(z/65)**2);line('Dome circulation',[(r*math.cos(i*math.tau/128),r*math.sin(i*math.tau/128),z) for i in range(128)],.12,blue,True)
for i in range(12):
 a=i*math.tau/12;line('Heart spiral',[(math.cos(a+t*.08)*(12-t*.05),math.sin(a+t*.08)*(12-t*.05),t*.6) for t in range(85)],.36,coral if i%3==0 else white)
line('Central beacon',[(0,0,0),(0,0,90)],.8,blue)
for i in range(30):
 a=i*math.tau/30;x=math.cos(a)*190;y=math.sin(a)*190
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=(x,y,20+i%8*5));fish=bpy.context.object;fish.name='Original mechanical manta';fish.scale=(2.8,1,.18);fish.data.materials.append(white)
 for f in [1,720,1440]:fish.location=(math.cos(a+f*.0008)*190,math.sin(a+f*.0008)*190,20+i%8*5+math.sin(f*.003+i)*5);fish.rotation_euler[2]=a+f*.0008;fish.keyframe_insert('location',frame=f);fish.keyframe_insert('rotation_euler',frame=f)
for p,power,color in [((30,-90,110),280000,(.35,.8,1)),((-110,40,75),160000,(.1,.65,.9)),((50,100,45),85000,(1,.35,.1))]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.color=color;o.data.shape='DISK';o.data.size=120;o.rotation_euler=(Vector((0,0,15))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.lens=27;camera.data.clip_end=1500
for frame,eye,target in [(1,(205,-205,100),(0,0,18)),(240,(160,-175,61),(0,0,22)),(480,(96,-92,32),(0,0,20)),(720,(48,-63,10),(0,0,21)),(960,(-20,-60,14),(0,0,24)),(1200,(-94,-117,42),(0,0,20)),(1440,(-220,-180,98),(0,0,18))]:
 camera.location=eye;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.keyframe_insert('location',frame=frame);camera.keyframe_insert('rotation_euler',frame=frame)
# Titles live in camera space and are authored with Blender's bundled typeface.
for body,pos,size in [('SIGNAL / TIDE',(-.82,.43,-2.5),.09),('AN ORIGINAL AFTERSIGNAL FILM',(-.82,.32,-2.5),.034),('NEREID  /  MEMORY BELOW THE SEA',(-.82,-.46,-2.5),.028)]:
 c=bpy.data.curves.new('Film title','FONT');c.body=body;c.size=size;c.extrude=.0001;o=bpy.data.objects.new(body,c);bpy.context.collection.objects.link(o);o.parent=camera;o.location=pos;o.data.materials.append(blue)
# Original quiet ambient soundtrack, no recordings or samples.
sound=OUT/'SignalTide-original.wav';rate=24000
with wave.open(str(sound),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate)
 for n in range(rate*60):
  t=n/rate;fade=min(1,t/3,(60-t)/4);tone=sum(math.sin(math.tau*h*t)*amp for h,amp in [(110,.11),(164.8138,.065),(220,.025),(293.6648,.019)]);pulse=.65+.35*math.sin(t*.42)**2;f.writeframesraw(struct.pack('<h',int(32000*tone*pulse*fade)))
ed=scene.sequence_editor_create();ed.strips.new_sound('Original underwater ambience',str(sound),channel=1,frame_start=1)
scene.render.use_sequencer=False
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Artifacts/FourCities/SignalTide.blend'))
bpy.ops.render.render(animation=True)

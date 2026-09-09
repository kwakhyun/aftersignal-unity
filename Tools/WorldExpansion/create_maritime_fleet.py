"""Original playable near-future coast guard cutter, destroyer and sea raider. Blender 4.5."""
import bpy, bmesh, math, pathlib
from mathutils import Vector
R=pathlib.Path(__file__).resolve().parents[2]
OUT=R/'Assets/AfterSignal/Resources/Maritime';OUT.mkdir(parents=True,exist_ok=True)
ART=R/'Artifacts/LivingHarbor/Models';ART.mkdir(parents=True,exist_ok=True)
helpers=(R/'Tools/WorldExpansion/create_security_units.py').read_text(encoding='utf8').split('def vehicle(')[0]
ns={ '__file__':str(R/'Tools/WorldExpansion/create_security_units.py') };exec(helpers,ns)
cube,cyl,pivot,clear=[ns[k] for k in ('cube','cyl','pivot','clear')]
armor,black,white,steel,red,cyan,glass,rubber=[ns[k] for k in ('armor','black','white','steel','red','cyan','glass','rubber')]
def ship(kind):
 clear();length=[28,72,18][kind];beam=[6.4,12,4.5][kind];height=[3,5.5,2][kind];root=pivot('MaritimeHull',(0,0,0));body=[white,armor,black][kind]
 verts=[];faces=[]
 sections=[(-.5,.68),(-.42,1),(-.1,1),(.2,.88),(.4,.5),(.52,.015)]
 for x,w in sections:
  for y,z in [(-.38,-1.1),(-.5,.4),(-.46,1),(.46,1),(.5,.4),(.38,-1.1)]:verts.append((x*length,y*beam*w,z*height*.65))
 for i in range(len(sections)-1):
  for j in range(6):a=i*6+j;b=i*6+(j+1)%6;faces.append((a,b,b+6,a+6))
 faces+=[tuple(range(5,-1,-1)),tuple(range(30,36))];m=bpy.data.meshes.new('Faceted displacement hull');m.from_pydata(verts,[],faces);m.update();bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();m.update();o=bpy.data.objects.new('Chined seaworthy hull',m);bpy.context.collection.objects.link(o);o.data.materials.append(body);o.parent=root
 deck=height*.66;cab=-length*.1
 cube('Raised armored superstructure',(cab,0,deck+height*.6),(length*.27,beam*.67,height*1.1),body,root,bevel=.18)
 cube('Bridge roof',(cab+length*.045,0,deck+height*1.23),(length*.22,beam*.73,.24),white if kind==0 else steel,root)
 for s in [-1,1]:
  cube('Bridge panoramic glazing',(cab+length*.045,s*beam*.34,deck+height*.99),(length*.17,.05,height*.3),glass,root)
  for i in range(8):
   x=(-.4+i*.09)*length;y=s*beam*.45
   cyl('Deck rail post',(x,y,deck+.55),.035,1.1,steel,root,vertices=8)
   if i<7:cube('Deck safety rail',(x+length*.045,y,deck+1.08),(length*.09,.04,.04),steel,root,bevel=0)
  for i in range(4):cyl('Liferaft canister',(-length*.28+i*length*.07,s*beam*.33,deck+.45),.36,.8,white,root,(math.pi/2,0,0))
  for i in range(3):cube('Navigation light',(-length*.15+i*length*.14,s*beam*.44,deck+.25),(.3,.08,.1),red if s<0 else cyan,root)
  cube('Waterline rubbing belt',(-length*.02,s*beam*.5,.15),(length*.7,.14,.3),red if kind==0 else black,root)
 cube('Forward bridge glass',(cab+length*.139,0,deck+height*.96),(.05,beam*.62,height*.31),glass,root)
 mast=pivot('RadarMast',(cab-length*.035,0,deck+height*1.25),root)
 cyl('Mast',(0,0,height*.75),.13,height*1.5,steel,mast)
 radar=pivot('RotatingRadar',(0,0,height*1.6),mast);cube('Radar array',(0,0,0),(.25,beam*.8,.28),white,radar)
 for s in [-1,1]:cyl('Whip antenna',(0,s*beam*.22,height*.55),.022,height*1.2,black,mast)
 turret=pivot('DeckTurret',(length*.25,0,deck+.24),root);cyl('Turret ring',(0,0,.3),beam*.15,.5,steel,turret)
 cube('Gun shield',(0,0,.7),(beam*.35,beam*.29,.9),body,turret,bevel=.14)
 for s in [-1,1]:cyl('Naval barrel',(beam*.4,s*.22,.84),.07,beam*.6,black,turret,(0,math.pi/2,0))
 if kind==1:
  for i in range(3):
   for j in [-1,1]:cube('Vertical launch hatch',(length*.08+i*2.4,j*1.9,deck+.06),(1.9,2.6,.13),steel,root)
  cube('Stern flight deck',(-length*.35,0,deck+.025),(length*.24,beam*.82,.12),steel,root)
  for y in [-2,2]:cube('Helipad marking',(-length*.35,y,deck+.095),(5,.15,.015),white,root,bevel=0)
  cube('Helipad crossbar',(-length*.35,0,deck+.095),(.16,4,.015),white,root,bevel=0)
 else:
  for s in [-1,1]:cube('Waterjet engine housing',(-length*.48,s*beam*.22,.4),(1.2,.8,1.2),black,root)
 for i in range(8):cube('Deck service hatch',(-length*.36+i*length*.068,0,deck+.06),(length*.055,beam*.38,.12),steel,root)
 name=['CoastGuardCutter','NavalDestroyer','PirateInterceptor'][kind]
 # Join static surfaces by parent/material. Preserve radar and gun pivots.
 groups={}
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH':groups.setdefault((o.parent.name if o.parent else '',o.data.materials[0].name if o.data.materials else ''),[]).append(o)
 for objects in groups.values():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(ART/(name+'.blend')))
 scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1100;scene.render.resolution_y=700;scene.render.resolution_percentage=100
 scene.world.color=(.25,.25,.25)
 bpy.ops.object.light_add(type='AREA',location=(length*.2,-length*.3,length*.75));bpy.context.object.data.energy=20000*(length/28)**2;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=length
 bpy.ops.object.camera_add(location=(length*.75,-length*.75,length*.48));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,2))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=length*1.4;scene.camera=camera
 scene.render.filepath=str(ART/(name+'.png'));bpy.ops.render.render(write_still=True)
for kind in range(3):ship(kind)

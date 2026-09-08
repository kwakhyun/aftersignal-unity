"""Editable anime character trial: anatomical topology, fitted proportions and illustrated surfacing."""
import bpy,math,json,bmesh
import numpy as np
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Artifacts/CharacterLab';OUT.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seo-Anatomy.blend'))
body=bpy.data.objects.get('Seoha anatomical foundation');rig=body.parent
if not rig or rig.type!='ARMATURE':rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
for m in list(body.modifiers):
 if m.type=='ARMATURE':body.modifiers.remove(m)
bpy.ops.object.convert(target='MESH');body=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
oldbones={b.name:(rig.matrix_world@b.head_local,rig.matrix_world@b.tail_local) for b in rig.data.bones}
# A smooth landmark deformation keeps the connected, production anatomical topology.
source=[];target=[]
def pin(a,b):source.append(a);target.append(b)
for side in [-1,1]:
 pin((side*.147,-.019,1.238),(side*.141,-.018,1.43))
 pin((side*.304,-.021,1.066),(side*.224,-.024,1.211))
 pin((side*.412,-.16,.951),(side*.289,-.04,1.005))
 pin((side*.47,-.195,.90),(side*.295,-.057,.94))
 pin((side*.092,-.019,.814),(side*.091,-.018,1.025))
 pin((side*.129,-.045,.436),(side*.123,-.036,.60))
 pin((side*.158,-.02,.065),(side*.14,-.02,.13))
 pin((side*.159,-.11,.01),(side*.14,-.13,.02))
 pin((side*.072,-.034,1.477),(side*.087,-.041,1.652))
pin((0,0,0),(0,0,0));pin((0,-.02,.83),(0,-.02,1.04));pin((0,0,.95),(0,0,1.16));pin((0,0,1.182),(0,0,1.35))
pin((0,0,1.319),(0,0,1.493));pin((0,-.04,1.422),(0,-.04,1.58));pin((0,-.034,1.558),(0,-.034,1.745))
source=np.array(source);target=np.array(target);n=len(source)
phi=lambda d:np.linalg.norm(d,axis=-1)**3
a=phi(source[:,None,:]-source[None,:,:]);a+=np.eye(n)*1e-7;p=np.column_stack([np.ones(n),source]);system=np.block([[a,p],[p.T,np.zeros((4,4))]])
coef=np.linalg.solve(system,np.vstack([target,np.zeros((4,3))]))
def warp(points):
 points=np.asarray(points);return np.column_stack([phi(points[:,None,:]-source[None,:,:]),np.ones(len(points)),points])@coef
points=warp([v.co[:] for v in body.data.vertices])
for v,p in zip(body.data.vertices,points):v.co=p
body.data.update()
# Update the skeleton to the same rest proportions, preserving imported deformation weights.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:
 a,c=oldbones[b.name];res=warp([a,c]);b.head=res[0];b.tail=res[1]
bpy.ops.object.mode_set(mode='OBJECT');rig.name='Seoha_Rig'
mod=body.modifiers.new('Seoha deformation','ARMATURE');mod.object=rig;body.parent=rig
body.name='Seoha fitted body and costume foundation'
img=bpy.data.images.load(str(OUT/'SeoTurnaround.png'),check_existing=True)
surface=bpy.data.materials.new('Illustration surface');surface.use_nodes=True;nodes=surface.node_tree.nodes;nodes.clear()
texture=nodes.new('ShaderNodeTexImage');texture.image=img;texture.interpolation='Linear'
shader=nodes.new('ShaderNodeEmission');shader.inputs['Strength'].default_value=1
output=nodes.new('ShaderNodeOutputMaterial');surface.node_tree.links.new(texture.outputs['Color'],shader.inputs['Color']);surface.node_tree.links.new(shader.outputs[0],output.inputs[0])
lit=nodes.new('ShaderNodeBsdfPrincipled');lit.inputs['Roughness'].default_value=.8;surface.node_tree.links.new(texture.outputs['Color'],lit.inputs['Base Color']);mix=nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=.16;surface.node_tree.links.new(shader.outputs[0],mix.inputs[1]);surface.node_tree.links.new(lit.outputs[0],mix.inputs[2]);surface.node_tree.links.new(mix.outputs[0],output.inputs[0])
K=1.75/1424
pixels=np.array(img.pixels[:]).reshape(img.size[1],img.size[0],4)[::-1,:,:3]
mask=(pixels.max(2)-pixels.min(2)>.035)|(pixels.max(2)<.36)|(pixels.min(2)>.74)
row_ink=[[np.where(mask[y,lo:hi])[0]+lo for y in range(1536)] for lo,hi in [(0,512),(514,1024)]]
def project(o,front_only=False):
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 o.data.materials.clear();o.data.materials.append(surface)
 if o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers.active)
 uv=o.data.uv_layers.new(name='Illustration projection')
 for poly in o.data.polygons:
  center=o.matrix_world@poly.center
  front=front_only or center.y<-.017
  for li in poly.loop_indices:
   co=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
   px=298+co.x/K if front else 735-co.x/K;py=1464-co.z/K
   py=min(1465,max(40,py));row=int(py);ink=row_ink[0 if front else 1][row]
   # Fit the UV seam to the illustrated garment boundary, never sample its neutral backdrop.
   if len(ink):px=float(ink[np.argmin(abs(ink-px))])
   uv.data[li].uv=((px+.5)/1024,1-(py+.5)/1536)
  poly.use_smooth=True
project(body)
sub=body.modifiers.new('Surface finish','SUBSURF');sub.levels=1;sub.render_levels=2
def fixed(o,bone='head',front_only=False):
 o.parent=rig;g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');a=o.modifiers.new('Rig deformation','ARMATURE');a.object=rig
 project(o,front_only);return o
def sphere(name,at,scale,bone='head'):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=at);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);return fixed(o,bone)
# Separate hair volume; the face remains a contoured mesh, not a billboard.
verts=[];faces=[];sides=40
for k in range(13):
 a=math.pi*(.07+k*.68/12)
 for j in range(sides):
  angle=math.tau*j/sides;z=1.635+math.cos(a)*.124;x=math.sin(a)*math.cos(angle)*.112;y=-.006+math.sin(a)*math.sin(angle)*.095
  if y<-.035 and z<1.69:y=-.04
  verts.append((x,y,z))
for k in range(12):
 for j in range(sides):
  # Open the lower frontal region for eyebrows, eyes and cheeks.
  if k>5 and math.sin(math.tau*(j+.5)/sides)<-.22:continue
  a=k*sides+j;b=k*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
mesh=bpy.data.meshes.new('Hair shell');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Swept lilac hair cap',mesh);bpy.context.collection.objects.link(o);fixed(o)
sphere('Asymmetric low hair bun',(-.086,.035,1.596),(.039,.034,.040))
def ribbon(name,coords):
 v=[]
 for px,py,halfwidth,depth in coords:
  for s in [-1,1]:v.append(((px+s*halfwidth-298)*K,depth,(1464-py)*K))
 f=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(coords)-1)];m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);fixed(o,front_only=True)
 solid=o.modifiers.new('Hair strand thickness','SOLIDIFY');solid.thickness=.0012
 smooth=o.modifiers.new('Curved hair strand','SUBSURF');smooth.levels=2
 return o
ribbon('Diagonal sculpted fringe',[(320,63,28,-.055),(329,99,24,-.094),(320,132,18,-.108),(299,172,10,-.12),(280,192,1,-.12)])
ribbon('Right face-framing strand',[(236,151,8,-.07),(232,233,7,-.085),(239,307,5,-.106),(247,355,1,-.135)])
ribbon('Left face-framing strand',[(353,126,8,-.075),(356,210,8,-.075),(353,282,5,-.09),(342,330,1,-.105)])
# Jacket lapels are actual folded surfaces, with a raised collar and asymmetric shoulder plate.
def panel(name,points,faces,bone):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);fixed(o,bone);s=o.modifiers.new('Garment thickness','SOLIDIFY');s.thickness=.002;return o
panel('Raised right jacket collar',[(-.065,-.052,1.493),(-.095,-.12,1.46),(-.074,-.13,1.394),(-.039,-.065,1.443)],[(0,1,2,3)],'spine01')
panel('Raised left jacket collar',[(.061,-.05,1.491),(.091,-.10,1.454),(.07,-.13,1.391),(.035,-.07,1.443)],[(0,1,2,3)],'spine01')
def plain(name,color,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.35;return m
black=plain('Black leather edges',(.024,.025,.033));silver=plain('Titanium',(.35,.4,.48),.8);cyan=plain('Forearm signal light',(.02,.85,.9),.4)
def toon(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;nodes=m.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');e=nodes.new('ShaderNodeEmission');e.inputs['Color'].default_value=(*color,1);e.inputs['Strength'].default_value=1;m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
edgecloth=toon('Costume side finish',(.032,.029,.045));skin=toon('Warm anime skin',(.87,.69,.64));hair=toon('Lilac hair side finish',(.55,.49,.66))
body.data.materials.append(edgecloth);body.data.materials.append(skin)
for poly in body.data.polygons:
 co=poly.center
 if abs(co.x)>.172 and .915<co.z<1.425:
  poly.material_index=2 if co.x>0 and 1.14<co.z<1.39 or co.z<.95 else 1
  continue
 if abs(poly.normal.y)<.42:
  exposed=1.08<co.z<1.20 and abs(co.x)<.15 or co.x>.17 and 1.14<co.z<1.39
  poly.material_index=2 if exposed else 1
for o in list(bpy.data.objects):
 if o.type=='MESH' and ('hair cap' in o.name or 'hair bun' in o.name):
  o.data.materials.append(hair)
  for poly in o.data.polygons:
   if abs(poly.normal.y)<.65 or 'bun' in o.name:poly.material_index=1
# Actual swept strands around the bun and side cap remain readable from profile and rear views.
for n in range(18):
 curve=bpy.data.curves.new('Combed hair groove','CURVE');curve.dimensions='3D';curve.bevel_depth=.0007;curve.bevel_resolution=2
 spline=curve.splines.new('POLY');spline.points.add(20)
 for j,point in enumerate(spline.points):
  a=j/20*math.tau*.78;lat=(n-8.5)*.13
  point.co=(-.086+math.sin(a)*.040*math.cos(lat),.035+math.cos(a)*.035*math.cos(lat),1.596+math.sin(lat)*.041,1)
 o=bpy.data.objects.new('Bun strand '+str(n),curve);bpy.context.collection.objects.link(o);o.data.materials.append(toon('Strand shade '+str(n),(.39+n%3*.06,.34+n%3*.06,.49+n%3*.06)))
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o=bpy.context.object
 g=o.vertex_groups.new(name='head');g.add(list(range(len(o.data.vertices))),1,'REPLACE');o.parent=rig;mod=o.modifiers.new('Hair follows head','ARMATURE');mod.object=rig
# Curved, tapered 3D locks describe the flow from the crown into the low side bun.
strand_materials=[plain('Lilac strand '+str(i),(.40+i*.035,.35+i*.035,.5+i*.035)) for i in range(6)]
for n in range(64):
 azimuth=math.tau*n/64
 if math.sin(azimuth)<-.4:continue
 verts=[];faces=[]
 for j in range(17):
  t=j/16;polar=.05+t*(2.04 if math.sin(azimuth)>.1 else 1.8);turn=azimuth+.16*math.sin(t*math.pi)
  width=.045*math.sin(math.pi*t)**.55+.004
  for side in [-1,1]:
   a=turn+side*width;verts.append((math.sin(polar)*math.cos(a)*.114,-.006+math.sin(polar)*math.sin(a)*.098,1.635+math.cos(polar)*.127))
 for j in range(16):faces.append((j*2,j*2+1,j*2+3,j*2+2))
 mesh=bpy.data.meshes.new('Swept hair lock');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Sculpted side hair lock '+str(n),mesh);bpy.context.collection.objects.link(o)
 o.data.materials.append(strand_materials[n%6]);g=o.vertex_groups.new(name='head');g.add(list(range(len(verts))),1,'REPLACE');o.parent=rig;mod=o.modifiers.new('Hair follows head','ARMATURE');mod.object=rig;s=o.modifiers.new('Hair thickness','SOLIDIFY');s.thickness=.0014
def detail(name,at,size,bone,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);fixed(o,bone);o.data.materials.clear();o.data.materials.append(material);b=o.modifiers.new('Rounded manufactured detail','BEVEL');b.width=.003;b.segments=3;return o
detail('Belt buckle',(-.01,-.107,1.072),(.032,.012,.035),'spine05',silver)
detail('Left forearm interface',(.272,-.063,1.092),(.032,.022,.09),'lowerarm02.L',black)
detail('Left cuff cyan status',(.276,-.076,1.109),(.006,.006,.031),'lowerarm02.L',cyan)
for index,(at,size,bone) in enumerate([((-.16,-.048,1.414),(.095,.058,.065),'upperarm01.R'),((-.197,-.058,1.316),(.074,.038,.10),'upperarm02.R'),((-.23,-.052,1.221),(.07,.036,.065),'lowerarm01.R'),((-.264,-.059,1.11),(.064,.03,.094),'lowerarm02.R')]):
 plate=detail('Right sleeve articulated armour '+str(index),at,size,bone,black)
 for offset in [-.019,.019]:detail('Armour inset fastening',tuple(Vector(at)+Vector((offset,-.021,0))),(.008,.009,.008),bone,silver)
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<.125],context='VERTS');bm.to_mesh(body.data);bm.free()
for side in [-1,1]:
 v=[];f=[];n=32
 for z,rx,ry,cy in [(.005,.052,.101,-.06),(.035,.054,.105,-.06),(.08,.052,.1,-.06),(.13,.049,.078,-.045),(.20,.045,.046,-.02),(.30,.043,.043,-.012)]:
  for j in range(n):a=math.tau*j/n;v.append((side*.14+math.cos(a)*rx,cy+math.sin(a)*ry,z))
 for k in range(5):
  for j in range(n):a=k*n+j;b=k*n+(j+1)%n;f.append((a,b,b+n,a+n))
 f.append(tuple(reversed(range(n))));m=bpy.data.meshes.new('Tech boot shell');m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new('Tailored tech boot',m);bpy.context.collection.objects.link(o);fixed(o,'foot.L' if side>0 else 'foot.R');o.data.materials.append(edgecloth)
 for poly in o.data.polygons:
  if abs(poly.normal.y)<.6 or poly.center.z<.045:poly.material_index=1
# Studio review renders, independent of game camera and without image post-processing.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.world.color=(.45,.45,.45)
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.render.image_settings.file_format='PNG';scene.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(0,-5,1.14));camera=bpy.context.object;camera.name='Review camera';camera.data.type='ORTHO';camera.data.ortho_scale=1.94;scene.camera=camera
def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
for at,power,size in [((-3,-4,5),500,4),((3,-1,3),300,3),((0,3,4),600,3)]:
 bpy.ops.object.light_add(type='AREA',location=at);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size;aim(lamp,(0,0,1))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025));floor=bpy.context.object;floor.data.materials.append(plain('Studio floor',(.17,.19,.23)))
scene.render.resolution_x=800;scene.render.resolution_y=1100
for name,at in [('Front',(0,-5,1.04)),('ThreeQuarter',(3,-5,1.16)),('Back',(0,5,1.04))]:
 camera.location=at;aim(camera,(0,0,.9));scene.render.filepath=str(OUT/('Seo-Trial-'+name+'.png'));bpy.ops.render.render(write_still=True)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seo-Trial.blend'))
(OUT/'trial.json').write_text(json.dumps({'vertices':len(body.data.vertices),'bones':len(rig.data.bones),'status':'visual_review_required'},indent=2))
print('SEO_SCULPT_TRIAL_READY')

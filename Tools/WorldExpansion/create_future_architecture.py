"""Original cyberpunk architecture kit; Blender metres, normalized unit footprints.
Eight silhouettes, separate presentation / distant LOD / collision meshes.
"""
import bpy, math, pathlib, json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]; OUT=ROOT/'Assets/AfterSignal/Resources/WorldAssets/FutureArchitecture'; OUT.mkdir(parents=True,exist_ok=True)
DOC=ROOT/'Documentation/FutureCity/Models';DOC.mkdir(parents=True,exist_ok=True)
def mat(n,c,metal):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.27;return m
shell=mat('FutureShell',(.28,.39,.43),.6);glass=mat('FutureGlass',(.04,.16,.22),.7);metal=mat('FutureMetal',(.55,.68,.7),.8);neon=mat('FutureNeon',(.14,.85,.76),.2);leaf=mat('FutureLeaf',(.06,.22,.14),0)
def obj(n,v,f,m):
 mesh=bpy.data.meshes.new(n);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(m)
 uv=mesh.uv_layers.new()
 for p in mesh.polygons:
  for li in p.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(math.atan2(co.y,co.x)/math.tau,co.z)
 return o

def polygon(n=12,roundness=1):
 return [(math.cos(i*math.tau/n)*.48,math.sin(i*math.tau/n)*.48) for i in range(n)]
chamfer=[(-.36,-.49),(.36,-.49),(.49,-.36),(.49,.36),(.36,.49),(-.36,.49),(-.49,.36),(-.49,-.36)]
def loft(n,poly,rings,m):
 v=[]
 for z,sx,sy,angle,dx,dy in rings:
  a=angle;v += [(x*sx*math.cos(a)-y*sy*math.sin(a)+dx,x*sx*math.sin(a)+y*sy*math.cos(a)+dy,z) for x,y in poly]
 k=len(poly);faces=[tuple(range(k-1,-1,-1)),tuple(range((len(rings)-1)*k,len(rings)*k))]
 for j in range(len(rings)-1):
  for i in range(k):faces.append((j*k+i,j*k+(i+1)%k,(j+1)*k+(i+1)%k,(j+1)*k+i))
 return obj(n,v,faces,m)
def cube(n,at,size,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=n;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);return o
def beam(n,a,b,r,m):
 d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.name=n;o.data.materials.append(m);return o

def rings_for(kind):
 if kind==0:return polygon(16),[(i/12,1-.2*i/12,1-.2*i/12,i*.035,0,0) for i in range(13)]
 if kind==1:return chamfer,[(0,1,1,0,0,0),(.42,1,1,0,0,0),(.43,.77,.78,0,-.09,.05),(.7,.77,.78,0,-.09,.05),(.71,.52,.52,0,-.18,.1),(1,.52,.52,0,-.18,.1)]
 if kind==2:return polygon(6),[(0,.78,1,0,-.1,0),(.12,.82,1,0,-.1,0),(.82,.8,.82,0,.11,0),(1,.32,.46,0,.22,0)]
 if kind==3:return polygon(24),[(0,1,.74,0,0,0),(.14,1,.74,0,0,0),(.23,.82,.66,0,0,0),(.87,.78,.66,0,0,0),(1,.53,.44,0,0,0)]
 if kind==4:return chamfer,[(0,.8,.8,0,0,0),(.12,.8,.8,0,0,0),(.13,1,.88,0,0,0),(.3,1,.88,0,0,0),(.32,.62,.62,0,.12,0),(.52,.62,.62,0,.12,0),(.53,1,.8,0,0,0),(.75,1,.8,0,0,0),(.76,.62,.62,0,-.12,0),(1,.62,.62,0,-.12,0)]
 if kind==5:return polygon(8),[(0,.5,.68,0,0,0),(.12,.5,.68,0,0,0),(.15,.95,.95,0,0,0),(.9,.66,.66,.2,0,0),(1,.9,.9,.2,0,0)]
 if kind==6:return chamfer,[(0,.78,.78,0,0,0),(.15,.78,.78,0,0,0),(.2,.95,.6,0,0,0),(.92,.68,.52,0,0,0),(1,.52,.4,0,0,0)]
 return polygon(12),[(0,.65,.65,0,0,0),(.64,.65,.65,0,0,0),(.72,.95,.95,0,0,0),(.87,.95,.95,0,0,0),(.92,.56,.56,0,0,0),(1,.42,.42,0,0,0)]
def interp(rings,z):
 for a,b in zip(rings,rings[1:]):
  if a[0]<=z<=b[0]:
   t=(z-a[0])/max(.0001,b[0]-a[0]);return tuple(x+(y-x)*t for x,y in zip(a,b))
 return rings[-1]
def group(name,objects):
 bpy.ops.object.empty_add();p=bpy.context.object;p.name=name
 for o in objects:o.parent=p
 return p
names=['Helix','Cascade','Prism','Oval','Cantilever','Lantern','TwinGate','Orbital']
report=[]
for kind,name in enumerate(names):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 poly,rings=rings_for(kind)
 if kind==6:
  parts=[]
  for s in [-1,1]:parts.append(loft('Gate pier',chamfer,[(0,.32,.8,0,s*.3,0),(.9,.3,.68,0,s*.3,0),(1,.28,.6,0,s*.3,0)],glass))
  parts.append(loft('Elevated inhabited bridge',chamfer,[(.75,.91,.62,0,0,0),(.9,.91,.62,0,0,0)],glass))
 else:parts=[loft('Curtain wall',poly,rings,glass)]
 # Independent collision and low-detail profile, never a box across the open gate.
 near=list(parts)
 low=[];coll=[]
 for p in parts:
  c=p.copy();c.data=p.data.copy();c.name='Collision hull';bpy.context.collection.objects.link(c);coll.append(c)
  l=p.copy();l.data=p.data.copy();l.data.materials.clear();l.data.materials.append(glass);l.name='Distant envelope';bpy.context.collection.objects.link(l);low.append(l)
 if kind!=6:
  for i in range(1,19):
   z=i/19;r=interp(rings,z);band=[(z-.004,r[1]*1.015,r[2]*1.015,r[3],r[4],r[5]),(z+.004,r[1]*1.015,r[2]*1.015,r[3],r[4],r[5])];near.append(loft('Recessed slab edge',poly,band,metal if i%4 else shell))
  for j in range(0,len(poly),max(1,len(poly)//8)):
   for a,b in zip(rings,rings[1:]):
    def pt(r):
     x,y=poly[j];z,sx,sy,ang,dx,dy=r;return (x*sx*math.cos(ang)-y*sy*math.sin(ang)+dx,x*sx*math.sin(ang)+y*sy*math.cos(ang)+dy,z)
    near.append(beam('Load-bearing exoskeleton',pt(a),pt(b),.006,shell))
 else:
  for s in [-1,1]:
   for i in range(1,18):near.append(cube('Bridge pier slab',(s*.3,0,i/18),(.32,.7,.005),metal))
 # Base plinth, ventilation, geometric retail canopy, fins and roof gardens.
 near.append(loft('Sculpted street podium',chamfer,[(0,1,1,0,0,0),(.055,1,1,0,0,0),(.07,.91,.92,0,0,0)],shell))
 for s in [-1,1]:
  near.append(cube('Neon eave',(0,s*.47,.075),(.7,.006,.008),neon))
  for j in range(6):near.append(cube('Retail mullion',(-.35+j*.14,s*.475,.033),(.012,.015,.06),metal))
 roof_x=-.3 if kind==6 else rings[-1][4];roof_y=rings[-1][5]
 for j in range(3):
  near.append(cube('Roof HVAC',(roof_x-.06+j*.06,roof_y,1.012),(.045,.10,.025),shell))
  for k in range(4):near.append(cube('Vent louvre',(roof_x-.06+j*.06,roof_y-.04+k*.027,1.027),(.038,.006,.004),metal))
 for s in [-1,1]:near.append(beam('Communication mast',(roof_x+s*.10,roof_y+.06,1),(roof_x+s*.10,roof_y+.06,1.06),.0025,metal));near.append(cube('Beacon',(roof_x+s*.10,roof_y+.06,1.06),(.006,.006,.004),neon))
 # Merge by material per LOD for GPU instancing; only five draw calls per close building.
 for label,objects in [('Near',near),('Far',low),('Collision',coll)]:
  merged=[]
  selections=[[o for o in objects if o.data.materials and o.data.materials[0]==m] for m in [shell,glass,metal,neon,leaf]]
  for selection in selections:
   if not selection:continue
   bpy.ops.object.select_all(action='DESELECT')
   for o in selection:o.select_set(True)
   bpy.context.view_layer.objects.active=selection[0];bpy.ops.object.join();o=bpy.context.object;o.name=label+' '+o.data.materials[0].name;merged.append(o)
  group(label,merged)
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(DOC/(name+'.blend')))
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 report.append({'profile':name,'vertices':sum(len(o.data.vertices) for o in bpy.context.scene.objects if o.type=='MESH')})
(ROOT/'Documentation/FutureCity/architecture.json').write_text(json.dumps(report,indent=2))

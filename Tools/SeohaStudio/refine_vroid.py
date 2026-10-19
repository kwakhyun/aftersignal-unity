"""Non-destructive Seoha tailoring on the authored VRoid mesh and expression rig.

All review images come from this real mesh. Originals stay untouched.
"""
from pathlib import Path
import sys, json, math
import bpy, bmesh
from mathutils import Vector
from mathutils.kdtree import KDTree
sys.path.insert(0, str(Path(__file__).parent))
from vroid_review import OUT, components, render_views
from vroid_hair import add_swept_locks

def clamp(x): return max(0., min(1., x))
def smooth(x):
    x=clamp(x); return x*x*(3-2*x)
def gauss(x,s): return math.exp(-(x/s)**2)

bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Imported.blend'))
rig=bpy.data.objects['Armature']; body=bpy.data.objects['Body']; face=bpy.data.objects['Face']; hair=bpy.data.objects['Hair']
hair_islands=components(hair.data)
report={'source':'Seoha-Authoring.vroid', 'game_ready':False, 'changes':[], 'rendered_from_geometry':True}

def transform_vertices(ob, ids, fn):
    ids=list(ids); matrix=ob.matrix_world.copy(); inv=matrix.inverted()
    layers=[k.data for k in ob.data.shape_keys.key_blocks] if ob.data.shape_keys else [ob.data.vertices]
    for layer in layers:
        for i in ids: layer[i].co=inv@fn(matrix@layer[i].co)
    ob.data.update()

def delete_vertices(ob, ids):
    bm=bmesh.new();bm.from_mesh(ob.data);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.verts[i] for i in sorted(set(ids))],context='VERTS')
    bm.to_mesh(ob.data);bm.free();ob.data.update()

def groups_for(ids):
    return {hair.vertex_groups[g.group].name for i in ids for g in hair.data.vertices[i].groups if g.weight>.035}

# Keep one tied bundle, and move both geometry and its actual spring bone roots.
remove=[]; bun=[]; front=[]; side_l=[]; side_r=[]
for ids in hair_islands:
    names=groups_for(ids)
    if any('_06' in n or '_07' in n for n in names): remove.extend(ids)
    elif any('_08' in n or '_09' in n for n in names): bun.extend(ids)
    elif any('_02' in n for n in names): side_r.extend(ids)
    elif any('_05' in n for n in names): side_l.extend(ids)
    elif any('_01' in n or '_03' in n or '_04' in n for n in names): front.extend(ids)

def low_bun(p):
    center=Vector((-.09,.069,1.62))
    p=center+(p-center)*.91
    return p+Vector((-.009,.010,-.095))
transform_vertices(hair,bun,low_bun)
def sweeping_fringe(p):
    t=smooth((1.645-p.z)/.10)
    p.x-=.023*t*gauss(p.x+.005,.052)
    p.z+=.026*t*gauss(p.x+.02,.06)
    p.y-=.002*t
    return p
transform_vertices(hair,front,sweeping_fringe)
def side_lock(p):
    t=smooth((1.59-p.z)/.115)
    p.z-=.028*t
    p.y-=.008*t
    p.x+=(-1 if p.x<0 else 1)*(.012*t-.013*math.sin(t*math.pi))
    return p
transform_vertices(hair,side_l+side_r,side_lock)
delete_vertices(hair,remove)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:
    if '_08' in b.name or '_09' in b.name:
        b.head=low_bun(b.head);b.tail=low_bun(b.tail)
    elif '_02' in b.name or '_05' in b.name:
        b.head=side_lock(b.head);b.tail=side_lock(b.tail)
bpy.ops.object.mode_set(mode='OBJECT')
add_swept_locks(hair.data.materials[0],rig)
report['changes'].append('One low right-side bun; swept fringe; long curved side locks; matching hair bones')
assert len(remove)>500 and len(bun)>500 and len(side_l)>50, 'Hair groups must resolve before saving a likeness revision'

# Refine all 57 facial expression shapes with the same continuous deformation.
def sculpt_face(p):
    eye=gauss(abs(p.x)-.041,.031)*gauss(p.z-1.549,.037)*smooth((.018-p.y)/.04)
    p.z+=(1.545-p.z)*.13*eye + .0032*eye*clamp((abs(p.x)-.015)/.05)
    p.x+=(.042-abs(p.x))*(1 if p.x>=0 else -1)*.055*eye
    # Soft jaw: broaden the lower chin slightly instead of sharpening a doll point.
    chin=gauss(p.z-1.468,.018)*gauss(p.x,.044)
    p.x*=1+.10*chin
    p.z+=.0012*chin
    # More natural mouth projection, preserving the actual oral cavity.
    lip=gauss(p.x,.024)*gauss(p.z-1.493,.009)*smooth((-.023-p.y)/.025)
    p.y-=.0028*lip
    # Reduce exposed ear size without changing attachment seams.
    ear=smooth((abs(p.x)-.068)/.018)*smooth((p.y-.003)/.022)
    p.x+=(.074*(1 if p.x>0 else -1)-p.x)*.21*ear
    p.z+=(1.533-p.z)*.12*ear
    return p
transform_vertices(face,range(len(face.data.vertices)),sculpt_face)
new_face=bpy.data.images.load(str(OUT/'Seoha-Face-Albedo.png'),check_existing=True)
face_mat=face.data.materials[2]
for n in face_mat.node_tree.nodes:
    if n.type=='TEX_IMAGE' and n.name in ['Mtoon1BaseColorTexture.Image','Mtoon1ShadeMultiplyTexture.Image']:n.image=new_face
report['changes'].append('Almond eye aperture, ear proportion, mouth projection and UV skin paint on all expression keys')

def material(name,color,rough=.45,metal=0,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    if emission:
        bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=emission
    return m
leather=material('Seoha | midnight leather',(.008,.010,.016),.68,.02)
lining=material('Seoha | folded graphite lining',(.016,.020,.032),.72,.02)
steel=material('Seoha | brushed gunmetal',(.19,.21,.25),.31,.8)
cyan=material('Seoha | narrow cyan piping',(.02,.40,.52),.34,.25,.4)
rubber=material('Seoha | matte black equipment',(.012,.016,.021),.64)
for mat in [leather,lining,rubber]:
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Specular IOR Level'].default_value=.22
cloth_image=bpy.data.images.load(str(OUT/'Seoha-Garment-Albedo.png'),check_existing=True)
cloth_mat=body.data.materials[3]
for key in ['Mtoon1BaseColorTexture.Image','Mtoon1ShadeMultiplyTexture.Image']:
    node=cloth_mat.node_tree.nodes.get(key)
    if not node:continue
    # Preserve the authored silhouette alpha; generated paint changes only color.
    original=cloth_mat.node_tree.nodes.new('ShaderNodeTexImage');original.image=node.image
    if node.inputs['Vector'].is_linked:
        cloth_mat.node_tree.links.new(node.inputs['Vector'].links[0].from_socket,original.inputs['Vector'])
    for link in list(node.outputs['Alpha'].links):cloth_mat.node_tree.links.new(original.outputs['Alpha'],link.to_socket)
    node.image=cloth_image
    # Generated paint has an RGB checker outside the islands. Retain authored
    # alpha and reject bright padding samples at the atlas borders in the shader.
    color_links=list(node.outputs['Color'].links)
    minimum=cloth_mat.node_tree.nodes.new('ShaderNodeVectorMath');minimum.operation='DOT_PRODUCT'
    minimum.inputs[1].default_value=(.2126,.7152,.0722)
    cloth_mat.node_tree.links.new(node.outputs['Color'],minimum.inputs[0])
    threshold=cloth_mat.node_tree.nodes.new('ShaderNodeMath');threshold.operation='GREATER_THAN';threshold.inputs[1].default_value=.32
    cloth_mat.node_tree.links.new(minimum.outputs['Value'],threshold.inputs[0])
    paint=cloth_mat.node_tree.nodes.new('ShaderNodeMixRGB');paint.inputs[2].default_value=(.007,.008,.012,1)
    cloth_mat.node_tree.links.new(node.outputs['Color'],paint.inputs[1]);cloth_mat.node_tree.links.new(threshold.outputs[0],paint.inputs[0])
    for link in color_links:cloth_mat.node_tree.links.new(paint.outputs[0],link.to_socket)
# Remove the preset red/gold boot accents through its shader, retaining the lace albedo.
for mat in [body.data.materials[2],body.data.materials[4]]:
    for key in ['Mtoon1BaseColorTexture.Image','Mtoon1ShadeMultiplyTexture.Image']:
        n=mat.node_tree.nodes.get(key)
        if not n:continue
        links=list(n.outputs['Color'].links)
        hue=mat.node_tree.nodes.new('ShaderNodeHueSaturation');hue.inputs['Saturation'].default_value=0
        hue.inputs['Value'].default_value=.46
        mat.node_tree.links.new(n.outputs['Color'],hue.inputs['Color'])
        for link in links:mat.node_tree.links.new(hue.outputs['Color'],link.to_socket)

# Skin is retained underneath the sleeve. The cloth mesh is cut, not alpha-erased.
top_ids={i for p in body.data.polygons if p.material_index==3 for i in p.vertices}
def crop_top(p):
    if abs(p.x)<.13 and p.z<1.22:p.z+=.054*smooth((1.23-p.z)/.11)
    if p.x<-.28:p.x-=.043*smooth((-p.x-.28)/.12)
    return p
transform_vertices(body,top_ids,crop_top)
bm=bmesh.new();bm.from_mesh(body.data)
cloth_faces=[f for f in bm.faces if f.material_index==3]
cloth_edges=set(e for f in cloth_faces for e in f.edges)
cloth_verts=set(v for f in cloth_faces for v in f.verts)
bmesh.ops.bisect_plane(bm,geom=cloth_faces+list(cloth_edges)+list(cloth_verts),
    dist=.00001,plane_co=body.matrix_world.inverted()@Vector((.237,0,0)),
    plane_no=Vector((1,0,0)),clear_outer=True,clear_inner=False)
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()

# Shrink the high trouser waistband slightly to leave the original bare midriff.
trouser_ids={i for p in body.data.polygons if p.material_index==1 for i in p.vertices}
def waist(p):
    p.z-=.035*smooth((p.z-1.015)/.097)
    return p
transform_vertices(body,trouser_ids,waist)

# VRoid removed the skin hidden by its original long sleeve on export. Extend
# the actual open forearm boundary under the new short sleeve, preserving its
# topology, UV coordinates and the original transition into the elbow.
bm=bmesh.new();bm.from_mesh(body.data)
opening=[e for e in bm.edges if len(e.link_faces)==1 and e.link_faces[0].material_index==0
    and all(.24 < (body.matrix_world@v.co).x < .28 for v in e.verts)]
assert len(opening)>=8, 'Expected the authored left forearm boundary'
extended=bmesh.ops.extrude_edge_only(bm,edges=opening)
weights=bm.verts.layers.deform.verify();upper=body.vertex_groups['J_Bip_L_UpperArm'].index
cloth_neighbors=[v for f in bm.faces if f.material_index==3 for v in f.verts if .14<v.co.x<.24]
for vert in extended['geom']:
    if not isinstance(vert,bmesh.types.BMVert):continue
    p=body.matrix_world@vert.co
    p.x=.18;p.y=.007+(p.y-.025)*.91;p.z=1.381+(p.z-1.381)*1.04
    vert.co=body.matrix_world.inverted()@p
    nearest=min(cloth_neighbors,key=lambda v:(v.co-vert.co).length_squared)
    adjacent_weights=dict(nearest[weights])
    vert[weights].clear()
    for group,weight in adjacent_weights.items():vert[weights][group]=weight
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()
exposed_skin=body.data.materials[0].copy();exposed_skin.name='Seoha | restored exposed arm skin'
for node in exposed_skin.node_tree.nodes:
    for socket in node.inputs:
        if (node.name=='Mtoon1Material.Mtoon1Output' and socket.name=='Lit Color Texture Alpha') or (node.name=='Mtoon1Material.CompatibleShader' and socket.name=='Alpha'):
            for link in list(socket.links):exposed_skin.node_tree.links.remove(link)
            socket.default_value=1
body.data.materials.append(exposed_skin);exposed_index=len(body.data.materials)-1
for poly in body.data.polygons:
    if poly.material_index==0 and .15<poly.center.x<.315 and poly.center.z>1.3:poly.material_index=exposed_index
tailored_sleeve=body.data.materials[3].copy();tailored_sleeve.name='Seoha | opaque tailored short sleeve'
for node in tailored_sleeve.node_tree.nodes:
    for socket in node.inputs:
        if (node.name=='Mtoon1Material.Mtoon1Output' and socket.name=='Lit Color Texture Alpha') or (node.name=='Mtoon1Material.CompatibleShader' and socket.name=='Alpha'):
            for link in list(socket.links):tailored_sleeve.node_tree.links.remove(link)
            socket.default_value=1
body.data.materials.append(tailored_sleeve);sleeve_index=len(body.data.materials)-1
for poly in body.data.polygons:
    if poly.material_index==3 and poly.center.x>.125:poly.material_index=sleeve_index
report['changes'].append('Restored exported-away upper-arm skin by extending the original forearm boundary')

# Nearest skin weights let all added tailoring follow existing chest / arm / pelvis bones.
tree=KDTree(len(body.data.vertices))
for v in body.data.vertices:tree.insert(body.matrix_world@v.co,v.index)
tree.balance()
def skin(ob,bone=None):
    if ob.type!='MESH':
        bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.convert(target='MESH');ob=bpy.context.object
    for v in ob.data.vertices:
        if bone:weights=[(bone,1.)]
        else:
            _,i,_=tree.find(ob.matrix_world@v.co)
            weights=[(body.vertex_groups[g.group].name,g.weight) for g in body.data.vertices[i].groups if g.weight>.0001]
        for name,w in weights:
            group=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);group.add([v.index],w,'REPLACE')
    m=ob.modifiers.new('Seoha skeleton','ARMATURE');m.object=rig
    ob.parent=rig
    for p in ob.data.polygons:p.use_smooth=True
    ob.select_set(False)
    return ob

def patch(name,verts,faces,mat,thickness=.002,bevel=.001,bone=None):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(mat)
    if thickness:
        sol=ob.modifiers.new('Tailored thickness','SOLIDIFY');sol.thickness=thickness
    if bevel:
        mod=ob.modifiers.new('Soft finished edge','BEVEL');mod.width=bevel;mod.segments=3
    result=skin(ob,bone)
    for poly in result.data.polygons:poly.use_smooth=False
    return result

def curve(name,points,radius,mat,bone=None,sharp=False):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=16
    data.bevel_depth=radius;data.bevel_resolution=3
    s=data.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type=p.handle_right_type='VECTOR' if sharp else 'AUTO'
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True)
    return skin(ob,bone)

# Folded collars and open jacket fronts have real thickness and curved contours.
for side in [-1,1]:
    def pt(x,y,z):return(side*x,y,z)
    verts=[pt(.031,.025,1.454),pt(.054,.036,1.448),pt(.082,.013,1.413),
           pt(.085,-.029,1.393),pt(.072,-.060,1.373),pt(.034,-.051,1.396),
           pt(.027,-.017,1.422)]
    patch('Seoha folded collar '+str(side),verts,[(0,1,6),(1,2,6),(2,3,5,6),(3,4,5)],lining,.003,.002,'J_Bip_C_UpperChest')
    curve('Collar cyan welt '+str(side),[verts[0],verts[6],verts[5],verts[4]],.00085,cyan)
    verts=[pt(.039,-.078,1.388),pt(.105,-.047,1.36),pt(.131,-.075,1.297),
           pt(.135,-.079,1.185),pt(.087,-.092,1.177),pt(.068,-.132,1.278),pt(.033,-.126,1.338)]
    patch('Seoha jacket open panel '+str(side),verts,[(0,1,6),(1,2,5,6),(2,3,4,5)],leather,.003,.002)
    lapel=[pt(.039,-.081,1.389),pt(.085,-.110,1.343),pt(.086,-.139,1.288),pt(.029,-.130,1.337)]
    patch('Seoha folded lapel '+str(side),lapel,[(0,1,2,3)],lining,.002,.001)
    curve('Lapel narrow seam '+str(side),[lapel[0],lapel[3],lapel[2]],.00065,steel)
    curve('Jacket zipper rail '+str(side),[verts[6],verts[5],verts[4]],.0011,steel)
    # Zipper teeth follow each side's diagonal opening.
    a,b=Vector(verts[5]),Vector(verts[4])
    for i in range(22):
        p=a.lerp(b,(i+.3)/22)
        patch('Jacket zipper tooth',[(p.x-.0015,p.y-.001,p.z),(p.x+.0015,p.y-.001,p.z),
             (p.x+.0015,p.y-.001,p.z+.0012),(p.x-.0015,p.y-.001,p.z+.0012)],[(0,1,2,3)],steel,0,0)

# Continuous oval choker / utility belt rings, avoiding floating rectangular slabs.
def ring_band(name,rx,ry,z,height,mat,bone,center_y=0):
    n=64;vs=[]
    for dz in [-height/2,height/2]:
        for i in range(n):
            a=2*math.pi*i/n;vs.append((rx*math.cos(a),center_y+ry*math.sin(a),z+dz))
    return patch(name,vs,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat,.002,.001,bone)
ring_band('Seoha neck strap',.032,.037,1.416,.015,leather,'J_Bip_C_Neck',.018)
ring_band('Seoha utility belt',.124,.087,1.074,.024,leather,'J_Bip_C_Hips')
patch('Seoha belt buckle',[(-.022,-.090,1.06),(.022,-.090,1.06),(.022,-.090,1.089),(-.022,-.090,1.089)],[(0,1,2,3)],steel,.005,.002,'J_Bip_C_Hips')
patch('Seoha buckle inset',[(-.013,-.096,1.067),(.013,-.096,1.067),(.013,-.096,1.082),(-.013,-.096,1.082)],[(0,1,2,3)],leather,.002,.001,'J_Bip_C_Hips')

# Small utility pouches use bevelled volume and separate closure flaps.
def box(name,loc,scale,mat,bone,bevel=.004):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);ob=bpy.context.object;ob.name=name
    ob.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);ob.data.materials.append(mat)
    b=ob.modifiers.new('Rounded sewn corners','BEVEL');b.width=bevel;b.segments=3
    return skin(ob,bone)
for side in [-1,1]:
    box('Seoha hip utility pouch',(side*.133,-.018,1.02),(.035,.072,.087),leather,'J_Bip_C_Hips')
    box('Seoha pouch flap',(side*.153,-.018,1.046),(.004,.065,.030),lining,'J_Bip_C_Hips',.002)
    # Hollow triangular jewelry, real metal frame.
    x=side*.083
    curve('Seoha triangular earring '+str(side),[(x-.006,.010,1.505),(x+.006,.010,1.505),
         (x,.007,1.477),(x-.006,.010,1.505)],.0011,steel,'J_Bip_C_Head',True)
    patch('Seoha onyx earring '+str(side),[(x-.004,.010,1.501),(x+.004,.010,1.501),(x,.008,1.482)],[(0,1,2)],leather,.001,.0004,'J_Bip_C_Head')

# Curved arm protection follows the cylinder of the retained long right sleeve.
vs=[];n=16
for x,r in [(-.322,.040),(-.355,.041),(-.407,.033),(-.432,.027)]:
    for j in range(n):
        a=-math.pi*.9+j/(n-1)*math.pi*.9
        vs.append((x,.021+r*math.sin(a),1.379+r*math.cos(a)))
patch('Seoha fitted right forearm shell',vs,[(r*n+j,r*n+j+1,(r+1)*n+j+1,(r+1)*n+j) for r in range(3) for j in range(n-1)],leather,.002,.001,'J_Bip_R_LowerArm')
curve('Forearm signal strip',[(-.344,-.020,1.394),(-.397,-.013,1.391)],.0017,cyan,'J_Bip_R_LowerArm')
report['changes'].append('Cropped asymmetric sleeve, separate folded collar/lapels, zipper teeth, belt, pouches and right forearm armor')

# Force textures into the .blend so it opens without depending on temp files.
for img in bpy.data.images:
    if img.source=='FILE' and img.has_data:
        try:img.pack()
        except RuntimeError:pass
for ob in bpy.context.selected_objects:ob.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
report.update(bones=len(rig.data.bones),expression_shapes=len(face.data.shape_keys.key_blocks)-1,
    mesh_objects=sum(ob.type=='MESH' for ob in bpy.context.scene.objects))
(OUT/'refinement-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Refined.blend'))
render_views('Refined')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Refined.blend'))

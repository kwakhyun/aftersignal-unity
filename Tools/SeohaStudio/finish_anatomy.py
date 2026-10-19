"""Tailored surfaces, coherent materials and side-parted hair on the real rig.

Private production asset only. This does not install the model in the game.
"""
from pathlib import Path
import sys,math,json
import bpy,bmesh
from mathutils import Vector,Quaternion
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
sys.path.insert(0,str(Path(__file__).parent))
from seoha_deformation import body_cage,clamp,sstep,g
from vroid_hair import catmull
from vroid_review import setup,components

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/SeohaStudio/VRoid/Anatomy'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Anatomy.blend'))
rig=bpy.data.objects['Armature'];body=bpy.data.objects['Body'];face=bpy.data.objects['Face']
rig.animation_data_clear()
for b in rig.pose.bones:
    b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
for k in face.data.shape_keys.key_blocks:k.value=0
bpy.context.view_layer.update()

# Rest-space vertex shading follows the skinned surface. Subtle anatomical
# accents reinforce the actual sculpt; they are not a projected portrait.
for ob in [body,face]:
    attr=ob.data.color_attributes.new(name='Seoha anatomy tint',type='FLOAT_COLOR',domain='POINT')
    points=ob.data.shape_keys.key_blocks[0].data if ob.data.shape_keys else ob.data.vertices
    for index,v in enumerate(points):
        x,y,z=v.co;front=sstep((.008-y)/.04)
        if ob==body:
            navel=body_cage(Vector((0,0,1.105))).z
            pit=g(x,.0048)*g(z-navel,.0055)*front
            fold=g(abs(x)-.040,.018)*g(z-(navel+.025),.046)*front
            line=g(x,.008)*g(z-(navel+.024),.040)*front
            rgb=(1-.34*pit-.025*fold-.018*line,1-.43*pit-.038*fold-.025*line,1-.43*pit-.038*fold-.025*line)
        else:
            cheek=body_cage(Vector((0,0,1.528))).z
            blush=g(abs(x)-.050,.025)*g(z-cheek,.017)*front
            lip=body_cage(Vector((0,0,1.492))).z
            mouth=g(x,.019)*g(z-lip,.0032)*front
            rgb=(1,1-.070*blush-.065*mouth,1-.052*blush-.041*mouth)
        attr.data[index].color=(*rgb,1)

def mesh(name,verts,faces,mat,uv=None,bone=None,warp=True):
    pts=[body_cage(Vector(v)) if warp else Vector(v) for v in verts]
    if name.startswith('Seoha hair'):
        center=body_cage(Vector((0,.023,1.578)))
        for i,p in enumerate(pts):
            if p.z<body_cage(Vector((0,0,1.574))).z:continue
            delta=p-center;direction=delta.normalized();origin=center.copy();farthest=None
            for _ in range(8):
                hit=head_surface.ray_cast(origin,direction,.20)[0]
                if hit is None:break
                farthest=hit;origin=hit+direction*.00015
            if farthest is not None and (farthest-center).length+.003>delta.length:
                pts[i]=farthest+direction*.003
    data=bpy.data.meshes.new(name);data.from_pydata(pts,[],faces);data.update();data.materials.append(mat)
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.parent=rig
    if uv:
        layer=data.uv_layers.new(name='Surface coordinates')
        for p in data.polygons:
            for i in p.loop_indices:layer.data[i].uv=uv[data.loops[i].vertex_index]
    for v in data.vertices:
        if bone:weights=[(bone,1.)]
        elif any(label in name for label in ['tailored','jacket','zipper','lapel','collar']):
            upper=sstep((v.co.z-1.185)/.12)
            weights=[('J_Bip_C_Chest',1-upper),('J_Bip_C_UpperChest',upper)]
        else:
            _,idx,_=skin_tree.find(v.co)
            weights=[(body.vertex_groups[w.group].name,w.weight) for w in body.data.vertices[idx].groups if w.weight>.00001]
        for name,w in weights:
            group=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);group.add([v.index],w,'REPLACE')
    mod=ob.modifiers.new('Seoha deformation rig','ARMATURE');mod.object=rig
    for p in data.polygons:p.use_smooth=True
    return ob

skin_tree=KDTree(len(body.data.vertices))
for v in body.data.vertices:skin_tree.insert(v.co,v.index)
skin_tree.balance()
surface=BVHTree.FromPolygons([v.co for v in body.data.vertices],[p.vertices[:] for p in body.data.polygons])
hv=[v.co[:] for v in body.data.vertices]+[v.co[:] for v in face.data.shape_keys.key_blocks[0].data]
hp=[p.vertices[:] for p in body.data.polygons if p.material_index==0]
hp += [tuple(len(body.data.vertices)+i for i in p.vertices) for p in face.data.polygons if p.material_index==2]
head_surface=BVHTree.FromPolygons(hv,hp)

def principled(name,color,rough=.5,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Specular IOR Level'].default_value=.28
    return m,bs

def grain(mat,bs,scale=280,depth=.00014):
    nodes=mat.node_tree.nodes;links=mat.node_tree.links
    noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale
    noise.inputs['Detail'].default_value=2
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.19;bump.inputs['Distance'].default_value=depth
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])

leather,bs=principled('Seoha finish | soft black leather',(.008,.010,.016),.48,.03);grain(leather,bs)
lining,bs=principled('Seoha finish | graphite collar lining',(.045,.047,.065),.7);grain(lining,bs,350,.0001)
fabric,bs=principled('Seoha finish | fitted charcoal knit',(.012,.011,.018),.86);grain(fabric,bs,480,.00011)
tex=fabric.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['_14']
fabric.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);fabric.surface_render_method='DITHERED'
metal,bs=principled('Seoha finish | dark titanium',(.12,.13,.17),.3,.8)
stitch,bs=principled('Seoha finish | seam thread',(.04,.045,.063),.8)
hairmat,bs=principled('Seoha finish | layered silver lavender',(.40,.32,.53),.42)
bs.inputs['Anisotropic'].default_value=.5
nodes=hairmat.node_tree.nodes;links=hairmat.node_tree.links
texcoord=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY'
mapping.inputs[1].default_value=(120,2,1);links.new(texcoord.outputs['UV'],mapping.inputs[0])
noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1.8;noise.inputs['Detail'].default_value=2
links.new(mapping.outputs['Vector'],noise.inputs['Vector'])
ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.16;ramp.color_ramp.elements[0].color=(.31,.24,.42,1)
ramp.color_ramp.elements[1].position=.86;ramp.color_ramp.elements[1].color=(.47,.37,.60,1)
links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs[0],bs.inputs['Base Color'])
bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.00009
links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])

# Diffuse lighting is remapped into a soft anime shadow palette. Build a clean
# material: mixing the add-on's custom MToon output with a BSDF causes artifacts.
skin_materials={}
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':continue
    for slot in ob.material_slots:
        mat=slot.material
        if not mat:continue
        if mat.name.endswith(' | sculpt lighting'):
            old=mat
            if old:
                if old.name in skin_materials:
                    slot.material=skin_materials[old.name];continue
                new=bpy.data.materials.new(old.name+' | soft anime volume');new.use_nodes=True
                nodes=new.node_tree.nodes;links=new.node_tree.links;nodes.clear()
                tex=nodes.new('ShaderNodeTexImage');tex.image=next(n.image for n in old.node_tree.nodes if n.type=='TEX_IMAGE' and n.image)
                diffuse=nodes.new('ShaderNodeBsdfDiffuse');diffuse.inputs['Color'].default_value=(1,1,1,1)
                diffuse.inputs['Roughness'].default_value=.8
                srgb=nodes.new('ShaderNodeShaderToRGB');links.new(diffuse.outputs[0],srgb.inputs[0])
                bw=nodes.new('ShaderNodeRGBToBW');links.new(srgb.outputs[0],bw.inputs[0])
                shade=nodes.new('ShaderNodeValToRGB');shade.color_ramp.interpolation='EASE'
                shade.color_ramp.elements[0].position=.02;shade.color_ramp.elements[0].color=(.54,.39,.46,1)
                shade.color_ramp.elements[1].position=.64;shade.color_ramp.elements[1].color=(1,.975,.965,1)
                links.new(bw.outputs[0],shade.inputs[0])
                mult=nodes.new('ShaderNodeMixRGB');mult.blend_type='MULTIPLY';mult.inputs[0].default_value=1
                if 'Body_' in old.name or 'restored exposed' in old.name:
                    tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1
                    tint.inputs[2].default_value=(1,.92,.88,1);links.new(tex.outputs[0],tint.inputs[1]);links.new(tint.outputs[0],mult.inputs[1])
                else:links.new(tex.outputs[0],mult.inputs[1])
                links.new(shade.outputs[0],mult.inputs[2])
                attrib=nodes.new('ShaderNodeAttribute');attrib.attribute_name='Seoha anatomy tint'
                paint=nodes.new('ShaderNodeMixRGB');paint.blend_type='MULTIPLY';paint.inputs[0].default_value=1
                links.new(mult.outputs[0],paint.inputs[1]);links.new(attrib.outputs['Color'],paint.inputs[2])
                emit=nodes.new('ShaderNodeEmission');links.new(paint.outputs[0],emit.inputs[0])
                output=nodes.new('ShaderNodeOutputMaterial')
                if 'Body_00_SKIN' in old.name:
                    trans=nodes.new('ShaderNodeBsdfTransparent');mix=nodes.new('ShaderNodeMixShader')
                    links.new(tex.outputs['Alpha'],mix.inputs[0]);links.new(trans.outputs[0],mix.inputs[1]);links.new(emit.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],output.inputs[0])
                    new.surface_render_method='DITHERED'
                else:links.new(emit.outputs[0],output.inputs[0])
                skin_materials[old.name]=new;slot.material=new

# Eyebrows and lash planes use cool charcoal, not the preset warm brown.
for idx,color in [(4,(.20,.155,.235,1)),(5,(.017,.011,.024,1))]:
    old=face.data.materials[idx];new=old.copy();new.name='Seoha finish | '+('brows' if idx==4 else 'lashes')
    for name in ['Mtoon1BaseColorTexture.Image','Mtoon1ShadeMultiplyTexture.Image']:
        n=new.node_tree.nodes.get(name)
        if not n:continue
        destinations=list(n.outputs['Color'].links)
        ink=new.node_tree.nodes.new('ShaderNodeRGB');ink.outputs[0].default_value=color
        for link in destinations:new.node_tree.links.new(ink.outputs[0],link.to_socket)
    face.data.materials[idx]=new

# Refine the open slit in neutral lips without changing any expression deltas.
base=face.data.shape_keys.key_blocks[0]
for index,v in enumerate(base.data):
    x,y,z=v.co
    # Deformed neutral mouth center, obtained from the same cage.
    center=body_cage(Vector((0,-.065,1.493))).z
    if abs(x)<.024 and y<-.04 and abs(z-center)<.006:
        delta=(center-z)*.30*g(x,.023)*g(z-center,.004)
        for key in face.data.shape_keys.key_blocks:key.data[index].co.z+=delta

# Subdivision refines shading and silhouette; expressions remain on the base cage.
for ob in [face,body]:
    if not any(m.type=='SUBSURF' for m in ob.modifiers):
        sub=ob.modifiers.new('Smooth sculpt surface','SUBSURF');sub.levels=1;sub.render_levels=1

remove_prefixes=['Seoha folded collar','Seoha jacket open panel','Seoha folded lapel','Collar cyan welt',
    'Lapel narrow seam','Jacket zipper rail','Jacket zipper tooth','Seoha central loose fringe','Seoha curved long lock']
for ob in list(bpy.context.scene.objects):
    if ob.name=='Hair':
        keep=set()
        for ids in components(ob.data):
            names={ob.vertex_groups[w.group].name for i in ids for w in ob.data.vertices[i].groups if w.weight>.02}
            if not any(any('_'+suffix in name for suffix in ['01','02','03','04','05']) for name in names):keep.update(ids)
        bm=bmesh.new();bm.from_mesh(ob.data);bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(ob.data);bm.free()
        ob.name='Seoha hair | authored low bun';ob.data.materials.clear();ob.data.materials.append(hairmat)
        for p in ob.data.polygons:p.material_index=0
    elif any(ob.name.startswith(p) for p in remove_prefixes):bpy.data.objects.remove(ob,do_unlink=True)

# Use a plain fitted undershirt at the center; keep authored sleeve seam detail.
body.data.materials.append(fabric);fabric_idx=len(body.data.materials)-1
for p in body.data.polygons:
    if p.material_index==3 and abs(p.center.x)<.10 and p.center.y<-.025 and p.center.z<1.34:p.material_index=fabric_idx

def ribbon(name,coords,width,mat=hairmat,depth=.0012,bone='J_Bip_C_Head',rows=48):
    points=[Vector(p) for p in coords];verts=[];faces=[];uv=[];cols=10
    for i in range(rows):
        t=i/(rows-1);p=catmull(points,t)
        tangent=(catmull(points,min(1,t+.001))-catmull(points,max(0,t-.001))).normalized()
        if p.z>1.57:
            normal=Vector((p.x/.088,(p.y-.021)/.095,(p.z-1.575)/.12))
        else:normal=Vector((p.x*4,-1,.1))
        across=tangent.cross(normal).normalized()
        if across.length<.1:across=Vector((1,0,0))
        normal=across.cross(tangent).normalized()
        envelope=(.035+.965*math.sin(math.pi*min(.999,t)))*(1-t**2)**.65
        half=max(.00002,width*.5*envelope)
        for j in range(cols):
            a=math.tau*j/cols
            verts.append(p+across*math.cos(a)*half+normal*math.sin(a)*min(depth,half*.22))
            uv.append((j/cols,t))
    for i in range(rows-1):
        for j in range(cols):faces.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
    faces.extend([tuple(reversed(range(cols))),tuple((rows-1)*cols+j for j in range(cols))])
    ob=mesh(name,verts,faces,mat,uv,bone)
    if 'long face lock' in name:
        chain='J_Sec_Hair3_02' if 'lock -1' in name else 'J_Sec_Hair3_05'
        if chain in rig.data.bones:
            root=ob.vertex_groups['J_Bip_C_Head'];tip=ob.vertex_groups.new(name=chain)
            for v in ob.data.vertices:
                t=(v.index//cols)/(rows-1);weight=sstep((t-.40)/.52)
                root.add([v.index],1-weight,'REPLACE');tip.add([v.index],weight,'REPLACE')
    return ob

# Continuous scalp under the individual locks prevents gaps between separated roots.
vs=[];uv=[];fs=[];rows=24;cols=96
for i in range(rows):
    for j in range(cols):
        a=math.tau*j/cols
        front=max(0,-math.cos(a));end=2.15-1.13*front**2
        theta=.008+(end-.008)*i/(rows-1)
        p=(.086*math.sin(theta)*math.sin(a),.023+.096*math.sin(theta)*math.cos(a),1.578+.111*math.cos(theta))
        vs.append(p);uv.append((j/cols,i/(rows-1)))
for i in range(rows-1):
    for j in range(cols):fs.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
# Fill small crown gaps. The higher front edge leaves the forehead exposed.
mesh('Seoha hair | fitted crown lining',vs,fs,hairmat,uv,'J_Bip_C_Head')

# Asymmetric, broad side-swept fringe. Each lock has a tapered volumetric section.
for i in range(7):
    u=i/6
    ribbon('Seoha hair | swept fringe %02d'%i,[(.045-.021*u,-.015+.026*u,1.683+.008*u),
        (.035-.034*u,-.079-.007*u,1.641+.026*u),
        (-.009-.052*u,-.094+.007*u,1.592+.043*u),
        (-.059-.025*u,-.064+.034*u,1.560+.024*u),
        (-.080-.016*u,-.015+.031*u,1.547+.014*u)],.040+.002*u)
for i in range(5):
    u=i/4
    ribbon('Seoha hair | right part %02d'%i,[(.040+.009*u,-.01+.030*u,1.686-.004*u),
        (.066+.014*u,-.060+.02*u,1.642),(.080+.010*u,-.060+.02*u,1.584),
        (.073+.015*u,-.063+.024*u,1.530-.011*u)],.020-.002*u)
# Retain the edited low bun and authored gathering topology at the back.
for side in [-1,1]:
    for i in range(3):
        u=i/2
        ribbon('Seoha hair | long face lock %s %s'%(side,i),[(side*(.072+.008*u),-.04,1.620),
            (side*(.075+.011*u),-.070,1.537),(side*(.057+.013*u),-.084,1.439),
            (side*(.070+.009*u),-.125,1.350),(side*(.049+.021*u),-.146,1.319+.022*u)],.009-.002*u)
ribbon('Seoha hair | crossing fine fringe',[(.048,-.014,1.681),(.042,-.087,1.619),
    (.015,-.103,1.563),(-.030,-.090,1.518),(-.044,-.078,1.514)],.005)
for i in range(2):
    ribbon('Seoha hair | loose bun strand %s'%i,[(-.082,.084,1.56),(-.123-.003*i,.061,1.552),
        (-.132,.045,1.517-.007*i),(-.111,.028,1.489-.006*i),(-.095,.022,1.500-.007*i)],.0027,rows=35)

def seam(name,points,radius,mat=stitch):
    pts=[Vector(p) for p in points];verts=[];uv=[];faces=[];rows=max(12,len(pts)*8);cols=6
    for i in range(rows):
        t=i/(rows-1);p=catmull(pts,t)
        tangent=(catmull(pts,min(1,t+.001))-catmull(pts,max(0,t-.001))).normalized()
        right=tangent.cross(Vector((0,1,0))).normalized();normal=right.cross(tangent).normalized()
        for j in range(cols):
            a=math.tau*j/cols;verts.append(p+radius*(math.cos(a)*right+math.sin(a)*normal));uv.append((j/cols,t))
    for i in range(rows-1):
        for j in range(cols):faces.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
    return mesh(name,verts,faces,mat,uv,warp=False)

def finish_surface(ob,thickness=.002):
    solid=ob.modifiers.new('Leather thickness','SOLIDIFY');solid.thickness=thickness
    bevel=ob.modifiers.new('Soft bound edge','BEVEL');bevel.width=.001;bevel.segments=3

for side in [-1,1]:
    rows=26;cols=10;verts=[];uv=[];faces=[]
    inner=[Vector(p) for p in [(side*.037,0,1.390),(side*.043,0,1.34),(side*.061,0,1.282),(side*.084,0,1.222),(side*.093,0,1.177)]]
    outer=[Vector(p) for p in [(side*.097,0,1.389),(side*.120,0,1.34),(side*.133,0,1.282),(side*.130,0,1.222),(side*.128,0,1.177)]]
    for i in range(rows):
        t=i/(rows-1);a=catmull(inner,t);b=catmull(outer,t)
        for j in range(cols):
            u=j/(cols-1);p=body_cage(a.lerp(b,u));hit=surface.ray_cast(Vector((p.x,-1,p.z)),Vector((0,1,0)),2)[0]
            if hit is None:
                # Stay just inside the actual torso silhouette instead of
                # jumping to an unrelated plane when a surface ray misses.
                for attempt in range(1,18):
                    test_x=p.x-side*attempt*.002
                    hit=surface.ray_cast(Vector((test_x,-1,p.z)),Vector((0,1,0)),2)[0]
                    if hit is not None:p.x=test_x;break
            p.y=(hit.y if hit else -.085)-.008-.004*sstep((u-.40)/.60)*(1-sstep((t-.5)/.4))-.002*math.sin(u*math.pi)
            verts.append(p);uv.append((u,t))
    for _ in range(8):
        old=[v.copy() for v in verts]
        for i in range(rows):
            for j in range(cols):
                ids=[ii*cols+jj for ii,jj in [(i-1,j),(i+1,j),(i,j-1),(i,j+1)] if 0<=ii<rows and 0<=jj<cols]
                verts[i*cols+j].y=.30*old[i*cols+j].y+.70*sum(old[k].y for k in ids)/len(ids)
    for i in range(rows-1):
        for j in range(cols-1):faces.append((i*cols+j,i*cols+j+1,(i+1)*cols+j+1,(i+1)*cols+j))
    ob=mesh('Seoha tailored jacket front %s'%side,verts,faces,leather,uv,warp=False);finish_surface(ob)
    if side<0:
        for p in ob.data.polygons:p.flip()
    sub=ob.modifiers.new('Tailored surface smoothing','SUBSURF');sub.levels=1;sub.render_levels=1
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.modifier_move_up(modifier=sub.name);bpy.ops.object.modifier_move_up(modifier=sub.name)
    seam('Seoha jacket opening seam %s'%side,[verts[i*cols]+Vector((0,-.0015,0)) for i in range(rows)],.0006)
    seam('Seoha jacket curved hem %s'%side,[verts[(rows-1)*cols+j]+Vector((0,-.0015,0)) for j in range(cols)],.00055)
    rail=[verts[i*cols]+Vector((side*.002,-.002,0)) for i in range(7,rows)]
    seam('Seoha fitted zipper rail %s'%side,rail,.00065,metal)

# Raised collar: a continuous curved arc, with a thin cyan bound edge.
verts=[];uv=[];faces=[];rows=7;cols=52
for i in range(rows):
    t=i/(rows-1)
    for j in range(cols):
        a=(-2.34+4.68*j/(cols-1));rx=.043+.034*t;ry=.050+.016*t
        p=(rx*math.sin(a),.017+ry*math.cos(a),1.389+.055*t+.006*math.cos(a))
        verts.append(p);uv.append((j/(cols-1),t))
for i in range(rows-1):
    for j in range(cols-1):faces.append((i*cols+j,i*cols+j+1,(i+1)*cols+j+1,(i+1)*cols+j))
ob=mesh('Seoha tailored raised collar',verts,faces,lining,uv,'J_Bip_C_UpperChest');finish_surface(ob)
cyan=bpy.data.materials['Seoha | narrow cyan piping']
seam('Seoha collar narrow signal welt',[body_cage(Vector(p)) for p in verts[-cols:]],.00055,cyan)
for side in [-1,1]:
    # Curved four-corner folded lapel with a soft center crease.
    a=Vector((side*.055,-.033,1.436));b=Vector((side*.105,-.064,1.383))
    c=Vector((side*.089,-.145,1.291));d=Vector((side*.033,-.125,1.349))
    vs=[];uv=[];fs=[];n=12
    for i in range(n):
        t=i/(n-1)
        for j in range(n):
            u=j/(n-1);p=a.lerp(b,u).lerp(d.lerp(c,u),t)
            p.y-=.010*math.sin(u*math.pi)*math.sin(t*math.pi)
            vs.append(p);uv.append((u,t))
    for i in range(n-1):
        for j in range(n-1):fs.append((i*n+j,i*n+j+1,(i+1)*n+j+1,(i+1)*n+j))
    ob=mesh('Seoha tailored folded lapel %s'%side,vs,fs,lining,uv);finish_surface(ob)
    seam('Seoha lapel topstitch %s'%side,[body_cage(v+Vector((0,-.001,0))) for v in [a,d,c]],.0005)

# The previous belt radius sat inside the trousers. Fit a visible continuous
# band around the garment, and use a framed buckle rather than a floating plate.
for name in ['Seoha utility belt','Seoha belt buckle','Seoha buckle inset']:
    ob=bpy.data.objects.get(name)
    if ob:bpy.data.objects.remove(ob,do_unlink=True)
verts=[];uv=[];faces=[];n=80
for row in range(3):
    for i in range(n):
        a=math.tau*i/n;verts.append((.140*math.cos(a),.005+.109*math.sin(a),1.055+.010*row));uv.append((i/n,row/2))
for row in range(2):
    for i in range(n):faces.append((row*n+i,row*n+(i+1)%n,(row+1)*n+(i+1)%n,(row+1)*n+i))
ob=mesh('Seoha fitted waist belt',verts,faces,leather,uv,'J_Bip_C_Hips');finish_surface(ob,.0014)
for z in [1.057,1.073]:
    seam('Seoha belt stitch',[body_cage(Vector((.141*math.cos(math.tau*i/80),.005+.110*math.sin(math.tau*i/80),z))) for i in range(81)],.0004)
seam('Seoha titanium buckle frame',[body_cage(Vector(p)) for p in [(-.017,-.108,1.056),(-.017,-.108,1.073),(.017,-.108,1.073),(.017,-.108,1.056),(-.017,-.108,1.056)]],.0015,metal)
seam('Seoha buckle tongue',[body_cage(Vector(p)) for p in [(0,-.110,1.057),(0,-.111,1.071)]],.0008,metal)

def rotate(name,axis,degrees):
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion()
    b.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q
def relaxed():
    for b in rig.pose.bones:b.rotation_quaternion=Quaternion()
    for side,angle in [('L',67),('R',-67)]:
        rotate('J_Bip_'+side+'_UpperArm',(0,1,0),angle)
        rotate('J_Bip_'+side+'_LowerArm',(0,0,1),-8 if side=='L' else 8)
        for digit in ['Index','Middle','Ring','Little']:
            for n,a in [(1,12),(2,18),(3,10)]:
                pb=rig.pose.bones.get('J_Bip_'+side+'_'+digit+str(n))
                if pb:pb.rotation_quaternion=Quaternion((1,0,0),math.radians(a))
relaxed()
scene,cam,aim=setup();scene.view_settings.view_transform='AgX'
scene.eevee.taa_render_samples=128;scene.eevee.shadow_ray_count=4;scene.eevee.shadow_step_count=8
for light in ['Key','Fill','Rim']:bpy.data.objects[light].data.use_shadow_jitter=True
scene.render.resolution_x=1100;scene.render.resolution_y=1300
bpy.data.objects['Key'].data.energy=220;bpy.data.objects['Fill'].data.energy=95;bpy.data.objects['Rim'].data.energy=200
views=[('Face',(0,-3,1.52),(0,0,1.505),.40),('Quarter',(.90,-2.7,1.52),(0,0,1.495),.50),
    ('Profile',(3,-.1,1.51),(0,0,1.49),.45),('Body',(.5,-3,1.04),(0,0,.83),1.89),('Torso',(.95,-3,1.24),(0,0,1.185),.79)]
for label,pos,target,scale in views:
    cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
    scene.render.filepath=str(OUT/('Finish-'+label+'.png'));bpy.ops.render.render(write_still=True)
for img in bpy.data.images:
    if img.source=='FILE' and img.has_data:
        try:img.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Anatomy-Finish.blend'))
(OUT/'finish-report.json').write_text(json.dumps({'source':'Seoha-Anatomy.blend','bones':len(rig.data.bones),
    'expressions':len(face.data.shape_keys.key_blocks)-1,'art_target_achieved':False,'gameplay_connected':False,
    'review_images':[v[0] for v in views]},indent=2),encoding='utf8')

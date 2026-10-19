"""Seoha likeness study, Blender 4.5. Run with blender -b -t 8 --python this_file.

Preserves the licensed MakeHuman deformation foundation. Rebuilds the head,
eyes, hair and costume as separate geometry; never projects portrait pixels.
Output is intentionally outside Unity Resources until the visual review passes.
"""
from pathlib import Path
import bpy, bmesh, math, json, sys
from mathutils import Vector, Quaternion
from mathutils.kdtree import KDTree
from math import sin, cos, pi, exp, sqrt

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).resolve().parent))
OUT=ROOT/'Artifacts/SeohaStudio';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Artifacts/CharacterLab/Refined/Seo-Trial.blend'))
body=bpy.data.objects['Seoha fitted body and costume foundation'];rig=bpy.data.objects['Seoha_Rig']
for o in list(bpy.data.objects):
    if o not in (body,rig):bpy.data.objects.remove(o,do_unlink=True)
rig.animation_data_clear()
for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
for m in list(body.modifiers):
    if m.type!='ARMATURE':body.modifiers.remove(m)
body.name='Seoha / weighted anatomical body'
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>1.477 or abs(v.co.x)<.072 and v.co.z>1.453],context='VERTS')
bm.to_mesh(body.data);bm.free();body.data.update()
# Undo the old radial-basis torso flattening while retaining imported weights.
for v in body.data.vertices:
    x,y,z=v.co
    torso=max(0,min(1,(.19-abs(x))/.045))*max(0,min(1,(z-.93)/.09))
    v.co.y=-.018+(y+.018)*(1+1.5*torso)
    # Small compressed cloth folds around bent joints, not a skin-tight paint.
    if .35<z<.94:
        angle=math.atan2(y+.003,abs(x)-.126)
        knee=exp(-((z-.60)/.095)**2)
        fold=.0018*knee*sin(z*172+angle*2.3)
        v.co.x+=(1 if x>0 else -1)*fold*cos(angle);v.co.y+=fold*sin(angle)
body.data.update()
kd=KDTree(len(body.data.vertices))
for v in body.data.vertices:kd.insert(v.co,v.index)
kd.balance()
materials={}
def mat(name,c,rough=.6,metal=0,glow=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=glow
    materials[name]=m;return m
skin=mat('01 / peach porcelain',(.78,.51,.43),.82,glow=.13)
cloth=mat('02 / charcoal woven knit',(.012,.014,.021),.85)
leather=mat('03 / midnight jacket leather',(.019,.022,.034),.57,.07)
panelmat=mat('04 / inset ballistic weave',(.027,.031,.049),.62,.12)
lining=mat('05 / cool satin collar lining',(.14,.17,.22),.58)
pants=mat('06 / graphite stretch trousers',(.021,.022,.034),.72)
steel=mat('07 / brushed titanium',(.21,.27,.33),.29,.8)
darksteel=mat('08 / dark anodized hardware',(.036,.046,.065),.35,.68)
seam=mat('09 / grey-violet piping',(.072,.082,.108),.63)
cyan=mat('10 / bounded cyan emitters',(.012,.49,.66),.32,.4,.85)
ink=mat('11 / plum lash ink',(.016,.006,.015),.73,glow=.15)
lip=mat('12 / muted rose lip',(.46,.19,.19),.77,glow=.12)
white=mat('13 / warm eye white',(.86,.81,.83),.53,glow=.3)
iris=mat('14 / violet iris',(.18,.069,.255),.4,glow=.4)
irislight=mat('15 / lavender iris light',(.42,.23,.56),.49,glow=.35)
shine=mat('16 / eye glints',(.98,.98,1),.23,glow=.7)
hairmats=[mat('Hair / lilac '+str(i),(.24+i*.037,.205+i*.034,.335+i*.046),.46,.015,.06) for i in range(7)]
hairshade=hairmats[0];hairbase=hairmats[4]
for fabric,scale in [(leather,240),(cloth,410),(pants,310),(panelmat,270)]:
    nodes=fabric.node_tree.nodes;links=fabric.node_tree.links;tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=scale;tex.inputs['Detail'].default_value=2
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.10;bump.inputs['Distance'].default_value=.00045;links.new(tex.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs[0],nodes.get('Principled BSDF').inputs['Normal'])
# The lock UVs run across/along each strand; fine fibres follow the sculpted flow.
for hm in hairmats:
    nodes=hm.node_tree.nodes;links=hm.node_tree.links;p=nodes.get('Principled BSDF');base=p.inputs['Base Color'].default_value[:]
    uv=nodes.new('ShaderNodeTexCoord');scale=nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(150,2.5,1);links.new(uv.outputs['UV'],scale.inputs[0])
    noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1;noise.inputs['Detail'].default_value=2;links.new(scale.outputs[0],noise.inputs['Vector'])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.20;ramp.color_ramp.elements[0].color=tuple(c*.93 for c in base[:3])+(1,);ramp.color_ramp.elements[1].position=.80;ramp.color_ramp.elements[1].color=tuple(c*1.04 for c in base[:3])+(1,)
    links.new(noise.outputs['Fac'],ramp.inputs[0])
    split=nodes.new('ShaderNodeSeparateXYZ');links.new(uv.outputs['UV'],split.inputs[0])
    def hair_ramp(axis,stops):
        r=nodes.new('ShaderNodeValToRGB');r.color_ramp.interpolation='B_SPLINE'
        for i,(pos,value) in enumerate(stops):
            e=r.color_ramp.elements[i] if i<2 else r.color_ramp.elements.new(pos)
            e.position=pos;e.color=(value,value,value,1)
        links.new(split.outputs[axis],r.inputs[0]);return r.outputs[0]
    across=hair_ramp('X',[(0,.57),(1,.61),(.14,.84),(.37,1.03),(.51,1.14),(.66,1.02),(.86,.80)])
    along=hair_ramp('Y',[(0,.70),(1,.75),(.21,.91),(.43,1.10),(.57,1.07),(.76,.85)])
    a=nodes.new('ShaderNodeMixRGB');a.blend_type='MULTIPLY';a.inputs[0].default_value=1;links.new(ramp.outputs[0],a.inputs[1]);links.new(across,a.inputs[2])
    b=nodes.new('ShaderNodeMixRGB');b.blend_type='MULTIPLY';b.inputs[0].default_value=1;links.new(a.outputs[0],b.inputs[1]);links.new(along,b.inputs[2]);links.new(b.outputs[0],p.inputs['Base Color'])
    p.inputs['Specular IOR Level'].default_value=.24

def bind(o,bone=None):
    o.parent=rig
    if bone:
        g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
    else:
        groups={}
        for v in o.data.vertices:
            weights={};near=kd.find_n(v.co,4)
            total=sum(1/max(.0001,d)**2 for _,_,d in near)
            for _,idx,d in near:
                influence=(1/max(.0001,d)**2)/total
                for old in body.data.vertices[idx].groups:
                    name=body.vertex_groups[old.group].name
                    if name not in rig.data.bones:continue
                    weights[name]=weights.get(name,0)+old.weight*influence
            for name,weight in weights.items():
                if name not in groups:groups[name]=o.vertex_groups.new(name=name)
                groups[name].add([v.index],weight,'REPLACE')
    mod=o.modifiers.new('Skin deformation','ARMATURE');mod.object=rig
    return o
def mesh(name,v,f,material,bone='head',sub=0):
    m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);m.materials.append(material)
    for p in m.polygons:p.use_smooth=True
    if bone is not False:bind(o,bone)
    if sub:s=o.modifiers.new('Surface subdivision','SUBSURF');s.levels=sub;s.render_levels=sub
    return o
def apply_xform(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
def ellipsoid(name,at,scale,material,bone='head',segments=32):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=20,location=at)
    o=bpy.context.object;o.name=name;o.scale=scale;apply_xform(o);o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    if bone is not False:bind(o,bone)
    return o
def box(name,at,size,material,bone=None,rounding=.002):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.scale=size;apply_xform(o)
    o.data.materials.append(material);b=o.modifiers.new('Manufactured edge radius','BEVEL');b.width=rounding;b.segments=3
    if bone is not False:bind(o,bone)
    return o
def line(name,coords,radius,material,bone='head',cyclic=False):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=12;cu.bevel_depth=radius;cu.bevel_resolution=2
    s=cu.splines.new('BEZIER');s.bezier_points.add(len(coords)-1);s.use_cyclic_u=cyclic
    for p,co in zip(s.bezier_points,coords):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(o);cu.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
    if bone is not False:bind(o,bone)
    return o
def catmull(points,t):
    n=len(points)-1;x=t*n;i=min(n-1,int(x));u=x-i
    a=Vector(points[max(0,i-1)]);b=Vector(points[i]);c=Vector(points[i+1]);d=Vector(points[min(n,i+2)])
    return .5*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u)
def garment_loft(name,sections,material,bone=None,sides=48,sub=1):
    # Cross sections: z, centre x, centre y, radius x, radius y.
    v=[];f=[]
    tailored=name.startswith(('Jacket / continuous','Shirt / fitted'))
    if tailored:
        sections=[catmull(sections,j/47) for j in range(48)]
    for row,(z,x,y,rx,ry) in enumerate(sections):
        for j in range(sides):
            a=2*pi*j/sides
            fold=0
            if tailored:
                elbow=exp(-((z-1.22)/.053)**2)
                shoulderfold=exp(-((z-1.426)/.027)**2)
                fold=.0027*elbow*sin(z*230+a*1.5)+.0013*shoulderfold*sin(a*5+z*55)
            v.append((x+(rx+fold)*cos(a),y+(ry+fold)*sin(a),z))
    for k in range(len(sections)-1):
        for j in range(sides):a=k*sides+j;b=k*sides+(j+1)%sides;f.append((a,b,b+sides,a+sides))
    if tailored:f.append(tuple(reversed(range(sides))))
    o=mesh(name,v,f,material,bone,sub);s=o.modifiers.new('Tailored shell thickness','SOLIDIFY');s.thickness=.002
    return o

# Opaque material regions on the original weighted anatomy. Garment thickness is separate.
body.data.materials.clear()
for m in [skin,cloth,leather,pants]:body.data.materials.append(m)
def region(p):
    x,y,z=p
    if z<1.084:return 3
    if x<-.168 and z<1.435:return 2
    if x>.166 and z<1.143:return 2
    if z>1.395:return 2
    if abs(x)<.167 and 1.192<z<1.44:return 1
    return 0
for p in body.data.polygons:p.material_index=region(p.center);p.use_smooth=True
# Analytic garment boundaries prevent polygon-shaped skin/cloth stair steps.
surface=skin.copy();surface.name='Body / continuous garment masks';nodes=surface.node_tree.nodes;links=surface.node_tree.links
restpos=body.data.attributes.new(name='TailoringRest',type='FLOAT_VECTOR',domain='POINT')
for v in body.data.vertices:restpos.data[v.index].vector=v.co
coords=nodes.new('ShaderNodeAttribute');coords.attribute_name='TailoringRest';split=nodes.new('ShaderNodeSeparateXYZ');links.new(coords.outputs['Vector'],split.inputs[0])
def op(operation,a,b):
    n=nodes.new('ShaderNodeMath');n.operation=operation
    if isinstance(a,(int,float)):n.inputs[0].default_value=a
    else:links.new(a,n.inputs[0])
    if isinstance(b,(int,float)):n.inputs[1].default_value=b
    else:links.new(b,n.inputs[1])
    return n.outputs[0]
x=split.outputs['X'];z=split.outputs['Z'];absolute=op('ABSOLUTE',x,0)
waist=op('LESS_THAN',z,1.077);top=op('MULTIPLY',op('GREATER_THAN',z,1.19),op('LESS_THAN',absolute,.165))
right=op('LESS_THAN',x,-.164);left=op('MULTIPLY',op('GREATER_THAN',x,.164),op('LESS_THAN',z,1.147))
shoulder=op('MULTIPLY',op('GREATER_THAN',z,1.341),op('GREATER_THAN',absolute,.11))
mask=op('MAXIMUM',shoulder,op('MAXIMUM',op('MAXIMUM',waist,top),op('MAXIMUM',right,left)))
exposure=body.data.attributes.new(name='FingerlessGlove',type='FLOAT',domain='POINT')
for v in body.data.vertices:
    weight=0
    for g in v.groups:
        name=body.vertex_groups[g.group].name
        if name.startswith('finger') and ('-2.' in name or '-3.' in name):weight+=g.weight
    exposure.data[v.index].value=min(1,weight*1.7)
finger=nodes.new('ShaderNodeAttribute');finger.attribute_name='FingerlessGlove'
mask=op('MULTIPLY',mask,op('SUBTRACT',1,finger.outputs['Fac']))
mix=nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.78,.51,.43,1);mix.inputs[2].default_value=(.019,.021,.03,1);links.new(mask,mix.inputs[0]);links.new(mix.outputs[0],nodes.get('Principled BSDF').inputs['Base Color']);links.new(mix.outputs[0],nodes.get('Principled BSDF').inputs['Emission Color'])
body.data.materials.clear();body.data.materials.append(surface)
for p in body.data.polygons:p.material_index=0
s=body.modifiers.new('Anatomical finish','SUBSURF');s.levels=1;s.render_levels=1

# A clean weighted neck and collar cover the old head/neck cut without a gap.
garment_loft('Neck / continuous transition',[(1.424,0,.010,.046,.039),(1.452,0,.012,.033,.031),(1.49,0,.018,.027,.028),(1.535,0,.021,.033,.032)],skin,'neck01')
garment_loft('Shirt / high choker',[(1.443,0,.009,.048,.045),(1.448,0,.009,.045,.043),(1.478,0,.011,.039,.038),(1.482,0,.011,.039,.038)],leather,'neck01')
line('Choker / front zip',[(0,-.037,1.448),(0,-.029,1.480)],.0015,steel,'neck01')
garment_loft('Jacket / continuous right sleeve',[(1.473,-.134,-.012,.004,.020),(1.463,-.145,-.013,.027,.041),(1.439,-.157,-.017,.046,.053),(1.406,-.171,-.018,.052,.055),(1.35,-.189,-.022,.049,.049),(1.278,-.209,-.025,.043,.042),(1.224,-.224,-.027,.040,.04),(1.184,-.24,-.03,.039,.039),(1.108,-.262,-.036,.034,.036),(1.032,-.284,-.04,.028,.029),(1.012,-.29,-.040,.028,.030)],leather)
garment_loft('Jacket / continuous left sleeve',[(1.473,.134,-.012,.004,.020),(1.463,.145,-.013,.027,.041),(1.439,.157,-.017,.046,.053),(1.407,.171,-.018,.053,.055),(1.354,.188,-.020,.049,.052),(1.341,.192,-.022,.049,.052)],leather)
garment_loft('Jacket / left sleeve hem',[(1.341,.192,-.022,.05,.053),(1.352,.189,-.02,.05,.053)],panelmat)
garment_loft('Shirt / clean cropped hem',[(1.186,0,-.035,.096,.067),(1.202,0,-.035,.099,.07)],cloth)
garment_loft('Shirt / fitted continuous neckline',[(1.188,0,-.022,.100,.072),(1.215,0,-.020,.106,.081),(1.290,0,-.015,.132,.105),(1.355,0,-.010,.151,.125),(1.400,0,-.013,.149,.126),(1.432,0,-.009,.121,.091),(1.444,0,.005,.077,.059),(1.451,0,.009,.050,.046),(1.466,0,.011,.041,.039)],cloth)

# Actual open jacket. Its panels inherit the underlying anatomical weights.
rows=[(1.208,.104,.09),(1.224,.109,.096),(1.30,.13,.117),(1.36,.15,.128),(1.402,.16,.123),(1.442,.117,.085)]
v=[];f=[];N=40
for z,rx,ry in rows:
    # Keep the jacket outside the new continuous undershirt at the back too.
    ry+=.010*max(0,min(1,(z-1.215)/.04))*max(0,min(1,(1.456-z)/.025))
    rx+=.002
    for j in range(N):
        a=.75+(2*pi-1.50)*j/(N-1)
        fold=.0022*sin(z*111+a*2)*sin(a)**2
        v.append(((rx+fold)*sin(a),-.01-(ry+fold)*cos(a),z))
for j in range(len(rows)-1):
    for k in range(N-1):a=j*N+k;f.append((a,a+1,a+N+1,a+N))
jacket=mesh('Jacket / open tailored torso',v,f,leather,None,2)
s=jacket.modifiers.new('Leather shell and lining','SOLIDIFY');s.thickness=.0035
for side in [-1,1]:
    coords=[(side*.046,-.036,1.496),(side*.088,-.087,1.452),(side*.116,-.136,1.364),(side*.096,-.133,1.30),(side*.078,-.088,1.218)]
    verts=[]
    for x,y,z in coords:verts.extend([(x,y,z),(x-side*.024,y-.007,z-.025)])
    lapel=mesh('Jacket / folded lapel '+str(side),verts,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(4)],lining,None,1)
    s=lapel.modifiers.new('Fold thickness','SOLIDIFY');s.thickness=.0025
    line('Cyan seam / lapel '+str(side),coords,.0008,cyan,None)
    line('Zipper track '+str(side),[(x-side*.018,y-.010,z-.018) for x,y,z in coords],.0013,steel,None)
    for j in range(19):
        q=catmull(coords,.10+j*.043);q.y-=.003
        box('Zipper / individual tooth',q,(.004,.002,.002),steel,None,.0004)

# Collar, belt, loops, articulated sleeve panels and fasteners.
for side in [-1,1]:
    base=[(side*.088,-.080,1.439),(side*.105,-.029,1.445),(side*.085,.025,1.446),(side*.05,.069,1.454),(0,.076,1.458)]
    rim=[(side*.047,-.048,1.505),(side*.079,-.008,1.507),(side*.079,.044,1.509),(side*.045,.081,1.511),(0,.087,1.511)]
    rim=[(x,y,z-.008) for x,y,z in rim]
    v=[];f=[];steps=28;bands=7
    for j in range(steps):
        t=j/(steps-1);lo=catmull(base,t);hi=catmull(rim,t)
        for k in range(bands):v.append(lo.lerp(hi,k/(bands-1)))
    for j in range(steps-1):
        for k in range(bands-1):a=j*bands+k;f.append((a,a+1,a+bands+1,a+bands))
    panel=mesh('Collar / shaped standing leaf '+str(side),v,f,lining,'neck01',1)
    panel.data.materials.append(leather)
    s=panel.modifiers.new('Collar cloth backing','SOLIDIFY');s.thickness=.0024;s.material_offset=1
    line('Collar / rolled outer edge '+str(side),rim,.0012,leather,'neck01')
    line('Collar / fine cyan piping '+str(side),[(x,y-.0003,z-.0015) for x,y,z in rim[:3]],.0005,cyan,'neck01')
v=[];f=[];N=64
for z in [1.061,1.095]:
    for j in range(N):a=j*2*pi/N;v.append((.125*sin(a),-.009-.099*cos(a),z))
for j in range(N):f.append((j,(j+1)%N,(j+1)%N+N,j+N))
belt=mesh('Belt / continuous leather band',v,f,leather,None);s=belt.modifiers.new('Belt thickness','SOLIDIFY');s.thickness=.003
for a,b in [((-.023,-.113,1.067),(.019,-.113,1.067)),((.019,-.113,1.067),(.019,-.113,1.097)),((.019,-.113,1.097),(-.023,-.113,1.097)),((-.023,-.113,1.097),(-.023,-.113,1.067))]:line('Belt / squared buckle',[a,b],.0025,steel,'spine05')
line('Belt / buckle tongue',[(0,-.115,1.084),(.025,-.115,1.084)],.0015,steel,'spine05')
for j in range(10):
    a=j*2*pi/10
    box('Belt / retaining loop',(.129*sin(a),-.009-.104*cos(a),1.079),(.011,.010,.044),panelmat,None,.002)
for j in range(5):
    z=1.394-j*.058;x=-.177-j*.018
    v=[];f=[];rows=7;cols=11;radius=.052-j*.004;cy=-.020-j*.004
    for row in range(rows):
        t=-1+2*row/(rows-1)
        for k in range(cols):
            a=(-.70+1.40*k/(cols-1))*(1-.14*abs(t)**5)
            v.append((x+radius*sin(a),cy-radius*cos(a)-.005-.002*(1-t*t),z+t*.025))
    for row in range(rows-1):
        for k in range(cols-1):a=row*cols+k;f.append((a,a+1,a+cols+1,a+cols))
    plate=mesh('Sleeve / curved armour '+str(j),v,f,panelmat,None,1)
    thick=plate.modifiers.new('Armour edge thickness','SOLIDIFY');thick.thickness=.002
    for sign in [-1,1]:
        a=sign*.44;ellipsoid('Sleeve / titanium rivet',(x+radius*sin(a),cy-radius*cos(a)-.008,z+.014),(.0015,.001,.0015),steel,None,16)
for side in [-1,1]:
    box('Thigh / fitted utility pouch',(side*.145,-.078,.936),(.052,.039,.108),leather,None,.006)
    box('Thigh / utility pouch flap',(side*.145,-.10,.963),(.054,.012,.033),panelmat,None,.003)
    box('Thigh / pocket fastener',(side*.145,-.11,.954),(.014,.005,.023),steel,None,.002)
    line('Thigh / holster seam',[(side*.14,-.098,.989),(side*.14,-.10,.95),(side*.14,-.095,.896)],.0012,steel,None)
    line('Trouser / knee articulation',[(side*.107,-.070,.624),(side*.123,-.081,.598),(side*.137,-.067,.576)],.0014,seam,None)
    line('Trouser / upper pocket stitching '+str(side),[(side*.130,-.062,1.039),(side*.147,-.057,1.015),(side*.153,-.057,.985)],.0007,seam,None)
box('Left wrist / signal terminal',(.263,-.073,1.122),(.045,.025,.077),darksteel,'lowerarm02.L',.009)
box('Left wrist / inset screen',(.263,-.089,1.131),(.027,.003,.038),ink,'lowerarm02.L',.004)
for z in [1.123,1.132,1.141]:box('Left wrist / illuminated status',(.261,-.091,z),(.019,.002,.0015),cyan,'lowerarm02.L',.0004)

# Shoes keep volume around the toes instead of a front/back projected rectangle.
for side in [-1,1]:
    v=[];f=[];N=40
    boot_sections=[(.001,.045,.09,-.051),(.005,.046,.091,-.051),(.043,.047,.092,-.051),(.071,.046,.093,-.051),(.105,.044,.079,-.040),(.147,.039,.048,-.012),(.205,.038,.039,-.003),(.276,.043,.038,0)]
    for z,rx,ry,cy in boot_sections:
        for j in range(N):a=j*2*pi/N;v.append((side*.14+rx*cos(a),cy+ry*sin(a),z))
    for j in range(len(boot_sections)-1):
        for k in range(N):a=j*N+k;b=j*N+(k+1)%N;f.append((a,b,b+N,a+N))
    f.append(tuple(reversed(range(N))));boot=mesh('Boot / contoured leather '+str(side),v,f,leather,'foot.L' if side>0 else 'foot.R',1)
    for z in [.12,.153,.185,.217,.249]:
        line('Boot / cross lacing',[(side*.14-.021,-.05,z),(side*.14+.021,-.054,z+.02)],.0014,seam,'foot.L' if side>0 else 'foot.R')
        line('Boot / cross lacing',[(side*.14+.021,-.05,z),(side*.14-.021,-.054,z+.02)],.0014,seam,'foot.L' if side>0 else 'foot.R')

# Head: smooth explicit jaw/cheek/forehead sections, nose and lip curvature.
sections=[(1.501,.001,.054,.010),(1.512,.023,.068,.029),(1.532,.047,.077,.047),(1.554,.063,.080,.065),(1.580,.076,.079,.079),(1.608,.083,.077,.089),(1.638,.086,.073,.095),(1.672,.087,.069,.090),(1.706,.080,.060,.076),(1.733,.059,.041,.054),(1.749,.028,.02,.026),(1.754,.001,.001,.001)]
def section(z):
    for a,b in zip(sections,sections[1:]):
        if a[0]<=z<=b[0]:t=(z-a[0])/(b[0]-a[0]);return [a[k]*(1-t)+b[k]*t for k in range(1,4)]
    return sections[0][1:] if z<sections[0][0] else sections[-1][1:]
def facey(x,z):
    rx,front,back=section(z);r=min(.999,abs(x)/max(.001,rx));y=-front*max(.01,1-r*r)**.34
    y-=.014*exp(-(x/.009)**2-((z-1.578)/.014)**2)
    y-=.003*exp(-(x/.008)**2-((z-1.600)/.018)**2)
    y-=.004*exp(-(x/.020)**2-((z-1.545)/.010)**2)
    return y
v=[];f=[];N=96;R=72
for j in range(R):
    z=1.501+(1.754-1.501)*j/(R-1);rx,front,back=section(z)
    for k in range(N):
        a=k*2*pi/N;x=rx*sin(a);y=facey(x,z) if cos(a)>0 else -back*cos(a)
        v.append((x,y,z))
for j in range(R-1):
    for k in range(N):a=j*N+k;b=j*N+(k+1)%N;f.append((a,b,b+N,a+N))
head=mesh('Face / sculpted anime topology',v,f,skin,'head',1)
# Vertex colour cheeks and nose: clean all-angle surfacing with no image seams.
attr=head.data.color_attributes.new(name='Complexion',type='FLOAT_COLOR',domain='POINT')
for vert in head.data.vertices:
    x,y,z=vert.co;blush=.24*exp(-((abs(x)-.05)/.028)**2-((z-1.583)/.017)**2)*max(0,-y/.07)
    attr.data[vert.index].color=(.78,.51-blush*.35,.43-blush*.21,1)
facemat=skin.copy();facemat.name='Face / soft vertex-painted blush';head.data.materials[0]=facemat
n=facemat.node_tree.nodes.new('ShaderNodeVertexColor');n.layer_name='Complexion'
facemat.node_tree.links.new(n.outputs['Color'],facemat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
for side in [-1,1]:
    ellipsoid('Ear / cartilage '+str(side),(side*.086,.002,1.59),(.014,.013,.028),skin)
    line('Ear / helix '+str(side),[(side*.096,-.009,1.607),(side*.10,-.013,1.591),(side*.092,-.013,1.575)],.0025,lip)
    ellipsoid('Earring / stud',(side*.091,-.008,1.565),(.004,.003,.004),steel)
    earpoints=[(side*.091,-.012,1.558),(side*.098,-.012,1.536),(side*.083,-.012,1.547)]
    for j in range(3):line('Earring / triangular drop',[earpoints[j],earpoints[(j+1)%3]],.0017,darksteel)

# Almond eyes with independent sclera, curved iris, ink eyelids and catchlights.
for side in [-1,1]:
    cx=side*.039;cz=1.610;w=.030;h=.0092
    def eyepos(u,v):
        x=cx+side*u*w;z=cz+v*h+.003*u
        return Vector((x,facey(x,z)-.0019-.003*max(0,1-u*u-v*v),z))
    vs=[eyepos(0,0)];fs=[]
    for j in range(64):a=j*2*pi/64;vs.append(eyepos(cos(a),sin(a)*(.85 if sin(a)<0 else 1)))
    for j in range(64):fs.append((0,j+1,(j+1)%64+1))
    eye=mesh('Eye / almond white '+str(side),vs,fs,white)
    # Circular violet iris is bounded inside the sclera's almond edge.
    vs=[eyepos(.00,-.04)+Vector((0,-.0007,0))];fs=[]
    for j in range(64):
        a=j*2*pi/64;vs.append(eyepos(cos(a)*.39,sin(a)*.86-.035)+Vector((0,-.0008,0)))
    for j in range(64):fs.append((0,j+1,(j+1)%64+1))
    o=mesh('Eye / violet iris '+str(side),vs,fs,iris);o.data.materials.append(irislight)
    for p in o.data.polygons:
        if p.center.z<cz-.002:p.material_index=1
    coords=[eyepos(cos(a)*.39,sin(a)*.86-.035)+Vector((0,-.0012,0)) for a in [j*2*pi/40 for j in range(40)]]
    line('Eye / limbal ring '+str(side),coords,.0008,ink,cyclic=True)
    q=eyepos(0,-.03);q.y-=.0019;ellipsoid('Eye / dark pupil '+str(side),q,(.0032,.001,.0048),ink)
    q=eyepos(-.10,.38);q.y-=.003;ellipsoid('Eye / large glint '+str(side),q,(.0022,.001,.0023),shine)
    q=eyepos(.18,-.4);q.y-=.002;ellipsoid('Eye / small glint '+str(side),q,(.0013,.001,.0014),shine)
    upper=[eyepos(u,sqrt(max(0,1-u*u)))+Vector((0,-.0015,0)) for u in [-1,-.7,-.35,0,.35,.7,1]]
    upper.append(eyepos(1.20,.27));line('Eye / upper tapered ink '+str(side),upper,.0016,ink)
    line('Eye / lower soft rim '+str(side),[eyepos(u,-.85*sqrt(max(0,1-u*u)))+Vector((0,-.001,0)) for u in [-.85,-.45,0,.45,.85]],.00065,lip)
    for u in [.65,.83,1.0]:
        p=eyepos(u,sqrt(max(0,1-u*u)));q=p+Vector((side*.006,-.001,.0035))
        line('Eye / individual eyelash',[p,q],.001,ink)
    brow=[]
    for u in [-.90,-.4,.2,.65,1.0]:
        x=cx+side*u*.029;z=cz+.024+.004*sin((u+1)*pi/2)
        brow.append((x,facey(x,z)-.0015,z))
    line('Brow / lilac taupe '+str(side),brow,.0013,hairshade)
line('Mouth / quiet relaxed expression',[(-.016,facey(-.016,1.544)-.001,1.544),(0,facey(0,1.542)-.0015,1.542),(.016,facey(.016,1.544)-.001,1.544)],.0008,lip)
line('Mouth / lower lip highlight',[(-.008,facey(-.008,1.539)-.001,1.539),(0,facey(0,1.538)-.002,1.538),(.008,facey(.008,1.539)-.001,1.539)],.0007,skin)

# Original sculpted hair with skull-following flow and tapered primary shapes.
from sculpt_hair import build as build_hair
build_hair(mesh, ellipsoid, line, hairmats, catmull, section, facey)

# Dedicated facial albedo: calibrated landmarks on the contoured head only.
# It is not a portrait billboard or a front/back costume projection.
albedo=OUT/'Seoha-Face-Albedo.png'
if albedo.exists():
    for o in list(bpy.data.objects):
        if o.name.startswith(('Eye /','Brow /','Mouth /')):bpy.data.objects.remove(o,do_unlink=True)
    image=bpy.data.images.load(str(albedo),check_existing=True);image.pack()
    facepaint=mat('Face / Seoha illustrated albedo',(.8,.6,.55),.85,glow=.30)
    nodes=facepaint.node_tree.nodes;links=facepaint.node_tree.links
    tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.extension='EXTEND'
    links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color']);links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Emission Color'])
    # Texture coordinates measured from the generated sheet, not assumed from its prompt.
    landmarks=[(1.49,1.02),(1.544,.729),(1.577,.611),(1.61,.398),(1.638,.249),(1.705,-.03),(1.76,-.20)]
    def uvface(co):
        z=co.z;py=0
        for a,b in zip(landmarks,landmarks[1:]):
            if a[0]<=z<=b[0]:t=(z-a[0])/(b[0]-a[0]);py=a[1]*(1-t)+b[1]*t;break
        u=.5+co.x/.169
        # Widen almond eyes without moving the nose, mouth or eye spacing.
        eyezone=max(0,min(1,(z-1.58)/.014))*max(0,min(1,(1.645-z)/.014))
        if abs(co.x)>.015:
            centre=.5+(.039 if co.x>0 else -.039)/.169
            widened=centre+(u-centre)/1.17;u=u*(1-eyezone)+widened*eyezone
        return (u,1-py)
    head.data.materials.clear();head.data.materials.append(facepaint);head.data.materials.append(skin)
    uv=head.data.uv_layers.new(name='Seoha calibrated face')
    for p in head.data.polygons:
        p.material_index=0
        for li in p.loop_indices:uv.data[li].uv=uvface(head.data.vertices[head.data.loops[li].vertex_index].co)
    # Smooth blend into the skin tone at the temples and under the jaw.
    mix=nodes.new('ShaderNodeMixRGB');colour=skin.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value[:];mix.inputs[2].default_value=colour
    attribute=head.data.color_attributes.new(name='Face feather',type='FLOAT_COLOR',domain='POINT')
    for vert in head.data.vertices:
        x,y,z=vert.co;edge=max(0,min(1,(abs(x)-.069)/.016));chin=max(0,min(1,(1.525-z)/.025));forehead=max(0,min(1,(z-1.669)/.022));rear=max(0,min(1,(y+.039)/.019));a=max(edge,chin,forehead,rear)
        attribute.data[vert.index].color=(a,a,a,1)
    color=nodes.new('ShaderNodeVertexColor');color.layer_name='Face feather';links.new(color.outputs['Color'],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],nodes.get('Principled BSDF').inputs['Base Color']);links.new(mix.outputs[0],nodes.get('Principled BSDF').inputs['Emission Color'])
    strength=nodes.new('ShaderNodeMath');strength.operation='MULTIPLY_ADD';strength.inputs[1].default_value=-.17;strength.inputs[2].default_value=.30;links.new(color.outputs['Color'],strength.inputs[0]);links.new(strength.outputs[0],nodes.get('Principled BSDF').inputs['Emission Strength'])
    eyes=[]
    for side in [-1,1]:
        v=[];f=[];cols=25;rows=13
        for j in range(rows):
            b=-1+2*j/(rows-1)
            for k in range(cols):
                a=-1+2*k/(cols-1);x=side*.039+a*.032;z=1.610+b*.016
                depth=.0015*max(0,1-a*a)*max(0,1-b*b)
                v.append((x,facey(x,z)-depth-.00015,z))
        for j in range(rows-1):
            for k in range(cols-1):a=j*cols+k;f.append((a,a+1,a+cols+1,a+cols))
        o=mesh('Eye / contoured illustrated lid '+str(side),v,f,facepaint,'head',1);uv=o.data.uv_layers.new(name='Seoha calibrated face')
        for loop in o.data.loops:uv.data[loop.index].uv=uvface(o.data.vertices[loop.vertex_index].co)
        eyes.append(o)
    for o in [head]+eyes:
        o.shape_key_add(name='Basis')
        for side,label in [(-1,'R'),(1,'L')]:
            mask=o.data.attributes.new(name='ClosedLid.'+label,type='FLOAT',domain='POINT')
            for vert in o.data.vertices:
                x,y,z=vert.co;dx=abs(x-side*.039);dz=abs(z-1.610)
                mask.data[vert.index].value=max(0,min(1,(.039-dx)/.008))*max(0,min(1,(.021-dz)/.008)) if y<-.015 else 0
            shape=o.shape_key_add(name='Blink.'+label)
            for pt in shape.data:
                x,y,z=pt.co;dx=abs(x-side*.039);dz=abs(z-1.609)
                if y>-.015 or dx>.038 or dz>.031:continue
                xf=max(0,min(1,(.038-dx)/.007));zf=max(0,min(1,(.023-dz)/.009))
                # Do not squeeze the entire painted face and brow into a line.
                # A shallow lid surface advances while the eye area crossfades.
                pt.co.y-=.0009*xf*zf
            rig['blink_'+label]=0.0
            f=shape.driver_add('value');v=f.driver.variables.new();v.name='blink';v.targets[0].id=rig;v.targets[0].data_path='["blink_'+label+'"]';f.driver.expression='blink'
    # Sample adjacent clear skin from the same albedo for a closed lid, keeping
    # the eyes' colour and the eyebrows independent of the blink deformation.
    uvnode=nodes.new('ShaderNodeTexCoord');offset=nodes.new('ShaderNodeVectorMath');offset.operation='ADD';offset.inputs[1].default_value=(0,-.17,0);links.new(uvnode.outputs['UV'],offset.inputs[0])
    lidtex=nodes.new('ShaderNodeTexImage');lidtex.image=image;lidtex.extension='EXTEND';links.new(offset.outputs[0],lidtex.inputs['Vector'])
    factors=[]
    for label in ['L','R']:
        attr=nodes.new('ShaderNodeAttribute');attr.attribute_name='ClosedLid.'+label
        value=nodes.new('ShaderNodeValue');value.label='Closed eyelid '+label
        driver=value.outputs[0].driver_add('default_value');var=driver.driver.variables.new();var.name='blink';var.targets[0].id=rig;var.targets[0].data_path='["blink_'+label+'"]';driver.driver.expression='blink'
        mul=nodes.new('ShaderNodeMath');mul.operation='MULTIPLY';links.new(attr.outputs['Fac'],mul.inputs[0]);links.new(value.outputs[0],mul.inputs[1]);factors.append(mul.outputs[0])
    maximum=nodes.new('ShaderNodeMath');maximum.operation='MAXIMUM';links.new(factors[0],maximum.inputs[0]);links.new(factors[1],maximum.inputs[1])
    eyelid=nodes.new('ShaderNodeMixRGB');links.new(maximum.outputs[0],eyelid.inputs[0]);links.new(mix.outputs[0],eyelid.inputs[1]);links.new(lidtex.outputs[0],eyelid.inputs[2])
    links.new(eyelid.outputs[0],nodes.get('Principled BSDF').inputs['Base Color']);links.new(eyelid.outputs[0],nodes.get('Principled BSDF').inputs['Emission Color'])
    smile=head.shape_key_add(name='SoftSmile')
    for pt in smile.data:
        x,y,z=pt.co
        if y<-.03:pt.co.z+=.0025*exp(-((abs(x)-.018)/.010)**2-((z-1.544)/.014)**2)
    for side,label in [(-1,'R'),(1,'L')]:
        points=[]
        for u in [-1,-.65,-.25,.2,.6,1]:
            x=side*.039+side*u*.026;z=1.610+.0085*sqrt(max(0,1-u*u))+.002*u
            points.append((x,facey(x,z)+.0015,z))
        lash=line('Eye / articulated upper lashes '+label,points,.0009,ink)
        lash.shape_key_add(name='Basis');shape=lash.shape_key_add(name='Blink.'+label)
        for pt in shape.data:
            pt.co.z=1.608+(pt.co.z-1.609)*.20;pt.co.y=facey(pt.co.x,pt.co.z)-.008
        f=shape.driver_add('value');v=f.driver.variables.new();v.name='blink';v.targets[0].id=rig;v.targets[0].data_path='["blink_'+label+'"]';f.driver.expression='blink'

# Secondary motion controls use explicit weighted chains, not static attached strips.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for side,label in [(-1,'R'),(1,'L')]:
    parent=rig.data.edit_bones['head']
    for j in range(3):
        b=rig.data.edit_bones.new('Hair.Front.'+label+'.'+str(j));b.head=(side*.081,-.074-j*.028,1.66-j*.10);b.tail=(side*.081,-.102-j*.028,1.56-j*.10);b.parent=parent;b.use_deform=True;parent=b
bpy.ops.object.mode_set(mode='OBJECT')
for o in bpy.data.objects:
    if not o.name.startswith('Hair / long face-framing lock'):continue
    side=1 if sum(v.co.x for v in o.data.vertices)>0 else -1;label='L' if side>0 else 'R';o.vertex_groups.clear()
    groups=[o.vertex_groups.new(name='head')]+[o.vertex_groups.new(name='Hair.Front.'+label+'.'+str(j)) for j in range(3)]
    for vert in o.data.vertices:
        t=max(0,min(2.999,(1.66-vert.co.z)/.10));j=int(t);f=t-j
        groups[j].add([vert.index],1-f,'REPLACE');groups[j+1].add([vert.index],f,'REPLACE')

# Clean studio setup. These are real mesh renders, not generated beauty images.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type!='CPU'
    scene.cycles.device='GPU'
except Exception:pass
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.21,.29,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.resolution_percentage=100
def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
for at,power,size,col in [((-2,-3,4),180,3,(1,.88,.81)),((2,-1,2.5),110,2,(.76,.82,1)),((0,2,3),260,2,(.65,.76,1))]:
    bpy.ops.object.light_add(type='AREA',location=at);o=bpy.context.object;o.name='Studio light';o.data.energy=power;o.data.size=size;o.data.color=col;aim(o,(0,0,1.35))
floor=box('Studio floor',(0,0,-.01),(5000,5000,.02),mat('Studio / charcoal',(.032,.040,.054)),False,0)
bpy.ops.object.camera_add(location=(0,-4,1.4));camera=bpy.context.object;camera.name='Likeness review camera';camera.data.type='ORTHO';scene.camera=camera
scene.render.resolution_x=960;scene.render.resolution_y=1200
rig.show_in_front=True
for o in bpy.context.selected_objects:o.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Likeness.blend'))
views=[('Portrait-Front',(0,-4,1.625),(0,0,1.56),.46),('Portrait-ThreeQuarter',(2,-4,1.66),(0,0,1.56),.46),('Portrait-Profile',(4,-.5,1.66),(0,0,1.56),.46),('Body-Front',(0,-5,1.08),(0,0,.9),1.98),('Body-Back',(2,5,1.15),(0,0,.9),1.98)]
if '--portrait-only' in sys.argv:views=views[:2]
for name,at,target,scale in views:
    camera.location=at;aim(camera,target);camera.data.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
camera.location=(1.8,-4,1.5);aim(camera,(0,0,1.05));camera.data.ortho_scale=1.98
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seoha-Likeness.blend'))
report={'status':'visual_review_required','shipping':False,'bones':len(rig.data.bones),'mesh_objects':sum(o.type=='MESH' and o.parent==rig for o in bpy.data.objects),'base_vertices':sum(len(o.data.vertices) for o in bpy.data.objects if o.type=='MESH' and o.parent==rig),'reference':'Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png','source':'MakeHuman / MPFB CC0 anatomical body plus original head, hair, costume geometry'}
(OUT/'model-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SEOHA_LIKENESS_READY',json.dumps(report))

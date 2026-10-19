"""Separate tailored surfaces, panel construction and physically modelled hardware."""
import math
import bpy,bmesh
from mathutils import Vector,Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
from atelier_common import plain,grain,collection,relocate,body_cage,catmull,g,sstep

def build_outfit(m,face):
    body=m.body
    leather,bs=plain('Atelier | graphite black leather',(.012,.014,.021),.53,.04);grain(leather,bs,390,.00010)
    lining,bs=plain('Atelier | cool woven collar lining',(.043,.046,.063),.80);grain(lining,bs,580,.00008)
    knit,bs=plain('Atelier | fitted technical knit',(.020,.018,.027),.80);grain(knit,bs,730,.00006)
    steel,bs=plain('Atelier | brushed titanium hardware',(.18,.19,.23),.36,.72)
    darksteel,bs=plain('Atelier | blackened fasteners',(.033,.036,.047),.32,.62)
    thread,_=plain('Atelier | grey topstitch thread',(.082,.080,.093),.82)
    cyan,bs=plain('Atelier | restrained cyan inlay',(.012,.23,.26),.48,.22)
    bs.inputs['Emission Color'].default_value=(.01,.17,.19,1);bs.inputs['Emission Strength'].default_value=.35
    toremove=['Seoha neck strap','Seoha fitted waist belt','Seoha belt stitch','Seoha titanium buckle frame','Seoha buckle tongue',
        'Seoha collar narrow signal welt','Seoha fitted right forearm shell','Forearm signal strip']
    for ob in list(bpy.context.scene.objects):
        if any(ob.name.startswith(p) for p in toremove):bpy.data.objects.remove(ob,do_unlink=True)
        elif ob.type=='MESH' and any(s in ob.name for s in ['tailored','lapel','jacket']):
            if 'raised collar' in ob.name:
                ob.data.materials[0]=lining
            elif 'folded lapel' in ob.name:
                ob.data.materials[0]=leather
            relocate(ob,m.outfit)

    # Remove only the existing shirt front beneath the new continuous garment.
    # The sleeve, jacket back, arm skin and weighted hands remain intact.
    bm=bmesh.new();bm.from_mesh(body.data)
    gone=[f for f in bm.faces if f.material_index==7 or (f.material_index==3 and abs(f.calc_center_median().x)<.105 and f.calc_center_median().y<-.028)]
    bmesh.ops.delete(bm,geom=gone,context='FACES_ONLY')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.to_mesh(body.data);bm.free();body.data.update()
    m.tree=KDTree(len(body.data.vertices))
    for v in body.data.vertices:m.tree.insert(v.co,v.index)
    m.tree.balance()
    skin_surface=BVHTree.FromPolygons([v.co for v in body.data.vertices],
        [tuple(p.vertices) for p in body.data.polygons if p.material_index in [0,5] and all(abs(body.data.vertices[i].co.x)<.16 for i in p.vertices)])

    # Continuous quad garment from the neck opening to a bound crop hem.
    # Profile values are authored in the current rest coordinates, metres.
    profile=[(1.145,.109,-.087,.075),(1.153,.110,-.088,.075),
        (1.180,.118,-.107,.077),(1.208,.130,-.131,.083),
        (1.241,.137,-.150,.090),(1.270,.133,-.139,.088),
        (1.302,.121,-.112,.086),(1.328,.095,-.087,.071),
        (1.348,.047,-.042,.057),(1.365,.034,-.031,.052),
        (1.380,.033,-.028,.050)]
    def profile_at(z):
        k=next((i for i in range(len(profile)-1) if profile[i][0]<=z<=profile[i+1][0]),len(profile)-2)
        a,b=profile[k],profile[k+1];t=(z-a[0])/(b[0]-a[0])
        return tuple(a[j]*(1-t)+b[j]*t for j in range(1,4))
    def shirt_y(x,z):
        rx,front,back=profile_at(max(profile[0][0],min(profile[-1][0],z)))
        cy=(front+back)*.5;ry=(back-front)*.5
        y=cy-ry*math.sqrt(max(.04,1-(x/rx)**2))
        hit=skin_surface.ray_cast(Vector((x,-.5,z)),Vector((0,1,0)),1)[0]
        if hit is not None:y=min(y,hit.y-.0035)
        return y
    vs=[];uv=[];fs=[];rows=38;cols=80
    for i in range(rows):
        z=profile[0][0]+(profile[-1][0]-profile[0][0])*i/(rows-1)
        rx,front,back=profile_at(z);cy=(front+back)/2;ry=(back-front)/2
        for j in range(cols+1):
            a=math.tau*j/cols+math.pi*.5
            q=Vector((rx*math.cos(a),cy+ry*math.sin(a),z))
            origin=Vector((0,cy,z));direction=(q-origin).normalized();start=origin.copy();farthest=None
            for attempt in range(6):
                hit=skin_surface.ray_cast(start,direction,.18)[0]
                if hit is None:break
                farthest=hit;start=hit+direction*.0002
            if farthest is not None:
                dist=(farthest-origin).length+.0035
                if (q-origin).length<dist<(q-origin).length+.035:q=origin+direction*dist
            vs.append(q);uv.append((j/cols,i/(rows-1)))
    for i in range(rows-1):
        for j in range(cols):
            a=i*(cols+1)+j;fs.append((a,a+1,a+cols+2,a+cols+1))
    top=m.mesh('Outfit | technical crop top LOW',vs,fs,knit,m.outfit,uv,torso=True,sub=1)
    solid=top.modifiers.new('Bound garment thickness','SOLIDIFY');solid.thickness=.0015
    top['pipeline']='Quad modelling -> high-resolution tension/compression sculpt -> tangent normal bake.'
    # Refit the jacket panels to the new garment volume; otherwise the fitted
    # shirt can poke through the older jacket fronts around the bust.
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH' or not any(label in ob.name for label in ['Seoha tailored jacket front','Seoha tailored folded lapel','Seoha jacket opening seam','Seoha jacket curved hem','Seoha fitted zipper rail','Seoha lapel topstitch']):continue
        for v in ob.data.vertices:
            if 1.145<=v.co.z<=1.375 and v.co.y<0:
                v.co.y=min(v.co.y,shirt_y(v.co.x,v.co.z)-(.008 if 'lapel' in ob.name else .006))

    # A separate high-resolution garment contains the small fabric folds.
    # Keep it hidden after baking; never ship this full sculpt in the game.
    highcol=collection('90 SCULPT | garment fold source',True)
    hi=top.copy();hi.data=top.data.copy();highcol.objects.link(hi);hi.name='Outfit | technical crop top HIGH'
    hi.parent=None
    for mod in list(hi.modifiers):
        if mod.type!='SUBSURF':hi.modifiers.remove(mod)
        else:mod.levels=2;mod.render_levels=2
    highcol.hide_viewport=False;highcol.hide_render=False
    bpy.context.view_layer.objects.active=hi
    bpy.ops.object.select_all(action='DESELECT');hi.select_set(True)
    bpy.ops.object.modifier_apply(modifier=hi.modifiers[0].name)
    hi.data.update()
    for v in hi.data.vertices:
        x,y,z=v.co;front=sstep((-.002-y)/.07)
        lower=g(z-1.174,.039)
        # Low-amplitude diagonal stretch folds radiate from the side seam;
        # compression gathers near the bound hem. Do not add random skin noise.
        fold=.00125*math.sin(170*(z-1.16)+24*abs(x))*lower*front
        fold+=.0008*math.sin(215*(z-1.204)-35*abs(x))*g(abs(x)-.092,.027)*g(z-1.21,.046)
        fold+=.00045*math.sin(310*(z-1.148)+12*x)*g(z-1.157,.014)
        v.co+=v.normal*fold
    hi['sculpt_method']='High-resolution mesh displacement along surface normals, following garment tension and compression.'
    highcol.hide_render=True;highcol.hide_viewport=True;hi.select_set(False)
    # The low surface receives the same broad fabric shape before tiny folds are baked.
    for v in top.data.vertices:
        x,y,z=v.co;front=sstep((-.002-y)/.07)
        v.co.y-=.0005*math.sin(170*(z-1.16)+24*abs(x))*g(z-1.174,.039)*front

    for z in [1.147,1.153,1.377]:
        rx,fr,back=profile_at(z);cy=(fr+back)/2;ry=(back-fr)/2
        pts=[(rx*math.cos(math.tau*i/80),cy+ry*math.sin(math.tau*i/80),z) for i in range(81)]
        m.tube('Outfit | bound hem seam',pts,.00048,thread,torso=True,closed=True)
    # Panel seams follow the real bust curvature, rather than floating straight lines.
    for side in [-1,1]:
        pts=[]
        for i in range(30):
            z=1.153+.159*i/29;x=side*(.078+.016*math.sin(math.pi*i/29))
            pts.append((x,shirt_y(x,z)-.0014,z))
        m.tube('Outfit | princess panel seam %s'%side,pts,.00055,leather,torso=True)
    zipps=[]
    for i in range(39):
        t=i/38;x=.024-.109*t;z=1.359-.105*t
        zipps.append(Vector((x,shirt_y(x,z)-.0025,z)))
    m.tube('Outfit | asymmetric chest zipper backing',zipps,.0023,leather,torso=True)
    m.tube('Outfit | asymmetric chest zipper teeth rail',zipps,.00055,steel,torso=True)
    for i in range(2,38):
        p=zipps[i];tangent=(zipps[i+1]-zipps[i-1]).normalized();cross=tangent.cross(Vector((0,-1,0))).normalized()
        for side in [-1,1]:
            m.box('Hardware | chest zipper tooth',p+cross*side*.0014,(.0014,.0015,.0013),steel,torso=True,bevel=.0002)
    p=zipps[-1]
    m.box('Hardware | chest zip slider',p+Vector((0,-.002,-.002)),(.004,.003,.007),steel,torso=True)
    m.tube('Hardware | chest zip pull tab',[p+Vector((-.002,-.004,-.003)),p+Vector((-.005,-.006,-.013)),p+Vector((.002,-.006,-.014)),p+Vector((.004,-.004,-.004))],.00065,steel,torso=True)

    # Replace the dangling choker with a fitted neck band connected to the top.
    for z in [1.365,1.379]:
        rx,fr,back=profile_at(z);cy=(fr+back)/2;ry=(back-fr)/2
        pts=[(rx*math.cos(math.tau*i/60),cy+ry*math.sin(math.tau*i/60)-.0004,z) for i in range(61)]
        m.tube('Outfit | neck band binding',pts,.0010,leather,'J_Bip_C_Neck',closed=True)
    for z in [1.369,1.376]:m.rivet('Hardware | neck fastener',(.005,shirt_y(.005,z)-.002,z),.0016,darksteel,'J_Bip_C_Neck')

    # Jacket lapel rivets and a second zipper follow each fitted front edge.
    for side in [-1,1]:
        for p in [(side*.083,-.092,1.363),(side*.072,-.139,1.318)]:
            q=body_cage(Vector(p));m.rivet('Hardware | lapel snap',q+Vector((0,-.002,0)),.0023,steel,torso=True)
        frontob=bpy.data.objects['Seoha tailored jacket front '+str(side)]
        verts=frontob.data.vertices;rows=26;cols=10
        for i in range(8,25):
            p=verts[i*cols].co+Vector((side*.002,-.003,0))
            m.box('Hardware | jacket zip tooth',p,(.0028,.0015,.0014),steel,torso=True,bevel=.0002)
        # Layered shoulder harness with rounded corners, buckle and stitch edge.
        pts=[body_cage(Vector(p)) for p in [(side*.076,-.051,1.402),(side*.108,-.058,1.375),(side*.125,-.065,1.335)]]
        for offset in [-.006,.006]:m.tube('Outfit | shoulder harness bound edge',[(p.x+offset,p.y-.002,p.z) for p in pts],.00075,leather)
        p=pts[1]+Vector((0,-.005,0));m.box('Outfit | shoulder harness tab',p,(.017,.004,.025),leather)
        for dz in [-.007,.007]:m.rivet('Hardware | shoulder harness rivet',p+Vector((0,-.003,dz)),.0017,steel)
    # Collar trim now follows the collar surface, not a detached halo at the back.
    collar=bpy.data.objects['Seoha tailored raised collar']
    pts=[v.co+Vector((0,-.0003,.0003)) for v in list(collar.data.vertices)[-52:]]
    m.tube('Outfit | collar cyan binding',pts,.00055,cyan,'J_Bip_C_UpperChest')

    # Fit the waist band to the trousers' actual cross-section. This closes the
    # old air gap at the sides and prevents the buckle floating off the body.
    vs=[];uv=[];fs=[];cols=80
    def belt_point(a,z):
        q=Vector((.137*math.cos(a),.004+.102*math.sin(a),z+.0015*math.cos(2*a)))
        origin=Vector((0,.004,q.z));direction=(q-origin).normalized();start=origin.copy();last=None
        for i in range(6):
            hit=skin_surface.ray_cast(start,direction,.20)[0]
            if hit is None:break
            last=hit;start=hit+direction*.0002
        if last is not None and (last-origin).length+.003>(q-origin).length:q=last+direction*.003
        return q
    for i,z in enumerate([1.022,1.025,1.046,1.049]):
        for j in range(cols+1):vs.append(belt_point(math.tau*j/cols,z));uv.append((j/cols,i/3))
    for i in range(3):
        for j in range(cols):a=i*(cols+1)+j;fs.append((a,a+1,a+cols+2,a+cols+1))
    belt=m.mesh('Outfit | fitted utility belt',vs,fs,leather,m.outfit,uv,'J_Bip_C_Hips',sub=1)
    solid=belt.modifiers.new('Leather strap thickness','SOLIDIFY');solid.thickness=.002
    for z in [1.025,1.046]:m.tube('Outfit | belt topstitch',[belt_point(math.tau*i/80,z)*Vector((1.004,1.004,1)) for i in range(81)],.0004,thread,'J_Bip_C_Hips',closed=True)
    pts=[(-.020,-.102,1.026),(-.020,-.102,1.047),(.018,-.102,1.047),(.018,-.102,1.026),(-.020,-.102,1.026)]
    m.tube('Hardware | rectangular belt buckle',pts,.0014,steel,'J_Bip_C_Hips')
    m.tube('Hardware | belt pin',[(0,-.104,1.028),(0,-.104,1.045)],.0007,steel,'J_Bip_C_Hips')
    for x in [.039,.052,.065]:m.rivet('Hardware | belt hole',(x,-.100,1.037),.0015,darksteel,'J_Bip_C_Hips')
    for a in [-2.6,-1.9,-1.15,-.4,.35,1.4,2.4]:
        p=belt_point(a,1.035);rot=Quaternion((0,0,1),a+math.pi/2)
        m.box('Outfit | trouser belt loop',p,(.009,.006,.038),leather,'J_Bip_C_Hips',bevel=.0012,rotation=rot)
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith('Seoha hip utility pouch'):
            center=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices)
            # Source boxes use local mesh coordinates plus object translation.
            center=ob.matrix_world@center
            side=1 if center.x>0 else -1
            q=Vector((side*.154,-.060,1.017))
            m.box('Outfit | pouch front reinforcement',q,(.026,.004,.045),leather,'J_Bip_C_Hips')
            m.rivet('Hardware | pouch snap',q+Vector((0,-.003,.013)),.002,steel,'J_Bip_C_Hips')

    # Segmented forearm protection and glove fastening: separate shallow
    # manufactured plates, with a narrow light strip rather than a large cube.
    for i in range(3):
        x=-.345-.026*i;ps=[];fs=[];uv=[];n=20
        for row,dx in enumerate([-.014,.014]):
            for j in range(n):
                a=-2.72+2.72*j/(n-1);r=.036-.003*i
                ps.append(body_cage(Vector((x+dx,.021+r*math.sin(a),1.379+r*math.cos(a)))))
                uv.append((j/(n-1),row))
        for j in range(n-1):fs.append((j,j+1,n+j+1,n+j))
        ob=m.mesh('Outfit | articulated forearm plate %s'%i,ps,fs,leather,m.outfit,uv,'J_Bip_R_LowerArm',sub=1)
        sol=ob.modifiers.new('Plate edge thickness','SOLIDIFY');sol.thickness=.002
        p=body_cage(Vector((x,-.018,1.384)))
        m.rivet('Hardware | forearm lock',p,.0022,steel,'J_Bip_R_LowerArm')
    m.tube('Outfit | wrist cyan signal',[body_cage(Vector(p)) for p in [(-.348,-.019,1.398),(-.398,-.014,1.394)]],.0011,cyan,'J_Bip_R_LowerArm')
    for side in [-1,1]:
        for x in [.49,.514]:
            center=body_cage(Vector((side*x,-.016,1.384)))
            m.box('Outfit | glove back strap',center,(.014,.007,.048),leather,'J_Bip_'+('L' if side>0 else 'R')+'_Hand',bevel=.002)
    return top,hi

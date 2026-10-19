"""Close visible collar/yoke gaps and add fitted reference-inspired fastening."""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from atelier_common import plain,grain,sstep,catmull

def refine_neckline(m, rebuild_front=False):
    # The old broad rear collar reads as a detached horizontal halo.
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith(('Seoha tailored raised collar','Outfit | collar cyan binding','Outfit | neck band binding','Hardware | neck fastener')):
            bpy.data.objects.remove(ob,do_unlink=True)
    leather,bs=plain('Silhouette | fitted graphite collar',(.018,.019,.027),.52,.03);grain(leather,bs,470,.000055)
    lining,bs=plain('Silhouette | collar grey lining',(.058,.061,.078),.75);grain(lining,bs,600,.00006)
    thread,_=plain('Silhouette | tailoring stitch',(.075,.074,.087),.77)
    steel,_=plain('Silhouette | zipper brushed steel',(.15,.16,.19),.38,.65)
    cyan,bs=plain('Silhouette | cyan seam piping',(.010,.20,.24),.48,.15)
    bs.inputs['Emission Color'].default_value=(.008,.16,.19,1);bs.inputs['Emission Strength'].default_value=.25

    # A separate high neck joins the existing shirt with an overlapping bound
    # edge. It follows the real body's transverse skin section at each height.
    body=m.body
    surf=BVHTree.FromPolygons([v.co for v in body.data.vertices],
        [tuple(p.vertices) for p in body.data.polygons if p.material_index in [0,5]])
    vs=[];fs=[];uv=[];nr=13;nc=65
    for i in range(nr):
        z=1.350+.052*i/(nr-1);t=i/(nr-1)
        for j in range(nc):
            a=math.tau*j/(nc-1)
            center=Vector((0,.018,z));d=Vector((math.cos(a),math.sin(a),0))
            hit=surf.ray_cast(center,d,.16)[0]
            radius=.033+.038*(1-sstep(t/.5))
            p=center+d*radius
            if hit is not None:p=hit+d*.0030
            p.z=z;vs.append(p);uv.append((j/(nc-1),t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    neck=m.mesh('Outfit | fitted zip neck yoke',vs,fs,leather,m.outfit,uv,sub=1)
    thick=neck.modifiers.new('Tailored neck thickness','SOLIDIFY');thick.thickness=.0012
    # Prefer upper chest at the base, blending towards the neck at the top.
    neck.vertex_groups.clear()
    chest=neck.vertex_groups.new(name='J_Bip_C_UpperChest');ng=neck.vertex_groups.new(name='J_Bip_C_Neck')
    for v in neck.data.vertices:
        w=.55*sstep((v.co.z-1.373)/.041);chest.add([v.index],1-w,'REPLACE');ng.add([v.index],w,'REPLACE')
    upper=vs[(nr-1)*nc:]
    m.tube('Outfit | high neck stitched edge',[p+Vector((0,0,.0004)) for p in upper],.00055,thread,'J_Bip_C_Neck',closed=True)
    # Front zipper follows the sampled yoke instead of floating above it.
    zipps=[]
    for i in range(nr):
        p=vs[i*nc+48].copy();p.y-=.0018;zipps.append(p)
    m.tube('Outfit | high neck zipper',zipps,.0012,steel,'J_Bip_C_Neck')
    slider=zipps[-3]+Vector((0,-.001,-.003))
    m.box('Hardware | high neck slider',slider,(.004,.002,.006),steel,'J_Bip_C_Neck')
    m.tube('Hardware | high neck pull',[slider+Vector((-.001,-.002,-.002)),slider+Vector((-.0015,-.003,-.010)),
        slider+Vector((.002,-.003,-.010)),slider+Vector((.002,-.002,-.002))],.0005,steel,'J_Bip_C_Neck')

    # Open, curved standing jacket collar: shorter at the nape, wider at the
    # front lapel roots. Thickness, piping and stitch all share its surface.
    vs=[];uv=[];fs=[];nr=9;nc=61
    for i in range(nr):
        t=i/(nr-1)
        for j in range(nc):
            a=-.25+(math.pi+.50)*j/(nc-1)
            frontness=1-math.sin(a)
            x=(.061+.020*t+.009*frontness)*math.cos(a)
            y=.009+(.050+.013*t)*math.sin(a)-.040*frontness
            z=1.332+.060*t-.019*frontness*t
            vs.append(Vector((x,y,z)));uv.append((j/(nc-1),t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    collar=m.mesh('Outfit | curved standing jacket collar',vs,fs,lining,m.outfit,uv,torso=True,sub=2)
    thick=collar.modifiers.new('Folded leather thickness','SOLIDIFY');thick.thickness=.0022
    top=vs[(nr-1)*nc:]
    m.tube('Outfit | close fitted cyan collar edge',[p+Vector((0,-.0004,.0004)) for p in top],.00050,cyan,'J_Bip_C_UpperChest')
    m.tube('Outfit | collar parallel seam',[p+Vector((0,-.0005,-.0020)) for p in top],.00038,thread,'J_Bip_C_UpperChest')

    # The front rebuild is an unapproved experiment: pose-dependent overlap
    # remains. Default delivery keeps the earlier jacket fronts and sleeves.
    if not rebuild_front:return

    # A continuous torso profile avoids sudden ray-hit/miss jumps at the neck
    # and armholes. Those jumps tear a panel when only some vertices snap.
    profiles=[(1.14,.110,-.089,.075),(1.18,.118,-.109,.078),(1.24,.137,-.151,.090),
        (1.27,.133,-.141,.088),(1.302,.121,-.115,.086),(1.33,.094,-.087,.071),
        (1.36,.060,-.050,.056),(1.40,.039,-.020,.052)]
    def front_y(x,z,raw=False):
        z=max(profiles[0][0],min(profiles[-1][0],z))
        i=next((i for i in range(len(profiles)-1) if profiles[i][0]<=z<=profiles[i+1][0]),len(profiles)-2)
        a,b=profiles[i],profiles[i+1];t=sstep((z-a[0])/(b[0]-a[0]))
        rx,fr,back=[a[j]*(1-t)+b[j]*t for j in [1,2,3]]
        y=(fr+back)/2-(back-fr)/2*math.sqrt(max(.08,1-(x/rx)**2))
        return y if raw else min(y,-.045-.070*sstep((1.335-z)/.080))
    followers=('Outfit | asymmetric chest zipper','Hardware | chest zip')
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH' or not ob.name.startswith(followers):continue
        offset=.009
        if ob.name.startswith('Hardware'):
            c=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices)
            delta=front_y(c.x,c.z)-offset-c.y
            for v in ob.data.vertices:v.co.y+=delta
        else:
            for v in ob.data.vertices:
                if v.co.y>.03:continue
                v.co.y=front_y(v.co.x,v.co.z)-offset
    # Replace the inherited distorted front topology instead of repeatedly
    # compressing it against a different torso. Keep the original sleeves/back.
    obsolete=('Seoha tailored jacket front','Seoha tailored folded lapel','Seoha lapel topstitch',
        'Seoha jacket opening seam','Seoha jacket curved hem','Seoha fitted zipper rail',
        'Hardware | jacket zip tooth','Hardware | lapel snap','Outfit | shoulder harness',
        'Hardware | shoulder harness','Outfit | shoulder yoke infill','Outfit | smooth under lapel')
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith(obsolete):bpy.data.objects.remove(ob,do_unlink=True)
    def coat_y(x,z):
        upper=sstep((z-1.355)/.05)
        base=front_y(x,z,raw=True)
        neck=-.021-.024*sstep(abs(x)/.09)
        base=base*(1-upper)+neck*upper
        floor=-.040*(1-upper)+neck*upper
        return min(base,floor)-.007
    for side in [-1,1]:
        nr=39;nc=15;vs=[];fs=[];uv=[];inner=[]
        for i in range(nr):
            t=i/(nr-1)
            xi=.090-.056*t-.022*math.sin(t*math.pi)+(0 if side<0 else .010*(1-t))
            xo=.134+.010*math.sin(t*math.pi)-.008*t
            for j in range(nc):
                u=j/(nc-1);x=side*(xi*(1-u)+xo*u);z=1.144+(.210+.045*(1-u))*t
                y=coat_y(x,z)-.0006*math.sin(t*13+u*4)*math.sin(math.pi*u)
                vs.append(Vector((x,y,z)));uv.append((u,t))
            inner.append(vs[i*nc])
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        panel=m.mesh('Outfit | rebuilt jacket front %s'%side,vs,fs,leather,m.outfit,uv,torso=True,sub=2)
        thick=panel.modifiers.new('Leather thickness','SOLIDIFY');thick.thickness=.0022
        m.tube('Outfit | rebuilt opening binding %s'%side,[p+Vector((0,-.001,0)) for p in inner],.00085,leather,torso=True)
        m.tube('Outfit | rebuilt hem stitch %s'%side,[p+Vector((0,-.001,.002)) for p in vs[:nc]],.00042,thread,torso=True)
        for i in range(2,29):
            p=inner[i]+Vector((side*.002,-.002,0))
            m.box('Hardware | rebuilt jacket tooth',p,(.0024,.0015,.0012),steel,torso=True,bevel=.0002)
        # A small folded lapel, shaped along both its rolled edge and its tip.
        inner_curve=[Vector((.032,0,1.389)),Vector((.043,0,1.355)),Vector((.051,0,1.317)),Vector((.086,0,1.281))]
        outer_curve=[Vector((.064,0,1.391)),Vector((.105,0,1.371)),Vector((.124,0,1.328)),Vector((.086,0,1.281))]
        nr=28;nc=13;vs=[];fs=[];uv=[];edge=[]
        for i in range(nr):
            t=i/(nr-1);a=catmull(inner_curve,t);b=catmull(outer_curve,t)
            for j in range(nc):
                u=j/(nc-1);p=a.lerp(b,u);p.x*=side
                p.y=coat_y(p.x,p.z)-.0035-.005*math.sin(math.pi*u)*math.sin(math.pi*t)
                vs.append(p);uv.append((u,t))
            edge.append(vs[i*nc])
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        flap=m.mesh('Outfit | rebuilt folded lapel %s'%side,vs,fs,leather,m.outfit,uv,torso=True,sub=2)
        thick=flap.modifiers.new('Folded lapel edge','SOLIDIFY');thick.thickness=.0024
        m.tube('Outfit | rebuilt lapel seam %s'%side,[p+Vector((0,-.0007,0)) for p in edge],.00045,thread,torso=True)
        p=vs[18*nc+8]+Vector((0,-.002,0));m.rivet('Hardware | rebuilt lapel snap',p,.0027,steel,torso=True)

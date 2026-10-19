"""Continuous collar/shoulder coverage and fitted, shorter jacket lapels.

Keep the authored sleeves and their skin weights, but replace incompatible
floating fronts and their hardware with surfaces sharing an explicit pattern.
"""
import math,bpy,bmesh
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from atelier_common import plain,grain,catmull,sstep,g

def rebuild(m):
    remove=('Seoha tailored jacket front','Seoha tailored folded lapel','Seoha jacket opening seam',
      'Seoha jacket curved hem','Seoha fitted zipper rail','Seoha lapel topstitch',
      'Outfit | shoulder harness','Hardware | shoulder harness','Hardware | lapel snap',
      'Hardware | jacket zip tooth','Outfit | fitted zip neck yoke','Outfit | curved standing jacket collar',
      'Outfit | high neck','Hardware | high neck','Outfit | close fitted cyan collar edge',
      'Outfit | collar parallel seam','Outfit | asymmetric chest zipper','Hardware | chest zip')
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith(remove):bpy.data.objects.remove(ob,do_unlink=True)
    leather,bs=plain('Likeness | soft graphite jacket',(.025,.023,.035),.58,.025);grain(leather,bs,420,.00008)
    lining,bs=plain('Likeness | cool charcoal lining',(.09,.091,.115),.78);grain(lining,bs,500,.00004)
    thread,_=plain('Likeness | discreet grey thread',(.12,.115,.14),.82)
    steel,bs=plain('Likeness | titanium closures',(.20,.21,.25),.36,.76)
    darksteel,bs=plain('Likeness | dark inset fasteners',(.045,.046,.060),.40,.60)
    cyan,bs=plain('Likeness | cyan seam inlay',(.009,.18,.22),.55,.15)
    bs.inputs['Emission Color'].default_value=(.005,.13,.18,1);bs.inputs['Emission Strength'].default_value=.35
    # Reuse the authored albedo's leather-only patch. The UV transform samples
    # inside that patch; the atlas padding and old drawn garment cutouts are
    # never used as alpha or projected over the new model.
    path=Path(__file__).resolve().parents[2]/'Artifacts/SeohaStudio/VRoid/Seoha-Garment-Albedo.png'
    if path.exists():
        image=bpy.data.images.load(str(path),check_existing=True);image.pack()
        ns=leather.node_tree.nodes;ls=leather.node_tree.links;shader=ns.get('Principled BSDF')
        coords=ns.new('ShaderNodeTexCoord');mapping=ns.new('ShaderNodeMapping')
        mapping.inputs['Scale'].default_value=(.062,.095,1);mapping.inputs['Location'].default_value=(.191,.540,0)
        ls.new(coords.outputs['UV'],mapping.inputs['Vector'])
        tex=ns.new('ShaderNodeTexImage');tex.image=image;tex.extension='EXTEND';ls.new(mapping.outputs[0],tex.inputs[0])
        mix=ns.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.68;mix.inputs[1].default_value=(.025,.023,.035,1)
        ls.new(tex.outputs['Color'],mix.inputs[2]);ls.new(mix.outputs[0],shader.inputs['Base Color'])
    # Preserve sleeve geometry. Their previous paint included alpha cutouts at
    # the neck/shoulder, inappropriate after changing the garment topology.
    m.body.data.materials[3]=leather;m.body.data.materials[6]=leather

    # The old sleeve paint disguised large spikes in its coarse surface. Fair
    # only garment vertices; skin, hands and the preserved UVs are untouched.
    sleeveids=set(i for p in m.body.data.polygons if p.material_index in [3,6] for i in p.vertices)
    ns={i:set() for i in sleeveids}
    for e in m.body.data.edges:
        a,b=e.vertices
        if a in ns and b in ns:ns[a].add(b);ns[b].add(a)
    for _ in range(10):
        old={i:m.body.data.vertices[i].co.copy() for i in sleeveids}
        for i in sleeveids:
            if abs(old[i].x)<.12 or not ns[i]:continue
            avg=sum((old[j] for j in ns[i]),Vector())/len(ns[i])
            m.body.data.vertices[i].co=old[i].lerp(avg,.24)
    if m.body.data.shape_keys:
        for v,k in zip(m.body.data.vertices,m.body.data.shape_keys.key_blocks[0].data):k.co=v.co
    m.body.data.update()

    skinbv=BVHTree.FromPolygons([v.co for v in m.body.data.vertices],
        [tuple(p.vertices) for p in m.body.data.polygons if p.material_index in [0,5]])

    top=bpy.data.objects['Outfit | technical crop top LOW']
    prof=[(1.328,.095,-.087,.071),(1.348,.061,-.060,.069),(1.372,.037,-.024,.061),(1.401,.035,-.014,.060)]
    def at(z,profile):
        if z<=profile[0][0]:return profile[0][1:]
        if z>=profile[-1][0]:return profile[-1][1:]
        a,b=next((a,b) for a,b in zip(profile,profile[1:]) if a[0]<=z<=b[0]);t=(z-a[0])/(b[0]-a[0])
        return tuple(a[i]*(1-t)+b[i]*t for i in range(1,len(a)))
    for v in top.data.vertices:
        if v.co.z<1.328:continue
        z=1.328+(v.co.z-1.328)/.052*.073
        a=math.tau*(v.index%81)/80+math.pi*.5;rx,front,back=at(z,prof)
        q=Vector((rx*math.cos(a),(front+back)/2+(back-front)/2*math.sin(a),z))
        origin=Vector((0,(front+back)/2,z));direction=(q-origin).normalized();start=origin.copy();last=None
        for _ in range(8):
            hit=skinbv.ray_cast(start,direction,.18)[0]
            if hit is None:break
            last=hit;start=hit+direction*.0002
        if last is not None and (last-origin).length<(q-origin).length+.050:
            q=origin+direction*max((q-origin).length,(last-origin).length+.005)
        v.co=q
        w=sstep((z-1.345)/.048)
        for vg in top.vertex_groups:vg.remove([v.index])
        for name,weight in [('J_Bip_C_UpperChest',1-w),('J_Bip_C_Neck',w)]:
            if weight>1e-6:
                vg=top.vertex_groups.get(name) or top.vertex_groups.new(name=name);vg.add([v.index],weight,'REPLACE')
    top.data.update()
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith('Outfit | bound hem seam') and min(v.co.z for v in ob.data.vertices)>1.35:
            bpy.data.objects.remove(ob,do_unlink=True)
    neckline=[]
    for j in range(65):
        a=math.tau*j/64;neckline.append((.0355*math.cos(a),.023+.0375*math.sin(a),1.401))
    m.tube('Tailoring | closed high neck binding',neckline,.00065,leather,'J_Bip_C_Neck',closed=True)
    m.tube('Tailoring | neck front closure',[(.006,-.016,1.400),(.006,-.025,1.376),(.006,-.042,1.360)],.0011,darksteel,torso=True)

    # Use the final top itself as the continuous lining support. Clamp to this
    # surface whenever the ray hits; outside it use a smooth shoulder profile.
    topbv=BVHTree.FromPolygons([v.co for v in top.data.vertices],[tuple(p.vertices) for p in top.data.polygons])
    bodybv=BVHTree.FromPolygons([v.co for v in m.body.data.vertices],[tuple(p.vertices) for p in m.body.data.polygons if p.material_index in [0,3,5,6]])
    surfaceprofile=[(1.119,.129,-.005,.089),(1.18,.143,-.010,.107),(1.24,.154,-.023,.131),
      (1.28,.152,-.022,.122),(1.31,.151,-.005,.109),(1.33,.153,.010,.081),
      (1.35,.153,.012,.061),(1.37,.135,.025,.054),(1.412,.070,.027,.039)]
    def front_y(x,z):
        rx,cy,ry=at(z,surfaceprofile)
        y=cy-ry*math.sqrt(max(.02,1-(x/rx)**2))-.005
        y-=.035*sstep((abs(x)-.095)/.055)*g(z-1.354,.025)
        hit=topbv.ray_cast(Vector((x,-.4,z)),Vector((0,1,0)),.8)[0]
        if hit is not None:y=min(y,hit.y-.0045)
        return y

    def tailored_bind(ob):
        # Torso cloth remains on the chest; the armhole gradually follows the
        # sleeve's shoulder deformation so the front and top do not tear apart.
        for v in ob.data.vertices:
            ax=abs(v.co.x);side='L' if v.co.x>0 else 'R'
            arm=sstep((ax-.085)/.085)*sstep((v.co.z-1.305)/.045)*.72
            chest=sstep((v.co.z-1.18)/.13)
            weights=[('J_Bip_C_Chest',(1-arm)*(1-chest)),('J_Bip_C_UpperChest',(1-arm)*chest),('J_Bip_'+side+'_UpperArm',arm)]
            for group in ob.vertex_groups:group.remove([v.index])
            for name,w in weights:
                if w<=1e-6:continue
                vg=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);vg.add([v.index],w,'REPLACE')

    for side in [-1,1]:
        inner=[Vector((side*x,z,0)) for x,z in [(.097,1.119),(.105,1.175),(.092,1.258),(.060,1.326),(.036,1.369)]]
        outer=[Vector((side*x,z,0)) for x,z in [(.129,1.126),(.142,1.182),(.150,1.262),(.150,1.331),(.151,1.354)]]
        nr=45;nc=19;vs=[];fs=[];uv=[]
        for i in range(nr):
            t=i/(nr-1);a=catmull(inner,t);b=catmull(outer,t)
            for j in range(nc):
                u=j/(nc-1);x=a.x*(1-u)+b.x*u;z=a.y*(1-u)+b.y*u
                y=front_y(x,z)-.0012*math.sin(math.pi*u)
                vs.append(Vector((x,y,z)));uv.append((u,t))
        # Remove isolated ray-support jumps while preserving a small clearance.
        for _ in range(4):
            ys=[v.y for v in vs]
            for i in range(1,nr-1):
                for j in range(1,nc-1):
                    a=i*nc+j;avg=sum(ys[k] for k in [a-1,a+1,a-nc,a+nc])/4
                    vs[a].y=min(vs[a].y,.55*ys[a]+.45*avg)
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        ob=m.mesh('Tailoring | fitted jacket front %s'%side,vs,fs,leather,m.outfit,uv,torso=True,sub=1)
        tailored_bind(ob);mod=ob.modifiers.new('Turned leather edge','SOLIDIFY');mod.thickness=.002
        opening=[vs[i*nc]+Vector((0,-.0018,0)) for i in range(nr)]
        m.tube('Tailoring | front zipper tape %s'%side,opening,.0016,darksteel,torso=True)
        seam=m.tube('Tailoring | front topstitch %s'%side,[p+Vector((side*.0035,-.0002,0)) for p in opening],.00035,thread,torso=True)
        tailored_bind(seam)
        for i in range(2,nr-3):
            p=opening[i];m.box('Tailoring | zipper tooth',p,(.0024,.0013,.0010),steel,torso=True,bevel=.00015)
        p=opening[4]
        m.box('Tailoring | zipper slider',p+Vector((0,-.003,-.002)),(.005,.003,.008),steel,torso=True)
        m.tube('Tailoring | zipper pull',[(p.x-.002,p.y-.004,p.z-.002),(p.x-.002,p.y-.006,p.z-.018),
            (p.x+.003,p.y-.006,p.z-.018),(p.x+.003,p.y-.004,p.z-.002)],.0006,darksteel,torso=True)

        # Side gusset closes the opening between each front and the back.
        gvs=[];gfs=[];guv=[];gc=9
        for i in range(nr):
            t=i/(nr-1);p=vs[i*nc+nc-1]
            for j in range(gc):
                u=j/(gc-1);q=p.copy();q.x+=side*.002*math.sin(math.pi*u)
                q.y=p.y*(1-u)+.012*u;gvs.append(q);guv.append((u,t))
        for i in range(nr-1):
            for j in range(gc-1):a=i*gc+j;gfs.append((a,a+1,a+gc+1,a+gc))
        gus=m.mesh('Tailoring | side gusset %s'%side,gvs,gfs,leather,m.outfit,guv,torso=True,sub=1);tailored_bind(gus)

        # Shoulder bridge follows matching front endpoints across to the back.
        vs2=[];fs2=[];uv2=[];rows=19;cols=23
        for i in range(rows):
            v=i/(rows-1)
            for j in range(cols):
                u=j/(cols-1);x=side*(.036+.115*u);z=1.369-.015*u+.007*math.sin(math.pi*v)
                yf=front_y(x,z);yb=.071-.009*u;y=yf*(1-v)+yb*v
                hit=bodybv.ray_cast(Vector((x,y,1.6)),Vector((0,0,-1)),.4)[0]
                if hit is not None and hit.z<1.405:z=max(z,hit.z+.003)
                vs2.append(Vector((x,y,z)));uv2.append((u,v))
        for i in range(rows-1):
            for j in range(cols-1):a=i*cols+j;fs2.append((a,a+1,a+cols+1,a+cols))
        bridge=m.mesh('Tailoring | continuous shoulder bridge %s'%side,vs2,fs2,leather,m.outfit,uv2,torso=True,sub=1)
        tailored_bind(bridge);sol=bridge.modifiers.new('Shoulder leather thickness','SOLIDIFY');sol.thickness=.002

        # Folded collar is short, with a light lining facing the cheek.
        paths=[[(side*.037,-.037,1.378),(side*.066,-.052,1.363),(side*.083,-.087,1.334),(side*.071,-.131,1.296)],
               [(side*.066,-.019,1.410),(side*.093,-.047,1.377),(side*.104,-.090,1.337),(side*.072,-.133,1.296)]]
        paths=[[Vector(p) for p in ps] for ps in paths];vs=[];fs=[];uv=[];nr=33;nc=11
        for i in range(nr):
            t=i/(nr-1);a=catmull(paths[0],t);b=catmull(paths[1],t)
            for j in range(nc):
                u=j/(nc-1);p=a.lerp(b,u);p.y-=.002*math.sin(math.pi*u)
                p.y=min(p.y,front_y(p.x,p.z)-.002)
                vs.append(p);uv.append((u,t))
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        lap=m.mesh('Tailoring | small folded collar %s'%side,vs,fs,lining,m.outfit,uv,torso=True,sub=1)
        mod=lap.modifiers.new('Folded collar thickness','SOLIDIFY');mod.thickness=.002
        edge=[vs[i*nc+nc-1]+Vector((0,-.001,0)) for i in range(nr)]
        m.tube('Tailoring | collar leather binding %s'%side,edge,.0013,leather,torso=True)
        m.tube('Tailoring | narrow cyan collar piping %s'%side,[p+Vector((0,-.001,0)) for p in edge],.00043,cyan,torso=True)
        m.rivet('Tailoring | collar press stud %s'%side,vs[13*nc+7]+Vector((0,-.003,0)),.0017,steel,torso=True)

    # Closed rear collar connects the two front leaves; no floating ring.
    vs=[];uv=[];fs=[];rows=9;cols=49
    for i in range(rows):
        t=i/(rows-1)
        for j in range(cols):
            a=math.pi*j/(cols-1);x=(.054+.016*t)*math.cos(a)
            y=.018+(.059+.005*t)*math.sin(a);z=1.362+.050*t-.006*math.sin(a)
            vs.append((x,y,z));uv.append((j/(cols-1),t))
    for i in range(rows-1):
        for j in range(cols-1):a=i*cols+j;fs.append((a,a+1,a+cols+1,a+cols))
    m.mesh('Tailoring | connected rear collar',vs,fs,lining,m.outfit,uv,torso=True,sub=1)

    backprof=[(1.126,.129,.090),(1.182,.142,.103),(1.262,.150,.107),(1.331,.150,.090),(1.354,.151,.073)]
    nr=43;nc=43;vs=[];fs=[];uv=[]
    for i in range(nr):
        t=i/(nr-1);z=1.126+.240*t
        rx,depth=at(min(z,1.354),backprof)
        for j in range(nc):
            u=2*j/(nc-1)-1;x=rx*u;zz=z-.011*sstep(t)*abs(u)
            y=.012+depth*math.sqrt(max(0,1-u*u))
            vs.append((x,y,zz));uv.append((j/(nc-1),t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    back=m.mesh('Tailoring | continuous jacket back',vs,fs,leather,m.outfit,uv,torso=True,sub=1);tailored_bind(back)
    sol=back.modifiers.new('Back leather thickness','SOLIDIFY');sol.thickness=.002
    for fraction in [-.58,.58]:
        pts=[]
        for i in range(nr):
            t=i/(nr-1);z=1.126+.240*t;rx,depth=at(min(z,1.354),backprof)
            pts.append((rx*fraction,.013+depth*math.sqrt(1-fraction*fraction),z-.011*sstep(t)*abs(fraction)))
        m.tube('Tailoring | rear panel topstitch',pts,.0004,thread,torso=True)

    # Shoulder patches inherit the exact existing body skin weights. This
    # closes the clavicle/back gaps that cannot be covered by a torso-only cage
    # when the upper arms rotate down or the portrait arm is raised.
    patchfaces=[p for p in m.body.data.polygons if p.material_index in [0,3]
      and all(1.325<v.co.z<1.409 and .036<abs(v.co.x)<.177 for v in [m.body.data.vertices[i] for i in p.vertices])]
    ids=sorted(set(i for p in patchfaces for i in p.vertices));mapping={old:i for i,old in enumerate(ids)}
    pvs=[m.body.data.vertices[i].co+m.body.data.vertices[i].normal*.0045 for i in ids]
    pfs=[tuple(mapping[i] for i in p.vertices) for p in patchfaces]
    if pfs:
        patch=m.mesh('Tailoring | skin weighted shoulder yoke',pvs,pfs,leather,m.outfit,sub=1)
        sol=patch.modifiers.new('Shoulder yoke backing','SOLIDIFY');sol.thickness=.002

    # Replace the damaged old sleeve shells with continuous quad sections. The
    # cross-sections follow the arm rather than shrinking a shirt illustration.
    sleeveprofile=[(.115,.015,1.320,.065,.073),(.150,.020,1.337,.059,.060),
      (.190,.024,1.340,.047,.049),(.240,.024,1.334,.044,.043),(.290,.024,1.328,.037,.038),
      (.335,.025,1.324,.034,.034),(.380,.025,1.332,.034,.030),(.430,.025,1.340,.030,.027),(.482,.025,1.349,.029,.026)]
    def sleeve_point(x,a,extra=0):
        cy,cz,ry,rz=at(x,sleeveprofile)
        fold=.0009*math.sin(160*x+3*math.sin(a))*g(x-.325,.045)+.0006*math.sin(210*x-2*a)*g(x-.20,.045)
        return Vector((x,cy+(ry+fold+extra)*math.cos(a),cz+(rz+fold+extra)*math.sin(a)))
    for side in [-1,1]:
        end=.482 if side<0 else .241;nr=49 if side<0 else 26;nc=49;vs=[];fs=[];uv=[]
        for i in range(nr):
            t=i/(nr-1);x=.115+(end-.115)*t
            for j in range(nc):
                a=math.tau*j/(nc-1);p=sleeve_point(x,a);p.x*=side
                vs.append(p);uv.append((j/(nc-1),t))
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        ob=m.mesh('Tailoring | right long sleeve' if side<0 else 'Tailoring | left short sleeve',vs,fs,leather,m.outfit,uv,torso=True,sub=2)
        for v in ob.data.vertices:
            x=abs(v.co.x);arm=sstep((x-.105)/.067);fore=sstep((x-.290)/.068)
            weights=[('J_Bip_C_UpperChest',1-arm),('J_Bip_'+('L' if side>0 else 'R')+'_UpperArm',arm*(1-fore)),
                     ('J_Bip_'+('L' if side>0 else 'R')+'_LowerArm',arm*fore)]
            for vg in ob.vertex_groups:vg.remove([v.index])
            for name,w in weights:
                if w>1e-6:
                    vg=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);vg.add([v.index],w,'REPLACE')
        sol=ob.modifiers.new('Sleeve turned hem','SOLIDIFY');sol.thickness=.0018
        for x in [end-.003,end-.010]:
            pts=[]
            for j in range(65):
                p=sleeve_point(x,math.tau*j/64,.0007);p.x*=side;pts.append(p)
            m.tube('Tailoring | sleeve hem stitch',pts,.00036,thread,
              'J_Bip_'+('L' if side>0 else 'R')+('_LowerArm' if side<0 else '_UpperArm'),closed=True)
        for angle in [math.pi-.56,math.pi+.50]:
            pts=[]
            for i in range(38):
                x=.193+(end-.198)*i/37;p=sleeve_point(x,angle+.06*math.sin(i/37*math.pi),.001)
                p.x*=side;pts.append(p)
            seam=m.tube('Tailoring | articulated sleeve panel seam',pts,.00045,thread)
        if side<0:
            pts=[sleeve_point(.215+.074*i/20,math.pi-.18,.0012) for i in range(21)]
            for p in pts:p.x=-p.x
            m.tube('Tailoring | upper arm pocket zipper',pts,.0011,darksteel,'J_Bip_R_UpperArm')
            for p in pts[1:-1]:m.box('Tailoring | pocket zipper tooth',p,(.0013,.0016,.0020),steel,'J_Bip_R_UpperArm',bevel=.0002)

    # The former exposed left upper arm was repurposed sleeve geometry and did
    # not share an elbow seam with the forearm. Replace it with one skin surface
    # whose centre follows the actual upper/lower arm rest bones.
    skinmat,bs=plain('Likeness | exposed arm skin',(.86,.625,.53),.67)
    bs.inputs['Subsurface Weight'].default_value=.055;bs.inputs['Specular IOR Level'].default_value=.17
    armprof=[(.180,1.341,.036,.037),(.235,1.334,.032,.033),(.285,1.328,.029,.029),
      (.328,1.323,.027,.027),(.360,1.329,.027,.027),(.410,1.337,.024,.023),(.465,1.346,.0215,.0205),(.507,1.352,.020,.019)]
    nr=49;nc=41;vs=[];fs=[];uv=[]
    for i in range(nr):
        t=i/(nr-1);x=.18+.327*t;z,ry,rz=at(x,armprof)
        for j in range(nc):
            a=math.tau*j/(nc-1);vs.append((x,.0245+ry*math.cos(a),z+rz*math.sin(a)));uv.append((j/(nc-1),t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    arm=m.mesh('Anatomy | continuous exposed left arm',vs,fs,skinmat,m.outfit,uv,torso=True,sub=2)
    for v in arm.data.vertices:
        w=sstep((v.co.x-.290)/.068)
        for vg in arm.vertex_groups:vg.remove([v.index])
        for name,weight in [('J_Bip_L_UpperArm',1-w),('J_Bip_L_LowerArm',w)]:
            if weight>1e-6:
                vg=arm.vertex_groups.get(name) or arm.vertex_groups.new(name=name);vg.add([v.index],weight,'REPLACE')
    for ob in bpy.context.scene.objects:
        if ob.name.startswith('Outfit | glove back strap'):
            center=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices)
            for v in ob.data.vertices:
                d=v.co-center;d.y*=.55;d.z*=.70;v.co=center+d
    for side in [-1,1]:
        bone='J_Bip_'+('L' if side>0 else 'R')+'_Hand';vs=[];fs=[];uv=[];nr=9;nc=41
        for i in range(nr):
            t=i/(nr-1);x=.475+.042*t
            for j in range(nc):
                a=math.tau*j/(nc-1);ry=.025+.001*math.sin(math.pi*t);rz=.023+.001*math.sin(math.pi*t)
                vs.append((side*x,.0245+ry*math.cos(a),1.350+rz*math.sin(a)));uv.append((j/(nc-1),t))
        for i in range(nr-1):
            for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
        cuff=m.mesh('Tailoring | fitted glove wrist cuff %s'%side,vs,fs,leather,m.outfit,uv,bone,sub=1)
        for x in [.478,.513]:
            pts=[(side*x,.0245+.0258*math.cos(math.tau*j/64),1.350+.0238*math.sin(math.tau*j/64)) for j in range(65)]
            m.tube('Tailoring | glove cuff stitch',pts,.00035,thread,bone,closed=True)
        for x in [.501,.520]:
            m.rivet('Tailoring | glove fastening stud',(side*x,-.0018,1.368),.0017,steel,bone)

    bm=bmesh.new();bm.from_mesh(m.body.data)
    gone=[f for f in bm.faces if f.material_index in [3,5,6] or
      (f.material_index==0 and f.calc_center_median().z>1.20 and
      (.177<f.calc_center_median().x<.505 or -.482<f.calc_center_median().x<-.177))]
    bmesh.ops.delete(bm,geom=gone,context='FACES_ONLY');bm.to_mesh(m.body.data);bm.free()
    bpy.context.view_layer.objects.active=m.body
    if m.body.data.has_custom_normals:bpy.ops.mesh.customdata_custom_splitnormals_clear()
    m.body.data.update()

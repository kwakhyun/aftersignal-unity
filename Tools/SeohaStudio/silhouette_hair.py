"""Continuous swept hair surfaces, with layered, individually shaped locks.

Design coordinates precede the existing body cage. Meshes are real opaque
geometry, not a projected portrait; rest-space paths remain editable.
"""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from atelier_common import plain, body_cage, catmull, sstep


def build_hair(m, face):
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith(('Hair |', 'Seoha hair')):
            bpy.data.objects.remove(ob, do_unlink=True)
    mat, bs = plain('Silhouette | silver lavender hair', (.46,.412,.54), .56)
    bs.inputs['Anisotropic'].default_value = .45
    bs.inputs['Specular IOR Level'].default_value = .19
    n=mat.node_tree.nodes; l=mat.node_tree.links
    uv=n.new('ShaderNodeTexCoord')
    xyz=n.new('ShaderNodeSeparateXYZ');l.new(uv.outputs['UV'],xyz.inputs[0])
    stretch=n.new('ShaderNodeVectorMath');stretch.operation='MULTIPLY'
    stretch.inputs[1].default_value=(73,1.6,1);l.new(uv.outputs['UV'],stretch.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1
    noise.inputs['Detail'].default_value=2;l.new(stretch.outputs[0],noise.inputs[0])
    ramp=n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color=(.36,.325,.425,1)
    ramp.color_ramp.elements[1].color=(.53,.485,.605,1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],bs.inputs['Base Color'])
    l.new(ramp.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.18
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.10
    bump.inputs['Distance'].default_value=.000065
    l.new(noise.outputs[0],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])
    edge,_=plain('Silhouette | fine silver locks',(.49,.445,.58),.59)
    tie,_=plain('Silhouette | gathered black tie',(.010,.011,.017),.48)

    body=m.body; basis=face.data.shape_keys.key_blocks[0].data
    verts=[v.co.copy() for v in body.data.vertices]+[v.co.copy() for v in basis]
    polys=[tuple(p.vertices) for p in body.data.polygons if p.material_index==0]
    polys += [tuple(len(body.data.vertices)+i for i in p.vertices) for p in face.data.polygons
              if p.material_index==2 and not all(abs(basis[i].co.x)>.073 and basis[i].co.z<1.545 for i in p.vertices)]
    skin=BVHTree.FromPolygons(verts,polys)
    center=body_cage(Vector((0,.025,1.578)))

    def fit(p, gap=.003, snap=False):
        original_z=p[2];q=body_cage(Vector(p))
        if original_z<1.551 and not snap:return q
        d=(q-center).normalized();o=center.copy();last=None
        for _ in range(10):
            hit=skin.ray_cast(o,d,.25)[0]
            if hit is None:break
            last=hit;o=hit+d*.00015
        if last is not None:
            if snap:q=last+d*gap
            elif (q-center).length<(last-center).length+gap:q=last+d*gap
        return q

    # Opaque, continuous scalp under the full hairstyle. The temple opening
    # leaves the ear visible; the nape reaches the gathered low bun.
    vs=[];fs=[];uvs=[];nr=30;nc=96
    for i in range(nr):
        t=i/(nr-1)
        for j in range(nc):
            a=math.tau*j/nc;front=max(0,-math.cos(a))
            end=1.67+.63*max(0,math.cos(a))-1.25*front**2
            th=.006+end*t
            p=(.086*math.sin(th)*math.sin(a),.025+.097*math.sin(th)*math.cos(a),1.576+.110*math.cos(th))
            vs.append(fit(p,.0035,snap=True));uvs.append((j/nc,t))
    for i in range(nr-1):
        for j in range(nc):
            ids=(i*nc+j,i*nc+(j+1)%nc,(i+1)*nc+(j+1)%nc,(i+1)*nc+j)
            middle=sum((vs[k] for k in ids),Vector())/4
            if middle.y<.009 and middle.z<1.615:continue
            fs.append(ids)
    m.mesh('Hair | continuous root foundation',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)

    def lock(name, points, width, depth=.0015, chain=None, material=mat, scalp=False, guide=True):
        ps=[Vector(p) for p in points];vs=[];uvs=[];fs=[];nr=44;nc=10
        if guide:m.guide(name+' | editable flow',[body_cage(p.copy()) for p in ps])
        previous=None
        for i in range(nr):
            t=i/(nr-1);p=catmull(ps,t)
            tangent=(catmull(ps,min(1,t+.001))-catmull(ps,max(0,t-.001))).normalized()
            if scalp:normal=Vector((p.x/.086,(p.y-.025)/.097,(p.z-1.576)/.110)).normalized()
            else:normal=Vector((p.x*2,-1,.05)).normalized()
            cross=tangent.cross(normal).normalized()
            if cross.length<.01:cross=Vector((1,0,0))
            if previous is not None and cross.dot(previous)<0:cross=-cross
            previous=cross.copy();normal=cross.cross(tangent).normalized()
            # Non-pointed roots tuck into a continuous underlayer. The long tip
            # becomes progressively thinner rather than ending in a blade.
            env=(.07+.93*sstep(t/.14))*(.86+.14*math.sin(math.pi*t))*(1-sstep((t-.66)/.34))**.72
            half=max(.000012,width*.5*env)
            for j in range(nc):
                a=math.tau*j/nc;u=math.cos(a)
                h=math.sin(a)*min(depth,half*.30)
                p2=p+cross*u*half+normal*h
                vs.append(fit(p2,.010+max(0,h),snap=True) if scalp else fit(p2,.0035));uvs.append((j/nc,t))
        for i in range(nr-1):
            for j in range(nc):fs.append((i*nc+j,i*nc+(j+1)%nc,(i+1)*nc+(j+1)%nc,(i+1)*nc+j))
        fs += [tuple(reversed(range(nc))),tuple((nr-1)*nc+j for j in range(nc))]
        ob=m.mesh(name,vs,fs,material,m.hair,uvs,'J_Bip_C_Head',sub=1)
        if chain:
            root=ob.vertex_groups['J_Bip_C_Head'];vg=ob.vertex_groups.new(name=chain)
            for v in ob.data.vertices:
                w=sstep((v.index//nc/(nr-1)-.36)/.57)
                root.add([v.index],1-w,'REPLACE');vg.add([v.index],w,'REPLACE')
        return ob

    # Four boundary flows make ONE continuous primary swept mass. Secondary
    # locks sit on this surface, so dividing the hair does not expose scalp.
    sweep=[
        [( .038,-.040,1.674),(.030,-.086,1.651),(-.004,-.105,1.614),(-.051,-.087,1.574),(-.079,-.049,1.554)],
        [( .040,-.010,1.687),(-.012,-.049,1.692),(-.059,-.077,1.660),(-.090,-.044,1.608),(-.102,.045,1.548)],
        [( .039, .024,1.690),(-.020, .016,1.703),(-.082,-.011,1.669),(-.107, .018,1.610),(-.108,.064,1.549)],
        [( .034, .057,1.682),(-.016, .078,1.685),(-.080, .084,1.646),(-.108, .083,1.589),(-.112,.071,1.548)],
    ]
    sweep=[[Vector(p) for p in path] for path in sweep]
    def patch(t,u):return catmull([catmull(path,t) for path in sweep],max(0,min(1,u)))
    nr=57;nc=53;vs=[];uvs=[];fs=[]
    for i in range(nr):
        t=i/(nr-1)
        for j in range(nc):
            u=j/(nc-1);p=patch(t,u)
            vs.append(fit(p,.0085,snap=True));uvs.append((u,t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    mass=m.mesh('Hair | connected side swept volume',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)
    thick=mass.modifiers.new('Hair mass edge thickness','SOLIDIFY');thick.thickness=.0015
    for k in range(19):
        u=(k+.4)/19;pts=[]
        for t in [0,.18,.38,.60,.81,1]:
            drift=.008*math.sin(k*2.1+t*5)*math.sin(math.pi*t)
            p=patch(t,u+drift)
            radial=Vector((p.x/.086,(p.y-.025)/.097,(p.z-1.576)/.110)).normalized()
            p+=radial*(.0003+.00035*math.sin(k*1.9)**2)*math.sin(math.pi*t)**.5
            pts.append(p)
        lock('Hair | swept layered lock %02d'%k,pts,.014+.004*math.sin(k*2.4)**2,.0007,scalp=True)

    # Character-left side has a smaller, asymmetric sweep, with tapered exits
    # around the temple rather than a row of identical cylindrical bangs.
    def sideflow(u):
        result=[Vector(p) for p in [(.039,-.026+.086*u,1.680-.005*u),(.065+.014*u,-.062+.082*u,1.656+.004*u),
             (.075+.021*u,-.078+.115*u,1.609+.013*u),(.073+.030*u,-.072+.135*u,1.566+.010*u),
             (.073+.027*u,-.040+.135*u,1.547-.018*u)]]
        result[0]=patch(0,u)+Vector((.001,0,-.0004))
        return result
    vs=[];fs=[];uvs=[];nr=49;nc=33
    for i in range(nr):
        t=i/(nr-1)
        for j in range(nc):
            u=j/(nc-1);p=catmull(sideflow(u),t)
            vs.append(fit(p,.0085,snap=True));uvs.append((u,t))
    for i in range(nr-1):
        for j in range(nc-1):a=i*nc+j;fs.append((a,a+1,a+nc+1,a+nc))
    m.mesh('Hair | connected short side volume',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)
    for k in range(11):
        u=(k+.4)/11;pts=sideflow(u)
        for j,p in enumerate(pts):
            p.x+=.001*math.sin(math.pi*j/4);p.y-=.0005*math.sin(math.pi*j/4)
        pts[-1]+=Vector((.002*math.sin(k*2),-.004*math.sin(k),.008*math.sin(k*1.6)))
        lock('Hair | short side layered lock %02d'%k,pts,.016+.004*math.sin(k*2)**2,.0009,scalp=True)
    for k in range(10):
        u=k/9
        pts=[(.035,.037+.027*u,1.685),(.061-.117*u,.088+.025*math.sin(math.pi*u),1.656),
             (.044-.125*u,.127-.015*u,1.608),(-.026-.063*u,.119-.008*u,1.568),(-.105,.069,1.543)]
        lock('Hair | gathered nape %02d'%k,pts,.021,.0014,scalp=True)

    # Loose diagonal fringe splits before its tips, with small curvature changes.
    fringe=[(.039,-.046,1.676),(.042,-.099,1.632),(.023,-.114,1.580),(-.013,-.111,1.543),(-.054,-.091,1.526)]
    lock('Hair | loose diagonal fringe',fringe,.0065,.0008)
    lock('Hair | face side soft fringe',[(.039,-.040,1.676),(.054,-.075,1.643),(.056,-.095,1.602),(.044,-.111,1.568),(.020,-.110,1.548)],.011,.0013)

    # Bun: irregular layered loops around a small oval core, with no regular
    # vertical grooves or sphere-sized rings.
    c=Vector((-.110,.064,1.549));vs=[];uvs=[];fs=[];nr=22;nc=48
    for i in range(nr+1):
        th=.003+(math.pi-.006)*i/nr
        for j in range(nc+1):
            a=math.tau*j/nc
            wobble=1+.06*math.sin(a*3+th*2)
            p=c+Vector((.027*math.sin(th)*math.cos(a)*wobble,.027*math.sin(th)*math.sin(a)*wobble,.030*math.cos(th)))
            vs.append(body_cage(p));uvs.append((j/nc,i/nr))
    for i in range(nr):
        for j in range(nc):a=i*(nc+1)+j;fs.append((a,a+1,a+nc+2,a+nc+1))
    m.mesh('Hair | loose gathered bun core',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)
    for k in range(13):
        phase=math.tau*k/13;pts=[]
        for t in [0,.16,.33,.52,.73,1]:
            a=phase+t*2.9
            radius=.5+.5*math.sin(math.pi*t)**.7
            p=c+Vector((.031*math.cos(a)*radius,.031*math.sin(a)*radius,.034*math.cos(t*math.pi)))
            pts.append(p)
        lock('Hair | uneven bun fold %02d'%k,pts,.008+.003*math.sin(k)**2,.0018)
    for k in range(6):
        phase=k*1.2;pts=[]
        for i in range(6):
            t=i/5;a=phase+t*4.6
            pts.append(c+Vector((.034*math.cos(a),.034*math.sin(a),.036-.070*t)))
        lock('Hair | escaping bun strand %02d'%k,pts,.0012,.00025,material=edge)

    # Main face-framing strands are soft S curves. Fine strands split to their
    # own endpoints; the broad lower tip of the earlier version is removed.
    for side in [-1,1]:
        chain='J_Sec_Hair3_02' if side<0 else 'J_Sec_Hair3_05'
        main=[(side*.070,.015,1.630),(side*.075,-.083,1.554),(side*.067,-.091,1.466),
              (side*.075,-.132,1.389),(side*.064,-.169,1.314),(side*.070,-.171,1.253 if side<0 else 1.280)]
        lock('Hair | soft long lock %s'%side,main,.0085 if side<0 else .010,.001,chain=chain)
        for k in range(3):
            pts=[Vector(p)+Vector((side*(.0010+.0014*k),-.0003*k,.001*k)) for p in main]
            pts[-2]+=Vector((side*.002*k,.001*k,.004*k))
            pts[-1]+=Vector((side*(.007+.003*k),.007*k,.012+.014*k))
            lock('Hair | split long lock %s %s'%(side,k),pts,.0022-.00035*k,.00035,chain,edge)
    for k in range(2):
        u=k
        lock('Hair | crown flyaway %02d'%k,[(.030,.011+.019*u,1.687),(-.022,.015+.023*u,1.709-.004*u),
             (-.087,.003+.020*u,1.695-.009*u),(-.107,-.004+.03*u,1.660-.025*u)],.00045,.00010,material=edge)
    m.hair['design']='Continuous scalp and swept primary surface; layered lock meshes and independent split tips.'
    return mat

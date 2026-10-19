"""Layered swept hair with independent surfaces, restrained ribs and messy bun.

Unlike the prior experiment, only the strand centreline is scalp-projected.
The cross section keeps volume instead of collapsing both sides onto the same
surface. Modelling guides are stored alongside the real weighted meshes.
"""
import math, bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from atelier_common import plain, body_cage, catmull, sstep

def build_hair(m,face):
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith(('Hair |','Seoha hair')):bpy.data.objects.remove(ob,do_unlink=True)
    mat,bs=plain('Likeness | lavender silver silk',(.47,.394,.58),.59)
    bs.inputs['Anisotropic'].default_value=.32;bs.inputs['Specular IOR Level'].default_value=.18
    n=mat.node_tree.nodes;l=mat.node_tree.links
    uv=n.new('ShaderNodeTexCoord'); stretch=n.new('ShaderNodeVectorMath');stretch.operation='MULTIPLY'
    stretch.inputs[1].default_value=(19,1.2,1);l.new(uv.outputs['UV'],stretch.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1;noise.inputs['Detail'].default_value=2
    l.new(stretch.outputs[0],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.29,.245,.37,1)
    ramp.color_ramp.elements[1].color=(.50,.433,.61,1)
    l.new(noise.outputs[0],ramp.inputs[0]);l.new(ramp.outputs[0],bs.inputs['Base Color'])
    l.new(ramp.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.13
    edge,_=plain('Likeness | fine silver strand',(.47,.414,.56),.66)
    tie,_=plain('Likeness | matte hair fastener',(.014,.011,.021),.5)
    basis=face.data.shape_keys.key_blocks[0].data;body=m.body
    vs=[v.co.copy() for v in body.data.vertices]+[v.co.copy() for v in basis]
    fs=[tuple(p.vertices) for p in body.data.polygons if p.material_index==0]
    fs += [tuple(len(body.data.vertices)+i for i in p.vertices) for p in face.data.polygons if p.material_index==2
           and not all(abs(basis[i].co.x)>.069 and basis[i].co.z<1.547 for i in p.vertices)]
    skin=BVHTree.FromPolygons(vs,fs);center=body_cage(Vector((0,.025,1.578)))

    def projected(p,gap=.005):
        q=body_cage(Vector(p));d=(q-center).normalized();o=center.copy();last=None
        for _ in range(10):
            hit=skin.ray_cast(o,d,.25)[0]
            if hit is None:break
            last=hit;o=hit+d*.0002
        return (last+d*gap if last is not None else q),d

    # Closed root foundation. Its front boundary lies under the swept fringe;
    # its side boundary follows the top of the ear, its rear covers the nape.
    vs=[];fs=[];uvs=[];nr=35;nc=96
    for i in range(nr):
        t=i/(nr-1)
        for j in range(nc):
            a=math.tau*j/nc;front=max(0,-math.cos(a));back=max(0,math.cos(a))
            end=1.66+.64*back-1.00*front**3
            th=.006+end*t
            p=(.086*math.sin(th)*math.sin(a),.025+.097*math.sin(th)*math.cos(a),1.576+.110*math.cos(th))
            q,d=projected(p,.0068);vs.append(q);uvs.append((j/nc,t))
    for i in range(nr-1):
        for j in range(nc):a=i*nc+j;b=i*nc+(j+1)%nc;fs.append((a,b,b+nc,a+nc))
    m.mesh('Hair | seamless scalp foundation',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)

    def strand(name,points,width,depth=.002,scalp=False,chain=None,material=mat,lift=.007):
        ps=[Vector(p) for p in points];vs=[];fs=[];uvs=[];nr=54;nc=12;prev=None
        centres=[];normals=[]
        for i in range(nr):
            t=i/(nr-1);p=catmull(ps,t)
            if scalp:q,normal=projected(p,lift+.0015*math.sin(math.pi*t))
            else:
                q=body_cage(p.copy());normal=Vector((p.x*2,-1,.10)).normalized()
                # Roots sit on the actual forehead instead of starting several
                # centimetres in front of it. Lower free locks retain their arc.
                rootfit=sstep((p.z-1.55)/.080)
                if rootfit>0:
                    attached,_=projected(p,.006);q=q.lerp(attached,rootfit)
            centres.append(q);normals.append(normal)
        m.guide(name+' | centreline',centres[::6]+[centres[-1]])
        for i in range(nr):
            t=i/(nr-1);p=centres[i]
            tangent=(centres[min(nr-1,i+1)]-centres[max(0,i-1)]).normalized()
            cross=tangent.cross(normals[i]).normalized()
            if prev is not None and cross.dot(prev)<0:cross=-cross
            prev=cross;normal=cross.cross(tangent).normalized()
            if normal.dot(normals[i])<0:normal=-normal
            root=.90 if 'gathered fan' in name else .18
            env=(root+(1-root)*sstep(t/.14))*(.83+.17*math.sin(math.pi*t))*(1-sstep((t-.63)/.37))**.72
            half=max(.000025,width*.5*env)
            for j in range(nc):
                a=math.tau*j/nc;u=math.cos(a)
                # Two curved surfaces enclose actual volume; upper ridge gives
                # a broad highlight. Roots overlap beneath the previous locks.
                h=math.sin(a)*min(depth,half*.26)
                q=p+cross*u*half+normal*h
                vs.append(q);uvs.append(((u+1)*.5,t))
        for i in range(nr-1):
            for j in range(nc):a=i*nc+j;b=i*nc+(j+1)%nc;fs.append((a,b,b+nc,a+nc))
        fs += [tuple(reversed(range(nc))),tuple((nr-1)*nc+j for j in range(nc))]
        ob=m.mesh(name,vs,fs,material,m.hair,uvs,'J_Bip_C_Head',sub=1)
        if chain:
            root=ob.vertex_groups['J_Bip_C_Head'];vg=ob.vertex_groups.new(name=chain)
            for v in ob.data.vertices:
                w=.88*sstep((v.index//nc/(nr-1)-.36)/.57)
                root.add([v.index],1-w,'REPLACE');vg.add([v.index],w,'REPLACE')
        return ob

    # Unequal primary clumps, swept from a real offset part to the gathered ear.
    flows=[
      [(.038,-.040,1.674),(.028,-.087,1.653),(-.008,-.109,1.622),(-.050,-.095,1.589),(-.082,-.045,1.564)],
      [(.041,-.006,1.690),(-.014,-.054,1.696),(-.063,-.075,1.662),(-.091,-.043,1.610),(-.106,.048,1.548)],
      [(.038,.029,1.692),(-.025,.028,1.703),(-.087,.002,1.671),(-.108,.029,1.612),(-.107,.063,1.542)],
      [(.031,.065,1.682),(-.028,.085,1.684),(-.081,.094,1.640),(-.106,.092,1.580),(-.111,.070,1.542)]
    ]
    flows=[[Vector(p) for p in path] for path in flows]
    def primary(t,u):return catmull([catmull(path,t) for path in flows],u)
    for k,u in enumerate([.015,.055,.112,.185,.262,.35,.44,.52,.60,.69,.79,.88,.97]):
        pts=[]
        for t in [0,.14,.33,.56,.78,1]:
            p=primary(t,max(0,min(1,u+.018*math.sin(2.5*k+4*t)*math.sin(math.pi*t))))
            if t==1:p+=Vector((-.004*math.sin(k),.004*math.cos(k),.009*math.sin(k*1.7)))
            pts.append(p)
        strand('Hair | primary swept clump %02d'%k,pts,.027+.007*math.sin(k*1.8)**2,.0019,True,lift=.008+.00045*(k%3))

    # Continuous fan across the whole rear skull, flowing into the low side
    # bun. The previous narrow vertical patch left a bald-looking nape strip.
    def on_head(theta,phi):
        return Vector((.086*math.sin(theta)*math.sin(phi),.025+.097*math.sin(theta)*math.cos(phi),1.576+.110*math.cos(theta)))
    for k in range(23):
        u=k/22
        pts=[on_head(.25+1.86*u,1.63-.48*u),on_head(.36+1.71*u,.82),
             on_head(.70+1.36*u,.10),on_head(1.50+.53*u,-.70),
             Vector((-.107,.068,1.545+.004*math.sin(k*1.5)))]
        strand('Hair | continuous gathered fan %02d'%k,pts,.023+.005*math.sin(k*1.7)**2,.0018,True,lift=.0075+(k%3)*.0004)
    strand('Hair | soft part root cover',[(.038,-.044,1.678),(.042,-.008,1.690),(.037,.033,1.692),(.030,.070,1.681)],
           .0048,.0011,True,lift=.010)
    for k in range(9):
        u=k/8
        pts=[(.040,-.016+.062*u,1.688),(.072,-.045+.055*u,1.658),(.085,-.060+.075*u,1.611),
             (.088,-.028+.086*u,1.568),(.080,.030+.045*u,1.540+.008*math.sin(k))]
        strand('Hair | ear swept overlap %02d'%k,pts,.026+.003*math.sin(k)**2,.0015,True,lift=.0095)

    # Forehead strands have deliberately different widths and tip heights.
    strand('Hair | diagonal signature fringe',[(.039,-.044,1.678),(.043,-.099,1.636),(.026,-.115,1.587),
           (-.009,-.114,1.551),(-.054,-.094,1.531)],.008,.0010)
    strand('Hair | secondary face fringe',[(.039,-.032,1.681),(.056,-.074,1.649),(.060,-.097,1.608),
           (.057,-.109,1.581),(.047,-.100,1.554)],.013,.0015)

    # Loose low bun: layered loops of different axis and size. A small core is
    # enclosed, so the silhouette comes from the curled locks, not a bare ball.
    c=Vector((-.102,.060,1.548))
    for k in range(16):
        phase=k*2.39996;pts=[]
        for i in range(8):
            t=i/7;a=phase+t*(3.9+.3*math.sin(k));r=.015+.011*math.sin(math.pi*t)
            pts.append(c+Vector((r*math.cos(a),r*math.sin(a),.025*math.cos(math.pi*t)+.004*math.sin(k))))
        strand('Hair | loose bun loop %02d'%k,pts,.012+.004*math.sin(k*1.7)**2,.0022)
    for k in range(10):
        a=k*2.4;pts=[]
        for i in range(7):
            t=i/6;aa=a+t*4.4
            pts.append(c+Vector((.030*math.cos(aa),.028*math.sin(aa),.029-.058*t+.004*math.sin(aa))))
        strand('Hair | bun escape %02d'%k,pts,.0008+.0004*(k%3),.0002,material=edge)
    m.tube('Hair | gathered black tie',[body_cage(Vector((-.092,.040,1.567))),body_cage(Vector((-.094,.029,1.558))),
           body_cage(Vector((-.094,.032,1.550)))],.0018,tie,'J_Bip_C_Head',col=m.hair)

    for side in [-1,1]:
        chain='J_Sec_Hair3_02' if side<0 else 'J_Sec_Hair3_05'
        main=[(side*.072,.007,1.631),(side*.078,-.040,1.553),(side*.071,-.064,1.466),
              (side*.076,-.078,1.389),(side*.075,-.139,1.310),(side*.064,-.161,1.249 if side<0 else 1.289)]
        strand('Hair | long framing lock %s'%side,main,.0095 if side<0 else .0115,.0018,chain=chain)
        for k in range(3):
            pts=[Vector(p)+Vector((side*(.001+.0016*k),-.0005*k,0)) for p in main]
            pts[-2]+=Vector((side*.002*k,.001*k,.007*k))
            pts[-1]+=Vector((side*(.004+.004*k),.008*k,.015+.016*k))
            strand('Hair | long split end %s %s'%(side,k),pts,.0022-.0004*k,.0003,chain=chain,material=edge)
    # Short close flyaways follow the crown, avoiding the previous antennae.
    for k in range(5):
        u=k/4
        pts=[(.035,.010+.05*u,1.688),(-.014,.007+.055*u,1.703),(-.070,-.007+.065*u,1.691),
             (-.102,-.018+.068*u,1.656),(-.108,-.007+.080*u,1.631)]
        strand('Hair | close crown wisp %02d'%k,pts,.00055,.00014,True,material=edge,lift=.012+.0015*math.sin(k))

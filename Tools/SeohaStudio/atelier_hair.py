"""Primary/secondary polygon hair masses with retained editable design paths."""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from atelier_common import plain,body_cage,catmull,sstep

def build_hair(m,face):
    for ob in list(bpy.context.scene.objects):
        if ob.name.startswith('Seoha hair'):bpy.data.objects.remove(ob,do_unlink=True)
    mat,bs=plain('Atelier | lavender hair satin',(.36,.295,.48),.48)
    bs.inputs['Anisotropic'].default_value=.35
    bs.inputs['Specular IOR Level'].default_value=.22
    n=mat.node_tree.nodes;l=mat.node_tree.links
    uv=n.new('ShaderNodeTexCoord');scale=n.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(95,1.5,1)
    l.new(uv.outputs['UV'],scale.inputs[0]);noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1.2
    l.new(scale.outputs[0],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.31,.252,.43,1);ramp.color_ramp.elements[1].color=(.40,.34,.52,1)
    l.new(noise.outputs[0],ramp.inputs[0]);l.new(ramp.outputs[0],bs.inputs['Base Color'])
    # An anime shadow floor keeps layered lavender hair from turning black in
    # narrow occluded areas. Surface lighting/specular still describes volume.
    l.new(ramp.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.16
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.06;bump.inputs['Distance'].default_value=.000045
    l.new(noise.outputs[0],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])
    dark,_=plain('Atelier | hair recess',(.22,.178,.32),.63)
    fine,_=plain('Atelier | fine lavender edge',(.42,.35,.54),.52)
    tie,_=plain('Atelier | black hair tie',(.009,.009,.015),.46)

    # A watertight-enough under-cap covers all roots; the front hairline stays
    # above the brow. Query the actual edited Basis, not stale mesh positions.
    body=m.body;v=[v.co.copy() for v in body.data.vertices]+[v.co.copy() for v in face.data.shape_keys.key_blocks[0].data]
    p=[tuple(p.vertices) for p in body.data.polygons if p.material_index==0]
    fp=face.data.shape_keys.key_blocks[0].data
    p += [tuple(len(body.data.vertices)+i for i in poly.vertices) for poly in face.data.polygons if poly.material_index==2
        and not all(abs(fp[i].co.x)>.073 and fp[i].co.z<1.545 for i in poly.vertices)]
    surface=BVHTree.FromPolygons(v,p)
    center=body_cage(Vector((0,.025,1.578)))
    def fit(q,extra=.002,snap=False):
        q=body_cage(Vector(q))
        fade=1. if snap else sstep((q.z-body_cage(Vector((0,0,1.555))).z)/.030)
        if fade<=0:return q
        delta=q-center;direction=delta.normalized();origin=center.copy();last=None
        for _ in range(10):
            hit=surface.ray_cast(origin,direction,.25)[0]
            if hit is None:break
            last=hit;origin=hit+direction*.0001
        if last is not None and snap:
            relief=min(.0015,max(0,delta.length-(last-center).length))*.4
            q=last+direction*(extra+relief)
        elif last is not None and (last-center).length+extra>delta.length:q=q.lerp(last+direction*extra,fade)
        return q

    vs=[];fs=[];uvs=[];rows=25;cols=96
    for i in range(rows):
        t=i/(rows-1)
        for j in range(cols):
            a=math.tau*j/cols;front=max(0,-math.cos(a))
            # Raise the temple hairline above the ear. The nape remains longer
            # and curved; scalp fitting must never coat the ear cartilage.
            end=1.65+.62*max(0,math.cos(a))-.58*front**2+.04*math.sin(a-.3)
            theta=.007+end*t
            q=(.086*math.sin(theta)*math.sin(a),.025+.096*math.sin(theta)*math.cos(a),1.576+.110*math.cos(theta))
            vs.append(fit(q,.0022,snap=True));uvs.append((j/cols,t))
    for i in range(rows-1):
        for j in range(cols):fs.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
    cap=m.mesh('Hair | continuous fitted under-cap',vs,fs,mat,m.hair,uvs,'J_Bip_C_Head')
    cap['design']='Continuous root coverage; independent from the clump silhouettes.'

    def clump(name,coords,width,depth=.004,mat=mat,chain=None,secondary=False,normal_mode='head',clearance=.003):
        ps=[Vector(p) for p in coords];rows=40 if not secondary else 30;cols=12
        vs=[];uvs=[];fs=[]
        # body_cage edits vectors in place. Keep the modelling controls in their
        # original rest space; otherwise guides accidentally deform the mesh twice.
        m.guide(name+' | edit path',[body_cage(p.copy()) for p in ps])
        for i in range(rows):
            t=i/(rows-1);p=catmull(ps,t)
            tangent=(catmull(ps,min(1,t+.001))-catmull(ps,max(0,t-.001))).normalized()
            if normal_mode=='bun':normal=(p-Vector((-.108,.061,1.546))).normalized()
            elif normal_mode=='scalp':normal=Vector((p.x/.086,(p.y-.023)/.10,(p.z-1.575)/.112)).normalized()
            else:
                headnormal=Vector((p.x/.086,(p.y-.023)/.10,(p.z-1.575)/.112)).normalized()
                frontnormal=Vector((p.x*3,-1,.06)).normalized()
                normal=frontnormal.lerp(headnormal,sstep((p.z-1.535)/.12)).normalized()
            cross=tangent.cross(normal).normalized()
            if cross.length<.01:cross=Vector((1,0,0))
            normal=cross.cross(tangent).normalized()
            # Broad roots, asymmetric widths, long tapered ends: avoid the old
            # repeated, round spaghetti profile and gaps at every hair root.
            envelope=(.045+.955*sstep(t/.24))*(1-sstep((t-.52)/.48))**.58
            w=max(.000045,width*.5*envelope)
            for j in range(cols):
                a=math.tau*j/cols;u=math.cos(a)
                h=math.sin(a)*min(depth,w*.22)
                # Low relief secondary strand valleys within each broad mass.
                if h>0:h-=.00027*math.sin((u+1)*math.pi*2.5+t*.4)**4*(1-u*u)*math.sin(math.pi*t)
                q=p+cross*(u*w)+normal*h
                root_clearance=.0014+(clearance-.0014)*sstep(t/.23)
                vs.append(body_cage(q) if normal_mode=='bun' else fit(q,root_clearance,snap=normal_mode=='scalp'));uvs.append((j/cols,t))
        for i in range(rows-1):
            for j in range(cols):fs.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
        fs.extend([tuple(reversed(range(cols))),tuple((rows-1)*cols+j for j in range(cols))])
        ob=m.mesh(name,vs,fs,mat,m.hair,uvs,'J_Bip_C_Head',sub=1)
        ob['design_width_m']=width;ob['design_depth_m']=depth
        if chain and chain in m.rig.data.bones:
            vg=ob.vertex_groups.new(name=chain);root=ob.vertex_groups['J_Bip_C_Head']
            for v in ob.data.vertices:
                t=(v.index//cols)/(rows-1);w=sstep((t-.40)/.50)
                root.add([v.index],1-w,'REPLACE');vg.add([v.index],w,'REPLACE')
        return ob

    # Gathered back volumes meet the low, character-right bun. Different paths
    # follow crown, occipital and ear regions rather than copying one ribbon.
    for i in range(9):
        u=i/8;a=-.95+2.55*u
        start=(.025+.008*math.cos(a),.006+.025*u,1.687-.008*u)
        mid=(.079*math.sin(a),.025+.105*math.cos(a),1.631-.012*u)
        low=(.043*math.sin(a)-.016,.102-.015*u,1.570-.015*u)
        end=(-.103,.057,1.540+.008*math.sin(a))
        clump('Hair | swept rear %02d'%i,[start,mid,low,end],.035+.009*math.sin(u*math.pi),.0033,normal_mode='scalp',clearance=.0027+.00035*i)

    # Four different front masses establish the side-parted design.
    clump('Hair | continuous sweep backing',[(.033,.012,1.683),(-.018,-.030,1.677),(-.076,-.053,1.635),(-.092,-.022,1.588),(-.095,.050,1.550)],.052,.0028,clearance=.0038)
    front=[
        ('crown sweep',[(.031,.018,1.685),(-.037,.002,1.686),(-.085,-.017,1.646),(-.100,.029,1.587),(-.093,.065,1.547)],.048),
        ('temple sweep',[(.036,.008,1.683),(-.008,-.048,1.678),(-.062,-.079,1.640),(-.090,-.045,1.596),(-.098,.037,1.552)],.051),
        ('brow sweep',[(.034,-.014,1.682),(.024,-.075,1.650),(-.008,-.102,1.611),(-.045,-.090,1.582),(-.076,-.054,1.562)],.046),
        ('diagonal fringe',[(.039,-.030,1.678),(.036,-.088,1.624),(.016,-.105,1.573),(-.013,-.104,1.539),(-.037,-.088,1.524)],.006),
    ]
    for index,(name,path,width) in enumerate(front):clump('Hair | '+name,path,width,clearance=.006+.0007*index)
    for i in range(3):
        u=i/2
        clump('Hair | short-side mass %s'%i,[(.040,.004+.022*u,1.680),(.067+.014*u,-.060+.029*u,1.645),
            (.080+.010*u,-.065+.032*u,1.589),(.071+.018*u,-.070+.039*u,1.542),(.075+.017*u,-.047+.040*u,1.507+.025*u)],.029-.004*u,clearance=.005+.0006*i)
    clump('Hair | inner side-part fringe',[(.039,-.003,1.681),(.045,-.060,1.652),(.052,-.079,1.612),(.049,-.091,1.575),(.035,-.095,1.552)],.018,.002,clearance=.0048)

    # The low bun is authored as overlapping looped locks, with an actual core.
    bvs=[];bfs=[];buv=[];nr=22;nc=48
    for i in range(nr+1):
        theta=.002+(math.pi-.004)*i/nr
        for j in range(nc+1):
            a=math.tau*j/nc
            q=(-.108+.027*math.sin(theta)*math.cos(a),.061+.029*math.sin(theta)*math.sin(a),1.546+.034*math.cos(theta))
            bvs.append(body_cage(Vector(q)));buv.append((j/nc,i/nr))
    for i in range(nr):
        for j in range(nc):a=i*(nc+1)+j;bfs.append((a,a+1,a+nc+2,a+nc+1))
    m.mesh('Hair | low bun volume',bvs,bfs,mat,m.hair,buv,'J_Bip_C_Head')
    for i in range(7):
        a=math.tau*i/7
        path=[]
        for theta in [.08,.60,1.2,1.82,2.43,3.04]:
            phase=a+.55*theta/math.pi
            path.append((-.108+.030*math.sin(theta)*math.cos(phase),.061+.032*math.sin(theta)*math.sin(phase),1.546+.036*math.cos(theta)))
        clump('Hair | gathered bun loop %02d'%i,path,.014,.0025,normal_mode='bun')
    tiepoints=[body_cage(Vector((-.079-.004*math.cos(math.tau*i/32),.064+.018*math.sin(math.tau*i/32),1.546+.021*math.cos(math.tau*i/32)))) for i in range(33)]
    m.tube('Hair | bound black tie',tiepoints,.0023,tie,'J_Bip_C_Head',col=m.hair,closed=True)

    # Long face framing is intentionally asymmetric. Only two primary locks,
    # with a few finer tips, so the face and jacket can be read clearly.
    for side in [-1,1]:
        path=[(side*.070,-.040,1.637),(side*.073,-.073,1.551),
            (side*.058,-.078,1.451),(side*.073,-.125,1.360),
            (side*.046,-.147,1.306 if side<0 else 1.336)]
        chain='J_Sec_Hair3_02' if side<0 else 'J_Sec_Hair3_05'
        clump('Hair | long face framing %s'%side,path,.012 if side<0 else .015,.0016,chain=chain)
        for k in range(2):
            path2=[(p[0]+side*(.005+.002*k),p[1]-.0015,p[2]+.004*k) for p in path]
            path2[-1]=(path2[-1][0]+side*(.008+.005*k),path2[-1][1]+.011,path2[-1][2]+.024+.011*k)
            clump('Hair | fine face framing %s %s'%(side,k),path2,.0026-.0007*k,.00045,fine,chain,True)
    wisps=[
        [(-.092,.044,1.577),(-.142,.018,1.575),(-.153,.006,1.530),(-.127,.014,1.500),(-.110,.026,1.517)],
        [(-.094,.066,1.553),(-.127,.013,1.516),(-.142,-.002,1.490),(-.122,-.004,1.476)],
        [(-.032,.006,1.686),(-.079,-.021,1.702),(-.110,-.012,1.680),(-.111,.019,1.661)],
        [(.029,-.027,1.686),(.068,-.071,1.674),(.105,-.072,1.604),(.096,-.071,1.560)],
    ]
    for i,path in enumerate(wisps):clump('Hair | silhouette wisp %02d'%i,path,.0013,.00024,fine,secondary=True)
    return mat

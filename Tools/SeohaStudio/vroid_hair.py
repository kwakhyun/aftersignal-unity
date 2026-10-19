"""Layered swept locks fitted to the VRoid head, with lengthwise UVs and rig weights."""
import bpy, math
from mathutils import Vector

def catmull(points,t):
    x=t*(len(points)-1);i=min(int(x),len(points)-2);s=x-i
    p0=points[max(0,i-1)];p1=points[i];p2=points[i+1];p3=points[min(len(points)-1,i+2)]
    return .5*((2*p1)+(-p0+p2)*s+(2*p0-5*p1+4*p2-p3)*s*s+(-p0+3*p1-3*p2+p3)*s*s*s)

def lock(name,coords,width,mat,rig,chain=None):
    ps=[Vector(p) for p in coords];verts=[];faces=[];uv=[];rows=60;cols=10
    for i in range(rows):
        t=i/(rows-1);p=catmull(ps,t)
        tangent=(catmull(ps,min(1,t+.001))-catmull(ps,max(0,t-.001))).normalized()
        normal=Vector((p.x/.09,(p.y-.018)/.12,(p.z-1.565)/.12)) if p.z>1.60 else Vector((p.x*1.5,-1,.1))
        across=tangent.cross(normal).normalized();normal=across.cross(tangent).normalized()
        envelope=(.22+.78*math.sin(math.pi*min(.99,t)/2))*(1-t)**.48
        w=max(.00006,width*envelope)
        for j in range(cols):
            a=2*math.pi*j/cols
            verts.append(p+across*math.cos(a)*w*.5+normal*math.sin(a)*min(.0022,w*.12))
            uv.append((j/cols,.06+.89*t))
    for i in range(rows-1):
        for j in range(cols):faces.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
    faces.extend([tuple(reversed(range(cols))),tuple((rows-1)*cols+j for j in range(cols))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(mat)
    layer=mesh.uv_layers.new(name='Strand direction')
    for p in mesh.polygons:
        p.use_smooth=True
        for loop in p.loop_indices:layer.data[loop].uv=uv[mesh.loops[loop].vertex_index]
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);ob.parent=rig
    root=ob.vertex_groups.new(name='J_Bip_C_Head')
    for v in mesh.vertices:
        t=(v.index//cols)/(rows-1)
        if chain and t>.43:
            w=min(1,(t-.43)/.42);root.add([v.index],1-w,'REPLACE')
            group=ob.vertex_groups.get(chain) or ob.vertex_groups.new(name=chain);group.add([v.index],w,'REPLACE')
        else:root.add([v.index],1,'REPLACE')
    mod=ob.modifiers.new('Seoha hair skeleton','ARMATURE');mod.object=rig
    return ob

def add_swept_locks(mat,rig):
    # A fine diagonal strand crosses the forehead; the eyes remain visible.
    lock('Seoha central loose fringe',[(.047,-.003,1.681),(.040,-.078,1.634),
        (.015,-.091,1.578),(-.020,-.078,1.541)],.006,mat,rig)
    for side in [-1,1]:
        for i in range(3):
            d=i/2
            lock('Seoha curved long lock %s %s'%(side,i),[(side*(.066+.007*d),-.024,1.644),
                (side*(.081+.005*d),-.059,1.56),
                (side*(.062+.009*d),-.058,1.45),
                (side*(.068+.006*d),-.092,1.351+.027*d),
                (side*(.053+.022*d),-.122,1.319+.037*d)],.010-.003*d,mat,rig,
                'J_Sec_Hair3_02' if side<0 else 'J_Sec_Hair3_05')

"""Editable polygon/curve modelling helpers for the private Seoha study."""
import math
import bpy,bmesh
from mathutils import Vector,Quaternion
from mathutils.kdtree import KDTree
from seoha_deformation import body_cage,g,sstep
from vroid_hair import catmull

def collection(name,hidden=False):
    c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c)
    c.hide_render=hidden;c.hide_viewport=hidden
    return c

def relocate(ob,col):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    col.objects.link(ob)

def plain(name,color,rough=.5,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Specular IOR Level'].default_value=.25
    return m,bs

def grain(m,bs,scale,depth):
    n=m.node_tree.nodes;l=m.node_tree.links
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=2
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=depth
    l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])

class Modeler:
    def __init__(self,rig,body):
        self.rig=rig;self.body=body;self.tree=KDTree(len(body.data.vertices))
        for v in body.data.vertices:self.tree.insert(v.co,v.index)
        self.tree.balance()
        self.guides=collection('00 AUTHORING | editable hair and seam paths',True)
        self.hair=collection('01 HAIR | designed clump topology')
        self.outfit=collection('02 OUTFIT | tailoring and equipment')
        self.detail=collection('03 DETAILS | hardware and bound seams')

    def bind(self,ob,bone=None,torso=False):
        for v in ob.data.vertices:
            if bone:weights=[(bone,1.)]
            elif torso:
                t=sstep((v.co.z-1.17)/.16)
                weights=[('J_Bip_C_Chest',1-t),('J_Bip_C_UpperChest',t)]
            else:
                _,idx,_=self.tree.find(v.co)
                weights=[(self.body.vertex_groups[w.group].name,w.weight) for w in self.body.data.vertices[idx].groups if w.weight>1e-5]
            total=sum(w for _,w in weights)
            for name,w in weights:
                if w<1e-6:continue
                group=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name)
                group.add([v.index],w/total,'REPLACE')
        mod=ob.modifiers.new('Deform with Seoha rig','ARMATURE');mod.object=self.rig
        ob.parent=self.rig

    def mesh(self,name,vs,fs,mat,col=None,uv=None,bone=None,torso=False,sub=0):
        data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();data.materials.append(mat)
        # Establish consistently outward normals before subdivision or skinning.
        bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
        ob=bpy.data.objects.new(name,data);(col or self.detail).objects.link(ob)
        if uv:
            layer=data.uv_layers.new(name='Authored UV')
            for p in data.polygons:
                for i in p.loop_indices:layer.data[i].uv=uv[data.loops[i].vertex_index]
        for p in data.polygons:p.use_smooth=True
        if sub:
            mod=ob.modifiers.new('Smooth authored topology','SUBSURF');mod.levels=sub;mod.render_levels=sub
        self.bind(ob,bone,torso)
        return ob

    def guide(self,name,points):
        data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=24
        spline=data.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
        for p,co in zip(spline.bezier_points,points):
            p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
        ob=bpy.data.objects.new(name,data);self.guides.objects.link(ob)
        ob['purpose']='Editable design path; rendered mesh is the corresponding polygon clump.'
        return ob

    def tube(self,name,points,r,mat,bone=None,torso=False,col=None,closed=False,rows=None):
        ps=[Vector(p) for p in points];vs=[];uv=[];fs=[];cols=8
        rows=rows or max(12,len(ps)*5)
        for i in range(rows):
            t=i/(rows-1);p=catmull(ps,t)
            tangent=(catmull(ps,min(1,t+.001))-catmull(ps,max(0,t-.001))).normalized()
            axis=Vector((0,1,0)) if abs(tangent.y)<.9 else Vector((1,0,0))
            right=tangent.cross(axis).normalized();up=tangent.cross(right).normalized()
            for j in range(cols):
                a=math.tau*j/cols;vs.append(p+r*(math.cos(a)*right+math.sin(a)*up));uv.append((j/cols,t))
        for i in range(rows-1):
            for j in range(cols):fs.append((i*cols+j,i*cols+(j+1)%cols,(i+1)*cols+(j+1)%cols,(i+1)*cols+j))
        if not closed:fs.extend([tuple(reversed(range(cols))),tuple((rows-1)*cols+j for j in range(cols))])
        return self.mesh(name,vs,fs,mat,col=col,uv=uv,bone=bone,torso=torso)

    def box(self,name,center,size,mat,bone=None,torso=False,bevel=.001,rotation=None):
        center=Vector(center);sx,sy,sz=[v*.5 for v in size]
        vs=[Vector((x,y,z)) for x in [-sx,sx] for y in [-sy,sy] for z in [-sz,sz]]
        if rotation:vs=[rotation@p for p in vs]
        vs=[center+p for p in vs]
        fs=[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]
        ob=self.mesh(name,vs,fs,mat,bone=bone,torso=torso)
        if bevel:
            mod=ob.modifiers.new('Manufactured rounded edges','BEVEL');mod.width=bevel;mod.segments=3
        return ob

    def rivet(self,name,p,r,mat,bone=None,torso=False):
        ps=[Vector(p)+Vector((r*math.cos(math.tau*i/20),0,r*math.sin(math.tau*i/20))) for i in range(21)]
        return self.tube(name,ps,r*.22,mat,bone=bone,torso=torso,closed=True,rows=36)

def reset_pose(rig,face):
    rig.animation_data_clear()
    for b in rig.pose.bones:
        b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
    for k in face.data.shape_keys.key_blocks:k.value=0

def rotate(rig,name,axis,degrees):
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion()
    b.rotation_quaternion=q.inverted()@Quaternion(Vector(axis),math.radians(degrees))@q

def relaxed(rig,face):
    reset_pose(rig,face)
    for side,angle in [('L',76),('R',-76)]:
        rotate(rig,'J_Bip_'+side+'_UpperArm',(0,1,0),angle)
        rotate(rig,'J_Bip_'+side+'_LowerArm',(0,0,1),-10 if side=='L' else 10)
        for digit in ['Index','Middle','Ring','Little']:
            for n,a in [(1,12),(2,20),(3,12)]:
                pb=rig.pose.bones.get('J_Bip_'+side+'_'+digit+str(n))
                if pb:pb.rotation_quaternion=Quaternion((1,0,0),math.radians(a))

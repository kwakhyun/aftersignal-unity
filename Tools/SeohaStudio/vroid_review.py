"""Actual Blender mesh renders; never image-generated review evidence."""
from pathlib import Path
import bpy, json, sys
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Artifacts/SeohaStudio/VRoid'

def setup():
    scene = bpy.context.scene
    # Repeated review passes must not accumulate light energy or cameras.
    for ob in list(bpy.data.objects):
        if (ob.type=='LIGHT' and ob.name.split('.')[0] in ['Key','Fill','Rim']) or (ob.type=='CAMERA' and ob.name.startswith('Review Camera')):
            bpy.data.objects.remove(ob,do_unlink=True)
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.render.resolution_x, scene.render.resolution_y = 900, 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.view_settings.view_transform = 'Standard'
    scene.world = bpy.data.worlds.new('Neutral studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.066,.085,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
    def aim(obj, target):
        obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    for name,pos,power,size in [('Key',(-2,-3,4),400,3),('Fill',(3,-2,2),220,3),('Rim',(1,2,3),500,2)]:
        data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK';data.size=size
        ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=pos;aim(ob,(0,0,1.35))
    data=bpy.data.cameras.new('Review Camera');data.type='ORTHO'
    cam=bpy.data.objects.new('Review Camera',data);scene.collection.objects.link(cam);scene.camera=cam
    return scene,cam,aim

def render_views(prefix='Refined', views=None):
    scene,cam,aim=setup()
    if views is None:
        views=[('Face',(0,-3,1.57),(0,0,1.54),.46),
               ('ThreeQuarter',(.9,-2.5,1.56),(0,0,1.52),.53),
               ('Profile',(3,-.05,1.55),(0,0,1.53),.49),
               ('Body',(.5,-3,1.05),(0,0,.87),1.97)]
    for label,pos,target,scale in views:
        cam.location=pos;aim(cam,target);cam.data.ortho_scale=scale
        scene.render.filepath=str(OUT/(prefix+'-'+label+'.png'))
        bpy.ops.render.render(write_still=True)
    return scene

def components(mesh):
    neighbors=[[] for v in mesh.vertices]
    for e in mesh.edges:
        a,b=e.vertices;neighbors[a].append(b);neighbors[b].append(a)
    seen=set(); result=[]
    for v in mesh.vertices:
        if v.index in seen:continue
        stack=[v.index];seen.add(v.index);ids=[]
        while stack:
            i=stack.pop();ids.append(i)
            for j in neighbors[i]:
                if j not in seen:seen.add(j);stack.append(j)
        result.append(ids)
    return result

if __name__=='__main__':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seoha-Imported.blend'))
    report={}
    for name in ['Hair','Body','Face']:
        ob=bpy.data.objects[name]
        report[name]=[]
        for i,ids in enumerate(components(ob.data)):
            points=[ob.matrix_world@ob.data.vertices[j].co for j in ids]
            lo=[min(p[k] for p in points) for k in range(3)];hi=[max(p[k] for p in points) for k in range(3)]
            groups=set(g.group for j in ids for g in ob.data.vertices[j].groups if g.weight>.4)
            report[name].append({'i':i,'n':len(ids),'min':lo,'max':hi,
                'groups':[ob.vertex_groups[g].name for g in groups], 'vertices':ids})
    (OUT/'mesh-islands.json').write_text(json.dumps(report),encoding='utf-8')
    render_views('Base', [('Face',(0,-3,1.57),(0,0,1.54),.46),('ThreeQuarter',(.9,-2.5,1.56),(0,0,1.52),.53)])

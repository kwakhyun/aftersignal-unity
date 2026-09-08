"""Render the authored .blend sources without changing the shipped models."""
from pathlib import Path
import bpy, math
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
out=root/'Artifacts/RegionalExpansion/Studio';out.mkdir(parents=True,exist_ok=True)
for name in ['VelaR','RosaM','ChimeraC','ApexRR','NomadGS','ClassicTwin']:
    bpy.ops.wm.open_mainfile(filepath=str(root/'Documentation/Mobility/Models'/f'{name}.blend'))
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
    scene.render.resolution_x=1000;scene.render.resolution_y=650;scene.render.resolution_percentage=100
    scene.world.color=(.27,.27,.27)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.02))
    mat=bpy.data.materials.new('Studio floor');mat.diffuse_color=(.13,.15,.17,1);bpy.context.object.data.materials.append(mat)
    for pos,power,size in [((2,-4,7),1800,6),((-3,3,5),2200,5),((5,3,2),800,4)]:
        bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size;lamp.rotation_euler=(Vector((0,0,.7))-lamp.location).to_track_quat('-Z','Y').to_euler()
    bike=name in ['ApexRR','NomadGS','ClassicTwin']
    bpy.ops.object.camera_add(location=(5.8,-8.3,4.1) if not bike else (3.6,-5.7,2.7));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=7.2 if not bike else 3.7;scene.camera=cam
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)

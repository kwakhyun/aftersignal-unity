"""Render all reduced botanical meshes at the same scale for silhouette review."""
import bpy,pathlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for lod in range(3):
    bpy.ops.import_scene.fbx(filepath=str(root/f'Artifacts/Fidelity/Botanical/BotanicalTree_LOD{lod}.fbx'))
    for obj in bpy.context.selected_objects:obj.location.x+=(lod-1)*5
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=-.02
m=bpy.data.materials.new('Matte studio ground');m.diffuse_color=(.13,.16,.17,1);bpy.context.object.data.materials.append(m)
bpy.ops.object.camera_add(location=(10,-23,10));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,2))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=18;bpy.context.scene.camera=camera
bpy.ops.object.light_add(type='SUN',location=(0,-4,8));sun=bpy.context.object;sun.rotation_euler=(.4,-.5,-.3);sun.data.energy=2;sun.data.angle=.15
scene=bpy.context.scene;scene.world.color=(.3,.3,.3);scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1600;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(root/'Artifacts/Fidelity/tree-lod-preview.png');bpy.ops.render.render(write_still=True)

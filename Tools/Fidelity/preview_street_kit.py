import bpy, pathlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];source=root/'Artifacts/Fidelity/StreetKit'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,at in [('TransitShelter',(-2,2,0)),('CoastalPalm',(3,3,0)),('CivicKiosk',(2,-.5,0)),('PromenadeBench',(-3,-2,0)),('ChargePoint',(0,-2,0)),('ClimateUnit',(4,-2,0)),('SmartBollard',(-5,-2,0))]:
    bpy.ops.import_scene.fbx(filepath=str(source/(name+'.fbx')))
    for obj in bpy.context.selected_objects:obj.location+=Vector(at)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.04;m=bpy.data.materials.new('Studio floor');m.diffuse_color=(.08,.1,.12,1);floor.data.materials.append(m)
bpy.ops.object.camera_add(location=(12,-17,11));cam=bpy.context.object;cam.rotation_euler=(Vector((0,.5,1.4))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=16;bpy.context.scene.camera=cam
for at,power,color,size in [((1,-4,10),1700,(.72,.85,1),8),((-7,0,6),1000,(1,.64,.34),5),((1,7,8),1800,(.4,.8,1),6)]:
    bpy.ops.object.light_add(type='AREA',location=at);l=bpy.context.object;l.data.energy=power;l.data.color=color;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.world.color=(.13,.13,.13);scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(root/'Artifacts/Fidelity/street-kit-preview.png');bpy.ops.render.render(write_still=True)

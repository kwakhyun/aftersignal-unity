"""Render the real reconstructed mesh, including side and rear views."""
from pathlib import Path
import math
import bpy
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Artifacts/SeohaStudio/Reconstruction"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(OUT / "Seoha-TripoSR.glb"))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
# reconstruct_reference exports standard glTF Y-up; Blender converts to Z-up.
bpy.context.view_layer.update()
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
factor = 1.73 / (high.z - low.z)
offset = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
for obj in meshes:
    obj.matrix_world = Matrix.Scale(factor, 4) @ Matrix.Translation(-offset) @ obj.matrix_world
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj["study_only"] = True
    obj["rigged"] = False
    obj["source"] = "MIT TripoSR single image reconstruction"

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 32
scene.cycles.use_denoising = True
cycles_preferences = bpy.context.preferences.addons["cycles"].preferences
cycles_preferences.compute_device_type = "OPTIX"
cycles_preferences.get_devices()
for device in cycles_preferences.devices:
    device.use = device.type != "CPU"
scene.cycles.device = "GPU"
scene.render.resolution_x = 800
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new("Neutral review world")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs[0].default_value = (.13, .15, .18, 1)
scene.world.node_tree.nodes["Background"].inputs[1].default_value = .6
scene.view_settings.view_transform = "Standard"
scene.render.image_settings.file_format = "PNG"

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

for name, position, power, size in [
    ("Key", (3,-3,4), 420, 4), ("Fill", (1,3,2), 200, 3), ("Rim", (-3,0,3), 250, 2)
]:
    data = bpy.data.lights.new(name, "AREA")
    data.energy, data.shape, data.size = power, "DISK", size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = position
    point_at(obj, (0,0,1))

bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.008))
floor = bpy.context.object
floor.name = "Review floor"
mat = bpy.data.materials.new("Neutral floor")
mat.diffuse_color = (.13,.15,.18,1)
floor.data.materials.append(mat)
camera_data = bpy.data.cameras.new("Review camera")
camera = bpy.data.objects.new("Review camera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"

views = [
    ("Front", (3,0,1.0), (0,0,.88), 1.98),
    ("ThreeQuarter", (3,2,1.05), (0,0,.88), 1.98),
    ("Side", (0,3,1.0), (0,0,.88), 1.98),
    ("Back", (-3,0,1.0), (0,0,.88), 1.98),
    ("Face", (3,0,1.61), (0,0,1.57), .45),
]
for label, position, target, scale in views:
    camera.location = position
    point_at(camera, target)
    camera_data.ortho_scale = scale
    scene.render.filepath = str(OUT / ("TripoSR-" + label + ".png"))
    bpy.ops.render.render(write_still=True)
camera.location = (3,2,1.05)
point_at(camera, (0,0,.88))
camera_data.ortho_scale = 1.98
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "Seoha-TripoSR-Review.blend"))
print("REAL_MESH_REVIEW_COMPLETE")

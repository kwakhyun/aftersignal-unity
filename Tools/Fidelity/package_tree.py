"""Package the reduced CC0 meshes and repack ARM channels for Unity URP Lit."""
import pathlib, shutil, json
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[2]
source=root/'Artifacts/Fidelity/TreeSource'; target=root/'Assets/AfterSignal/Resources/WorldAssets/Botanical'
target.mkdir(parents=True,exist_ok=True)
for model in (root/'Artifacts/Fidelity/Botanical').glob('*.fbx'):shutil.copyfile(model,target/model.name)
for part,prefix in [('branch','tree_small_02_branch'),('leaves','tree_small_02_leaves'),('trunk','tree_small_02')]:
    for source_suffix,dest_suffix in [('diff','albedo'),('nor_gl','normal')]:
        shutil.copyfile(source/'textures'/f'{prefix}_{source_suffix}_1k.jpg',target/f'{part}_{dest_suffix}.jpg')
    arm=Image.open(source/'textures'/f'{prefix}_arm_1k.jpg').convert('RGB');ao,rough,metal=arm.split()
    Image.merge('RGBA',(metal,ao,Image.new('L',arm.size,0),rough.point(lambda p:255-p))).save(target/f'{part}_mask.png')
provenance=json.loads((source/'provenance.json').read_text())
record={'source':provenance,'processing':'Blender mesh decimation; deterministic leaf thinning with canopy coverage compensation for distant LODs; Unity metallic/AO/smoothness channel packing.', 'lods':json.loads((root/'Artifacts/Fidelity/Botanical/lod-stats.json').read_text())}
(root/'Documentation/Fidelity/botanical-provenance.json').write_text(json.dumps(record,indent=2))
print('Packaged botanical LODs and nine material maps.')

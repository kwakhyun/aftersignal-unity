"""Record original batch mesh GUIDs without replacing the relocated prefabs."""
import json, re, subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
baseline = '14a5ed787f8ec2035f994640497dbbce6d03a4a3'
items = []
for source in ('AfterlightExpansion', 'MobilityDistricts', 'CivicRenewal', 'NeonHarbor'):
    path = f'Assets/AfterSignal/Resources/WorldAssets/{source}.prefab'
    data = subprocess.check_output(['git', 'show', f'{baseline}:{path}'], cwd=root).decode('utf-8')
    docs = re.split(r'^--- !u!', data, flags=re.M)
    names = {}
    for doc in docs:
        match = re.match(r'1 &(\d+)\n', doc)
        name = re.search(r'^  m_Name: (.*)$', doc, re.M)
        if match and name:
            names[match[1]] = name[1].strip('"')
    for doc in docs:
        if not doc.startswith('33 &'):
            continue
        owner = re.search(r'm_GameObject: \{fileID: (\d+)\}', doc)
        mesh = re.search(r'm_Mesh: \{fileID: \d+, guid: ([a-f0-9]+)', doc)
        if owner and mesh and names.get(owner[1], '').startswith('Batch '):
            items.append(dict(source=source, name=names[owner[1]], guid=mesh[1]))
out = root / 'Artifacts/CompactCities/original-batches.json'
out.write_text(json.dumps(dict(items=items)), encoding='utf-8')
print(f'{len(items)} original batch mesh references recorded')

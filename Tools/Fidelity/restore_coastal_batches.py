"""Recover only the missing baked presentation subtree, retaining newer gameplay edits.

The last complete coastal prefab is 2dc22dce. No assets are deleted and no full
prefab rollback is performed. Verify all restored mesh GUIDs before writing.
"""
from pathlib import Path
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
REL = 'Assets/AfterSignal/Resources/WorldAssets/AfterlightExpansion.prefab'
SOURCE = '2dc22dce'

def docs(text):
    return {m.group(1): m.group(0) for m in re.finditer(
        r'^--- !u!\d+ &(-?\d+)\n.*?(?=^--- !u!|\Z)', text, re.M | re.S)}

def refs(text):
    return re.findall(r'\{fileID: (-?\d+)\}', text)

def recover():
    old = subprocess.check_output(['git', 'show', SOURCE + ':' + REL], cwd=ROOT).decode('utf-8')
    path = ROOT / REL
    current = path.read_text(encoding='utf-8')
    before, now = docs(old), docs(current)
    parent_go = next(k for k, v in before.items() if '\n  m_Name: Batched district geometry\n' in v)
    parent = next(x for x in refs(before[parent_go]) if x != '0')
    current_go = next(k for k, v in now.items() if '\n  m_Name: Batched district geometry\n' in v)
    current_parent = next(x for x in refs(now[current_go]) if x != '0')
    assert 'm_Children: []' in now[current_parent], 'Refuse to overwrite a populated batch subtree'
    child_text = before[parent].split('  m_Children:\n', 1)[1].split('  m_Father:', 1)[0]
    pending = refs(child_text)
    recovered = {}
    while pending:
        key = pending.pop()
        if key == parent or key in recovered:
            continue
        assert key not in now, f'Refuse to overwrite existing component {key}'
        block = before[key]
        recovered[key] = block
        pending.extend(x for x in refs(block) if x != '0' and x != parent and x not in recovered)
    guids = set(re.findall(r'guid: ([a-f0-9]{32})', ''.join(recovered.values())))
    known = set()
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})', meta.read_text(encoding='utf-8'), re.M)
        if match:
            known.add(match[1])
    assert not guids - known, f'Missing dependencies: {guids - known}'
    fixed_parent = now[current_parent].replace('  m_Children: []\n', '  m_Children:\n' + child_text)
    subtree = ''.join(recovered.values()).replace('{fileID: ' + parent + '}', '{fileID: ' + current_parent + '}')
    result = current.replace(now[current_parent], fixed_parent) + subtree
    assert len(docs(result)) == len(now) + len(recovered)
    path.write_text(result, encoding='utf-8', newline='\n')
    report = {'source': SOURCE, 'prefab': REL, 'restored_components': len(recovered),
              'restored_batches': sum('  m_Name: Batch ' in s for s in recovered.values()),
              'dependencies_present': len(guids)}
    destination = ROOT / 'Artifacts/WorldRecovery/restoration.json'
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))

if __name__ == '__main__':
    recover()

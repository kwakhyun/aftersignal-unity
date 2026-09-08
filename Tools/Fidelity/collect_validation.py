"""Preserve compact evidence from the native quality pass without claiming gameplay FPS."""
import pathlib,json,shutil
root=pathlib.Path(__file__).resolve().parents[2];art=root/'Artifacts/Fidelity';docs=root/'Documentation/Fidelity'
before=json.loads((art/'BeforeRendered/benchmark.json').read_text())
after=json.loads((art/'AfterShipping/benchmark.json').read_text())
floors=json.loads((art/'Floors/four-cities.json').read_text())
build=json.loads((root/'Artifacts/build-result.json').read_text())
if not after['completed'] or after['errors'] or floors['errors'] or build['errors']:
    raise SystemExit('Do not publish a successful validation record while a required check is failing.')
comparison=[]
for first,last in zip(before['samples'],after['samples']):
    if first['view']!=last['view']:raise ValueError('Camera order changed; comparisons would be invalid.')
    comparison.append({'view':first['view'],'beforeMeanMs':first['meanMs'],'afterMeanMs':last['meanMs'],'beforeP95Ms':first['p95Ms'],'afterP95Ms':last['p95Ms'],'afterMemoryBytes':last['memoryBytes']})
result={'build':build,'device':after['device'],'unity':after['unity'],'resolution':[after['width'],after['height']],
    'timingMethod':'120 explicitly submitted URP frames per view, including synchronous one-pixel GPU readback and a frame yield; hidden Windows player. Comparative timings include synchronization and scheduling overhead; they are not normal gameplay FPS or a controlled repeatability study.',
    'baselineCommit':'0968b341','comparison':comparison,'floorChecks':floors['passed'],'fidelityChecks':after['passed'],'errors':after['errors'],
    'screenshots':[str(p.relative_to(root)).replace('\\','/') for p in sorted((art/'AfterShipping').glob('*.png'))]}
(docs/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
review=docs/'Review';review.mkdir(exist_ok=True)
for source,name in [('BeforeRendered/02-core-night.png','night-before.png'),('AfterShipping/02-core-night.png','night-after.png'),('BeforeRendered/06-interior.png','interior-before.png'),('AfterShipping/06-interior.png','interior-after.png'),('AfterShipping/07-garden.png','garden-after.png'),('AfterShipping/08-street-detail.png','street-after.png')]:shutil.copyfile(art/source,review/name)
print('Recorded',len(floors['passed'])+len(after['passed']),'successful essential checks;',len(result['screenshots']),'native views.')

"""Behavioral tests; fixtures stay in temporary folders, never game Resources."""
import copy
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch
import wave

import audio_pack as pack


class AudioPackTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.base = Path(self.temp.name)
        self.root = self.base / 'project'
        story = self.root / pack.STORY
        story.parent.mkdir(parents=True)
        shutil.copyfile(pack.ROOT / pack.STORY, story)
        self.incoming = self.base / 'incoming'
        self.incoming.mkdir()
        self.manifest = self.incoming / 'pack.json'
        self.assets = pack.plan(self.root, include_voice=True)['assets']

    def tearDown(self):
        self.temp.cleanup()

    def asset(self, index=0, sample=1000):
        entry = copy.deepcopy(self.assets[index])
        entry.update(source_file=entry['id'] + '.wav', source_url='local-test-fixture', reviewed=True)
        with wave.open(str(self.incoming / entry['source_file']), 'wb') as wav:
            wav.setnchannels(1)
            wav.setsampwidth(2)
            wav.setframerate(48000)
            wav.writeframes(sample.to_bytes(2, 'little', signed=True) * 4800)
        return entry

    def write(self, *assets):
        self.manifest.write_text(json.dumps(dict(schema=1, assets=list(assets)), ensure_ascii=False), encoding='utf-8')
        return self.manifest

    def test_plan_matches_live_story_and_contains_no_produced_files(self):
        self.assertEqual(14, len(self.assets))
        self.assertEqual(8, sum(a['kind'] == 'sfx' for a in self.assets))
        for asset in self.assets:
            self.assertIsNone(asset['source_file'])
            self.assertFalse(asset['reviewed'])
            if asset['kind'] == 'voice':
                self.assertEqual((asset['speaker'], asset['text']), pack.story_line(self.root, asset['source_story']))
                self.assertNotEqual('', asset['spoken_text'])
        self.assertFalse((self.root / pack.GENERATED).exists())

    def test_default_plan_excludes_cancelled_voice_production(self):
        assets = pack.plan(self.root)['assets']
        self.assertEqual(8, len(assets))
        self.assertTrue(all(a['kind'] == 'sfx' for a in assets))

    def test_full_catalog_loop_requires_seam_review_and_installs_separately(self):
        catalog = self.root / 'Documentation/Audio/mirelo-sfx-plan.json'
        catalog.parent.mkdir(parents=True)
        catalog.write_text(json.dumps({'requests':[{'cue':'sedan','kind':'loop'}]}),encoding='utf-8')
        asset = self.asset()
        asset.update(kind='loop', id='sedan', cue='sedan')
        with self.assertRaisesRegex(ValueError, 'reviewed seam'):
            pack.validate(self.write(asset),self.root)
        asset['loop_seam_reviewed'] = True
        outputs = pack.install(self.write(asset),self.root)
        self.assertTrue((self.root/pack.GENERATED/'Loops/sedan.wav').is_file())
        self.assertFalse((self.root/pack.GENERATED/'Voice/catalog.json').exists())
        self.assertIn('Loops',outputs[0])

    def test_valid_sfx_installs_unchanged_and_records_hash(self):
        asset = self.asset()
        path = self.write(asset)
        original = (self.incoming / asset['source_file']).read_bytes()
        before = self.manifest.read_bytes()
        _, rows = pack.validate(path, self.root)
        self.assertFalse((self.root / pack.GENERATED).exists())
        outputs = pack.install(path, self.root)
        self.assertEqual(original, (self.root / outputs[0]).read_bytes())
        self.assertEqual(before, self.manifest.read_bytes())
        receipt = next((self.root / 'Artifacts/AudioProduction/receipts').glob('*.json'))
        self.assertEqual(rows[0][2]['sha256'], pack.read_json(receipt)['assets'][0]['sha256'])

    def test_invalid_later_asset_never_partially_installs(self):
        path = self.write(self.asset(0), self.asset(1, sample=0))
        with self.assertRaisesRegex(ValueError, 'silent'):
            pack.install(path, self.root)
        self.assertFalse((self.root / pack.GENERATED).exists())

    def test_voice_catalog_merges_exact_speaker_and_text(self):
        first = self.asset(8)
        second = self.asset(9)
        pack.install(self.write(first), self.root)
        pack.install(self.write(second), self.root)
        catalog = pack.read_json(self.root / pack.GENERATED / 'Voice/catalog.json')
        self.assertEqual(2, len(catalog['lines']))
        self.assertEqual(first['text'], catalog['lines'][0]['text'])
        self.assertEqual(first['id'], catalog['lines'][0]['clip'])
        self.assertEqual('서하 · 조사 기록', catalog['lines'][1]['speaker'])

    def test_existing_clip_prevents_entire_batch(self):
        first = self.asset()
        pack.install(self.write(first), self.root)
        target = self.root / pack.GENERATED / 'SFX' / (first['id'] + '.wav')
        before = target.read_bytes()
        second = self.asset(1)
        with self.assertRaisesRegex(ValueError, 'overwrite'):
            pack.install(self.write(second, first), self.root)
        self.assertEqual(before, target.read_bytes())
        self.assertFalse((target.parent / (second['id'] + '.wav')).exists())

    def test_parent_escape_is_rejected(self):
        asset = self.asset()
        shutil.copyfile(self.incoming / asset['source_file'], self.base / 'outside.wav')
        asset['source_file'] = '../outside.wav'
        with self.assertRaisesRegex(ValueError, 'inside the pack'):
            pack.validate(self.write(asset), self.root)

    def test_stale_story_text_is_rejected(self):
        asset = self.asset(8)
        asset['text'] += ' edited'
        with self.assertRaisesRegex(ValueError, 'current story'):
            pack.validate(self.write(asset), self.root)

    def test_unreviewed_or_missing_provenance_is_rejected(self):
        asset = self.asset()
        asset['reviewed'] = False
        with self.assertRaisesRegex(ValueError, 'reviewed'):
            pack.validate(self.write(asset), self.root)
        asset['reviewed'] = True
        asset['source_url'] = ''
        with self.assertRaisesRegex(ValueError, 'provenance'):
            pack.validate(self.write(asset), self.root)

    def test_noncommercial_preview_keeps_human_review_false_and_rejects_other_packs(self):
        asset = self.asset()
        asset.update(reviewed=False, license_plan='Free', commercial_use=False)
        pack.install(self.write(asset), self.root, local_preview=True)
        receipt = pack.read_json(next((self.root / 'Artifacts/AudioProduction/receipts').glob('*.json')))
        self.assertEqual('noncommercial_local_preview', receipt['installation_mode'])
        self.assertFalse(receipt['assets'][0]['reviewed'])
        asset['commercial_use'] = True
        with self.assertRaisesRegex(ValueError, 'noncommercial'):
            pack.validate(self.write(asset), self.root, local_preview=True)

    def test_preview_loop_checks_actual_pcm_seam(self):
        catalog = self.root / 'Documentation/Audio/mirelo-sfx-plan.json'
        catalog.parent.mkdir(parents=True)
        catalog.write_text(json.dumps({'requests':[{'cue':'sedan','kind':'loop'}]}),encoding='utf-8')
        asset = self.asset()
        asset.update(kind='loop',id='sedan',cue='sedan',reviewed=False,license_plan='Free',commercial_use=False)
        source = self.incoming / asset['source_file']
        with wave.open(str(source), 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(48000)
            wav.writeframes((1000).to_bytes(2,'little',signed=True)*4799 + bytes(2))
        with self.assertRaisesRegex(ValueError, 'discontinuous seam'):
            pack.validate(self.write(asset),self.root,local_preview=True)

    def test_mixed_generated_single_and_variants_are_rejected(self):
        single = self.asset()
        variant = dict(single, id=single['id'] + '_0')
        with self.assertRaisesRegex(ValueError, 'not both'):
            pack.validate(self.write(single, variant), self.root)
        pack.install(self.write(single), self.root)
        with self.assertRaisesRegex(ValueError, 'shadow'):
            pack.install(self.write(variant), self.root)

    def test_clipping_and_truncated_pcm_are_rejected(self):
        asset = self.asset(sample=32767)
        with self.assertRaisesRegex(ValueError, 'clipped'):
            pack.validate(self.write(asset), self.root)
        asset = self.asset()
        source = self.incoming / asset['source_file']
        source.write_bytes(source.read_bytes()[:-20])
        with self.assertRaisesRegex(ValueError, 'truncated'):
            pack.validate(self.write(asset), self.root)

    def test_io_failure_restores_catalog_and_removes_new_copies(self):
        first = self.asset(8)
        pack.install(self.write(first), self.root)
        catalog = self.root / pack.GENERATED / 'Voice/catalog.json'
        before = catalog.read_bytes()
        second = self.asset(9)
        self.write(second)
        original = Path.write_text

        def fail_receipt(path, text, *args, **kwargs):
            if path.parent.name == 'receipts':
                raise OSError('Simulated disk failure')
            return original(path, text, *args, **kwargs)

        with patch.object(Path, 'write_text', fail_receipt):
            with self.assertRaisesRegex(OSError, 'Simulated'):
                pack.install(self.manifest, self.root)
        self.assertEqual(before, catalog.read_bytes())
        self.assertFalse((catalog.parent / (second['id'] + '.wav')).exists())



    def test_windows_reserved_voice_ids_are_rejected(self):
        for name in ('con', 'nul', 'prn', 'aux', 'com1', 'lpt9'):
            asset = self.asset(8)
            asset['id'] = name
            with self.assertRaisesRegex(ValueError, 'Invalid'):
                pack.validate(self.write(asset), self.root)

    def test_source_open_failure_leaves_no_empty_target(self):
        asset = self.asset()
        self.write(asset)
        source = (self.incoming / asset['source_file']).resolve()
        original = Path.open
        validated = pack.validate(self.manifest, self.root)

        def fail_source(path, mode='r', *args, **kwargs):
            if path.resolve() == source and mode == 'rb':
                raise OSError('Source disappeared after validation')
            return original(path, mode, *args, **kwargs)

        with patch.object(pack, 'validate', return_value=validated), patch.object(Path, 'open', fail_source):
            with self.assertRaisesRegex(OSError, 'Source disappeared'):
                pack.install(self.manifest, self.root)
        self.assertFalse((self.root / pack.GENERATED / 'SFX' / (asset['id'] + '.wav')).exists())


    def test_other_audio_extension_with_same_resource_name_is_rejected(self):
        asset = self.asset()
        folder = self.root / pack.GENERATED / 'SFX'
        folder.mkdir(parents=True)
        existing = folder / (asset['id'] + '.ogg')
        existing.write_bytes(b'existing manually installed clip')
        with self.assertRaisesRegex(ValueError, 'overwrite'):
            pack.install(self.write(asset), self.root)
        self.assertFalse((folder / (asset['id'] + '.wav')).exists())
        self.assertEqual(b'existing manually installed clip', existing.read_bytes())

if __name__ == '__main__':
    unittest.main()

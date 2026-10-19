"""Offline production plan, WAV validation and additive Unity import. No API calls."""
import argparse
from array import array
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import wave

ROOT = Path(__file__).resolve().parents[2]
GENERATED = Path('Assets/AfterSignal/Resources/Audio/Generated')
STORY = Path('Assets/AfterSignal/Resources/Story/CityChronicle.json')
NAME = re.compile(r'[a-z][a-z0-9_]{0,79}\Z')
RESERVED = {'con', 'prn', 'aux', 'nul'} | {f'{p}{i}' for p in ('com', 'lpt') for i in range(1, 10)}
SFX = [
    ('blade_swing', .4, 'One fast katana slash through air. Sharp narrow whoosh, crisp attack, delicate metallic edge and short clean decay.'),
    ('blade_hit', .5, 'One katana strike against armor. Sharp cutting snap, compact solid impact and subtle metal texture. No gore or screams.'),
    ('heavy_swing', .7, 'One massive greatsword swing. Broad deep whoosh, heavy air displacement and restrained metallic resonance.'),
    ('heavy_hit', .8, 'One greatsword impact against reinforced armor. Dense low thud, blunt metallic crunch, brief tail and controlled bass. No explosion.'),
    ('guard', .6, 'One weapon strike blocked by a steel blade. Crisp metallic clash, brief bright ring and solid low impact. Controlled high frequencies.'),
    ('dash', .4, 'One extremely fast directional dash. Tight rush of displaced air, subtle futuristic energy accent, immediate attack and quick taper.'),
    ('rope_attach', .5, 'One compact grappling hook latching onto metal. Fast mechanical snap, firm metal click and brief cable tension. One event.'),
    ('rope_release', .35, 'One grappling cable release. Precise latch click followed by a short soft cable recoil. Light and agile.'),
]
VOICE_REFS = [('side01', 0), ('side01', 1), ('side05', 0), ('side09', 0), ('side13', 0), ('side17', 0)]


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def story_line(root, reference):
    quests = read_json(root / STORY)['quests']
    quest = next((q for q in quests if q['id'] == reference['quest']), None)
    index = reference['step']
    if quest is None or type(index) is not int or not 0 <= index < len(quest['steps']):
        raise ValueError('Unknown story reference')
    step = quest['steps'][index]
    if step['kind'] not in ('meet', 'scan', 'deliver', 'choice', 'combat'):
        raise ValueError('This story step does not use the prepared StoryDialogue playback hook')
    return step.get('person') or '서하 · 조사 기록', step['line']


def plan(root=ROOT, include_voice=False):
    assets = []
    for cue, seconds, prompt in SFX:
        assets.append(dict(kind='sfx', id=cue, cue=cue, target_seconds=seconds,
            prompt=prompt + ' Isolated dry game sound, one event only. No music, speech, ambience or long reverb.',
            source_file=None, provider='Mirelo', model='sfx-2.0',
            license_url='https://mirelo.ai/terms', source_url='', reviewed=False))
    for quest, index in (VOICE_REFS if include_voice else []):
        reference = dict(quest=quest, step=index)
        speaker, text = story_line(root, reference)
        spoken = text[len(speaker) + 1:].lstrip() if text.startswith(speaker + ':') else text
        assets.append(dict(kind='voice', id=f'voice_{quest}_{index:02}', source_story=reference,
            speaker=speaker, text=text, spoken_text=spoken,
            direction='Original adult Korean voice; natural conversational delivery, clear consonants, restrained emotion. Do not read the speaker label. No music or sound effects.',
            source_file=None, provider='Fish Audio', model='drama-3',
            license_url='https://huggingface.co/fishaudio/drama-3/blob/main/LICENSE',
            source_url='', reviewed=False))
    return dict(schema=1, status='awaiting_audio', assets=assets)


def inspect_wav(path, kind):
    maximum = 5 if kind == 'sfx' else 20 if kind == 'loop' else 45
    if path.stat().st_size > 48000 * maximum * 2 + 65536:
        raise ValueError(f'{path.name}: WAV exceeds size limit')
    with wave.open(str(path), 'rb') as wav:
        rate, frames = wav.getframerate(), wav.getnframes()
        if wav.getnchannels() != 1 or wav.getsampwidth() != 2 or wav.getcomptype() != 'NONE':
            raise ValueError(f'{path.name}: expected uncompressed 16-bit mono WAV')
        if rate not in (44100, 48000) or not .02 <= frames / rate <= maximum:
            raise ValueError(f'{path.name}: expected 44.1/48 kHz and 0.02–{maximum} seconds')
        raw = wav.readframes(frames)
        if len(raw) != frames * 2:
            raise ValueError(f'{path.name}: truncated WAV')
    samples = array('h', raw)
    if sys.byteorder != 'little':
        samples.byteswap()
    peak = max(abs(n) for n in samples)
    if peak < 32:
        raise ValueError(f'{path.name}: silent or inaudibly quiet WAV')
    if sum(abs(n) >= 32760 for n in samples) / frames > .001:
        raise ValueError(f'{path.name}: more than 0.1% clipped samples')
    onset = next((i for i, n in enumerate(samples) if abs(n) > max(32, peak * .01)), 0) / rate
    return dict(seconds=round(frames / rate, 3), sample_rate=rate, peak=round(peak / 32768, 4),
        onset_seconds=round(onset, 3), sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def validate(manifest, root=ROOT, local_preview=False):
    manifest = Path(manifest).resolve()
    pack = read_json(manifest)
    if not isinstance(pack, dict) or pack.get('schema') != 1:
        raise ValueError('Expected an audio pack with schema: 1')
    assets = pack.get('assets')
    if not isinstance(assets, list) or not 1 <= len(assets) <= 128:
        raise ValueError('Pack must contain 1–128 assets; remove unproduced entries for a partial pack')
    allowed_sfx = {item[0] for item in SFX}
    allowed_loops = set()
    catalog = root / 'Documentation/Audio/mirelo-sfx-plan.json'
    if catalog.exists():
        for entry in read_json(catalog)['requests']:
            (allowed_loops if entry['kind'] == 'loop' else allowed_sfx).add(entry['cue'])
    names, voice_keys, cue_forms, validated = set(), set(), {}, []
    for asset in assets:
        if not isinstance(asset, dict):
            raise ValueError('Asset must be an object')
        kind, name = asset.get('kind'), asset.get('id')
        if kind not in ('sfx', 'loop', 'voice') or not isinstance(name, str) or not NAME.fullmatch(name) or name in RESERVED:
            raise ValueError('Invalid kind or clip id; use lowercase letters, numbers and underscores')
        if (kind, name) in names:
            raise ValueError(f'Duplicate clip: {name}')
        names.add((kind, name))
        for field in ('provider', 'model', 'license_url', 'source_url'):
            if not isinstance(asset.get(field), str) or not asset[field].strip():
                raise ValueError(f'{name}: missing {field} provenance')
        preview = local_preview and kind in ('sfx', 'loop') and asset.get('provider') == 'Mirelo' and asset.get('model') == 'sfx-2.0' and asset.get('commercial_use') is False and asset.get('license_plan') == 'Free'
        if local_preview and not preview:
            raise ValueError(f'{name}: local preview accepts only the noncommercial Mirelo Free SFX pack')
        if asset.get('reviewed') is not True and not preview:
            raise ValueError(f'{name}: listen to the clip and set reviewed: true before import')
        if kind in ('sfx', 'loop'):
            cue = asset.get('cue')
            if cue not in (allowed_loops if kind == 'loop' else allowed_sfx):
                raise ValueError(f'{name}: cue is outside the approved production catalog')
            if kind == 'loop' and (name != cue or (asset.get('loop_seam_reviewed') is not True and not preview)):
                raise ValueError(f'{name}: loops require the exact cue name and reviewed seam')
            if name not in [cue] + [f'{cue}_{i}' for i in range(3)]:
                raise ValueError(f'{name}: id must match cue or cue_0..2')
            cue_forms.setdefault(cue, set()).add('single' if name == cue else 'variant')
            if len(cue_forms[cue]) > 1:
                raise ValueError(f'{cue}: use a single clip or variants, not both')
        else:
            reference = asset.get('source_story')
            if not isinstance(reference, dict) or 'quest' not in reference or 'step' not in reference:
                raise ValueError(f'{name}: missing source_story quest/step')
            speaker, text = story_line(root, reference)
            if (asset.get('speaker'), asset.get('text')) != (speaker, text):
                raise ValueError(f'{name}: voice does not match the current story speaker and text')
            key = (speaker, text)
            if key in voice_keys:
                raise ValueError(f'{name}: duplicate speaker/text mapping')
            voice_keys.add(key)
        source_file = asset.get('source_file')
        if not isinstance(source_file, str) or not source_file:
            raise ValueError(f'{name}: missing source_file; plan contains no generated audio')
        source = (manifest.parent / source_file).resolve()
        if Path(source_file).is_absolute() or not source.is_relative_to(manifest.parent):
            raise ValueError(f'{name}: source_file must stay inside the pack folder')
        if source.suffix.lower() != '.wav' or not source.is_file():
            raise ValueError(f'{name}: WAV file does not exist')
        metrics = inspect_wav(source, kind)
        if preview:
            if metrics['peak'] > .801 or (kind == 'sfx' and metrics['onset_seconds'] > .05):
                raise ValueError(f'{name}: preview requires limited peaks and trimmed onset')
            if kind == 'loop':
                with wave.open(str(source), 'rb') as wav:
                    first = int.from_bytes(wav.readframes(1), 'little', signed=True)
                    wav.setpos(wav.getnframes()-1)
                    last = int.from_bytes(wav.readframes(1), 'little', signed=True)
                if abs(first-last) > 4:
                    raise ValueError(f'{name}: preview loop has a discontinuous seam')
        validated.append((asset, source, metrics))
    return pack, validated


def install(manifest, root=ROOT, local_preview=False):
    _, assets = validate(manifest, root, local_preview=local_preview)
    root = root.resolve()
    destination = (root / GENERATED).resolve()
    if not destination.is_relative_to(root):
        raise ValueError('Generated audio destination leaves project root')
    catalog_path = (destination / 'Voice/catalog.json').resolve()
    if not catalog_path.is_relative_to(destination):
        raise ValueError('Voice catalog leaves generated folder')
    catalog = read_json(catalog_path) if catalog_path.exists() else {'lines': []}
    lines = catalog.get('lines')
    if not isinstance(lines, list):
        raise ValueError('Existing voice catalog has no lines array')
    mappings = {(entry['speaker'], entry['text']) for entry in lines}
    pending = []
    for asset, source, _ in assets:
        folder = destination / {'sfx': 'SFX', 'loop': 'Loops', 'voice': 'Voice'}[asset['kind']]
        target = (folder / (asset['id'] + '.wav')).resolve()
        collisions = [p for p in folder.glob(asset['id'] + '.*') if p.suffix.lower() in ('.wav', '.ogg', '.mp3', '.aif', '.aiff', '.flac')]
        if not target.is_relative_to(destination) or target.exists() or collisions:
            raise ValueError(f'Refusing to overwrite or leave generated folder: {target.name}')
        if asset['kind'] == 'sfx':
            cue = asset['cue']
            conflicts = list(folder.glob(cue + '_[012].*')) if asset['id'] == cue else list(folder.glob(cue + '.*'))
            if conflicts:
                raise ValueError(f'{cue}: existing single/variant pack would shadow this clip')
        elif asset['kind'] == 'voice':
            key = (asset['speaker'], asset['text'])
            if key in mappings:
                raise ValueError(f'{asset["id"]}: story mapping already installed')
            mappings.add(key)
            lines.append(dict(speaker=key[0], text=key[1], clip=asset['id']))
        pending.append((source, target))
    # Finish all validation/collision checks before writing any runtime asset.
    receipt = dict(schema=1, installation_mode='noncommercial_local_preview' if local_preview else 'reviewed', assets=[dict(a, **metrics) for a, _, metrics in assets])
    receipt_name = hashlib.sha256(json.dumps(receipt, sort_keys=True).encode()).hexdigest()[:16]
    receipt_path = (root / 'Artifacts/AudioProduction/receipts' / (receipt_name + '.json')).resolve()
    if not receipt_path.is_relative_to(root):
        raise ValueError('Receipt destination leaves project root')
    prior_catalog = catalog_path.read_bytes() if catalog_path.exists() else None
    copied, catalog_changed = [], False
    try:
        receipt_path.parent.mkdir(parents=True, exist_ok=True)
        for source, target in pending:
            target.parent.mkdir(parents=True, exist_ok=True)
            with source.open('rb') as original, target.open('xb') as output:
                copied.append(target)
                shutil.copyfileobj(original, output)
        if any(a['kind'] == 'voice' for a, _, _ in assets):
            catalog_changed = True
            catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        receipt_path.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    except Exception:
        for target in copied:
            target.unlink(missing_ok=True)
        if catalog_changed:
            if prior_catalog is None:
                catalog_path.unlink(missing_ok=True)
            else:
                catalog_path.write_bytes(prior_catalog)
        raise
    return [str(target.relative_to(root)) for _, target in pending]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    generate = commands.add_parser('plan', help='Print an unproduced 8-effect pilot; Fish voice production cancelled')
    generate.add_argument('--output', type=Path)
    for name in ('validate', 'install'):
        command = commands.add_parser(name)
        command.add_argument('manifest', type=Path)
        command.add_argument('--local-preview', action='store_true', help='Install the noncommercial Mirelo Free pack for local playback review without claiming human audition')
    args = parser.parse_args()
    try:
        if args.command == 'plan':
            text = json.dumps(plan(), ensure_ascii=False, indent=2) + '\n'
            if args.output:
                args.output.parent.mkdir(parents=True, exist_ok=True)
                with args.output.open('x', encoding='utf-8') as output:
                    output.write(text)
                print(f'Prepared 8 SFX (voice production disabled): {args.output}')
            else:
                print(text)
        elif args.command == 'validate':
            _, assets = validate(args.manifest, local_preview=args.local_preview)
            for asset, _, metrics in assets:
                print(asset['id'], json.dumps(metrics))
                if asset['kind'] == 'sfx' and metrics['onset_seconds'] > .15:
                    print('  Warning: slow onset; audition the timing against gameplay before import.')
            print(f'Validated {len(assets)} assets. No files changed.')
        else:
            for path in install(args.manifest, local_preview=args.local_preview):
                print('Installed', path)
    except (ValueError, KeyError, TypeError, OSError, wave.Error, EOFError) as error:
        parser.exit(1, f'Audio pack: {error}\n')


if __name__ == '__main__':
    main()

"""Build the reviewable, offline Mirelo SFX 2.0 production queue. Never calls an API."""
import argparse
import collections
import json
import math
from pathlib import Path
import re
import wave

ROOT = Path(__file__).resolve().parents[2]
FOLDERS = ('Quality', 'Firearms', 'CombatDetail', 'Transport')
ALIASES = {'pistol': 'gun_pistol', 'pistol_overdrive': 'gun_pistol'}
PILOT = {'blade_swing', 'blade_hit', 'heavy_swing', 'heavy_hit', 'guard', 'dash',
         'rope_attach', 'rope_release', 'step_tile', 'step_metal', 'gun_pistol', 'urban_crash'}
TRANSPORT_LOOPS = {'airliner','boat','bus','combathelicopter','fighter','motorcycle',
                   'sedan','ship','sportscar','tank','taxi','truck','turbine','underwater'}
EXTRA = {
 'skill': 'One short controlled energy ability activation: a quick rising air pulse, compact resonant shimmer and clean decay. Subtle near-future technology, no musical phrase or voice.',
 'ui_warn': 'One short restrained interface warning: two compact soft electronic pulses with a clear but non-piercing attack. No alarm sequence, music or voice.',
 'gun_pistol': 'One close compact semiautomatic pistol shot: dry ballistic crack, firm low punch, a quiet slide mechanism. No tail; distant reflection is a separate game layer.',
 'gun_police_pistol': 'One service pistol shot: clean sharp report, controlled bass, precise short slide click. Slightly brighter and more disciplined than a civilian pistol. No distant reflection.',
 'gun_gang_pistol': 'One worn compact handgun shot: rough dry snap, modest uneven mechanical rattle, tight body. No distant reflection.',
 'gun_shotgun': 'One pump shotgun shot: broad heavy crack, dense low pressure, short dry muzzle body. No pump or reload, no distant reflection.',
 'gun_rifle': 'One rifle shot: high velocity dry crack, compact forceful midrange body, light action click. No distant reflection.',
 'gun_auto': 'Exactly one automatic rifle shot, not a burst: fast hard transient, restrained low punch, rapid mechanical click. Clear when repeated rapidly. No distant reflection.',
 'blast_pressure': 'One distant heavy explosion pressure wave, rounded low impulse and a diffuse decaying air rumble. No close crack, no debris impacts, controlled sub bass.',
 'impact_concrete': 'One bullet striking concrete: compact stone tick and gritty chip scatter with a short dry decay. No gunshot.',
 'impact_flesh': 'One non-graphic compact impact on a clothed torso, muted body thump and fabric scuff. No voice, gore, crack or gunshot.',
 'impact_glass': 'One bullet impact on glass: sharp brittle tick followed by a small short shower of glass fragments. No gunshot.',
 'impact_metal': 'One bullet impact on steel: precise metallic ping, compact low contact and restrained ringing. No gunshot.',
 'tail_pistol': 'One distant environmental reflection of a pistol report only: soft diffuse slap echo and quick air decay. No direct muzzle crack or mechanical sound.',
 'tail_rifle': 'One distant reflection of a rifle report only: wider diffuse echo scattering between city buildings and fading into air. No direct muzzle crack.',
 'tail_shotgun': 'One distant reflection of a shotgun report only: broad low echo with a smooth decay. No direct muzzle crack or reload.',
 'titan_barrage': 'One gigantic mechanical cannon discharge: huge controlled low punch, metallic chamber resonance, brief decaying air pressure. One shot, no barrage sequence or music.',
 'airliner': 'Steady passenger jet turbine at constant cruise thrust, smooth broad fan wash and restrained low body. Onboard mechanical texture, no announcements or changing engine speed.',
 'boat': 'Steady small motorboat engine and gentle close water wash at fixed speed. Stable pitch, no acceleration, no voices.',
 'bus': 'Steady heavy city bus diesel engine at fixed idle, warm low rhythmic body and light chassis vibration. No acceleration, brakes, horn or voices.',
 'combathelicopter': 'Steady utility helicopter rotor and turbine at fixed flight speed, distinct rhythmic blade pulses and controlled low mechanical body. No flyby, weapons or voices.',
 'fighter': 'Steady military jet engine at constant thrust, powerful controlled turbine roar and smooth air wash. No flyby, missiles or changing speed.',
 'motorcycle': 'Steady motorcycle engine at constant midrange RPM, tight mechanical combustion pulses and restrained exhaust rasp. No rev changes, gear shifts or horn.',
 'sedan': 'Steady modern petrol sedan engine at constant idle, soft low combustion pulses and subtle smooth mechanical texture. No gear shift, horn or rev change.',
 'ship': 'Steady large freight vessel diesel machinery at constant speed, deep restrained mechanical vibration and smooth ventilation texture. No horns, voices or wave impacts.',
 'sportscar': 'Steady sports car engine under constant load at fixed RPM, focused exhaust growl and clean mechanical body. No acceleration sweep, gear change or tire skid.',
 'tank': 'Steady armored tracked vehicle engine and continuous mechanical tread texture at fixed speed. Restrained low weight, no gunfire, acceleration or voices.',
 'taxi': 'Steady compact taxi combustion engine at fixed idle, muted low purr with subtle cabin vibration. No horn, doors or voices.',
 'truck': 'Steady heavy truck diesel engine at fixed RPM, deep rhythmic mechanical pulses and light chassis resonance. No horn, brake hiss or acceleration.',
 'turbine': 'Steady aircraft turbine load layer, smooth high fan whine and restrained low air pressure at constant pitch. No flyby, speed sweep or voices.',
 'underwater': 'Steady submerged water ambience, soft low pressure wash and sparse tiny bubbles. Calm close perspective, no music, breathing, swimming strokes or voices.',
 'waterstroke': 'One human swimming stroke through water: compact hand splash, smooth short water pull and a few droplets. One stroke, no breathing or voices.',
 'cannon': 'One mounted vehicle cannon shot: forceful compact blast, heavy low mechanical punch and short clean chamber decay. No music, voices, reload or repeated shots.',
 'ignition': 'One car engine startup: brief starter crank, engine catches, one small settling vibration, then clean end. No driving away or horn.',
 'bike_start': 'One motorcycle engine startup: quick starter, compact engine catch, restrained initial exhaust pulse, then clean end. No accelerating away.',
 'horn_car': 'One short real passenger car horn, clear restrained dual tone with quick attack and release. No engine, traffic, siren or background.',
 'horn_heavy': 'One short heavy truck air horn, deep dual tone and restrained air body with clean attack and release. No engine, traffic, siren or background.',
 'police_siren': 'Continuous two tone electronic police siren, smoothly alternating high and low tones at a regular fixed cadence. Stationary isolated source, no Doppler, traffic, radio or speech.'
}


def source_prompts(root):
    text = (root/'Documentation/Audio/SFX-PROMPTS.md').read_text(encoding='utf-8-sig')
    result = {}
    for section in re.split(r'(?m)^## ', text):
        name = re.search(r'파일명: `([a-z0-9_]+)\.wav`', section)
        prompt = re.search(r'```text\s*\n(.*?)```', section, re.S)
        if name and prompt:
            result[name[1]] = prompt[1].strip()
    result.update(EXTRA)
    return result


def build(root=ROOT, credits_per_second=10):
    if not math.isfinite(credits_per_second) or credits_per_second <= 0:
        raise ValueError('credits_per_second must be positive')
    sources = collections.defaultdict(list)
    for folder in FOLDERS:
        for path in sorted((root/'Assets/AfterSignal/Resources/Audio'/folder).glob('*.wav')):
            cue = re.sub(r'_[0-2]$', '', path.stem)
            if cue in ALIASES:
                continue
            with wave.open(str(path),'rb') as wav:
                duration = wav.getnframes()/wav.getframerate()
            sources[cue].append({'path':path.relative_to(root).as_posix(), 'seconds':round(duration,3)})
    for cue in ('horn_car','horn_heavy','police_siren'):
        sources[cue] = []
    prompts = source_prompts(root)
    missing = sorted(set(sources)-set(prompts))
    if missing:
        raise ValueError('Missing authored prompts: '+', '.join(missing))
    requests = []
    for cue, original in sorted(sources.items(), key=lambda item:(item[0] not in PILOT,item[0])):
        loop = cue in TRANSPORT_LOOPS or cue.startswith('ambient_') or cue == 'police_siren'
        seconds = 10 if loop else max(.8, min(4.5, max((x['seconds'] for x in original),default=.6)+.25))
        duration_ms = math.ceil(seconds*10)*100
        variants = 1 if loop else 2
        prompt = prompts[cue]
        prompt += (' Maintain constant loudness, timbre and speed throughout; no intro, fade or ending. This is raw material for an externally edited game loop.' if loop else ' Exactly one event in each variant, immediate onset, preserve the full short decay. No second event. Leave a little clean silence after the tail.')
        prompt += ' Cohesive grounded near-future Korean city action game sound design. Clear at modest volume; no exaggerated cinematic bass.'
        body = {'model':'sfx-2.0','duration_ms':duration_ms,'num_variants':variants,
                'input':{'prompt':prompt,'negative_prompt':'music, speech, vocals, clipping, distortion, unrelated sounds'},'output':{'format':'wav'}}
        route = ('requires_loop_override_hook' if loop else 'requires_horn_override_hook' if cue.startswith('horn_') else 'existing_optional_sfx_override')
        requests.append({'cue':cue,'priority':'pilot' if cue in PILOT else 'full_game',
            'kind':'loop' if loop else 'one_shot','existing_sources':original,
            'runtime_route':route,'needs_seam_edit':loop,
            'planned_files':[cue+'.wav'] if variants==1 else [f'{cue}_{i}.wav' for i in range(variants)],
            'request':body,'estimated_credits':math.ceil(duration_ms/1000*variants*credits_per_second),
            'generation_status':'not_generated','reviewed':False})
    def total(rows):
        return {'cues':len(rows),'clips':sum(r['request']['num_variants'] for r in rows),
                'generated_seconds':round(sum(r['request']['duration_ms']/1000*r['request']['num_variants'] for r in rows),1),
                'estimated_credits':sum(r['estimated_credits'] for r in rows)}
    return {'schema':'mirelo-production-plan-v1','status':'awaiting_account_and_spend_authorization',
        'fish_audio_drama_3':'cancelled_by_user','model':'sfx-2.0','generated_assets':0,
        'api_endpoint':'https://api.mirelo.ai/v3/text-to-sfx/generations',
        'estimate':{'basis':'Public list rate, not an authenticated preflight quote; excludes retries and subscription purchase.',
            'rate_date':'2026-10-20','credits_per_second':credits_per_second,
            'source':'https://mirelo.ai/pricing/usage','pilot':total([r for r in requests if r['priority']=='pilot']),
            'full_game_including_pilot':total(requests)},
        'exclusions':{'music':'Keep all six user BGM masters.', 'voice':'No Fish generation or voice replacement.',
            'legacy_aliases':ALIASES,'root_urban_engine_files':'Legacy fallback files remain; current transport engines are covered.'},
        'quality_gates':['Audition each output against its existing cue; reject worse takes.',
            'Trim onset without cutting transients; keep decays, avoid clicks and excessive bass.',
            'Match perceived in-game loudness to the current cue, then compare with BGM playing.',
            'Convert reviewed exports to PCM16 mono 48 kHz; preserve raw output and provenance.',
            'SFX 2.0 has no loop control. Edit and audition seams externally before loop import.',
            'Run actual Unity SFX, mute, pause, spatial and transition playback checks after installation.'],
        'requests':requests}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path)
    parser.add_argument('--credits-per-second',type=float,default=10)
    args=parser.parse_args()
    result=build(credits_per_second=args.credits_per_second)
    text=json.dumps(result,ensure_ascii=False,indent=2)+'\n'
    if args.output:
        args.output.parent.mkdir(parents=True,exist_ok=True)
        with args.output.open('x',encoding='utf-8') as stream:
            stream.write(text)
        print(json.dumps(result['estimate'],ensure_ascii=False,indent=2))
    else:
        print(text)

if __name__=='__main__':
    main()

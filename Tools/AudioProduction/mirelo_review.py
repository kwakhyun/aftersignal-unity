"""Build an offline A/B listening page and honest production evidence. No API requests."""
import html, json, wave
from pathlib import Path
import numpy as np
from mirelo_generate import ROOT, OUT, PLAN, read, save
from mirelo_prepare import load


def main():
    plan=read(PLAN); ledger=read(OUT/'generation-ledger.json'); pack=read(OUT/'prepared/pack.json')
    if len(ledger['jobs'])!=71 or any(j['status']!='succeeded' for j in ledger['jobs'].values()):
        raise ValueError('Complete the approved generation queue first')
    metrics=read(OUT/'preparation-metrics.json')
    by_id={m['id']:m for m in metrics}; cards=[]
    for row in plan['requests']:
        cue=row['cue']; loop=row['kind']=='loop'
        buttons=[]
        for index,name in enumerate(row['planned_files']):
            info=by_id[Path(name).stem]['after']
            label='Mirelo 반복음' if loop else f'Mirelo {index+1}'
            buttons.append(f'<button data-src="prepared/{name}" data-title="{cue} · {label}" data-loop="{str(loop).lower()}">{label} · {info["seconds"]:.2f}s</button>')
        if row['existing_sources']:
            source=row['existing_sources'][-1]['path']
            buttons.append(f'<button class="original" data-src="../../../{source}" data-title="{cue} · 기존 음원" data-loop="{str(loop).lower()}">기존 음원</button>')
        cards.append(f'<article data-search="{cue} {"loop 반복 환경 차량" if loop else "sfx 단발 효과음"}"><small>{"반복음" if loop else "단발 효과음"}</small><h2>{cue}</h2><div class="buttons">'+''.join(buttons)+'</div></article>')
    credits=sum(j['credits'] for j in ledger['jobs'].values())
    page="""<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>AFTERSIGNAL · 효과음 비교</title>
<style>:root{color-scheme:dark;font-family:system-ui,sans-serif;background:#08151c;color:#e4eff0}body{max-width:1120px;margin:0 auto;padding:44px 24px 145px}h1{font-size:36px;letter-spacing:-1px;margin-bottom:12px}p{color:#a6bac2;line-height:1.7}a{color:#8ee5cf}.tag{color:#8ee5cf;letter-spacing:3px;font-size:12px}.stats{display:flex;gap:32px;padding:24px 0}.stats b{display:block;font-size:25px}.stats span{font-size:13px;color:#a6bac2}input{width:100%;box-sizing:border-box;padding:16px;background:#10262f;border:1px solid #29444f;border-radius:8px;color:inherit;font-size:16px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:15px;margin-top:24px}article{background:#10232c;border:1px solid #24404a;border-radius:10px;padding:20px}small{color:#85c1b5}h2{font-size:18px;margin:9px 0 20px}.buttons{display:flex;flex-wrap:wrap;gap:8px}button{background:#9de3ce;color:#10252a;border:0;padding:10px 12px;border-radius:6px;cursor:pointer;font-weight:650}.original{background:#29454f;color:#dbe7eb}button:hover{filter:brightness(1.12)}button:focus-visible{outline:3px solid white;outline-offset:3px}footer{position:fixed;bottom:0;left:0;right:0;background:#122b35f5;border-top:1px solid #36535e;padding:14px max(24px,calc((100vw - 1120px)/2));backdrop-filter:blur(12px)}audio{width:100%;margin-top:8px;height:36px}#now{font-size:14px;color:#b7d7d5}article[hidden]{display:none}</style>
<div class="tag">AFTERSIGNAL / SOUND LAB</div><h1>도시의 소리를 비교하세요.</h1>
<p>동작별 두 가지 변형과 차량·환경 반복음입니다. 기존 음원과 번갈아 재생해 타격감, 길이, 반복 경계를 확인할 수 있습니다.<br>파일 검사와 Unity 재생 검사는 청취 검수를 대신하지 않습니다. 사람의 청취 검수는 아직 기록되지 않았습니다.</p>
<div class="stats"><div><b>71</b><span>효과음 종류</span></div><div><b>123</b><span>새 WAV 파일</span></div><div><b>CREDITS</b><span>무료 크레딧 사용</span></div></div>
<p>효과음 제공: <a href="https://mirelo.ai">Mirelo AI</a> · SFX 2.0 · Free 플랜 비상업용. <a href="https://mirelo.ai/terms">이용 조건</a></p>
<label for="search">효과음 검색</label><input id="search" type="search" placeholder="blade, gun, sedan, 반복…" autocomplete="off"><main>CARDS</main>
<footer><div id="now" role="status">효과음 버튼을 누르면 재생합니다. 반복음은 자동 반복됩니다.</div><audio id="player" controls preload="none"></audio></footer>
<script>const player=document.querySelector('#player'),status=document.querySelector('#now');document.querySelector('#search').addEventListener('input',e=>{const q=e.target.value.toLowerCase();document.querySelectorAll('article').forEach(card=>card.hidden=!card.dataset.search.includes(q));});document.querySelectorAll('button[data-src]').forEach(button=>button.addEventListener('click',()=>{player.pause();player.src=button.dataset.src;player.loop=button.dataset.loop==='true';status.textContent=button.dataset.title+(player.loop?' · 반복 중':'');player.play().catch(()=>status.textContent='파일을 재생할 수 없습니다. 프로젝트 폴더와 음원이 함께 있는지 확인해 주세요.');}));</script></html>"""
    (OUT/'listen.html').write_text(page.replace('CREDITS',str(credits)).replace('CARDS',''.join(cards)),encoding='utf-8')
    examples=['blade_swing_0','blade_hit_0','heavy_swing_0','heavy_hit_0','guard_0','dash_0','rope_attach_0','rope_release_0','step_tile_0','step_metal_0','gun_pistol_0','urban_crash_0','horn_car_0','sedan','police_siren','ambient_town']
    pieces=[]; timeline=[]; time=0
    for name in examples:
        a=load(OUT/'prepared'/(name+'.wav'))
        if len(a)>96000:
            a=a[:96000].copy();a[-2400:]*=np.linspace(1,0,2400)
        timeline.append({'clip':name,'start_seconds':round(time,3),'seconds':round(len(a)/48000,3)})
        pieces.extend([a,np.zeros(12000)]);time+=(len(a)+12000)/48000
    pcm=np.clip(np.rint(np.concatenate(pieces)*32768),-32768,32767).astype('<i2')
    with wave.open(str(OUT/'mirelo-preview.wav'),'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(48000);wav.writeframes(pcm.tobytes())
    save(OUT/'preview-timeline.json',timeline)
    save(OUT/'production-summary.json',{'model':'sfx-2.0','completed_cues':71,'files':123,'oneshots':104,'loops':19,
        'credits_used':credits,'approved_credit_ceiling':5000,'cash_purchase':False,'commercial_use':False,
        'human_listening_reviewed':False,'installation_mode':'noncommercial_local_preview',
        'trimmed_leading_silence_files':sum(m['after']['trimmed_lead_seconds']>.01 for m in metrics),
        'max_peak':max(m['after']['peak'] for m in metrics),'loop_seam_max_delta':max(a['technical_checks']['boundary_delta'] for a in pack['assets'] if a['kind']=='loop')})
    print(json.dumps({'page':str(OUT/'listen.html'),'preview_seconds':round(time,2),'credits':credits}))

if __name__=='__main__':main()

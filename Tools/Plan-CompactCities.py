"""Deterministic infill planning against the authored road and civic footprints."""
import json, math
from pathlib import Path
from PIL import Image, ImageDraw
data=json.loads(Path('Artifacts/CompactCities/survey.json').read_text(encoding='utf-8'))
def rect(x,z,w,d,pad=0): return (x-w/2-pad,z-d/2-pad,x+w/2+pad,z+d/2+pad)
def overlap(a,b): return a[0]<b[2] and a[2]>b[0] and a[1]<b[3] and a[3]>b[1]
def linehits(r,a,b):
    if max(a[0],b[0])<r[0] or min(a[0],b[0])>r[2] or max(a[1],b[1])<r[1] or min(a[1],b[1])>r[3]:return False
    # Liang-Barsky clip, including segments with endpoints outside the footprint.
    lo,hi=0.,1.
    for p,q in [(-(b[0]-a[0]),a[0]-r[0]),(b[0]-a[0],r[2]-a[0]),(-(b[1]-a[1]),a[1]-r[1]),(b[1]-a[1],r[3]-a[1])]:
        if abs(p)<1e-9:
            if q<0:return False
        elif p<0:lo=max(lo,q/p)
        else:hi=min(hi,q/p)
    return lo<=hi
roads=[((a['x'],a['z']),(b['x'],b['z'])) for road in data['roads'] for a,b in zip(road['points'],road['points'][1:])]
fixed=[]
for b in data['buildings']:
    if b['movable']:continue
    n=b['name'];x,z=b['center']['x'],b['center']['z'];w,d=b['size']['x'],b['size']['z']
    if max(w,d)>70 or any(k in n for k in ['스카이레일','Terminal','항공','기지','교정','생활관']):fixed.append(rect(x,z,w,d,12))
fixed.extend([rect(390,0,790,650),rect(1730,610,350,170),rect(1730,-475,750,180),rect(860,625,110,75),rect(995,-185,105,95),rect(880,80,74,66),rect(950,260,45,45)])
lands=[(180,-3190,1020,-2400),(1110,-3190,2040,-2400),(180,-4180,1020,-3260),(1110,-4180,2040,-3260)]
chosen=[dict(id='dawn-lowlands',x=240,z=460,w=280,d=220,city=0,rect=rect(240,460,280,220,8))]
def place(id,w,d,city,preferred=None,roadmargin=16):
    possibilities=[]
    for x in range(35,2161,15):
      for z in range(345 if city==0 else -4120,1080 if city==0 else -2430,15):
        for zz in ([z,z-900] if city==0 else [z]):
          r=rect(x,zz,w,d,8)
          if city==0:
            if r[0]<10 or r[2]>2190 or r[1]<-540 or r[3]>1090:continue
          elif not any(r[0]>l[0] and r[2]<l[2] and r[1]>l[1] and r[3]<l[3] for l in lands):continue
          if any(overlap(r,b) for b in fixed) or any(overlap(r,q['rect']) for q in chosen):continue
          pref=preferred or ((900,150) if city==0 else (1100,-3280))
          cost=math.hypot(x-pref[0],zz-pref[1])
          possibilities.append((cost,x,zz,r))
    for cost,x,z,r in sorted(possibilities):
      if any(linehits(rect(x,z,w,d,roadmargin),a,b) for a,b in roads):continue
      chosen.append(dict(id=id,x=x,z=z,w=w,d=d,city=city,rect=r));print(id,x,z,w,d,flush=True);return
    print('NO LOT',id,w,d,flush=True)

place('apex-circuit',500,290,0,(1750,-30),10)
place('dawn-stadium',245,200,0,(1150,350))
place('aurora-ballpark',260,250,0,(530,500))
for id,w,d,pref in [('lumen-garden',180,150,(950,400)),('pulse-arena',155,150,(1200,400)),('signal-spire',90,100,(830,450)),('prism-cinema',100,100,(1200,-100))]:place(id,w,d,0,pref,8)
for id,w,d in [('nova-wonder',350,260),('nova-fort',300,210),('nova-prison',260,200),('nova-baseball',260,250),('nova-football',245,200),('nova-school',180,155),('nova-medical',170,155),('nova-basketball',155,150),('nova-skyhotel',155,130),('nova-museum',150,130),('nova-cityhall',150,130),('nova-library',130,120),('nova-police',130,110),('nova-fire',140,110),('nova-bank',120,100),('nova-plaza',130,110),('nova-cinema',100,100),('nova-restaurant',90,85),('nova-cafe',90,85),('nova-observatory',85,90)]:place(id,w,d,1,None,10)
Path('Artifacts/CompactCities/layout.json').write_text(json.dumps(chosen,indent=2),encoding='utf-8')
im=Image.new('RGB',(1600,730),'#eeeeee');dr=ImageDraw.Draw(im);scale=.35
for city in range(2):
    def pt(x,z):return (city*800+x*scale,((1100 if city==0 else -2300)-z)*scale)
    for a,b in roads:dr.line([pt(*a),pt(*b)],fill='#90a7a9',width=2)
    for b in fixed:dr.rectangle([pt(b[0],b[3]),pt(b[2],b[1])],outline='#bd5443')
    for q in chosen:
      if q['city']!=city:continue
      r=q['rect'];dr.rectangle([pt(r[0],r[3]),pt(r[2],r[1])],fill='#144855',outline='#21b8b5');dr.text(pt(r[0]+4,r[3]-8),q['id'],fill='white')
im.save('Artifacts/CompactCities/layout.png')

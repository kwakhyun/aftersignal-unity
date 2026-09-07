"""Mechanical matting and fixed-pivot normalization of the authored depth-walk sheet."""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import ast, shutil, json
ROOT=Path(__file__).resolve().parents[1];DOC=ROOT/'Documentation/Residence';ART=ROOT/'Assets/AfterSignal/Resources/Art/Hero/Depth'
name='exec-66114614-1f18-4aa9-af84-51ca8aaaf82e.png';src=DOC/'Sources'/name
if not src.exists():shutil.copy2(Path('C:/Users/82105/.codex/generated_images/01a0732c-239b-7072-99fe-6782f54c0ec8')/name,src)
tree=ast.parse((ROOT/'Tools/Build-ResidenceArt.py').read_text());functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ['components','extent','count']]
exec(compile(ast.Module(body=functions,type_ignores=[]),'component-helpers','exec'))
arr=np.array(Image.open(src).convert('RGB'));rgb=arr.astype(np.int16)
# Connected near-white checkerboard is a background matte. Enclosed white hair remains intact.
candidate=(rgb.min(2)>210)&((rgb.max(2)-rgb.min(2))<28)
background=np.zeros(candidate.shape,dtype=bool)
for group in components(candidate):
    x,y,z,w=extent(group)
    if x==0 or y==0 or z==arr.shape[1] or w==arr.shape[0] or count(group)>1500:
        for yy,a,b in group:background[yy,a:b]=True
groups=[g for g in components(~background) if count(g)>2600]
assert len(groups)==24,[(count(g),extent(g)) for g in groups]
groups.sort(key=lambda g:extent(g)[3]);ordered=[]
for row in range(6):ordered.extend(sorted(groups[row*4:row*4+4],key=lambda g:extent(g)[0]))
preview=Image.new('RGB',(4*288,6*288),(23,35,47));audit=[]
for i,g in enumerate(ordered):
    x,y,z,w=extent(g);out=np.zeros((w-y,z-x,4),dtype=np.uint8)
    for yy,a,b in g:out[yy-y,a-x:b-x,:3]=arr[yy,a:b];out[yy-y,a-x:b-x,3]=255
    yy,xx=np.where(out[:,:,3]>0);foot=xx[yy>=yy.max()-7];anchor=(foot.min()+foot.max())*.5;scale=.49
    tile=Image.fromarray(out).resize((round((z-x)*scale),round((w-y)*scale)),Image.Resampling.NEAREST)
    frame=Image.new('RGBA',(288,288));dx=round(144-anchor*scale);dy=272-tile.height;frame.alpha_composite(tile,(dx,dy))
    weapon=['Katana','Greatsword','Pistol'][i//8];dest=ART/weapon;dest.mkdir(parents=True,exist_ok=True);frame.save(dest/f'depth-{i%8:02}.png')
    preview.paste(frame,((i%4)*288,(i//4)*288),frame);audit.append({'weapon':weapon,'frame':i%8,'view':'front' if i%8<4 else 'back','pivot':[144,272],'source_bounds':[x,y,z,w],'scale':scale})
preview.save(DOC/'Previews/Depth-contact-sheet.png');(DOC/'depth-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('24 depth-walk sprites prepared, 288x288, fixed foot pivot, shared scale .49')

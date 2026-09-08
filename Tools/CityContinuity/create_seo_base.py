"""Create an editable, CC0 MakeHuman anatomical foundation in Blender/MPFB.
The trial is kept outside Unity Resources until the character passes visual review.
"""
import sys, json
from pathlib import Path
import bpy, addon_utils
ROOT=Path(__file__).resolve().parents[2]
import importlib
repo=bpy.context.preferences.extensions.repos.new(name='AfterSignal production tools',module='aftersignal',custom_directory=str(ROOT/'Artifacts/Tools/mpfb2/src'))
addon_utils.enable('bl_ext.aftersignal.mpfb',default_set=True,persistent=False)
HumanService=importlib.import_module('bl_ext.aftersignal.mpfb.services.humanservice').HumanService
TargetService=importlib.import_module('bl_ext.aftersignal.mpfb.services.targetservice').TargetService
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict()
macro.update(gender=0,age=.42,muscle=.36,weight=.36,proportions=.65,height=.6,cupsize=.5,firmness=.7)
macro['race']={'asian':.7,'caucasian':.3,'african':0}
body=HumanService.create_human(mask_helpers=True,detailed_helpers=True,extra_vertex_groups=True,feet_on_ground=True,scale=.1,macro_detail_dict=macro)
body.name='Seoha anatomical foundation'
rig=HumanService.add_builtin_rig(body,'default',import_weights=True)
out=ROOT/'Artifacts/CharacterLab';out.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Seo-Anatomy.blend'))
data={'vertices':len(body.data.vertices),'dimensions':list(body.dimensions),'groups':[g.name for g in body.vertex_groups],'bones':[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local)} for b in rig.data.bones] if rig else []}
(out/'anatomy.json').write_text(json.dumps(data,indent=2))
print('SEO_ANATOMY_READY',len(body.data.vertices))

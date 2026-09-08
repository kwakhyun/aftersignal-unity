"""Editable, non-shipping action/deformation review for the Seoha anatomical rig.

Foot targets are animated in world space: the planted foot stays on the floor
through the support phase. The source trial remains outside Unity Resources.
Run with Blender --background --python Tools/CityContinuity/animate_seo_review.py.
"""
from pathlib import Path
import bpy, math, json
from mathutils import Vector, Quaternion

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Artifacts/CharacterLab/Refined'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Seo-Trial.blend'))
rig=bpy.data.objects['Seoha_Rig'];scene=bpy.context.scene
scene.render.fps=60
rest={b.name:b.matrix_local.to_quaternion() for b in rig.data.bones}
for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'

def rotate(name,xyz):
    bone=rig.pose.bones.get(name)
    if not bone:return
    world=Quaternion((1,0,0),math.radians(xyz[0]))@Quaternion((0,1,0),math.radians(xyz[1]))@Quaternion((0,0,1),math.radians(xyz[2]))
    bone.rotation_quaternion=rest[name].inverted()@world@rest[name]

feet={};solvers=[]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for side in ['L','R']:
    bones=rig.data.edit_bones
    thigh=bones.new('Review thigh.'+side);thigh.head=bones['upperleg01.'+side].head;thigh.tail=bones['lowerleg01.'+side].head;thigh.roll=bones['upperleg01.'+side].roll;thigh.parent=bones['root'];thigh.use_deform=False
    shin=bones.new('Review shin.'+side);shin.head=thigh.tail;shin.tail=bones['foot.'+side].head;shin.roll=bones['lowerleg01.'+side].roll;shin.parent=thigh;shin.use_connect=True;shin.use_deform=False
    for name,control in [('upperleg01',thigh),('upperleg02',thigh),('lowerleg01',shin),('lowerleg02',shin)]:
        source=bones[name+'.'+side];helper=bones.new('Review rest offset / '+name+'.'+side);helper.head=source.head;helper.tail=source.tail;helper.roll=source.roll;helper.parent=control;helper.use_deform=False
bpy.ops.object.mode_set(mode='POSE');bpy.ops.object.mode_set(mode='OBJECT')
rest={b.name:b.matrix_local.to_quaternion() for b in rig.data.bones}
for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'
for side in ['L','R']:
    ankle=rig.data.bones['foot.'+side].head_local
    target=bpy.data.objects.new('Foot contact target '+side,None);scene.collection.objects.link(target);target.location=ankle
    pole=bpy.data.objects.new('Forward knee pole '+side,None);scene.collection.objects.link(pole);pole.location=(ankle.x,-1,.62)
    ik=rig.pose.bones['Review shin.'+side].constraints.new('IK');ik.name='Two-segment foot contact';ik.target=target;ik.pole_target=pole;ik.chain_count=2;ik.use_stretch=False
    ik.pole_angle=0;feet[side]=(target,Vector(ankle));solvers.append(ik)
    for deform,control in [('upperleg01','Review thigh'),('upperleg02','Review thigh'),('lowerleg01','Review shin'),('lowerleg02','Review shin')]:
        c=rig.pose.bones[deform+'.'+side].constraints.new('COPY_ROTATION');c.target=rig;c.subtarget='Review rest offset / '+deform+'.'+side;c.target_space='WORLD';c.owner_space='WORLD';solvers.append(c)
    target.rotation_mode='QUATERNION';target.rotation_quaternion=rest['foot.'+side]
    c=rig.pose.bones['foot.'+side].constraints.new('COPY_ROTATION');c.target=target;c.target_space='WORLD';c.owner_space='WORLD';solvers.append(c)
    # Select the pole convention from the evaluated knee instead of assuming a
    # Blender roll convention for imported anatomical bones.
    rig.pose.bones['root'].location=rest['root'].inverted()@Vector((0,0,-.08))
    angles=[]
    for sample in range(180):
        angle=-math.pi+sample*math.tau/180
        ik.pole_angle=angle;bpy.context.view_layer.update();angles.append((rig.pose.bones['Review shin.'+side].head.y,angle))
    ik.pole_angle=min(angles)[1]

def reset():
    for b in rig.pose.bones:b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
    for side,(target,ankle) in feet.items():target.location=ankle
    for c in solvers:c.influence=0

def curl(hand,angle):
    for finger in range(1,6):
        for joint in range(1,4):rotate(f'finger{finger}-{joint}.{hand}',(-angle,0,0))

def run(t,direction=0):
    for c in solvers:c.influence=1
    phase=t*math.tau
    rotate('spine05',(7,0,math.sin(phase)*3));rotate('spine02',(3,0,-math.sin(phase)*5))
    # Keep a bent-knee suspension without stretching the two-segment solver.
    root=rig.pose.bones['root'];world=Vector((0,0,-.12+.025*math.cos(phase*2)))
    root.location=rest['root'].inverted()@world
    rotation=Quaternion((0,0,1),math.radians(direction))
    for index,side in enumerate(['L','R']):
        f=(t+index*.5)%1;target,ankle=feet[side]
        if f<.60:
            y=-.40+.80*f/.60;z=0
        else:
            u=(f-.60)/.40;s=u*u*(3-2*u);y=.40-.80*s;z=.27*math.sin(u*math.pi)
        delta=rotation@Vector((0,y,z));target.location=ankle+delta
        swing=math.sin(phase+index*math.pi)
        rotate('upperarm01.'+side,(swing*27,(-1 if side=='L' else 1)*6,0));rotate('lowerarm01.'+side,(-52+9*swing,0,0))
        rotate('foot.'+side,(-7*math.sin(f*math.tau),0,0));curl(side,22)

def pose(kind,t):
    phase=t*math.tau;wave=math.sin(math.pi*t)
    if kind.startswith('Run'):
        directions={'RunForward':0,'RunBack':180,'RunLeft':-90,'RunRight':90,'RunForwardLeft':-45,'RunForwardRight':45,'RunBackLeft':-135,'RunBackRight':135}
        run(t,directions[kind]);return
    if kind=='Idle':rotate('spine02',(math.sin(phase)*.7,0,0));rotate('head',(0,0,math.sin(phase)*1.2));return
    if kind in ['Jump','DoubleJump','Land']:
        lift=wave if kind!='Land' else (1-t)**2
        rotate('spine05',(12*lift,0,0))
        for side in ['L','R']:
            rotate('upperleg01.'+side,(-38*lift,0,0));rotate('lowerleg01.'+side,(65*lift,0,0));rotate('upperarm01.'+side,(-70*wave,0,0))
        if kind=='DoubleJump':rotate('root',(360*t,0,0))
        return
    if kind in ['Rope','WallClimb']:
        for i,side in enumerate(['L','R']):
            w=math.sin(phase+i*math.pi);rotate('upperarm01.'+side,(-140+(w*22 if kind=='WallClimb' else 0),0,0));rotate('lowerarm01.'+side,(-22,0,0));rotate('upperleg01.'+side,(-28-w*22,0,0));rotate('lowerleg01.'+side,(45+w*25,0,0));curl(side,42)
        return
    if kind in ['Swim','Dive']:
        rotate('root',(-75 if kind=='Swim' else -115,0,0))
        for i,side in enumerate(['L','R']):
            w=math.sin(phase+i*math.pi);rotate('upperarm01.'+side,(-90+w*65,0,0));rotate('lowerarm01.'+side,(-28,0,0));rotate('upperleg01.'+side,(w*15,0,0));rotate('lowerleg01.'+side,(12+10*w,0,0))
        return
    if kind in ['Drive','Passenger']:
        for side in ['L','R']:
            rotate('upperleg01.'+side,(-80,0,0));rotate('lowerleg01.'+side,(82,0,0));rotate('upperarm01.'+side,(-48 if kind=='Drive' else -12,0,0));rotate('lowerarm01.'+side,(-35,0,0));curl(side,38 if kind=='Drive' else 12)
        rotate('head',(0,0,math.sin(phase)*3));return
    if kind in ['Pistol','Rifle','Reload']:
        recoil=max(0,1-t*7)*8 if kind!='Reload' else math.sin(phase)*15
        rotate('spine02',(-recoil*.3,0,0));rotate('upperarm01.R',(-76-recoil,0,-7));rotate('lowerarm01.R',(-15,0,0));curl('R',36)
        rotate('upperarm01.L',(-63,0,16));rotate('lowerarm01.L',(-35 if kind=='Rifle' else -60,0,0));curl('L',30)
        if kind=='Reload':rotate('upperarm01.L',(-36-35*wave,0,18))
        return
    if kind in ['Slash','HeavySlash','Throw','Guard']:
        arc=-40+105*(t*t*(3-2*t));rotate('spine02',(8,0,arc*.5));rotate('upperarm01.R',(-65-wave*50,0,arc));rotate('lowerarm01.R',(-20-25*(1-wave),0,0));rotate('upperarm01.L',(-42,0,12));rotate('lowerarm01.L',(-50,0,0));curl('R',44);curl('L',32)
        if kind=='Guard':rotate('upperarm01.R',(-100,0,-20));rotate('lowerarm01.R',(-65,0,0))
        return
    if kind in ['Hit','Death']:
        rotate('spine05',(-18*wave,0,10*wave));rotate('head',(-12*wave,0,-8*wave))
        if kind=='Death':rotate('root',(85*t,0,0))

clips=['Idle','RunForward','RunBack','RunLeft','RunRight','RunForwardLeft','RunForwardRight','RunBackLeft','RunBackRight','Jump','DoubleJump','Land','WallClimb','Rope','Swim','Dive','Drive','Passenger','Pistol','Rifle','Reload','Slash','HeavySlash','Throw','Guard','Hit','Death']
summary=[]
for name in clips:
    reset();rig.animation_data_create();action=bpy.data.actions.new('Review / '+name);action.use_fake_user=True;rig.animation_data.action=action
    for target,_ in feet.values():target.animation_data_clear();target.animation_data_create();target.animation_data.action=bpy.data.actions.new(name+' / '+target.name);target.animation_data.action.use_fake_user=True
    for frame in range(1,50,2):
        t=(frame-1)/48;scene.frame_set(frame);reset();pose(name,t)
        for b in rig.pose.bones:b.keyframe_insert('rotation_quaternion',frame=frame);b.keyframe_insert('location',frame=frame)
        for c in solvers:c.keyframe_insert('influence',frame=frame)
        for target,_ in feet.values():target.keyframe_insert('location',frame=frame)
    summary.append({'clip':name,'frames':49,'fps':60,'status':'deformation_review','foot_ik':name.startswith('Run')})

# Activate one representative contact-cycle and keep target actions paired explicitly.
rig.animation_data.action=bpy.data.actions['Review / RunForward']
for target,_ in feet.values():target.animation_data.action=bpy.data.actions['RunForward / '+target.name]
scene.frame_start=1;scene.frame_end=49
scene.render.resolution_x=640;scene.render.resolution_y=880;scene.cycles.samples=16
camera=scene.camera;camera.location=(3,-5,1.16);camera.rotation_euler=(Vector((0,0,.9))-camera.location).to_track_quat('-Z','Y').to_euler()
for frame in [1,13,25,37]:
    scene.frame_set(frame);scene.render.filepath=str(OUT/f'Run-contact-{frame:02}.png');bpy.ops.render.render(write_still=True)
scene.frame_set(1);bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Seo-Actions-Review.blend'))
(OUT/'actions.json').write_text(json.dumps({'shipping':False,'reason':'Face, hair, garment seams and action poses still require art review','clips':summary},indent=2),encoding='utf-8')
print('SEO_ACTION_REVIEW_READY',len(clips))

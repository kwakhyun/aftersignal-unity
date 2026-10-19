"""Reference-inspired portrait pose, solved on the actual preserved arm rig."""
import math,bpy
from mathutils import Vector,Quaternion

def orient(bone,start,end):
    rest=bone.bone
    delta=(rest.tail_local-rest.head_local).normalized().rotation_difference((end-start).normalized())
    mat=(delta@rest.matrix_local.to_quaternion()).to_matrix().to_4x4();mat.translation=start
    bone.matrix=mat;bpy.context.view_layer.update()

def arm(rig,side,target,pole,hand_direction):
    upper=rig.pose.bones['J_Bip_'+side+'_UpperArm'];lower=rig.pose.bones['J_Bip_'+side+'_LowerArm']
    hand=rig.pose.bones['J_Bip_'+side+'_Hand']
    bpy.context.view_layer.update();start=upper.head.copy();target=Vector(target)
    direction=target-start;d=direction.length;direction.normalize()
    a=upper.bone.length;b=lower.bone.length;d=max(abs(a-b)+.0001,min(a+b-.0001,d))
    target=start+direction*d
    along=(a*a-b*b+d*d)/(2*d);height=math.sqrt(max(0,a*a-along*along))
    pp=Vector(pole)-start;pp-=direction*pp.dot(direction);pp.normalize()
    elbow=start+direction*along+pp*height
    orient(upper,start,elbow);orient(lower,elbow,target)
    orient(hand,target,target+Vector(hand_direction).normalized()*hand.bone.length)
    for digit in ['Index','Middle','Ring','Little']:
        for n in [1,2,3]:
            bone=rig.pose.bones.get('J_Bip_'+side+'_'+digit+str(n))
            if bone:bone.rotation_quaternion=Quaternion((1,0,0),math.radians([0,10,15,8][n]))
    bpy.context.view_layer.update()
    print('PORTRAIT_ARM',side,'lengths',a,b,'elbow',list(elbow),'actual',list(lower.head),'target',list(target))
    return (hand.head-target).length

def portrait_arms(rig):
    residuals=[arm(rig,'L',(.185,-.040,1.448),(.36,-.07,1.20),(-.55,.03,.84)),
               arm(rig,'R',(.010,-.145,1.09),(-.36,-.13,1.08),(.91,-.05,-.41))]
    assert max(residuals)<.002,residuals
    return residuals

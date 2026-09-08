"""Original coachwork studies from documented manufacturer design references.
Blender 4.5: +X nose, +Z up. Articulated wheels and material-separated glazing.
"""
import sys, pathlib, math, bpy
from mathutils import Vector
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import create_transport as t

def loft(name,rings,mat):
    vertices=[]
    for x,w,z in rings:
        for yy,zz in [(-.8,.28),(-1,.38),(-1,z*.76),(-.94,z),(-.68,z+.045),(0,z+.055),(.68,z+.045),(.94,z),(1,z*.76),(1,.38),(.8,.28)]:
            vertices.append((x,yy*w,zz))
    n=11;faces=[tuple(range(n-1,-1,-1)),tuple(range((len(rings)-1)*n,len(rings)*n))]
    for r in range(len(rings)-1):
        for j in range(n):faces.append((r*n+j,r*n+(j+1)%n,(r+1)*n+(j+1)%n,(r+1)*n+j))
    body=t.mesh(name,vertices,faces,mat,.035)
    # Cut real wheel wells through the body. Tyres remain independent pivots.
    bpy.context.view_layer.objects.active=body
    for x in [-1.63,1.59]:
        for side in [-1,1]:
            bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.495,depth=.55,location=(x,side*1.1,.43),rotation=(math.pi/2,0,0))
            cutter=bpy.context.object;mod=body.modifiers.new('Open wheel arch','BOOLEAN');mod.object=cutter;mod.operation='DIFFERENCE'
            bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    return body

def car(name,kind):
    t.clear();supercar=kind<3;suv=kind==5;hatch=kind==4;estate=kind==6
    roof=1.29 if supercar else 1.91 if suv else 1.7
    tail=1.11 if suv else .91
    rings=[(-2.5,.79,tail),(-2.32,.98,tail+.03),(-1.65,1.065,1.04 if supercar else 1.09),(-.8,1.02,.88 if supercar else 1.02),(.5,1.01,.85 if supercar else 1.02),(1.6,1.045,.85 if supercar else 1.01),(2.34,.93,.64 if kind==0 else .83),(2.51,.83,.6 if kind==0 else .77)]
    loft('Sculpted coachwork',rings,t.paint)
    rf=.25 if supercar else .45;rr=-.85 if supercar else -1.68 if estate or suv or hatch else -.9
    fw=1.19;rw=-1.65 if supercar else -2.19 if estate or suv or hatch else -1.74
    t.mesh('Panoramic windscreen',[(rf,-.74,roof),(rf,.74,roof),(fw,.88,.97),(fw,-.88,.97)],[(0,1,2,3)],t.glass)
    t.box('Roof pressing',((rf+rr)*.5,0,roof+.035),(rf-rr+.15,1.53,.08),t.paint,.07)
    t.mesh('Rear windscreen',[(rr,.74,roof),(rr,-.74,roof),(rw,-.9,1.0),(rw,.9,1.0)],[(0,1,2,3)],t.glass)
    for s in [-1,1]:
        t.mesh('Side glazing',[(fw,s*.91,.98),(rf,s*.76,roof),(rr,s*.76,roof),(rw,s*.94,1.0)],[(0,1,2,3)],t.glass)
        for a,b in [((fw,s*.91,.96),(rf,s*.76,roof)),((rf,s*.76,roof),(rr,s*.76,roof)),((rr,s*.76,roof),(rw,s*.94,1.0))]:t.rod('Glazing structural rail',a,b,.045,t.paint)
        if not supercar:t.rod('B pillar',(-.38,s*.78,roof),(-.38,s*1.02,1.02),.047,t.black)
        t.rod('Window waist',(-1.4,s*1.025,1.0),(1.2,s*1.025,.98),.018,t.alloy)
        t.box('Flush door handle',(-.45,s*1.036,.92),(.22,.027,.04),t.alloy,.012)
        if not supercar:t.box('Rear door handle',(-1.1,s*1.04,.95),(.19,.027,.04),t.alloy,.01)
        t.rod('Door shut line',(.91,s*1.027,.44),(.91,s*1.027,.96),.006,t.black)
        t.rod('Rear door shut line',(-.94,s*1.027,.38),(-.94,s*1.027,.96),.006,t.black)
        t.box('Side sill',(-.08,s*1.025,.29),(2.13,.13,.105),t.black,.02)
        t.box('Mirror housing',(.75,s*1.16,1.06),(.31,.24,.14),t.paint,.05)
        t.box('Mirror glass',(.60,s*1.16,1.06),(.008,.18,.085),t.alloy,.005)
        for x in [-1.63,1.59]:
            t.wheel(x,s*1.01,.44,.44,.25)
            # Concentric wheel-lip contour follows the opening, not a solid fender box.
            for j in range(16):
                a=j*math.pi/16;b=(j+1)*math.pi/16
                t.rod('Rolled arch lip',(x+math.cos(a)*.505,s*1.058,.44+math.sin(a)*.505),(x+math.cos(b)*.505,s*1.058,.44+math.sin(b)*.505),.015,t.paint)
        t.box('Front intake',(2.45,s*.63,.44),(.07,.48,.22),t.black,.06)
        t.rod('Polished exhaust',(-2.59,s*.73,.35),(-2.31,s*.73,.35),.085,t.alloy)
        t.box('Rear stop lamp',(-2.51,s*.58,.94),(.045,.55,.08),t.red,.028)
        if kind==0:
            for a,b in [((2.42,s*.38,.72),(2.3,s*.7,.8)),((2.3,s*.7,.8),(2.07,s*.87,.85)),((2.3,s*.7,.8),(2.38,s*.88,.59))]:t.rod('Y-shaped running light',a,b,.02,t.lamp)
            t.wing('Angular intake shoulder',[(-.8,s*1.05,.88),(-1.25,s*1.08,1.0),(-1.1,s*1.09,.42),(-.64,s*1.05,.48)],.025,t.black)
            t.wing('Flying rear blade',[(-.6,s*.71,1.46),(-1.65,s*.82,1.01),(-1.86,s*.96,.97),(-.72,s*.82,1.43)],.04,t.paint)
        elif kind==1:
            t.ellipsoid('Rounded rear haunch',(-1.53,s*.83,.97),(.54,.21,.12),t.paint)
            t.ellipsoid('Air intake',(-.98,s*1.04,.72),(.34,.032,.16),t.black)
            t.rod('Slender LED eyebrow',(2.34,s*.35,.85),(2.18,s*.84,.92),.021,t.lamp)
            t.rod('Rear aero bridge',(-.9,s*.76,1.44),(-1.76,s*.9,1.0),.07,t.paint)
        elif kind==2:
            for j in range(22):
                a=-math.pi/2+j*math.pi/22;b=-math.pi/2+(j+1)*math.pi/22
                t.rod('C-line sculpted trim',(-.78-math.cos(a)*.53,s*1.065,.83+math.sin(a)*.48),(-.78-math.cos(b)*.53,s*1.065,.83+math.sin(b)*.48),.031,t.alloy)
            for n in range(4):t.box('Quad optic',(2.44,s*(.37+n*.12),.84),(.06,.085,.1),t.lamp,.022)
        else:
            t.box('Headlight lens',(2.44,s*.6,.86),(.075,.52,.11),t.lamp,.025)
            t.box('Lower driving lamp',(2.46,s*.8,.47),(.06,.22,.05),t.lamp,.012)
    if kind==2:
        for j in range(25):
            a=math.pi*j/24;b=math.pi*(j+1)/24
            t.rod('Horseshoe grille surround',(2.57,math.cos(a)*.27,.44+math.sin(a)*.39),(2.57,math.cos(b)*.27,.44+math.sin(b)*.39),.022,t.alloy)
        t.box('Central dark grille',(2.51,0,.54),(.06,.5,.37),t.black,.07)
    else:
        t.box('Radiator opening',(2.53,0,.5),(.055,.78,.22),t.black,.04)
        for n in range(7):t.box('Grille fins',(2.564,-.34+n*.113,.5),(.018,.016,.15),t.alloy,.003)
    for n in range(6):t.box('Rear diffuser blade',(-2.34,-.78+n*.31,.27),(.38,.025,.16),t.black,.01)
    if supercar:
        for n in range(7):t.box('Engine cover louver',(-1.59-n*.07,0,1.04),(.035,1.12,.035),t.black,.008)
        t.box('Integrated rear spoiler',(-2.31,0,1.08),(.23,1.88,.055),t.paint,.025)
    if suv:
        t.box('Spare wheel carrier',(-2.6,0,1.1),(.25,.77,.78),t.black,.22)
        for s in [-1,1]:t.rod('Roof luggage rail',(-1.68,s*.64,roof+.11),(.28,s*.64,roof+.11),.028,t.alloy)
    t.seats(1 if supercar else 2,.3,.95,.31 if supercar else .72,.43)
    t.box('Dashboard',(.82,0,.96),(.32,1.6,.12),t.black,.04)
    t.box('Console screen',(.69,0,1.04),(.04,.35,.15),t.black,.025)
    t.export(name)

def bike(name,kind):
    export=t.export;t.export=lambda _:None;t.bike();t.export=export
    if kind==0:
        for s in [-1,1]:
            t.wing('Full racing fairing',[(.75,s*.25,1.14),(-.12,s*.36,.87),(-.43,s*.3,.4),(.58,s*.31,.42)],.028,t.paint)
            t.wing('Front aerodynamic winglet',[(.76,s*.27,1.1),(.49,s*.33,1.1),(.5,s*.58,1.08),(.78,s*.56,1.11)],.025,t.black)
            t.rod('Fairing vent',(.23,s*.366,.8),(-.18,s*.366,.68),.019,t.black)
    elif kind==1:
        for s in [-1,1]:
            t.box('Expedition pannier',(-.73,s*.47,.92),(.57,.31,.49),t.alloy,.05)
            t.rod('Engine crash bar',(.36,s*.28,.5),(.36,s*.42,.86),.028,t.black)
            t.rod('Crash bar return',(.36,s*.42,.86),(-.21,s*.39,.91),.028,t.black)
        t.box('High front beak',(.86,0,1.08),(.47,.4,.065),t.paint,.04)
        t.mesh('Tall touring screen',[(.66,-.22,1.18),(.66,.22,1.18),(.58,.2,1.75),(.58,-.2,1.75)],[(0,1,2,3)],t.glass)
    else:
        for obj in list(bpy.context.scene.objects):
            if obj.name.startswith(('Front fairing','Windshield','Headlight')):bpy.data.objects.remove(obj,do_unlink=True)
        t.rod('Round headlamp shell',(.69,0,1.08),(.86,0,1.08),.18,t.alloy)
        t.ellipsoid('Round headlamp lens',(.87,0,1.08),(.025,.155,.155),t.lamp)
        t.box('Long stitched leather saddle',(-.52,0,.91),(.83,.39,.12),t.black,.1)
        for n in range(9):t.rod('Saddle stitching',(-.87+n*.078,-.18,.977),(-.87+n*.078,.18,.977),.006,t.alloy)
    t.export(name)

if __name__=='__main__':
    for i,name in enumerate(['VelaR','RosaM','ChimeraC','MetroSedan','CivicHatch','TrailSUV','EstateTourer']):car(name,i)
    for i,name in enumerate(['ApexRR','NomadGS','ClassicTwin']):bike(name,i)

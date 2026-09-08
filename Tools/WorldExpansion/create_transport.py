"""Original articulated transport meshes, metres; Blender X forward, Z up."""
import bpy, math, pathlib, bmesh
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/AfterSignal/Resources/WorldAssets'
def clear():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def material(n,c,metal=0):
    m=bpy.data.materials.get(n) or bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.26
    return m
paint=material('BodyPaint',(.19,.32,.38),.7);black=material('TrimRubber',(.018,.025,.03));alloy=material('BrushedAlloy',(.6,.66,.68),.85)
glass=material('Glazing',(.15,.32,.38));lamp=material('Headlamp',(.7,.95,1));red=material('Taillamp',(.92,.03,.02))
def finish(o,n,m,b=0):
    o.name=n;o.data.materials.append(m)
    if b:
        mod=o.modifiers.new('Rounded manufactured edges','BEVEL');mod.width=b;mod.segments=3
    for p in o.data.polygons:p.use_smooth=True
    return o
def box(n,at,size,m,b=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,b)
def ellipsoid(n,at,size,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=at);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m)
def rod(n,a,b,r,m):
    d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,n,m,.008)
def mesh(n,v,f,m,bevel=.01):
    d=bpy.data.meshes.new(n);d.from_pydata(v,[],f);d.update();o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);return finish(o,n,m,bevel)
def pivot(n,at):
    bpy.ops.object.empty_add(location=at);o=bpy.context.object;o.name=n;return o
def parent(o,p):o.parent=p;o.matrix_parent_inverse=p.matrix_world.inverted();return o
def wing(n,points,depth,m):
    v=list(points)+[(x,y,z-depth) for x,y,z in points];return mesh(n,v,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],m)
def wheel(x,y,z,r=.44,width=.2):
    p=pivot('Wheel assembly',(x,y,z))
    bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=10,location=(x,y,z),major_radius=r*.75,minor_radius=r*.25,rotation=(math.pi/2,0,0));parent(finish(bpy.context.object,'Tyre',black),p)
    for s in [-1,1]:
        yy=y+s*width*.5
        for i in range(9):
            a=i*math.tau/9;parent(rod('Machined spoke',(x,yy,z),(x+math.cos(a)*r*.7,yy,z+math.sin(a)*r*.7),r*.045,alloy),p)
    parent(rod('Wheel axle',(x,y-width*.6,z),(x,y+width*.6,z),r*.18,alloy),p)
def seats(rows,start,step,height,width=1.0):
    for i in range(rows):
        for s in [-1,1]:
            x=start-i*step;y=s*width
            box('Seat cushion',(x,y,height),(.66,.56,.15),black,.07)
            o=box('Seat back',(x-.33,y,height+.34),(.14,.6,.7),black,.07);o.rotation_euler.y=-.13
            box('Seat headrest',(x-.37,y,height+.81),(.15,.35,.28),black,.07)
def export(name):
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':continue
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    groups={}
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':continue
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
        groups.setdefault((o.parent.name if o.parent else '',o.data.materials[0].name),[]).append(o)
    for (pn,mn),objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=mn
    d=OUT/name;d.mkdir(parents=True,exist_ok=True)
    bpy.context.preferences.filepaths.save_version=0
    source=ROOT/'Documentation/Mobility/Models';source.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source/(name+'.blend')))
    bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(d/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    print('TRANSPORT EXPORTED',name,flush=True)
def sports():
    clear()
    # Low wedge, long bonnet, sculpted arches and distinctly upright tail.
    rings=[(-2.55,.65,.8),(-2.3,.95,.98),(-1.6,1.06,1.02),(-.7,1.02,.88),(.7,1.01,.88),(1.6,1.08,.79),(2.5,.88,.57)]
    v=[]
    for x,w,z in rings:
        for y,h in [(-.85,.25),(-1,.45),(-1,.8),(-.8,1),(.8,1),(1,.8),(1,.45),(.85,.25)]:v.append((x,y*w,h*z))
    f=[tuple(range(7,-1,-1)),tuple(range(48,56))]
    for i in range(6):
        for j in range(8):f.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
    mesh('Sculpted wedge',v,f,paint,.07)
    mesh('Windshield',[(.4,-.73,1.53),(.4,.73,1.53),(1.3,.89,.87),(1.3,-.89,.87)],[(0,1,2,3)],glass)
    box('Floating roof',(-.43,0,1.56),(1.6,1.52,.08),black,.09)
    mesh('Rear screen',[(-1.2,-.7,1.51),(-1.8,-.83,1),(-1.8,.83,1),(-1.2,.7,1.51)],[(0,1,2,3)],glass)
    for s in [-1,1]:
        mesh('Side glass',[(.4,s*.75,1.51),(-1.2,s*.73,1.51),(-1.72,s*.9,.99),(1.28,s*.91,.9)],[(0,1,2,3)],glass)
        rod('A pillar',(.4,s*.75,1.55),(1.3,s*.91,.86),.044,paint);rod('B pillar',(-.4,s*.76,1.53),(-.4,s*1,.91),.035,black)
        wing('Sculpted door skin',[(1.15,s*.995,.82),(-1.2,s*1.03,.89),(-1.35,s*.97,.36),(1.14,s*.95,.33)],.08,paint)
        rod('Window sill',(-1.6,s*.96,.94),(1.18,s*.96,.87),.025,black)
        rod('Rear sail pillar',(-1.17,s*.73,1.51),(-1.79,s*.86,.96),.075,paint)
        rod('Roof side rail',(-1.18,s*.74,1.55),(.39,s*.75,1.55),.035,paint)
        box('Flush door handle',(-.85,s*1.04,.84),(.23,.018,.045),alloy,.012)
        box('Side skirt',(-.04,s*1.04,.27),(2.4,.1,.09),black,.025)
        box('Side intake',(-1.04,s*1.045,.57),(.6,.025,.32),black,.07)
        box('Mirror',(.7,s*1.16,1.04),(.28,.27,.15),black,.04)
        rod('Door seam',(.88,s*1.01,.3),(.88,s*1.01,.8),.007,black)
        wheel(1.6,s*1.02,.44);wheel(-1.65,s*1.02,.44)
        box('Sharp white headlamp',(2.39,s*.65,.64),(.17,.38,.07),lamp,.025)
        box('Rear red light',(-2.53,s*.52,.82),(.035,.73,.085),red,.025)
        box('Front splitter',(2.42,s*.64,.25),(.32,.62,.08),black)
        rod('Exhaust',(-2.64,s*.67,.35),(-2.3,s*.67,.35),.105,alloy)
        box('Spoiler support',(-2.04,s*.76,1.16),(.08,.09,.4),black)
    box('Rear wing',(-2.1,0,1.38),(.4,2.05,.085),black)
    box('Wide front grille',(2.48,0,.43),(.09,1.25,.23),black,.04)
    for i in range(9):box('Front grille slat',(2.54,-.55+i*.137,.43),(.03,.035,.18),alloy,.005)
    seats(2,.25,.9,.57,.43);box('Dashboard',(.94,0,.95),(.4,1.52,.15),black,.04)
    export('SportsCar')
def bike():
    clear();wheel(.92,0,.42,.42,.22);wheel(-.95,0,.42,.42,.28)
    for s in [-1,1]:
        rod('Front fork',(.95,s*.14,.43),(.63,s*.18,1.22),.055,alloy)
        rod('Swingarm',(-.95,s*.17,.43),(-.05,s*.19,.57),.062,alloy)
        rod('Triangular frame',(-.6,s*.17,.82),(.35,s*.17,.95),.045,alloy)
        rod('Frame cradle',(-.5,s*.17,.8),(.1,s*.17,.36),.045,alloy)
        rod('Handlebar',(.6,0,1.27),(.52,s*.42,1.23),.028,alloy)
        rod('Grip',(.52,s*.32,1.23),(.52,s*.48,1.23),.035,black)
        rod('Footrest',(-.18,0,.48),(-.18,s*.34,.48),.035,alloy)
        rod('Exhaust',(-1.04,s*.29,.5),(-.34,s*.29,.4),.085,alloy)
        rod('Mirror stalk',(.58,s*.31,1.26),(.7,s*.41,1.55),.018,black)
        ellipsoid('Mirror',(.7,s*.43,1.55),(.055,.105,.07),alloy)
    ellipsoid('Fuel tank',(.1,0,.95),(.39,.24,.23),paint)
    box('Rider saddle',(-.44,0,.88),(.65,.37,.13),black,.1)
    ellipsoid('Engine casing',(-.1,0,.53),(.33,.21,.24),black)
    for i in range(7):box('Engine cooling fin',(-.1,0,.43+i*.045),(.48,.46,.014),alloy,.005)
    ellipsoid('Front fairing',(.72,0,1.07),(.22,.3,.26),paint)
    box('Headlight',(.94,0,1.1),(.06,.4,.14),lamp,.04)
    mesh('Windshield',[(.63,-.22,1.2),(.63,.22,1.2),(.49,.15,1.52),(.49,-.15,1.52)],[(0,1,2,3)],glass)
    box('Tail light',(-.95,0,.88),(.05,.27,.08),red)
    export('Motorcycle')
def aircraft(name,fighter=False):
    clear();length=15 if not fighter else 7.5;radius=1.85 if not fighter else .8;center=3 if not fighter else 1.65
    rings=[(-1,.05),(-.85,.45),(-.65,.9),(-.45,1),(.45,1),(.7,.87),(.85,.57),(.96,.22),(1,.025)]
    # Narrow individual cabin apertures, with a continuous opaque fuselage between them.
    if not fighter:
        window_centers=[-8.1+i*.95 for i in range(18)]
        xs=sorted(set([x*length for x,r in rings]+[x+d for x in window_centers for d in [-.27,.27]]+[9.2,11.8]))
        def radius_at(x):
            for i in range(len(rings)-1):
                a,ra=rings[i];b,rb=rings[i+1]
                if a*length<=x<=b*length:return ra+(rb-ra)*(x/length-a)/(b-a)
            return .025
        rings=[(x/length,radius_at(x)) for x in xs]
    v=[];n=32
    for x,r in rings:
        for a in range(n):t=a*math.tau/n;v.append((x*length,math.cos(t)*r*radius,center+math.sin(t)*r*radius))
    body=[];windows=[]
    for i in range(len(rings)-1):
        for j in range(n):
            face=(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
            mid=(rings[i][0]+rings[i+1][0])*length*.5
            cabin=not fighter and any(abs(mid-x)<.27 for x in window_centers) and j in [0,1,14,15]
            cockpit=not fighter and 9.2<mid<11.8 and j in [1,2,13,14]
            (windows if cabin or cockpit else body).append(face)
    mesh('Aerodynamic fuselage',v,body,paint);mesh('Cabin glazing',v,windows,glass)
    for s in [-1,1]:
        wing('Swept wing',[(3,s*.6,center-.45),(-3,s*(6 if fighter else 14),center-.4),(-5,s*(6 if fighter else 14),center-.5),(-5,s*.6,center-.5)],.18,paint)
        wing('Tailplane',[(-length*.65,s*.3,center),(-length*.87,s*(3 if fighter else 5),center+.8),(-length*.99,s*(3 if fighter else 5),center+.8),(-length*.97,s*.3,center)],.1,paint)
        rod('Landing strut',(length*.45,s*.6,.5),(length*.45,s*.6,center-.4),.1,alloy);wheel(length*.45,s*.6,.4,.37)
        wheel(-length*.24,s*(1.2 if fighter else 2.4),.4,.4)
        if not fighter:
            ellipsoid('Turbofan nacelle',(-1,s*5,2),(2.4,.95,.95),paint)
            rod('Engine black inlet',(.94,s*5,2),(1.27,s*5,2),.77,black)
            for j in range(18):
                a=j*math.tau/18;rod('Fan blade',(1.29,s*5,2),(1.29,s*5+math.cos(a)*.71,2+math.sin(a)*.71),.024,alloy)
            rod('Cabin belt trim',(-8.5,s*1.79,2.63),(8.5,s*1.79,2.63),.07,black)
        else:
            rod('Jet exhaust',(-7.3,s*.35,1.65),(-6.8,s*.35,1.65),.42,black)
            rod('Missile',(-3,s*4.7,1.05),(0,s*4.7,1.05),.13,alloy)
        box('Wing tip navigation',(-4,s*(6 if fighter else 14),center-.28),(.3,.12,.12),red if s<0 else lamp)
    wing('Vertical stabilizer',[(-length*.58,-.08,center),(-length*.87,-.08,center+length*.35),(-length*.99,-.08,center+length*.34),(-length*.96,-.08,center)],.14,paint)
    if fighter:ellipsoid('Cockpit canopy',(3,0,2.15),(1.8,.62,.72),glass)
    else:seats(6,6,2.4,1.7,1.1);seats(1,10,1,2.2,.7)
    export(name)
def boat():
    clear();v=[];rings=[(-8,1.8),(-7,2.3),(-3,2.55),(3,2.45),(6,1.65),(8,.06)]
    for x,w in rings:
        for y,z in [(-1,1),(-.93,-.15),(-.4,-.9),(.4,-.9),(.93,-.15),(1,1)]:v.append((x,y*w,z))
    f=[tuple(range(5,-1,-1)),tuple(range(30,36))]
    for i in range(5):
        for j in range(6):f.append((i*6+j,i*6+(j+1)%6,(i+1)*6+(j+1)%6,(i+1)*6+j))
    mesh('Deep V passenger hull',v,f,paint,.08);box('Passenger deck',(-1,0,1.08),(12,4.45,.18),alloy,.06)
    box('Wheelhouse sill',(3.7,0,2),(4.6,3.4,1.5),paint,.18)
    box('Windshield',(5.91,0,3.22),(.055,3.15,1.15),glass)
    for s in [-1,1]:
        box('Side wheelhouse glazing',(3.7,s*1.72,3.2),(4.4,.055,1.15),glass)
        for i in range(12):rod('Deck railing post',(-7+i*1.2,s*2.3,1.1),(-7+i*1.2,s*2.3,2),.03,alloy)
        rod('Deck safety rail',(-7,s*2.3,2),(6,s*2.3,2),.035,alloy)
        for i in range(4):ellipsoid('Dock fender',(-6+i*3,s*2.5,.75),(.35,.19,.6),black)
    box('Wheelhouse roof',(3.7,0,3.93),(5,3.85,.18),paint,.09)
    box('Passenger canopy',(-3.1,0,3.4),(6.8,4,.13),paint,.08)
    for s in [-1,1]:
        for x in [-6,0]:rod('Canopy post',(x,s*1.9,1.1),(x,s*1.9,3.4),.05,alloy)
    seats(6,0,1.1,1.42,1.2);seats(1,3.5,1,2.36,.7)
    rod('Radar mast',(4,0,3.95),(4,0,5.2),.07,alloy);box('Marine radar',(4,0,5.2),(.5,1.65,.16),alloy)
    export('Boat')
def helicopter():
    clear();ellipsoid('Armored fuselage',(0,0,1.8),(3.65,1.03,1.3),paint)
    ellipsoid('Cockpit glass',(2.65,0,2.03),(1.46,.93,1.04),glass)
    rod('Cockpit divider',(3.8,0,1.5),(2.56,0,3.02),.055,black)
    rod('Tail boom',(-2.5,0,1.85),(-7.2,0,2.65),.28,paint)
    wing('Tail fin',[(-6.3,0,2.5),(-7.2,0,4.0),(-7.8,0,4),(-7.6,0,2.4)],.13,paint)
    for s in [-1,1]:
        rod('Landing skid',(-2.5,s*1.4,.27),(3,s*1.4,.27),.085,alloy)
        for x in [-1.5,1.8]:rod('Skid strut',(x,s*.7,1.4),(x,s*1.4,.27),.075,alloy)
        box('Side glass',(-.3,s*1.03,2), (2,.06,1.2),glass,.06)
        wing('Weapon stub wing',[(.2,s*.8,1.35),(-.8,s*2.8,1.4),(-2,s*2.8,1.4),(-2,s*.8,1.35)],.15,paint)
        for j in range(3):rod('Rocket tube',(-1.7,s*(1.6+j*.32),1.08),(.9,s*(1.6+j*.32),1.08),.14,alloy)
    main=pivot('Main rotor',(0,0,3.52));parent(rod('Rotor shaft',(0,0,2.8),(0,0,3.6),.13,alloy),main)
    for i in range(4):
        a=i*math.pi/2;c,s=math.cos(a),math.sin(a)
        points=[(.3,-.1,3.57),(6.8,-.35,3.57),(6.7,.12,3.57),(.3,.13,3.57)]
        parent(wing('Rotor blade',[(x*c-y*s,x*s+y*c,z) for x,y,z in points],.045,black),main)
    tail=pivot('Tail rotor',(-7.2,-.3,2.8))
    for i in range(4):
        a=i*math.pi/2;parent(rod('Tail rotor blade',(-7.2,-.4,2.8),(-7.2+math.cos(a)*1.1,-.4,2.8+math.sin(a)*1.1),.045,black),tail)
    export('CombatHelicopter')
def tank():
    clear();box('Armored hull',(0,0,1.03),(7.4,3.35,1.35),paint,.25)
    wing('Sloped frontal glacis',[(3.9,-1.65,.7),(3.9,1.65,.7),(2.9,1.65,1.72),(2.9,-1.65,1.72)],.16,paint)
    for s in [-1,1]:
        for i in range(7):wheel(-2.8+i*.93,s*1.65,.58,.46,.35)
        for i in range(64):
            t=i/64*math.tau;x=3.36*math.cos(t);z=.62+.55*math.sin(t)
            o=box('Track shoe',(x,s*1.68,z),(.22,.62,.13),black,.012);o.rotation_euler.y=-math.atan2(.55*math.cos(t),-3.36*math.sin(t))
        box('Side armor skirt',(0,s*1.99,1.34),(7.1,.18,.65),paint,.055)
    turret=pivot('Turret assembly',(-.4,0,1.85));parent(ellipsoid('Turret armor',(-.4,0,1.98),(2,1.3,.67),paint),turret)
    parent(rod('Main cannon',(1,0,2.12),(6.5,0,2.12),.16,alloy),turret)
    parent(box('Muzzle brake',(6.3,0,2.12),(.5,.38,.35),black),turret)
    for s in [-1,1]:
        parent(box('Commander hatch',(-.7,s*.6,2.6),(.6,.6,.12),black,.09),turret)
        box('Front headlamp',(3.71,s*1.25,1.15),(.1,.35,.25),lamp)
        box('Rear lamp',(-3.71,s*1.25,1.15),(.1,.3,.15),red)
    rod('Radio antenna',(-1.7,.8,2.4),(-1.7,.8,4.1),.016,black)
    export('Tank')
for fn in [sports,bike,lambda:aircraft('Airliner'),lambda:aircraft('Fighter',True),boat,helicopter,tank]:fn()

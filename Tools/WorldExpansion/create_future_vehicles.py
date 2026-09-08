"""Manufactured cyberpunk coachwork built on the existing seat and wheel hardpoints."""
import sys,pathlib,math,bpy
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import create_transport as t
ROOT=t.ROOT
for base,name in [('DetailedSedan','FutureSedan'),('SportsCar','FutureSportsCar'),('Motorcycle','FutureMotorcycle')]:
 file=ROOT/('Documentation/WorldExpansion' if base=='DetailedSedan' else 'Documentation/Mobility/Models')/(base+'.blend')
 bpy.ops.wm.open_mainfile(filepath=str(file))
 t.paint=t.material('BodyPaintFuture',(.19,.3,.35),.72);t.black=t.material('TrimRubberFuture',(.018,.026,.03));t.alloy=t.material('BrushedAlloyFuture',(.45,.57,.63),.9);t.lamp=t.material('HeadlampFuture',(.35,.9,.87));t.red=t.material('TaillampFuture',(.9,.025,.04))
 if base=='Motorcycle':
  for s in [-1,1]:
   t.wing('Angular battery fairing',[(.52,s*.22,.89),(-.22,s*.33,.9),(-.6,s*.32,.44),(.18,s*.29,.37)],.025,t.paint)
   t.rod('Power core luminous spine',(.25,s*.35,.58),(-.35,s*.35,.61),.017,t.lamp)
   t.box('Radial motor casing',(-.95,s*.2,.42),(.34,.055,.34),t.alloy,.08)
  t.box('Forward sensor',(1,0,.84),(.12,.27,.08),t.black,.03)
 else:
  for s in [-1,1]:
   t.wing('Faceted front shoulder',[(2.25,s*.74,.78),(1.7,s*1.03,1.01),(.85,s*1.04,.87),(1.42,s*.91,.78)],.04,t.paint)
   t.wing('Aerodynamic rear haunch',[(-.75,s*1.03,.87),(-1.85,s*1.04,1.01),(-2.35,s*.75,.84),(-1.56,s*.89,.74)],.045,t.paint)
   t.rod('Continuous segmented DRL',(2.52,s*.07,.72),(2.36,s*.8,.8),.025,t.lamp)
   t.rod('Illuminated side crease',(1.14,s*1.058,.38),(-1.14,s*1.06,.4),.012,t.lamp)
   for i in range(5):
    o=t.box('Directed side heat vent',(-1.0+i*.075,s*1.07,.63),(.035,.02,.21),t.black,.005);o.rotation_euler.y=-.38
   for x in [-1.6,1.6]:
    # Turbine covers are parented to the wheel assembly and spin with the tyre.
    piv=min([o for o in bpy.context.scene.objects if o.type=='EMPTY' and o.name.startswith('Wheel')],key=lambda o:(o.location.x-x)**2+(o.location.y-s)**2,default=None)
    if piv:
     for i in range(7):
      a=i*math.tau/7;part=t.rod('Turbine aero spoke',(x+math.cos(a)*.12,s*1.13,.44+math.sin(a)*.12),(x+math.cos(a+.23)*.31,s*1.13,.44+math.sin(a+.23)*.31),.035,t.alloy);t.parent(part,piv)
  t.box('Roof lidar',(-.4,0,1.69),(.38,.5,.06),t.black,.06)
 t.export(name)
for truck in [False,True]:
 t.clear();length=3.9 if truck else 4.6;front=length
 t.paint=t.material('BodyPaintFuture',(.09,.3,.37),.65);t.black=t.material('TrimRubberFuture',(.018,.026,.03));t.alloy=t.material('BrushedAlloyFuture',(.45,.57,.63),.9);t.glass=t.material('GlazingFuture',(.12,.27,.31));t.lamp=t.material('HeadlampFuture',(.35,.9,.87));t.red=t.material('TaillampFuture',(.9,.025,.04))
 # Chamfered utility monocoque, a raked front screen and raised floating roof.
 t.box('Low floor chassis',(0,0,.67),(length*2,2.4,.48),t.paint,.17)
 t.box('Floating roof',(0,0,3.13),(length*2-.16,2.32,.18),t.paint,.17)
 for s in [-1,1]:
  t.box('Side rocker',(0,s*1.19,1.24),(length*2-.3,.12,.85),t.paint,.08)
  t.mesh('Continuous passenger glazing',[(-length+.18,s*1.17,1.68),(length-.48,s*1.17,1.68),(length-.88,s*1.09,3.03),(-length+.18,s*1.09,3.03)],[(0,1,2,3)],t.glass)
  for i in range(7):t.rod('Monocoque glazing post',(-length+.3+i*(length*2-.9)/6,s*1.18,1.66),(-length+.3+i*(length*2-.9)/6,s*1.1,3.07),.035,t.alloy)
  t.rod('Low amber side light',(-length+.4,s*1.26,.82),(length-.4,s*1.26,.82),.02,t.lamp)
  t.wheel(length-1.2,s*1.14,.57,.57,.24);t.wheel(-length+1.45,s*1.14,.57,.57,.24)
  if truck:t.wheel(-length+2.65,s*1.14,.57,.57,.24)
 t.mesh('Wraparound front screen',[(length, -1.13,1.35),(length,1.13,1.35),(length-.86,1.08,3.03),(length-.86,-1.08,3.03)],[(0,1,2,3)],t.glass)
 t.box('Directed front light',(length+.04,0,1.07),(.07,2.15,.08),t.lamp,.02)
 t.box('Dark tail panel',(-length,0,2),(.13,2.3,2.3),t.paint,.13)
 for s in [-1,1]:t.box('Rear vertical brake light',(-length-.08,s*1.02,1.9),(.05,.08,1.35),t.red,.02)
 if truck:
  t.box('Modular sealed cargo cassette',(-.9,0,2.13),(5.6,2.2,2.35),t.paint,.17)
  for i in range(9):t.box('Cargo panel spline',(-3.6+i*.56,1.14,2.1),(.04,.025,2),t.alloy,.006)
  t.seats(1,2.8,1,1.25,.53)
 else:t.seats(7,2.3,.91,1.25,.72)
 t.export('FutureTruck' if truck else 'FutureBus')

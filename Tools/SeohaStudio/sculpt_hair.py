"""Original Seoha hair: scalp-aware flow, crescent sections and tapered tips.

The official Wuthering Waves showcase is an appearance reference only. No
third-party geometry, textures or measured character dimensions are imported.
"""
import math
from math import sin, cos, pi
from mathutils import Vector


def build(mesh, ellipsoid, line, materials, catmull, section, facey):
    centre = Vector((0, .010, 1.650))
    radius = Vector((.099, .111, .119))

    def scalp(a, p, lift=0):
        z=centre.z+radius.z*cos(p)
        rx,front,back=section(z-.010)
        rx+=.006+lift;front+=.006+lift;back+=.006+lift
        if z>1.746:
            dome=[(1.746,.052,.040,.050),(1.756,.036,.027,.035),(1.764,.020,.016,.020),(1.770,.0002,.0002,.0002)]
            for a0,b0 in zip(dome,dome[1:]):
                if a0[0]<=z<=b0[0]:
                    t=(z-a0[0])/(b0[0]-a0[0]);rx,front,back=[a0[i]*(1-t)+b0[i]*t+lift*(1-t) for i in range(1,4)];break
        c=cos(a)
        y=.003-front*max(0,c)**.68 if c>0 else .003-back*c
        return Vector((rx*sin(a),y,z))

    def clump(name, points, width, tone, height=.0025, root=.25, normal_hint=None):
        """A closed crescent, broad at mid-length and genuinely pointed at tips.

        A transported outward normal avoids the old fixed-camera cross section
        twisting abruptly into rectangular fins around the crown and temples.
        """
        rows, cols = 38, 18
        verts, faces = [], []
        previous = None
        def path(t):
            co=catmull(points,t)
            if name.startswith(('Hair / primary fringe','Hair / airy crossing','Hair / fringe outer')):
                rx,_,_=section(co.z-.009)
                xf=max(0,min(1,(rx+.022-abs(co.x))/.024))
                co.y=co.y*(1-xf)+(facey(co.x*.91,co.z-.009)-.008)*xf
            return co
        for j in range(rows):
            t = j/(rows-1)
            co = path(t)
            tangent = (path(min(1,t+.002))-path(max(0,t-.002))).normalized()
            if normal_hint is None:
                outward = Vector(tuple((co[i]-centre[i])/(radius[i]**2) for i in range(3)))
                if co.z < 1.56: outward = Vector((co.x*1.2,-1,.05))
            else: outward = Vector(normal_hint)
            normal = (outward-tangent*outward.dot(tangent)).normalized()
            if normal.length < .01:
                normal = previous or Vector((0,-1,0))
            if previous is not None and normal.dot(previous)<0: normal.negate()
            previous=normal.copy()
            side=tangent.cross(normal).normalized()
            taper=(root+(1-root)*sin(pi*t*.84)**.7)*(max(.0001,1-t)**.57)
            half=width*taper*(.012+.988*min(1,t/.10))
            # Opposite edge meets the underside; no blunt tubular end caps.
            for k in range(cols):
                a=2*pi*k/cols;u=cos(a);top=sin(a)
                camber=height*(.82+.18*cos(t*pi))*max(0,top)
                underside=.0006*min(0,top)
                grooves=.00014*sin(u*pi*6+t*.8)*max(0,top)
                root_inset=.0045*max(0,1-t/.18)**2
                verts.append(co+side*half*u+normal*(camber+underside+grooves-root_inset))
        for j in range(rows-1):
            for k in range(cols):
                a=j*cols+k;b=j*cols+(k+1)%cols
                faces.append((a,b,b+cols,a+cols))
        faces.append(tuple(reversed(range(cols))))
        faces.append(tuple((rows-1)*cols+k for k in range(cols)))
        obj=mesh(name,verts,faces,materials[tone],'head',1)
        uv=obj.data.uv_layers.new(name='Sculpted lock flow')
        for loop in obj.data.loops:
            j,k=divmod(loop.vertex_index,cols)
            uv.data[loop.index].uv=((cos(2*pi*k/cols)+1)*.5,j/(rows-1))
        return obj

    # One continuous underlayer reaches the nape. Its front opening follows
    # the hairline rather than exposing a smooth hemisphere under short strips.
    verts,faces=[],[];columns=96;rows=42
    for j in range(rows):
        t=j/(rows-1)
        for k in range(columns):
            a=k*2*pi/columns
            q=max(0,min(1,(.84-cos(a))/.94));q=q*q*(3-2*q)
            end=.80+1.29*q+.035*q*sin(a*9)
            p=.015+t*end
            # Low broad sculpted ridges, swept diagonally into the tied section.
            lift=.0005+.0011*(.5+.5*cos(a*23+p*7))*sin(pi*t)
            verts.append(scalp(a,p,lift))
    for j in range(rows-1):
        for k in range(columns):
            a=j*columns+k;b=j*columns+(k+1)%columns
            faces.append((a,b,b+columns,a+columns))
    mesh('Hair / continuous swept underlayer',verts,faces,materials[2],'head',1)

    # Flow follows the skull instead of interpolating through it toward the bun.
    # The lower path converges only after passing the temporal/occipital volume.
    for j in range(28):
        a=.82+j*(2*pi-1.64)/27
        points=[]
        for t in [0,.16,.34,.54,.74,.9,1]:
            p=.22+1.84*t
            angle=a+(4.02-a)*t*t*.98
            q=scalp(angle,p,.0028)
            points.append(q)
        clump('Hair / occipital sweep %02d'%j,points,.0175,2+j%3,.0020,root=.06)

    # Secondary side sweeps cover the temporal underlayer and continue all the
    # way to the bun. Their roots disappear underneath the overlying fringe.
    for side,start in [('right',1.24),('left',5.04)]:
        for j in range(7):
            start_p=1.14+j*.135;points=[]
            for t in [0,.16,.34,.54,.74,1]:
                angle=start+(4.02-start)*t
                p=start_p+(2.10-start_p)*t
                points.append(scalp(angle,p,.0026+j*.00015))
            clump('Hair / gathered side %s %02d'%(side,j),points,.013,2+j%4,.0018,root=.10)

    # Wide asymmetric primary locks share an intentional part. They have
    # independent endpoints rather than eight coincident tubes over the brow.
    primaries=[
        ([(.048,-.027,1.758),(.004,-.069,1.753),(-.049,-.092,1.724),(-.086,-.071,1.682),(-.101,-.004,1.618)],.027,4),
        ([(.043,-.042,1.754),(.000,-.084,1.739),(-.041,-.104,1.703),(-.077,-.091,1.662),(-.095,-.048,1.619)],.024,3),
        ([(.041,-.058,1.747),(.014,-.093,1.720),(-.019,-.112,1.681),(-.055,-.103,1.642),(-.080,-.078,1.611)],.023,4),
        ([(.044,-.064,1.742),(.024,-.096,1.704),(.001,-.109,1.661),(-.027,-.107,1.621),(-.059,-.092,1.597)],.013,5),
    ]
    for j,(points,width,tone) in enumerate(primaries):
        clump('Hair / primary fringe %02d'%j,points,width,tone,.0031,root=.18)

    # Fine secondary cut ends divide the silhouette, not the entire scalp.
    clump('Hair / airy crossing fringe',[(.030,-.084,1.728),(.012,-.106,1.687),(-.009,-.112,1.642),(-.043,-.102,1.607),(-.073,-.079,1.600)],.0056,5,.0010,root=.08)
    clump('Hair / fringe outer wisp',[(-.021,-.097,1.722),(-.058,-.105,1.693),(-.088,-.087,1.653),(-.091,-.043,1.606)],.0050,4,.0011,root=.07)

    for j in range(5):
        q=j*.006
        points=[(.046+q*.3,-.021+q*.7,1.756-q*.15),(.073+q*.50,-.058+q*.5,1.722),(.089+q*.35,-.062+q*.9,1.672),(.092+q*.30,-.029+q*.9,1.618),(.080+q*.40,.037+q*.7,1.571)]
        clump('Hair / temporal return %02d'%j,points,.0135,2+j%4,.0025,root=.10)

    # Slim temple locks, with two offset cut ends per side and one fine wisp.
    for side in [-1,1]:
        for j in range(3):
            x=side*(.083+j*.0045)
            points=[(x,-.055,1.680),(x*1.05,-.083,1.577),(x*.95,-.104,1.477),(x*1.07,-.146,1.397),(x*.76+side*j*.004,-.139,1.332-j*.024)]
            clump('Hair / long face-framing lock %s %s'%(side,j),points,.0078 if j==0 else .0049,3+j,.0016,root=.15,normal_hint=(0,-1,0))

    # A layered, low, loose left bun with visibly interleaved loops.
    bun=Vector((-.105,.063,1.592))
    ellipsoid('Hair / low left bun core',bun,(.041,.035,.044),materials[2])
    for j in range(10):
        a=j*2*pi/10;points=[]
        for t in [0,.15,.34,.55,.76,1]:
            p=.19+2.87*t;turn=a+2*t
            points.append(bun+Vector((.046*sin(p)*cos(turn),.039*sin(p)*sin(turn),.047*cos(p))))
        clump('Hair / loose bun fold %02d'%j,points,.0105,2+j%4,.0027,root=.1)
    for j in range(6):
        off=j*.003
        clump('Hair / bun tapered escape %02d'%j,[(-.116,.047+off,1.625),(-.151,.045+off,1.614),(-.153,.054+off,1.581),(-.128,.041+off,1.548-off)],.0036,4+j%2,.0010,root=.08)
    for j in range(5):
        line('Hair / fine flyaway %02d'%j,[(-.062,.067,1.731),(-.103,.069,1.717),(-.128,.057,1.672),(-.138,.047,1.646-j*.008)],.00028,materials[4])
    # A single restrained crown flyaway; no random spikes around the hairline.
    line('Hair / crown flyaway',[(.033,-.025,1.758),(-.018,-.045,1.779),(-.071,-.042,1.749),(-.089,-.03,1.730)],.00032,materials[5])

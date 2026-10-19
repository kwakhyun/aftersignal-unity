"""Continuous rest-mesh and skeleton deformation for the authored Seoha source."""
import math

def clamp(x):return min(1.,max(0.,x))
def sstep(x):x=clamp(x);return x*x*(3-2*x)
def g(x,s):return math.exp(-(x/s)**2)

def face_sculpt(p):
    x,y,z=p;side=1 if x>=0 else -1;ax=abs(x)
    front=sstep((.014-y)/.045)
    # Rounded chin and a softly changing jaw plane, rather than the stock V tip.
    chin=g(x,.027)*g(z-1.468,.021)*front
    p.x+=side*.0012*g(ax-.020,.019)*g(z-1.475,.016)*front
    p.z+=.0012*g(x,.012)*g(z-1.461,.010)*front
    p.y-=.0020*chin
    p.x-=side*.0018*g(ax-.050,.022)*g(z-1.492,.020)*front
    # Defined cheekbone, under-eye transition and a softer lower cheek.
    p.y-=.0028*g(ax-.049,.028)*g(z-1.526,.021)*front
    p.y+=.0015*g(ax-.052,.018)*g(z-1.498,.014)*front
    # Slightly longer horizontal eye silhouette and a gentle lift at the corner.
    orbit=g(ax-.043,.029)*g(z-1.548,.033)*front
    p.x+=side*.0016*orbit*sstep((ax-.014)/.049)
    p.z+=.0015*orbit*sstep((ax-.026)/.039)
    p.y+=.0011*orbit
    p.z+=(1.546-z)*.10*orbit
    # Relax the inner brow rather than locking the resting face into a frown.
    p.z+=.0021*g(ax-.024,.017)*g(z-1.574,.010)*front
    # Continuous nose bridge, restrained rounded tip and separate alar planes.
    p.y-=.0028*g(x,.010)*g(z-1.544,.019)*front
    p.y+=.0011*g(x,.006)*g(z-1.520,.007)*front
    p.x+=side*.0012*g(ax-.008,.006)*g(z-1.518,.009)*front
    p.y+=.0007*g(ax-.006,.0025)*g(z-1.513,.0025)*front
    # The lips and oral cavity share the same deformation, preserving speech.
    mouth=g(x,.027)*g(z-1.492,.012)*front
    p.x*=1+.09*mouth
    p.y-=.0008*g(x,.019)*g(z-1.490,.004)*front
    p.y-=.0006*g(x,.016)*g(z-1.497,.003)*front
    return p

def body_cage(p):
    x,y,z=p;ax=abs(x);side=1 if x>=0 else -1
    torso=sstep((z-.94)/.16)*(1-sstep((z-1.40)/.06))
    central=1-sstep((ax-.135)/.055)
    # Shoulder breadth is increased with the arms translated along with it.
    shoulders=g(z-1.374,.076)
    offset=.023*sstep(ax/.13)*shoulders
    p.x+=side*offset
    # Rib cage and pelvis expand a little; the waist retains its slender shape.
    p.x*=1+.075*g(z-1.260,.08)*central+.055*g(z-.975,.09)-.020*g(z-1.123,.035)*central
    front=sstep((.025-y)/.07)
    back=sstep((y-.005)/.075)
    p.y-=.028*g(ax-.055,.041)*g(z-1.274,.052)*front*central
    p.y+=.003*g(z-1.203,.014)*front*central
    p.y+=.011*g(ax-.074,.072)*g(z-.940,.070)*back
    p.y-=.007*g(ax-.083,.065)*g(z-.788,.13)*front
    p.y+=.006*g(ax-.079,.055)*g(z-.365,.085)*back
    # The abdomen has obliques, a shallow linea alba and a small navel depression.
    midriff=g(z-1.118,.060)*central*front
    p.y-=.0026*g(ax-.043,.023)*midriff
    p.y+=.0013*g(x,.010)*midriff
    p.y+=.0038*g(x,.005)*g(z-1.105,.006)*front
    p.y-=.0011*g(x,.009)*g(z-1.096,.004)*front
    # Subtle sternocleidomastoid / clavicle, not painted-on anatomy lines.
    p.y-=.0018*g(ax-.021,.012)*g(z-1.426,.038)*front
    p.y-=.0015*g(ax-.048,.030)*g(z-1.372,.012)*front
    p.x*=1+.055*g(z-1.435,.040)*(1-sstep((ax-.03)/.025))
    # Shorten overlong legs and neck, retaining the feet on the ground.
    p.z-=.029*sstep(z/1.025)
    p.z-=.013*sstep((z-1.405)/.073)
    p.z-=.013*sstep((z-1.582)/.100)
    p.y-=.006*sstep((z-1.40)/.10)
    return p

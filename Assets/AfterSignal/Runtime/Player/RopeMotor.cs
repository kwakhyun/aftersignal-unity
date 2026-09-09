using UnityEngine;

namespace AfterSignal
{
    public sealed class RopeMotor : MonoBehaviour
    {
        public GrappleAnchor Target {get;private set;}
        public GrappleAnchor Candidate {get;private set;}
        public bool Attached=>Target&&Target.Valid;
        public float Length {get;private set;}
        public int AttachCount {get;private set;}
        public float Range=>player&&CivicWorld.Exploration(player.Director.stage)?160:player?player.Tuning.ropeRange:24;
        public bool AimAssisted {get;private set;}
        PlayerMotor player;LineRenderer line;bool previousHeld;int selectionFrame=-1;float nextSelection,pressBuffer,blockedFor;
        GrappleAnchor surfacePreview,surfaceLatch;
        readonly RaycastHit[] hits=new RaycastHit[64];

        public void Initialize(PlayerMotor owner)
        {
            player=owner;line=gameObject.AddComponent<LineRenderer>();line.positionCount=13;line.widthMultiplier=.037f;
            line.sharedMaterial=Resources.Load<Material>("Materials/CyanFX");line.numCapVertices=3;line.startColor=Color.white;line.endColor=new Color(.1f,.8f,1);line.enabled=false;
        }
        GrappleAnchor SurfaceAnchor(ref GrappleAnchor anchor)
        {
            if(!anchor){var go=new GameObject("Rope surface contact");go.transform.SetParent(transform,false);anchor=go.AddComponent<GrappleAnchor>();}
            return anchor;
        }
        bool Ignore(Collider c)
        {
            if(!c||c.isTrigger||c.transform.IsChildOf(player.transform)||c.GetComponentInParent<PlayerMotor>())return true;
            var body=c.GetComponentInParent<WorldActor>();return body&&!body.environmental&&!c.GetComponentInParent<CityVehicle>();
        }
        bool FirstSurface(Ray ray,float distance,out RaycastHit nearest)
        {
            int count=Physics.RaycastNonAlloc(ray,hits,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            // Dense vehicle hulls can saturate the small buffer. Never attach through the first wall.
            var found=hits;if(count==hits.Length){found=Physics.RaycastAll(ray,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);count=found.Length;}
            nearest=default;float best=float.PositiveInfinity;
            for(int i=0;i<count;i++){var hit=found[i];if(Ignore(hit.collider)||hit.distance>=best)continue;best=hit.distance;nearest=hit;}
            return nearest.collider;
        }
        bool ClearTo(GrappleAnchor anchor)
        {
            var d=anchor.transform.position-player.Shoulder;if(d.sqrMagnitude<.04f)return true;
            if(!FirstSurface(new Ray(player.Shoulder,d.normalized),Mathf.Max(0,d.magnitude-.16f),out var hit))return true;
            return anchor.Surface&&hit.collider==anchor.SurfaceCollider&&hit.distance>=d.magnitude-.45f;
        }
        public GrappleAnchor Select(Vector2 pointer)
        {
            AimAssisted=false;
            var cam=Camera.main;if(!cam||!player)return null;
            // Preserve authored capacitor puzzles. Exploration uses the crosshair surface.
            GrappleAnchor best=null;float score=float.MaxValue;
            if(!CivicWorld.Exploration(player.Director.stage))foreach(var anchor in GrappleAnchor.All)
            {
                if(!anchor||!anchor.Valid)continue;float dist=Vector3.Distance(player.Shoulder,anchor.transform.position);if(dist>Range||dist<1)continue;
                var screen=cam.WorldToScreenPoint(anchor.transform.position);float pixels=Vector2.Distance(pointer,new Vector2(screen.x,screen.y));
                if(screen.z<=0||pixels>player.Tuning.aimAssistPixels*Screen.height/900f||!ClearTo(anchor))continue;
                float rank=pixels+dist*.8f;if(rank<score){score=rank;best=anchor;}
            }
            if(best)return best;
            var direct=SurfaceAt(cam,pointer);if(direct)return direct;
            // A small screen-space fan catches edges and slender structures without attaching through walls.
            float radius=Screen.height*.035f;
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI*.25f;
                var assisted=SurfaceAt(cam,pointer+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                if(assisted){AimAssisted=true;return assisted;}
            }
            return null;
        }
        GrappleAnchor SurfaceAt(Camera camera,Vector2 pointer)
        {
            if(!FirstSurface(camera.ScreenPointToRay(pointer),Range+Vector3.Distance(camera.transform.position,player.Shoulder),out var hit))return null;
            float distance=Vector3.Distance(player.Shoulder,hit.point);if(distance<2.2f||distance>Range)return null;
            var preview=SurfaceAnchor(ref surfacePreview);preview.SetSurface(hit.collider,hit.point+hit.normal*.065f);
            return ClearTo(preview)?preview:null;
        }
        public void Tick(ControlFrame input,float dt)
        {
            bool pressed=input.grapple&&!previousHeld;previousHeld=input.grapple;
            if(pressed&&Attached){Release();return;}
            if(pressed)pressBuffer=.3f;
            // Preview is bounded to 12 Hz; a fresh click still gets an immediate query.
            if(selectionFrame!=Time.frameCount&&(pressed||Time.time>=nextSelection))
            {selectionFrame=Time.frameCount;nextSelection=Time.time+.08f;Candidate=Select(input.pointer);}
            if(Candidate)Candidate.FollowSurface();
            if(pressBuffer>0&&!Attached&&Candidate){Attach(Candidate);pressBuffer=0;}
            pressBuffer=Mathf.Max(0,pressBuffer-dt);
            if(Target&&!Target.Valid){Release();return;}if(!Attached)return;
            Target.FollowSurface();
            blockedFor=ClearTo(Target)?0:blockedFor+dt;
            if(blockedFor>.12f||Vector3.Distance(player.Shoulder,Target.transform.position)>Range*1.35f){Release();return;}
            float reel=input.move.y<-.1f?-16:input.move.y>.1f?28:14;
            Length=Mathf.Clamp(Length-(Target.cityAnchor?reel:input.move.y*player.Tuning.ropeReelSpeed+1.6f)*dt,2.5f,Range);
            Vector3 delta=Target.transform.position-player.Shoulder;var v=player.Velocity;
            v+=delta.normalized*(Mathf.Max(0,delta.magnitude-Length)*48+5)*dt;
            v+=player.Director.CameraRig.ViewRight*(input.move.x*22*dt);player.Velocity=Vector3.ClampMagnitude(v,Target.cityAnchor?40:24);
            if(Target.hasLanding&&input.move.y>.1f&&delta.magnitude<3.3f)
            {
                var landing=Target.landing;var d=landing-player.transform.position;
                if(d.magnitude<4&&!Physics.CheckCapsule(landing+Vector3.up*.42f,landing+Vector3.up*1.7f,.34f,1,QueryTriggerInteraction.Ignore)&&!Physics.Linecast(player.Shoulder,landing+Vector3.up*1.2f,1,QueryTriggerInteraction.Ignore))
                {player.Controller.Move(d);player.Velocity=Vector3.zero;player.Carry(Vector3.zero);Release();return;}
            }
            if(input.jump){player.Velocity+=Vector3.up*5.5f;player.Director.Audio.Play("jump",player.Shoulder,.22f,1);Release();}
        }
        public void Attach(GrappleAnchor anchor)
        {
            if(!anchor||!anchor.Valid||Vector3.Distance(player.Shoulder,anchor.transform.position)>Range||!ClearTo(anchor))return;
            if(anchor.Surface){var latch=SurfaceAnchor(ref surfaceLatch);latch.SetSurface(anchor.SurfaceCollider,anchor.transform.position);Target=latch;}else Target=anchor;
            Length=Vector3.Distance(player.Shoulder,Target.transform.position)*.94f;blockedFor=0;AttachCount++;
            player.Velocity+=(Target.transform.position-player.Shoulder).normalized*8+Vector3.up*5;
            player.Director.OnAnchor(Target);SignalEffects.Burst(Target.transform.position,SignalEffects.Cyan,14,3);player.Director.Audio.Play("rope_attach",player.Shoulder,.3f,2);
        }
        public void Constrain()
        {
            if(!Attached)return;Target.FollowSurface();Vector3 radial=player.Shoulder-Target.transform.position;float distance=radial.magnitude;
            if(distance>Length+.1f){Vector3 outward=radial/distance;player.Controller.Move(-outward*Mathf.Min(distance-Length,.65f));float speed=Vector3.Dot(player.Velocity,outward);if(speed>0)player.Velocity-=outward*speed;}
        }
        public void Release(){if(Target&&player&&player.Director&&player.Director.Audio)player.Director.Audio.Play("rope_release",player.Shoulder,.13f,1);Target=null;pressBuffer=0;blockedFor=0;if(line)line.enabled=false;}
        void LateUpdate()
        {
            if(!line)return;line.enabled=Attached;if(!Attached)return;Target.FollowSurface();
            for(int i=0;i<13;i++){float t=i/12f;var point=Vector3.Lerp(player.Shoulder,Target.transform.position,t);point.y-=Mathf.Sin(t*Mathf.PI)*.1f;line.SetPosition(i,point);}
        }
        void OnDisable(){Release();previousHeld=false;selectionFrame=-1;}
    }
}

using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CameraRig : MonoBehaviour
    {
        public GameDirector director;
        public bool ReducedMotion { get; set; }

        Vector3 velocity, impulse, basePosition;
        float shake, lead = 2;
        float orbitYaw=-8, orbitPitch=15, orbitDistance=8.5f;
        public float OrbitDistance=>orbitDistance;
        public bool FreeOrbit { get; private set; }=true;
        public float OrbitYaw => orbitYaw;
        public float OrbitPitch => orbitPitch;
        public bool WallCorrection=>!Driving&&director&&director.Player&&director.Player.WallClimbing;
        public bool CloseQuarters=>WallCorrection&&(transform.position-director.Player.Shoulder).sqrMagnitude<2.2f*2.2f;
        Vector3 cachedFocus;int focusFrame=-1;SphereCollider cameraProbe;
        readonly Collider[] cameraOverlaps=new Collider[16];readonly RaycastHit[] focusHits=new RaycastHit[12];
        Renderer[] heroRenderers;bool[] heroWasHidden;bool heroHidden;
        public void SetView(Vector3 target){var d=target-director.Player.Shoulder;orbitYaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;orbitPitch=Mathf.Clamp(-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg,-65,78);velocity=Vector3.zero;Snap();}
        public bool CanLook => director && director.Ready && !director.Blocked && !(UrbanSimulation.Instance && UrbanSimulation.Instance.MapOpen);
        Vector3 Focus
        {
            get
            {
                if(Driving)return VehicleFocus();
                if(focusFrame==Time.frameCount)return cachedFocus;focusFrame=Time.frameCount;
                var p=director.Player;var origin=p.Shoulder;
                if(!WallCorrection)return cachedFocus=origin+Quaternion.Euler(0,orbitYaw,0)*Vector3.right*.75f+Vector3.up*.25f;
                var shift=p.WallNormal*.9f+Vector3.up*.35f;
                float distance=shift.magnitude;
                int count=Physics.SphereCastNonAlloc(origin,.18f,shift.normalized,focusHits,distance,1,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(focusHits[i].collider)distance=Mathf.Min(distance,Mathf.Max(0,focusHits[i].distance-.08f));
                return cachedFocus=origin+shift.normalized*distance;
            }
        }
        public Vector3 ViewRight => FreeOrbit ? Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized : Vector3.right;
        public Vector3 LookForward=>Vector3.Cross(ViewRight,Vector3.up);
        public Vector3 MoveDirection(Vector2 input) => ViewRight * input.x + Vector3.Cross(ViewRight, Vector3.up) * input.y;

        public void ReadLook(ControlFrame input)
        {
            if (!CanLook) return;
            ReadVehicleView(input);
            if (input.cameraReset) { FreeOrbit = true; orbitYaw=-8;orbitPitch=15;orbitDistance=8.5f; velocity = Vector3.zero; if (Viewing) { CityLife.Instance.PanoramaYaw = -15; CityLife.Instance.PanoramaPitch = 14; } return; }
            if(Mathf.Abs(input.zoom)>.01f){orbitDistance=Mathf.Clamp(orbitDistance-Mathf.Sign(input.zoom)*.9f,3.5f,18);FreeOrbit=true;}
            if (!input.look || input.lookDelta.sqrMagnitude < .001f) return;
            if (Viewing)
            {
                CityLife.Instance.PanoramaYaw += input.lookDelta.x * .18f;
                CityLife.Instance.PanoramaPitch = Mathf.Clamp(CityLife.Instance.PanoramaPitch - input.lookDelta.y * .18f, -65, 78);
                return;
            }
            if (!FreeOrbit)
            {
                var offset = transform.position - Focus;
                orbitDistance = Mathf.Clamp(offset.magnitude, 4, 24);
                var angle = Quaternion.LookRotation(-offset).eulerAngles;
                orbitYaw = angle.y;
                orbitPitch = Mathf.DeltaAngle(0, angle.x);
                FreeOrbit = true;
            }
            orbitYaw = Mathf.Repeat(orbitYaw + input.lookDelta.x * .18f, 360);
            orbitPitch = Mathf.Clamp(orbitPitch - input.lookDelta.y * .18f, -65, 78);
        }

        Vector3 AvoidWalls(Vector3 desired)
        {
            var focus = Focus;
            var ray = desired - focus;
            float length = ray.magnitude;
            if (length < .001f) return desired;
            int n = Physics.SphereCastNonAlloc(focus, .3f, ray / length, walls, length, 1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var hit = walls[i];
                if (!hit.collider || Driving && hit.collider.transform.IsChildOf(UrbanSimulation.Instance.Current.transform)) continue;
                length = Mathf.Min(length, Mathf.Max(.05f, hit.distance - .08f));
            }
            var result=focus+ray.normalized*length;
            if(WallCorrection)
            {
                int count=Physics.OverlapSphereNonAlloc(result,.28f,cameraOverlaps,1,QueryTriggerInteraction.Ignore);
                if(count>0)
                {
                    if(!cameraProbe){cameraProbe=gameObject.AddComponent<SphereCollider>();cameraProbe.radius=.28f;cameraProbe.enabled=false;}
                    for(int i=0;i<count;i++)
                    {
                        var obstacle=cameraOverlaps[i];if(!obstacle||obstacle.transform.IsChildOf(director.Player.transform))continue;
                        if(Physics.ComputePenetration(cameraProbe,result,Quaternion.identity,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var push,out float depth))result+=push*(depth+.02f);
                    }
                }
            }
            return result;
        }
        readonly RaycastHit[] walls = new RaycastHit[24];
        bool Driving => UrbanSimulation.Instance && UrbanSimulation.Instance.Driving;

        bool Viewing => CityLife.Instance && CityLife.Instance.Panorama;

        bool Stairwell => director.stage == StageId.Residence && director.Player.transform.position.x > 54;

        public void Kick(float amount)
        {
            Impact(Vector3.right, amount * .65f);
        }

        public void Impact(Vector3 direction, float amount)
        {
            if (ReducedMotion || PresentationSettings.Motion < .01f)
                return;
            impulse = Vector3.ClampMagnitude(impulse + new Vector3(direction.x, .35f, 0) * amount, .16f) * PresentationSettings.Motion;
            shake = .13f;
        }

        Quaternion Angle => Viewing ? Quaternion.LookRotation(director.Player.Shoulder - basePosition) : Driving ? Quaternion.LookRotation(UrbanSimulation.Instance.Current.transform.position + UrbanSimulation.Instance.Current.Forward * 5 + Vector3.up * 1.5f - basePosition, Vector3.up) : Stairwell ? Quaternion.LookRotation(director.Player.transform.position + Vector3.up * 1.1f - basePosition, Vector3.up) : director.stage == StageId.UrbanCity || director.stage == StageId.UrbanInterior ? Quaternion.Euler(20, -8, 0) : CivicWorld.Interior(director.stage) ? Quaternion.Euler(14, -5, 0) : CivicWorld.Exploration(director.stage) ? Quaternion.Euler(16, -8, 0) : Quaternion.Euler(10, 0, 0);

        void SetCinemaSeat(){basePosition=director.Player.transform.position+Vector3.up*1.65f;transform.SetPositionAndRotation(basePosition,Quaternion.Euler(orbitPitch,orbitYaw,0));velocity=Vector3.zero;}

        public void Snap()
        {
            focusFrame=-1;
            if (VenueRuntime.ViewingCinema) { SetCinemaSeat(); return; }
            if (FirstPersonVehicle) { SetCockpit(); return; }
            basePosition = Target();
            velocity = Vector3.zero;
            transform.position = basePosition;
            transform.rotation = FreeOrbit && !Viewing ? Quaternion.LookRotation(Focus-basePosition) : Angle;
        }

        Vector3 Target()
        {
            var p = director.Player;
            if (FreeOrbit && !Viewing)
                return AvoidWalls(Focus + Quaternion.Euler(orbitPitch, orbitYaw, 0) * Vector3.back * (orbitDistance + (Driving?Mathf.Max(0,UrbanSimulation.Instance.Current.HalfLength-2)*1.5f:0)));
            if (Viewing)
            {
                var focus = p.Shoulder;
                var desired = focus + Quaternion.Euler(CityLife.Instance.PanoramaPitch, CityLife.Instance.PanoramaYaw, 0) * Vector3.back * 32;
                var ray = desired - focus;
                float length = ray.magnitude;
                int n = Physics.SphereCastNonAlloc(focus, .5f, ray.normalized, walls, length, 1, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = walls[i];
                    if (h.collider)
                        length = Mathf.Min(length, Mathf.Max(.05f, h.distance - .1f));
                }

                return focus + ray.normalized * length;
            }

            // A low sightline passes beneath the alternating stair flights.
            // Only the view changes; the real decks and collisions remain in place.
            if (Stairwell)
                return p.transform.position + new Vector3(.7f, 2.35f, -10);
            if (Driving)
            {
                var car = UrbanSimulation.Instance.Current;
                var focus = car.transform.position + Vector3.up * 1.6f;
                float chaseDistance = car.HalfLength + 8 + Mathf.Abs(car.speed) * .06f;
                var desired = car.transform.position - car.Forward * chaseDistance + Vector3.up * 5.3f;
                var ray = desired - focus;
                float length = ray.magnitude;
                int n = Physics.SphereCastNonAlloc(focus, .35f, ray.normalized, walls, length, 1, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = walls[i];
                    if (!h.collider || h.collider.transform.IsChildOf(car.transform) || h.collider.GetComponentInParent<CityBuildingCutaway>())
                        continue;
                    length = Mathf.Min(length, Mathf.Max(3, h.distance - .2f));
                }

                return focus + ray.normalized * length;
            }

            if (director.stage == StageId.UrbanInterior)
                return p.transform.position + new Vector3(2.2f, 6.2f, -15);
            if (director.stage == StageId.UrbanCity)
                return p.transform.position + new Vector3(2.4f, 7, -17);
            float distance = director.stage == StageId.Roof ? 23 : 21;
            bool explore = CivicWorld.Exploration(director.stage), inside = CivicWorld.Interior(director.stage);
            if (explore)
                distance = inside ? 14 : 18;
            float rise = inside ? 4 : explore ? 5.6f : 4.5f;
            return new Vector3(Mathf.Clamp(p.transform.position.x + lead + (inside ? 1.2f : explore ? 2 : 0), inside ? 7 : 10, director.stageLength - 9), Mathf.Max(rise, p.transform.position.y + rise), -distance + p.transform.position.z * (explore ? 1 : .22f));
        }

        void LateUpdate()
        {
            if (!director || !director.Player || director.Blocked)
                return;
            if (VenueRuntime.ViewingCinema) { SetCinemaSeat(); return; }
            if (FirstPersonVehicle) { SetHeroHidden(false);SetCockpit(); return; }
            focusFrame=-1;
            GetComponent<Camera>().nearClipPlane=WallCorrection?.08f:.15f;
            lead = Mathf.Lerp(lead, Mathf.Clamp(director.Player.Velocity.x * .22f + director.Player.Facing * .65f, -2.4f, 2.4f), 1 - Mathf.Exp(-Time.deltaTime * 4));
            basePosition = Vector3.SmoothDamp(basePosition, Target(), ref velocity, Driving ? .22f : .14f);
            if (FreeOrbit || Viewing) basePosition = AvoidWalls(basePosition);
            shake = Mathf.Max(0, shake - Time.unscaledDeltaTime);
            impulse = Vector3.Lerp(impulse, Vector3.zero, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 24));
            transform.position = basePosition + (ReducedMotion ? Vector3.zero : impulse * Mathf.Cos((.13f - shake) * 46));
            transform.rotation = FreeOrbit && !Viewing ? Quaternion.LookRotation(Focus-basePosition) : Quaternion.Slerp(transform.rotation, Angle, 1 - Mathf.Exp(-Time.deltaTime * (Driving ? 9 : 14)));
            SetHeroHidden(WallCorrection&&(transform.position-director.Player.Shoulder).sqrMagnitude<1.2f*1.2f);
            var camera = GetComponent<Camera>();
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, Viewing ? 62 : Driving ? 56 : CloseQuarters?62:45, 1 - Mathf.Exp(-Time.deltaTime * 5));
        }
        void SetHeroHidden(bool hidden)
        {
            if(hidden==heroHidden)return;heroHidden=hidden;
            if(hidden)
            {
                heroRenderers=director.Player.GetComponentsInChildren<Renderer>();heroWasHidden=new bool[heroRenderers.Length];
                for(int i=0;i<heroRenderers.Length;i++){heroWasHidden[i]=heroRenderers[i].forceRenderingOff;heroRenderers[i].forceRenderingOff=true;}
            }
            else if(heroRenderers!=null)for(int i=0;i<heroRenderers.Length;i++)if(heroRenderers[i])heroRenderers[i].forceRenderingOff=heroWasHidden[i];
        }
        void OnDisable()=>SetHeroHidden(false);
    }
}

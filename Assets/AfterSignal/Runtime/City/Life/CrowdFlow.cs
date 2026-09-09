using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    // One spatial index includes facility residents, responders and street pedestrians.
    [DefaultExecutionOrder(1100)]
    public sealed class CrowdFlow:MonoBehaviour
    {
        public static CrowdFlow Instance{get;private set;}
        readonly Dictionary<Vector2Int,List<WorldActor>> cells=new();
        readonly Stack<List<WorldActor>> cellPool=new();
        readonly List<WorldActor> active=new();float next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=SceneEntered;SceneManager.sceneLoaded+=SceneEntered;SceneEntered(default,LoadSceneMode.Single);}
        static void SceneEntered(Scene scene,LoadSceneMode mode){if(!FindAnyObjectByType<CrowdFlow>())new GameObject("Crowd spacing and city activity").AddComponent<CrowdFlow>();}
        void Awake(){Instance=this;gameObject.AddComponent<ActorSpatialIndex>();gameObject.AddComponent<CityActivityDirector>();}
        static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/3),Mathf.FloorToInt(p.z/3));
        static bool Movable(WorldActor a)=>a&&a.Alive&&!a.Downed&&!a.monster&&!a.helicopter&&!a.robot&&!a.GetComponent<MedicalPending>()&&!a.GetComponent<RescueMedic>()&&!a.GetComponent<StolenVehicle>()&&!CivilianImpact.Active(a);
        void LateUpdate()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||Time.time<next)return;next=Time.time+.12f;
            foreach(var list in cells.Values){list.Clear();cellPool.Push(list);}cells.Clear();active.Clear();
            foreach(var a in WorldActor.All)if(a&&(a.transform.position-g.Player.transform.position).sqrMagnitude<240*240&&Movable(a)){active.Add(a);var k=Cell(a.transform.position);if(!cells.TryGetValue(k,out var list))cells[k]=list=cellPool.Count>0?cellPool.Pop():new();list.Add(a);}
            foreach(var a in active)
            {
                var art=a.GetComponent<DirectionalPerson>();if(art&&(art.Sitting||art.Lying))continue;
                Vector3 at=a.transform.position,shift=Vector3.zero;var key=Cell(at);
                float radius=a.GetComponent<FamilyMember>()?.Child==true?.86f:1.05f;int checkedNeighbors=0;
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if(cells.TryGetValue(key+new Vector2Int(x,z),out var nearby))foreach(var b in nearby)
                {
                    if(checkedNeighbors>=24)break;
                    if(a==b||Mathf.Abs(at.y-b.transform.position.y)>.9f)continue;var d=at-b.transform.position;d.y=0;float length=d.magnitude;
                    if(length>=radius)continue;checkedNeighbors++;
                    if(length<.01f){int lo=Mathf.Min(a.GetInstanceID(),b.GetInstanceID());d=Quaternion.Euler(0,Mathf.Abs(lo%360),0)*Vector3.right*(a.GetInstanceID()<b.GetInstanceID()?1:-1);length=0;}
                    shift+=d.normalized*(radius-length)*.55f;
                }
                shift=Vector3.ClampMagnitude(shift,.3f);if(shift.sqrMagnitude<.00001f)continue;
                if(PedestrianGround.Step(at,at+shift,out var safe)){var cc=a.GetComponent<CharacterController>();if(cc&&cc.enabled)cc.Move(safe-at);else a.transform.position=safe;}
            }
        }
        public static bool Vacant(Vector3 p,float radius=1.1f)
        {
            ActorSpatialIndex.Nearby(p,radius+2,placement);
            foreach(var a in placement)if(a&&a.gameObject.activeInHierarchy&&!a.helicopter&&!a.monster&&Mathf.Abs(a.transform.position.y-p.y)<1.5f&&(a.transform.position-p).sqrMagnitude<radius*radius)return false;
            for(int i=reservations.Count-1;i>=0;i--){if(Time.time-reservations[i].time>.4f){reservations.RemoveAt(i);continue;}if((reservations[i].at-p).sqrMagnitude<radius*radius)return false;}
            return true;
        }
        static readonly List<WorldActor> placement=new();
        static readonly List<(Vector3 at,float time)> reservations=new();
        public static bool Place(Vector3 origin,int seed,out Vector3 point,float radius=12)
        {
            for(int i=0;i<24;i++){float angle=(seed*29+i*137.508f)*Mathf.Deg2Rad;float r=i==0?0:Mathf.Sqrt(i/23f)*radius;var p=origin+new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);if(Vacant(p,1.45f)&&PedestrianGround.Stand(p,origin.y,1.2f,out point)&&Vacant(point,1.45f)){reservations.Add((point,Time.time));return true;}}point=origin;return false;
        }
        void OnDestroy(){if(Instance==this){Instance=null;DistrictGeometryCut.Reset();}}
    }
    public static class PedestrianGround
    {
        public static bool Stand(Vector3 p,float floor,float step,out Vector3 safe)
        {
            safe=p;if(!NpcGroundSupport.Floor(p,floor+step+.08f,step+2.3f,out float y)||y<floor-1.8f||y>floor+step)return false;
            safe.y=y+.035f;return !Physics.CheckCapsule(safe+Vector3.up*.38f,safe+Vector3.up*1.65f,.28f,1,QueryTriggerInteraction.Ignore);
        }
        public static bool Step(Vector3 from,Vector3 to,out Vector3 safe,float height=.85f)
        {
            if(!Stand(to,from.y,height,out safe))
            {
                // A body radius reaches a curb before its centre does. Probe the top of
                // that curb so short movement steps cannot get stuck against the riser.
                var horizontal=Vector3.ProjectOnPlane(to-from,Vector3.up).normalized;
                var lip=to+horizontal*.62f;
                if(horizontal.sqrMagnitude<.01f||!Stand(lip,from.y,height,out safe)||safe.y<from.y+.14f)return false;
            }
            Vector3 a=from+Vector3.up*(Mathf.Max(0,safe.y-from.y)+.4f),b=safe+Vector3.up*.4f,d=b-a;
            return d.sqrMagnitude<.0001f||!Physics.CapsuleCast(a,a+Vector3.up*1.2f,.26f,d.normalized,d.magnitude,1,QueryTriggerInteraction.Ignore);
        }
    }
}

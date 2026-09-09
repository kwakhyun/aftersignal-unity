using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Shared queries only index actors. World geometry, collision and mission state stay intact.
    public sealed class ActorSpatialIndex:MonoBehaviour
    {
        static ActorSpatialIndex instance;
        readonly Dictionary<Vector2Int,List<WorldActor>> grid=new();
        readonly Stack<List<WorldActor>> pool=new();
        float next;static bool dirty=true;
        static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/12),Mathf.FloorToInt(p.z/12));
        void Awake(){instance=this;dirty=true;}
        public static void Changed()=>dirty=true;
        void Refresh()
        {
            if(!dirty&&Time.time<next)return;dirty=false;next=Time.time+.18f;
            foreach(var bucket in grid.Values){bucket.Clear();pool.Push(bucket);}grid.Clear();
            foreach(var actor in WorldActor.All)
            {
                if(!actor||!actor.gameObject.activeInHierarchy)continue;var key=Cell(actor.transform.position);
                if(!grid.TryGetValue(key,out var bucket))grid[key]=bucket=pool.Count>0?pool.Pop():new List<WorldActor>(12);
                bucket.Add(actor);
            }
        }
        public static void Nearby(Vector3 at,float radius,List<WorldActor> result)
        {
            result.Clear();float limit=radius*radius;
            if(!instance||radius>150){foreach(var a in WorldActor.All)if(a&&(a.transform.position-at).sqrMagnitude<=limit)result.Add(a);return;}
            instance.Refresh();var low=Cell(at-Vector3.one*(radius+8));var high=Cell(at+Vector3.one*(radius+8));
            for(int x=low.x;x<=high.x;x++)for(int z=low.y;z<=high.y;z++)if(instance.grid.TryGetValue(new Vector2Int(x,z),out var bucket))foreach(var a in bucket)
                if(a&&a.gameObject.activeInHierarchy&&(a.transform.position-at).sqrMagnitude<=limit)result.Add(a);
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
    public static class ActorWorkBudget
    {
        public static long MotionUpdates,MotionSkipped;
        static int frame=-1;static Vector3 player;static bool ready;
        public static float DistanceSquared(Component actor)
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;var g=GameDirector.Instance;ready=g&&g.Player;player=ready?g.Player.transform.position:Vector3.zero;}
            return ready?(actor.transform.position-player).sqrMagnitude:0;
        }
        public static bool Tick(Component actor,ref float next,ref float last,out float dt,bool urgent=false)
        {
            float now=Time.time;if(now<next&&!urgent){MotionSkipped++;dt=0;return false;}
            float d=DistanceSquared(actor);float interval=urgent?0:d<30*30?.025f:d<75*75?.075f:d<140*140?.18f:.4f;
            dt=last==0?Mathf.Min(.06f,Time.deltaTime):Mathf.Min(.32f,now-last);last=now;next=now+interval;MotionUpdates++;return true;
        }
        public static float VisualInterval(Component a){float d=DistanceSquared(a);return d<40*40?0:d<100*100?.06f:d<180*180?.14f:.35f;}
    }
    public static class PopulationBudget
    {
        static int frame=-1,used;static double started;static readonly List<WorldActor> nearby=new(),walkers=new();
        public const int PerFrame=2;
        public static int Deferred{get;private set;}
        public static bool ClaimFrame(int count=1){if(frame!=Time.frameCount){frame=Time.frameCount;used=0;started=Time.realtimeSinceStartupAsDouble;}int limit=count==3&&used==0?3:PerFrame;if(count<1||used+count>limit||used>0&&Time.realtimeSinceStartupAsDouble-started>.0015){Deferred++;return false;}used+=count;return true;}
        public static bool Room(Vector3 p,bool seated=false)
        {
            ActorSpatialIndex.Nearby(p,seated?1.05f:24,nearby);int mobile=0;walkers.Clear();
            foreach(var a in nearby)
            {
                if(!a.Alive||a.helicopter||a.monster||Mathf.Abs(a.transform.position.y-p.y)>1.5f)continue;
                float d=(a.transform.position-p).sqrMagnitude;if(d<(seated?.62f* .62f:1.45f*1.45f))return false;
                var art=a.GetComponent<DirectionalPerson>();var venue=a.GetComponent<VenueActor>();
                if(!(art&&art.Sitting)&&!(venue&&venue.spectator)){walkers.Add(a);if(d<144)mobile++;}
            }
            if(!seated&&mobile>=12)return false;
            // New positions must not gradually fill the ring around an already crowded person.
            if(!seated&&walkers.Count>=18)foreach(var a in walkers)
            {
                if((a.transform.position-p).sqrMagnitude>=144)continue;int count=0;
                foreach(var b in walkers)if((a.transform.position-b.transform.position).sqrMagnitude<144&&++count>=18)return false;
            }
            ActorSpatialIndex.Nearby(p,180,nearby);int population=0;
            int cap=FidelityPresentation.Preset==0?150:220;
            foreach(var a in nearby){if(!a.Alive||a.police||a.military||a.gang||a.monster||a.helicopter||a.robot||a.terrorist)continue;var v=a.GetComponent<VenueActor>();if(v&&(v.spectator||v.athlete))continue;if(++population>=cap)return false;}
            return true;
        }
    }
    public static class AmbientWorkBudget
    {
        static int frame=-1,ground;static double began;
        public static int DeferredGround {get;private set;}
        public static bool ClaimGround()
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;ground=0;began=Time.realtimeSinceStartupAsDouble;}
            if(ground>=18||ground>0&&Time.realtimeSinceStartupAsDouble-began>.0012){DeferredGround++;return false;}
            ground++;return true;
        }
    }
}

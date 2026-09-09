using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public enum CityEventKind { None, Monster, Gang, Terror }
    // One city-wide encounter lease, including its warning and arriving vehicles.
    public static class CityEventGate
    {
        public static CityEventKind Kind { get; private set; }
        static Object owner;
        static float reservedUntil, quietSince, next, began;
        static readonly Dictionary<WorldActor,float> idle=new();
        static readonly List<WorldActor> actors=new();
        static readonly List<CityVehicle> carriers=new();
        public static bool Busy { get { Refresh(); return Kind!=CityEventKind.None; } }
        public static string Diagnostic=>$"kind={Kind}, actors={actors.Count}, carriers={carriers.Count}, quiet={Time.time-quietSince:0.0}, reserve={reservedUntil-Time.time:0.0}, bombs={CivicBomb.Active}";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Boot(){Reset();SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene s,LoadSceneMode mode)=>Reset();
        public static void Reset(){Kind=CityEventKind.None;owner=null;actors.Clear();carriers.Clear();idle.Clear();reservedUntil=quietSince=next=began=0;}
        public static bool Begin(Object requester,CityEventKind kind)
        {
            Refresh();if(Kind!=CityEventKind.None)return owner==requester&&Kind==kind;
            owner=requester;Kind=kind;began=Time.time;reservedUntil=Time.time+20;quietSince=0;return true;
        }
        public static bool Enrolled(WorldActor a)=>a&&actors.Contains(a);
        public static void Enroll(WorldActor a){if(a&&!actors.Contains(a)){actors.Add(a);reservedUntil=0;quietSince=0;}}
        public static void Enroll(CityVehicle c){if(c&&!carriers.Contains(c)){carriers.Add(c);reservedUntil=0;quietSince=0;}}
        public static void Cancel(Object requester){if(owner==requester)Reset();}
        public static bool JoinGang(WorldActor a)
        {
            if(Enrolled(a))return true;
            if(!Begin(a,CityEventKind.Gang))return false;Enroll(a);return true;
        }
        public static void Refresh()
        {
            if(Kind==CityEventKind.None||Time.time<next)return;next=Time.time+.35f;
            actors.RemoveAll(Finished);
            carriers.RemoveAll(c=>!c||c.Wrecked||c.owned||!c.occupied);
            if(actors.Count>0||carriers.Count>0||Time.time<reservedUntil){quietSince=0;return;}
            // A delayed bomb is part of the same event, even after its attackers are down.
            if(Kind==CityEventKind.Terror&&CivicBomb.Active>0)return;
            if(quietSince==0)quietSince=Time.time;
            if(Time.time-quietSince>8)Reset();
        }
        static bool Finished(WorldActor a)
        {
            if(!a||!a.gameObject.activeInHierarchy||!a.Alive||a.Downed){if(a)idle.Remove(a);return true;}
            if(Kind!=CityEventKind.Gang||Time.time-began<30)return false;
            var gang=a.GetComponent<GangMember>();var crime=a.GetComponent<GangCrime>();
            if(!gang||gang.Target||gang.AttackingPlayer||crime&&crime.ActiveCrime){idle.Remove(a);return false;}
            if(!idle.TryGetValue(a,out float since)){idle[a]=Time.time;return false;}
            return Time.time-since>15;
        }
    }
}

using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public sealed class FacadeGlass:MonoBehaviour
    {
        readonly Vector4[] openings=new Vector4[32];readonly Dictionary<Vector3Int,float> damage=new();
        readonly List<Renderer> windows=new();int count;MaterialPropertyBlock props;
        public int BrokenWindows=>count;
        void Start()
        {
            foreach(var r in GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial&&(r.sharedMaterial.shader.name=="AfterSignal/NovaWindows"||r.sharedMaterial.shader.name=="AfterSignal/Architectural Glass"))windows.Add(r);
            props=new MaterialPropertyBlock();
        }
        public bool OpenAt(Vector3 p)
        {for(int i=0;i<count;i++)if((p-(Vector3)openings[i]).sqrMagnitude<openings[i].w*openings[i].w)return true;return false;}
        public void Hit(Vector3 point,float amount)
        {
            if(windows.Count==0||amount<=0||OpenAt(point))return;
            var key=new Vector3Int(Mathf.FloorToInt(point.x/2.8f),Mathf.FloorToInt(point.y/3.7f),Mathf.FloorToInt(point.z/2.8f));
            damage.TryGetValue(key,out var value);value+=amount;damage[key]=value;
            if(value<32){SignalEffects.Burst(point,new Color(.5f,.9f,1),8,2);return;}
            if(count>=openings.Length)return;
            openings[count++]=new Vector4(point.x,point.y,point.z,1.45f);
            foreach(var r in windows)if(r){r.GetPropertyBlock(props);props.SetInt("_BreakCount",count);props.SetVectorArray("_BreakCenters",openings);r.SetPropertyBlock(props);}
            SignalEffects.Glass(point,new Vector3(2.5f,2.8f,.07f));GameDirector.Instance?.Audio.PlayCue(1300,.28f,.18f);
            var building=GetComponent<HighriseBuilding>();if(building)
            {
                var entry=new GameObject("Broken window access");entry.transform.SetParent(transform,true);entry.transform.position=point;
                var door=entry.AddComponent<HighriseDoor>();door.building=building;door.floor=Mathf.Clamp(Mathf.FloorToInt((point.y-building.Bounds.min.y)/Mathf.Max(3.7f,building.Bounds.size.y/building.Floors)),0,building.Floors-1);
                var interaction=entry.AddComponent<InteractionPoint>();interaction.kind=InteractionKind.Furniture;interaction.title="깨진 창으로 "+(door.floor+1)+"층 진입";interaction.radius=2.4f;
            }
        }
        public static void Strike(Vector3 origin,Vector3 direction,float range,float amount)
        {
            if(Physics.SphereCast(origin,.4f,direction,out var hit,range,1,QueryTriggerInteraction.Ignore))hit.collider.GetComponentInParent<FacadeGlass>()?.Hit(hit.point,amount);
            foreach(var glass in BreakableGlass.All)if(glass&&!glass.Broken)
            {
                var collider=glass.GetComponent<Collider>();if(!collider)continue;var at=collider.ClosestPoint(origin);
                if((at-origin).sqrMagnitude<range*range&&Vector3.Dot(at-origin,direction)>-.3f)glass.Hit(amount);
            }
        }
    }
}

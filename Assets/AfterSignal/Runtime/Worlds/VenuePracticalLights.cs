using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Fixtures are batched with architecture; only the twelve closest lamps on the occupied
    // floor emit light. Large interiors do not create thousands of realtime Unity lights.
    public sealed class VenuePracticalLights:MonoBehaviour
    {
        readonly Light[] lamps=new Light[12];readonly List<Vector3> candidates=new(80);float next;
        public int ActiveLights{get;private set;}
        public static IEnumerable<Vector3> Positions(float width,float depth,float ceiling)
        {
            for(float x=-width*.5f+10;x<width*.5f-6;x+=13)
            for(float z=-depth*.5f+10;z<depth*.5f-6;z+=13)
            {if(x< -width*.5f+12&&Mathf.Abs(z)<7)continue;yield return new Vector3(x,ceiling-.32f,z);}
        }
        void Awake()
        {
            for(int i=0;i<lamps.Length;i++)
            {var light=new GameObject("Occupied floor practical light").AddComponent<Light>();light.transform.SetParent(transform,false);light.type=LightType.Point;light.range=16;light.intensity=26;light.color=new Color(.82f,.91f,1);light.shadows=LightShadows.None;light.enabled=false;lamps[i]=light;}
        }
        void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.3f;var game=GameDirector.Instance;var world=FourCityWorld.Instance;
            if(!game||!game.Ready||!world)return;var eye=game.Player.transform.position;candidates.Clear();
            foreach(var venue in world.Facilities)
            {
                if(venue.floorCount<1||venue.Definition.kind==VenueKind.Cinema)continue;
                var local=venue.transform.InverseTransformPoint(eye);int floor=Mathf.FloorToInt((local.y+.2f)/venue.floorHeight);
                if(Mathf.Abs(local.x)>venue.roomWidth*.5f||Mathf.Abs(local.z)>venue.roomDepth*.5f||floor<0||floor>=venue.floorCount)continue;
                foreach(var at in Positions(venue.roomWidth,venue.roomDepth,(floor+1)*venue.floorHeight))candidates.Add(venue.transform.TransformPoint(at));
                break;
            }
            candidates.Sort((a,b)=>(a-eye).sqrMagnitude.CompareTo((b-eye).sqrMagnitude));ActiveLights=Mathf.Min(candidates.Count,lamps.Length);
            for(int i=0;i<lamps.Length;i++){lamps[i].enabled=i<ActiveLights;if(i<ActiveLights)lamps[i].transform.position=candidates[i];}
        }
    }
}

using NUnit.Framework;
using UnityEngine;
namespace AfterSignal.Tests
{
    public sealed class CityNetworkTests
    {
        [Test] public void SignalCycleNeverPermitsConflictingTrafficOrCrossing()
        {
            for(float t=0;t<132;t+=.05f){int east=CityRoadNetwork.SignalAt(t,true),north=CityRoadNetwork.SignalAt(t,false);Assert.IsFalse(east>0&&north>0,"Conflicting greens/yellows at "+t);if(CityRoadNetwork.WalkAt(t)){Assert.AreEqual(0,east);Assert.AreEqual(0,north);}}
        }
        [Test] public void EveryTrafficLoopClosesAndRemainsInsideRoadPavement()
        {
            for(int c=0;c<5;c++)for(int r=0;r<4;r++){var path=CityRoadNetwork.TrafficLoop(c,r);Assert.AreEqual(28,path.Length);for(int i=0;i<path.Length;i++){var a=path[i];var b=path[(i+1)%path.Length];for(int sample=0;sample<=10;sample++){var p=Vector3.Lerp(a,b,sample/10f);var j=CityRoadNetwork.NearestJunction(p);Assert.LessOrEqual(Mathf.Min(Mathf.Abs(p.x-j.x),Mathf.Abs(p.z-j.z)),10.01f,"Lane leaves street "+p);}}}
        }
    }
}

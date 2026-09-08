using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    // Save games and old campaign exits share the same continuous hometown now.
    public sealed class RegionalOrigin:MonoBehaviour
    {
        IEnumerator Start()
        {
            var g=GameDirector.Instance;
            while(g&&(!g.Ready||g.Blocked))yield return null;
            if(g&&g.stage==StageId.Haven)CivicWorld.Travel(g,StageId.Haven,new Vector3(20,.15f,-10));
        }
    }
}

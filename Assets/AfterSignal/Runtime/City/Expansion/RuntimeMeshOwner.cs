using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RuntimeMeshOwner:MonoBehaviour {public Mesh mesh;void OnDestroy(){if(mesh)Destroy(mesh);}}
}

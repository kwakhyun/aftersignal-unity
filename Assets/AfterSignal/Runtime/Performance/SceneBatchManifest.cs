using UnityEngine;

namespace AfterSignal
{
    public sealed class SceneBatchManifest : MonoBehaviour
    {
        public MeshRenderer[] sources;
        public GameObject[] generated;
        public int originalRenderers, batches;
    }
}

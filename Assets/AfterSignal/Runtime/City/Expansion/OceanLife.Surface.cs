using UnityEngine;using UnityEngine.Rendering;
namespace AfterSignal
{
    public sealed partial class OceanLife
    {
        Mesh coastalSurface;
        void EnsureCoastalSurface()
        {
            var sample=new Vector3(1200,Surface,-1300);
            foreach(var r in FindObjectsByType<MeshRenderer>())
                if(r.sharedMaterial&&r.sharedMaterial.shader.name=="AfterSignal/CoastalWater"&&r.bounds.min.x<sample.x&&r.bounds.max.x>sample.x&&r.bounds.min.z<sample.z&&r.bounds.max.z>sample.z)return;
            // Older coastal prefabs omit their ocean mesh. Join them to the northern edge of
            // Nova's existing water; land naturally occludes the surface along the coastline.
            const int cols=110,rows=87;var vertices=new Vector3[(cols+1)*(rows+1)];var triangles=new int[cols*rows*6];int index=0;
            for(int z=0;z<=rows;z++)for(int x=0;x<=cols;x++)vertices[z*(cols+1)+x]=new Vector3(x*20,Surface,-2190+z*20);
            for(int z=0;z<rows;z++)for(int x=0;x<cols;x++){int a=z*(cols+1)+x;triangles[index++]=a;triangles[index++]=a+cols+1;triangles[index++]=a+1;triangles[index++]=a+1;triangles[index++]=a+cols+1;triangles[index++]=a+cols+2;}
            coastalSurface=new Mesh{name="Continuous coastal water"};coastalSurface.vertices=vertices;coastalSurface.triangles=triangles;coastalSurface.RecalculateNormals();coastalSurface.RecalculateBounds();
            var water=new GameObject("Restored coastal water",typeof(MeshFilter),typeof(MeshRenderer));water.transform.SetParent(transform,false);water.GetComponent<MeshFilter>().sharedMesh=coastalSurface;
            var renderer=water.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/Ocean");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
}

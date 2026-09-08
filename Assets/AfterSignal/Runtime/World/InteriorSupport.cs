using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(950)]
    public sealed class InteriorSupport:MonoBehaviour
    {
        GameDirector game;BoxCollider foundation;Vector3 lastSafe;float check;
        public static void Install(GameDirector g)
        {
            if(!CivicWorld.Interior(g.stage))return;
            var support=g.gameObject.AddComponent<InteriorSupport>();support.game=g;support.Create();
        }
        void Create()
        {
            var floor=new GameObject("Interior structural collision floor",typeof(BoxCollider));floor.layer=0;floor.transform.SetParent(transform,false);
            foundation=floor.GetComponent<BoxCollider>();Resize();
            if(game.stage==StageId.Residence)
            {
                // Keep the 3.6m elevator shaft open through the sixth-floor safety slab.
                Slab(new Vector3(18.6f,21.75f,0),new Vector3(43.2f,.5f,30));
                Slab(new Vector3(47.9f,21.75f,0),new Vector3(8.2f,.5f,30));
                Slab(new Vector3(42,21.75f,-8.4f),new Vector3(3.6f,.5f,13.2f));
                Slab(new Vector3(42,21.75f,8.4f),new Vector3(3.6f,.5f,13.2f));
            }
            lastSafe=CivicWorld.SafeSpawn(game.stage,game.spawn);
            Physics.SyncTransforms();
        }
        void Slab(Vector3 center,Vector3 size)
        {
            var room=new GameObject("Apartment slab outside elevator shaft",typeof(BoxCollider));room.transform.SetParent(transform,false);
            room.GetComponent<BoxCollider>().center=center;room.GetComponent<BoxCollider>().size=size;
        }
        void Resize(){foundation.center=new Vector3(game.stageLength*.5f,-.55f,0);foundation.size=new Vector3(game.stageLength+2,1.1f,game.halfDepth*2+2);}
        void LateUpdate()
        {
            if(!game.Ready||!game.Player.Controller.enabled)return;
            Resize();
            var p=game.Player.transform.position;
            if(Time.time>=check)
            {
                check=Time.time+.2f;
                if(game.Player.Grounded&&p.y>=0)lastSafe=p;
            }
            // A continuous independent collision floor stays active when interior themes change.
            if(p.y<-.35f){game.Player.Respawn(lastSafe.y>=0?lastSafe:new Vector3(5,.12f,0),false);game.CameraRig.Snap();}
        }
    }
}

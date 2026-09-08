using UnityEngine;
namespace AfterSignal
{
    public static class CompactHome
    {
        public static readonly Vector3 Spawn=new(8,.15f,-3),Exit=RegionalCatalog.HomeQuarter+new Vector3(0,.15f,-9);
        public static void Prepare(GameDirector game)
        {
            if(game.stage!=StageId.Residence)return;
            foreach(var root in game.gameObject.scene.GetRootGameObjects())if(root.name.StartsWith("WORLD")){root.SetActive(false);Object.Destroy(root);}
            var home=new GameObject("Seoha / small ground-floor home").transform;var g=new CityGeometry(home);
            game.spawn=game.checkpoint=Spawn;game.stageLength=17;game.halfDepth=6;
            g.Box("Home floor",new(8,-.09f,0),new(16,.18f,12),"Pavement",true);
            for(int s=-1;s<=1;s+=2){g.Box("House side",new(8+s*8,1.8f,0),new(.2f,3.6f,12),"SlumPlaster",true);g.Box("Front wall",new(8+s*4.6f,1.8f,-6),new(6.8f,3.6f,.2f),"SlumPatina",true);}
            g.Box("Home rear wall",new(8,1.8f,6),new(16,3.6f,.2f),"SlumPlaster",true);g.Box("Weather roof",new(8,3.7f,0),new(17,.2f,13),"Steel",true);
            g.Box("Bed frame",new(12,.35f,2.5f),new(2,.7f,3.4f),"SlumPatina",true);g.Box("Bedding",new(12,.8f,2.5f),new(1.9f,.2f,3.3f),"SeatBlue");
            var mattress=new GameObject("Mattress").transform;mattress.SetParent(home,false);mattress.position=new(12,.85f,2.5f);
            g.Box("Seoha wardrobe",new(14,1.25f,4.8f),new(2.2f,2.5f,1.1f),"FutureCarbon",true);g.Box("Kitchen cupboards",new(3,1,4.8f),new(4,2,1.2f),"SlumPatina",true);g.Box("Cooking surface",new(3,2.05f,4.8f),new(4.1f,.1f,1.3f),"FutureSilver");
            g.Box("Dining table",new(4,.8f,0),new(2.4f,.16f,1.4f),"Wood",true);FourCityArchitecture.Bench(g,new(4,0,-1.3f));g.Box("Terminal desk",new(8,.85f,4.5f),new(2.6f,.12f,1.2f),"Steel",true);g.Box("Signal terminal",new(8,1.5f,4.8f),new(1.3f,.8f,.1f),"NeonCyan");
            foreach(float x in new[]{3f,5f})foreach(float z in new[]{-.5f,.5f})g.Box("Table leg",new(x,.37f,z),new(.1f,.74f,.1f),"Steel",true);
            foreach(float x in new[]{6.9f,9.1f})g.Box("Desk side support",new(x,.4f,4.5f),new(.14f,.8f,1.1f),"Steel",true);
            g.Box("Pillow",new(12,.97f,3.7f),new(1.35f,.16f,.5f),"FutureCeramic");g.Box("Folded blanket",new(12,.97f,1.3f),new(1.9f,.12f,.7f),"SeatCoral");
            for(int i=0;i<4;i++){g.Box("Cupboard panel",new(1.5f+i,1,4.17f),new(.92f,1.85f,.05f),i%2==0?"SlumPlaster":"SlumPatina");g.Box("Cupboard handle",new(1.8f+i,1.1f,4.11f),new(.07f,.22f,.05f),"FutureSilver");}
            for(int i=0;i<2;i++){g.Cylinder(new(3.5f+i,2.13f,4.8f),.27f,.025f,"FutureCarbon",16);g.Box("Wardrobe door trim",new(13.45f+i*1.1f,1.25f,4.22f),new(1.04f,2.42f,.04f),"Steel");g.Box("Wardrobe grip",new(13.9f+i*.2f,1.25f,4.15f),new(.04f,.3f,.07f),"FutureSilver");}
            g.Box("Kitchen shelf",new(3,2.85f,5.35f),new(3.8f,.1f,.9f),"Wood",true);for(int i=0;i<5;i++)g.Cylinder(new(1.6f+i*.65f,2.91f,5.3f),.17f,.3f,"FutureCeramic",12);
            g.Box("Curtained window frame",new(.15f,2,1),new(.06f,1.9f,3.3f),"Steel");g.Box("Curtained window",new(.2f,2,1),new(.04f,1.7f,3.1f),"Glass");for(int i=0;i<10;i++)g.Box("Window blind slat",new(.26f,1.24f+i*.17f,1),new(.08f,.09f,3.1f),"SlumPlaster");
            g.Box("Warm ceiling light",new(8,3.5f,0),new(2,.08f,.25f),"NeonWarm");g.Finish();
            Point(home,"침대 · 취침",new(10.6f,1,2),InteractionKind.Sleep);Point(home,"옷장 · 의상 변경",new(13.5f,1,3.5f),InteractionKind.Wardrobe);
            var exit=Point(home,"현관 · 새벽 골목으로",new(8,1,-5),InteractionKind.FacilityTravel);exit.destination=StageId.UrbanCity;exit.hasArrival=true;exit.arrival=Exit;
            var light=home.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=19;light.intensity=12;light.color=new Color(.85f,.7f,.5f);home.gameObject.AddComponent<CompactHomeLamp>();
            Physics.SyncTransforms();
        }
        static InteractionPoint Point(Transform root,string title,Vector3 p,InteractionKind kind){var t=new GameObject(title).transform;t.SetParent(root,false);t.localPosition=p;var point=t.gameObject.AddComponent<InteractionPoint>();point.kind=kind;point.title=title;point.radius=2.5f;return point;}
    }
    public sealed class CompactHomeLamp:MonoBehaviour{void Start(){var child=new GameObject("Room light").transform;child.SetParent(transform,false);child.localPosition=new(8,3,0);var a=GetComponent<Light>();var b=child.gameObject.AddComponent<Light>();b.type=a.type;b.range=a.range;b.intensity=a.intensity;b.color=a.color;Destroy(a);}}
}

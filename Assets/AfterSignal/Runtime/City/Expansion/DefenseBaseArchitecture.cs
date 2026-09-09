using UnityEngine;
namespace AfterSignal
{
    public static class DefenseBaseArchitecture
    {
        public static Transform Build(Transform parent,Vector3 at,bool navy)
        {
            var root=new GameObject(navy?"해군 · 해협 통합기지":"공군 · 루멘 항공작전기지").transform;root.SetParent(parent,false);root.position=at;
            var b=new CityGeometry(root);float width=navy?150:200,depth=navy?105:160;
            b.Box("Reinforced service apron",new(0,-.32f,10),new(width,.55f,depth),"DefenseDeck",true);
            for(int i=1;i<width/12;i++)b.Box("Apron expansion joint",new(-width*.5f+i*12,-.035f,10),new(.04f,.01f,depth),"DarkMetal");
            for(int i=1;i<depth/12;i++)b.Box("Apron cross joint",new(0,-.034f,10-depth*.5f+i*12),new(width,.01f,.04f),"DarkMetal");
            Command(b,new(-width*.3f,0,12),navy);
            Gate(b,new(0,0,-depth*.5f+12),navy?"NAVAL COMMAND / 해군기지":"AIR OPERATIONS / 공군기지");
            for(int s=-1;s<=1;s+=2)for(int i=0;i<8;i++)
            {
                float z=-depth*.5f+18+i*(depth-22)/8;
                b.Box("Perimeter security post",new(s*(width*.5f-2),2,z),new(.3f,4,.3f),"Metal",true);
                b.Box("Perimeter fence panel",new(s*(width*.5f-2),2,z+(depth-22)/16),new(.1f,2.8f,(depth-22)/8),"Glass",true);
            }
            if(navy)
            {
                for(int pier=0;pier<2;pier++)
                {
                    float x=pier==0?12:61;b.Box("Destroyer service quay",new(x,-.4f,-90),new(13,.65f,116),"DefenseDeck",true);
                    for(int j=0;j<8;j++){b.Box("Mooring bollard",new(x+(j%2==0?-5:5),.45f,-47-j/2*27),new(.5f,.9f,.5f),"Metal",true);b.Box("Quay safety stripe",new(x+(j%2==0?-6:6),.02f,-47-j/2*27),new(.18f,.025f,8),"DistrictIvory");}
                    var crane=b.Box("Dock crane mast",new(x,9,-50),new(1,18,1),"Metal",true);b.Box("Cargo crane jib",new(x+6,17,-50),new(14,.65f,.65f),"Metal");b.Box("Cargo hoist cable",new(x+11,10,-50),new(.08f,13,.08f),"DarkMetal");
                }
                Hangar(b,new(32,0,42),"함정 정비창 / MARINE ENGINEERING",36,26,12);
                for(int i=0;i<8;i++){b.Box("Secured naval supplies",new(-8+i%4*8,1.35f,28+i/4*10),new(6,2.7f,7),i%2==0?"DistrictBlue":"Metal",true);}
            }
            else
            {
                for(int i=0;i<3;i++)Hangar(b,new(-28+i*44,0,54),"HANGAR 0"+(i+1),39,42,17);
                b.Box("Flight line",new(34,.025f,-4),new(126,.035f,44),"DarkMetal",true);
                for(int i=0;i<16;i++)b.Box("Flight line centre stripe",new(-24+i*7.6f,.05f,-4),new(3,.012f,.24f),"DistrictIvory");
                for(int i=0;i<5;i++)for(int s=-1;s<=1;s+=2)b.Box("Taxiway marker",new(-28+i*28,.14f,-4+s*20),new(.35f,.2f,.35f),"NeonCyan");
                b.Box("Control tower stem",new(-83,10,53),new(7,20,7),"Concrete",true);
                b.Box("Tower observation cabin",new(-83,21,53),new(15,3,13),"Glass");b.Box("Tower cantilever roof",new(-83,23,53),new(17,.55f,15),"Metal");
                for(int i=0;i<5;i++)b.Box("Tower antenna",new(-88+i*2.5f,25,53),new(.1f,4+i%2*2,.1f),"Chrome");
                for(int i=0;i<3;i++){b.Cylinder(new(53+i*13,0,75),5,6,"Metal");b.Box("Fuel tank collision",new(53+i*13,3,75),new(9,6,9),"Metal",true);}
            }
            b.Finish();return root;
        }
        static void Command(CityGeometry b,Vector3 p,bool navy)
        {
            b.Box("Command floor",p+new Vector3(0,-.05f,0),new(37,.18f,36),"Metal",true);
            b.Box("Command rear wall",p+new Vector3(0,3.5f,18),new(37,7,.45f),"NovaConcrete",true);
            for(int s=-1;s<=1;s+=2){b.Box("Command side wall",p+new Vector3(s*18.5f,3.5f,0),new(.45f,7,36),"NovaConcrete",true);b.Box("Lobby front wing",p+new Vector3(s*11.5f,3.5f,-18),new(14,7,.45f),"Metal",true);b.Box("Lobby glazing",p+new Vector3(s*11.5f,4,-18.3f),new(11,2.5f,.12f),"Glass");}
            b.Box("Command roof",p+new Vector3(0,7.2f,0),new(40,.45f,39),"Metal",true);
            b.Box("Room partition",p+new Vector3(0,2,8),new(24,4,.22f),"Glass",true);
            for(int i=0;i<5;i++){b.Box("Operator desk",p+new Vector3(-12+i*6,1.1f,12),new(3.6f,.15f,1.8f),"Metal",true);b.Box("Operations display",p+new Vector3(-12+i*6,1.8f,12.4f),new(2,.95f,.12f),"DistrictBlue");b.Box("Duty chair",p+new Vector3(-12+i*6,.5f,10),new(.9f,1,.9f),"DarkMetal",true);}
            b.Box("Briefing table",p+new Vector3(0,1,-3),new(8,.25f,4),"DarkMetal",true);
            b.Box("Mission chart",p+new Vector3(0,1.16f,-3),new(7,.03f,3),"DistrictBlue");
            for(int i=0;i<6;i++)b.Box("Briefing chair",p+new Vector3(-3+i%3*3,.55f,i<3?-6:0),new(.9f,1.1f,.9f),"Metal",true);
            b.Sign(navy?"해협 관제 / 함대 작전실":"비행 통제 / 조종사 브리핑",p+new Vector3(0,5.3f,-18.5f),.3f);
        }
        static void Hangar(CityGeometry b,Vector3 p,string label,float w,float d,float h)
        {
            b.Box("Hangar service floor",p+new Vector3(0,-.08f,0),new(w,.2f,d),"Metal",true);
            b.Box("Hangar rear wall",p+new Vector3(0,h*.5f,d*.5f),new(w,h,.55f),"Concrete",true);
            for(int s=-1;s<=1;s+=2){b.Box("Hangar side wall",p+new Vector3(s*w*.5f,h*.5f,0),new(.5f,h,d),"Metal",true);for(int j=0;j<5;j++)b.Box("Exposed frame",p+new Vector3(s*(w*.5f-.4f),h*.5f,-d*.45f+j*d*.23f),new(.55f,h,.65f),"Chrome");}
            b.Box("Hangar roof",p+new Vector3(0,h,0),new(w+2,.6f,d+2),"Metal",true);
            b.Box("Retracted overhead door",p+new Vector3(0,h-1.3f,-d*.5f),new(w,2.5f,.5f),"DarkMetal");
            for(int i=0;i<6;i++)b.Box("Roof girder",p+new Vector3(0,h-.7f,-d*.4f+i*d*.16f),new(w,.4f,.45f),"Chrome");
            b.Sign(label,p+new Vector3(0,h-1,-d*.5f-.4f),.3f);
            for(int i=0;i<3;i++){b.Box("Maintenance bench",p+new Vector3(-w*.37f,1,d*.25f-i*4),new(3,.2f,2),"Metal",true);b.Box("Tool cabinet",p+new Vector3(w*.4f,1.3f,d*.3f-i*5),new(2,2.6f,2),"DistrictBlue",true);}
        }
        static void Gate(CityGeometry b,Vector3 p,string title)
        {
            for(int s=-1;s<=1;s+=2){b.Box("Guardhouse",p+new Vector3(s*11,1.7f,0),new(5,3.4f,5),"DarkMetal",true);b.Box("Guardhouse window",p+new Vector3(s*11,2,-2.6f),new(3.8f,1.4f,.08f),"Glass");b.Box("Security scan arch",p+new Vector3(s*6,3.5f,0),new(.4f,7,.4f),"Metal",true);}
            b.Box("Checkpoint overhead",p+new Vector3(0,7,0),new(13,.5f,.7f),"Metal");b.Sign(title,p+new Vector3(0,5.5f,-.5f),.29f);
        }
    }
}

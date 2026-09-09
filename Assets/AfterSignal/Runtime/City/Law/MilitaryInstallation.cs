using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryInstallation:MonoBehaviour
    {
        public static readonly Vector3 Gate=new Vector3(435,.1f,739);
        Transform barrier;BoxCollider blocker;float warning,inspection;PoliceOfficer[] guards=new PoliceOfficer[2];
        IEnumerator Start()
        {
            while(!UrbanSimulation.Instance||!GameDirector.Instance.Ready)yield return null;
            var root=new GameObject("군부대 위병소").transform;root.position=Gate;
            var booth=ResidentialWorld.Box(root,"Guardhouse",new Vector3(-7,1.6f,0),new Vector3(4,3.2f,4),"Concrete");
            WorldGeometry.Part(root,"Guardhouse window",new Vector3(-7,1.9f,-2.02f),new Vector3(3,.8f,.04f),"Glass");
            WorldGeometry.Part(root,"Guardhouse canopy",new Vector3(-3,3.4f,0),new Vector3(14,.25f,5),"DarkMetal");
            barrier=new GameObject("Controlled gate arm").transform;barrier.SetParent(root,false);barrier.localPosition=new Vector3(-3,1.1f,0);
            var arm=ResidentialWorld.Box(barrier,"Red white barrier",new Vector3(5,0,0),new Vector3(10,.25f,.3f),"DistrictIvory");blocker=arm.GetComponent<BoxCollider>();
            for(int i=0;i<5;i++)WorldGeometry.Part(barrier,"Barrier red stripe",new Vector3(1+i*2,0,-.16f),new Vector3(.8f,.27f,.02f),"RedFX");
            for(int i=0;i<2;i++){var at=Gate+new Vector3(i==0?-2:9,0,-3);if(CityGangWar.FindGround(at,out var safe)){guards[i]=PoliceOfficer.Create(WantedSystem.Instance,safe,4,i);guards[i].Ambient=true;guards[i].name="위병소 경계병";guards[i].Body.military=true;guards[i].Body.police=false;GarrisonSupport.Register(guards[i].Body,Gate);PeopleArt.Attach(guards[i].gameObject,"Soldier");}}
            MilitaryArmory.Build(transform,new Vector3(384,.1f,792));
            for(int row=0;row<2;row++)for(int col=0;col<4;col++)
            {
                var at=new Vector3(290+col*20,.1f,777+row*31);var v=UrbanSimulation.Instance.Spawn(at,false,(int)CityVehicleType.Tank);v.name="군부대 대기 전차";
                yield return null;
            }
            for(int i=0;i<4;i++){UrbanSimulation.Instance.Spawn(new Vector3(250+i*42,.2f,920),false,(int)CityVehicleType.Fighter);UrbanSimulation.Instance.Spawn(new Vector3(432+i*24,.2f,856),false,(int)CityVehicleType.CombatHelicopter);yield return null;}
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!barrier)return;
            var p=g.Player.transform.position;bool near=(p-Gate).sqrMagnitude<18*18;
            inspection=near&&WantedSystem.Level==0?inspection+Time.deltaTime:0;
            bool authorized=inspection>4;
            barrier.localRotation=Quaternion.RotateTowards(barrier.localRotation,Quaternion.Euler(0,0,near&&authorized?80:0),Time.deltaTime*65);
            if(blocker)blocker.enabled=barrier.localEulerAngles.z<55;
            if(near&&!authorized&&Time.time>warning){warning=Time.time+8;foreach(var guard in guards)if(guard&&guard.Body.Alive)NpcSpeech.Say(guard,"통제 구역입니다. 잠시 정지하세요. 신원을 조회하겠습니다.",4);g.Toast("위병소 · 정문에서 4초간 신원 조회 · 수배 중 출입 금지",3);}
        }
    }
}

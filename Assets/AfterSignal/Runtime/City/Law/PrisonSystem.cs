using UnityEngine;
namespace AfterSignal
{
    public sealed class PrisonSystem:MonoBehaviour
    {
        public static readonly Vector3 Cell=new Vector3(1156,.2f,800);
        public static readonly Vector3 ReleasePoint=new Vector3(1180,.2f,740);
        const string SaveKey="AFTERSIGNAL.Unity.JailSeconds";
        public static PrisonSystem Instance{get;private set;}
        public bool Jailed=>remaining>0;
        public float Remaining=>remaining;
        float remaining,surrender,save;
        GameDirector game;
        public static bool Capture(GameDirector g)
        {
            if(WantedSystem.Level<=0||!CivicWorld.Exploration(g.stage))return false;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current){sim.Current.speed=0;sim.EmergencyExit(sim.Current);}
            int level=WantedSystem.Level;WantedSystem.Clear("");
            if(!LifeState.SuppressSave)PlayerPrefs.SetFloat(SaveKey,45+level*8);
            g.Player.Respawn(Cell,true);LifeState.Hours+=2;
            if(g.stage!=StageId.UrbanCity)CivicWorld.Travel(g,StageId.UrbanCity,Cell);
            else{if(!Instance)Instance=g.gameObject.AddComponent<PrisonSystem>();Instance.remaining=45+level*8;g.CameraRig.Snap();}
            g.Toast("체포 · 애프터라이트 교도소로 이송되었습니다",6);
            return true;
        }
        void Awake(){Instance=this;game=GameDirector.Instance;remaining=PlayerPrefs.GetFloat(SaveKey,0);}
        void Start(){if(remaining>0&&game&&game.stage==StageId.UrbanCity)game.Player.Respawn(Cell);}
        public static void BeforeInput(ref ControlFrame input,float dt)
        {
            if(!Instance)return;
            if(Instance.Jailed){input.attack=input.grapple=input.skill=false;input.weapon=-1;}
            if(WantedSystem.Level>0&&input.surrender)
            {
                bool near=false;foreach(var a in WorldActor.All)if(a&&a.police&&a.Alive&&(a.transform.position-Instance.game.Player.transform.position).sqrMagnitude<64){near=true;break;}
                Instance.surrender=near?Instance.surrender+dt:0;
                if(Instance.surrender>1.4f){Instance.surrender=0;Capture(Instance.game);}
            }
            else Instance.surrender=0;
        }
        public void Release(bool bail)
        {
            if(bail&&!LifeState.Spend(450)){game.Toast("보석금 450 C가 필요합니다.");return;}
            remaining=0;if(!LifeState.SuppressSave)PlayerPrefs.DeleteKey(SaveKey);
            game.Player.Respawn(ReleasePoint);game.CameraRig.Snap();game.Toast("출소 · 개인 물품을 돌려받았습니다",5);
        }
        void Update()
        {
            if(!game||!game.Ready||game.Blocked||!Jailed)return;
            remaining=Mathf.Max(0,remaining-Time.deltaTime);
            if(remaining<=0){Release(false);return;}
            save+=Time.deltaTime;
            if(save>5){save=0;if(!LifeState.SuppressSave)PlayerPrefs.SetFloat(SaveKey,remaining);}
            if(Vector3.Distance(game.Player.transform.position,Cell)>24){remaining=0;if(!LifeState.SuppressSave)PlayerPrefs.DeleteKey(SaveKey);WantedSystem.Report(85,game.Player.transform.position);game.Toast("교도소 탈주 · 수배가 발령되었습니다");}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}

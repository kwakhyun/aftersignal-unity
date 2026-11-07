using UnityEngine;
namespace AfterSignal
{
    public sealed class PrisonSystem:MonoBehaviour
    {
        public static readonly Vector3 Cell=new Vector3(1156,.2f,800);
        public static readonly Vector3 ReleasePoint=new Vector3(1180,.2f,740);
        const string SaveKey="AFTERSIGNAL.Unity.JailSeconds",BailKey="AFTERSIGNAL.Unity.JailBail";
        const float CellRadius=6,EscapeRadius=24;
        public static PrisonSystem Instance{get;private set;}
        // A sentence from an earlier session or scene is still owed; Continue and respawn resume it in the cell.
        public static bool Sentenced=>Instance&&Instance.Jailed||!LifeState.SuppressSave&&PlayerPrefs.GetFloat(SaveKey,0)>0;
        public bool Jailed=>remaining>0;
        public float Remaining=>remaining;
        public int Bail=>bailAmount;
        float remaining,surrender,save;int bailAmount;
        // The escape check only arms once Seoha has actually been seated inside the cell.
        bool confined;
        GameDirector game;
        static int DefaultBail=>Mathf.RoundToInt(GameTuning.Active.bailBase*DifficultySettings.Penalty);
        public static bool Capture(GameDirector g)
        {
            if(!g||g.Player.Health<=0||g.Dead||WantedSystem.Level<=0||!CivicWorld.Exploration(g.stage))return false;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current){sim.Current.speed=0;sim.EmergencyExit(sim.Current);}
            var t=GameTuning.Active;int level=WantedSystem.Level;float heat=LifeState.Heat;
            float sentence=(t.jailBase+level*t.jailPerLevel)*DifficultySettings.Penalty;
            int bail=Mathf.RoundToInt((t.bailBase+heat*t.bailPerHeat)*DifficultySettings.Penalty);
            WantedSystem.Clear("");
            int seized=ConfiscateReserve();
            if(!LifeState.SuppressSave){PlayerPrefs.SetFloat(SaveKey,sentence);PlayerPrefs.SetInt(BailKey,bail);PlayerPrefs.Save();}
            g.Player.Respawn(Cell,true);LifeState.Hours+=2;
            if(g.stage!=StageId.UrbanCity)CivicWorld.Travel(g,StageId.UrbanCity,Cell);
            else{if(!Instance)Instance=g.gameObject.AddComponent<PrisonSystem>();Instance.remaining=sentence;Instance.bailAmount=bail;Instance.confined=true;g.CameraRig.Snap();}
            g.Toast("체포 · 애프터라이트 교도소로 이송되었습니다"+(seized>0?" · 예비 탄약 "+seized+"발 압수":"")+" · 보석금 "+bail.ToString("N0")+" C",6);
            return true;
        }
        // Booking seizes firearm reserve ammunition (rifles, launcher, shotgun); the loaded magazine stays.
        // ArmoryInventory has no direct API for this, so it is composed from Consume/Reload with one save.
        static int ConfiscateReserve()
        {
            int seized=0;bool suppress=LifeState.SuppressSave;LifeState.SuppressSave=true;
            try
            {
                for(int id=0;id<ArmoryInventory.Items.Length;id++)
                {
                    var item=ArmoryInventory.Items[id];if(item.slot<3||item.slot==4||item.magazine<=0||!ArmoryInventory.Owns(id))continue;
                    int keep=Mathf.Min(ArmoryInventory.Rounds(id),item.magazine),before=ArmoryInventory.Reserve(id);
                    for(int guard=0;ArmoryInventory.Reserve(id)>0&&guard<100000;guard++)
                    {
                        if(ArmoryInventory.Rounds(id)>=item.magazine)ArmoryInventory.Consume(id);
                        if(!ArmoryInventory.Reload(id))break;
                        while(ArmoryInventory.Rounds(id)>keep&&ArmoryInventory.Consume(id)){}
                    }
                    seized+=Mathf.Max(0,before-ArmoryInventory.Reserve(id));
                }
            }
            finally{LifeState.SuppressSave=suppress;}
            if(seized>0)ArmoryInventory.Save();
            return seized;
        }
        public static void SaveSentence(){if(Instance&&Instance.Jailed)Instance.Persist();}
        public static void ResetSentence(){if(Instance){Instance.remaining=0;Instance.confined=false;}Forget();}
        static void Forget(){if(!LifeState.SuppressSave){PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.DeleteKey(BailKey);}}
        void Persist(){if(!LifeState.SuppressSave){PlayerPrefs.SetFloat(SaveKey,remaining);PlayerPrefs.SetInt(BailKey,bailAmount);}}
        void Awake(){Instance=this;game=GameDirector.Instance;remaining=LifeState.SuppressSave?0:PlayerPrefs.GetFloat(SaveKey,0);bailAmount=PlayerPrefs.GetInt(BailKey,0);if(bailAmount<=0)bailAmount=DefaultBail;}
        void Start(){if(Jailed&&game&&game.stage==StageId.UrbanCity&&!game.Title)Seat();}
        void Seat(){game.Player.Rope.Release();game.Player.Respawn(Cell);game.CameraRig.Snap();confined=true;}
        public static void BeforeInput(ref ControlFrame input,float dt)
        {
            if(!Instance)return;
            if(Instance.Jailed){input.attack=input.grapple=input.skill=false;input.weapon=-1;}
            if(WantedSystem.Level>0&&input.surrender)
            {
                input.attack=input.skill=input.grapple=false;input.move=Vector2.zero;
                bool near=false;foreach(var a in WorldActor.All)if(a&&a.police&&a.Alive&&(a.transform.position-Instance.game.Player.transform.position).sqrMagnitude<64){near=true;break;}
                Instance.surrender=near?Instance.surrender+dt:0;
                if(Instance.surrender>1.4f){Instance.surrender=0;Capture(Instance.game);}
            }
            else Instance.surrender=0;
        }
        public void Release(bool bail)
        {
            if(bail&&!LifeState.Spend(bailAmount)){game.Toast("보석금 "+bailAmount.ToString("N0")+" C가 필요합니다.");return;}
            remaining=0;confined=false;Forget();
            game.Player.Respawn(ReleasePoint);game.CameraRig.Snap();game.Toast((bail?"보석 출소":"출소")+" · 개인 물품을 돌려받았습니다",5);
        }
        void Update()
        {
            if(!game||!game.Ready||game.Blocked||!Jailed)return;
            // A sentence carried into another scene (old save, interior) is served at the prison, never voided.
            if(game.stage!=StageId.UrbanCity){CivicWorld.Travel(game,StageId.UrbanCity,Cell);return;}
            float distance=Vector3.Distance(game.Player.transform.position,Cell);
            if(!confined){if(distance<CellRadius)confined=true;else{Seat();return;}}
            remaining=Mathf.Max(0,remaining-Time.deltaTime);
            if(remaining<=0){Release(false);return;}
            save+=Time.deltaTime;
            if(save>5){save=0;Persist();}
            if(distance>EscapeRadius){remaining=0;confined=false;Forget();WantedSystem.Report(85,game.Player.transform.position);game.Toast("교도소 탈주 · 수배가 발령되었습니다");}
        }
        void OnDestroy(){if(Jailed)Persist();if(Instance==this)Instance=null;}
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class IncursionCampaignSmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();GameDirector game;string output;float start;bool done;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-incursion-smoke")&&!FindAnyObjectByType<IncursionCampaignSmoke>()){GameDirector.SkipTitle=true;new GameObject("Incursion campaign essential checks").AddComponent<IncursionCampaignSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;start=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/IncursionCampaign/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(text+"\n"+trace);Save();}}
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("INCURSION "+yes+" / "+label);Save();}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.CameraRig.enabled=false;game.enabled=false;game.Player.enabled=false;LifeState.Hours=15;
            WantedSystem.Clear("");WantedSystem.Instance.enabled=false;CityChronicle.Instance.enabled=false;RiftIncursion.Instance.enabled=false;
            yield return new WaitForSeconds(1);
            if(Environment.GetCommandLineArgs().Contains("-incursion-ui"))
            {
                var story=CityChronicle.Instance;story.ResetProgress();story.enabled=true;var point=story.CurrentStep.position;game.Player.Respawn(point+Vector3.back*20);yield return new WaitForSeconds(6);
                var uiBattle=FindAnyObjectByType<CampaignBattle>();Check(uiBattle&&uiBattle.Remaining>=3,"Combat HUD has live enemy count");
                if(uiBattle)foreach(var label in uiBattle.GetComponentsInChildren<UnityEngine.UI.Text>())Check(label.preferredHeight<=label.rectTransform.rect.height+1,"Complete battle text fits its panel / "+label.transform.parent.name);
                yield return View("CampaignHUD",point+new Vector3(-26,15,-38),point+Vector3.up*2);Finish();yield break;
            }
            var sim=UrbanSimulation.Instance;if(sim.Current)sim.Exit();var at=new Vector3(40,.2f,-270);game.Player.Respawn(at+new Vector3(0,0,-18));
            var car=sim.Spawn(at,false,(int)CityVehicleType.Sedan);car.occupied=false;car.owned=false;car.speed=0;car.InitializeDurability();
            var observer=PoliceOfficer.Create(WantedSystem.Instance,at+Vector3.right*12,1,0);observer.enabled=false;
            WantedSystem.Clear("");bool entered=sim.Enter(car);Check(entered&&sim.Current==car&&WantedSystem.Level==0&&CrimeObservation.Instance.PendingCalls==0,$"Parked car entry with nearby police produces no wanted report / entered={entered}, heat={LifeState.Heat}, calls={CrimeObservation.Instance.PendingCalls}");sim.Exit();
            float hp=car.health;car.CollisionDamage(12,car.transform.position);Check(hp-car.health<car.MaxHealth*.02f,"Ordinary road collision remains a minor repair");
            game.Player.Respawn(at+Vector3.back*40);car.health=car.MaxHealth;BlastDamage.Create(car.transform.position,12,270,TrafficDamageSource.Environment,null,BlastPayload.Missile,car);yield return null;yield return null;Check(car.Wrecked||car.health<=0,"Direct missile destroys an unarmoured sedan");Destroy(observer.gameObject);WantedSystem.Clear("");
            var tank=sim.Spawn(new Vector3(40,.2f,-200),false,(int)CityVehicleType.Tank);tank.InitializeDurability();tank.health=tank.MaxHealth;hp=tank.health;
            BlastDamage.Create(tank.transform.position,12,270,TrafficDamageSource.Environment,null,BlastPayload.Missile,tank);yield return null;yield return null;Check(tank.health>0&&tank.health<hp*.7f,"Tank armour survives one missile with substantial damage");
            var plane=sim.Spawn(new Vector3(40,70,-200),false,(int)CityVehicleType.Airliner);plane.InitializeDurability();plane.health=plane.MaxHealth;hp=plane.health;
            BlastDamage.Create(plane.transform.position,12,270,TrafficDamageSource.Environment,null,BlastPayload.Missile,plane);yield return null;yield return null;Check(plane.health>0&&plane.health<hp*.4f,"Airliner is vulnerable to missile warheads despite high base durability");Destroy(plane.gameObject);Destroy(tank.gameObject);
            Camera.main.transform.SetPositionAndRotation(at+new Vector3(-15,7,-25),Quaternion.LookRotation(Vector3.forward));
            bool found=ResponseDispatch.TryOrigin(at,true,false,0,out var origin);Check(found&&Vector3.Distance(at,origin)>=220&&ResponseDispatch.Hidden(origin),"Ground response origin is distant, on ground and out of view / "+ResponseDispatch.LastSearch);
            if(found)
            {
                var route=ResponseDispatch.Route(origin,at);Check(route.Count>2,"Response road graph connects origin to the incident");
                WantedSystem.ConfirmReport(60,at);var transport=TacticalTransport.Create(WantedSystem.Instance,origin,4);var initial=transport.transform.position;yield return new WaitForSeconds(4);
                Check(transport.Deployed==0&&Vector3.Distance(initial,transport.transform.position)>1,"SWAT drives toward scene without immediate nearby disembarkation");transport.Withdraw();WantedSystem.Clear("");
            }
            var titans=new List<RiftCreature>();
            for(int i=0;i<3;i++)
            {
                var titan=RiftCreature.Create(new Vector3(40,.15f,-140+i*50),i);titan.transform.rotation=Quaternion.Euler(0,180,0);titan.enabled=false;titans.Add(titan);yield return null;
                var renderers=titan.GetComponentsInChildren<Renderer>();var bounds=new Bounds(titan.transform.position,Vector3.zero);foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Check(renderers.Length>5&&bounds.size.y>6&&bounds.size.x>4,"Authored articulated giant has vehicle-scale dimensions / "+i);
                var head=titan.GetComponentsInChildren<Transform>().First(t=>t.name=="Cranium");Check(head.position.y>titan.transform.position.y+5.5f,"Imported FBX stands upright with elevated cranium / "+i);
                hp=titan.Body.health;titan.Body.Damage(18,Vector3.zero,TrafficDamageSource.Environment);Check(hp-titan.Body.health<8,"Titan shell resists ordinary rifle fire / "+i);
            }
            game.Player.Respawn(new Vector3(40,.2f,-240));
            yield return View("Titans",new Vector3(23,13,-170),new Vector3(40,5,-140));
            var victim=GameObject.CreatePrimitive(PrimitiveType.Capsule);victim.name="Incursion civilian check";victim.transform.position=titans[0].transform.position+new Vector3(6,1,-4);victim.layer=9;var civilian=victim.AddComponent<WorldActor>();civilian.health=70;
            titans[0].Shockwave(17,100);yield return null;Check(!civilian.Alive&&WantedSystem.Level==0,"Titan shockwave kills nearby civilians without blaming the player");Destroy(victim);
            var aircraft=sim.Spawn(titans[0].transform.position+new Vector3(0,45,-48),false,(int)CityVehicleType.CombatHelicopter);aircraft.occupied=true;aircraft.InitializeDurability();aircraft.health=aircraft.MaxHealth;hp=aircraft.health;
            typeof(RiftCreature).GetField("locked",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(titans[0],aircraft.transform.position+Vector3.up);
            titans[0].FireLaser();yield return null;Check(aircraft.health<hp*.5f,"Titan anti-air laser hits and heavily damages a helicopter");Destroy(aircraft.gameObject);
            foreach(var titan in titans)Destroy(titan.gameObject);
            var incident=RiftIncursion.Instance;incident.Trigger(new Vector3(40,.15f,140));yield return new WaitForSeconds(2);
            var response=FindObjectsByType<MilitaryResponse>().FirstOrDefault(r=>r.Incident==incident);Check(response&&response.VehicleCount==0&&response.SoldierCount==0,"Monster incident waits for military mobilisation instead of instant troops");
            incident.enabled=true;
            foreach(var a in WorldActor.All.ToArray())if(a&&a.monster){a.health=0;}yield return new WaitForSeconds(.4f);incident.enabled=false;
            var chronicle=CityChronicle.Instance;Check(chronicle.Quests.Count(q=>q.main)==30&&chronicle.Quests.Where(q=>q.main).SelectMany(q=>q.steps).All(s=>s.kind=="battle"),"All 30 main operations use combat phases");
            foreach(var q in chronicle.Quests.Where(q=>q.main))
            {var p=q.steps[0].position;bool ground=CityGangWar.FindGround(p,out _);Check(ground,"Main combat area has accessible ground / "+q.id);}
            chronicle.ResetProgress();chronicle.enabled=true;var mission=chronicle.CurrentStep;game.Player.Respawn(mission.position+new Vector3(0,0,-20));
            yield return new WaitForSeconds(6);var battle=FindAnyObjectByType<CampaignBattle>();Check(battle&&battle.Wave==1&&battle.Remaining>=3,"Entering main objective automatically starts live combat");
            yield return View("Campaign",mission.position+new Vector3(-26,15,-38),mission.position+Vector3.up*2);
            for(int phase=0;phase<3;phase++)
            {
                float end=Time.time+50;
                while(chronicle.Progress.tracked=="main01"&&chronicle.Entry(chronicle.Tracked).step==phase&&Time.time<end)
                {
                    game.Player.Heal(100);foreach(var enemy in FindObjectsByType<GangMember>())if(enemy.CampaignUnit&&enemy.Body.Alive)enemy.Body.Damage(10000,Vector3.zero);
                    foreach(var a in WorldActor.All.ToArray())if(a&&a.name.Contains("소거 증폭기")&&a.Alive)a.Damage(1000,Vector3.zero);
                    if(phase==2){var survivor=FindObjectsByType<CityNpc>().FirstOrDefault(n=>n.name.Contains("구출 대상"));var extraction=chronicle.CurrentStep.position+new Vector3(-24,0,-18);game.Player.Respawn(survivor?Vector3.MoveTowards(survivor.transform.position,extraction,7):extraction);}
                    yield return new WaitForSeconds(.6f);
                }
                Check(chronicle.Progress.tracked!="main01"||chronicle.Entry(chronicle.Tracked).step>phase,"Main operation phase resolves through combat and objective actions / "+phase);
            }
            Check(chronicle.Progress.tracked=="main02","Completed operation advances naturally to the next story operation");
            Check(NpcDialogueBank.Count>=550,"Situation dialogue catalog contains over 550 lines");Finish();
        }
        IEnumerator View(string name,Vector3 eye,Vector3 target)
        {
            Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));Camera.main.fieldOfView=60;
            var overlays=FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;}
            yield return new WaitForSeconds(.3f);var rt=RenderTexture.GetTemporary(1280,720,24);
            for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});yield return null;}
            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Destroy(image);
            foreach(var canvas in overlays)if(canvas){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
        }
        void Update(){if(!done&&Time.realtimeSinceStartup-start>420){report.errors.Add("Essential incursion check timed out");Finish();}}
        void Finish(){if(done)return;done=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}

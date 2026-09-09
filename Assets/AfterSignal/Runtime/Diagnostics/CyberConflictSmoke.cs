using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class CyberConflictSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float start;bool finished;GameDirector game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-cyber-conflict-smoke")&&!FindAnyObjectByType<CyberConflictSmoke>()){GameDirector.SkipTitle=true;new GameObject("Cyber conflict essential checks").AddComponent<CyberConflictSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;start=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CyberConflict/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(message+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("CYBER "+yes+" / "+label);Save();}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;LifeState.Hours=14;WantedSystem.Clear("");WantedSystem.Instance.enabled=false;CityChronicle.Instance.enabled=false;RiftIncursion.Instance.enabled=false;
            yield return new WaitForSeconds(4);if(GangStrongholds.Instance)GangStrongholds.Instance.enabled=false;
            Check(GangStrongholds.Instance&&GangStrongholds.Instance.Bases.Count==3,"Three accessible gang compounds fit existing city parcels");
            Check(CityChronicle.Instance.Quests.Count(q=>q.id.StartsWith("gang-main-")&&q.main&&q.steps.All(s=>s.kind=="battle"))==3,"Three linked main-plot gang combat missions");
            foreach(string art in new[]{"CyberGang","CyberPolice"}){var sheet=Resources.LoadAll<Sprite>("Art/CyberSecurity/"+art);Check(sheet.Length==16,"Four directions by four poses / "+art);Check(sheet.Length>0&&sheet.Max(s=>s.bounds.size.y)<2.3f,"Human scale and crop / "+art);}
            var sim=UrbanSimulation.Instance;var at=new Vector3(40,.15f,-210);game.Player.Respawn(at);
            var police=PoliceCar.Create(WantedSystem.Instance,at+Vector3.right*8);police.enabled=false;yield return new WaitForSeconds(.6f);
            var car=police.Vehicle;var cabin=car.GetComponent<VehicleCabin>();string identity=cabin.IdentityAt(0);var ejected=cabin.EjectDriver();yield return null;
            Check(ejected&&NpcVoice.Role(ejected.GetComponent<CityNpc>()).Contains("Police"),"Hijacked police driver retains the same identity outside");
            Check(Mathf.Abs(VehicleSeats.Local(car,0).x)<1&&Mathf.Abs(VehicleSeats.Local(car,0).z)<.7f,"Police sedan uses cabin coordinates, not armoured truck coordinates");
            var a=ArmyResponder.Create(at+Vector3.right*16,0,null);a.enabled=false;yield return null;float before=a.Body.health;
            CityPopulation.Instance.VehicleSweep(car,a.transform.position-Vector3.right*3,a.transform.position+Vector3.right*3,5);
            Check(a.Body.health<before,"NPC-driven vehicle collision damages a soldier outside pedestrian pool");
            yield return new WaitForSeconds(.12f);a.Body.Damage(130,Vector3.zero,TrafficDamageSource.Environment);yield return new WaitForSeconds(.1f);
            Check(a.Body.Alive&&a.Body.Downed&&!a.GetComponent<CharacterController>().enabled,"Wounded soldier stays alive and becomes incapacitated");
            Check(NpcDialogueBank.Line(a.GetComponent<CityNpc>(),"ambient").Length>10&&NpcPersona.Group(a.GetComponent<CityNpc>())=="military","Soldier uses military dialogue persona");
            var bot=CombatRobot.Create(at+new Vector3(8,0,12),false);var armyBot=CombatRobot.Create(at+new Vector3(12,0,12),true);bot.enabled=armyBot.enabled=false;
            var truck=sim.Spawn(at+new Vector3(18,0,12),false,3);SecurityVehicleArt.Install(truck,false);
            var interceptor=sim.Spawn(at+new Vector3(26,0,12),false,0);SecurityVehicleArt.Install(interceptor,true);
            var gang=GangMember.Create(at+new Vector3(4,0,12),0,0);gang.enabled=false;
            yield return new WaitForSeconds(1);
            Check(bot.Body.robot&&armyBot.Body.robot&&bot.GetComponentsInChildren<MeshRenderer>().Length>10,"Separate articulated police and military robot models");
            Check(truck.GetComponentsInChildren<Transform>().Any(t=>t.name=="RearRamp"),"Military carrier has dedicated rear deployment ramp");
            yield return View("SecurityFleet",at+new Vector3(-3,7,-6),at+new Vector3(14,2,12));
            var titan=RiftCreature.Create(at+new Vector3(35,0,0),0);titan.enabled=false;var close=VehicleOccupant.Create(car,"CivilianMan",1,false,at);close.transform.position=titan.transform.position+Vector3.back*8;
            var officer=PoliceOfficer.Create(WantedSystem.Instance,titan.transform.position+Vector3.back*35,4,0);officer.enabled=false;yield return null;titan.Attacked(officer.Body,100);titan.SendMessage("ChooseTargets",game);
            Check(titan.PriorityTarget==officer.Body,"Titan prioritizes its attacker over a closer civilian");
            Check(FactionCombat.NearestOpponent(bot.Body,150)==titan.Body,"Security response prioritizes monster over nearby gang");
            Check(titan.GetComponent<TitanHealthDisplay>()&&titan.MaximumHealth>10000,"Titan health bar and maximum health are connected");
            titan.Shockwave(17,60);yield return View("TitanAndResponders",at+new Vector3(2,13,-30),titan.transform.position+Vector3.up*5);
            foreach(var unit in new[]{titan.gameObject,bot.gameObject,armyBot.gameObject,gang.gameObject,officer.gameObject,close.gameObject})Destroy(unit);
            var hospital=EmergencyAmbulance.Hospital(at);Vector3 patientAt;
            if(!CityGangWar.FindGround(hospital+new Vector3(8,0,-17),out patientAt))patientAt=hospital+new Vector3(0,.1f,-18);
            game.Player.Respawn(patientAt+Vector3.right*15);a.transform.position=patientAt;a.gameObject.AddComponent<MedicalPending>();var ambulance=EmergencyAmbulance.Create(a.Body,()=>{});
            float timeout=Time.time+65;bool carried=false,recovered=false;
            while(ambulance&&Time.time<timeout)
            {
                if(ambulance.Phase==3&&!carried){carried=true;Check(ambulance.Stretcher&&a.GetComponent<MedicalPending>().carried,"Two paramedics carry original patient on a stretcher");yield return View("StretcherRescue",ambulance.Stretcher.position+new Vector3(-7,4,-7),ambulance.Stretcher.position+Vector3.up);}
                if(ambulance.Phase==5){recovered=true;break;}yield return null;
            }
            Check(carried&&recovered&&!a.Body.Downed&&a.Body.health>50,"Ambulance loads, transports and discharges the same wounded soldier");
            if(GangStrongholds.Instance&&GangStrongholds.Instance.Bases.Count>0){var b=GangStrongholds.Instance.Bases[0];yield return View("GangStronghold",b+new Vector3(-24,15,-28),b+Vector3.up*3);}
            finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator View(string name,Vector3 eye,Vector3 target)
        {
            var cam=Camera.main;cam.transform.position=eye;cam.transform.LookAt(target);cam.fieldOfView=60;yield return new WaitForSeconds(.3f);var rt=new RenderTexture(1280,720,24);rt.Create();for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});yield return null;}var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-start>240){report.errors.Add("Native check timeout");finished=true;Save();Application.Quit(1);}}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}

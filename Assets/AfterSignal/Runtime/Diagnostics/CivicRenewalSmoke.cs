using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class CivicRenewalSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();public int consoles,props,armyShots;public string government;}
        readonly Report report=new();string output;float began;bool finished,hadJail;float jail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-civic-renewal-smoke")&&!FindAnyObjectByType<CivicRenewalSmoke>()){GameDirector.SkipTitle=true;new GameObject("Civic renewal essential verification").AddComponent<CivicRenewalSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CivicRenewal/Essential"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)if(report.errors.Count<18)report.errors.Add(text+"\n"+stack);}
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("CIVIC CHECK "+ok+" / "+text);}
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>230){Check(false,"Essential verification timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);g.CameraRig.enabled=false;LifeState.Hours=15;
            yield return new WaitForSeconds(3);
            var incident=RiftIncursion.Instance;incident.enabled=false;
            report.consoles=FindObjectsByType<FacilityConsole>().Length;report.props=FindObjectsByType<UsableProp>().Length;
            var campus=GovernmentCampus.Instance;report.government=campus?campus.transform.position.ToString():"missing";
            Check(campus&&report.consoles>100&&report.props>100&&FindObjectsByType<ArmoryCounter>().Length>0,"Government, distinct districts, physical weapon store and usable facility objects loaded");
            if(Environment.GetCommandLineArgs().Contains("-civic-presentation")){yield return Presentation(g);Finish();yield break;}
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Isolated verification floor";deck.transform.position=new Vector3(1400,179.5f,50);deck.transform.localScale=new Vector3(700,1,120);
            var p=new Vector3(1100,180.04f,50);g.Player.Respawn(p);var npc=Actor(p+Vector3.forward*2,1);yield return null;yield return null;Physics.SyncTransforms();
            bool collided=false;for(int i=0;i<15;i++){g.Player.Velocity=Vector3.forward*4;collided|=(g.Player.Controller.Move(Vector3.forward*.12f)&CollisionFlags.Sides)!=0;yield return null;}
            Check(collided&&npc.GetComponent<NpcBody>().Bumps>0,"Player capsule physically collides with NPC and triggers a bump reply");
            Destroy(npc.gameObject);g.Player.Respawn(p+Vector3.right*20);
            int impact=g.Audio.Count("urban_impact"),hurt=g.Audio.Count("hurt");var source=Actor(p+Vector3.right*40,2);source.military=true;
            for(int i=0;i<10;i++){var victim=Actor(p+new Vector3(i*2,0,12),10+i);victim.police=i%2==0;victim.Damage(10,Vector3.right,source);yield return new WaitForSeconds(.1f);victim.Damage(500,Vector3.right,source);}
            yield return null;yield return null;
            Check(FindObjectsByType<CreditDrop>().Any(d=>d.Amount>0)&&g.Audio.Count("urban_impact")>impact&&g.Audio.Count("hurt")>hurt,"Civilian and police deaths drop credits; hit and hurt sounds actually play");
            int money=LifeState.Credits;var drop=CreditDrop.Spawn(g.Player.transform.position,37);yield return new WaitForSeconds(1.1f);
            Check(!drop&&LifeState.Credits==money+37,"Walking over dropped credits adds the amount once and removes the pickup");
            var blastAt=p+new Vector3(85,0,0);var car=UrbanSimulation.Instance.Spawn(blastAt,false,0);
            var near=Actor(blastAt+Vector3.right*4,30);near.health=500;var far=Actor(blastAt+Vector3.right*25,31);far.health=500;var covered=Actor(blastAt+Vector3.left*6,32);covered.health=500;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=blastAt+new Vector3(-3,2,0);wall.transform.localScale=new Vector3(.8f,4,8);
            yield return null;Physics.SyncTransforms();car.Damage(1000,car.transform.position,source);yield return new WaitForSeconds(.3f);
            Check(near.health<500&&far.health==500&&covered.health==500,"Vehicle explosion damages nearby people, respects distance and is blocked by a wall");
            Destroy(near.gameObject);Destroy(far.gameObject);Destroy(covered.gameObject);Destroy(wall);
            ArmoryInventory.Reset();LifeState.Earn(20000);money=LifeState.Credits;
            Check(ArmoryInventory.Buy(8)&&ArmoryInventory.Buy(10)&&ArmoryInventory.Buy(11)&&ArmoryInventory.Buy(12)&&LifeState.Credits==money-5970,"Rifle, grenade, bazooka and shotgun purchases debit the exact total and equip owned slots");
            g.Player.Respawn(p+new Vector3(160,0,0));var target=Actor(g.Player.transform.position+Vector3.forward*16,40);target.health=2000;Physics.SyncTransforms();
            Camera.main.transform.position=g.Player.Shoulder-Vector3.forward*4;Camera.main.transform.LookAt(target.Center);
            var frame=ControlFrame.Empty;frame.pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);g.Input.ExternalFrame=frame;
            g.Player.Equipment.Select(3);yield return new WaitForSeconds(.3f);float hp=target.health;g.Player.Equipment.Attack();yield return new WaitForSeconds(.15f);
            Check(target.health<hp&&ArmoryInventory.Rounds(8)==29,"Purchased rifle consumes a round and hits the actual centered target");
            while(ArmoryInventory.Consume(8)){}g.Player.Equipment.BeginReload();yield return new WaitForSeconds(1.9f);
            Check(ArmoryInventory.Rounds(8)==30&&ArmoryInventory.Reserve(8)==60,"Rifle reload transfers finite reserve ammunition into the magazine");
            g.Player.Equipment.Select(6);yield return new WaitForSeconds(.3f);hp=target.health;g.Player.Equipment.Attack();yield return new WaitForSeconds(.15f);
            Check(target.health<hp-20&&ArmoryInventory.Rounds(12)==5,"Shotgun combines multiple pellet hits before the target invulnerability window");
            g.Player.Equipment.Select(5);yield return new WaitForSeconds(.3f);hp=target.health;int blasts=BlastDamage.Detonations;g.Player.Equipment.Attack();yield return new WaitForSeconds(.7f);
            Check(BlastDamage.Detonations>blasts&&target.health<hp&&ArmoryInventory.Rounds(11)==0,"Bazooka projectile impacts, explodes and applies radial damage");
            WantedSystem.Clear("verification");g.Player.Equipment.Select(4);yield return new WaitForSeconds(.3f);blasts=BlastDamage.Detonations;g.Player.Equipment.Attack();yield return new WaitForSeconds(2.8f);
            Check(BlastDamage.Detonations>blasts&&ArmoryInventory.Rounds(10)==0,"Thrown grenade uses its fuse and consumes the purchased grenade");
            WantedSystem.Clear("verification");g.Player.Respawn(p+new Vector3(230,0,0));money=LifeState.Credits;
            bool work=FacilityOperation.Begin("repair-essential",240,1);Check(work&&LifeState.Credits==money,"Facility work starts without paying a reward before the work is done");
            if(work)for(int i=0;i<3;i++){var op=FacilityOperation.Current;g.Player.Respawn(op.Target+Vector3.back);yield return null;FindObjectsByType<FacilityWorkPoint>().First(n=>n.owner==op).Use();yield return null;}
            int pay=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.CivicPermit",0)>0?264:240;
            Check(!FacilityOperation.Current&&LifeState.Credits==money+pay&&!FacilityOperation.Begin("repair-essential",240,1),"Three physical work stations pay once and prevent same-day reward farming");
            foreach(var a in WorldActor.All.ToArray())if(a&&a.transform.position.y>170)Destroy(a.gameObject);Destroy(deck);g.Player.Equipment.Select(0);WantedSystem.Clear("verification");
            g.Player.Respawn(new Vector3(740,.1f,140));var lighting=FindAnyObjectByType<NightIllumination>();LifeState.Hours=23;yield return new WaitForSeconds(1);
            Check(lighting&&lighting.Night>.9f&&lighting.LitFixtures>0,"Night lights activate near the player and neon emission follows the clock");
            yield return View("night-street",g.Player.transform.position,g.Player.transform.position+new Vector3(-18,7,-28),g.Player.transform.position+Vector3.forward*40);
            LifeState.Hours=13;yield return new WaitForSeconds(.7f);Check(lighting.Night<.1f&&lighting.LitFixtures==0,"Daylight disables the local streetlight pool");
            if(campus){var center=campus.transform.position;yield return View("government",campus.Entrance,center+new Vector3(145,83,-170),center+Vector3.up*19);yield return View("government-interior",center+new Vector3(0,.3f,-24),center+new Vector3(0,3,-28),center+new Vector3(17,2,2));}
            yield return View("weapon-store",new Vector3(950,.2f,246),new Vector3(958,3,249),new Vector3(946,1.5f,269));
            g.Player.Respawn(new Vector3(740,.1f,140));yield return null;Camera.main.transform.position=g.Player.transform.position+new Vector3(0,1.25f,-3.3f);Camera.main.transform.LookAt(g.Player.transform.position+Vector3.up*1.2f);yield return new WaitForSeconds(.2f);Capture("seo-finish",true);
            g.Player.Respawn(new Vector3(780,.1f,140));incident.enabled=true;bool spawned=incident.Trigger(new Vector3(740,.1f,140));var creatureHealth=FindObjectsByType<RiftCreature>().ToDictionary(c=>c,c=>c.Body.health);yield return new WaitForSeconds(11);
            report.armyShots=incident.MilitaryShots;
            Check(spawned&&FindObjectsByType<ArmyResponder>().Length>0&&report.armyShots>0&&creatureHealth.Any(pair=>pair.Key&&pair.Key.Body.health<pair.Value),"Story anomaly spawns creatures and dispatches military units that actually fire and damage them");
            yield return View("military-response",g.Player.transform.position,new Vector3(782,19,105),incident.Position+Vector3.up*2);
            Finish();
        }
        WorldActor Actor(Vector3 at,int variation){var go=new GameObject("Essential test resident",typeof(SpriteRenderer),typeof(CityNpc));go.layer=9;go.transform.position=at;var n=go.GetComponent<CityNpc>();n.Configure(variation);n.enabled=false;var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get("CivilianMan",0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");return go.GetComponent<WorldActor>();}
        IEnumerator Presentation(GameDirector g)
        {
            var campus=GovernmentCampus.Instance;var center=campus.transform.position;LifeState.Hours=15;
            yield return View("government",campus.Entrance,center+new Vector3(145,83,-170),center+Vector3.up*19);
            yield return View("government-atrium",center+new Vector3(0,.2f,-23),center+new Vector3(0,2.1f,-23),center+new Vector3(-1,3,2));
            Physics.SyncTransforms();bool opening=Physics.Raycast(center+new Vector3(0,10,0),Vector3.down,out var hall,15,1)&&hall.point.y<center.y+.3f;bool gallery=Physics.Raycast(center+new Vector3(-20,10,0),Vector3.down,out var floor,4,1)&&Mathf.Abs(floor.point.y-center.y-8.4f)<.2f;
            Check(opening&&gallery,"Three-level government atrium has an open central volume and solid upper galleries");
            yield return View("government-office",center+new Vector3(-22,.2f,-17),center+new Vector3(-23,1.8f,-17),center+new Vector3(-35,1.8f,-17));
            yield return View("weapon-store",new Vector3(950,.2f,256),new Vector3(950,1.9f,257),new Vector3(950,1.85f,270));
            g.Player.Respawn(new Vector3(740,.1f,140));LifeState.Hours=23;yield return new WaitForSeconds(1);yield return View("night-street",g.Player.transform.position,g.Player.transform.position+new Vector3(-18,7,-28),g.Player.transform.position+Vector3.forward*40);
            LifeState.Hours=13;g.Player.Respawn(new Vector3(740,.1f,140));yield return null;Camera.main.transform.position=g.Player.transform.position+new Vector3(0,1.25f,-3.3f);Camera.main.transform.LookAt(g.Player.transform.position+Vector3.up*1.2f);yield return new WaitForSeconds(.2f);Capture("seo-finish",true);
            foreach(var stage in new[]{StageId.Clinic,StageId.School})
            {
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
                g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.CameraRig.enabled=false;yield return new WaitForSeconds(1);
                var details=FindAnyObjectByType<InteriorDistinct>();Check(details&&details.UsableObjects>0,"Facility-specific usable interior detail loads in "+stage);
                var point=FindObjectsByType<FacilityConsole>().FirstOrDefault();if(point){var at=point.transform.position;yield return View(stage.ToString().ToLowerInvariant()+"-interior",at+Vector3.back*2,at+new Vector3(-3,1.6f,-5),at+Vector3.up);}
            }
        }
        IEnumerator View(string file,Vector3 player,Vector3 eye,Vector3 target){GameDirector.Instance.Player.Respawn(player);Camera.main.transform.position=eye;Camera.main.transform.LookAt(target);yield return new WaitForSeconds(.8f);Capture(file);}
        void Capture(string file,bool hero=false)
        {
            var rt=RenderTexture.GetTemporary(1600,900,24);var prior=RenderTexture.active;var tex=new Texture2D(1600,900,TextureFormat.RGBA32,false);var renderers=GameDirector.Instance.Player.GetComponentsInChildren<Renderer>();var states=renderers.Select(r=>r.enabled).ToArray();if(!hero)foreach(var r in renderers)r.enabled=false;
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});for(int i=0;i<renderers.Length;i++)renderers[i].enabled=states[i];RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,file+".png"),tex.EncodeToPNG());RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Destroy(tex);
        }
        void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"civic.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}

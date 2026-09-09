using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    // Explicit native check only; saved progress is never written.
    public sealed class LocalSimulationProbe : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool completed; public int nearSeaShots,farSeaShots; public List<string> passed=new(),errors=new(); }
        readonly Report report=new(); GameDirector game; float began; bool finished;
        const string Output="Artifacts/LocalSimulation/Native";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-local-simulation-probe")||FindAnyObjectByType<LocalSimulationProbe>())return;
            GameDirector.SkipTitle=true;new GameObject("Local simulation essentials").AddComponent<LocalSimulationProbe>();
        }
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Directory.CreateDirectory(Output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string line,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(line+"\n"+stack);Save();}}
        void Check(bool result,string line){(result?report.passed:report.errors).Add(line);Debug.Log("LOCAL SIMULATION "+result+" / "+line);Save();}
        void Save()=>File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(report,true));
        static void Set(object o,string key,object value)=>o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        GameObject Box(string name,Vector3 at,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");return go;}
        Vector2 Aim(Vector3 point){var camera=Camera.main;camera.transform.SetPositionAndRotation(game.Player.Shoulder,Quaternion.LookRotation(point-game.Player.Shoulder));return new(Screen.width*.5f,Screen.height*.5f);}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;
            game.CloseDialogue();game.SetPaused(false);Set(game,"<Title>k__BackingField",false);Set(game,"<Fade>k__BackingField",0f);
            game.enabled=false;game.CameraRig.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is CityChronicle||b is CitySafety||b is CityFireService||b is WantedSystem||b is CityGangWar||b is GangStrongholds||b is CitySocial||b is CityActivityDirector||b is GangCrime||b is GangMember||b is PoliceOfficer||b is ArmyResponder||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents||b is FourCityCampaign)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);
            var chronicle=CityChronicle.Instance;chronicle.Entry(chronicle.Quests[0]).step=chronicle.Quests[0].steps.Length;
            WantedSystem.Clear("");CityEventGate.Reset();yield return null;
            var seaPoint=new Vector3(1500,0,-1800);game.Player.Respawn(seaPoint);yield return null;
            var far=seaPoint+Vector3.right*1800;
            Check(!RiftIncursion.Instance.Trigger(far)&&!CivicTerrorEvents.Instance.Spawn(far)&&!GangConvoy.Dispatch(far),"Remote monster, terror and gang-convoy requests are rejected");
            game.Toast("unchanged");game.ToastNear("remote",far,180,5);
            Check(game.Notice=="unchanged","Distant incident notices cannot replace the player's notice");
            game.ToastNear("near",seaPoint,180,5);Check(game.Notice=="near"&&game.NoticeInRange,"Nearby notice is visible");
            // Check the same notice before a new district's arrival message can replace it.
            game.Player.Respawn(far);Check(!game.NoticeInRange,"Leaving a notice's area hides it before its timer expires");yield return null;
            game.Player.Respawn(seaPoint);yield return null;
            var pirate=MaritimeWorld.Instance.Spawn(seaPoint+new Vector3(70,0,0),SeaFaction.Pirates);
            var navy=MaritimeWorld.Instance.Spawn(seaPoint+new Vector3(160,0,0),SeaFaction.Navy);
            CityEventGate.Reset();yield return new WaitForSeconds(3.5f);
            report.nearSeaShots=pirate.Shots+navy.Shots;
            Check(report.nearSeaShots>0&&CityEventGate.Kind==CityEventKind.Gang,"Nearby sea opponents still fight and occupy the shared incident slot");
            CityEventGate.Reset();Check(CityEventGate.Begin(this,CityEventKind.Monster,seaPoint),"A nearby monster warning acquires the incident slot");
            int blockedShots=pirate.Shots+navy.Shots;yield return new WaitForSeconds(2);
            Check(pirate.Shots+navy.Shots==blockedShots,"Sea combat does not overlap a monster incident");
            CityEventGate.Reset();game.Player.Respawn(far);yield return null;int before=pirate.Shots+navy.Shots;
            yield return new WaitForSeconds(3);
            report.farSeaShots=pirate.Shots+navy.Shots-before;
            Check(report.farSeaShots==0&&!pirate.Target&&!navy.Target,"Moving beyond simulation range stops sea target searches and gunfire");
            game.Toast("remote ship silent");VehicleFailure.Begin(pirate.Car,null);
            Check(game.Notice=="remote ship silent","An unoccupied remote sinking ship produces no escape warning");
            Set(UrbanSimulation.Instance,"<Current>k__BackingField",navy.Car);VehicleFailure.Begin(navy.Car,null);
            Check(game.Notice.Contains("선체 침수"),"The ship carrying Seoha still produces the sinking escape warning");
            Set(UrbanSimulation.Instance,"<Current>k__BackingField",null);Destroy(pirate.gameObject);Destroy(navy.gameObject);CityEventGate.Reset();
            var site=new Vector3(1000,200,-800);Box("Diagnostic floor",site-Vector3.up*.5f,new(180,1,140));game.Player.Respawn(site);yield return null;
            Physics.SyncTransforms();var incident=RiftIncursion.Instance;bool created=incident.Trigger(site+Vector3.right*50);
            typeof(RiftIncursion).GetMethod("End",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(incident,null);yield return null;
            Check(created&&!incident.Active&&!CityEventGate.Busy&&!FindObjectsByType<RiftCreature>().Any(c=>c.Body&&c.Body.Alive&&(c.transform.position-site).sqrMagnitude<160*160),"Retiring an unfinished local monster event removes its live attackers before another incident can begin");
            var wall=Box("Ordinary grapple facade",site+new Vector3(32,15,0),new(2,30,18));Physics.SyncTransforms();
            var rope=game.Player.Rope;var input=ControlFrame.Empty;input.pointer=Aim(site+new Vector3(31,23,0));input.grapple=true;
            rope.Tick(input,.02f);Check(rope.Attached&&rope.Target.SurfaceCollider==wall.GetComponent<Collider>(),"One click attaches to an ordinary facade");
            float length=rope.Length;input.grapple=false;rope.Tick(input,.1f);
            Check(rope.Attached&&rope.Length<length,"Releasing the mouse keeps the rope latched and automatically reels in");
            input.grapple=true;rope.Tick(input,.02f);Check(!rope.Attached,"A second click releases the rope");
            input.grapple=false;rope.Tick(input,.02f);yield return null;input.grapple=true;rope.Tick(input,.02f);
            input.grapple=false;input.jump=true;rope.Tick(input,.02f);Check(!rope.Attached&&game.Player.Velocity.y>0,"Jump releases the rope with upward momentum");
            input.jump=false;rope.Tick(input,.02f);
            // The crosshair misses this thin pillar; an adjacent assist ray reaches its real collider.
            input.pointer=Aim(site+new Vector3(0,4,40));var camera=Camera.main;
            var assistedRay=camera.ScreenPointToRay(input.pointer+Vector2.right*Screen.height*.035f);
            var pillar=Box("Slender assisted pillar",assistedRay.GetPoint(36),new(.55f,5,.55f));Physics.SyncTransforms();
            var candidate=rope.Select(input.pointer);Check(candidate&&rope.AimAssisted&&candidate.SurfaceCollider==pillar.GetComponent<Collider>(),"Aim assistance catches a thin structure when the center ray misses");
            Destroy(pillar);Destroy(wall);
            var tightWall=Box("Camera wall",site+new Vector3(0,3,-.5f),new(20,6,1));Physics.SyncTransforms();
            game.Player.Respawn(site+Vector3.forward*.55f);Set(game.Player,"<WallClimbing>k__BackingField",true);Set(game.Player,"<WallNormal>k__BackingField",Vector3.forward);
            Set(game.CameraRig,"orbitYaw",0f);Set(game.CameraRig,"orbitPitch",0f);game.CameraRig.Snap();game.CameraRig.enabled=true;
            yield return new WaitForSeconds(.5f);
            Check(!tightWall.GetComponent<Collider>().bounds.Contains(camera.transform.position)&&camera.transform.position.z>site.z+.05f,"Wall-climbing camera stays on the open side of the facade");
            Check(game.CameraRig.CloseQuarters&&camera.fieldOfView>50,"Close quarters widens the view instead of filling it with the character");
            Capture("wall-clearance");
            input.look=true;input.lookDelta=new Vector2(600,100);float oldYaw=game.CameraRig.OrbitYaw;game.CameraRig.ReadLook(input);yield return new WaitForSeconds(.4f);
            Check(Mathf.Abs(Mathf.DeltaAngle(oldYaw,game.CameraRig.OrbitYaw))>40,"Mouse view can turn freely while clinging to a wall");
            Set(game.Player,"<WallClimbing>k__BackingField",false);game.Player.Respawn(site+new Vector3(0,0,24));game.CameraRig.Snap();yield return new WaitForSeconds(.4f);
            var hero=(Renderer[])typeof(CameraRig).GetField("heroRenderers",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.CameraRig);
            var original=(bool[])typeof(CameraRig).GetField("heroWasHidden",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.CameraRig);
            Check(hero==null||hero.Select((r,i)=>!r||r.forceRenderingOff==original[i]).All(v=>v),"Character visibility is restored after moving away from the wall");
            Destroy(tightWall);game.Player.Respawn(new Vector3(300,1,0));game.CameraRig.SetView(new Vector3(400,4,0));yield return new WaitForSeconds(2);Capture("city-render");
            Finish();
        }
        void Capture(string name)
        {
            var camera=Camera.main;var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
            var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(texture);
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>160){report.errors.Add("Local simulation probe timed out");Finish();}}
        void Finish(){if(finished)return;finished=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}

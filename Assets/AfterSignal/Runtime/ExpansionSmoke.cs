using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    public sealed class ExpansionSmoke:MonoBehaviour
    {
        [Serializable] public class Check {public string stage,detail;public int kills,ropes;public float health,seconds;public bool passed;}
        [Serializable] public class Report {public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();public bool completed;public string scope="FullExpansion";}
        static Report report=new Report();static Dictionary<string,int> prefs=new Dictionary<string,int>();static HashSet<string> had=new HashSet<string>();static bool active,harborDone;static int townVisits;
        GameDirector game;string output,root;bool failed;QualitySession metrics;
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string t,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){report.errors.Add(t);failed=true;Write();}}
        IEnumerator Start()
        {
            game=GetComponent<GameDirector>();root=Path.GetFullPath(QualitySession.Arg("-quality-output",Path.Combine(Application.dataPath,"../../../Artifacts/Expansion/Run")));Directory.CreateDirectory(root);
            if(!active){active=true;foreach(string key in new[]{"Stage","Memories","Completed","Expansion.Chapters","Expansion.Accepted","Expansion.Jobs"}){string k="AFTERSIGNAL.Unity."+key;prefs[k]=PlayerPrefs.GetInt(k);if(PlayerPrefs.HasKey(k))had.Add(k);}foreach(string key in new[]{"Chapters","Accepted","Jobs"})PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.Expansion."+key);}
            if(game.stage==StageId.Station){PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed",1);GameDirector.SkipTitle=true;StageId start=Enum.TryParse(QualitySession.Arg("-expansion-only"),out StageId fixture)?fixture:StageId.Haven;if(start!=StageId.Haven)report.scope="SingleStage:"+start;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(start));yield break;}
            output=Path.Combine(root,game.stage+"-"+townVisits);Directory.CreateDirectory(output);Application.runInBackground=true;FramePacing.Apply();game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.SetPaused(false);
            metrics=gameObject.AddComponent<QualitySession>();QualitySession.Output=output;
            yield return Hold(.5f,ControlFrame.Empty);yield return Capture("entry");if(game.Dialogue){yield return Hold(.7f,ControlFrame.Empty);game.CloseDialogue();}
            if(game.stage==StageId.Haven){yield return Town();yield break;}
            var spec=CampaignCatalog.Get(game.stage);
            if(spec.layout==DistrictLayout.Escalator){yield return Go(28,0,false);yield return Go(49,0,false);if(game.Player.transform.position.y<4.5f)Fail("Escalator did not reach the upper deck");yield return Capture("upper");}
            if(spec.layout==DistrictLayout.Lift||spec.layout==DistrictLayout.Exterior){yield return Go(26,0,false);yield return Use(InteractionKind.Lift);yield return Hold(3.4f,ControlFrame.Empty);if(game.Player.transform.position.y<3.7f)Fail("Lift did not carry the player");yield return Capture("lift");if(spec.layout==DistrictLayout.Exterior){yield return Go(41,0,false);yield return Swing(54,57,1.6f);if(game.Player.transform.position.y<7.5f)yield return Hold(1.2f,ControlFrame.Empty);}}
            if(spec.layout==DistrictLayout.Canal){yield return Go(20,0,true);yield return Swing(30,32,1.2f);yield return Go(49,0,true);yield return Swing(58,61,1.2f);}
            if(spec.id==StageId.Harbor){yield return Go(24,-1,false);yield return Use(InteractionKind.Supply);game.CloseDialogue();harborDone=true;}
            yield return Go(spec.length-12,0,true);yield return Clear();yield return Go(spec.length-18,2,false);yield return Use(InteractionKind.Memory);game.CloseDialogue();
            yield return Go(spec.length-9,-1.2f,false);yield return Use(InteractionKind.DistrictRelay);yield return Hold(.5f,ControlFrame.Empty);yield return Go(spec.length-3,0,true);yield return Capture("exit");
            Record(game.Cleared&&game.Power&&(!spec.glass||game.BrokenGlass),"Traversal, guards, archive, relay and exit prerequisites");metrics.Save();if(!string.IsNullOrEmpty(QualitySession.Arg("-expansion-only"))){report.completed=!failed;Finish();yield break;}if(failed){Finish();yield break;}yield return Use(InteractionKind.DistrictExit);
        }
        IEnumerator Town()
        {
            townVisits++;
            if(!harborDone){
                yield return Go(19,1,false);yield return Use(InteractionKind.MinJob);game.CloseDialogue();
                yield return Go(9,1,false);yield return Go(9,4,false);yield return Go(32,4,false);yield return Use(InteractionKind.YunJob);game.CloseDialogue();yield return Capture("promenade");
                yield return Go(9,4,false);yield return Go(9,0,false);yield return Go(64,0,false);yield return Use(InteractionKind.Lift);yield return Hold(3.4f,ControlFrame.Empty);if(game.Player.transform.position.y<5.5f)Fail("Town lift did not reach workshop");
                yield return Go(76,1,false);yield return Use(InteractionKind.Rest);game.CloseDialogue();yield return Go(86,3,false);yield return Go(108,3,false);yield return Capture("rooftop");
                yield return Go(115,3,false);yield return Use(InteractionKind.Signal);game.CloseDialogue();yield return Go(86,3,false);yield return Go(64,0,false);yield return Use(InteractionKind.Lift);yield return Hold(3.4f,ControlFrame.Empty);
                yield return Go(9,0,false);yield return Go(9,4,false);yield return Go(32,4,false);yield return Use(InteractionKind.YunJob);game.CloseDialogue();yield return Go(9,4,false);yield return Go(9,0,false);yield return Go(54,-3,false);
                Record((CampaignCatalog.Jobs&16)!=0,"Town lift, escalators, workshop, roof antenna and Yun delivery");metrics.Save();if(failed){Finish();yield break;}yield return Use(InteractionKind.HarborTravel);yield break;
            }
            yield return Go(19,1,false);yield return Use(InteractionKind.MinJob);game.CloseDialogue();yield return Go(10,.5f,false);yield return Use(InteractionKind.QuestGiver);yield return Capture("quest");game.CloseDialogue();
            int chapter=CampaignCatalog.NextChapter;Record((CampaignCatalog.Jobs&4)!=0,"Town return, supply delivery and chapter "+chapter);metrics.Save();
            if(chapter==0||failed){report.completed=!failed&&report.errors.Count==0&&townVisits>=5;Finish();yield break;}
            yield return Go(25,-2,false);yield return Use(InteractionKind.MissionBoard);
        }
        ControlFrame Aim(Vector3 p,bool attack=false){var c=ControlFrame.Empty;c.pointer=Camera.main.WorldToScreenPoint(p);c.attack=attack;c.weapon=1;return c;}
        EnemyBrain Target(){EnemyBrain best=null;float distance=999;foreach(var e in game.Enemies)if(e&&e.Alive&&Mathf.Abs(e.transform.position.y-game.Player.transform.position.y)<3){float d=Vector3.Distance(e.transform.position,game.Player.transform.position);if(d<distance){distance=d;best=e;}}return best;}
        IEnumerator Go(float x,float z,bool fight)
        {
            if(failed)yield break;
            float until=Time.realtimeSinceStartup+35;while(!failed&&Time.realtimeSinceStartup<until){if(game.Dead){Fail("Player died en route to "+x);break;}if(game.Paused)game.SetPaused(false);var p=game.Player.transform.position;if(Mathf.Abs(x-p.x)<.32f&&Mathf.Abs(z-p.z)<.22f)break;var enemy=Target();bool contact=fight&&enemy&&Vector3.Distance(enemy.transform.position,p)<3.7f;bool glazing=false;if(fight)foreach(var glass in game.Glass)if(glass&&!glass.Broken&&Mathf.Abs(glass.transform.position.x-p.x)<3.5f)glazing=true;var c=Aim(contact?enemy.transform.position+Vector3.up*1.2f:new Vector3(x,p.y+1.3f,z),contact||glazing);c.move=new Vector2(Mathf.Abs(x-p.x)>.18f?Mathf.Sign(x-p.x):0,Mathf.Abs(z-p.z)>.14f?Mathf.Sign(z-p.z):0);c.skill=fight&&enemy&&Vector3.Distance(enemy.transform.position,p)<4&&game.Player.Energy>40;if(game.stage==StageId.Canal||game.stage==StageId.Aqueduct){bool crossing=p.x>22&&p.x<30||p.x>50&&p.x<58;if(crossing){c.attack=c.skill=false;if(game.Player.Grounded&&!Physics.Raycast(p+Vector3.up+Vector3.right*Mathf.Sign(x-p.x)*1.2f,Vector3.down,2.2f,1))c.jump=true;}}game.Input.ExternalFrame=c;yield return null;}
            game.Input.ExternalFrame=ControlFrame.Empty;if(Mathf.Abs(game.Player.transform.position.x-x)>1)Fail("Movement timeout at "+game.Player.transform.position+", target "+x);
        }
        IEnumerator Swing(float x,float landing,float seconds){var anchor=GrappleAnchor.All.Find(a=>Mathf.Abs(a.transform.position.x-x)<.1f);if(!anchor){Fail("Missing rope anchor "+x);yield break;}float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until){var c=Aim(anchor.transform.position);c.grapple=true;c.move=Vector2.one;game.Input.ExternalFrame=c;yield return null;}if(!game.Player.Rope.Attached)Fail("Rope did not attach "+x);var release=Aim(anchor.transform.position);release.grapple=true;release.jump=true;release.move=Vector2.right;game.Input.ExternalFrame=release;yield return null;yield return Hold(.2f,ControlFrame.Empty);yield return Go(landing,0,true);}
        IEnumerator Clear(){float until=Time.realtimeSinceStartup+45;while(!failed&&!game.Cleared&&Time.realtimeSinceStartup<until){var e=Target();if(!e){yield return null;continue;}var d=e.transform.position-game.Player.transform.position;var c=Aim(e.transform.position+Vector3.up,true);c.move=new Vector2(Mathf.Abs(d.x)>2?Mathf.Sign(d.x):0,Mathf.Abs(d.z)>.6f?Mathf.Sign(d.z):0);c.skill=game.Player.Energy>35&&d.magnitude<4;game.Input.ExternalFrame=c;if(game.Dead)Fail("Player died clearing guards");yield return null;}game.Input.ExternalFrame=ControlFrame.Empty;if(!game.Cleared)Fail("Guards remain");}
        IEnumerator Use(InteractionKind kind){yield return Hold(.22f,ControlFrame.Empty);if(!game.Nearby||game.Nearby.kind!=kind){Fail("Expected nearby "+kind+", got "+(game.Nearby?game.Nearby.kind.ToString():"none")+" at "+game.Player.transform.position);yield break;}var c=ControlFrame.Empty;c.interact=true;game.Input.ExternalFrame=c;yield return null;yield return Hold(.25f,ControlFrame.Empty);}
        IEnumerator Hold(float seconds,ControlFrame c){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until){game.Input.ExternalFrame=c;yield return null;}game.Input.ExternalFrame=ControlFrame.Empty;}
        IEnumerator Capture(string name){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quality-no-captures")>=0)yield break;yield return new WaitForEndOfFrame();var cam=Camera.main;var canvas=FindAnyObjectByType<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);canvas.renderMode=mode;canvas.worldCamera=previousCamera;}
        void Fail(string reason){if(failed)return;failed=true;report.errors.Add(game.stage+": "+reason+"; position="+game.Player.transform.position+"; hits="+game.Player.HitsTaken);Write();}
        void Record(bool passed,string detail){if(!passed)Fail(detail);report.checks.Add(new Check{stage=game.stage.ToString(),detail=detail,passed=passed&&!failed,kills=game.Kills,ropes=game.Player.Rope.AttachCount,health=game.Player.Health,seconds=game.Elapsed});Write();}
        void Write(){if(!string.IsNullOrEmpty(root))File.WriteAllText(Path.Combine(root,"expansion.json"),JsonUtility.ToJson(report,true));}
        void Restore(){if(!active)return;foreach(var pair in prefs){if(had.Contains(pair.Key))PlayerPrefs.SetInt(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}PlayerPrefs.Save();}
        void OnApplicationQuit(){Restore();}
        void Finish(){Write();Restore();Application.Quit(report.completed?0:1);}
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    // Explicit command-line QA runner. It drives the same ControlFrame boundary as real input.
    // Normal play never enables this component or changes player/enemy health for verification.
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        [Serializable] public class Result {public string stage;public bool passed;public string detail;public int kills,attacks,ropes,coreStrikes;public float health,seconds;}
        [Serializable] public class Report {public List<Result> checks=new List<Result>();public List<string> errors=new List<string>();public bool completed;}
        static Report report=new Report();
        static readonly Dictionary<string,int> savedPrefs=new Dictionary<string,int>();
        static readonly HashSet<string> existingPrefs=new HashSet<string>();
        static bool backedUp;
        GameDirector game;
        string output;
        bool failed;
        bool quality;
        float qualityStart;
        void Awake(){Application.logMessageReceived+=Log;}
        void OnApplicationQuit(){if(backedUp)RestorePrefs();}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){report.errors.Add(text);report.completed=false;Write();}}
        IEnumerator Start()
        {
            game=GetComponent<GameDirector>();quality=Array.IndexOf(Environment.GetCommandLineArgs(),"-quality-slice")>=0;qualityStart=Time.realtimeSinceStartup;output=GetComponent<QualitySession>()?QualitySession.Output:Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Artifacts/Runtime"));
            if(!quality&&GetComponent<QualitySession>()){output=Path.Combine(output,game.stage.ToString());QualitySession.Output=output;}Directory.CreateDirectory(output);
            if(!backedUp){backedUp=true;foreach(string key in new[]{"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories","AFTERSIGNAL.Unity.Completed","AFTERSIGNAL.Unity.Expansion.Chapters","AFTERSIGNAL.Unity.Expansion.Accepted","AFTERSIGNAL.Unity.Expansion.Jobs"}){savedPrefs[key]=PlayerPrefs.GetInt(key);if(PlayerPrefs.HasKey(key))existingPrefs.Add(key);}}
            Application.runInBackground=true;FramePacing.Apply();
            yield return null;game.Input.ExternalControl=true;if(game.Title)game.Begin();if(game.Paused)game.SetPaused(false);
            yield return Hold(.4f,ControlFrame.Empty);
            yield return Capture("00-"+game.stage);
            if(game.stage==StageId.Station){
                yield return Go(7,1.1f,false);yield return PressInteract();if(!game.Dialogue)Fail("Noa interaction did not open dialogue");if(quality)yield return Hold(4,ControlFrame.Empty);game.CloseDialogue();
                if(quality){yield return Go(10,-1.8f,false);yield return Hold(2,ControlFrame.Empty);yield return Go(7,1.1f,false);var jump=AimAt(game.Player.Shoulder+Vector3.right*5);jump.jump=true;yield return Hold(.02f,jump);yield return Hold(1,ControlFrame.Empty);}
                yield return Go(16,-.4f,false);yield return PressInteract();if(!game.Power)Fail("Station power was not restored");
                yield return Capture("01-station-power");
                if(quality){yield return Hold(2,ControlFrame.Empty);yield return Go(28,0,false);yield return Capture("02-combat-entry");yield return Hold(.8f,ControlFrame.Empty);}
                yield return Go(50,0,true);yield return ClearGuards();yield return Go(53,2.1f,true);
                yield return Hold(4.3f,ControlFrame.Empty);yield return Capture("02-boarding");
                Record(game.Power&&game.Cleared&&game.Arrival>=1,"Power, six guards and train arrival");
                if(quality){
                    yield return Go(38,0,false);var hook=GrappleAnchor.All.Find(a=>Mathf.Abs(a.transform.position.x-37)<.1f);var rope=AimAt(hook.transform.position);rope.grapple=true;rope.move=new Vector2(-1,1);yield return Hold(.8f,rope);yield return Hold(.2f,ControlFrame.Empty);
                    yield return Go(16,0,false);yield return Go(10,-2.4f,false);yield return PressInteract();if(game.Dialogue)yield return Hold(3,ControlFrame.Empty);game.CloseDialogue();
                    yield return Go(7,1.1f,false);yield return PressInteract();if(game.Dialogue)yield return Hold(2,ControlFrame.Empty);game.CloseDialogue();
                    var dash=AimAt(game.Player.Shoulder+Vector3.right*5);dash.dash=true;yield return Hold(.02f,dash);yield return Hold(.8f,ControlFrame.Empty);yield return Go(53,2.1f,false);
                    yield return Hold(Mathf.Max(1,70-(Time.realtimeSinceStartup-qualityStart)),ControlFrame.Empty);Record(game.Player.Rope.AttachCount>0&&game.Memories>0,"Postcombat rope, archive pickup and return route");GetComponent<QualitySession>().Save();report.completed=!failed&&report.errors.Count==0;Write();RestorePrefs();Application.Quit(report.completed?0:1);yield break;
                }
                if(!failed)yield return PressInteract();
            }else if(game.stage==StageId.Carriage){
                yield return Go(24,0,true);yield return Capture("03-carriage-combat");
                yield return Go(48,-.9f,true);yield return PressInteract();
                yield return Go(55,0,true);yield return ClearGuards();yield return Go(58,.9f,true);yield return Capture("04-hatch");
                Record(game.BrokenGlass&&game.Release&&game.Cleared,"Glass breach, ten guards, hatch release");if(!failed)yield return PressInteract();
            }else if(game.stage==StageId.Roof){
                yield return Go(16,0,true);yield return Swing(27,27);yield return Go(37,0,true);yield return Swing(49,49);
                yield return Go(55,0,true);yield return ClearGuards();yield return Capture("05-roof-city");
                for(int cycle=0;cycle<3&&!failed;cycle++){
                    yield return Go(59,0,false);
                    yield return AttachCapacitor(0);yield return AttachCapacitor(1);yield return Capture("06-core-exposed-"+cycle);
                    yield return Go(59.1f,0,true);yield return Hold(2.2f,AimAt(game.Boss.transform.position+Vector3.up*1.3f,true));
                }
                Record(game.Boss&&!game.Boss.Alive&&game.CoreStrikes==3,"Rope crossings, six guards, three capacitor/core cycles");
                if(!failed){yield return Go(73,0,false);yield return PressInteract();}
            }else{
                yield return Go(8.2f,.5f,false);yield return PressInteract();yield return Capture("07-afterlight-dialogue");
                Record(game.Dialogue&&PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)==1,"Campaign ending, save and Noa epilogue");
                report.completed=!failed&&report.errors.Count==0;Write();RestorePrefs();Application.Quit(report.completed?0:1);
            }
            if(failed){Write();RestorePrefs();Application.Quit(1);}
        }
        ControlFrame AimAt(Vector3 point,bool attack=false){var c=ControlFrame.Empty;var p=Camera.main.WorldToScreenPoint(point);c.pointer=new Vector2(p.x,p.y);c.attack=attack;c.weapon=1;return c;}
        EnemyBrain Nearest()
        {
            EnemyBrain closest=null;float best=float.MaxValue;
            foreach(var e in game.Enemies)if(e&&e.Alive&&!e.boss&&e.Active){float d=Vector3.Distance(e.transform.position,game.Player.transform.position);if(d<best){best=d;closest=e;}}
            return closest;
        }
        IEnumerator Go(float x,float z,bool fight,float timeout=23)
        {
            float started=Time.realtimeSinceStartup;
            while(!failed&&Time.realtimeSinceStartup-started<timeout){
                if(game.Dead){Fail("Player died while moving to "+x);break;}
                if(game.Paused)game.SetPaused(false);
                Vector3 pos=game.Player.transform.position;
                if(Mathf.Abs(pos.x-x)<.38f&&Mathf.Abs(pos.z-z)<.22f)break;
                var target=Nearest();var aim=target&&fight?target.transform.position+Vector3.up*1.2f:new Vector3(x,pos.y+1.4f,z);
                var c=AimAt(aim,fight);c.move=new Vector2(Mathf.Abs(x-pos.x)<.2f?0:Mathf.Sign(x-pos.x),Mathf.Abs(z-pos.z)<.15f?0:Mathf.Sign(z-pos.z));
                if(target&&Vector3.Distance(target.transform.position,pos)<4.2f&&game.Player.Energy>40)c.skill=true;
                if(quality){c.weapon=0;c.skill=false;if(!fight)c.move*=.5f;}
                game.Input.ExternalFrame=c;yield return null;
            }
            game.Input.ExternalFrame=ControlFrame.Empty;
            if(Mathf.Abs(game.Player.transform.position.x-x)>1.2f)Fail("Movement timed out at "+game.Player.transform.position+", target "+x);
        }
        IEnumerator ClearGuards()
        {
            float started=Time.realtimeSinceStartup;
            while(!game.Cleared&&!failed&&Time.realtimeSinceStartup-started<40){
                var e=Nearest();if(!e){yield return null;continue;}
                var pos=game.Player.transform.position;var c=AimAt(e.transform.position+Vector3.up*1.2f,true);
                var d=e.transform.position-pos;c.move=new Vector2(Mathf.Abs(d.x)>2?Mathf.Sign(d.x):0,Mathf.Abs(d.z)>.6f?Mathf.Sign(d.z):0);c.skill=game.Player.Energy>35&&d.magnitude<4.5f;
                if(quality){c.weapon=0;c.skill=false;}game.Input.ExternalFrame=c;if(game.Dead)Fail("Player died during guard combat");yield return null;
            }
            game.Input.ExternalFrame=ControlFrame.Empty;if(!game.Cleared)Fail("Guard combat timed out");
        }
        IEnumerator Swing(float anchorX,float destination)
        {
            var anchor=GrappleAnchor.All.Find(a=>Mathf.Abs(a.transform.position.x-anchorX)<.1f);
            var c=AimAt(anchor.transform.position);c.grapple=true;c.move=new Vector2(1,1);
            // Re-project every frame while the camera follows the player.
            float begin=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-begin<1.2f){c.pointer=Camera.main.WorldToScreenPoint(anchor.transform.position);game.Input.ExternalFrame=c;yield return null;}
            if(!game.Player.Rope.Attached)Fail("Rope failed to latch at "+anchorX);
            c.grapple=true;c.jump=true;c.move=new Vector2(1,0);game.Input.ExternalFrame=c;yield return null;c.grapple=false;c.jump=false;yield return Hold(.08f,c);yield return Go(destination,0,true);
        }
        IEnumerator AttachCapacitor(int id)
        {
            var anchor=GrappleAnchor.All.Find(a=>a.capacitor==id);yield return Hold(.05f,ControlFrame.Empty);
            int strikesBefore=game.CoreStrikes,attachmentsBefore=game.Player.Rope.AttachCount;
            var c=AimAt(anchor.transform.position);c.grapple=true;yield return Hold(.15f,c);
            // A buffered blade hit can consume the exposed core before this observation.
            // Require a real hook plus either the charged bit or the resulting core-strike event.
            if(game.Player.Rope.AttachCount<=attachmentsBefore||((game.Capacitors&(1<<id))==0&&game.CoreStrikes<=strikesBefore))Fail("Capacitor "+id+" did not charge through mouse targeting");
            yield return Hold(.15f,ControlFrame.Empty);
        }
        IEnumerator PressInteract(){var c=ControlFrame.Empty;c.interact=true;game.Input.ExternalFrame=c;yield return null;game.Input.ExternalFrame=ControlFrame.Empty;yield return Hold(.2f,ControlFrame.Empty);}
        IEnumerator Hold(float seconds,ControlFrame controls){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until){game.Input.ExternalFrame=controls;yield return null;}game.Input.ExternalFrame=ControlFrame.Empty;}
        IEnumerator Capture(string name)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quality-no-captures")>=0)yield break;
            game.Input.ExternalFrame=ControlFrame.Empty;yield return new WaitForEndOfFrame();
            // Standalone batch mode does not present the window backbuffer. Render the real URP camera and HUD explicitly.
            var camera=Camera.main;var canvas=FindAnyObjectByType<Canvas>();var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;
            var texture=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=texture});
            var previous=RenderTexture.active;RenderTexture.active=texture;var png=new Texture2D(1600,900,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1600,900),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),png.EncodeToPNG());
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(texture);Destroy(png);canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;Canvas.ForceUpdateCanvases();yield return null;
        }
        void RestorePrefs(){foreach(var pair in savedPrefs){if(existingPrefs.Contains(pair.Key))PlayerPrefs.SetInt(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}PlayerPrefs.Save();}
        void Fail(string message){if(failed)return;failed=true;report.errors.Add(message);Debug.LogWarning("SMOKE: "+message);Write();}
        void Record(bool passed,string detail){if(!passed)Fail(detail);report.checks.Add(new Result {stage=game.stage.ToString(),passed=passed&&!failed,detail=detail,kills=game.Kills,attacks=game.Player.Attacks,ropes=game.Player.Rope.AttachCount,coreStrikes=game.CoreStrikes,health=game.Player.Health,seconds=game.Elapsed});Write();if(!quality&&GetComponent<QualitySession>())GetComponent<QualitySession>().Save();}
        void Write(){if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"playthrough.json"),JsonUtility.ToJson(report,true));}
    }
}

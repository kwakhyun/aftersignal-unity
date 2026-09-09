using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class HomeReturnProbe:MonoBehaviour
    {
        const string StageKey="AFTERSIGNAL.Unity.Stage";
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();bool hadStage;int savedStage;float began;string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-home-return-probe"))new GameObject("Home return essentials").AddComponent<HomeReturnProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);began=Time.realtimeSinceStartup;hadStage=PlayerPrefs.HasKey(StageKey);savedStage=PlayerPrefs.GetInt(StageKey);LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;output=Path.GetFullPath("Artifacts/HomeReturn/result.json");Directory.CreateDirectory(Path.GetDirectoryName(output));Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message+"\n"+stack);}
        void Check(bool pass,string message){(pass?report.passed:report.errors).Add(message);}
        IEnumerator Ready(){while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;}
        IEnumerator Start()
        {
            yield return Ready();LifeState.Load();int credits=LifeState.Credits;
            PlayerPrefs.SetInt(StageKey,(int)StageId.UrbanCity);var previous=GameDirector.Instance;previous.Begin(true);
            while(GameDirector.Instance==previous)yield return null;yield return Ready();var game=GameDirector.Instance;
            Check(game.stage==StageId.Residence&&Vector3.Distance(game.Player.transform.position,CompactHome.Spawn)<1,"Continue from an old city save starts inside Seoha's house");
            Check(LifeState.Credits==credits,"Continue preserves saved money");
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));yield return Ready();game=GameDirector.Instance;game.CloseDialogue();game.Input.ExternalControl=true;
            for(int i=0;i<4;i++)RespawnNetwork.Register(i);
            previous=game;game.Die();Check(game.Dead,"Death state is entered");game.Retry();
            while(GameDirector.Instance==previous)yield return null;yield return Ready();game=GameDirector.Instance;
            Check(game.stage==StageId.Residence&&Vector3.Distance(game.Player.transform.position,CompactHome.Spawn)<1,"Death retry returns home even after using recovery centres");
            Check(Physics.Raycast(game.Player.transform.position+Vector3.up,Vector3.down,3,1,QueryTriggerInteraction.Ignore),"Home return has supporting ground");
            report.completed=true;Finish();
        }
        void Update(){if(Time.realtimeSinceStartup-began>150){report.errors.Add("Home return probe timeout");Finish();}}
        void Finish(){Restore();File.WriteAllText(output,JsonUtility.ToJson(report,true));Application.Quit(report.errors.Count==0?0:1);}
        void Restore(){if(hadStage)PlayerPrefs.SetInt(StageKey,savedStage);else PlayerPrefs.DeleteKey(StageKey);}
        void OnDestroy(){Restore();Application.logMessageReceived-=Log;}
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    public sealed partial class GameDirector
    {
        const string StageKey="AFTERSIGNAL.Unity.Stage";
        public static bool HasSavedGame => PlayerPrefs.HasKey(StageKey)&&PlayerPrefs.GetInt(StageKey,-1)>=0&&PlayerPrefs.GetInt(StageKey,-1)<CampaignRules.Scenes.Length;
        public float LoadProgress { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetTitleSession(){SkipTitle=false;}
        public void Begin(bool resume=false)
        {
            if(Transition || resume&&!HasSavedGame)return;
            StartCoroutine(BeginFromTitle(resume));
        }
        IEnumerator BeginFromTitle(bool resume)
        {
            Transition=true;LoadProgress=0;Title=true;SkipTitle=true;Time.timeScale=0;inputSuppress=.25f;
            if(Paused){Paused=false;prePauseScale=1;}
            Dead=false;CloseDialogue();Audio.SetPaused(false);
            StageId destination;
            if(resume) destination=(StageId)PlayerPrefs.GetInt(StageKey);
            else
            {
                LifeState.Reset();RespawnNetwork.ResetHome();Memories=0;
                PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",0);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed",0);
                ResetExpansion();CivicWorld.ClearArrival();UrbanCatalog.Reset();ResidentialWorld.VisitHome=-1;
                var args=System.Environment.GetCommandLineArgs();
                bool legacyProbe=System.Array.IndexOf(args,"-aftersignal-smoke")>=0||System.Array.IndexOf(args,"-expansion-smoke")>=0;
                destination=legacyProbe?StageId.Station:StageId.Residence;
                PlayerPrefs.SetInt(StageKey,(int)destination);PlayerPrefs.Save();
                if(legacyProbe&&stage==destination){Title=Transition=false;Time.timeScale=1;yield break;}
            }
            yield return null;
            var operation=SceneManager.LoadSceneAsync(CampaignRules.Scene(destination));
            operation.allowSceneActivation=false;
            float start=Time.realtimeSinceStartup;
            while(operation.progress<.9f || Time.realtimeSinceStartup-start<.45f)
            {
                LoadProgress=Mathf.Clamp01(operation.progress/.9f);yield return null;
            }
            LoadProgress=1;yield return null;
            Time.timeScale=1;operation.allowSceneActivation=true;
        }
        public void ReturnToTitle()
        {
            if(Transition)return;
            UrbanSimulation.Instance?.SaveCar();LifeState.Save();CityChronicle.Instance?.Save();
            PlayerPrefs.SetInt(StageKey,(int)stage);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",Memories);PlayerPrefs.Save();
            CloseDialogue();if(Paused)SetPaused(false);Dead=false;Player.Rope.Release();
            Title=true;Time.timeScale=0;Audio.SetPaused(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}

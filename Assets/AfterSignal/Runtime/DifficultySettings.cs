using UnityEngine;
namespace AfterSignal
{
    // 0 쉬움 / 1 보통 / 2 어려움. Multipliers are read live, so changing the setting mid-session
    // affects the next spawn, report, payout or charge without touching existing saves.
    public static class DifficultySettings
    {
        const string Key="AFTERSIGNAL.Unity.Difficulty";
        public static readonly string[] Names={"쉬움","보통","어려움"};
        static int difficulty=1;static bool loaded;
        public static int Difficulty
        {
            get{if(!loaded){loaded=true;difficulty=Mathf.Clamp(PlayerPrefs.GetInt(Key,1),0,2);}return difficulty;}
            set{loaded=true;difficulty=Mathf.Clamp(value,0,2);PlayerPrefs.SetInt(Key,difficulty);PlayerPrefs.Save();}
        }
        static float Pick(float easy,float hard)=>Difficulty==0?easy:Difficulty==2?hard:1;
        // Enemy (campaign / gang) health and damage dealt to Seoha.
        public static float EnemyHealth=>Pick(.75f,1.3f);
        public static float EnemyDamage=>Pick(.7f,1.3f);
        // Police weapon damage against Seoha and the heat each confirmed report adds.
        public static float PoliceDamage=>Pick(.65f,1.35f);
        public static float HeatGain=>Pick(.75f,1.3f);
        // Legal and illegal income (jobs, shifts, savings interest, robbery) and costs of failure (fines, bail, hospital, sentence).
        public static float Payout=>Pick(1.25f,.8f);
        public static float Penalty=>Pick(.6f,1.5f);
    }
}

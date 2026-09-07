using UnityEngine;

namespace AfterSignal
{
    // Values 0..3 are the original save IDs. New content is append-only.
    public enum StageId { Station, Carriage, Roof, Haven, Market, Arcade, Canal, Transit, Lab, Archive, Tower, Skyline, Foundry, Crown, Aqueduct, Observatory, Origin, Harbor, Residence, School, Clinic, Headquarters, Breach, UrbanCity, UrbanInterior }
    public enum WeaponId { Katana, Greatsword, Pistol }
    [System.Serializable] public class AttackTiming
    {
        public float duration,contact,activeEnd,buffer=.14f,hitStop,stagger;
        public AttackTiming(float duration,float contact,float activeEnd,float hitStop,float stagger){this.duration=duration;this.contact=contact;this.activeEnd=activeEnd;this.hitStop=hitStop;this.stagger=stagger;}
    }
    [CreateAssetMenu(menuName = "AFTERSIGNAL/Game tuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Locomotion")]
        public float moveSpeed = 7.6f;
        public float acceleration = 34f;
        public float jumpSpeed = 12.2f;
        public float gravity = 30f;
        public float dashSpeed = 19f;
        [Header("Grapple")]
        public float ropeRange = 20f;
        public float ropeReelSpeed = 6f;
        public float ropeGravity = 13f;
        public float aimAssistPixels = 110f;
        [Header("Combat")]
        public int maxHealth = 100;
        public float katanaDamage = 36f;
        public float greatswordDamage = 68f;
        public float pistolDamage = 32f;
        public float skillDamage = 92f;
        public float skillCost = 30f;
        public float bossCoreDamage = 360f;
        [Header("Presentation timing / seconds; damage and repeat rates unchanged")]
        public AttackTiming katana=new AttackTiming(.32f,.10f,.17f,.038f,.21f);
        public AttackTiming greatsword=new AttackTiming(.56f,.23f,.34f,.065f,.32f);
        public AttackTiming pistol=new AttackTiming(.24f,0,.025f,.014f,.12f);
        public AttackTiming Timing(WeaponId weapon)=>weapon==WeaponId.Greatsword?greatsword:weapon==WeaponId.Pistol?pistol:katana;
        [Header("Magazine / seconds")]
        public int magazineSize=12;
        public float reloadDuration=1.15f;
        public AttackTiming ComboTiming(WeaponId weapon,int combo){
            if(weapon==WeaponId.Pistol)return pistolReady;
            if(combo==0)return Timing(weapon);
            if(weapon==WeaponId.Katana)return combo==1?katanaReturn:katanaFinish;
            if(weapon==WeaponId.Greatsword)return combo==1?heavyReturn:heavyFinish;
            return pistol;
        }
        public AttackTiming katanaReturn=new AttackTiming(.35f,.12f,.20f,.042f,.23f),katanaFinish=new AttackTiming(.46f,.19f,.27f,.055f,.29f);
        public AttackTiming pistolReady=new AttackTiming(.26f,.055f,.10f,.014f,.12f);
        public AttackTiming heavyReturn=new AttackTiming(.62f,.28f,.39f,.065f,.34f),heavyFinish=new AttackTiming(.78f,.38f,.50f,.08f,.4f);
        public AttackTiming katanaDash=new AttackTiming(.34f,.08f,.18f,.045f,.26f),heavyDash=new AttackTiming(.54f,.19f,.31f,.07f,.35f),pistolDash=new AttackTiming(.34f,.04f,.22f,.014f,.12f);
        public AttackTiming katanaSkill=new AttackTiming(.60f,.22f,.30f,.04f,.3f),heavySkill=new AttackTiming(.82f,.40f,.50f,.075f,.5f),pistolSkill=new AttackTiming(.65f,.10f,.48f,.014f,.15f);
    }
    public static class CampaignRules
    {
        public static readonly string[] Scenes = { "01_NeonStation", "02_NightCarriage", "03_SkylineRoof", "04_Afterlight", "05_RainDistrict", "06_GlassAtrium", "07_DrownedQuarter", "08_DeadLine", "09_MemoryVault", "10_VerticalArchive", "11_SignalTower", "12_Skyline", "13_Foundry", "14_Crown", "15_Aqueduct", "16_Observatory", "17_Origin", "18_Harbor", "19_SeoResidence", "20_AfterlightSchool", "21_AfterlightClinic", "22_SignalHeadquarters", "23_FacadeBreach", "24_OpenCity", "25_CityInterior" };
        public static string Scene(StageId stage) => Scenes[(int)stage];
        public static float Damage(WeaponId weapon, int combo, GameTuning tuning) => weapon == WeaponId.Katana ? tuning.katanaDamage * (combo == 2 ? 1.5f : 1f) : weapon == WeaponId.Greatsword ? tuning.greatswordDamage : tuning.pistolDamage;
        public static bool CanBoard(bool power, bool cleared) => power && cleared;
        public static bool CanOpenHatch(bool release, bool cleared, bool glassBroken) => release && cleared && glassBroken;
    }
}

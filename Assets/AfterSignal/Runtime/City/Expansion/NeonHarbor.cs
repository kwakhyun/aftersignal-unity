using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class NeonHarbor:MonoBehaviour
    {
        public const float South=-4550;
        public static readonly Vector3 OldDock=new(1272.5f,-.95f,-708),NewDock=new(920,-.95f,-2379);
        public static readonly Rect[] Land={new(180,-3190,840,790),new(1110,-3190,930,790),new(180,-4180,840,920),new(1110,-4180,930,920)};
        public static bool OnIsland(Vector3 p){foreach(var r in Land)if(r.Contains(new Vector2(p.x,p.z)))return true;return false;}
        public static bool Region(Vector3 p)=>p.z< -2320;
        public static readonly string[] Names={"노바 해협 여객항","크로마 수로시장","펠라직 해양연구소","오벨리스크 기록금고","스카이워드 시민의회","에코 수중 관측소","노바 공항","조수 발전 관제소"};
        public static readonly Vector3[] Sites={new(920,.05f,-2460),new(610,.05f,-2810),new(1360,.05f,-2810),new(1610,.05f,-3470),new(680,.05f,-3700),new(980,-28,-2180),new(1790,.05f,-3940),new(470,.05f,-4010)};
        static Vector3 P(float x,float z,float y=.035f)=>new(x,y,z);
        static NeonHarbor(){Sites[0]=new Vector3(899.2945f,.05f,-2537.274f);Sites[1]=new Vector3(653.3013f,.05f,-2785);Sites[2]=new Vector3(1475,.05f,-2713);Sites[3]=new Vector3(1617.7645f,.05f,-3498.9778f);}
        static readonly Vector3[][] streets={
            new[]{P(250,-2460),P(750,-2460),P(920,-2460),P(1000,-2460,8),P(1130,-2460,8),P(1250,-2460),P(1980,-2460),P(1980,-3050),P(1980,-3160,8),P(1980,-3290,8),P(1980,-3400),P(1980,-4100),P(1250,-4100),P(1130,-4100,8),P(1000,-4100,8),P(880,-4100),P(250,-4100),P(250,-3400),P(250,-3290,8),P(250,-3160,8),P(250,-3050),P(250,-2460)},
            new[]{P(250,-2660),P(440,-2660),P(650,-2850),P(920,-3050),P(1000,-3110,8),P(1130,-3260,8),P(1250,-3350),P(1450,-3540),P(1700,-3750),P(1980,-3970)},
            new[]{P(250,-3050),P(650,-3050),P(920,-3050),P(1000,-3050,8),P(1130,-3050,8),P(1250,-3050),P(1640,-3050),P(1980,-3050)},
            new[]{P(250,-3400),P(680,-3400),P(880,-3400),P(1000,-3400,8),P(1130,-3400,8),P(1250,-3400),P(1590,-3400),P(1980,-3400)},
            new[]{P(750,-2460),P(750,-2600),P(880,-2700),P(920,-2870),P(920,-3050)},
            new[]{P(1250,-2460),P(1250,-2670),P(1430,-2900),P(1640,-3050)},
            new[]{P(1980,-2760),P(1650,-2760),P(1430,-2900),P(1250,-3050)},
            new[]{P(680,-3400),P(560,-3550),P(400,-3730),P(510,-3930),P(880,-4100)},
            new[]{P(1590,-3400),P(1450,-3540),P(1260,-3780),P(1250,-4100)},
            new[]{P(250,-3730),P(400,-3730),P(800,-3870),P(880,-4100)},
            new[]{P(920,-2460),P(920,-2412)},
        };
        public static readonly List<Vector3[]> Roads=BuildRoads();
        static List<Vector3[]> BuildRoads()
        {
            var result=new List<Vector3[]>();foreach(var line in streets){var samples=new List<Vector3>();for(int i=1;i<line.Length;i++){int count=Mathf.CeilToInt(Vector3.Distance(line[i-1],line[i])/9);for(int j=0;j<count;j++)samples.Add(Vector3.Lerp(line[i-1],line[i],j/(float)count));}samples.Add(line[^1]);result.Add(samples.ToArray());}return result;
        }
        bool announced;float check;
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||Time.time<check)return;check=Time.time+1;
            bool here=Region(g.Player.transform.position);if(here&&!announced)g.Toast("NOVA STRAIT / 노바 해협도시\n수로시장 · 기업 기록금고 · 하늘 정원 · 해양 연구구역",6);announced=here;
        }
    }
}

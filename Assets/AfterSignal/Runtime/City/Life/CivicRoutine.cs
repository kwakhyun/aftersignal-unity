using UnityEngine;
namespace AfterSignal
{
    public enum CivicActivity{Work,Lesson,Meal,Rest,Toilet,Social,Home}
    public sealed class CivicRoutine:MonoBehaviour
    {
        public string role;
        public Vector3 work,meal,toilet,rest,social;
        public bool patient,student,prisoner,homeResident;
        public CivicActivity Activity{get;private set;}
        public string State{get;private set;}
        CityNpc npc;DirectionalPerson art;WorldActor body;
        readonly PursuitPath path=new PursuitPath();
        float plan,nextSpeech;Vector3 target;int slot=-1;bool initialized;
        public void Initialize(string job,Vector3 at)
        {
            role=job;work=at;rest=at+Vector3.right*3;meal=at+Vector3.back*4;toilet=at+Vector3.right*6;social=at+Vector3.forward*3;initialized=true;
            npc=GetComponent<CityNpc>();body=GetComponent<WorldActor>();art=GetComponent<DirectionalPerson>();
            var walker=GetComponent<ResidentWalker>();if(walker)walker.enabled=false;
        }
        public void Replan()
        {
            float h=LifeState.Hour;
            int s=Mathf.FloorToInt(h*4)+(npc?npc.variation:0);
            if(prisoner){Activity=s%3==0?CivicActivity.Rest:CivicActivity.Work;target=s%3==0?rest:work;}
            else if(patient){Activity=h<7||h>=21?CivicActivity.Rest:s%9==0?CivicActivity.Toilet:h>=12&&h<14||h>=18&&h<19?CivicActivity.Meal:s%4==0?CivicActivity.Rest:CivicActivity.Social;target=Activity==CivicActivity.Rest?rest:Activity==CivicActivity.Toilet?toilet:Activity==CivicActivity.Meal?meal:social;}
            else if(homeResident){Activity=h>=22||h<7?CivicActivity.Rest:h>=18&&h<20||h>=7&&h<8?CivicActivity.Meal:s%8==0?CivicActivity.Toilet:CivicActivity.Home;target=Activity==CivicActivity.Rest?rest:Activity==CivicActivity.Meal?meal:Activity==CivicActivity.Toilet?toilet:social;}
            else{Activity=s%11==0?CivicActivity.Toilet:h>=12&&h<13?CivicActivity.Meal:student&&h>=9&&h<15?CivicActivity.Lesson:h>=9&&h<18?CivicActivity.Work:CivicActivity.Social;target=Activity==CivicActivity.Toilet?toilet:Activity==CivicActivity.Meal?meal:Activity==CivicActivity.Social?social:work;}
            State=Activity==CivicActivity.Toilet?"화장실 다녀오는 중":Activity==CivicActivity.Meal?"식사 중":Activity==CivicActivity.Rest?"침대에서 휴식":Activity==CivicActivity.Lesson?"교실에서 수업 중":Activity==CivicActivity.Home?"집에서 TV 보는 중":Activity==CivicActivity.Work?role+" 업무 중":"이웃과 이야기 중";
            if(npc)npc.context="현재 "+State+". 자신의 직업은 "+role+"이며 이 시설에서 일상생활을 한다.";
            slot=s;
        }
        void Update()
        {
            if(CivilianImpact.Active(this)||CivilianDefense.Active(this))return;
            var g=GameDirector.Instance;if(!initialized||!g||g.Blocked||body&&!body.Alive)return;
            if(npc&&(npc.Fleeing||Time.time<npc.SocialUntil)||!GetComponent<SpriteRenderer>().enabled)return;
            if(Mathf.FloorToInt(LifeState.Hour*4)+(npc?npc.variation:0)!=slot)Replan();
            var dir=target-transform.position;dir.y=0;
            bool resting=(patient||homeResident)&&Activity==CivicActivity.Rest;
            bool arrived=dir.magnitude<(resting?1.9f:.75f);
            if(arrived&&resting)transform.position=rest;
            var height=transform.position;height.y=arrived&&(patient||homeResident)&&Activity==CivicActivity.Rest?rest.y:work.y;transform.position=height;
            if(art&&arrived&&Activity==CivicActivity.Home)art.Face(new Vector3(11,transform.position.y,-6),1);
            if(art){art.Lying=(patient||homeResident)&&Activity==CivicActivity.Rest&&arrived;art.Sitting=arrived&&(Activity==CivicActivity.Meal||Activity==CivicActivity.Home||Activity==CivicActivity.Lesson);}
            if(!arrived)
            {
                var step=path.Direction(transform.position,target)*Mathf.Min(dir.magnitude,Time.deltaTime*(student?1.55f:1.25f));
                if(!Physics.Raycast(transform.position+Vector3.up*.75f,step.normalized,.6f,1,QueryTriggerInteraction.Ignore))transform.position+=step;
                else
                {
                    var side=Vector3.Cross(dir.normalized,Vector3.up)*Time.deltaTime*1.2f;
                    if(!Physics.Raycast(transform.position+Vector3.up,side.normalized,.55f,1,QueryTriggerInteraction.Ignore))transform.position+=side;
                }
            }
            else if(Time.time>nextSpeech)
            {
                nextSpeech=Time.time+Random.Range(18,35);
                NpcSpeech.Say(this,Activity==CivicActivity.Lesson?"오늘 수업도 힘내자.":Activity==CivicActivity.Meal?"맛있게 잘 먹겠습니다.":Activity==CivicActivity.Home?"이 방송 재미있네요.":Activity==CivicActivity.Rest?"조금 쉬어야겠어요.":State);
            }
        }
    }
}

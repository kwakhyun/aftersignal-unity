using UnityEngine;

namespace AfterSignal
{
    public enum InteractionKind { Noa, Power, Board, Release, Hatch, Core, Return, Memory, Citizen, QuestGiver, MissionBoard, DistrictExit, DistrictRelay, Lift, HarborTravel, Supply, Signal, Rest, MinJob, YunJob, HomeDoor, Furniture, FacilityTravel, ReturnTown, FirstRail, BreachMission, UrbanEnter, UrbanExit, UrbanTown, UrbanService }
    public sealed class InteractionPoint : MonoBehaviour
    {
        public InteractionKind kind;
        public string title;
        [TextArea] public string dialogue;
        public float radius=2.7f;
        public bool Used { get; set; }
        public MovingLift lift;
        public HingedDoor door;
        public int siteId;public StageId destination;public Vector3 arrival;public bool hasArrival,restoresHealth;
        public bool CanReach(PlayerMotor player) => Vector3.Distance(transform.position,player.Shoulder)<radius;
        public void Interact(GameDirector game)
        {
            switch(kind){
                case InteractionKind.UrbanEnter:UrbanCatalog.Enter(game,siteId);break;
                case InteractionKind.UrbanExit:UrbanCatalog.Exit(game);break;
                case InteractionKind.UrbanTown:CivicWorld.Travel(game,StageId.Haven,new Vector3(215,.15f,-8));break;
                case InteractionKind.UrbanService:if(UrbanCatalog.Kind(UrbanCatalog.Current)==4||UrbanCatalog.Kind(UrbanCatalog.Current)==9||UrbanCatalog.Kind(UrbanCatalog.Current)==15)game.Player.Heal(100);game.ShowDialogue(UrbanCatalog.Name(UrbanCatalog.Current),UrbanCatalog.Descriptions[UrbanCatalog.Kind(UrbanCatalog.Current)]);break;
                case InteractionKind.HomeDoor:if(door)door.Toggle(game);break;
                case InteractionKind.Furniture:if(door)door.Toggle(game);if(restoresHealth)game.Player.Heal(100);game.ShowDialogue(title,dialogue);break;
                case InteractionKind.FacilityTravel:if(hasArrival)CivicWorld.Travel(game,destination,arrival);else game.Travel(destination);break;
                case InteractionKind.ReturnTown:CivicWorld.Travel(game,StageId.Haven,CivicWorld.TownDoor(game.stage));break;
                case InteractionKind.FirstRail:game.Travel(StageId.Station);break;
                case InteractionKind.BreachMission:
                    if(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)==0)game.ShowDialogue("구조대 작전 단말","먼저 중앙역의 유령 열차를 조사하세요. 돌아오면 외벽 기록 회수 작전을 시작할 수 있습니다.");else game.Travel(StageId.Breach);break;
                case InteractionKind.QuestGiver:
                    if(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)==0){game.ShowDialogue(title,"좋은 아침이야, 서하. 북쪽 본부에서 유령 열차의 구조 신호를 접수했어. 중앙역으로 향하기 전에 학교와 병원도 둘러봐.");break;}
                    int chapter=CampaignCatalog.NextChapter;
                    if(chapter>0){PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Expansion.Accepted",chapter);PlayerPrefs.Save();game.ShowDialogue(title,chapter==2?"돌아왔구나, 서하. 열차 기록은 청계 시장으로 이어져. 광장 아래 의뢰 단말에서 새 노선을 선택해. 이번에는 도시 전체의 기억을 되찾자.":chapter==3?"시장의 기억이 돌아왔어. 하지만 옥상 송신망이 신호를 가로채고 있어. 윤이 남긴 경로를 따라가 줘.":"전력은 되찾았지만 첫 기억이 아직 수로 아래에 남아 있어. 해진의 기록을 따라 도시의 기원을 찾아 줘.");}
                    else game.ShowDialogue(title,"이제 누구도 기억의 주인이 될 수 없어. 네가 되찾은 도시는 주민들이 함께 이어 갈 거야. 아직 만나지 못한 이웃도 찾아가 봐.");break;
                case InteractionKind.MissionBoard:
                    if(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)==0){game.Travel(StageId.Station);break;}
                    int next=CampaignCatalog.NextChapter;
                    if(next==0)game.ShowDialogue("도시 노선 복구 완료","모든 주요 노선의 기억을 되찾았습니다. 선착장과 마을의 주민 의뢰를 마무리하세요.");
                    else if(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Expansion.Accepted",0)!=next)game.Toast("노아와 대화해 다음 메인 의뢰를 먼저 받으세요");
                    else game.Travel(CampaignCatalog.ChapterStart(next));break;
                case InteractionKind.DistrictRelay:game.Power=true;Used=true;game.Toast("중계기 복구 · 경비망 해제 후 출구로 이동");break;
                case InteractionKind.DistrictExit:
                    var spec=CampaignCatalog.Get(game.stage);if(spec==null)break;
                    if(!game.Power||!game.Cleared||(spec.glass&&!game.BrokenGlass)){game.Toast(!game.Power?"중계기를 먼저 복구하세요":!game.Cleared?"남은 경비병을 제압하세요":"상층의 유리 격벽을 파괴하세요");break;}
                    if(spec.Finale)CampaignCatalog.Complete(spec.chapter);game.Travel(spec.Next);break;
                case InteractionKind.Lift:if(lift)lift.Use(game);break;
                case InteractionKind.HarborTravel:game.Travel(game.stage==StageId.Harbor?StageId.Haven:StageId.Harbor);break;
                case InteractionKind.Rest:game.Player.Heal(100);game.ShowDialogue(title,"장비를 정비하고 잠시 숨을 돌렸습니다. 체력이 회복되었습니다.");break;
                case InteractionKind.MinJob:
                    if((CampaignCatalog.Jobs&2)!=0&&(CampaignCatalog.Jobs&4)==0){CampaignCatalog.Jobs|=4;game.Memories++;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",game.Memories);PlayerPrefs.Save();game.Player.Heal(100);game.ShowDialogue(title,"보급품이 무사히 도착했구나. 오늘은 시장의 모두에게 따뜻한 식사를 나눌 수 있겠어. 고마워, 서하.");}
                    else{CampaignCatalog.Jobs|=1;game.ShowDialogue(title,(CampaignCatalog.Jobs&4)!=0?"네가 가져온 보급품으로 식당의 불을 다시 켰어.":"선착장 동쪽 창고의 보급 상자를 가져다줄래? 강가의 해진이 길을 알려 줄 거야.");}break;
                case InteractionKind.Supply:CampaignCatalog.Jobs|=2;Used=true;game.ShowDialogue("보급품 확보","손상되지 않은 보급 상자를 확보했습니다. 애프터라이트의 민에게 돌아가세요.");break;
                case InteractionKind.Signal:CampaignCatalog.Jobs|=8;game.ShowDialogue("옥상 안테나","도시의 새 방송을 수신했습니다. 상층 산책로의 윤에게 소식을 전하세요.");break;
                case InteractionKind.YunJob:
                    if((CampaignCatalog.Jobs&8)!=0&&(CampaignCatalog.Jobs&16)==0){CampaignCatalog.Jobs|=16;game.Memories++;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",game.Memories);PlayerPrefs.Save();game.ShowDialogue(title,"좋아, 이제 옥상에서 도시의 목소리가 들려. 첫 방송을 오래 기억할게.");}else game.ShowDialogue(title,(CampaignCatalog.Jobs&16)!=0?"도시가 다시 숨 쉬는 소리, 들리지?":"동쪽 승강기와 에스컬레이터를 타고 옥상 안테나를 확인해 줄래?");break;
                case InteractionKind.Noa:
                    game.ShowDialogue("NOA / 노아","서하, 유령 열차가 곧 도착해. 상층 단말에서 전력을 복구하고 승강장으로 내려가. 도시의 기억을 싣고 달리는 열차야."); break;
                case InteractionKind.Power:
                    if(!game.Power){game.Power=true;Used=true;game.Toast("승강장 전력 복구 · 경비병을 제압하세요");SignalEffects.Burst(transform.position,SignalEffects.Cyan,24,4);}
                    break;
                case InteractionKind.Board:
                    if(!CampaignRules.CanBoard(game.Power,game.Cleared))game.Toast(!game.Power?"상층의 전력 단말을 먼저 복구하세요":"승강장에 남은 경비병을 제압하세요");
                    else if(game.Arrival<1)game.Toast("열차 진입 중 · 문이 열릴 때까지 기다리세요");
                    else game.Travel(StageId.Carriage);break;
                case InteractionKind.Release:
                    game.Release=true;Used=true;game.Toast("수동 잠금 해제 · 객실 끝의 사다리로 이동하세요");break;
                case InteractionKind.Hatch:
                    if(CampaignRules.CanOpenHatch(game.Release,game.Cleared,game.BrokenGlass))game.Travel(StageId.Roof);
                    else game.Toast(!game.BrokenGlass?"객실 사이의 유리 격벽을 파괴하세요":!game.Release?"주황색 수동 잠금 단말을 작동하세요":"객실 안의 경비병을 모두 제압하세요");break;
                case InteractionKind.Core:
                    if(game.Boss&&game.Boss.Alive)game.Toast("먼저 컨덕터의 축전기 두 개를 과부하시켜 제압하세요");
                    else {PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed",1);PlayerPrefs.Save();game.Travel(StageId.Haven);}break;
                case InteractionKind.Return:game.Restart();break;
                case InteractionKind.Memory:
                    if(!Used){Used=true;game.Memories++;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",game.Memories);PlayerPrefs.Save();game.ShowDialogue("기억 파편 / ARCHIVE",dialogue);game.Player.Heal(15);}break;
                default:game.ShowDialogue(title,dialogue);break;
            }
            game.Audio.PlayCue(550,.14f,.08f);
        }
    }
}

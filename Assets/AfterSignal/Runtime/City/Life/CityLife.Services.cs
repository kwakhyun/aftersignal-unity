using System;
using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void Services()
        {
            int site = UrbanCatalog.Current;
            int type = game.stage == StageId.Clinic ? 4 : game.stage == StageId.Headquarters ? 14 : game.stage == StageId.School ? 5 : UrbanCatalog.Kind(site);
            bool hotel = game.stage == StageId.UrbanInterior && UrbanCatalog.IsHotel(site);
            Panel("service", hotel ? "애프터뷰 호텔" : game.stage == StageId.UrbanInterior ? UrbanCatalog.Name(site) : CivicWorld.Title(game.stage), "잔액 " + LifeState.Credits.ToString("N0") + " C · 원하시는 서비스를 선택하세요.");
            switch (type)
            {
                case 0:
                    Option(hotel ? "객실 · 8시간 숙박 / 240 C" : "게스트룸 · 6시간 휴식 / 120 C", () => Rest(hotel ? 8 : 6, hotel ? 240 : 120));
                    Option("보관 의상 갈아입기", Wardrobe);
                    break;
                case 1:
                    Option("분실물 찾아주기 / 하루 보상 100 C", () => Daily("lost", 100, "분실물을 주인에게 돌려주었습니다."));
                    Option("출석 및 벌금 납부 / " + (Mathf.Max(1, WantedSystem.Level) * 180) + " C", () => Purchase(Mathf.Max(1, WantedSystem.Level) * 180, () => WantedSystem.Clear("사건 정리 · 수배 해제")));
                    break;
                case 2:
                    Option("안전 물자 분류 / 2시간 · 180 C", () => Work("fire", 180, 2));
                    Option("응급 처치 / 60 C", () => Purchase(60, () => game.Player.Heal(35)));
                    break;
                case 3:
                    Body += "\n예금 " + LifeState.Savings.ToString("N0") + " C";
                    Option("500 C 예금", () => Bank(true));
                    Option("500 C 인출", () => Bank(false));
                    break;
                case 4:
                    Option("진료 및 완전 회복 / 180 C", () => Purchase(180, () =>
                    {
                        game.Player.Heal(100);
                        game.Player.RestoreEnergy(100);
                    }));
                    Option("외래 응급 처치 / 60 C", () => Purchase(60, () => game.Player.Heal(35)));
                    break;
                case 5:
                    Option("도서 정리 / 1시간 · 100 C", () => Work("school", 100, 1));
                    break;
                case 6:
                case 8:
                    Option("야간 순찰 코트 / 650 C", () => BuyClothes(2, 650));
                    Option("도시 여행 코트 / 850 C", () => BuyClothes(3, 850));
                    Option("구매한 의상 착용", Wardrobe);
                    break;
                case 7:
                    Option("도시락 / 체력 +40 · 45 C", () => Purchase(45, () => game.Player.Heal(40)));
                    Option("에너지 음료 / 에너지 +60 · 35 C", () => Purchase(35, () => game.Player.RestoreEnergy(60)));
                    break;
                case 9:
                    Option("따뜻한 정식 / 체력 +80 · 90 C", () => Purchase(90, () => game.Player.Heal(80)));
                    Option("든든한 특선 / 완전 회복 · 140 C", () => Purchase(140, () =>
                    {
                        game.Player.Heal(100);
                        game.Player.RestoreEnergy(100);
                    }));
                    break;
                case 10:
                    Option("중앙역 임무 출발", () =>
                    {
                        Dismiss();
                        game.Travel(StageId.Station);
                    });
                    break;
                case 11:
                case 12:
                    Option("소유 차량 정비 / 150 C", () =>
                    {
                        if (PlayerPrefs.GetInt(UrbanCatalog.Prefix + "Car", 0) == 0)
                        {
                            Body = "정비할 소유 차량이 없습니다.";
                            Revision++;
                            return;
                        }

                        Purchase(150, () =>
                        {
                            PlayerPrefs.SetFloat(UrbanCatalog.Prefix + "CarHealth", 100);
                            PlayerPrefs.Save();
                        });
                    });
                    break;
                case 13:
                    Option("물류 정리 / 2시간 · 240 C", () => Work("logistics", 240, 2));
                    break;
                case 14:
                    Option("신호 복원 활동 수당 / 하루 120 C", () => Daily("signal", 120, "도시 신호 복원 활동 수당을 받았습니다."));
                    break;
                case 15:
                    Option("따뜻한 커피 / 에너지 +70 · 35 C", () => Purchase(35, () => game.Player.RestoreEnergy(70)));
                    Option("커피와 샌드위치 / 체력 +45 · 60 C", () => Purchase(60, () =>
                    {
                        game.Player.Heal(45);
                        game.Player.RestoreEnergy(45);
                    }));
                    break;
            }

            if (LifeState.Errand != null && LifeState.Errand.accepted && !LifeState.Errand.completed && LifeState.Errand.site == site && game.stage == StageId.UrbanInterior && LifeState.Errand.kind != "rooftop")
                Option("숨은 의뢰 · 전달 / 확인", CompleteErrand);
            var staff = FindObjectsByType<CityNpc>(FindObjectsSortMode.None);
            foreach (var npc in staff)
                if (npc.point && npc.point.kind == InteractionKind.UrbanService)
                {
                    var target = npc;
                    Option("직원과 이야기", () => Talk(target));
                    break;
                }
        }

        void BuyClothes(int id, int price)
        {
            if (!LifeState.BuyOutfit(id, price))
            {
                Body = (LifeState.Outfits & (1 << id)) != 0 ? "이미 보유한 의상입니다." : "잔액이 부족합니다.";
                Revision++;
                return;
            }

            LifeState.Wear(id);
            Wardrobe();
        }

        void Bank(bool deposit)
        {
            bool ok = deposit ? LifeState.Deposit(500) : LifeState.Withdraw(500);
            Services();
            Body = ok ? "거래 완료 · 잔액 " + LifeState.Credits + " C / 예금 " + LifeState.Savings + " C" : "잔액이 부족합니다.";
            Revision++;
        }

        void Daily(string id, int reward, string message)
        {
            if (WantedSystem.Level > 0)
            {
                Body = "수배 중에는 업무를 받을 수 없습니다.";
                Revision++;
                return;
            }

            bool ok = LifeState.RewardOnce(LifeState.CampaignSerial + "-" + id + "-" + LifeState.Day, reward);
            Body = ok ? message + " +" + reward + " C" : "오늘의 업무는 이미 마쳤습니다.";
            Revision++;
        }

        void Work(string id, int reward, float hours)
        {
            int before = LifeState.Credits;
            Daily(id, reward, "업무를 마쳤습니다.");
            if (LifeState.Credits > before)
            {
                LifeState.Hours += hours;
                LifeState.Save();
            }
        }

        void Purchase(int price, Action effect)
        {
            if (!LifeState.Spend(price))
            {
                Body = "잔액이 부족합니다. 현재 " + LifeState.Credits + " C";
                Revision++;
                return;
            }

            effect();
            Services();
            Body = "이용 완료 · " + price + " C 사용 / 잔액 " + LifeState.Credits + " C";
            Revision++;
            game.Audio.Play("ui_confirm", game.Player.Shoulder, .18f, 1);
        }
    }
}

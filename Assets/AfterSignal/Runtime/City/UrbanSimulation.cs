using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed partial class UrbanSimulation : MonoBehaviour
    {
        public static UrbanSimulation Instance { get; private set; }

        public CityVehicle vehiclePrefab;
        public CityVehicle[] vehiclePrefabs;
        public CityVehicle Current { get; private set; }
        public int SeatIndex { get; private set; }
        public CityVehicle Owned { get; private set; }

        public readonly List<CityVehicle> Cars = new List<CityVehicle>();
        public bool Driving => Current;
        public bool Refueling { get; private set; }
        public int Hijacks { get; private set; }
        public int Entries { get; private set; }
        public string Prompt { get; private set; }
        public bool MapOpen { get; private set; }

        public void CloseMap()=>MapOpen=false;
        public void OpenMap()=>MapOpen=true;
        GameDirector game;
        Renderer[] playerRenderers;
        bool[] rendererStates;
        ContactShadow contact;
        float spawnClock, saveClock;
        CityVehicle nearest;
        readonly System.Random random = new System.Random();
        float fuelUnits;
        void Awake()
        {
            Instance = this;
        }

        System.Collections.IEnumerator Start()
        {
            game = GameDirector.Instance;
            while (!game.Ready)
                yield return null;
            Cars.AddRange(FindObjectsByType<CityVehicle>());
            foreach (var c in Cars)
                c.occupied = false;
            if (PlayerPrefs.GetInt(UrbanCatalog.Prefix + "Car", 0) > 0 && PlayerPrefs.GetInt(UrbanCatalog.Prefix + "CarStage", 23) == (int)game.stage)
            {
                Owned = Spawn(CompactCityLayout.Migrate(new Vector3(PlayerPrefs.GetFloat(UrbanCatalog.Prefix + "CarX"), .02f, PlayerPrefs.GetFloat(UrbanCatalog.Prefix + "CarZ"))), false, PlayerPrefs.GetInt(UrbanCatalog.Prefix + "CarType", 0));
                Owned.transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat(UrbanCatalog.Prefix + "CarYaw"), 0);
                Owned.fuel = PlayerPrefs.GetFloat(UrbanCatalog.Prefix + "CarFuel", 45);
                Owned.designVariant=PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarDesign",Owned.designVariant);
                Owned.InitializeDurability();
                Owned.health = PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarDurabilityVersion",0)>0?Mathf.Clamp(PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarHealth",Owned.MaxHealth),0,Owned.MaxHealth):Mathf.Clamp01(PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarHealth",100)/100)*Owned.MaxHealth;
                Owned.owned = true;
            }

            if (game.stage == StageId.UrbanCity)
            {
                var parking = new Vector3(73, .02f, -254);
                if (!Owned || Vector3.Distance(Owned.transform.position, parking) > 8)
                    Spawn(parking, false);
                for (int i = 0; i < 32; i++)
                    SpawnTraffic();
            }
        }

        public CityVehicle Spawn(Vector3 position, bool ai, int variant = -1)
        {
            var prefab = variant < 0 ? vehiclePrefab : vehiclePrefabs != null && vehiclePrefabs.Length > variant ? vehiclePrefabs[variant] : vehiclePrefab;
            var c = Instantiate(prefab, position, Quaternion.identity);
            VehicleFleet.Configure(c,variant);
            FleetDesign.Configure(c);
            c.gameObject.SetActive(true);
            c.occupied = c.traffic = ai;
            c.owned = false;
            Cars.Add(c);
            var paints = new[]
            {
                "SedanIvory",
                "SedanRed",
                "DistrictBlue",
                "DistrictWarm"
            };
            string paint = paints[random.Next(paints.Length)];
            foreach (var mesh in c.GetComponentsInChildren<MeshRenderer>())
                if (c.type == CityVehicleType.Sedan && (mesh.name == "Sculpted chassis" || mesh.name == "Sedan roof"))
                    mesh.sharedMaterial = Resources.Load<Material>("Materials/" + paint);
            return c;
        }

        int TrafficVariant() { int n=random.Next(10);return n<5?0:n<7?1:n==7?3:n==8?4:5; }

        void SpawnTraffic()
        {
            if (!game || !vehiclePrefab)
                return;
            var p = game.Player.transform.position;
            if(ExpansionRoads.Outside(p))
            {
                if(FourCityCatalog.CityAt(p)==2)return;
                var loop=LocalCityRoutes.Loop(p);int closest=0;float best=float.MaxValue;
                for(int i=0;i<loop.Length;i++){float d=(p-loop[i]).sqrMagnitude;if(d<best){best=d;closest=i;}}
                int at=(closest+random.Next(7,18))%loop.Length;foreach(var existingCar in Cars)if(existingCar&&Vector3.Distance(existingCar.transform.position,loop[at])<20)return;
                var car=Spawn(loop[at],true,TrafficVariant());car.route=loop;car.waypoint=(at+1)%loop.Length;car.speed=7;Vector3 dnext=loop[car.waypoint]-car.transform.position;car.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(-dnext.z,dnext.x)*Mathf.Rad2Deg,0);return;
            }
            int col = Mathf.Clamp(Mathf.RoundToInt((p.x - 40) / 140) + (random.Next(3) - 1), 0, 4), row = Mathf.Clamp(Mathf.RoundToInt((p.z + 280) / 140) + (random.Next(3) - 1), 0, 3);
            float x = 40 + col * 140, z = -280 + row * 140;
            var route = CityRoadNetwork.TrafficLoop(col, row);
            int start = random.Next(4) * 7 + 6;
            int next = (start + 1) % route.Length;
            var position = Vector3.Lerp(route[start], route[next], .25f + (float)random.NextDouble() * .5f);
            foreach (var existing in Cars)
                if (existing && Vector3.Distance(existing.transform.position, position) < 18)
                    return;
            var c = Spawn(position, true, TrafficVariant());
            c.route = route;
            c.waypoint = next;
            c.speed = 4;
            c.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(-(route[next] - position).z, (route[next] - position).x) * Mathf.Rad2Deg, 0);
        }

        public void BeforeInput(ref ControlFrame input, float dt)
        {
            if(MapOpen&&!input.map){input.move=Vector2.zero;input.attack=input.interact=input.passenger=input.exit=false;}
            CityBusService.Instance?.BeforeInput(ref input);
            if (input.map)
                MapOpen = !MapOpen;
            if(BikeDeliveryPending&&game.stage==StageId.UrbanCity)SummonBike();
            nearest = null;
            Prompt = "";
            if (Current)
            {
                bool pump = NearPump(Current.transform.position);
                Prompt = Refueling ? "주유 중 · E 중단 / 출발하면 자동 중단" : pump && Mathf.Abs(Current.speed) < .7f ? "E 주유 시작   ·   F 하차" : VehicleSeats.Name(Current,SeatIndex)+" · "+(SeatIndex==0?VehicleSeats.Controls(Current):"승객으로 이동 중 · F 하차");
                if (input.interact)
                {
                    input.interact = false;
                    if (pump && Mathf.Abs(Current.speed) < .7f)
                        Refueling = !Refueling;
                    else
                        Exit();
                }

                if (input.exit)
                {
                    Exit();
                }

                if (Current && Refueling)
                {
                    if (Mathf.Abs(input.move.y) > .1f || Mathf.Abs(Current.speed) > .8f)
                        Refueling = false;
                    else
                    {
                        fuelUnits += 10 * dt;
                        if (fuelUnits >= 1)
                        {
                            fuelUnits -= 1;
                            float litres = Mathf.Min(1, CityVehicle.Capacity - Current.fuel);
                            if (litres > 0 && LifeState.Spend(Mathf.CeilToInt(litres * 6)))
                                Current.fuel += litres;
                            else if (litres > 0)
                            {
                                Refueling = false;
                                game.Toast("잔액 부족 · 주유는 1 L당 6 C입니다.");
                            }
                        }

                        if (Current.fuel >= CityVehicle.Capacity)
                        {
                            Refueling = false;
                            game.Toast("주유 완료 · 안전 운전하세요");
                        }
                    }
                }

                bool armed=Current&&(Current.type==CityVehicleType.Tank||Current.type==CityVehicleType.Fighter||Current.type==CityVehicleType.CombatHelicopter);
                input.grapple = input.dash = input.skill = false;
                if(!armed||SeatIndex>0)input.secondaryFire=false;
                Prompt+=" · B 크락션"+(!armed||SeatIndex>0?" · 좌클릭 사격 / R 장전":"");
                var intercity=Current?Current.GetComponent<IntercityService>():null;if(intercity)Prompt+=" · "+intercity.Status;
            }
            else
            {
                float best = 5.3f;
                foreach (var c in Cars)
                    if (c && !c.Wrecked && !c.GetComponent<CityBusLine>())
                    {
                        float d = Vector3.Distance(VehicleSeats.Door(c), game.Player.transform.position);
                        if (d < best)
                        {
                            best = d;
                            nearest = c;
                        }
                    }

                if (nearest && !(CityBusService.Instance && CityBusService.Instance.Riding))
                {
                    var taxi=nearest.GetComponent<CityTaxiService>();
                    if(taxi)
                    {
                        Prompt=(taxi.Air?"공중 무인택시 · 120 C":"수상택시 · 35 C")+" · E 탑승";
                        if(input.interact||input.passenger){input.interact=input.passenger=false;taxi.Board();}
                    }
                    else
                    {
                    Prompt = VehicleSeats.Title(nearest.type)+" · E "+(nearest.IsAircraft?"조종석":nearest.IsWatercraft?"선장석":"운전석")+(VehicleSeats.Count(nearest)>1?" / G 조수석·승객석":"");
                    var scheduled=nearest.GetComponent<PassengerRoute>();if(scheduled)Prompt+=" · "+scheduled.Status;
                    var service=nearest.GetComponent<IntercityService>();if(service)Prompt+=" · "+service.Status+" / G 승차권 구매";
                    if(input.passenger&&service){input.passenger=false;service.BuyTicket();}
                    if(input.passenger&&VehicleSeats.Count(nearest)>1){input.passenger=false;int count=nearest.GetComponent<VehicleCabin>()?nearest.GetComponent<VehicleCabin>().PassengerCount:0;Enter(nearest,Mathf.Clamp(count+1,1,VehicleSeats.Count(nearest)-1));}
                    if (input.interact)
                    {
                        input.interact = false;
                        Enter(nearest);
                    }
                    }
                }
            }

            if (MapOpen)
            {
                input.move = Vector2.zero;
                input.guard = true;
                input.attack = input.grapple = input.jump = input.dash = input.skill = false;
                input.secondaryFire = input.reload = input.vehicleView = false;
            }

            spawnClock -= dt;
            saveClock -= dt;
            if (game.stage == StageId.UrbanCity && spawnClock <= 0)
            {
                spawnClock = 1.25f;
                int count = 0;
                for (int i = Cars.Count - 1; i >= 0; i--)
                {
                    var c = Cars[i];
                    if (!c)
                    {
                        Cars.RemoveAt(i);
                        continue;
                    }

                    if (c != Owned && !c.GetComponent<CityBusLine>() && !c.GetComponent<EmergencyAmbulance>() && !VehicleFleet.Persistent(c) && Vector3.Distance(c.transform.position, game.Player.transform.position) > 420)
                    {
                        Destroy(c.gameObject);
                        Cars.RemoveAt(i);
                    }
                    else if (c.traffic && !c.GetComponent<CityBusLine>() && Vector3.Distance(c.transform.position,game.Player.transform.position)<420)
                        count++;
                }

                if (count < 48)
                    for(int spawn=0;spawn<4 && count+spawn<48;spawn++) SpawnTraffic();
                int parked = 0;
                foreach (var car in Cars)
                    if (car && !car.traffic && !car.owned)
                        parked++;
                if (parked < 32)
                {
                    int id = random.Next(UrbanCatalog.SiteCount);
                    var p = UrbanCatalog.Center(id) + new Vector3(-12, .02f, -44);
                    if (Vector3.Distance(p, game.Player.transform.position) < 210 && Vector3.Distance(p, game.Player.transform.position) > 45)
                        Spawn(p, false, TrafficVariant());
                }
            }

            foreach (var c in Cars)
                if (c && (c != Current || SeatIndex>0))
                    c.TickTraffic(dt);
            if (saveClock <= 0)
            {
                saveClock = 8;
                SaveCar();
            }
        }

        public void Tick(ControlFrame input, float dt)
        {
            if (!Current)
                return;
            game.Player.TickVehicleStatus(dt);
            if(SeatIndex==0)Current.Drive(input, dt);
            game.Player.transform.position = Current.transform.TransformPoint(VehicleSeats.Local(Current,SeatIndex));
            if(input.horn)Current.GetComponent<VehicleHorn>()?.Honk();
            bool armed=Current.GetComponent<VehicleArmament>()&&SeatIndex==0;
            if(!armed)game.Player.TickMountedCombat(input,dt);
        }

        public bool Enter(CityVehicle car,int seat=0)
        {
            if (!car || Current || car.Wrecked)
                return false;
            if (Mathf.Abs(car.speed) > 12)
            {
                game.Toast("차가 너무 빠릅니다 · 속도가 줄었을 때 접근하세요");
                return false;
            }

            if (seat==0 && car.occupied && !car.owned)
            {
                Hijacks++;
                CityPopulation.Instance?.Eject(car.transform.position - car.transform.forward * 2);
                WantedSystem.Report(car.GetComponent<PoliceCar>() ? 22 : 9, car.transform.position);
                game.Toast("운전자가 하차했습니다");
            }

            // Entering an empty parked vehicle does not create a crime report.

            SeatIndex=Mathf.Clamp(seat,0,VehicleSeats.Count(car)-1);
            Current=car;
            if(SeatIndex==0){if(Owned)Owned.owned=false;Owned=car;car.owned=car.occupied=true;}
            car.ApplyCustomization();
            if(SeatIndex==0){car.traffic = false;car.speed = 0;}
            Entries++;
            game.Toast(VehicleSeats.Title(car.type)+" · "+VehicleSeats.Name(car,SeatIndex)+" 탑승");
            game.Player.Rope.Release();
            game.Player.Respawn(game.Player.transform.position, false);
            game.Player.Controller.enabled = false;
            playerRenderers = game.Player.GetComponentsInChildren<Renderer>();
            rendererStates = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                rendererStates[i] = playerRenderers[i].enabled;
                playerRenderers[i].enabled = false;
            }

            contact = game.Player.GetComponent<ContactShadow>();
            if (contact)
                contact.enabled = false;
            game.Audio.Play("urban_door", car.transform.position, .25f, 1);
            SaveCar();
            return true;
        }

        public bool Exit()
        {
            if (!Current)
                return false;
            if(Mathf.Abs(Current.speed)>2 || Current.IsAircraft&&Current.transform.position.y>3&&!VehicleGround.Sample(Current,Current.transform.position,0,3,out _))return BailOut();
            Vector3 point = Vector3.zero;
            bool found = false;
            foreach (var offset in new[]
            {
                Current.transform.forward * (Current.HalfWidth + 1.5f),
                -Current.transform.forward * (Current.HalfWidth + 1.5f),
                -Current.Forward * (Current.HalfLength + 1.5f)
            }

            )
            {
                var p = Current.IsSpecial?VehicleSeats.Door(Current):Current.transform.position + offset;
                var intercity=Current.GetComponent<IntercityService>();if(intercity&&intercity.Boarding)p=intercity.ExitPoint;
                if(VehicleGround.Sample(Current,p,3,8,out var floor))p.y=floor.point.y+.06f;
                else if(Current.IsWatercraft)p.y=OceanLife.Surface;
                if (Physics.CheckCapsule(p + Vector3.up * .4f, p + Vector3.up * 1.7f, .34f, 1, QueryTriggerInteraction.Ignore))
                    continue;
                point = p + Vector3.up * .15f;
                found = true;
                break;
            }

            if (!found)
            {
                game.Toast("문 옆 공간이 좁습니다 · 넓은 곳에 정차하세요");
                return false;
            }

            if(SeatIndex==0)Current.speed = 0;
            if(SeatIndex==0)Current.occupied = false;
            game.Audio.Play("urban_door", point, .25f, 1);
            Current = null;
            SeatIndex=0;
            Refueling = false;
            game.Player.Respawn(point, false);
            for (int i = 0; i < playerRenderers.Length; i++)
                if (playerRenderers[i])
                    playerRenderers[i].enabled = rendererStates[i];
            if (contact)
                contact.enabled = true;
            SaveCar();
            return true;
        }

        public bool NearPump(Vector3 p)
        {
            if (game.stage != StageId.UrbanCity)
                return false;
            for (int i = 0; i < UrbanCatalog.SiteCount; i++)
                if (UrbanCatalog.Kind(i) == 11 && Vector3.Distance(p, UrbanCatalog.Pump(i)) < 8)
                    return true;
            return false;
        }

        public void SaveCar()
        {
            if (LifeState.SuppressSave || !Owned || !game || Owned.IsSpecial)
                return;
            string p = UrbanCatalog.Prefix;
            PlayerPrefs.SetInt(p + "Car", 1);
            PlayerPrefs.SetInt(p + "CarStage", (int)game.stage);
            PlayerPrefs.SetFloat(p + "CarX", Owned.transform.position.x);
            PlayerPrefs.SetFloat(p + "CarZ", Owned.transform.position.z);
            PlayerPrefs.SetFloat(p + "CarYaw", Owned.transform.eulerAngles.y);
            PlayerPrefs.SetFloat(p + "CarFuel", Owned.fuel);
            PlayerPrefs.SetFloat(p + "CarHealth", Owned.health);
            PlayerPrefs.SetInt(p + "CarDurabilityVersion",1);
            PlayerPrefs.SetInt(p+"CarDesign",Owned.designVariant);
            PlayerPrefs.SetInt(p + "CarType", (int)Owned.type);
            PlayerPrefs.Save();
        }

        public void EmergencyExit(CityVehicle car)
        {
            if (Current != car)
                return;
            if (!Exit())
            {
                var point = CityRoadNetwork.Sidewalk(car.transform.position);
                Current.occupied = false;
                Current = null;
                Refueling = false;
                game.Player.Respawn(point, false);
                for (int i = 0; i < playerRenderers.Length; i++)
                    if (playerRenderers[i])
                        playerRenderers[i].enabled = rendererStates[i];
                if (contact)
                    contact.enabled = true;
            }

            SaveCar();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

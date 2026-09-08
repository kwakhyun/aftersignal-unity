"""Integrate the second city without rebuilding the existing authored scenes."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
def edit(path,old,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig')
 if old in s:p.write_text(s.replace(old,new),encoding='utf-8')
 elif new not in s:raise RuntimeError('Missing anchor '+path+': '+old[:60])
edit('Assets/AfterSignal/Runtime/City/Expansion/ExpansionRoads.cs','roads.Add(r.ToArray());}return roads;','roads.Add(r.ToArray());}roads.AddRange(NeonHarbor.Roads);return roads;')
edit('Assets/AfterSignal/Runtime/City/Expansion/ExpansionRoads.cs','int start=NearestNode(from),end=NearestNode(to);','Link(NeonHarbor.OldDock,NeonHarbor.NewDock);Link(NeonHarbor.OldDock,new Vector3(1250,.035f,-666));Link(NeonHarbor.NewDock,new Vector3(920,.035f,-2412));int start=NearestNode(from),end=NearestNode(to);')
edit('Assets/AfterSignal/Runtime/Presentation/FacilityPeople.cs','"NightMarket","Utilities"','"NightMarket","Utilities","Pelagic","NeonTrade","Arcology","CanalLife","CivicFuture","SkyWard"')
edit('Assets/AfterSignal/Runtime/Presentation/FacilityPeople.cs','"항만 구급대원"','"항만 구급대원","해양 로봇 기술자","해조류 연구원","해협 운항 관제사","침몰선 잠수부","네온 간판 장인","수로시장 요리사","해협 특송원","거리 연주자","기업 조사관","데이터 기록사","도시 설계사","기업 보안관","여객항 매표원","항만 기관사","클럽 가수","노바 대학생","해안경비대원","방벽 정비사","노바 의사","교통 검표원","드론 조종사","공중정원 관리사","해협 기자","시민의회 의원"')
edit('Assets/AfterSignal/Runtime/Presentation/FacilityPeople.cs','Mathf.Clamp(role,0,23)','Mathf.Clamp(role,0,Jobs.Length-1)')
edit('Assets/AfterSignal/Runtime/Presentation/FacilityPeople.cs','key.StartsWith("Facility")&&facing==3&&','key.StartsWith("Facility")&&System.Array.IndexOf(Sheets,key.Substring(8,key.Length-9))<6&&facing==3&&')
edit('Assets/AfterSignal/Runtime/City/Expansion/ExpansionWorld.cs','"도시기억관리청"};','"도시기억관리청","노바 해협도시"};')
edit('Assets/AfterSignal/Runtime/City/Expansion/ExpansionWorld.cs','new Vector3(1270,0,284)};','new Vector3(1270,0,284),new Vector3(920,0,-2460)};')
edit('Assets/AfterSignal/Runtime/Presentation/Hud/UrbanHud.cs','1147,287,264,26','1147,318,264,26')
edit('Assets/AfterSignal/Runtime/Presentation/Hud/UrbanHud.cs','320 + (i % 20) * 18, 264, 18','350 + (i % 20) * 16, 264, 16')
edit('Assets/AfterSignal/Runtime/City/Traffic/VehicleFleet.cs','public static string Controls(CityVehicle c) =>','public static string Controls(CityVehicle c) => c.type==CityVehicleType.Tank ? "W/S 전후진 · A/D 궤도 선회 · 마우스 포탑 · 좌/우클릭 주포/기관총 · C 시점" :')
edit('Assets/AfterSignal/Runtime/City/Traffic/VehicleFleet.cs','· F 하차" : c.IsWatercraft','· C 시점 · 우클릭 미사일 / R 장전 · F 하차" : c.IsWatercraft')
edit('Assets/AfterSignal/Runtime/City/Traffic/VehicleFleet.cs','· F 하선" :','· C 시점 · F 하선" :')
edit('Assets/AfterSignal/Runtime/City/Traffic/VehicleFleet.cs','· E 하차";','· C 시점 · E 하차";')
print('Neon Harbor runtime integration updated.')

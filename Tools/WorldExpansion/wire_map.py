from pathlib import Path
p=Path(__file__).resolve().parents[2]/'Assets/AfterSignal/Runtime/Presentation/Hud/UrbanHud.cs'
s=p.read_text(encoding='utf-8-sig')
a=s.index('            for (int i = 0; i < 6; i++)');b=s.index('            for (int i = 0; i < UrbanCatalog.SiteCount; i++)',a)
s=s[:a]+'''            AddExpansionMap(parent,false);
'''+s[b:]
a=s.index('            for (int i = 0; i < 6; i++)');b=s.index('            for (int i = 0; i < UrbanCatalog.SiteCount; i++)',a)
s=s[:a]+'''            AddExpansionMap(cityMapOverlay.transform,true);
            for(int i=0;i<ExpansionWorld.Names.Length;i++)
            {
                int id=i;var p=ExpansionWorld.Places[i];var m=ExpansionRoads.Map(p);
                var b=MakeButton(cityMapOverlay.transform,ExpansionWorld.Names[i],872+(i%2)*275,118+(i/2)*32,264,28,()=>{ExpansionWorld.Selected=id;selectedSite=-1;});
                b.GetComponentInChildren<Text>().fontSize=14;
                Label(cityMapOverlay.transform,ExpansionWorld.Names[i],55+m.x*790,675-m.y*550,150,20,12,mint);
            }
            MakeButton(cityMapOverlay.transform,"메인 의뢰 경로",872,286,264,26,()=>{ExpansionWorld.Selected=-1;selectedSite=-1;});
'''+s[b:]
s=s.replace('20 + p.x / 790 * 272, 145 - (p.z + 330) / 660 * 116','20 + ExpansionRoads.Map(p).x * 272, 145 - ExpansionRoads.Map(p).y * 116')
s=s.replace('55 + p.x, 675 - (p.z + 330) / 660 * 550, 32, 27, () => selectedSite = id','55 + ExpansionRoads.Map(p).x*790, 675 - ExpansionRoads.Map(p).y*550, 14, 14, () => {selectedSite = id;ExpansionWorld.Selected=-1;}')
s=s.replace('125 + (i % 20) * 27, 264, 24, () => selectedSite = id','320 + (i % 20) * 18, 264, 18, () => {selectedSite = id;ExpansionWorld.Selected=-1;}')
s=s.replace('16 + p.x / 790 * 272, -(145 - (p.z + 330) / 660 * 116)','16 + ExpansionRoads.Map(p).x*272, -(145 - ExpansionRoads.Map(p).y*116)')
s=s.replace('52 + p.x, -(676 - (p.z + 330) / 660 * 550)','52 + ExpansionRoads.Map(p).x*790, -(676 - ExpansionRoads.Map(p).y*550)')
s=s.replace('cityDestination.text = selectedSite < 0 ?', 'cityDestination.text = ExpansionWorld.Selected>=0 ? ExpansionWorld.Names[ExpansionWorld.Selected]+" · "+Vector3.Distance(game.Player.transform.position,ExpansionWorld.Places[ExpansionWorld.Selected]).ToString("0")+" m" : selectedSite < 0 ?')
i=s.index('        void EnsureCityHud()')
s=s[:i]+'''        void AddExpansionMap(Transform parent,bool large)
        {
            var go=new GameObject("Geographic street atlas",typeof(RectTransform),typeof(ExpansionMapGraphic));go.transform.SetParent(parent,false);
            var map=go.GetComponent<ExpansionMapGraphic>();map.raycastTarget=false;Rect(map.rectTransform,large?55:20,large?125:29,large?790:272,large?550:116);
        }

'''+s[i:]
p.write_text(s,encoding='utf-8')

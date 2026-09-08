from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'Assets/AfterSignal/Runtime/City/Life/CitySocial.cs';s=p.read_text(encoding='utf-8-sig')
a=s.index('    public static class NpcVoice');b=s.index('    public sealed class CitySocial',a)
s=s[:a]+'''    public static class NpcVoice
    {
        public static string Role(CityNpc n)=>n.GetComponent<DirectionalPerson>()?.art??"CivilianMan";
        public static string Hurt(CityNpc n,bool down)=>NpcDialogueBank.Line(n,down?"down":"hurt");
        public static void React(CityNpc n,bool down){if(!n)return;n.SocialUntil=0;NpcSpeech.Say(n,Hurt(n,down),5,10);}
        public static string Greeting(CityNpc n)=>NpcDialogueBank.Line(n,"greeting");
    }
'''+s[b:]
a=s.index('            int topic=Random.Range(0,5);');b=s.index('\n        }',a)
s=s[:a]+'''            var pair=NpcDialogueBank.Exchange(a);
            NpcSpeech.Say(a,pair[0],4,1);if(pair.Length>1)StartCoroutine(Reply(a,b,pair[1]));'''+s[b:]
p.write_text(s,encoding='utf-8')
p=root/'Documentation/ASSETS.md';s=p.read_text(encoding='utf-8-sig');i=s.find('### Seo original 3D trial')
if i>=0:s=s[:i]+'''### Seo 3D trial withdrawn (2026-09-08)

The rejected trial character, rig, scripts and build have been withdrawn from this project. Seo uses the existing directional sprites. The withdrawn source is archived outside this repository in `../AFTERSIGNAL-Withdrawn/Seo3D-20260908`.
'''
p.write_text(s,encoding='utf-8')

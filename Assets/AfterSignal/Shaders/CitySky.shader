Shader "AfterSignal/City Sky"
{
 Properties {_Horizon("Horizon",Color)=(.3,.4,.5,1) _Zenith("Zenith",Color)=(.1,.2,.4,1) _Night("Night",Float)=0 _SunColor("Sun",Color)=(1,.9,.6,1) _SunDirection("Direction",Vector)=(.3,.5,.5,0)}
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Horizon,_Zenith,_SunColor,_SunDirection;float _Night;
 struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 dir:TEXCOORD0;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.dir=i.positionOS.xyz;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);}
 float clouds(float2 p){float value=0,amplitude=.5;for(int n=0;n<5;n++){value+=noise(p)*amplitude;p=p*2.03+7.9;amplitude*=.5;}return value;}
 half4 frag(V i):SV_Target
 {
  float3 d=normalize(i.dir);float h=saturate(d.y);half3 c=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.55));float solar=dot(d,normalize(_SunDirection.xyz));
  float2 cloudUv=d.xz/max(.12,d.y+.18)*1.6+float2(_Time.y*.002,0);
  float density=smoothstep(.46,.73,clouds(cloudUv))*smoothstep(0,.18,h);float rim=smoothstep(.35,.58,clouds(cloudUv+.025));
  half3 cloudColor=lerp(half3(.52,.57,.62),_SunColor.rgb*.94,saturate(solar*.7+.2));cloudColor=lerp(cloudColor,half3(.035,.052,.08),_Night);
  c=lerp(c,cloudColor*(.72+rim*.28),density*.85);
  c+=_SunColor.rgb*(pow(saturate(solar),650)*2+pow(saturate(solar),16)*.16)*(1-_Night*.7)*(1-density*.8);
  float3 grid=floor(d*520);float star=frac(sin(dot(grid,float3(127.1,311.7,74.7)))*43758.5453);c+=step(.9988,star)*_Night*smoothstep(.04,.4,h)*.65*(1-density);
  return half4(c,1);
 }
 ENDHLSL}
 }
}

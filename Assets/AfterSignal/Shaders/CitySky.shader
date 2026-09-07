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
 half4 frag(V i):SV_Target{float3 d=normalize(i.dir);float h=saturate(d.y);half3 c=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.55));float solar=dot(d,normalize(_SunDirection.xyz));c+=_SunColor.rgb*(pow(saturate(solar),650)*2+pow(saturate(solar),16)*.16)*(1-_Night*.7);float3 grid=floor(d*520);float hash=frac(sin(dot(grid,float3(127.1,311.7,74.7)))*43758.5453);c+=step(.9988,hash)*_Night*smoothstep(.04,.4,h)*.65;return half4(c,1);}
 ENDHLSL}
 }
}

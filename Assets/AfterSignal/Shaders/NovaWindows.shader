Shader "AfterSignal/NovaWindows"
{
 Properties{_BaseColor("Tint",Color)=(.11,.23,.27,1) _EmissionColor("Night illumination",Color)=(.12,.32,.3,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_EmissionColor;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.p=TransformWorldToHClip(o.w);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target
 {
  float3 n=normalize(i.n);float side=abs(n.x)>.5?i.w.z:i.w.x;
  float2 cell=floor(float2(side/2.8,i.w.y/4));float rand=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
  float jamb=smoothstep(.018,.05,frac(side/2.8))*smoothstep(.018,.05,1-frac(side/2.8));
  float lit=step(.29,rand);float blind=lerp(.8,1,step(.18,frac(i.w.y*9)));
  float3 warm=lerp(float3(.52,.86,1),float3(1,.68,.37),step(.69,rand));
  float3 eye=normalize(_WorldSpaceCameraPos-i.w);float reflection=pow(1-saturate(abs(dot(n,eye))),3);
  Light sun=GetMainLight();float3 glass=_BaseColor.rgb*(SampleSH(n)*.5+sun.color*saturate(dot(n,sun.direction))*.24)+reflection*float3(.06,.11,.15);
  float3 color=lerp(float3(.025,.037,.046),glass+warm*_EmissionColor.rgb*lit*blind,jamb);
  return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL}
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}

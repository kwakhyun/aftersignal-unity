Shader "AfterSignal/NovaSurface"
{
 Properties{_BaseMap("Concrete detail",2D)="white"{} _BaseColor("Tint",Color)=(.5,.6,.65,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_BaseMap_ST;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.p=TransformWorldToHClip(o.w);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target
 {
  float3 n=normalize(i.n);float2 uv=abs(n.y)>.5?i.w.xz:abs(n.x)>.5?i.w.zy:i.w.xy;
  float3 concrete=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv/4).rgb;
  float2 seam=abs(frac(uv/4)-.5);float groove=1-smoothstep(.484,.498,max(seam.x,seam.y))*.25;
  float3 albedo=_BaseColor.rgb*lerp(float3(.7,.7,.7),concrete, .55)*groove;
  Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));float light=saturate(dot(n,sun.direction))*sun.shadowAttenuation;
  float3 color=albedo*(SampleSH(n)+sun.color*light);
  return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL}
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}

Shader "AfterSignal/PelagicFloor"
{
 Properties{_BaseMap("Seabed albedo",2D)="white"{} _BaseColor("Color",Color)=(.32,.42,.4,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseColor;
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
 V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);return o;}
 half4 frag(V i):SV_Target
 {
  float2 p=i.w.xz*.75;float t=_Time.y*.55;
  float a=sin(p.x+sin(p.y*.71+t)*2+t),b=sin(p.y+cos(p.x*.68-t)*1.8-t);
  float caustic=pow(saturate(1-abs(a+b)*.8),12)*.24;
  float3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.xz/5).rgb*_BaseColor.rgb;
  Light light=GetMainLight();float diffuse=.35+saturate(dot(normalize(i.n),light.direction))*.65;
  float3 color=albedo*diffuse+float3(.08,.26,.22)*caustic;
  float fog=1-exp(-distance(_WorldSpaceCameraPos,i.w)*.023);if(_WorldSpaceCameraPos.y<-.95)color=lerp(color,float3(.025,.19,.24),fog);
  return half4(color,1);
 }
 ENDHLSL}}
}

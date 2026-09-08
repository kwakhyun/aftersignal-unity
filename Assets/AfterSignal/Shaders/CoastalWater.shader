Shader "AfterSignal/CoastalWater"
{
 Properties{_BaseColor("Color",Color)=(.04,.2,.25,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;};
 float Wave(float2 p){return sin(p.x*.13+_Time.y*.7)*.12+sin(p.y*.23-_Time.y*.55)*.08+sin((p.x+p.y)*.42+_Time.y)*.04;}
 V vert(A i){V o;float3 w=TransformObjectToWorld(i.p.xyz);w.y+=Wave(w.xz);o.w=w;o.p=TransformWorldToHClip(w);return o;}
 half4 frag(V i):SV_Target
 {
  float h=Wave(i.w.xz);float3 n=normalize(float3((h-Wave(i.w.xz+float2(.1,0)))*4,1,(h-Wave(i.w.xz+float2(0,.1)))*4));
  float3 eye=normalize(_WorldSpaceCameraPos-i.w);if(eye.y<0)n=-n;
  float fresnel=pow(1-saturate(dot(n,eye)),4);Light sun=GetMainLight();float glint=pow(saturate(dot(n,normalize(eye+sun.direction))),140);
  float2 uv=i.p.xy/_ScaledScreenParams.xy;float opaque=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);float water=-TransformWorldToView(i.w).z;
  float depth=max(0,opaque-water);float shallows=exp(-depth*.23);float foam=(1-smoothstep(.05,1.4,depth))*(.45+.3*sin(i.w.x*2+i.w.z*1.5+_Time.y*2));
  float3 color=lerp(float3(.018,.08,.13),float3(.04,.37,.36),shallows);color=lerp(color,float3(.21,.35,.44),fresnel);
  color+=sun.color*glint*.8+foam*.65;float alpha=saturate(.58+depth*.018+fresnel*.25);
  return half4(color,alpha);
 }
 ENDHLSL}}
}

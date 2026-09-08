Shader "AfterSignal/UnderwaterHaze"
{
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay"}
 Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);return o;}
 half4 frag(V i):SV_Target
 {
  float2 uv=i.p.xy/_ScaledScreenParams.xy;
  float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
  float veil=clamp(1-exp(-depth*.043),.12,.97);
  float glow=pow(saturate(uv.y),3)*.07;
  return half4(float3(.024,.18,.23)+float3(.03,.07,.065)*glow,veil);
 }
 ENDHLSL}}
}

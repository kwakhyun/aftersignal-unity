Shader "AfterSignal/Structural Glass"
{
 Properties { _BaseColor("Transmission tint",Color)=(.1,.28,.32,.12) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float fog:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);
    half3 n=SafeNormalize(i.normalWS),view=GetWorldSpaceNormalizeViewDir(i.positionWS);
    half facing=abs(dot(n,view));half fresnel=pow(1-saturate(facing),5);
    // Thin architectural glazing: bounded reflected sky, no opaque specular lobe
    // and no recursively sampled local reflection capture covering an entire dome.
    half3 reflected=reflect(-view,n);half3 sky=lerp(half3(.12,.20,.25),half3(.32,.45,.56),saturate(reflected.y));
    Light sun=GetMainLight();half glint=pow(saturate(abs(dot(n,SafeNormalize(view+sun.direction)))),96)*.08;
    half3 color=lerp(_BaseColor.rgb,sky,fresnel*.65)+min(sun.color,2)*glint;
    return half4(MixFog(min(color,1),i.fog),saturate(_BaseColor.a+fresnel*.16));
   }
   ENDHLSL
  }
 }
}

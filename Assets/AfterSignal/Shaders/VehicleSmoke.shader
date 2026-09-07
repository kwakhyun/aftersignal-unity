Shader "AfterSignal/Vehicle Smoke"
{
 Properties { _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;}
   half4 frag(V i):SV_Target {float2 uv=(floor(i.uv*24)/24-.5)*2;float d=dot(uv,uv);half a=saturate((1-d)*2);a*=a;return half4(i.color.rgb,i.color.a*a);}
   ENDHLSL
  }
 }
}

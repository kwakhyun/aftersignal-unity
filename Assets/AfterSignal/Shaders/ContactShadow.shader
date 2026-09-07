Shader "AfterSignal/Contact Shadow" {
 Properties { _BaseColor("Tint",Color)=(0,0,0,.5) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;}; struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {float d=length(i.uv*2-1);return half4(_BaseColor.rgb,_BaseColor.a*pow(saturate(1-d*d),2));}
 ENDHLSL }
 } }

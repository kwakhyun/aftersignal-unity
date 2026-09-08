Shader "AfterSignal/WorldLettering"
{
 Properties{_MainTex("Font atlas",2D)="white"{}}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}ZWrite Off ZTest LEqual Cull Back Blend SrcAlpha OneMinusSrcAlpha
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 float _CityNightGlow;
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c;return o;}
 half4 frag(V i):SV_Target{return half4(i.c.rgb*(1+_CityNightGlow*1.8),i.c.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
 ENDHLSL}}
}

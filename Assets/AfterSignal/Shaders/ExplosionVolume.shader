Shader "AfterSignal/ExplosionVolume"
{
 Properties{_MainTex("Unused",2D)="white"{} _Smoke("Smoke",Float)=0}
 SubShader{Tags{"Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Smoke;
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 f=frac(p);float2 q=f*f*(3-2*f);float2 a=floor(p);return lerp(lerp(hash(a),hash(a+float2(1,0)),q.x),lerp(hash(a+float2(0,1)),hash(a+1),q.x),q.y);}
 half4 frag(V i):SV_Target{float2 p=i.uv*2-1;float n=noise(i.uv*5+float2(_Time.y*.45,-_Time.y*.7))*.55+noise(i.uv*13-_Time.y*.35)*.3+noise(i.uv*31)*.15;float edge=1-length(p);float a=saturate((edge+n*.45-.35)*3.8)*i.c.a;float hot=saturate(edge*.8+n*.7);float3 fire=lerp(float3(.15,.022,.007),lerp(float3(2.6,.33,.025),float3(5,3.6,.8),pow(hot,2)),hot);float3 smoke=lerp(float3(.035,.042,.05),float3(.28,.29,.3),n*.7+edge*.3);return half4(lerp(fire,smoke,_Smoke)*i.c.rgb,a);}
 ENDHLSL}}
}

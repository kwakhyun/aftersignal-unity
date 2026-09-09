Shader "AfterSignal/CombatParticle"
{
 Properties { _MainTex("Texture",2D)="white"{} }
 SubShader
 {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   V vert(A v){V o;o.position=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;o.color=v.color;return o;}
   half4 frag(V i):SV_Target
   {
    float2 p=i.uv*2-1;float r=length(p);float grain=sin(p.x*19+sin(p.y*13))*sin(p.y*21+p.x*7)*.12;
    half alpha=saturate((1-r+grain)*2);alpha*=alpha;
    return half4(i.color.rgb,alpha*i.color.a);
   }
   ENDHLSL
  }
 }
}

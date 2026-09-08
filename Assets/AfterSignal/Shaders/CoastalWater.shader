Shader "AfterSignal/CoastalWater"
{
 Properties{_BaseColor("Color",Color)=(.04,.2,.25,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;};
 float Wave(float2 p){return sin(p.x*.13+_Time.y*.7)*.12+sin(p.y*.23-_Time.y*.55)*.08+sin((p.x+p.y)*.42+_Time.y)*.04;}
 V vert(A i){V o;float3 w=TransformObjectToWorld(i.p.xyz);w.y+=Wave(w.xz);o.w=w;o.p=TransformWorldToHClip(w);return o;}
 half4 frag(V i):SV_Target{float h=Wave(i.w.xz);float3 n=normalize(float3((h-Wave(i.w.xz+float2(.1,0)))*4,1,(h-Wave(i.w.xz+float2(0,.1)))*4));float3 eye=normalize(_WorldSpaceCameraPos-i.w);float f=pow(1-saturate(dot(n,eye)),4);Light sun=GetMainLight();float glint=pow(saturate(dot(n,normalize(eye+sun.direction))),120);float foam=pow(saturate(h*3+.27),7)*.18;return half4(lerp(float3(.015,.115,.14),float3(.22,.37,.46),f)+sun.color*glint*.9+foam,1);}
 ENDHLSL}}
}

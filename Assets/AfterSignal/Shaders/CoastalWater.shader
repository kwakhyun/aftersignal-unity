Shader "AfterSignal/CoastalWater"
{
 Properties{_BaseColor("Water tint",Color)=(.025,.19,.23,1) _WaveStrength("Wave strength",Range(0,2))=1 _Smoothness("Smoothness",Range(0,1))=.93}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _WaveStrength,_Smoothness;
   CBUFFER_END
   struct A{float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float fog:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   float Swell(float2 p){return sin(dot(p,float2(.13,.047))-_Time.y*.82)*.16+sin(dot(p,float2(-.065,.24))+_Time.y*.62)*.085+sin(dot(p,float2(.34,.16))-_Time.y*1.25)*.035;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
   float2 Ripple(float2 p)
   {
    float t=_Time.y;
    return float2(cos(dot(p,float2(.13,.047))-t*.82)*.13*.16+cos(dot(p,float2(-.065,.24))+t*.62)*-.065*.085+cos(dot(p,float2(.34,.16))-t*1.25)*.34*.035,
     cos(dot(p,float2(.13,.047))-t*.82)*.047*.16+cos(dot(p,float2(-.065,.24))+t*.62)*.24*.085+cos(dot(p,float2(.34,.16))-t*1.25)*.16*.035)
     +float2(Noise(p*.84+float2(t*.27,t*.16))-.5,Noise(p*1.13+float2(43-t*.16,17+t*.21))-.5)*.065;
   }
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.w=TransformObjectToWorld(i.p.xyz);o.w.y+=Swell(o.w.xz)*_WaveStrength;o.p=TransformWorldToHClip(o.w);o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target
   {
    float3 eye=normalize(_WorldSpaceCameraPos-i.w);float2 slope=Ripple(i.w.xz)*_WaveStrength;float3 n=normalize(float3(-slope.x*2,1,-slope.y*2));if(eye.y<0)n=-n;
    float2 uv=i.p.xy/_ScaledScreenParams.xy;
    float opaque=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);float water=-TransformWorldToView(i.w).z;float depth=max(0,opaque-water);
    float2 refractedUV=saturate(uv+slope*.018*saturate(depth*.5));float displacedDepth=LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams);
    if(displacedDepth<water+.08)refractedUV=uv;
    float3 through=SampleSceneColor(refractedUV);float3 extinction=exp(-depth*float3(.35,.105,.065));
    float3 scatter=lerp(float3(.012,.06,.095),_BaseColor.rgb,exp(-depth*.08));float3 volume=through*extinction+scatter*(1-extinction);
    float fresnel=.025+.975*pow(1-saturate(dot(n,eye)),5);float3 reflected=reflect(-eye,n);
    float3 sky=lerp(float3(.12,.22,.29),float3(.26,.42,.53),saturate(reflected.y));
    float3 environment=GlossyEnvironmentReflection(reflected,1-_Smoothness,1);
    Light sun=GetMainLight();float3 h=normalize(eye+sun.direction);float sunGlint=pow(saturate(dot(n,h)),360)*1.7;
    float glitter=pow(saturate(dot(n,h)),90)*.13;
    float patches=Noise(i.w.xz*1.3+_Time.y*.12)*.65+Noise(i.w.xz*3.7-_Time.y*.23)*.35;
    float foam=(1-smoothstep(.03,.65,depth))*smoothstep(.48,.73,patches)*.55;
    float crests=smoothstep(.21,.27,Swell(i.w.xz))*saturate(length(slope)*3)*.12;
    float3 color=lerp(volume,max(environment,sky*.65),fresnel)+sun.color*(sunGlint+glitter)*(1-fresnel*.5);
    color=lerp(color,float3(.68,.83,.8),saturate(foam+crests));
    return half4(MixFog(color,i.fog),saturate(.73+depth*.022+fresnel*.24));
   }
   ENDHLSL
  }
 }
}

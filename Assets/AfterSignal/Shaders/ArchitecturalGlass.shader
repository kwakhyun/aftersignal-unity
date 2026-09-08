Shader "AfterSignal/Architectural Glass"
{
 Properties { _BaseColor("Glass tint",Color)=(.14,.24,.29,1) _EmissionColor("Office light",Color)=(1,.68,.34,1) _WindowScale("Window bay width / storey height",Vector)=(2.8,4,0,0) _Smoothness("Smoothness",Range(0,1))=.82 [HideInInspector] _BreakCount("Broken panes",Int)=0 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor,_EmissionColor;float4 _WindowScale;half _Smoothness;int _BreakCount;float4 _BreakCenters[32];
   CBUFFER_END
   float _CityNightGlow;
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
   V vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.world=TransformObjectToWorld(i.positionOS.xyz);o.normal=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);
    [branch] if(_BreakCount>0){[loop] for(int opening=0;opening<min(_BreakCount,32);opening++)clip(distance(i.world,_BreakCenters[opening].xyz)-_BreakCenters[opening].w);}
    half3 n=normalize(i.normal);float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    bool side=abs(n.x)>abs(n.z);float2 grid=float2(side?i.world.z:i.world.x,i.world.y)/_WindowScale.xy;
    float2 cell=floor(grid),uv=frac(grid);float random=hash(cell+floor(i.world.xz*.03));
    float2 edge=min(uv,1-uv);float aa=max(fwidth(uv.x),fwidth(uv.y));float pane=smoothstep(.018,.018+aa,min(edge.x,edge.y));
    float2 parallax=float2(side?view.z:view.x,view.y)*.16/max(.4,abs(dot(n,view)));
    float2 room=uv+parallax;float wall=step(.1,room.x)*step(room.x,.9)*step(.12,room.y)*step(room.y,.87);
    float blinds=smoothstep(.35,.55,frac(room.y*14))*step(.78,random);
    float desk=step(.12,room.x)*step(room.x,.7)*step(.2,room.y)*step(room.y,.25);
    float lamp=step(.4,room.x)*step(room.x,.65)*step(.8,room.y)*step(room.y,.83);
    half3 tint=lerp(_BaseColor.rgb*.55,_BaseColor.rgb*1.15,random);
    SurfaceData s=(SurfaceData)0;s.albedo=lerp(half3(.045,.052,.056),tint*(.72+wall*.28)-blinds*.018,pane);s.metallic=pane*.42;s.smoothness=lerp(.3,_Smoothness,pane);s.occlusion=1;s.alpha=1;
    float occupied=step(.42,random);float lightColor=hash(cell+17.6);half3 warm=lerp(half3(.4,.69,.9),half3(1,.68,.32),step(.32,lightColor));
    s.emission=warm*(.05+_CityNightGlow*.95)*occupied*pane*(.12+wall*.5+lamp*.6)*(1-blinds*.55)*(1-desk*.8);
    InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=n;data.viewDirectionWS=view;data.bakedGI=SampleSH(n);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=1;
    half4 color=UniversalFragmentPBR(data,s);return half4(MixFog(color.rgb,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}

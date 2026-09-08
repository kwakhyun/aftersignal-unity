Shader "AfterSignal/Urban Surface"
{
 Properties
 {
  _BaseMap("Albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
  _NormalMap("OpenGL normal",2D)="bump"{} _MaskMap("AO / Roughness / Metal",2D)="white"{}
  _Density("Repeats per metre",Float)=0.25 _UsePbrMaps("Scanned maps",Float)=0
  _NormalStrength("Normal strength",Range(0,2))=0.55 _Metallic("Metallic",Range(0,1))=0
  _Smoothness("Smoothness",Range(0,1))=0.25 _RainResponse("Rain response",Range(0,1))=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
   TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Density,_UsePbrMaps,_NormalStrength,_Metallic,_Smoothness,_RainResponse;
   CBUFFER_END
   float _CityWetness;
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);
    half3 n=normalize(i.normal),a=abs(n);float2 uv;half3 tangent,bitangent;
    if(a.y>.6){uv=i.world.xz;tangent=half3(1,0,0);bitangent=half3(0,0,1);}
    else if(a.x>.6){uv=i.world.zy;tangent=half3(0,0,1);bitangent=half3(0,1,0);}
    else{uv=i.world.xy;tangent=half3(1,0,0);bitangent=half3(0,1,0);}
    uv*=_Density;
    half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb*_BaseColor.rgb;
    half3 mask=half3(1,1-_Smoothness,_Metallic);
    if(_UsePbrMaps>.5)
    {
     mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).rgb;
     half3 bump=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uv));
     n=normalize(n*bump.z+(tangent*bump.x+bitangent*bump.y)*_NormalStrength);
    }
    half patches=smoothstep(.2,.78,sin(i.world.x*.17+sin(i.world.z*.23))*cos(i.world.z*.13)*.5+.5);
    half wet=saturate(_CityWetness*_RainResponse)*saturate(i.normal.y)*patches;
    SurfaceData surface=(SurfaceData)0;surface.albedo=albedo*lerp(1,.58,wet);surface.metallic=saturate(mask.b+_Metallic);surface.specular=half3(.04,.04,.04);surface.smoothness=lerp(clamp(1-mask.g,.05,.88),.94,wet);surface.occlusion=lerp(1,mask.r,.65);surface.alpha=1;
    InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.bakedGI=SampleSH(n);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
    half4 color=UniversalFragmentPBR(data,surface);return half4(MixFog(color.rgb,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}

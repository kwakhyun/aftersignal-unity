Shader "AfterSignal/Urban Surface"
{
 Properties { _BaseMap("Surface",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Density("Repeats per metre",Float)=0.2 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Density;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;};
   V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(V i):SV_Target
   {
    half3 n=normalize(i.normal);half3 a=abs(n);float2 uv=a.y>.6?i.world.xz:a.x>.6?i.world.zy:i.world.xy;
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*_Density).rgb*_BaseColor.rgb;
    Light key=GetMainLight(TransformWorldToShadowCoord(i.world));half diffuse=saturate(dot(n,key.direction));
    half3 lighting=half3(.30,.34,.40)+key.color*diffuse*(.32+.35*key.shadowAttenuation);
    return half4(MixFog(tex*lighting,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}

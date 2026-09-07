Shader "AfterSignal/Building Fade"
{
 Properties { _BaseMap("Surface",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _EmissionMap("Emission",2D)="white"{} _EmissionColor("Glow",Color)=(0,0,0,1) _Opacity("Opacity",Float)=1 _Triplanar("World UV",Float)=0 _Density("Density",Float)=0.2 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor,_EmissionColor;float _Opacity,_Triplanar,_Density;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
   V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(V i):SV_Target {half3 n=normalize(i.normal),a=abs(n);float2 uv=_Triplanar>.5?(a.y>.6?i.world.xz:a.x>.6?i.world.zy:i.world.xy)*_Density:i.uv;half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb*_BaseColor.rgb;Light key=GetMainLight();half3 lit=SampleSH(n)+key.color*saturate(dot(n,key.direction))*.65;half3 glow=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;return half4(MixFog(tex*max(lit,.22)+glow,i.fog),_Opacity);}
   ENDHLSL
  }
 }
}

Shader "AfterSignal/Signal Effect" {
 Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _SpriteMode("Sprite renderer",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial) half4 _BaseColor; half _SpriteMode; CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 V vert(A i){V o;i.p.xy*=lerp(float2(1,1),unity_SpriteProps.xy,_SpriteMode);o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color*_BaseColor*lerp(half4(1,1,1,1),unity_SpriteColor,_SpriteMode);return o;}
 half4 frag(V i):SV_Target {return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;}
 ENDHLSL }
 } }

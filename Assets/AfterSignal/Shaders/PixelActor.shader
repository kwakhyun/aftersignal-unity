Shader "AfterSignal/Pixel Actor URP"
{
    Properties {
        [PerRendererData] _MainTex("Sprite", 2D)="white"{}
        _Color("Tint", Color)=(1,1,1,1)
        _Cutoff("Cutout", Range(0,1))=.12
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite On
        Pass {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half4 _Color; half _Cutoff;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float3 world:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes i) {Varyings o;i.positionOS.xy*=unity_SpriteProps.xy;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.color=i.color*_Color*unity_SpriteColor;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 Frag(Varyings i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;clip(c.a-_Cutoff);Light light=GetMainLight(TransformWorldToShadowCoord(i.world));half3 ambient=SampleSH(half3(0,.55,-.84));c.rgb*=clamp(half3(.66,.68,.71)+ambient*.6+light.color*.18*light.shadowAttenuation,.65,1.08);c.rgb=MixFog(c.rgb,i.fog);return c;}
            ENDHLSL
        }
    }
}

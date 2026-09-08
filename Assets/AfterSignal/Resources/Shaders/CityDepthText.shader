Shader "AfterSignal/City Depth Text"
{
    Properties { _MainTex("Font atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _Color;
            struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float fog:TEXCOORD1;};
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color*_Color;o.fog=ComputeFogFactor(o.position.z);return o;}
            half4 frag(V i):SV_Target {half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*i.color.a;clip(alpha-.01);return half4(MixFog(i.color.rgb,i.fog),alpha);}
            ENDHLSL
        }
    }
}

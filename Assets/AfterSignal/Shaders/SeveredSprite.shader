Shader "AfterSignal/SeveredSprite"
{
 Properties{[PerRendererData]_MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Atlas("Atlas",Vector)=(0,0,1,1) _Cut("Cut",Vector)=(0,0,0,0)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _Color,_Atlas,_Cut;
 struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv;o.color=i.color*_Color;return o;}
 half4 frag(V i):SV_Target{float2 p=(i.uv-_Atlas.xy)/_Atlas.zw;if(p.x>_Cut.x&&p.y>_Cut.y&&p.x<_Cut.z&&p.y<_Cut.w)discard;return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;}
 ENDHLSL}
 }
}

Shader "UI/OutlineGlowSimple_URP"
{
Properties
{
_MainTex ("Mask (A=Shape)", 2D) = "white" {}
_Color ("Tint", Color) = (1,1,1,1)
[HDR] _OutlineColor ("Outline Color", Color) = (1,0.84,0.4,0.65)
_OutlineWidth ("Outline Width (px)", Range(0, 12)) = 6
_Softness ("Softness (px)", Range(0, 8)) = 3
}
SubShader
{
Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
Cull Off
ZWrite Off
Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
            float4 color      : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv          : TEXCOORD0;
            float4 color       : COLOR;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        float4 _Color;
        float4 _OutlineColor;
        float  _OutlineWidth;
        float  _Softness;

        Varyings vert (Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.color = v.color * _Color;
            return o;
        }

        float sampleAlpha(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
        }

        float edgeStrength(float2 uv, float2 texel, float px)
        {
            float2 d = texel * px;
            float aC = sampleAlpha(uv);
            float a1 = sampleAlpha(uv + float2( d.x,  0));
            float a2 = sampleAlpha(uv + float2(-d.x,  0));
            float a3 = sampleAlpha(uv + float2( 0,  d.y));
            float a4 = sampleAlpha(uv + float2( 0, -d.y));
            float a5 = sampleAlpha(uv + float2( d.x,  d.y));
            float a6 = sampleAlpha(uv + float2(-d.x,  d.y));
            float a7 = sampleAlpha(uv + float2( d.x, -d.y));
            float a8 = sampleAlpha(uv + float2(-d.x, -d.y));

            float nMax = max(max(max(a1,a2),max(a3,a4)), max(max(a5,a6),max(a7,a8)));
            return saturate(nMax - aC);
        }

        float4 frag (Varyings i) : SV_Target
        {
            float2 texel = _MainTex_TexelSize.xy;
            float w = max(0.0, _OutlineWidth);
            float s = max(0.0, _Softness);

            float edgeCore  = edgeStrength(i.uv, texel, w * 0.5);
            float edgeOuter = edgeStrength(i.uv, texel, w);
            float edge = saturate(edgeCore * 0.7 + edgeOuter * 0.3);

            if (s > 0.0)
            {
                float edgeSoft = edgeStrength(i.uv, texel, w + s);
                edge = saturate(edge - 0.5 * max(0, edgeSoft - edge));
            }

            float4 col = _OutlineColor;
            col.a *= edge;
            return col;
        }
        ENDHLSL
    }
}
FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
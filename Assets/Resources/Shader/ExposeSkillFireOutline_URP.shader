Shader "UI/ExposeSkillFireOutline_URP"
{
Properties
{
    _MainTex ("Mask (A=Shape)", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)
    [HDR] _OutlineColor ("Outline Color", Color) = (2.5, 1.1, 0.15, 1)
    [HDR] _FlowHighlightColor ("Flow Highlight", Color) = (4, 2.2, 0.4, 1)
    [HDR] _InnerGlowColor ("Inner Glow", Color) = (1.8, 0.55, 0.1, 0.35)
    _OutlineWidth ("Outline Width (px)", Range(0, 16)) = 8
    _Softness ("Softness (px)", Range(0, 10)) = 4
    _InnerGlowStrength ("Inner Glow Strength", Range(0, 1)) = 0.35
    _NoiseScale ("Noise Scale", Range(4, 120)) = 36
    _NoiseStrength ("Noise Edge Wobble", Range(0, 1)) = 0.55
    _NoiseSpeed ("Noise Speed", Range(0, 8)) = 2.2
    _FlowSpeed ("Flow Speed", Range(0, 12)) = 4.5
    _FlowSwirl ("Flow Bands", Range(1, 12)) = 5
    _FlowIntensity ("Flow Intensity", Range(0, 2)) = 1.1
    _FlowSharpness ("Flow Sharpness", Range(1, 8)) = 3.5
    _SparkleThreshold ("Sparkle Threshold", Range(0, 1)) = 0.72
    _HoldProgress ("Hold Progress", Range(0, 1)) = 0
    _FlowOffset ("Flow Offset", Float) = 0
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
        float4 _FlowHighlightColor;
        float4 _InnerGlowColor;
        float  _OutlineWidth;
        float  _Softness;
        float  _InnerGlowStrength;
        float  _NoiseScale;
        float  _NoiseStrength;
        float  _NoiseSpeed;
        float  _FlowSpeed;
        float  _FlowSwirl;
        float  _FlowIntensity;
        float  _FlowSharpness;
        float  _SparkleThreshold;
        float  _HoldProgress;
        float  _FlowOffset;

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

        float hash21(float2 p)
        {
            return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
        }

        float valueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);

            float a = hash21(i);
            float b = hash21(i + float2(1.0, 0.0));
            float c = hash21(i + float2(0.0, 1.0));
            float d = hash21(i + float2(1.0, 1.0));

            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float fbmNoise(float2 p)
        {
            float v = 0.0;
            float a = 0.5;
            float2 shift = float2(100.0, 100.0);
            for (int k = 0; k < 3; k++)
            {
                v += a * valueNoise(p);
                p = p * 2.02 + shift;
                a *= 0.5;
            }
            return v;
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

            float nMax = max(max(max(a1, a2), max(a3, a4)), max(max(a5, a6), max(a7, a8)));
            return saturate(nMax - aC);
        }

        float4 frag (Varyings i) : SV_Target
        {
            float2 texel = _MainTex_TexelSize.xy;
            float w = max(0.0, _OutlineWidth);
            float s = max(0.0, _Softness);
            float holdBoost = lerp(0.85, 1.35, saturate(_HoldProgress));

            float time = _Time.y;
            float2 noiseUv = i.uv * _NoiseScale + float2(time * _NoiseSpeed, -time * _NoiseSpeed * 0.7);
            float edgeNoise = fbmNoise(noiseUv);
            float widthJitter = 1.0 + (edgeNoise - 0.5) * 2.0 * _NoiseStrength;
            float effectiveW = w * widthJitter * holdBoost;

            float edgeCore  = edgeStrength(i.uv, texel, effectiveW * 0.45);
            float edgeOuter = edgeStrength(i.uv, texel, effectiveW);
            float edge = saturate(edgeCore * 0.55 + edgeOuter * 0.45);

            if (s > 0.0)
            {
                float edgeSoft = edgeStrength(i.uv, texel, effectiveW + s);
                edge = saturate(edge + 0.35 * max(0.0, edgeSoft - edge));
            }

            float2 centered = i.uv - 0.5;
            float polar = atan2(centered.y, centered.x);
            float flowPhase = polar * _FlowSwirl + time * _FlowSpeed + _FlowOffset;
            float flowWave = pow(saturate(sin(flowPhase) * 0.5 + 0.5), _FlowSharpness);

            float sparkleNoise = fbmNoise(i.uv * (_NoiseScale * 1.8) + float2(-time * 3.1, time * 2.4));
            float sparkle = step(_SparkleThreshold, sparkleNoise) * edgeOuter;

            float shapeAlpha = sampleAlpha(i.uv);
            float innerBand = edgeStrength(i.uv, texel, effectiveW * 0.25);
            float innerGlow = shapeAlpha * innerBand * _InnerGlowStrength * holdBoost;

            float3 rgb = _OutlineColor.rgb;
            rgb = lerp(rgb, _FlowHighlightColor.rgb, flowWave * _FlowIntensity * edge);
            rgb += _FlowHighlightColor.rgb * sparkle * 0.85;
            rgb = lerp(rgb, _InnerGlowColor.rgb, innerGlow);

            float alpha = saturate(edge * _OutlineColor.a + innerGlow * _InnerGlowColor.a + sparkle * 0.6);
            alpha *= i.color.a;

            return float4(rgb * i.color.rgb, alpha);
        }
        ENDHLSL
    }
}
FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

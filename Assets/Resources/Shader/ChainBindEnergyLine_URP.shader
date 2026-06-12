Shader "SceneEffect/ChainBindEnergyLine_URP"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (0.65, 0.03, 0.06, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (2.2, 0.35, 0.28, 1)
        _CoreWidth ("Core Width", Range(0.01, 1)) = 0.45
        _GlowSoftness ("Glow Softness", Range(0.01, 1)) = 0.85
        _CoreAlpha ("Core Alpha", Range(0, 1)) = 1
        _GlowAlpha ("Glow Alpha", Range(0, 1)) = 0.55
        _FlowScale ("Flow Scale", Range(0, 16)) = 4
        _FlowSpeed ("Flow Speed", Range(-12, 12)) = 3.5
        _FlowIntensity ("Flow Intensity", Range(0, 2)) = 0.65
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 4
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.18
        _Intensity ("Intensity", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+10"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

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

            float4 _CoreColor;
            float4 _GlowColor;
            float _CoreWidth;
            float _GlowSoftness;
            float _CoreAlpha;
            float _GlowAlpha;
            float _FlowScale;
            float _FlowSpeed;
            float _FlowIntensity;
            float _PulseSpeed;
            float _PulseStrength;
            float _Intensity;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float crossDist = abs(i.uv.y - 0.5) * 2.0;
                float core = 1.0 - smoothstep(_CoreWidth, min(_CoreWidth + 0.12, 1.0), crossDist);
                float glow = 1.0 - smoothstep(0.0, _GlowSoftness, crossDist);

                float t = _Time.y;
                float flow = sin((i.uv.x * _FlowScale - t * _FlowSpeed) * 6.2831853) * 0.5 + 0.5;
                flow = smoothstep(0.35, 1.0, flow);
                float pulse = 1.0 + sin(t * _PulseSpeed) * _PulseStrength;

                float3 rgb = _GlowColor.rgb * glow * _GlowAlpha;
                rgb += _CoreColor.rgb * core * _CoreAlpha;
                rgb += _GlowColor.rgb * flow * glow * _FlowIntensity * 0.35;
                rgb *= _Intensity * pulse;

                float alpha = saturate(glow * _GlowAlpha + core * _CoreAlpha);
                alpha *= i.color.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}

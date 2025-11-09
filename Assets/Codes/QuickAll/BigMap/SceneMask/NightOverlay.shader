Shader "Custom/NightOverlay"
{
    Properties
    {
        _LightMask ("Light Mask (RT)", 2D) = "white" {}
        _NightColor ("Night Color", Color) = (0,0,0,1)
        _Intensity ("Night Intensity", Range(0,1)) = 1
        _Threshold ("Mask Threshold", Range(0,1)) = 0.0
        _Smoothness ("Mask Smoothness", Range(0,1)) = 0.1
        _Gamma ("Mask Gamma", Range(0.2,3)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalRenderPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_LightMask);
            SAMPLER(sampler_LightMask);

            float4 _NightColor;
            float _Intensity;
            float _Threshold;
            float _Smoothness;
            float _Gamma;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float luminance(float3 c) {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float smoothMask(float m, float th, float sm) {
                // 可选：对亮度应用阈值与平滑，得到更自然边缘
                return smoothstep(th, th + max(sm, 1e-5), m);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 maskRGB = SAMPLE_TEXTURE2D(_LightMask, sampler_LightMask, i.uv).rgb;
                // 可选伽马校正
                maskRGB = pow(max(maskRGB, 0), 1.0 / max(_Gamma, 1e-5));

                float m = luminance(maskRGB);
                // 平滑阈值处理
                float mSmooth = smoothMask(m, _Threshold, _Smoothness);

                // 夜幕透明度：亮处透明，暗处不透明
                float alpha = saturate(_Intensity * (1.0 - mSmooth));

                float4 col = _NightColor;
                col.a = alpha;

                // 支持顶点色影响强度（用于 UI 或 Sprite 的逐片段调控）
                col.a *= i.color.a;

                return col;
            }
            ENDHLSL
        }
    }
}
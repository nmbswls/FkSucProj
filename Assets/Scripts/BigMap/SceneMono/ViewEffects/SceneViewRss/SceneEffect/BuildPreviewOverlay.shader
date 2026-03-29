// 文件名：PreviewOverlay.shader
// 适用于：URP（也可用于内置管线，见下方内置版）
// 用途：统一的"建筑预览"材质，支持合法/非法状态、透明、脉动、条纹、可选描边（简化版）
// 使用：创建材质，指定本 Shader；运行时用 MaterialPropertyBlock 设置 _PreviewState/_Alpha 等。
// 建议：Render Queue 透明，关闭投射阴影。

Shader "Custom/BuildPreviewOverlay"
{
    Properties
    {
        _MainTex        ("Sprite Texture", 2D) = "white" {}
        _Color          ("Tint", Color) = (1,1,1,1)
        _Alpha          ("Global Alpha", Range(0,1)) = 1

        _ValidColor     ("Valid (Green)", Color)   = (0.4, 1.0, 0.4, 1)
        _InvalidColor   ("Invalid (Red)", Color)   = (1.0, 0.3, 0.3, 1)
        _PreviewColor   ("Preview (Yellow)", Color)= (1.0, 0.95, 0.4, 1)
        _TintStrength   ("Tint Strength", Range(0,1)) = 0.35
        _State          ("State (0=Valid,1=Invalid,2=Preview)", Float) = 0
    }

    SubShader
    {
        // 关键：这两个标签让 2D Renderer 识别为 Sprite 材质
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "UniversalMaterialType"="Sprite"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Sprite2D"
            // 关键：与 2D Renderer 期望一致的设置
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 2D Renderer 支持的宏（与官方 Sprite-Unlit 一致）
            #pragma multi_compile_local _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile _ PIXELSNAP_ON
            // SRP Batcher 友好
            #pragma target 2.0

            // 2D Renderer 用的公共库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 与 Sprite-Unlit 相同的纹理声明方式
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Alpha;
                float4 _ValidColor;
                float4 _InvalidColor;
                float4 _PreviewColor;
                float  _TintStrength;
                float  _State;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float3 SelectStateColor(int s, float3 valid, float3 invalid, float3 preview)
            {
                if (s == 1) return invalid;
                if (s == 2) return preview;
                return valid;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float a = tex.a * saturate(_Alpha);

                int s = (int)round(_State);
                float tintK = saturate(_TintStrength);

                // 若纹理完全透明或强度为0，直接原样输出
                if (tintK <= 0.0001 || tex.a <= 0.0001)
                {
                    return float4(tex.rgb, a);
                }

                // 目标亮色（来自材质颜色）
                float3 validTarget   = _ValidColor.rgb;   // 建议设为亮绿，如 (0.2, 1.0, 0.2)
                float3 invalidTarget = _InvalidColor.rgb; // 亮红，如 (1.0, 0.0, 0.0)
                float3 previewTarget = _PreviewColor.rgb; // 亮黄，如 (1.0, 0.95, 0.4)

                // 为覆盖插值设一个最低强度，避免太弱
                float k = max(tintK, 0.6);

                float3 rgb = tex.rgb;

                if (s == 1) // Invalid: 覆盖到红色
                {
                    rgb = lerp(tex.rgb, invalidTarget, k);
                    // 如需更实心可提升 alpha 下限：
                    // a = max(a, 0.85);
                }
                else if (s == 0) // Valid: 覆盖到亮绿
                {
                    rgb = lerp(tex.rgb, validTarget, k);
                }
                else // s == 2, Preview: 覆盖到亮黄（也可保留乘色）
                {
                    rgb = lerp(tex.rgb, previewTarget, k);
                }

                return float4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
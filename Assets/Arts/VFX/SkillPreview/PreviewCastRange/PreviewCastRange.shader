// 施法距离环预览：UV 空间 procedural ring，不依赖贴图缩放清晰度
Shader "Custom/PreviewCastRange"
{
    Properties
    {
        _Color ("Color", Color) = (0.27, 1, 0, 0.85)
        _RingWidth ("Ring Width (UV)", Range(0.005, 0.15)) = 0.028
        _Softness ("Edge Softness (UV)", Range(0.001, 0.05)) = 0.012
    }

    SubShader
    {
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
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ ETC1_EXTERNAL_ALPHA
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _RingWidth;
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dist = length(input.uv - 0.5);
                float outer = 0.5;
                float inner = outer - _RingWidth;
                float soft = max(_Softness, 1e-4);
                float ring = smoothstep(inner - soft, inner, dist)
                    * (1.0 - smoothstep(outer - soft, outer, dist));
                return half4(_Color.rgb, _Color.a * ring);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

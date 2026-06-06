// ExtendSprite 功能子集 + 高草局部裁剪（物体空间 Y）
Shader "Custom/ExtendSpriteCover"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Fade ("Fade", Range(0, 1)) = 0
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 0)
        _BrightBoost ("Bright Boost", Range(0, 1)) = 0.3
        _CoverStrength ("Cover Strength", Range(0, 1)) = 0
        _CoverClipLocalY ("Cover Clip Local Y", Float) = 0
        _CoverLocalMinY ("Cover Local Min Y", Float) = -0.5
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
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Fade;
                float _FlashAmount;
                float4 _FlashColor;
                float _BrightBoost;
                float _CoverStrength;
                float _CoverClipLocalY;
                float _CoverLocalMinY;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionOS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.positionOS = input.positionOS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_CoverStrength > 0.001)
                {
                    float clipY = lerp(_CoverLocalMinY, _CoverClipLocalY, _CoverStrength);
                    if (input.positionOS.y < clipY)
                    {
                        discard;
                    }
                }

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= input.color;

                col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount);
                col.rgb += _BrightBoost * _FlashAmount;

                col.a *= (1.0 - _Fade);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}

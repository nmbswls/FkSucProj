Shader "Custom/FOV2D_Fade_URP"
{
Properties
{
_Tint("Tint", Color) = (1,1,1,1)
}
SubShader
{
Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalRenderPipeline" }
Pass
{
Tags { "LightMode"="UniversalForward" }
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Off

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color      : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float4 color        : COLOR;
        };

        Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = _Tint;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return i.color; // Êä³ö²ÄÖÊÑÕÉ«
            }
        ENDHLSL
    }
}
FallBack Off
}
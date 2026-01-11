Shader "Custom/FOV2D_Fade_URP"
{
Properties
{
    [HDR]_BaseColor("Base Color", Color) = (1,1,1,1)
    _GlobalAlpha("Global Alpha", Range(0,1)) = 1

    // 径向远端虚化：在 [RadialStart, RadialEnd] 区间由0->1
    _RadialStart("Radial Fade Start (0..1)", Range(0,1)) = 0.75
    _RadialEnd("Radial Fade End (0..1)", Range(0,1)) = 1.0

    // 角向两侧虚化宽度（0..0.5）
    _EdgeFadeWidth("Lateral Edge Fade Width (0..0.5)", Range(0,0.5)) = 0.08

    // 额外参数（可选）
    _RadialPow("Radial Power", Range(0.1, 4)) = 1.0
    _LateralPow("Lateral Power", Range(0.1, 4)) = 1.0


    [HDR] _GlowColor("Glow Color", Color) = (1,0.9,0.6,1)
    _GlowIntensity("Glow Intensity", Range(0,5)) = 2
    _EdgeWidth("Edge Width (radial 0…1)", Range(0.001, 0.3)) = 0.08
    _Feather("Feather Pow", Range(0.5, 4)) = 1.2
}
SubShader
{
Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalRenderPipeline" }
Pass
{
        Tags { "LightMode"="SRPDefaultUnlit" }
        
        
        // Blend SrcAlpha OneMinusSrcAlpha
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off



        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        // URP 通用包含
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _GlobalAlpha;
            float _UseMainTex;

            float _RadialStart;
            float _RadialEnd;
            float _EdgeFadeWidth;

            float _RadialPow;
            float _LateralPow;

            float4 _GlowColor;
            float _GlowIntensity;
            float _EdgeWidth;
            float _Feather;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0; // uv.x=t, uv.y=dist/radius
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv          : TEXCOORD0;
            float4 col  : COLOR;
        };

        Varyings vert (Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            //o.col = v.color;

            return o;
        }

        // 安全除法
        inline float safeDiv(float a, float b, float eps)
        {
            return a / max(b, eps);
        }

        float4 frag (Varyings i) : SV_Target
        {
             // 径向位置 0..1：越接近1越靠外缘
            float rad = saturate(i.uv.y);

            // 边缘因子：只在外缘 EdgeWidth 内渐变
            float edge = smoothstep(1.0 - _EdgeWidth, 1.0, rad);
            edge = pow(edge, _Feather);

            float3 base = _BaseColor.rgb;
            float3 emission = _GlowColor.rgb * _GlowIntensity * edge;

            // alpha 用 edge 控制，让外缘更明显；也可混入顶点色a
            float a = saturate(_BaseColor.a * edge);

            return half4(base + emission, a);
        }
        ENDHLSL
    }
}
FallBack Off
}
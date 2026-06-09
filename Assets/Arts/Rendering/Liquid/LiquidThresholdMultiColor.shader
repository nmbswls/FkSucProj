Shader "Custom/LiquidThresholdMultiColor"
{
    Properties
    {
        _LiquidTex ("Render Texture (Liquid)", 2D) = "black" {}
        _NoiseTex ("Detail Noise", 2D) = "gray" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 1.0, 1)
        _PoisonColor ("Poison Color", Color) = (0.2, 0.9, 0.2, 1)
        _OilColor ("Oil Color", Color) = (0.1, 0.1, 0.1, 1)
        _Threshold ("Liquid Threshold", Range(0, 1)) = 0.5
        _SmoothRange ("Liquid Edge Softness", Range(0.001, 0.2)) = 0.05
        _NoiseTiling ("Noise Tiling", Float) = 1.5
        _FlowSpeed ("Flow Speed", Vector) = (0.1, 0.06, 0, 0)
        _DetailStrength ("Ripple Density", Range(0, 5)) = 2.5
        _ShimmerStrength ("Ripple Threshold", Range(0, 1)) = 0.42
        _EdgeFoamStrength ("Edge Wobble", Range(0, 1)) = 0.5
        _OverallAlpha ("Overall Alpha", Range(0, 1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _LiquidTex;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float4 _WaterColor;
            float4 _PoisonColor;
            float4 _OilColor;
            float _Threshold;
            float _SmoothRange;
            float _NoiseTiling;
            float4 _FlowSpeed;
            float _DetailStrength;
            float _ShimmerStrength;
            float _EdgeFoamStrength;
            float _OverallAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldXY = worldPos.xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float ChannelMask(float channel, float wobble)
            {
                float range = max(_SmoothRange, 0.001);
                return smoothstep(
                    _Threshold - range + wobble,
                    _Threshold + range + wobble,
                    channel);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 rtCol = tex2D(_LiquidTex, i.uv);
                float time = _Time.y;

                // 边缘轮廓噪声抖动
                float2 edgeScale = max(_NoiseTiling, 0.1) * _NoiseTex_ST.xy * 0.65;
                float2 edgeUV = frac(i.worldXY * edgeScale + float2(time * 0.04, time * 0.03));
                float edgeNoise = tex2D(_NoiseTex, edgeUV).r;
                float wobble = (edgeNoise - 0.5) * _EdgeFoamStrength * 0.3;

                float waterMask = ChannelMask(rtCol.r, wobble);
                float poisonMask = ChannelMask(rtCol.g, wobble);
                float oilMask = ChannelMask(rtCol.b, wobble);
                float liquidMask = saturate(waterMask + poisonMask + oilMask);
                if (liquidMask <= 0.001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float sumMask = max(waterMask + poisonMask + oilMask, 0.001);
                fixed3 baseRGB = (_WaterColor.rgb * waterMask +
                                  _PoisonColor.rgb * poisonMask +
                                  _OilColor.rgb * oilMask) / sumMask;

                // 与雾气相同：世界坐标双层流动噪声
                float2 scale = max(_NoiseTiling, 0.1) * _NoiseTex_ST.xy;
                float2 uv1 = frac(i.worldXY * scale + _NoiseTex_ST.zw + _FlowSpeed.xy * time);
                float2 uv2 = frac(i.worldXY * scale * 1.5 + _NoiseTex_ST.zw + float2(-_FlowSpeed.y, _FlowSpeed.x) * time);
                float noise1 = tex2D(_NoiseTex, uv1).r;
                float noise2 = tex2D(_NoiseTex, uv2).r;
                float flowNoise = (noise1 + noise2) * 0.5;

                // 有色区域 × 噪声 → 阈值切透明度（波纹处镂空/变淡）
                float combined = liquidMask * flowNoise * _DetailStrength;
                float rippleSmooth = max(_SmoothRange * 0.6, 0.02);
                float alpha = smoothstep(
                    _ShimmerStrength - rippleSmooth,
                    _ShimmerStrength + rippleSmooth,
                    combined);
                alpha *= liquidMask * _OverallAlpha;

                return fixed4(baseRGB, alpha);
            }
            ENDCG
        }
    }
}

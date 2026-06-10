Shader "Custom/LiquidThresholdMultiColor"
{
    Properties
    {
        _LiquidTex ("Render Texture (Liquid)", 2D) = "black" {}
        _NoiseTex ("Edge Noise", 2D) = "gray" {}
        _RippleTex ("Surface Ripple", 2D) = "gray" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 1.0, 1)
        _PoisonColor ("Poison Color", Color) = (0.2, 0.9, 0.2, 1)
        _OilColor ("Oil Color", Color) = (0.1, 0.1, 0.1, 1)
        _Threshold ("Liquid Threshold", Range(0, 1)) = 0.5
        _SmoothRange ("Liquid Edge Softness", Range(0.001, 0.2)) = 0.05
        _NoiseTiling ("Edge Noise Tiling", Float) = 1.0
        _RippleTiling ("Ripple Tiling", Float) = 1.0
        _RippleHighlight ("Ripple Highlight", Range(0, 1)) = 0.25
        _RippleShimmerSpeed ("Ripple Shimmer Speed", Float) = 0.15
        _EdgeAnimSpeed ("Edge Anim Speed", Float) = 0.0
        _FlowSpeed ("Flow Speed", Vector) = (0, 0, 0, 0)
        _DetailStrength ("Ripple Density (Legacy)", Range(0, 5)) = 2.5
        _ShimmerStrength ("Ripple Threshold (Legacy)", Range(0, 1)) = 0.42
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
            sampler2D _RippleTex;
            float4 _NoiseTex_ST;
            float4 _RippleTex_ST;
            float4 _WaterColor;
            float4 _PoisonColor;
            float4 _OilColor;
            float _Threshold;
            float _SmoothRange;
            float _NoiseTiling;
            float _RippleTiling;
            float _RippleHighlight;
            float _RippleShimmerSpeed;
            float _EdgeAnimSpeed;
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

                // 边缘轮廓：perlin 噪声抖动，速度由 _EdgeAnimSpeed 控制
                float2 edgeScale = max(_NoiseTiling, 0.1) * _NoiseTex_ST.xy * 0.65;
                float edgeAnim = _EdgeAnimSpeed * time;
                float2 edgeUV = frac(i.worldXY * edgeScale + _NoiseTex_ST.zw + float2(edgeAnim, edgeAnim * 0.75));
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

                // 表面波纹：世界坐标平铺，双层慢速相位变化产生轻微 shimmer
                float2 rippleScale = max(_RippleTiling, 0.1) * _RippleTex_ST.xy;
                float shimmerPhase = _RippleShimmerSpeed * time;
                float2 rippleUV1 = frac(i.worldXY * rippleScale + _RippleTex_ST.zw + float2(shimmerPhase, shimmerPhase * 0.6));
                float2 rippleUV2 = frac(i.worldXY * rippleScale * 1.37 + _RippleTex_ST.zw + float2(-shimmerPhase * 0.8, shimmerPhase * 0.5));
                float ripple1 = tex2D(_RippleTex, rippleUV1).r;
                float ripple2 = tex2D(_RippleTex, rippleUV2).r;
                float rippleShimmer = lerp(ripple1, ripple2, 0.5 + 0.5 * sin(time * _RippleShimmerSpeed * 2.0));
                baseRGB += rippleShimmer * _RippleHighlight * liquidMask;

                float alpha = liquidMask * _OverallAlpha;
                return fixed4(saturate(baseRGB), alpha);
            }
            ENDCG
        }
    }
}

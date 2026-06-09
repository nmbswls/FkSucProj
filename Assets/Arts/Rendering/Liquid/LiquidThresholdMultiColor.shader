Shader "Custom/LiquidThresholdMultiColor"
{
    Properties
    {
        _LiquidTex ("Render Texture (Liquid)", 2D) = "black" {}
        _NoiseTex ("Detail Noise", 2D) = "gray" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 1.0, 1)
        _PoisonColor ("Poison Color", Color) = (0.2, 0.9, 0.2, 1)
        _OilColor ("Oil Color", Color) = (0.1, 0.1, 0.1, 1)
        _Threshold ("Merge Threshold", Range(0, 1)) = 0.5
        _SmoothRange ("Edge Smoothness", Range(0.001, 0.2)) = 0.05
        _NoiseTiling ("Noise Tiling", Float) = 2.5
        _FlowSpeed ("Flow Speed", Vector) = (0.04, 0.02, 0, 0)
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.35
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0.2
        _EdgeFoamStrength ("Edge Foam", Range(0, 1)) = 0.25
        _OverallAlpha ("Overall Alpha", Range(0, 1)) = 0.85
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

            float SampleLiquidMask(float2 uv, out float waterAlpha, out float poisonAlpha, out float oilAlpha)
            {
                fixed4 rtCol = tex2D(_LiquidTex, uv);
                float smoothRange = max(_SmoothRange, 0.001);
                waterAlpha = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.r);
                poisonAlpha = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.g);
                oilAlpha = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.b);
                return saturate(waterAlpha + poisonAlpha + oilAlpha);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float waterAlpha;
                float poisonAlpha;
                float oilAlpha;
                float totalAlpha = SampleLiquidMask(i.uv, waterAlpha, poisonAlpha, oilAlpha);
                if (totalAlpha <= 0.001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float sumAlpha = max(waterAlpha + poisonAlpha + oilAlpha, 0.001);
                fixed3 baseRGB = (_WaterColor.rgb * waterAlpha +
                                  _PoisonColor.rgb * poisonAlpha +
                                  _OilColor.rgb * oilAlpha) / sumAlpha;

                float time = _Time.y;
                float2 flowUV = i.worldXY * _NoiseTiling + _FlowSpeed.xy * time;
                float2 shimmerUV = i.worldXY * (_NoiseTiling * 1.7) - _FlowSpeed.xy * time * 0.6;

                float noiseA = tex2D(_NoiseTex, flowUV).r;
                float noiseB = tex2D(_NoiseTex, shimmerUV * float2(1.1, 0.9)).g;
                float detail = lerp(1.0, 0.82 + noiseA * 0.36, _DetailStrength);
                fixed3 finalRGB = baseRGB * detail;
                finalRGB += (noiseB - 0.5) * _ShimmerStrength * totalAlpha;

                float edgeWater;
                float edgePoison;
                float edgeOil;
                float edgeSample = SampleLiquidMask(
                    i.uv + float2(0.0025, 0.0015),
                    edgeWater,
                    edgePoison,
                    edgeOil);
                float edgeFoam = saturate((totalAlpha - edgeSample) * 12.0) * _EdgeFoamStrength;
                finalRGB = lerp(finalRGB, fixed3(1, 1, 1), edgeFoam * 0.5);

                return fixed4(finalRGB, totalAlpha * _OverallAlpha);
            }
            ENDCG
        }
    }
}

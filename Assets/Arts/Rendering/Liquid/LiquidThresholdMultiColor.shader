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
        _DetailStrength ("Ripple Strength", Range(0, 1)) = 0.65
        _ShimmerStrength ("Ripple Brightness", Range(0, 1)) = 0.55
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float SampleTexNoise(float2 uv)
            {
                return tex2D(_NoiseTex, uv).r;
            }

            // 程序化噪声为主（不依赖贴图），贴图仅作细节叠加
            float SampleWhiteRippleMask(float2 worldXY, float time)
            {
                float scale = max(_NoiseTiling, 0.1) * 0.22;
                float2 flow = _FlowSpeed.xy * time;
                float2 p = worldXY * scale + flow;

                float n1 = ValueNoise(p);
                float n2 = ValueNoise(p * 1.63 + float2(1.9, 0.7) + flow * 0.35);
                float n3 = ValueNoise(p * 0.81 - float2(0.6, 2.4) - flow * 0.2);

                float tex = SampleTexNoise(p * 0.55 + float2(3.7, 1.2));
                n1 = lerp(n1, tex, 0.35);

                float ridge = saturate(1.0 - abs(n1 - n2) * 2.8);
                float crest = smoothstep(0.42, 0.88, n1);
                float fine = smoothstep(0.5, 0.92, n3);

                return saturate(ridge * 0.55 + crest * 0.45 + fine * 0.25);
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

                float rippleMask = SampleWhiteRippleMask(i.worldXY, _Time.y);
                fixed3 white = fixed3(1, 1, 1);

                // 粉色底 + 叠加白色波纹（比 lerp 更容易看见）
                float rippleAdd = rippleMask * (_DetailStrength * 0.5 + _ShimmerStrength * 0.4);
                fixed3 finalRGB = saturate(baseRGB + white * rippleAdd);

                float edgeWater;
                float edgePoison;
                float edgeOil;
                float edgeSample = SampleLiquidMask(
                    i.uv + float2(0.0025, 0.0015),
                    edgeWater,
                    edgePoison,
                    edgeOil);
                float edgeFoam = saturate((totalAlpha - edgeSample) * 12.0) * _EdgeFoamStrength;
                finalRGB = lerp(finalRGB, white, edgeFoam * 0.4);

                return fixed4(finalRGB, totalAlpha * _OverallAlpha);
            }
            ENDCG
        }
    }
}

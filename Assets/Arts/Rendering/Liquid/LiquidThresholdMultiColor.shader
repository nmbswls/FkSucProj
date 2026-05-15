Shader "Custom/LiquidThresholdMultiColor"
{
    Properties
    {
        _LiquidTex  ("Render Texture (Liquid)", 2D) = "black" {}
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 1.0, 1)
        _PoisonColor ("Poison Color", Color) = (0.2, 0.9, 0.2, 1)
        _OilColor ("Oil Color", Color) = (0.1, 0.1, 0.1, 1)
        _Threshold ("Merge Threshold", Range(0, 1)) = 0.5
    }
    SubShader
    {
        // 允许透明
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _LiquidTex ;
            float4 _WaterColor, _PoisonColor, _OilColor;
            float _Threshold;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 采样辅助摄像机拍出的 RT
				fixed4 rtCol = tex2D(_LiquidTex, i.uv);
				
				// 设定一个平滑范围（值越大边缘越柔和，0.05是个不错的初始值，你也可以把它做成公开变量 _Smoothness）
				float smoothRange = 0.05; 

				// 2. 分别计算三种液体的 Alpha 掩码 (使用 smoothstep 替代 if，消除锯齿)
				float waterAlpha  = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.r);
				float poisonAlpha = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.g);
				float oilAlpha    = smoothstep(_Threshold - smoothRange, _Threshold + smoothRange, rtCol.b);

				// 3. 计算总透明度 (saturate 保证总和不超过 1)
				float totalAlpha = saturate(waterAlpha + poisonAlpha + oilAlpha);

				// 如果没有任何液体，直接丢弃（优化性能）
				if (totalAlpha <= 0) return fixed4(0,0,0,0);

				// 4. 计算最终颜色 (按比例混合，这样水和毒碰在一起时会平滑渐变，而不是硬切)
				float sumAlpha = waterAlpha + poisonAlpha + oilAlpha;
				fixed3 finalRGB = (_WaterColor.rgb  * waterAlpha + 
								   _PoisonColor.rgb * poisonAlpha + 
								   _OilColor.rgb    * oilAlpha) / sumAlpha;

				// 5. 最终输出 (你可以在 totalAlpha 后面乘上比如 0.8 来让整体半透明)
				return fixed4(finalRGB, totalAlpha);
            }
            ENDCG
        }
    }
}
Shader "Custom/SmokeThresholdMultiColor"
{
    Properties
    {
        _SmokeTex  ("Render Texture (Liquid)", 2D) = "black" {}
		
		_FogColor ("Fog Color", Color) = (0.8, 0.9, 1.0, 1.0)
		_Speed1 ("Layer 1 Speed (X,Y)", Vector) = (0.1, 0.05, 0, 0)
		_Speed2 ("Layer 2 Speed (X,Y)", Vector) = (-0.05, 0.08, 0, 0)
		_Density ("Fog Density", Range(0, 2)) = 1.0
		_Threshold ("Fog Threshold", Range(0, 1)) = 0.3
		_Smoothness ("Edge Smoothness", Range(0.01, 0.5)) = 0.2
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

            sampler2D _SmokeTex;
			float4 _SmokeTex_ST;
			fixed4 _FogColor;
			float2 _Speed1;
			float2 _Speed2;
			float _Density;
			float _Threshold;
			float _Smoothness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 让 UV 随时间流动
				float2 uv1 = i.uv * _SmokeTex_ST.xy + _SmokeTex_ST.zw + _Speed1 * _Time.y;
				float2 uv2 = i.uv * _SmokeTex_ST.xy + _SmokeTex_ST.zw + _Speed2 * _Time.y;

				// 为了让雾气更有层次，第二层噪音可以稍微放大或者旋转一下
				uv2 *= 1.5; 

				// 2. 采样两层流动的噪音图 (只取 R 通道即可，因为是黑白图)
				float noise1 = tex2D(_SmokeTex, uv1).r;
				float noise2 = tex2D(_SmokeTex, uv2).r;

				// 3. 混合两层噪音 (相乘可以让雾气产生很自然的丝缕状空洞)
				// 加上 _Density 控制总体浓度
				float finalNoise = (noise1 * noise2) * _Density;

				// 4. 类似融球的处理：用 smoothstep 切出一个软边缘的形状
				float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, finalNoise);

				// 5. 乘上颜色输出 (如果你希望雾气有渐隐效果，可以让 alpha 乘以一个大范围的遮罩)
				return fixed4(_FogColor.rgb, alpha * _FogColor.a);
            }
            ENDCG
        }
    }
}
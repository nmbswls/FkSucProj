Shader "Custom/SmokeThresholdMultiColor"
{
    Properties
    {
        // 1. 程序的 Render Texture，决定雾气在哪几个格子（黑色代表无雾，白色/红色代表有雾）
        _SmokeTex  ("Render Texture (Mask)", 2D) = "black" {}
        
        // 2. 噪波图，用来制造雾气的流动细节（需要自己找一张黑白云彩图，设为 Repeat）
        _NoiseTex  ("Noise Texture (Detail)", 2D) = "white" {}
		
        _FogColor ("Fog Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _Speed1 ("Layer 1 Speed (X,Y)", Vector) = (0.1, 0.05, 0, 0)
        _Speed2 ("Layer 2 Speed (X,Y)", Vector) = (-0.05, 0.08, 0, 0)
        _Density ("Fog Density", Range(0, 5)) = 2.0
        _Threshold ("Fog Threshold", Range(0, 1)) = 0.2
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
            struct v2f { 
                float4 vertex : SV_POSITION; 
                float2 uv : TEXCOORD0; 
                // 【新增】：用于存储世界坐标
                float2 worldPos : TEXCOORD1; 
            };

            sampler2D _SmokeTex;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            
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
				// 【关键修改】：获取该顶点的真实世界坐标 (XY平面)
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 区域遮罩 (如果你的 RenderTexture 也是跟随世界坐标生成的，这里用 i.uv 或者 i.worldPos 取决于你相机的投影设置。一般用 i.uv 没问题)
                float maskVal = tex2D(_SmokeTex, i.uv).r;

                // 2. 【关键修改】：流动 UV 改为基于 世界坐标 (i.worldPos)
                // 这样相机移动时，世界坐标不变，雾气的纹理就固定在原地了！
                float2 uv1 = i.worldPos * _NoiseTex_ST.xy + _NoiseTex_ST.zw + _Speed1 * _Time.y;
                float2 uv2 = i.worldPos * _NoiseTex_ST.xy + _NoiseTex_ST.zw + _Speed2 * _Time.y;
                uv2 *= 1.5; 

                // 3. 采样流动的噪波图
                float noise1 = tex2D(_NoiseTex, uv1).r;
                float noise2 = tex2D(_NoiseTex, uv2).r;
                float finalNoise = (noise1 + noise2) * 0.5;

                // 4. 将区域遮罩和噪音结合
                float combinedVal = maskVal * finalNoise * _Density;

                // 5. 软阈值切分
                float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, combinedVal);
                alpha *= maskVal;

                return fixed4(_FogColor.rgb, clamp(alpha, 0.0, 1.0) * _FogColor.a);
            }
            ENDCG
        }
    }
}
Shader "Custom/CircleRangeWarn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,0.6,0.6,0.35)
        _FillColor ("Fill Color", Color) = (1,0,0,0.6)
        _Progress  ("Progress", Range(0,1)) = 0
        _Softness  ("Edge Softness (UV space)", Float) = 0.02
        _RadiusUV  ("Radius in UV (0-0.5)", Float) = 0.45 // UV 半径；0.5 ~ 正好到边
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _BaseColor;
            float4 _FillColor;
            float  _Progress;
            float  _Softness;
            float  _RadiusUV;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 以 UV 中心为圆心
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                float prog = saturate(_Progress);
                float filledRadius = _RadiusUV * prog;

                // 已填权重：中心→filledRadius 为 1，越过边缘平滑到 0
                float fillMask = 1.0 - smoothstep(filledRadius, filledRadius + _Softness, dist);

                // 底色与填充色插值：未填=Base，已填=Fill
                fixed4 col = lerp(_BaseColor, _FillColor, saturate(fillMask));

                // 圆外透明（最大半径为 _RadiusUV）
                float inCircle = step(dist, _RadiusUV);
                col.a *= inCircle;

                // 可选：叠加原 Sprite 纹理（若需要显示形状纹理）
                // fixed4 texCol = tex2D(_MainTex, i.uv) * i.color;
                // col.rgb = lerp(col.rgb, texCol.rgb, texCol.a);

                return col;
            }
            ENDCG
        }
    }
}
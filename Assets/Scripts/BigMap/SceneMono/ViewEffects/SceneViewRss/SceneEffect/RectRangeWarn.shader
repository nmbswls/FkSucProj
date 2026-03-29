Shader "Custom/RectRangeWarn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,0.6,0.6,0.35)
        _FillColor ("Fill Color", Color) = (1,0,0,0.6)
        _Progress  ("Progress", Range(0,1)) = 0
        _Direction ("Direction (0:+X,1:-X,2:+Y,3:-Y)", Float) = 0
        _Softness  ("Edge Softness (UV space)", Float) = 0.02
        // 可选：如果你确实需要用Shader强制矩形裁剪范围（而不是靠Sprite边界），可以用UV内的矩形尺寸
        _RectMin   ("Rect Min (UV)", Vector) = (0.0, 0.0, 0, 0)  // 左下角
        _RectMax   ("Rect Max (UV)", Vector) = (1.0, 1.0, 0, 0)  // 右上角
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
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float4 color: COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _BaseColor;
            float4 _FillColor;
            float  _Progress;
            float  _Direction;
            float  _Softness;
            float4 _RectMin; // xy
            float4 _RectMax; // xy

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 基于 UV 的矩形裁剪（通常Sprite本身是矩形，UV在[0,1]）
                float2 uv = i.uv;

                // inRect 让矩形范围外透明（可选，默认Sprite已是矩形，这里提供更精确控制）
                float inRect =
                    step(_RectMin.x, uv.x) * step(_RectMin.y, uv.y) *
                    step(uv.x, _RectMax.x) * step(uv.y, _RectMax.y);

                float prog = saturate(_Progress);
                float fillMask = 0.0;

                if (_Direction < 0.5) {           // +X 左→右
                    float edge = lerp(_RectMin.x, _RectMax.x, prog);
                    fillMask = smoothstep(edge, edge + _Softness, uv.x);
                } else if (_Direction < 1.5) {    // -X 右→左
                    float edge = lerp(_RectMax.x, _RectMin.x, prog);
                    fillMask = 1.0 - smoothstep(edge - _Softness, edge, uv.x);
                } else if (_Direction < 2.5) {    // +Y 下→上
                    float edge = lerp(_RectMin.y, _RectMax.y, prog);
                    fillMask = smoothstep(edge, edge + _Softness, uv.y);
                } else {                           // -Y 上→下
                    float edge = lerp(_RectMax.y, _RectMin.y, prog);
                    fillMask = 1.0 - smoothstep(edge - _Softness, edge, uv.y);
                }

                fixed4 col = lerp(_FillColor, _BaseColor, saturate(fillMask));
                col.a *= inRect;

                return col;
            }
            ENDCG
        }
    }
}
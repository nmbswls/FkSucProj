Shader "Custom/GaussianBlur1D"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Direction ("Direction", Vector) = (1,0,0,0) // (1,0)=横向, (0,1)=纵向
        _Radius ("Radius", Range(1,12)) = 8
        _Sigma ("Sigma", Range(0.5,10)) = 3.0
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }
        ZWrite Off
        Cull Off
        Blend One Zero // 覆盖写入目标

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float2 _Direction;
            float _Radius;
            float _Sigma;

            struct A{ float4 positionOS: POSITION; float2 uv: TEXCOORD0; };
            struct V{ float4 positionHCS: SV_POSITION; float2 uv: TEXCOORD0; };

            V vert(A v){ V o; o.positionHCS=TransformObjectToHClip(v.positionOS); o.uv=v.uv; return o; }

            float gaussian(float x, float s){ return exp(- (x*x) / (2.0*s*s)); }

            float4 frag(V i): SV_Target
            {
                int r = (int)_Radius;
                float2 stepUV = float2(_MainTex_TexelSize.x * _Direction.x,
                                       _MainTex_TexelSize.y * _Direction.y);

                float3 acc = 0;
                float wsum = 0;

                float3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                float w0 = gaussian(0, _Sigma);
                acc += c * w0; wsum += w0;

                [loop]
                for(int k=1;k<=r;k++)
                {
                    float w = gaussian(k, _Sigma);
                    float2 offs = stepUV * k;
                    float3 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + offs).rgb;
                    float3 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - offs).rgb;
                    acc += (c1 + c2) * w;
                    wsum += 2*w;
                }

                return float4(acc / max(wsum,1e-6), 1);
            }
            ENDHLSL
        }
    }
}
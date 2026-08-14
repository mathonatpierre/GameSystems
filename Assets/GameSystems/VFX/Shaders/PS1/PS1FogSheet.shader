Shader "Lennie/PS1 Fog Sheet"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.55,0.52,0.62,1)
        _Density("Density", Range(0,0.4)) = 0.1
        _NoiseScale("Noise Scale", Range(0.1,8)) = 2
        _Speed("Drift Speed", Range(0,0.2)) = 0.025
        _Bands("Alpha Bands", Range(2,16)) = 6
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _NoiseScale;
                float _Speed;
                float _Bands;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.uv = input.uv;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xy * (_NoiseScale * 0.18) + float2(_Time.y * _Speed, 0);
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x), lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
                float edgeFade = smoothstep(0.0, 0.16, input.uv.x) * smoothstep(0.0, 0.16, 1.0 - input.uv.x);
                float vertical = smoothstep(0.0, 0.2, input.uv.y) * smoothstep(0.0, 0.35, 1.0 - input.uv.y);
                float alpha = _Density * lerp(0.45, 1.0, n) * edgeFade * vertical;
                alpha = floor(alpha * _Bands + 0.5) / _Bands;
                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}

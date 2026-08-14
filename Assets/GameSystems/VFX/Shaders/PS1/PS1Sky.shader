Shader "Lennie/PS1 Sky"
{
    Properties
    {
        _SkyMap("Painted Sky", 2D) = "grey" {}
        _PurifiedSkyMap("Purified Sky", 2D) = "grey" {}
        _ZenithColor("Zenith", Color) = (0.18,0.16,0.24,1)
        _HorizonColor("Horizon", Color) = (0.46,0.41,0.5,1)
        _GroundColor("Lower Haze", Color) = (0.22,0.23,0.28,1)
        _Bands("Color Bands", Range(4,32)) = 12
        _CloudStrength("Cloud Strength", Range(0,0.3)) = 0.08
        _PurifiedSkyTint("Purified Sky Tint", Color) = (1,1,1,1)
        _PurifiedBrightness("Purified Brightness", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Front
        ZWrite Off
        ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_SkyMap); SAMPLER(sampler_SkyMap);
            TEXTURE2D(_PurifiedSkyMap); SAMPLER(sampler_PurifiedSkyMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _HorizonColor;
                float4 _GroundColor;
                float _Bands;
                float _CloudStrength;
                float4 _PurifiedSkyTint;
                float _PurifiedBrightness;
            CBUFFER_END
            float _PurificationCameraStrength;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = normalize(input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float2 skyUV = float2(atan2(d.x, d.z) / (2.0 * PI) + 0.5, asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5);
                half3 color = SAMPLE_TEXTURE2D(_SkyMap, sampler_SkyMap, skyUV).rgb;
                half skyChange = saturate((half)_PurificationCameraStrength);
                skyChange = skyChange * skyChange * skyChange * (skyChange * (skyChange * 6.0h - 15.0h) + 10.0h);
                half3 purifiedSky = SAMPLE_TEXTURE2D(_PurifiedSkyMap, sampler_PurifiedSkyMap, skyUV).rgb;
                purifiedSky = purifiedSky * _PurifiedSkyTint.rgb + _PurifiedBrightness;
                color = lerp(color, purifiedSky, skyChange);
                color = floor(color * _Bands + 0.5) / _Bands;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

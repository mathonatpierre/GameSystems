Shader "Lennie/Rainbow Glide Track"
{
    Properties
    {
        _ConcreteMap("Concrete", 2D) = "white" {}
        _RainbowMap("Rainbow Track", 2D) = "white" {}
        _ConcreteColor("Concrete Color", Color) = (.34,.35,.38,1)
        _Reveal("Reveal", Range(0,1)) = 0
        _TrackProgress("Track Progress", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = .38
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_ConcreteMap); SAMPLER(sampler_ConcreteMap);
            TEXTURE2D(_RainbowMap); SAMPLER(sampler_RainbowMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _ConcreteMap_ST;
                float4 _RainbowMap_ST;
                float4 _ConcreteColor;
                float _Reveal;
                float _TrackProgress;
                float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float2 progress : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float progress : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.progress = input.progress.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 concreteTexture = SAMPLE_TEXTURE2D(_ConcreteMap, sampler_ConcreteMap,
                    TRANSFORM_TEX(input.uv, _ConcreteMap)).rgb;
                half3 concrete = lerp(_ConcreteColor.rgb, concreteTexture *
                    _ConcreteColor.rgb, .42h);
                // The generated rainbow texture varies vertically; map that axis along
                // the track and keep its width across U.
                float2 rainbowUV = float2(input.uv.y, frac(input.uv.x * .08));
                half3 rainbow = SAMPLE_TEXTURE2D(_RainbowMap, sampler_RainbowMap, rainbowUV).rgb;
                float revealed = 1.0 - smoothstep(_Reveal, _Reveal + .025, input.progress);
                half3 baseColor = lerp(concrete, rainbow, revealed);
                Light light = GetMainLight();
                half diffuse = saturate(abs(dot(normalize(input.normalWS), light.direction)));
                half lighting = .5h + diffuse * light.shadowAttenuation * .5h;
                half3 litConcrete = concrete * lighting;
                half3 visibleRainbow = rainbow * (1.08h + diffuse * .16h);
                return half4(lerp(litConcrete, visibleRainbow, revealed), 1);
            }
            ENDHLSL
        }
    }
}

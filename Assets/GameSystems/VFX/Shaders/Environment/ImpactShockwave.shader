Shader "GameSystems/VFX/Impact Shockwave"
{
    Properties
    {
        _Color("Impact Color", Color) = (1, .16, .72, 1)
        _Progress("Progress", Range(0,1)) = 0
        _Thickness("Ring Thickness", Range(.005,.2)) = .055
        _Distortion("Warp Strength", Range(0,.08)) = .024
        _Emission("Emission", Range(0,8)) = 3.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Progress;
                float _Thickness;
                float _Distortion;
                float _Emission;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float distanceFromCenter = length(centered);
                float radius = lerp(.06, .92, saturate(_Progress));
                float ring = 1.0 - smoothstep(_Thickness * .3, _Thickness, abs(distanceFromCenter - radius));
                float fade = saturate(1.0 - _Progress);
                clip(ring * fade - .002);
                float2 radial = centered / max(distanceFromCenter, .001);
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half3 warped = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture,
                    sampler_CameraOpaqueTexture, screenUV + radial * ring * _Distortion * fade).rgb;
                half glow = pow(ring, 2.0) * fade;
                half3 color = warped + _Color.rgb * glow * _Emission;
                return half4(color, saturate(ring * .82 * fade));
            }
            ENDHLSL
        }
    }
}

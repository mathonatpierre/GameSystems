Shader "Hidden/Lennie/URP Volumetric Fog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.42,0.42,0.48,1)
        _Density("Density", Range(0,0.15)) = 0.038
        _FogStart("Fog Start", Float) = 5
        _NearFadeDistance("Near Fog Fade", Float) = 8
        _FogEnd("Fog End", Float) = 34
        _BaseHeight("Base Height", Float) = 0
        _HeightFalloff("Height Falloff", Range(0,2)) = 0.22
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.22
        _ColorSteps("Fullscreen Color Steps", Range(16,128)) = 56
        _DitherStrength("Fullscreen Dither", Range(0,1)) = 0.18
        _DitherScale("Dither Pattern Scale", Range(1,8)) = 2
        _Pixelation("Fullscreen Pixel Size", Range(1,6)) = 1
        _ScanlineStrength("Scanline Strength", Range(0,.3)) = .025
        _ScanlineSpacing("Scanline Spacing", Range(1,8)) = 3
        _GlowThreshold("Glow Threshold", Range(.5,2)) = .92
        _GlowIntensity("Glow Intensity", Range(0,2)) = .42
        _GlowRadius("Glow Radius", Range(.5,8)) = 2.2
        _PurifiedFogColor("Purified Fog Color", Color) = (0.62,0.66,0.48,1)
        _PurifiedFogReduction("Purified Fog Reduction", Range(0,1)) = 0.5
        _GodRayStrength("Purified God Rays", Range(0,1)) = .16
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "VolumetricFogAndPS1Finish"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _FogColor;
            float4 _PurifiedFogColor;
            float _Density, _FogStart, _NearFadeDistance, _FogEnd, _BaseHeight, _HeightFalloff, _NoiseStrength;
            float _ColorSteps, _DitherStrength, _DitherScale, _Pixelation, _ScanlineStrength, _ScanlineSpacing, _PurifiedFogReduction, _GodRayStrength;
            float _GlowThreshold, _GlowIntensity, _GlowRadius;
            TEXTURE2D(_PurificationMap); SAMPLER(sampler_PurificationMap);
            float4 _PurificationOriginSize;
            float _PurificationGlobalStrength;

            float Noise(float3 p)
            {
                return frac(sin(dot(floor(p), float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float pixelSize = max(1.0, _Pixelation);
                float2 renderUV = (floor(uv * _ScreenParams.xy / pixelSize) + .5) * pixelSize / _ScreenParams.xy;
                half3 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, renderUV).rgb;
                float2 glowTexel = _BlitTexture_TexelSize.xy * _GlowRadius;
                half3 glowSamples = 0;
                glowSamples += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, renderUV + float2(glowTexel.x, 0)).rgb;
                glowSamples += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, renderUV - float2(glowTexel.x, 0)).rgb;
                glowSamples += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, renderUV + float2(0, glowTexel.y)).rgb;
                glowSamples += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, renderUV - float2(0, glowTexel.y)).rgb;
                glowSamples *= .25h;
                half glowLuminance = max(glowSamples.r, max(glowSamples.g, glowSamples.b));
                half glowMask = saturate((glowLuminance - (half)_GlowThreshold) * 3.0h);
                source += glowSamples * glowMask * (half)_GlowIntensity;
                float depth = SampleSceneDepth(renderUV);
                #if UNITY_REVERSED_Z
                    float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                #else
                    float3 world = ComputeWorldSpacePosition(uv, lerp(UNITY_NEAR_CLIP_VALUE, 1, depth), UNITY_MATRIX_I_VP);
                #endif
                float3 ray = world - _WorldSpaceCameraPos;
                float distanceToSurface = min(length(ray), _FogEnd);
                float3 direction = ray / max(length(ray), 0.001);
                float marchLength = max(0, distanceToSurface - _FogStart);
                float stepLength = marchLength / 10.0;
                float accumulation = 0;
                float purificationAccumulation = 0;
                [unroll] for (int i = 0; i < 10; i++)
                {
                    float3 p = _WorldSpaceCameraPos + direction * (_FogStart + (i + 0.5) * stepLength);
                    float heightDensity = exp(-max(0, p.y - _BaseHeight) * _HeightFalloff);
                    float noise = lerp(1.0, 0.55 + Noise(p * 0.32 + float3(0, _Time.y * 0.025, 0)) * 0.9, _NoiseStrength);
                    accumulation += heightDensity * noise * stepLength;
                    float2 rawPurificationUV = (p.xz - _PurificationOriginSize.xy) * _PurificationOriginSize.zw;
                    float insideMap = step(0.0, rawPurificationUV.x) * step(rawPurificationUV.x, 1.0) * step(0.0, rawPurificationUV.y) * step(rawPurificationUV.y, 1.0);
                    purificationAccumulation += SAMPLE_TEXTURE2D_LOD(_PurificationMap, sampler_PurificationMap, saturate(rawPurificationUV), 0).r * insideMap * stepLength;
                }
                float localPurification = marchLength > .001 ? saturate(purificationAccumulation / marchLength) : 0;
                float atmosphereStrength = localPurification * lerp(.2, 1.0, _PurificationGlobalStrength);
                float fog = 1.0 - exp(-accumulation * _Density);
                // Accumulate from close to the camera, then ease density in over a long
                // distance. This avoids a readable fog plane between gameplay and scenery.
                float nearFade = 1.0 - exp(-marchLength / max(_NearFadeDistance, 0.001));
                fog *= nearFade;
                fog *= lerp(1.0, 1.0 - _PurifiedFogReduction, atmosphereStrength);
                half3 fogColor = lerp(_FogColor.rgb, _PurifiedFogColor.rgb, atmosphereStrength);
                half3 locallyLitSource = source + half3(.18, .145, .045) * atmosphereStrength;
                half3 color = lerp(locallyLitSource, fogColor, saturate(fog));
                float2 rayUV = uv - float2(.12, 1.08);
                float rayAngle = atan2(rayUV.y, rayUV.x);
                half rayBands = pow(saturate(sin(rayAngle * 29.0 + .8) * .5 + .5), 9.0);
                half depthMask = smoothstep(12.0, 38.0, distanceToSurface);
                half horizonMask = smoothstep(.08, .48, uv.y) * (1.0h - smoothstep(.82, 1.0, uv.y));
                half godRays = rayBands * depthMask * horizonMask * atmosphereStrength * _GodRayStrength;
                color += half3(1.0h, .78h, .38h) * godRays;

                // A restrained fullscreen PSone finish: colour precision and subtle ordered dither,
                // without lowering the camera resolution.
                float2 pixel = floor(input.positionCS.xy / max(1.0, _DitherScale));
                float bayer = frac(dot(pixel, float2(0.5, 0.25))) - 0.5;
                color = floor(saturate(color) * _ColorSteps + 0.5 + bayer * _DitherStrength) / _ColorSteps;
                half scanline = (half)frac(floor(input.positionCS.y) / max(1.0, _ScanlineSpacing));
                color *= 1.0h - step(.5h, scanline) * (half)_ScanlineStrength;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

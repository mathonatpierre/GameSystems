Shader "Lennie/PS1 Slime Gel"
{
    Properties
    {
        _BaseMap("Slime Texture", 2D) = "white" {}
        _Tint("Gel Tint", Color) = (1,.2,.72,1)
        _Alpha("Opaque Detail Alpha", Range(.2,1)) = .96
        _CenterAlpha("Open Gel Alpha", Range(.15,.9)) = .48
        _WobbleAmount("Gel Wobble", Range(0,.2)) = .055
        _WobbleSpeed("Gel Speed", Range(.2,12)) = 4.2
        [HideInInspector] _GelPhysics("Procedural Gel Physics", Vector) = (0,0,0,0)
        _RimColor("Rim Glow", Color) = (1,.45,1,1)
        _RimPower("Rim Power", Range(.5,8)) = 2.1
        _Emission("Edge Glow", Range(0,8)) = .12
        _InnerGlow("Transparent Face Glow", Range(0,4)) = .8
        _SpecularStrength("Wet Specular", Range(0,8)) = 0
        _Gloss("Wet Gloss", Range(8,128)) = 32
        _SparkleDensity("Sparkle Density", Range(2,40)) = 15
        _SparkleStrength("Sparkle Strength", Range(0,12)) = 6
        _VertexSnap("Vertex Snap", Range(64,640)) = 260
        _TextureSteps("PS1 UV Steps", Range(24,256)) = 96
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _Tint, _RimColor, _GelPhysics;
                float _Alpha, _CenterAlpha, _WobbleAmount, _WobbleSpeed, _RimPower, _Emission, _InnerGlow, _SpecularStrength, _Gloss, _SparkleDensity, _SparkleStrength, _VertexSnap, _TextureSteps;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            float Hash31(float3 p)
            {
                p = frac(p * .1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float heightWeight = saturate(positionOS.y * 1.35 + .45);
                float squash = _GelPhysics.y;
                positionOS.xz *= 1.0 + squash;
                positionOS.y *= 1.0 - squash * .72;
                positionOS.x += _GelPhysics.x * heightWeight * heightWeight;
                positionOS.z += _GelPhysics.z * heightWeight * .45;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float4 clip = TransformWorldToHClip(positionWS);
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    ndc = floor(ndc * _VertexSnap + .5) / _VertexSnap;
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip;
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 smoothNormalWS = normalize(input.normalWS);
                float3 normalWS = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                if (dot(normalWS, smoothNormalWS) < 0.0) normalWS = -normalWS;
                float3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirection)), _RimPower);
                float2 ps1UV = floor(input.uv * _TextureSteps + .5) / _TextureSteps;
                float3 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, ps1UV).rgb;
                Light mainLight = GetMainLight();
                float diffuse = saturate(dot(normalWS, mainLight.direction)) * .45 + .55;
                float2 sparkleCoordinates = input.uv * _SparkleDensity;
                float2 sparkleCell = floor(sparkleCoordinates);
                float2 sparkleLocal = frac(sparkleCoordinates);
                float sparkleNoise = Hash31(float3(sparkleCell, 7.1));
                float2 sparkleCenter = float2(.22 + Hash31(float3(sparkleCell, 13.7)) * .56,
                                               .22 + Hash31(float3(sparkleCell, 21.3)) * .56);
                float sparkleDistance = length(sparkleLocal - sparkleCenter);
                float sparkleCore = 1.0 - smoothstep(.035, .095, sparkleDistance);
                float sparkleHalo = 1.0 - smoothstep(.07, .19, sparkleDistance);
                float sparklePulse = pow(saturate(sin(_Time.y * 5.2 + sparkleNoise * 19.0) * .5 + .5), 3.0);
                float sparklePresent = step(.78, sparkleNoise);
                float sparkle = (sparkleCore + sparkleHalo * .28) * sparklePresent * (.32 + sparklePulse * .68);

                float2 microCoordinates = input.uv * (_SparkleDensity * 1.73);
                float2 microCell = floor(microCoordinates);
                float microNoise = Hash31(float3(microCell, 31.9));
                float2 microCenter = float2(.3 + Hash31(float3(microCell, 41.2)) * .4,
                                            .3 + Hash31(float3(microCell, 51.8)) * .4);
                float microCore = 1.0 - smoothstep(.022, .055, length(frac(microCoordinates) - microCenter));
                sparkle += microCore * step(.91, microNoise) * (.25 + .75 * sparklePulse);
                sparkle *= _SparkleStrength;
                float normalVariation = length(ddx(smoothNormalWS)) + length(ddy(smoothNormalWS));
                float geometryDetail = saturate(normalVariation * 2.8);
                float luminance = dot(textureColor, float3(.2126, .7152, .0722));
                float modeledFeature = 1.0 - smoothstep(.08, .34, luminance);
                float volumeOpacity = saturate(max(fresnel, max(geometryDetail, modeledFeature)));
                float3 color = textureColor * _Tint.rgb * diffuse;
                color += _RimColor.rgb * fresnel * _Emission;
                color += _Tint.rgb * (1.0 - volumeOpacity) * _InnerGlow;
                color += lerp(float3(1,.55,1), float3(.65,.9,1), sparkleNoise) * sparkle;
                color = floor(color * 32.0 + .5) / 32.0;
                float alpha = lerp(_CenterAlpha, _Alpha, volumeOpacity);
                alpha = saturate(alpha + sparkle * .018);
                alpha = floor(alpha * 6.0 + .5) / 6.0;
                float screenDither = Hash31(float3(fmod(floor(input.positionCS.x), 4.0), fmod(floor(input.positionCS.y), 4.0), 2.0));
                clip(alpha - screenDither * .22);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

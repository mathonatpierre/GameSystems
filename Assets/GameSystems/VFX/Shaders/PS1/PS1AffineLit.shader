Shader "Lennie/PS1 Affine Lit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,1)) = 0.18
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _VertexSnap("Vertex Snap", Range(64,640)) = 240
        _UVSteps("UV Compression (0 = off)", Range(0,256)) = 64
        _ColorSteps("Color Compression", Range(4,64)) = 24
        _Wobble("UV Wobble", Range(0,2)) = 0.45
        _ScreenPixelSize("Texture Pixel Size", Range(1,4)) = 2
        _Ambient("Ambient", Range(0,1)) = 0.2
        _Metallic("PS1 Metallic", Range(0,1)) = 0
        _Smoothness("PS1 Smoothness", Range(0,1)) = .25
        _SpecularSteps("Specular Quantization", Range(2,16)) = 6
        _EdgeWear("Edge Wear", Range(0,1)) = 0
        _EdgeWidth("Edge Width", Range(0.005,0.2)) = 0.055
        _EdgeTint("Edge Tint", Color) = (1.2,1.18,1.16,1)
        _AdditionalLightStrength("Point Light Strength", Range(0,1.5)) = 1
        _PurificationResponse("Purification Response", Range(0,1)) = 0
        _PurifiedVertexRoughness("Purified Vertex Roughness", Range(0,0.25)) = 0
        _PurifiedMap("Purified Grass Surface", 2D) = "white" {}
        [Toggle] _VertexSurfaceBlend("Vertex Surface Blend", Float) = 0
        _SecondaryMap("Secondary Surface", 2D) = "white" {}
        _SecondaryColor("Secondary Color", Color) = (1,1,1,1)
        _SecondaryEmission("Secondary Emission", Range(0,3)) = 0
        _TeleportAmount("Teleport Dissolve", Range(0,1)) = 0
        _TeleportGlowColor("Teleport Glow", Color) = (.76,.28,1,1)
        _RimColor("PS1 Rim Color", Color) = (.48,.32,1,1)
        _RimStrength("PS1 Rim Strength", Range(0,5)) = 0
        _RimPower("PS1 Rim Width", Range(.5,8)) = 2
        _SceneLitRim("Scene Light Influence On Rim", Range(0,1)) = 0
        [HideInInspector] _GelPhysics("Procedural Gel Physics", Vector) = (0,0,0,0)
        _GelPhysicsStrength("Gel Physics Strength", Range(0,2)) = 0
        _GelLeanHeight("Gel Lean Height", Range(.2,4)) = 1.35
        _GelSquashHorizontal("Gel Horizontal Squash", Range(0,2)) = 1
        _GelSquashVertical("Gel Vertical Squash", Range(0,2)) = .72
        _GelTopLag("Gel Top Lag", Range(.5,4)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _LIGHT_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_PurificationMap); SAMPLER(sampler_PurificationMap);
            TEXTURE2D(_PurifiedMap); SAMPLER(sampler_PurifiedMap);
            TEXTURE2D(_SecondaryMap); SAMPLER(sampler_SecondaryMap);
            float4 _PurificationOriginSize;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _VertexSnap;
                float _UVSteps;
                float _ColorSteps;
                float _Wobble;
                float _ScreenPixelSize;
                float _Ambient;
                float _Metallic;
                float _Smoothness;
                float _SpecularSteps;
                float _BumpScale;
                float _EdgeWear;
                float _EdgeWidth;
                float4 _EdgeTint;
                float _AdditionalLightStrength;
                float _PurificationResponse;
                float _PurifiedVertexRoughness;
                float _VertexSurfaceBlend;
                float4 _SecondaryColor;
                float _SecondaryEmission;
                float _TeleportAmount;
                float4 _TeleportGlowColor;
                float4 _RimColor;
                float _RimStrength;
                float _RimPower;
                float _SceneLitRim;
                float4 _GelPhysics;
                float _GelPhysicsStrength;
                float _GelLeanHeight;
                float _GelSquashHorizontal;
                float _GelSquashVertical;
                float _GelTopLag;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                noperspective float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float vertexSurfaceMask : TEXCOORD5;
            };

            float HashRock(float3 p)
            {
                p = frac(p * .1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float RockNoise(float3 p)
            {
                float3 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = HashRock(cell + float3(0,0,0));
                float n100 = HashRock(cell + float3(1,0,0));
                float n010 = HashRock(cell + float3(0,1,0));
                float n110 = HashRock(cell + float3(1,1,0));
                float n001 = HashRock(cell + float3(0,0,1));
                float n101 = HashRock(cell + float3(1,0,1));
                float n011 = HashRock(cell + float3(0,1,1));
                float n111 = HashRock(cell + float3(1,1,1));
                return lerp(lerp(lerp(n000,n100,f.x), lerp(n010,n110,f.x), f.y),
                            lerp(lerp(n001,n101,f.x), lerp(n011,n111,f.x), f.y), f.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float gelHeight = saturate(positionOS.y * _GelLeanHeight + .45);
                float gelSquash = _GelPhysics.y * _GelPhysicsStrength;
                positionOS.xz *= 1.0 + gelSquash * _GelSquashHorizontal;
                positionOS.y *= 1.0 - gelSquash * _GelSquashVertical;
                positionOS.x += _GelPhysics.x * pow(gelHeight, _GelTopLag) * _GelPhysicsStrength;
                positionOS.z += _GelPhysics.z * gelHeight * .45 * _GelPhysicsStrength;
                float3 originalWS = TransformObjectToWorld(positionOS);
                float2 purificationUVVertex = (originalWS.xz - _PurificationOriginSize.xy) * _PurificationOriginSize.zw;
                float vertexInside = step(0.0, purificationUVVertex.x) * step(purificationUVVertex.x, 1.0) * step(0.0, purificationUVVertex.y) * step(purificationUVVertex.y, 1.0);
                float vertexPurification = SAMPLE_TEXTURE2D_LOD(_PurificationMap, sampler_PurificationMap, saturate(purificationUVVertex), 0).r * _PurificationResponse * vertexInside;
                vertexPurification = smoothstep(.18, .92, vertexPurification);
                float3 gridPoint = floor(originalWS * 2.0 + .5);
                float3 roughNoise = frac(sin(float3(
                    dot(gridPoint, float3(12.9898, 78.233, 37.719)),
                    dot(gridPoint, float3(39.346, 11.135, 83.155)),
                    dot(gridPoint, float3(73.156, 52.235, 9.151)))) * 43758.5453) * 2.0 - 1.0;
                float3 displacedWS = originalWS + roughNoise * float3(.45, 1.0, .45) * (_PurifiedVertexRoughness * vertexPurification);
                VertexPositionInputs pos = GetVertexPositionInputs(TransformWorldToObject(displacedWS));
                float4 clip = pos.positionCS;
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    ndc = floor(ndc * _VertexSnap + 0.5) / _VertexSnap;
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip;
                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float wobble = sin((pos.positionWS.x + pos.positionWS.y) * 11.7 + _Time.y * 2.0) * _Wobble * 0.0005;
                uv += wobble;
                output.uv = _UVSteps > 0.5 ? floor(uv * _UVSteps + 0.5) / _UVSteps : uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(clip.z);
                output.shadowCoord = TransformWorldToShadowCoord(pos.positionWS);
                output.positionWS = pos.positionWS;
                output.vertexSurfaceMask = input.color.r;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixelDelta = (floor(input.positionCS.xy / _ScreenPixelSize) + 0.5) * _ScreenPixelSize - input.positionCS.xy;
                float2 sampleUV = input.uv + ddx(input.uv) * pixelDelta.x + ddy(input.uv) * pixelDelta.y;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sampleUV) * _BaseColor;
                half vertexSurfaceMask = saturate(input.vertexSurfaceMask * _VertexSurfaceBlend);
                half3 secondarySurface = SAMPLE_TEXTURE2D(_SecondaryMap, sampler_SecondaryMap,
                    float2(frac(input.uv.x * .08), lerp(.02, .98, saturate(input.uv.y)))).rgb * _SecondaryColor.rgb;
                tex.rgb = lerp(tex.rgb, secondarySurface, vertexSurfaceMask);
                half teleportAmount = saturate((half)_TeleportAmount);
                half teleportPattern = (half)frac(sin(dot(floor(input.positionWS * 17.0), float3(12.9898, 78.233, 37.719))) * 43758.5453);
                teleportPattern = saturate(teleportPattern * .72h + frac(input.positionWS.y * 6.0h - _Time.y * 2.2h) * .28h);
                half teleportBoundary = (1.0h - teleportAmount) - teleportPattern * .92h;
                clip(teleportBoundary);
                half teleportEdge = (1.0h - smoothstep(0.0h, .085h, abs(teleportBoundary))) * step(.001h, teleportAmount);
                tex.rgb += _TeleportGlowColor.rgb * teleportEdge * 2.4h;
                float2 purificationUV = (input.positionWS.xz - _PurificationOriginSize.xy) * _PurificationOriginSize.zw;
                half insideMap = step(0.0, purificationUV.x) * step(purificationUV.x, 1.0) * step(0.0, purificationUV.y) * step(purificationUV.y, 1.0);
                half purification = SAMPLE_TEXTURE2D(_PurificationMap, sampler_PurificationMap, saturate(purificationUV)).r * _PurificationResponse * insideMap;
                half3 purifiedSurface = SAMPLE_TEXTURE2D(_PurifiedMap, sampler_PurifiedMap, sampleUV).rgb;
                half upwardFace = smoothstep(.72h, .92h, normalize(input.normalWS).y);
                float2 organicCell = floor(input.positionWS.xz * 1.35);
                half organicNoise = (half)frac(sin(dot(organicCell, float2(12.9898, 78.233))) * 43758.5453);
                half soilPatch = smoothstep(.77h, .94h, organicNoise) * .72h;
                half mossPatch = smoothstep(.28h, .62h, organicNoise) * (1.0h - soilPatch * .65h);
                half3 soilColor = half3(.24h, .17h, .085h);
                half3 mossColor = half3(.22h, .39h, .105h);
                purifiedSurface = lerp(purifiedSurface, mossColor, mossPatch * .28h);
                purifiedSurface = lerp(purifiedSurface, soilColor, soilPatch);
                purifiedSurface *= lerp(float3(0.42, 0.56, 0.34), float3(1.0, 1.0, 1.0), (float)upwardFace);
                half surfaceReplacement = saturate((purification - .06h) / .88h);
                surfaceReplacement = surfaceReplacement * surfaceReplacement * surfaceReplacement * (surfaceReplacement * (surfaceReplacement * 6.0h - 15.0h) + 10.0h);
                half rockMacro = (half)RockNoise(input.positionWS * .62);
                half rockDetail = (half)RockNoise(input.positionWS * 2.35 + float3(9.1, 3.7, 5.4));
                half organicRockNoise = saturate(rockMacro * .68h + rockDetail * .32h);
                half rockStrata = floor(organicRockNoise * 7.0h + .5h) / 7.0h;
                half3 cliffDark = half3(.19h, .205h, .19h);
                half3 cliffLight = half3(.39h, .405h, .35h);
                half3 organicCliff = lerp(cliffDark, cliffLight, rockStrata);
                half moisture = (half)RockNoise(input.positionWS * .31 + float3(17.0, 2.0, -8.0));
                half crevice = 1.0h - smoothstep(.2h, .48h, abs(rockMacro - rockDetail));
                half mossIslands = smoothstep(.48h, .7h, moisture) * smoothstep(.34h, .62h, rockDetail);
                half creviceMoss = saturate(mossIslands * .78h + crevice * moisture * .42h);
                half3 mossDark = half3(.075h, .19h, .045h);
                half3 mossBright = half3(.19h, .38h, .085h);
                half3 mossSurface = lerp(mossDark, mossBright, rockDetail);
                organicCliff = lerp(organicCliff, mossSurface, creviceMoss * .72h);
                half verticalReplacement = surfaceReplacement * (1.0h - upwardFace);
                tex.rgb = lerp(tex.rgb, organicCliff, verticalReplacement);
                surfaceReplacement *= upwardFace;
                tex.rgb = lerp(tex.rgb, purifiedSurface, surfaceReplacement);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, sampleUV), _BumpScale);
                float3 baseNormal = normalize(input.normalWS);
                float3 dp1 = ddx(input.positionWS), dp2 = ddy(input.positionWS);
                float3 geometricNormal = normalize(cross(dp2, dp1));
                if (dot(geometricNormal, baseNormal) < 0.0) geometricNormal = -geometricNormal;
                baseNormal = normalize(lerp(baseNormal, geometricNormal, saturate(_GelPhysicsStrength)));
                float2 duv1 = ddx(sampleUV), duv2 = ddy(sampleUV);
                float3 tangent = dp2 * duv1.x - dp1 * duv2.x;
                float3 bitangent = dp2 * duv1.y - dp1 * duv2.y;
                tangent = normalize(tangent - baseNormal * dot(baseNormal, tangent));
                bitangent = normalize(cross(baseNormal, tangent)) * (dot(bitangent, cross(baseNormal, tangent)) < 0 ? -1 : 1);
                float3 normalWS = normalize(tangent * normalTS.x + bitangent * normalTS.y + baseNormal * normalTS.z);

                float edgeDistance = min(min(frac(input.uv.x), 1.0 - frac(input.uv.x)), min(frac(input.uv.y), 1.0 - frac(input.uv.y)));
                float irregularEdge = _EdgeWidth * (0.72 + 0.28 * sin(dot(floor(input.positionWS * 7.0), float3(1.7, 2.3, 3.1))));
                float edgeMask = (1.0 - smoothstep(irregularEdge, irregularEdge * 2.1, edgeDistance)) * _EdgeWear;
                tex.rgb = lerp(tex.rgb, tex.rgb * _EdgeTint.rgb, edgeMask);

                Light light = GetMainLight(input.shadowCoord);
                half ndl = saturate(dot(normalWS, light.direction));
                ndl = floor((0.3h + ndl * 0.7h) * 4.0h) / 4.0h;
                half pixelShadow = floor(light.shadowAttenuation * 3.0h + 0.5h) / 3.0h;
                // Keep PS1 contrast without crushing shadowed concrete into purple-black.
                half directLight = lerp(0.38h, 1.0h, pixelShadow) * light.distanceAttenuation;
                float3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize((half3)light.direction + (half3)viewDirection);
                half specularPower = lerp(4.0h, 72.0h, (half)_Smoothness);
                half specular = pow(saturate(dot((half3)normalWS, halfDirection)), specularPower);
                specular = floor(specular * _SpecularSteps + .5h) / max(2.0h, (half)_SpecularSteps);
                half3 specularColor = lerp(half3(.055h, .06h, .07h), tex.rgb, (half)_Metallic);
                half specularStrength = lerp(.12h, 1.0h, (half)_Metallic) * (half)_Smoothness;
                half3 diffuseColor = tex.rgb * (1.0h - (half)_Metallic * .42h);
                half3 color = diffuseColor * (ndl * light.color * directLight + _Ambient);
                color += specularColor * light.color * specular * specularStrength * directLight;
                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint index = 0u; index < lightCount; ++index)
                {
                    Light extra = GetAdditionalLight(index, input.positionWS);
                    half extraNdl = saturate(dot(normalWS, extra.direction));
                    color += tex.rgb * extra.color * extra.distanceAttenuation * (0.35h + extraNdl * 0.65h) * _AdditionalLightStrength;
                }
                #endif
                half rim = pow(saturate(1.0h - dot((half3)normalWS, (half3)viewDirection)), (half)_RimPower);
                // Keep the rim continuous and texture-aware: it belongs to the model instead
                // of looking like a separate dithered overlay.
                half3 sceneRimColor = lerp(half3(.34h, .4h, .52h), light.color,
                                           saturate(directLight * 1.15h));
                half3 chosenRimColor = lerp(_RimColor.rgb, sceneRimColor, _SceneLitRim);
                half3 integratedRim = lerp(tex.rgb, tex.rgb * chosenRimColor * 1.35h, .72h);
                color += integratedRim * rim * _RimStrength;
                color += secondarySurface * vertexSurfaceMask * _SecondaryEmission;
                color = floor(color * _ColorSteps + 0.5h) / _ColorSteps;
                color = MixFog(color, input.fogFactor);
                return half4(color, tex.a);
            }
            ENDHLSL
        }
        // Use URP's maintained caster pass. The old custom caster produced invalid
        // directional projections and giant clipped polygons on Metal.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}

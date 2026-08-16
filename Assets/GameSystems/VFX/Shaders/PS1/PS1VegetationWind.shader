Shader "Lennie/PS1 Vegetation Wind"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.08,0.22,0.07,1)
        _TipColor("Tip / Flower Color", Color) = (0.35,0.62,0.18,1)
        _VertexSnap("Vertex Snap", Range(100,800)) = 300
        _ColorSteps("Color Steps", Range(6,64)) = 64
        _WindStrength("Wind Strength", Range(0,0.25)) = 0.055
        _WindSpeed("Wind Speed", Range(0,5)) = 1.4
        _GustStrength("Gust Strength", Range(0,3)) = 1.7
        _GustSpeed("Gust Rhythm", Range(.05,2)) = .42
        _Turbulence("Leaf Turbulence", Range(0,1)) = .34
        _HangingGrowth("Hanging Cascade Growth", Range(0,1)) = 0
        _AdditionalLightStrength("Local Light Strength", Range(0,3)) = .85
        [HideInInspector] _MovingPlatformSmooth("Moving Platform Smooth", Range(0,1)) = 0
        [HideInInspector] _PlatformGrowthOverride("Platform Local Growth", Range(0,1)) = 0
        [HideInInspector] _PlatformGrowthAmount("Platform Growth Amount", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+2" }
        Cull Off ZWrite On
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_PurificationMap); SAMPLER(sampler_PurificationMap);
            float4 _PurificationOriginSize;
            float _PurificationGlobalStrength;
            float4 _LennieFootLight0, _LennieFootLight1, _LennieFootLightColor;
            float4 _LennieVegetationInteraction, _LennieVegetationMotion;
            float4 _EnemyVegetationInteractions[16];
            float4 _PlatformGrowthPoints[32];
            float _PlatformGrowthPointCount;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _TipColor;
                float _VertexSnap, _ColorSteps, _WindStrength, _WindSpeed, _GustStrength, _GustSpeed, _Turbulence, _HangingGrowth, _AdditionalLightStrength, _MovingPlatformSmooth, _PlatformGrowthOverride, _PlatformGrowthAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float3 rootOS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float growth : TEXCOORD1;
                float purification : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float flower : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 rootWS = TransformObjectToWorld(input.rootOS);
                float2 rawPurificationUV = (rootWS.xz - _PurificationOriginSize.xy) * _PurificationOriginSize.zw;
                float insideMap = step(0.0, rawPurificationUV.x) * step(rawPurificationUV.x, 1.0) * step(0.0, rawPurificationUV.y) * step(rawPurificationUV.y, 1.0);
                float purification = saturate(SAMPLE_TEXTURE2D_LOD(_PurificationMap, sampler_PurificationMap, saturate(rawPurificationUV), 0).r) * insideMap;
                if (_PlatformGrowthOverride > .5)
                {
                    float localPurification = 0.0;
                    [loop] for (int growthIndex = 0; growthIndex < 32; growthIndex++)
                    {
                        if (growthIndex >= (int)_PlatformGrowthPointCount) break;
                        float4 growthPoint = _PlatformGrowthPoints[growthIndex];
                        float radius = lerp(.12, .82, growthPoint.z);
                        float distanceToStep = distance(input.rootOS.xz, growthPoint.xy);
                        float stamp = 1.0 - smoothstep(radius - .14, radius + .14, distanceToStep);
                        localPurification = max(localPurification, stamp * growthPoint.z);
                    }
                    purification = localPurification;
                }
                float plantGrowth = saturate(smoothstep(.025, .82, purification));
                float3 localOffset = input.positionOS.xyz - input.rootOS;
                // Ivy stores normalized distance from its root in vertex red, so growth
                // remains progressive even when a colony is stretched across a 20 m cliff.
                float hangingDepth = saturate(1.0 - input.color.r);
                float lateralSpread = saturate(abs(localOffset.x) / 1.35);
                float propagationDistance = saturate(hangingDepth * .72 + lateralSpread * .28);
                float cascadeGrowth = smoothstep(propagationDistance * .72, propagationDistance * .72 + .2, purification);
                plantGrowth = lerp(plantGrowth, cascadeGrowth, _HangingGrowth);
                float leafGrowth = smoothstep(.22, .9, purification);
                plantGrowth *= lerp(1.0, leafGrowth, input.color.g * _HangingGrowth);
                localOffset.y *= plantGrowth;
                localOffset.xz *= saturate(plantGrowth * 2.4);
                float3 localPosition = input.rootOS + localOffset;
                float3 positionWS = TransformObjectToWorld(localPosition);
                float heightWeight = saturate(input.color.r);
                float time = _Time.y;
                // Static plants use world-space currents. Plants riding a moving platform
                // use stable local coordinates so the wind field does not slide through them.
                float2 windRoot = lerp(rootWS.xz, input.rootOS.xz, saturate(_MovingPlatformSmooth));
                float2 windPosition = lerp(positionWS.xz, localPosition.xz, saturate(_MovingPlatformSmooth));
                float rootVariation = frac(sin(dot(windRoot, float2(12.9898, 78.233))) * 43758.5453);
                float broadWave = sin(time * _GustSpeed + windRoot.x * .075 + windRoot.y * .11) * .5 + .5;
                broadWave = broadWave * broadWave * (3.0 - 2.0 * broadWave);
                float gustPulse = saturate(sin(time * (_GustSpeed * 1.87) - windRoot.x * .13 + windRoot.y * .055) * .5 + .5);
                gustPulse = gustPulse * gustPulse * gustPulse;
                float gustEnvelope = lerp(.18, 1.0 + _GustStrength, broadWave * .62 + gustPulse * .38);
                float directionAngle = .18 + sin(time * .43 + windRoot.y * .095) * .72 + sin(time * .77 - windRoot.x * .071) * .34;
                float2 globalDirection = float2(cos(directionAngle), sin(directionAngle));
                float localAngle = rootVariation * 6.28318 + sin(time * .29 + windRoot.x * .13) * .65;
                float2 localDirection = float2(cos(localAngle), sin(localAngle));
                float currentMix = .18 + (sin(time * .61 + windRoot.x * .17 + windRoot.y * .14) * .5 + .5) * .34;
                float2 windDirection = normalize(lerp(globalDirection, localDirection, currentMix));
                float flutterPhase = time * (_WindSpeed * 2.1) + windPosition.x * 2.7 + windPosition.y * 3.3 + rootVariation * 6.28318;
                float2 turbulence = float2(sin(flutterPhase), cos(flutterPhase * .73)) * _Turbulence;
                float bendWeight = pow(heightWeight, 1.35) * plantGrowth;
                positionWS.xz += (windDirection * gustEnvelope + turbulence) * _WindStrength * bendWeight;
                positionWS.y += sin(flutterPhase * 1.31) * _WindStrength * _Turbulence * bendWeight * .28;
                float2 playerDelta = positionWS.xz - _LennieVegetationInteraction.xz;
                float playerDistance = length(playerDelta);
                float contactBend = 1.0 - smoothstep(.18, max(.19, _LennieVegetationInteraction.w), playerDistance);
                float2 awayFromPlayer = playerDelta / max(playerDistance, .04);
                float2 motionDirection = _LennieVegetationMotion.xz / max(length(_LennieVegetationMotion.xz), .04);
                float interactionStrength = contactBend * bendWeight * lerp(.65, 1.35, _LennieVegetationMotion.w);
                positionWS.xz += (awayFromPlayer * .22 + motionDirection * .12) * interactionStrength;
                positionWS.y -= contactBend * bendWeight * lerp(.035, .12, _LennieVegetationMotion.w);
                [unroll] for (int enemyIndex = 0; enemyIndex < 16; enemyIndex++)
                {
                    float4 enemyInteraction = _EnemyVegetationInteractions[enemyIndex];
                    float2 enemyDelta = positionWS.xz - enemyInteraction.xz;
                    float enemyDistance = length(enemyDelta);
                    float enemyBend = 1.0 - smoothstep(.08, max(.09, enemyInteraction.w), enemyDistance);
                    enemyBend *= step(.001, enemyInteraction.w);
                    float2 enemyAway = enemyDelta / max(enemyDistance, .04);
                    positionWS.xz += enemyAway * enemyBend * bendWeight * .24;
                    positionWS.y -= enemyBend * bendWeight * .075;
                }
                float4 clip = TransformWorldToHClip(positionWS);
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    float2 snappedNdc = floor(ndc * _VertexSnap + .5) / _VertexSnap;
                    ndc = lerp(snappedNdc, ndc, saturate(_MovingPlatformSmooth));
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.growth = heightWeight;
                output.purification = purification;
                output.positionWS = positionWS;
                // Ivy flowers alone use low vertex alpha. Other flowers and leaves keep
                // their original palette even when their RGB vertex color is white.
                output.flower = 1.0 - step(.8, input.color.a);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(input.purification - .018h);
                half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, input.growth);
                albedo = lerp(albedo, half3(.94h, .96h, .9h), saturate(input.flower));
                Light mainLight = GetMainLight(input.shadowCoord);
                half ndl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half shadow = smoothstep(.05h, .95h, mainLight.shadowAttenuation);
                half lightTerm = (.2h + ndl * .8h) * lerp(.34h, 1.0h, shadow);
                half localAtmosphere = input.purification * lerp(.15h, 1.0h, (half)_PurificationGlobalStrength);
                half3 normal = normalize(input.normalWS);
                half3 viewDirection = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                half backLight = pow(saturate(dot(-normal, mainLight.direction)), 2.0h);
                half rim = pow(1.0h - saturate(dot(normal, viewDirection)), 2.0h);
                half translucency = (backLight * .82h + rim * .42h) * input.growth * localAtmosphere;
                half3 color = albedo * (lightTerm * mainLight.color + .11h + localAtmosphere * half3(.22h, .25h, .055h));
                color += _TipColor.rgb * translucency;
                #if defined(_ADDITIONAL_LIGHTS)
                uint localLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < localLightCount; ++lightIndex)
                {
                    Light localLight = GetAdditionalLight(lightIndex, input.positionWS);
                    half localNdl = saturate(dot(normal, localLight.direction));
                    half transmitted = pow(saturate(dot(-normal, localLight.direction)), 1.35h);
                    half localIntensity = saturate(localLight.distanceAttenuation * _AdditionalLightStrength);
                    half shapedLight = localIntensity * (.18h + localNdl * .42h);
                    half3 preservedGreen = lerp(albedo, _TipColor.rgb, .22h + transmitted * .28h);
                    half3 tintedPlantLight = preservedGreen * lerp(half3(1.0h, 1.0h, 1.0h), localLight.color, .72h);
                    color = lerp(color, color * .82h + tintedPlantLight * .62h, shapedLight);
                    half edgeGlow = localIntensity * (transmitted * .24h + rim * .16h) * input.growth;
                    color += localLight.color * _TipColor.rgb * edgeGlow;
                }
                #endif
                float footRange = max(.05, _LennieFootLightColor.w);
                half foot0 = saturate(1.0h - (half)(distance(input.positionWS, _LennieFootLight0.xyz) / footRange));
                half foot1 = saturate(1.0h - (half)(distance(input.positionWS, _LennieFootLight1.xyz) / footRange));
                foot0 = foot0 * foot0 * (half)_LennieFootLight0.w;
                foot1 = foot1 * foot1 * (half)_LennieFootLight1.w;
                half footInfluence = saturate(foot0 + foot1);
                half3 footLitColor = color * .62h + albedo * .28h + _TipColor.rgb * .22h + _LennieFootLightColor.rgb * .2h;
                color = lerp(color, footLitColor, footInfluence * .78h);
                color += _LennieFootLightColor.rgb * (rim * .14h + backLight * .1h) * footInfluence * input.growth;
                color = floor(color * _ColorSteps + .5h) / _ColorSteps;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

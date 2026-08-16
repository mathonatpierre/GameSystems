Shader "Lennie/PS1 Translucent Gem"
{
    Properties
    {
        _BaseColor("Gem Color", Color) = (.32,.92,1,.62)
        _RimColor("Rim Color", Color) = (1,.42,.92,1)
        _Emission("Emission", Range(0,6)) = 2.8
        _ReflectionStrength("Reflections", Range(0,4)) = 1.35
        _RimPower("Rim Width", Range(.5,8)) = 2.2
        _VertexSnap("Vertex Snap", Range(64,640)) = 220
        _ColorSteps("Color Steps", Range(2,64)) = 64
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "ForwardTranslucent"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _Emission;
                float _ReflectionStrength;
                float _RimPower;
                float _VertexSnap;
                float _ColorSteps;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                if (output.positionCS.w > .05)
                {
                    float2 ndc = output.positionCS.xy / output.positionCS.w;
                    ndc = floor(ndc * _VertexSnap + .5) / _VertexSnap;
                    output.positionCS.xy = ndc * output.positionCS.w;
                }
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light light = GetMainLight();
                half diffuse = .3h + saturate(dot(normalWS, light.direction)) * .7h;
                diffuse = floor(diffuse * _ColorSteps + .5h) / _ColorSteps;
                half rim = pow(1.0h - saturate(dot(normalWS, viewWS)), _RimPower);
                half pulse = .88h + sin(_Time.y * 4.0h + input.positionWS.y * 3.0h) * .12h;
                half3 color = _BaseColor.rgb * diffuse;
                color += _BaseColor.rgb * _Emission * .22h * pulse;
                color += _RimColor.rgb * rim * _Emission;
                half reflection = pow(saturate(dot(reflect(-light.direction, normalWS), viewWS)), 24.0h);
                color += lerp(_BaseColor.rgb, _RimColor.rgb, .65h) * reflection * _ReflectionStrength;
                half alpha = saturate(_BaseColor.a + rim * .22h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

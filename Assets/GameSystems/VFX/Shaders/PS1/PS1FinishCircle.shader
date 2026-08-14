Shader "Lennie/PS1 Finish Circle"
{
    Properties
    {
        _RingColor("Ring Color", Color) = (.76,.25,1,1)
        _CoreColor("Core Color", Color) = (1,.78,1,1)
        _Intensity("Intensity", Range(.2,4)) = 1.6
        _RingWidth("Ring Width", Range(.01,.3)) = .075
        _PulseSpeed("Pulse Speed", Range(0,5)) = 1.35
        _Opacity("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off ZTest LEqual Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor, _CoreColor;
                float _Intensity, _RingWidth, _PulseSpeed, _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    ndc = floor(ndc * 380.0 + .5) / 380.0;
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip; output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv - .5; float radius = length(centered) * 2.0;
                half outerFade = 1.0h - smoothstep(.82h, 1.0h, (half)radius);
                half ring = 1.0h - smoothstep(_RingWidth, _RingWidth * 2.1h, abs((half)radius - .72h));
                half innerRing = 1.0h - smoothstep(.018h, .055h, abs((half)radius - .43h));
                half pulse = .76h + .24h * sin(_Time.y * _PulseSpeed);
                half centerGlow = pow(saturate(1.0h - (half)radius), 2.2h) * .24h;
                half alpha = saturate((ring + innerRing * .42h + centerGlow) * outerFade * pulse) * _Opacity;
                half3 color = lerp(_RingColor.rgb, _CoreColor.rgb, saturate(ring + innerRing)) * _Intensity;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

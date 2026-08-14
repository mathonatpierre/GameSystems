Shader "Lennie/PS1 Finish Solid"
{
    Properties
    {
        _Color("Glow Color", Color) = (.72,.22,1,1)
        _Intensity("Intensity", Range(.2,8)) = 3
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
                float4 _Color;
                float _Intensity;
                float _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    ndc = floor(ndc * 420.0 + .5) / 420.0;
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half pulse = .88h + .12h * sin(_Time.y * 2.1h);
                return half4(_Color.rgb * input.color.rgb * _Intensity * pulse, _Color.a * input.color.a * _Opacity);
            }
            ENDHLSL
        }
    }
}

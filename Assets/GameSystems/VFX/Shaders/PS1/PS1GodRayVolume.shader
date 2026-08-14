Shader "Lennie/PS1 God Ray Volume"
{
    Properties
    {
        _Color("Ray Color", Color) = (1,.78,.38,1)
        _Opacity("Opacity", Range(0,.3)) = .075
        _VertexSnap("Vertex Snap", Range(100,800)) = 360
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off ZTest LEqual Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_PurificationMap); SAMPLER(sampler_PurificationMap);
            float4 _PurificationOriginSize;
            float _PurificationCameraStrength;
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Opacity;
                float _VertexSnap;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 color : COLOR; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 clip = TransformWorldToHClip(output.positionWS);
                if (clip.w > .05)
                {
                    float2 ndc = clip.xy / clip.w;
                    ndc = floor(ndc * _VertexSnap + .5) / _VertexSnap;
                    clip.xy = ndc * clip.w;
                }
                output.positionCS = clip; output.color = input.color; output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 rawPurificationUV = (input.positionWS.xz - _PurificationOriginSize.xy) * _PurificationOriginSize.zw;
                half insideMap = step(0.0, rawPurificationUV.x) * step(rawPurificationUV.x, 1.0) * step(0.0, rawPurificationUV.y) * step(rawPurificationUV.y, 1.0);
                half localPurity = SAMPLE_TEXTURE2D(_PurificationMap, sampler_PurificationMap, saturate(rawPurificationUV)).r * insideMap;
                half reveal = smoothstep(.18h, .82h, localPurity) * smoothstep(.08h, .55h, (half)_PurificationCameraStrength);
                half softWidth = sin(saturate(input.uv.x) * 3.14159265h);
                half longitudinalFade = smoothstep(0.0h, .12h, (half)input.uv.y) * (1.0h - smoothstep(.84h, 1.0h, (half)input.uv.y));
                half movingDust = .72h + .28h * sin(input.positionWS.y * .47h + input.positionWS.x * .31h + _Time.y * .22h);
                half alpha = _Opacity * input.color.a * softWidth * longitudinalFade * movingDust * reveal;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}

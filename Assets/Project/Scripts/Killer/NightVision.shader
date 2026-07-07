Shader "Hidden/NightVision"
{
    Properties
    {
        _BlitTexture ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _BlitTexture_TexelSize;
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _NoiseIntensity;
            float _VignetteRadius;
            float _VignetteSoftness;
            float _LensGap;
            float4 _TintColor;
            float _Time1;
            float _Gain;
            float _GammaCurve;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy * 2.0 - 1.0, 0.0, 1.0);
                output.uv = float2(input.uv.x, 1.0 - input.uv.y);
                return output;
            }

            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float BinocularMask(float2 uv)
            {
                float aspect = max(_BlitTexture_TexelSize.z / max(_BlitTexture_TexelSize.w, 1.0), 0.001);
                float2 centered = (uv - 0.5) * float2(aspect, 1.0);
                float distanceLeft = length(centered - float2(-_LensGap, 0));
                float distanceRight = length(centered - float2(_LensGap, 0));
                return min(distanceLeft, distanceRight);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv);

                float safeGain = max(_Gain, 0.01);
                float safeGamma = max(_GammaCurve, 0.01);
                float3 safeTint = max(_TintColor.rgb, float3(0.0, 0.0, 0.0));

                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                luminance = pow(saturate(luminance * safeGain), safeGamma);
                half3 nightColor = luminance * safeTint;

                float scanline = sin(input.uv.y * _BlitTexture_TexelSize.w * 1.5 - _Time1 * _ScanlineSpeed);
                nightColor *= 1.0 - (scanline * 0.5 + 0.5) * saturate(_ScanlineIntensity);

                float noise = Random(input.uv * max(_Time1, 0.01)) * saturate(_NoiseIntensity);
                nightColor += noise;

                float mask = BinocularMask(input.uv);
                float outerRadius = max(_VignetteRadius, 0.001);
                float innerRadius = max(outerRadius - max(_VignetteSoftness, 0.001), 0.0);
                float vignette = 1.0 - smoothstep(innerRadius, outerRadius, mask);
                nightColor *= vignette;

                return half4(nightColor, col.a);
            }
            ENDHLSL
        }
    }
}

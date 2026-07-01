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

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _ScanlineIntensity;
            float  _ScanlineSpeed;
            float  _NoiseIntensity;
            float  _VignetteRadius;
            float  _VignetteSoftness;
            float  _LensGap;
            float4 _TintColor;
            float  _Time1;
            float _Gain;
            float _GammaCurve;

            //idk randomness this is the best you're gonna get
            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float BinocularMask(float2 uv)
            {
                float2 centered = (uv - 0.5) * float2(_BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w, 1.0);
                float distanceLeft = length(centered - float2(-_LensGap, 0));
                float distanceRight = length(centered - float2( _LensGap, 0));
                return min(distanceLeft, distanceRight);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                //remind me to remove magic numbers
                //stackoverflow more like chudoverflow
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                luminance = pow(saturate(luminance * _Gain), _GammaCurve);
                half3 nightColor = luminance * _TintColor.rgb;

                float scanline = sin(i.texcoord.y * _BlitTexture_TexelSize.w * 1.5 - _Time1 * _ScanlineSpeed);
                nightColor *= 1.0 - (scanline * 0.5 + 0.5) * _ScanlineIntensity;

                float noise = Random(i.texcoord * _Time1) * _NoiseIntensity;
                nightColor += noise;

                float mask = BinocularMask(i.texcoord);
                float vignette = smoothstep(_VignetteRadius, _VignetteRadius - _VignetteSoftness, mask);
                nightColor *= vignette;

                return half4(nightColor, col.a);
            }
            ENDHLSL
        }
    }
}
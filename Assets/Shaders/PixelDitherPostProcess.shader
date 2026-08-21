Shader "Hidden/DebtPit/PixelDitherPostProcess"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 4.8
        _ColorSteps ("Color Steps", Float) = 7
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Pixel Dither"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _PixelSize;
            float _ColorSteps;
            float _DitherStrength;
            CBUFFER_END
            
            float Bayer4(float2 pixel)
            {
                int2 cell = int2(floor(fmod(pixel, 4.0)));
                float4 row = cell.y == 0 ? float4(0.0, 8.0, 2.0, 10.0) :
                             cell.y == 1 ? float4(12.0, 4.0, 14.0, 6.0) :
                             cell.y == 2 ? float4(3.0, 11.0, 1.0, 9.0) :
                                           float4(15.0, 7.0, 13.0, 5.0);
                return (row[cell.x] / 16.0) - 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenPixels = input.texcoord * _ScreenParams.xy;
                float2 cell = floor(screenPixels / _PixelSize);
                float2 pixelUv = ((cell + 0.5) * _PixelSize) / _ScreenParams.xy;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelUv);

                float dither = Bayer4(cell) * _DitherStrength / _ColorSteps;
                color.rgb = floor(saturate(color.rgb + dither) * _ColorSteps) / _ColorSteps;

                return color;
            }
            ENDHLSL
        }
    }
}

// 화면 전체를 큼직한 픽셀 격자로 뭉갠다.
// URP의 Full Screen Pass Renderer Feature에 물려 쓴다.
//
// 씬/에셋 배선:
//   1) 이 셰이더로 머티리얼을 만든다
//   2) URP Renderer 에셋 > Add Renderer Feature > Full Screen Pass Renderer Feature
//   3) Pass Material 에 그 머티리얼을 지정
//   4) Injection Point = After Rendering Post Processing
//
// 주의: Screen Space - Overlay 캔버스는 포스트 처리 뒤에 그려져서 뭉개지지 않는다.
//       UI까지 같이 뭉개려면 캔버스를 Screen Space - Camera로 바꿀 것.

Shader "LSO/Fullscreen/Pixelate"
{
    Properties
    {
        [Header(Resolution)]
        [Toggle] _UseTargetResolution ("Use Target Resolution", Float) = 1

        // 가상 세로 해상도. 240이면 240p처럼 보인다.
        _TargetHeight   ("Target Height (virtual 240p etc)", Range(32, 1080)) = 240

        // 위 토글을 끄면 이 값을 쓴다. 한 덩어리가 화면에서 차지할 픽셀 수.
        _PixelSize      ("Pixel Size (px, toggle off)", Range(1, 32)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Pixelate"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            // Core를 먼저 넣어야 _ScreenParams 같은 전역이 잡힌다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _UseTargetResolution;
            float _TargetHeight;
            float _PixelSize;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float2 grid;

                if (_UseTargetResolution > 0.5)
                {
                    // 가상 해상도 기준. 실제 창 크기가 바뀌어도 덩어리 개수가 그대로라
                    // 어느 해상도에서 봐도 같은 느낌이 난다.
                    float height = max(_TargetHeight, 1.0);
                    float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

                    // 가로도 같이 계산해야 덩어리가 정사각형으로 유지된다.
                    grid = float2(round(height * aspect), height);
                }
                else
                {
                    // 화면 픽셀 기준. 4K에서는 상대적으로 잘게 보인다.
                    grid = _ScreenParams.xy / max(_PixelSize, 1.0);
                }

                // 덩어리 한가운데를 집는다. +0.5를 빼면 왼쪽 위로 반 칸 밀린다.
                uv = (floor(uv * grid) + 0.5) / grid;

                // 포인트 샘플링이라야 덩어리 경계가 뭉개지지 않고 각지게 남는다.
                float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

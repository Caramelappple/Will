// 화면 색을 몇 단계로 뭉갠다. 옛날 저색심도 화면처럼 보인다.
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

Shader "LSO/Fullscreen/Posterize"
{
    Properties
    {
        [Header(Mode)]
        // 켜면 색조와 채도와 명도를 따로 뭉갠다. 끄면 RGB 채널을 같은 단계로 뭉갠다.
        [Toggle] _UseHsv ("Separate Hue Sat Value", Float) = 0

        [Header(RGB)]
        _Levels      ("Levels (RGB)", Range(2, 64)) = 8

        [Header(HSV)]
        // 색조는 적게 줄수록 색이 몇 가지로 확 몰린다. 4~8이 확실히 티가 난다.
        _HueLevels   ("Hue Levels", Range(2, 64)) = 8

        _SatLevels   ("Saturation Levels", Range(2, 64)) = 6

        // 명도만 잘게 두면 원래 색을 살린 채 계단만 도드라진다.
        _ValueLevels ("Value Levels", Range(2, 64)) = 6

        [Header(Blend)]
        // 0이면 원본, 1이면 완전히 뭉갠 화면. 연출용으로 켜고 끌 때 쓴다.
        _Strength    ("Strength", Range(0, 1)) = 1
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
            Name "Posterize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            // Core를 먼저 넣어야 전역들이 잡힌다. Color.hlsl에 RgbToHsv와 감마 변환이 들어 있다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _UseHsv;
            float _Levels;
            float _HueLevels;
            float _SatLevels;
            float _ValueLevels;
            float _Strength;

            // 양 끝을 살리는 계단. levels가 4면 0, 1/3, 2/3, 1 네 값이 나온다.
            //
            // floor(x * n) / n 을 쓰면 최댓값이 (n-1)/n 이라 흰색이 흰색으로 안 남고
            // 화면 전체가 한 단계씩 어두워진다.
            float PosterizeLinear(float x, float levels)
            {
                float steps = max(levels - 1.0, 1.0);

                return round(saturate(x) * steps) / steps;
            }

            // 색조는 0과 1이 같은 빨강이라 양 끝을 살리면 빨강 구간만 두 배로 넓어진다.
            // 그래서 여기만 floor로 균등하게 자른다.
            float PosterizeHue(float h, float levels)
            {
                float steps = max(levels, 1.0);

                return floor(frac(h) * steps) / steps;
            }

            float3 PosterizeRgb(float3 c)
            {
                return float3(
                    PosterizeLinear(c.r, _Levels),
                    PosterizeLinear(c.g, _Levels),
                    PosterizeLinear(c.b, _Levels));
            }

            float3 PosterizeHsv(float3 c)
            {
                float3 hsv = RgbToHsv(c);

                hsv.x = PosterizeHue(hsv.x, _HueLevels);
                hsv.y = PosterizeLinear(hsv.y, _SatLevels);
                hsv.z = PosterizeLinear(hsv.z, _ValueLevels);

                return HsvToRgb(hsv);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 source = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp, input.texcoord).rgb;

                // 프로젝트가 Linear 컬러스페이스라 버퍼 값도 선형이다.
                // 선형 값을 그대로 자르면 사람 눈에는 어두운 쪽에만 계단이 몰려 보인다.
                // 눈에 보이는 밝기(감마) 기준으로 잘라야 화면 전체에 계단이 고르게 깔린다.
                float3 gamma = LinearToSRGB(saturate(source));

                float3 posterized = _UseHsv > 0.5
                    ? PosterizeHsv(gamma)
                    : PosterizeRgb(gamma);

                float3 color = SRGBToLinear(posterized);

                return float4(lerp(source, color, saturate(_Strength)), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

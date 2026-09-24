Shader "Will/Fullscreen/Boss Roar Ripple"
{
    Properties
    {
        _RippleParams ("Progress, Strength, Aspect", Vector) = (0, 0, 1.7778, 0)
        _ShakeParams ("Shake Offset XY, Edge Crop", Vector) = (0, 0, 0, 0)
        _FocusParams ("Boss Viewport Center", Vector) = (0.5, 0.5, 0, 0)
        _DimmingParams ("Reveal Progress, Darkness, Radius, Enabled", Vector) = (0, 0, 0.22, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "Boss Roar Ripple"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _RippleParams;
            float4 _ShakeParams;
            float4 _FocusParams;
            float4 _DimmingParams;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 center = _FocusParams.xy;
                // URP 카메라 컬러를 Blit한 UV에 viewport 좌표를 그대로 사용한다.
                float aspect = max(_RippleParams.z, 0.01);
                // 떨림·확대로 이동한 모델과 파동·암전의 중심을 함께 맞춘다.
                float2 sceneUV = (uv - 0.5) * (1 - 2 * _ShakeParams.z) + 0.5 + _ShakeParams.xy;
                float2 radial = (sceneUV - center) * float2(aspect, 1);
                float distanceFromCenter = length(radial);
                float2 direction = radial / max(distanceFromCenter, 0.0001);
                float progress = _RippleParams.x;
                // 마지막 파동도 화면 모서리까지 도달하도록 종횡비에 맞춘다.
                float2 farthestCorner = max(abs(center), abs(1 - center)) * float2(aspect, 1);
                float maxRadius = length(farthestCorner);
                float travel = (maxRadius + 0.18) / 0.68;
                float displacement = 0;
                [unroll]
                for (int ring = 0; ring < 3; ring++)
                {
                    float age = progress - ring * 0.14;
                    float delta = distanceFromCenter - max(age, 0) * travel;
                    float band = exp(-delta * delta / (0.065 * 0.065));
                    float envelope = smoothstep(0, 0.055, age) / (1 + max(age, 0) * 3);
                    displacement += sin(delta * 85) * band * envelope * (1 - ring * 0.2);
                }
                float edge = min(min(uv.x, 1 - uv.x), min(uv.y, 1 - uv.y));
                displacement *= _RippleParams.y * (1 - smoothstep(0.72, 1, progress));
                displacement *= smoothstep(0, 0.025, edge);
                float2 offset = direction * displacement / float2(aspect, 1);
                float2 sampleUV = sceneUV + offset * (1 - 2 * _ShakeParams.z);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(sampleUV));
                float dimProgress = _DimmingParams.x;
                float radius = max(_DimmingParams.z, 0.001);
                // 포효 전에는 주변을 검게 유지하고 보스 중심만 희미하게 드러낸다.
                float silhouette = (1 - _DimmingParams.y)
                    * (1 - smoothstep(radius * 0.35, radius, distanceFromCenter));
                float revealRadius = lerp(-0.08, maxRadius + 0.08, smoothstep(0, 1, dimProgress));
                float unrevealed = smoothstep(revealRadius - 0.07, revealRadius + 0.07, distanceFromCenter);
                color.rgb *= lerp(1, silhouette, _DimmingParams.w * unrevealed);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

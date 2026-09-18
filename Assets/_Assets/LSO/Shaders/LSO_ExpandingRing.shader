// 중심에서 밖으로 퍼져나가는 네온 링.
//
// 바닥에 눕힌 평면(Quad)에 쓴다. 한 번 퍼지고 사라지는 연출이며,
// 시간이 아니라 _Progress 값이 모든 것을 정한다.
//
// ── 왜 시간이 아니라 진행도인가 ───────────────────────────────
// _Time 으로 돌리면 연출의 길이를 쉐이더가 정하게 된다. 그러면 "0.6초짜리
// 발동 연출"을 만들 때 쉐이더 속도와 오브젝트 수명을 따로 맞춰야 하고,
// 둘이 어긋나면 링이 덜 퍼진 채로 사라지거나 다 퍼지고도 남는다.
//
// 진행도를 밖에서 주면 길이를 정하는 곳이 하나가 된다(LSO_RingPulse).
// 에디터에서 슬라이더를 끌어 아무 순간이나 그대로 볼 수 있는 것도 덤이다.
// ─────────────────────────────────────────────────────────────
//
// ── 링 말고는 완전히 투명하다 ─────────────────────────────────
// 번짐은 exp 곡선을 쓰지 않는다. exp 는 아무리 멀어도 0 이 되지 않아서,
// 평면 전체에 아주 옅은 빛이 깔린다. 더하기 블렌드에서는 그 옅은 값이
// 겹칠수록 쌓여 네모난 자국으로 드러난다.
//
// 지금은 _Glow 거리에서 정확히 0 이 되는 곡선을 쓴다. 그 밖은 알파가 0 이다.
// ─────────────────────────────────────────────────────────────

Shader "LSO/Expanding Ring"
{
    Properties
    {
        [HDR] _Color ("색", Color) = (0.30, 1.0, 0.15, 1)

        [Space]
        _Progress ("진행도", Range(0,1)) = 0

        [Header(Ring)]
        _RingCount ("링 개수", Range(1,4)) = 3
        _RingDelay ("링 사이 간격", Range(0,0.4)) = 0.12
        _Reach ("퍼지는 거리", Range(0.1,1)) = 0.92
        _Thickness ("선 굵기", Range(0.002,0.2)) = 0.025
        _ThicknessGrow ("퍼질수록 굵어짐", Range(0,3)) = 0.8

        [Header(Glow)]
        _Glow ("번짐 폭", Range(0.005,0.4)) = 0.09
        _GlowStrength ("번짐 세기", Range(0,3)) = 0.7
        _GlowFalloff ("번짐 감쇠", Range(1,6)) = 2
        _CoreBoost ("심지 밝기", Range(1,8)) = 3

        [Header(Shape)]
        // 회색(0.5)이 "안 흔듦"이다. 비워두면 유니티가 회색 텍스처를 주므로
        // 아무것도 안 꽂아도 완전한 원이 나온다 — 조용히 이상해지지 않는다.
        [NoScaleOffset] _WobbleTex ("일그러짐 무늬", 2D) = "gray" {}
        _Wobble ("일그러짐 세기", Range(0,0.08)) = 0.01
        _WobbleTiling ("무늬 반복 (둘레 / 반지름)", Vector) = (4,1,0,0)
        _WobbleScroll ("무늬 흐름 (둘레 / 반지름)", Vector) = (0,0,0,0)
        _EdgeFade ("가장자리 여백", Range(0.5,1)) = 0.92
        _FadeStart ("사라지기 시작", Range(0,1)) = 0.6

        // ── 이름을 왜 이렇게 지었나 ───────────────────────────────
        // 그냥 _SrcBlend · _DstBlend 로 두면 **URP Lit 과 이름이 겹친다.**
        // 쉐이더를 바꿔도 이름이 같은 값은 머티리얼에 그대로 남으므로,
        // 불투명 머티리얼에서 넘어오면 One/Zero(덮어쓰기)를 물려받아
        // 링 밖의 검은 부분까지 그대로 그려진다.
        //
        // 겹치지 않는 이름을 쓰면 언제나 아래 기본값에서 시작한다.
        // ─────────────────────────────────────────────────────────
        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _LSO_SrcBlend ("Src", Float) = 5  // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _LSO_DstBlend ("Dst", Float) = 1  // One (더하기)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "ExpandingRing"

            // 바닥에 눕히는 평면이라 양면 모두 그린다. 깊이는 쓰지 않는다 —
            // 겹쳐 놓아도 서로를 가리지 않아야 한다.
            Cull Off
            ZWrite Off
            ZTest LEqual

            // 바닥 메쉬와 같은 높이에 놓였을 때 z-파이팅으로 지글거리는 것을 막는다.
            // 오브젝트를 살짝 띄우는 것보다 이쪽이 확실하다 — 카메라 각도에 안 흔들린다.
            Offset -1, -1

            Blend [_LSO_SrcBlend] [_LSO_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Progress;
                float  _RingCount;
                float  _RingDelay;
                float  _Reach;
                float  _Thickness;
                float  _ThicknessGrow;
                float  _Glow;
                float  _GlowStrength;
                float  _GlowFalloff;
                float  _CoreBoost;
                float  _Wobble;
                float4 _WobbleTiling;
                float4 _WobbleScroll;
                float  _EdgeFade;
                float  _FadeStart;
            CBUFFER_END

            // 텍스처와 샘플러는 CBUFFER 밖에 둔다. 안에 넣으면 컴파일이 안 된다.
            TEXTURE2D(_WobbleTex);
            SAMPLER(sampler_WobbleTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 가운데가 0, 안쪽 원의 가장자리가 1.
                float2 p = IN.uv - 0.5;
                float  r = length(p) * 2.0;
                float  ang = atan2(p.y, p.x);

                // 완벽한 원은 기계처럼 보인다. 무늬를 읽어 반지름을 조금 흔든다.
                //
                // ── 왜 극좌표로 읽나 ──────────────────────────────────
                // 무늬를 평면 UV 로 그냥 읽으면 링이 퍼져나갈 때 무늬는 제자리에
                // 박혀 있어서, 링이 무늬 위를 미끄러지는 것처럼 보인다.
                //
                // 가로를 각도, 세로를 반지름으로 읽으면 무늬가 링을 따라 돈다.
                // 가로 반복은 **정수**로 둘 것 — 0도와 360도가 만나는 자리에서
                // 무늬가 이어지지 않으면 거기 한 줄이 그어진다.
                //
                // 밉맵을 끄고 읽는다(LOD 0). 각도는 -180도와 180도 사이에서 값이
                // 껑충 뛰는데, 그 자리에서 GPU 가 "무늬가 급히 변한다"고 오해해
                // 흐릿한 선을 남긴다.
                // ─────────────────────────────────────────────────────
                float2 wobbleUV = float2(
                    ang * (1.0 / 6.2831853) * _WobbleTiling.x + _WobbleScroll.x * _Progress,
                    r * _WobbleTiling.y + _WobbleScroll.y * _Progress);

                float noise =
                    SAMPLE_TEXTURE2D_LOD(_WobbleTex, sampler_WobbleTex, wobbleUV, 0).r;

                // 회색(0.5)이 0 이 되도록 옮긴다. 밝으면 밖으로, 어두우면 안으로 밀린다.
                float rr = r + (noise * 2.0 - 1.0) * _Wobble;

                float core = 0.0;
                float halo = 0.0;

                // 링 개수는 인스펙터에서 바뀌므로 최대치만큼 돌고 마스크로 끈다.
                // 중간에 break 하면 컴파일러가 풀지 못해 오히려 느려진다.
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    float active = step((float)k, _RingCount - 0.5);

                    // 뒤쪽 링일수록 늦게 떠난다. 남은 시간 안에서 다 퍼지도록 늘려 잡는다.
                    float delay = (float)k * _RingDelay;
                    float span  = max(1e-3, 1.0 - delay);
                    float t     = saturate((_Progress - delay) / span);

                    // 아직 떠나지 않은 링은 가운데 점으로 보이면 안 된다.
                    float started = step(1e-4, _Progress - delay);

                    float radius = t * _Reach;
                    float d      = abs(rr - radius);

                    // 퍼질수록 굵어진다. 멀어질수록 힘이 풀리는 모양이다.
                    float th = _Thickness * (1.0 + _ThicknessGrow * t);

                    // 끝에 다다르면 사라진다. 다 퍼진 링이 테두리에 남아 있으면 안 된다.
                    float life = 1.0 - smoothstep(_FadeStart, 1.0, t);

                    float on = started * active * life;

                    core += (1.0 - smoothstep(0.0, th, d)) * on;

                    // _Glow 거리에서 **정확히 0** 이 된다. 그 밖은 아무것도 안 남는다.
                    float falloff = saturate(1.0 - d / max(1e-5, _Glow));

                    halo += pow(falloff, _GlowFalloff) * on;
                }

                // 평면 끝에서 잘려 네모로 보이는 것을 막는다.
                float edge = 1.0 - smoothstep(_EdgeFade, 1.0, r);

                float intensity = (core * _CoreBoost + halo * _GlowStrength) * edge;

                half4 col = _Color * IN.color;
                col.rgb *= intensity;
                col.a    = saturate(intensity) * _Color.a * IN.color.a;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

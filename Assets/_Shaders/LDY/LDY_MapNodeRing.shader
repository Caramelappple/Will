Shader "LDY/UI/MapNodeRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color      ("Color", Color) = (1, 0.92, 0.6, 1)
        _Progress   ("Progress", Range(0,1)) = 0
        _Radius     ("Radius", Range(0,0.5)) = 0.4
        _Thickness  ("Thickness", Range(0,0.2)) = 0.02
        _Softness   ("Softness", Range(0.0001,0.05)) = 0.009

        [Header(Hand Drawn)]
        _StartAngle ("Start Angle Offset (0 = 6시 시작)", Range(0,1)) = 0
        _Turns      ("Turns (한 바퀴 넘겨 겹치는 양)", Range(1,2)) = 1.15
        _Wobble     ("Wobble (손떨림)", Range(0,0.05)) = 0.012
        _Drift      ("Drift (겹칠 때 벌어지는 간격)", Range(0,0.05)) = 0.018
        _Rough      ("Rough (분필처럼 잘게 튐)", Range(0,0.03)) = 0.006
        _RoughScale ("Rough Scale", Range(4,200)) = 46
        _TipLength  ("Tip Length (펜촉 테이퍼)", Range(0.001,0.4)) = 0.13
        _Pressure   ("Pressure (필압 변화)", Range(0,1)) = 0.35
        _Grain      ("Grain (연필 질감)", Range(0,1)) = 0.45
        _GrainScale ("Grain Scale", Range(4,400)) = 90
        _Seed       ("Seed", Range(0,10)) = 0

        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID", Float) = 0
        _StencilOp        ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask", Float) = 255
        _ColorMask        ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull      Off
        ZWrite    Off
        ZTest     [unity_GUIZTestMode]
        Blend     SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TAU 6.28318530718

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
                float  _Radius;
                float  _Thickness;
                float  _Softness;
                float  _StartAngle;
                float  _Turns;
                float  _Wobble;
                float  _Drift;
                float  _Rough;
                float  _RoughScale;
                float  _TipLength;
                float  _Pressure;
                float  _Grain;
                float  _GrainScale;
                float  _Seed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            // 텍스처 없이 알갱이를 만들기 위한 값싼 해시
            float Hash21(float2 p)
            {
                p  = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 획 위의 한 점을 그린다.
            //   t     : 획을 따라 진행한 양. 바퀴 단위라 1.0을 넘으면 두 바퀴째다.
            //   swept : 지금까지 그어진 총량. t가 이보다 크면 아직 안 그려진 부분.
            // 두 바퀴째는 t가 더 크므로 _Drift만큼 바깥으로 벌어져, 시작점 위를 스쳐 지나가며
            // 선이 교차한다. 손으로 급하게 원을 그릴 때 나오는 그 겹침이다.
            float Stroke(float t, float d, float swept)
            {
                float a = t * TAU;

                // 손떨림: 주파수 3/5/8을 겹쳐 한 바퀴 안에서 규칙적으로 보이지 않게 한다.
                float wob = sin(a * 3.0 + _Seed)       * 0.55
                          + sin(a * 5.0 + _Seed * 2.3) * 0.30
                          + sin(a * 8.0 + _Seed * 4.1) * 0.15;

                // 분필이 종이 결에 걸려 튀는 느낌. floor로 구간을 끊어 계단식으로 어긋나게 한다.
                float rough = Hash21(float2(floor(t * _RoughScale), _Seed + 1.0)) - 0.5;

                float radius = _Radius + wob * _Wobble + rough * _Rough + t * _Drift;

                // 필압. 획을 대는 순간(t=0 부근)과 펜 끝(swept 부근)이 가늘어진다.
                // 선단부가 항상 뾰족해서 "지금 그려지는 중"으로 읽힌다.
                float tipIn  = smoothstep(0.0, _TipLength, t);
                float tipOut = smoothstep(0.0, _TipLength, swept - t);
                float press  = 1.0 - _Pressure * 0.5 * (1.0 + sin(a * 4.0 + _Seed * 3.7));

                float th = _Thickness * min(tipIn, tipOut) * press;

                float ring = 1.0 - smoothstep(th, th + _Softness, abs(d - radius));

                // 아직 안 지나간 구간은 자른다. 경계는 tipOut이 이미 뾰족하게 만들어 놨다.
                return ring * step(t, swept);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv - 0.5;
                float  d = length(p);

                // atan2(x, y)는 12시에서 시계방향으로 도는 각이다.
                // +0.5를 더해 출발점을 6시로 옮긴다. (_StartAngle은 그 위에 얹는 추가 오프셋)
                float ang = frac(atan2(p.x, p.y) / TAU + 0.5 + _StartAngle);

                // 총 _Turns 바퀴를 긋는다. 1을 넘는 만큼이 시작점을 지나쳐 겹치는 구간이다.
                float swept = _Progress * _Turns;

                // 이 각도는 첫 바퀴와 두 바퀴째, 최대 두 번 지나간다. 둘 다 그려보고 진한 쪽을 쓴다.
                float cover = max(Stroke(ang, d, swept),
                                  Stroke(ang + 1.0, d, swept));

                // 연필 질감: 획을 따라 잔 알갱이. 완전히 지우지는 않도록 0.45까지만 깎는다.
                float grain = lerp(0.45, 1.0, Hash21(float2(ang * _GrainScale, d * _GrainScale)));
                float ink   = lerp(1.0, grain, _Grain);

                half4 col = _Color * IN.color;
                col.a *= cover * ink;

                clip(col.a - 0.003);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

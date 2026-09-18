// 다각형 에너지 필드 벽면.
// UV 규약 : u = 둘레(0~1), v = 0 바닥 → 1 꼭대기
// 노이즈 두 겹을 각각 다른 각도로 회전시키면 같은 텍스처로도 반복 티가 사라진다.
Shader "KDY/PolygonField"
{
    Properties
    {
        [HDR] _BaseColor      ("Base Color (HDR)", Color) = (0.12, 1.0, 0.35, 1)
        [HDR] _RimColor       ("Rim Color (HDR)",  Color) = (0.65, 1.0, 0.75, 1)

        _NoiseTex             ("Noise (R 채널 사용)", 2D) = "white" {}
        _NoiseTiling          ("Noise Tiling (xy=1층, zw=2층)", Vector) = (1, 1, 2, 3)
        _ScrollA              ("Scroll 1층 (xy)", Vector) = (0.03, 0.25, 0, 0)
        _ScrollB              ("Scroll 2층 (xy)", Vector) = (-0.02, -0.14, 0, 0)
        _NoiseContrast        ("Noise Contrast", Range(0.5, 6)) = 2.0

        [Header(UV Rotation)]
        _RotationPivot        ("회전 중심 (기본 0.5, 0.5)", Vector) = (0.5, 0.5, 0, 0)
        _RotA                 ("1층 회전 (도)", Range(-360, 360)) = 0
        _RotSpeedA            ("1층 회전 속도 (도/초)", Range(-180, 180)) = 0
        _RotB                 ("2층 회전 (도)", Range(-360, 360)) = 90
        _RotSpeedB            ("2층 회전 속도 (도/초)", Range(-180, 180)) = 0

        [Header(Shape)]
        _FresnelPower         ("Fresnel Power", Range(0.2, 8)) = 2.5
        _RimIntensity         ("Rim Intensity", Range(0, 6)) = 1.6
        _TopFade              ("Top Fade Power", Range(0.1, 8)) = 1.8
        _BottomBoost          ("Bottom Boost", Range(0, 4)) = 1.2
        _Intensity            ("Overall Intensity", Range(0, 8)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Transparent"
            "Queue"            = "Transparent"
            "RenderPipeline"   = "UniversalPipeline"
            "IgnoreProjector"  = "True"
        }

        Pass
        {
            Name "PolygonFieldUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One          // Additive
            ZWrite Off
            Cull Off               // 안쪽 면도 보이게
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewWS     : TEXCOORD2;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float4 _NoiseTex_ST;
                float4 _NoiseTiling;
                float4 _ScrollA;
                float4 _ScrollB;
                float4 _RotationPivot;
                float  _RotA;
                float  _RotSpeedA;
                float  _RotB;
                float  _RotSpeedB;
                float  _NoiseContrast;
                float  _FresnelPower;
                float  _RimIntensity;
                float  _TopFade;
                float  _BottomBoost;
                float  _Intensity;
            CBUFFER_END

            float2 RotateUV(float2 uv, float degrees, float2 pivot)
            {
                float rad = radians(degrees);
                float s, c;
                sincos(rad, s, c);
                uv -= pivot;
                uv = float2(uv.x * c - uv.y * s,
                            uv.x * s + uv.y * c);
                return uv + pivot;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = p.positionCS;
                OUT.uv         = IN.uv;
                OUT.normalWS   = n.normalWS;
                OUT.viewWS     = GetWorldSpaceViewDir(p.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float  t     = _Time.y;
                float2 pivot = _RotationPivot.xy;

                // 두 겹을 서로 다른 각도 + 속도로 흘려 반복 티를 지운다
                float2 uvA = RotateUV(IN.uv, _RotA + _RotSpeedA * t, pivot);
                float2 uvB = RotateUV(IN.uv, _RotB + _RotSpeedB * t, pivot);

                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                             uvA * _NoiseTiling.xy + t * _ScrollA.xy).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                             uvB * _NoiseTiling.zw + t * _ScrollB.xy).r;
                float noise = saturate(pow(saturate(n1 * n2 * 2.0), _NoiseContrast));

                // 위로 갈수록 옅어짐 + 바닥은 진하게 (회전과 무관하게 원본 v 를 쓴다)
                float up     = saturate(1.0 - IN.uv.y);
                float vFade  = pow(up, _TopFade);
                float bottom = pow(up, 4.0) * _BottomBoost;

                // 양면 대응 프레넬
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewWS);
                float fres = pow(1.0 - saturate(abs(dot(N, V))), _FresnelPower);

                float3 col = _BaseColor.rgb * (noise * vFade + bottom)
                           + _RimColor.rgb  * (fres * _RimIntensity * vFade);

                return half4(col * _Intensity, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

Shader "Will/Particle/PixelExplosion"
{
    Properties
    {
        [Header(Color)]
        [HDR] _CoreColor ("Core Color", Color) = (4, 3.2, 0.8, 1)
        [HDR] _MidColor ("Middle Color", Color) = (3, 1.1, 0.05, 1)
        [HDR] _OuterColor ("Outer Color", Color) = (0.8, 0.08, 0.01, 1)

        [Header(Shape)]
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12
        _NoiseStrength ("Edge Noise", Range(0, 0.5)) = 0.16
        _NoiseScale ("Noise Scale", Range(1, 24)) = 7
        _NoiseSpeed ("Noise Speed", Range(-3, 3)) = 0.35
        [Toggle] _RingMode ("Ring Mode", Float) = 0
        _RingRadius ("Ring Radius", Range(0, 1.5)) = 0.72
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.14

        [Header(Pixel Dithering)]
        _DitherScale ("Screen Pixel Scale", Range(1, 12)) = 3
        _ColorSteps ("Color Steps", Range(2, 6)) = 4
        _DitherStrength ("Dither Strength", Range(0, 1)) = 1

        [Header(Render)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Dst", Float) = 1
        [Toggle] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend [_BlendSrc] [_BlendDst]
        Cull Off
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            half4 _CoreColor;
            half4 _MidColor;
            half4 _OuterColor;
            float _EdgeSoftness;
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            float _RingMode;
            float _RingRadius;
            float _RingWidth;
            float _DitherScale;
            float _ColorSteps;
            float _DitherStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1, 0));
                float c = Hash21(cell + float2(0, 1));
                float d = Hash21(cell + float2(1, 1));
                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            float Bayer2x2(float2 pixelPosition)
            {
                float2 p = fmod(floor(pixelPosition), 2.0);
                return 3.0 * p.y + 2.0 * p.x - 4.0 * p.x * p.y;
            }

            float Bayer4x4(float2 pixelPosition)
            {
                float2 p = fmod(floor(pixelPosition), 4.0);
                float lowBits = Bayer2x2(fmod(p, 2.0));
                float highBits = Bayer2x2(floor(p / 2.0));
                return (4.0 * lowBits + highBits + 0.5) / 16.0;
            }

            half3 ExplosionGradient(float value)
            {
                half3 inner = lerp(_CoreColor.rgb, _MidColor.rgb, saturate(value * 2.0));
                half3 outer = lerp(_MidColor.rgb, _OuterColor.rgb, saturate((value - 0.5) * 2.0));
                return lerp(inner, outer, step(0.5, value));
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float radius = length(centeredUV);
                float animatedNoise = ValueNoise(
                    centeredUV * _NoiseScale + float2(_Time.y * _NoiseSpeed, -_Time.y * _NoiseSpeed * 0.73)
                );
                float noisyRadius = radius + (animatedNoise - 0.5) * _NoiseStrength;

                float filledAlpha = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0 + _EdgeSoftness, noisyRadius);
                float ringDistance = abs(noisyRadius - _RingRadius);
                float ringAlpha = 1.0 - smoothstep(_RingWidth * 0.35, _RingWidth, ringDistance);
                float shapeAlpha = lerp(filledAlpha, ringAlpha, step(0.5, _RingMode));

                float colorPosition = saturate(noisyRadius) * max(_ColorSteps - 1.0, 1.0);
                float lowerStep = floor(colorPosition);
                float screenThreshold = Bayer4x4(i.vertex.xy / max(_DitherScale, 1.0));
                float ditheredStep = lowerStep + step(screenThreshold, frac(colorPosition));
                float steppedRadius = ditheredStep / max(_ColorSteps - 1.0, 1.0);
                float gradientPosition = lerp(saturate(noisyRadius), steppedRadius, saturate(_DitherStrength));

                half3 color = ExplosionGradient(gradientPosition) * i.color.rgb;
                half alpha = shapeAlpha * i.color.a;
                half4 result = half4(color, alpha);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, result, half4(0, 0, 0, 0));
                return result;
            }
            ENDCG
        }
    }
}

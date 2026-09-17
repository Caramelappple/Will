Shader "DLJ/Shark Water Splash"
{
    Properties
    {
        _Mode ("0 Droplet, 1 Pool, 2 Crown, 3 Ripples", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        _Age ("Normalized Age", Range(0,1)) = 0
        _Tint ("Water Tint", Color) = (0.14, 0.78, 0.88, 0.85)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float _Opacity;
                float _Age;
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 foam = half3(0.7, 1.0, 0.94);
                if (_Mode > 1.5 && _Mode < 2.5)
                {
                    float u = input.uv.x;
                    float v = input.uv.y;
                    float flow = sin(u * 150.796 - v * 7.0 + _Age * 9.0);
                    float veins = pow(saturate(flow), 10.0);
                    float lip = smoothstep(0.84, 1.0, v);
                    float breaks = sin(u * 106.814 + v * 11.0) * sin(u * 62.832 - v * 9.0);
                    float holes = 1.0 - smoothstep(0.48, 0.7, breaks) * smoothstep(0.32, 0.8, v);
                    half3 body = lerp(_Tint.rgb * half3(0.28, 0.68, 0.87), _Tint.rgb, v);
                    half3 color = lerp(body, foam, saturate(lip * 0.9 + veins * 0.5));
                    float alpha = (0.48 + lip * 0.4 + veins * 0.18) * holes
                        * smoothstep(0.0, 0.12, v) * _Opacity * _Tint.a;
                    return half4(color, alpha);
                }

                float2 p = input.uv * 2.0 - 1.0;
                if (_Mode > 0.5)
                {
                    float r = length(p);
                    float angle = atan2(p.y, p.x);
                    float distortion = 0.02 * sin(angle * 9.0 + _Age * 5.0)
                        + 0.015 * sin(angle * 17.0 - _Age * 3.0);
                    float radius = r + distortion;
                    float edge = 1.0 - smoothstep(0.035, 0.065, abs(radius - 0.88));
                    float inner = (1.0 - smoothstep(0.015, 0.04, abs(radius - 0.66))) * 0.4;
                    float wisps = 0.65 + 0.35 * sin(angle * 11.0 + radius * 18.0 - _Age * 8.0);
                    float disk = 1.0 - smoothstep(0.78, 0.92, radius);
                    float ripples = pow(saturate(sin(radius * 28.0 - _Age * 14.0 + sin(angle * 5.0))), 12.0);
                    if (_Mode > 2.5)
                        return half4(lerp(_Tint.rgb, foam, 0.7), (edge + inner) * wisps * _Opacity * _Tint.a * 0.75);
                    half3 deep = _Tint.rgb * half3(0.22, 0.46, 0.6);
                    half3 pool = lerp(deep, _Tint.rgb, saturate(radius * 0.85 + ripples * 0.2));
                    pool = lerp(pool, foam, saturate(edge * wisps * 0.75));
                    return half4(pool, (disk * 0.52 + edge * 0.4) * _Opacity * _Tint.a);
                }

                // 길쭉한 물방울 실루엣. 둥근 구형 광택은 사용하지 않음.
                float width = lerp(0.62, 0.2, saturate(p.y * 0.5 + 0.5));
                float droplet = length(float2(p.x / width, p.y));
                float mask = 1.0 - smoothstep(0.78, 1.0, droplet);
                half3 dropColor = lerp(input.color.rgb, foam, smoothstep(0.25, 0.8, p.y) * 0.3);
                return half4(dropColor, input.color.a * mask);
            }
            ENDHLSL
        }
    }
}

Shader "DLJ/VFX/Corvo King Crimson"
{
    Properties
    {
        [HDR] _Tint ("Crimson", Color) = (1.8, 0.015, 0.055, 0.65)
        _Strength ("Strength", Float) = 0
        _Expansion ("Shell Expansion", Float) = 0
        [HideInInspector] _EyeMode ("Eye Mode", Float) = 0
        [HideInInspector] _Clock ("Clock", Float) = 0
        [HideInInspector] _BoundsMin ("Bounds Min", Vector) = (0,0,0,0)
        [HideInInspector] _BoundsSize ("Bounds Size", Vector) = (1,1,1,0)
        [HideInInspector] _EyeCenter ("Eye Center", Vector) = (0.59,0.5,0.83,0)
        [HideInInspector] _EyeRadius ("Eye Radius", Vector) = (0.055,0.12,0.028,0)
        [HideInInspector] _EyeSeparation ("Eye Separation", Float) = 0.29
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _Cull ("Cull", Float) = 2
        [HideInInspector] _GlowSize ("Glow Size", Float) = 0.16
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Name "CrimsonOverlay"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Strength, _Expansion, _EyeMode, _Clock;
                float4 _BoundsMin, _BoundsSize, _EyeCenter, _EyeRadius;
                float _EyeSeparation;
                float _GlowSize;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 expanded = input.positionOS.xyz + input.normalOS * _Expansion;
                output.positionCS = TransformObjectToHClip(expanded);
                output.positionWS = TransformObjectToWorld(expanded);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                if (_EyeMode > 1.5)
                {
                    float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                    float3 rightWS = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 upWS = UNITY_MATRIX_I_V._m01_m11_m21;
                    float2 q = input.uv - 0.5;
                    output.positionWS = centerWS + (rightWS * q.x + upWS * q.y) * _GlowSize;
                    output.positionCS = TransformWorldToHClip(output.positionWS);
                }
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float mask;
                if (_EyeMode > 1.5)
                {
                    // 눈 중심에서 모든 방향으로 부드럽게 번지는 원형 발광.
                    float2 q = input.uv * 2.0 - 1.0;
                    float radiusSquared = dot(q, q);
                    float core = exp(-radiusSquared * 32.0);
                    float halo = exp(-radiusSquared * 3.0) * 0.65;
                    float edge = 1.0 - smoothstep(0.45, 1.0, radiusSquared);
                    half alpha = saturate((core + halo) * edge * _Strength * _Tint.a);
                    clip(alpha - 0.002);
                    return half4(_Tint.rgb + half3(0.3, 0.08, 0.035) * core, alpha);
                }
                if (_EyeMode > 0.5)
                {
                    float3 p = (input.positionOS - _BoundsMin.xyz) / max(_BoundsSize.xyz, 0.000001);
                    float3 delta = p - _EyeCenter.xyz;
                    delta.y = abs(delta.y) - _EyeSeparation;
                    float distanceToEye = length(delta / max(_EyeRadius.xyz, 0.0001));
                    mask = 1.0 - smoothstep(0.35, 1.0, distanceToEye);
                }
                else
                {
                    float rim = pow(1.0 - saturate(abs(dot(normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS)))), 1.5);
                    float smoke = sin(dot(input.positionWS, float3(11, 7, 13)) - _Clock * 10) * 0.5 + 0.5;
                    mask = lerp(0.38, 1.0, rim) * lerp(0.78, 1.0, smoke);
                }
                half alpha = saturate(mask * _Strength * _Tint.a);
                clip(alpha - 0.001);
                return half4(_Tint.rgb, alpha);
            }
            ENDHLSL
        }
    }
}

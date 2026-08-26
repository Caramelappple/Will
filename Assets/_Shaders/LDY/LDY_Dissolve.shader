Shader "LDY/Dissolve"
{
    Properties
    {
        [MainTexture] _BaseMap    ("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor  ("Base Color", Color) = (1,1,1,1)
        _Metallic   ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _BumpMap    ("Normal Map", 2D) = "bump" {}
        _BumpScale  ("Normal Scale", Float) = 1

        [Space(10)][Header(Dissolve)][Space(4)]
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale     ("Noise Scale", Float) = 8
        _EdgeWidth      ("Edge Width", Range(0.001,0.3)) = 0.06
        [HDR] _EdgeColor("Edge Color", Color) = (3.0, 1.0, 0.2, 1)
        _DirBias        ("Direction Bias (XYZ=dir, W=strength)", Vector) = (0,1,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _Metallic;
            float  _Smoothness;
            float  _BumpScale;
            float  _DissolveAmount;
            float  _NoiseScale;
            float  _EdgeWidth;
            float4 _EdgeColor;
            float4 _DirBias;
        CBUFFER_END

        // ---- 3D value noise (노이즈 텍스처 불필요, UV 이음매 없음) ----
        float LDY_Hash31(float3 p)
        {
            p  = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float LDY_ValueNoise(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);

            float n000 = LDY_Hash31(i + float3(0,0,0));
            float n100 = LDY_Hash31(i + float3(1,0,0));
            float n010 = LDY_Hash31(i + float3(0,1,0));
            float n110 = LDY_Hash31(i + float3(1,1,0));
            float n001 = LDY_Hash31(i + float3(0,0,1));
            float n101 = LDY_Hash31(i + float3(1,0,1));
            float n011 = LDY_Hash31(i + float3(0,1,1));
            float n111 = LDY_Hash31(i + float3(1,1,1));

            return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                        lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
        }

        float LDY_Fbm(float3 p)
        {
            float v = 0.0, a = 0.5;
            [unroll]
            for (int i = 0; i < 3; i++)
            {
                v += a * LDY_ValueNoise(p);
                p *= 2.0;
                a *= 0.5;
            }
            return v / 0.875;
        }

        // 디졸브 판정값. 반환값이 cutoff 보다 작으면 잘려나간다.
        float LDY_DissolveValue(float3 positionOS)
        {
            float n = LDY_Fbm(positionOS * _NoiseScale);

            // 방향 바이어스: 아래에서 위로 타오르듯 지우고 싶을 때 사용
            float3 dir  = normalize(_DirBias.xyz + float3(0, 1e-5, 0));
            float  bias = saturate(dot(dir, positionOS) * 0.5 + 0.5);

            return lerp(n, n * (1.0 - _DirBias.w) + bias * _DirBias.w, saturate(_DirBias.w));
        }

        float LDY_Cutoff()
        {
            return _DissolveAmount * (1.0 + _EdgeWidth);
        }
        ENDHLSL

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float  fogCoord   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS   = nrm.normalWS;
                OUT.tangentWS  = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float value  = LDY_DissolveValue(IN.positionOS);
                float cutoff = LDY_Cutoff();
                clip(value - cutoff);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                float sgn = IN.tangentWS.w;
                float3 bitangent = sgn * cross(IN.normalWS, IN.tangentWS.xyz);
                float3x3 tbn = float3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);

                // 잘리는 경계에서만 빛나는 테두리
                float edge = 1.0 - smoothstep(cutoff, cutoff + _EdgeWidth, value);
                edge *= step(0.0001, _DissolveAmount);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = albedo.rgb;
                surface.metallic   = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS   = normalTS;
                surface.emission   = _EdgeColor.rgb * edge;
                surface.occlusion  = 1.0;
                surface.alpha      = 1.0;

                InputData input = (InputData)0;
                input.positionWS          = IN.positionWS;
                input.normalWS            = NormalizeNormalPerPixel(mul(normalTS, tbn));
                input.viewDirectionWS     = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                input.shadowCoord         = TransformWorldToShadowCoord(IN.positionWS);
                input.fogCoord            = IN.fogCoord;
                input.bakedGI             = SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, IN.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct SAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            SVaryings ShadowVert(SAttributes IN)
            {
                SVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float4 clip = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    clip.z = min(clip.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z = max(clip.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = clip;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 ShadowFrag(SVaryings IN) : SV_Target
            {
                clip(LDY_DissolveValue(IN.positionOS) - LDY_Cutoff());
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            DVaryings DepthVert(DAttributes IN)
            {
                DVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 DepthFrag(DVaryings IN) : SV_Target
            {
                clip(LDY_DissolveValue(IN.positionOS) - LDY_Cutoff());
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

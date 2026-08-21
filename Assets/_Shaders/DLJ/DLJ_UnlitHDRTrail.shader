Shader "DLJ/VFX/Unlit HDR Trail"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0, 0.925, 1, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 4, 4, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UnlitTrail"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 조명과 표면색을 합산하지 않고 HDR 발광색만 직접 출력한다.
                half3 color = _EmissionColor.rgb * input.color.rgb;
                half alpha = _BaseColor.a * input.color.a;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

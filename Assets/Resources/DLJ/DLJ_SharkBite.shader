Shader "DLJ/Shark Bite"
{
    Properties
    {
        _Tint ("Teeth Color", Color) = (0.78, 0.95, 1, 0.62)
        _Opacity ("Opacity", Range(0,1)) = 1
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
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb, input.color.a * _Opacity);
            }
            ENDHLSL
        }
    }
}

// UI 전용 픽셀화 셰이더. Unity 내장 UI/Default를 복사한 뒤 프래그먼트에서 UV만 블록 단위로 스냅한다.
//
// UI/Default를 베이스로 삼는 이유:
//   - _Stencil* 프로퍼티가 있어야 Mask(스텐실 마스킹)가 동작한다.
//   - UNITY_UI_CLIP_RECT / _ClipRect / _UIMaskSoftness* 가 있어야 RectMask2D가 동작한다.
//   둘 다 CanvasRenderer가 "프로퍼티 이름"으로 찾아 꽂는 규약이라, 새로 짜면 통째로 깨진다.
//
// 스냅은 UV가 아니라 "스크린 픽셀" 기준이다. 그래야 서로 다른 UI 요소끼리 격자가 어긋나지 않고
// 화면 전역으로 한 장의 격자처럼 이어진다.
//
// TMP는 자기 SDF 머티리얼을 그대로 쓰므로 이 셰이더가 닿지 않는다 = 텍스트는 항상 선명하다.

Shader "LDY/UI/Pixelate"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // _Block(블록 한 변의 길이, 스크린 픽셀)은 일부러 Properties에 두지 않는다.
        // Mask(스텐실) 밑의 그래픽은 UGUI가 StencilMaterial.Add로 머티리얼 "사본"을 만들어 쓰기 때문에,
        // 머티리얼 프로퍼티로 두면 사본이 굳어져서 런타임에 값을 바꿔도 반영되지 않는다.
        // 전역(Shader.SetGlobalFloat)으로 두면 사본까지 한 번에 따라온다. → LDY_UIPixelizer가 세팅.
        // 아무도 세팅하지 않으면 0 → max(_Block,1) = 1 → 원본 그대로(픽셀화 없음).

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // ddx/ddy와 tex2Dgrad를 쓰므로 UI/Default의 2.0에서 3.0으로 올린다.
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4  mask          : TEXCOORD2;
                float4 screenPos     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            // Properties에 없는 전역 값 (윗쪽 주석 참고)
            float _Block;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;
                OUT.screenPos = ComputeScreenPos(vPosition);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1) 이 프래그먼트가 화면 어느 픽셀인지 (렌더타겟 픽셀 좌표)
                float2 px = IN.screenPos.xy / max(IN.screenPos.w, 1e-6) * _ScreenParams.xy;

                // 2) 블록 중심으로 스냅. 스크린 기준이라 인접 UI끼리 격자가 이어진다.
                float block = max(_Block, 1.0);
                float2 snapped = (floor(px / block) + 0.5) * block;

                // 3) 스크린 이동량을 UV 이동량으로 환산.
                //    ddx/ddy를 야코비안 전체로 쓰면 RectTransform이 회전/스큐돼 있어도 성립한다.
                float2 dUVdx = ddx(IN.texcoord);
                float2 dUVdy = ddy(IN.texcoord);
                float2 delta = snapped - px;
                float2 uv = IN.texcoord + dUVdx * delta.x + dUVdy * delta.y;

                // 스냅된 UV로 미분값을 다시 뽑으면 블록 경계에서 튀므로 원본 미분을 그대로 넘긴다.
                half4 color = IN.color * (tex2Dgrad(_MainTex, uv, dUVdx, dUVdy) + _TextureSampleAdd);

                // 마스킹은 UV가 아니라 정점 위치 기반이라 스냅과 무관하게 정확히 남는다.
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}

Shader "DLJ/Ledger Ink"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Ink", Color) = (1,1,1,1)
        _InkProgress ("Ink Spread", Range(0,1)) = 1
        _GradientScale ("Gradient Scale", Float) = 4
        _TextureWidth ("Texture Width", Float) = 4096
        _TextureHeight ("Texture Height", Float) = 2048
        _WeightNormal ("Normal Weight", Float) = 0
        _WeightBold ("Bold Weight", Float) = .5
        _FaceDilate ("Face Dilate", Float) = 0
        _OutlineWidth ("Outline Width", Float) = 0
        // TMP reads this unconditionally when calculating SDF padding and scale ratios.
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _ScaleRatioA ("Scale Ratio A", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _FaceColor;
            float _InkProgress;
            struct Vertex { float4 position : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Pixel { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float2 paper : TEXCOORD1; float4 color : COLOR; };
            Pixel vert(Vertex v)
            {
                Pixel o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv; o.paper = v.position.xy; o.color = v.color * _FaceColor;
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float4 frag(Pixel i) : SV_Target
            {
                float p = saturate(_InkProgress);
                float sdf = tex2D(_MainTex, i.uv).a;
                float wet = sin(p * 3.14159265);
                float grain = noise(i.paper * 72) * .65 + noise(i.paper * 173) * .35;
                // Pigment starts in stroke cores, feathers unevenly outward, then dries to a crisp edge.
                float edge = lerp(.68, .5, smoothstep(0,.8,p)) + (grain-.5)*wet*.1;
                float aa = max(fwidth(sdf), .007);
                float ink = smoothstep(edge-aa-wet*.035, edge+aa+wet*.035, sdf);
                float bleed = smoothstep(edge-.14-aa,edge+.02+aa,sdf) * wet * .24;
                float alpha = max(ink,bleed) * smoothstep(0,.08,p) * i.color.a;
                return float4(i.color.rgb * (1-wet*.22), alpha);
            }
            ENDCG
        }
    }
}

Shader "DLJ/Ledger Rule"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (.105,.055,.025,1)
        _DrawProgress ("Draw Progress", Range(0,1)) = 1
        _MinX ("Left Edge", Float) = .2
        _MaxX ("Right Edge", Float) = 1.86
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
            float4 _LineColor;
            float _DrawProgress, _MinX, _MaxX;
            struct Pixel { float4 position : SV_POSITION; float across : TEXCOORD0; };
            Pixel vert(float4 position : POSITION)
            {
                Pixel o;
                o.position = UnityObjectToClipPos(position);
                o.across = (position.x - _MinX) / max(.0001, _MaxX - _MinX);
                return o;
            }
            float4 frag(Pixel i) : SV_Target
            {
                clip(_DrawProgress - .00001);
                float tip = max(fwidth(i.across), .001);
                float alpha = _DrawProgress >= 1 ? 1 : 1 - smoothstep(_DrawProgress - tip, _DrawProgress, i.across);
                return float4(_LineColor.rgb, _LineColor.a * alpha);
            }
            ENDCG
        }
    }
}

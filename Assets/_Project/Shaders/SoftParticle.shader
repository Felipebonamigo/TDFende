// Partícula macia sem textura: a borda redonda e esfumada sai da própria UV do quad.
// Fumaça e poeira usam mistura alfa; clarão e faísca usam aditiva (_SrcBlend/_DstBlend
// vêm do código). Sem tag LightMode, como o overlay da fronteira: roda igual em URP e Built-in.
Shader "TDFende/SoftParticle"
{
    Properties
    {
        _SrcBlend("Src Blend", Float) = 5   // SrcAlpha
        _DstBlend("Dst Blend", Float) = 10  // OneMinusSrcAlpha
        _Softness("Softness", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv * 2.0 - 1.0;
                float a = saturate(1.0 - dot(d, d));
                a = pow(a, _Softness);
                return fixed4(i.color.rgb, i.color.a * a);
            }
            ENDCG
        }
    }
}

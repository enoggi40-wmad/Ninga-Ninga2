Shader "Unlit/Enemy_VisionCone_Gradation"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,0.25)
        _Power ("Fade Power", Range(0.2, 6)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Power;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fade = pow(saturate(1.0 - i.uv.x), _Power); // edge(1) -> 0
                fixed4 c = _Color;
                c.a *= fade;
                return c;
            }
            ENDCG
        }
    }
}

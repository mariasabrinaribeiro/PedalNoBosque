Shader "GirlPower/OrlaBlobShadow"
{
    Properties
    {
        _Color ("Shadow Tint", Color) = (0.15, 0.11, 0.24, 0.42)
        _Softness ("Edge Softness", Range(0.1, 1.0)) = 0.65
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
    }

    SubShader
    {
        Tags { "Queue"="Transparent-50" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Softness;
            float _CurveStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                if (_CurveStrength > 0.00001)
                {
                    float dz = worldPos.z - _WorldSpaceCameraPos.z;
                    float dx = worldPos.x - _WorldSpaceCameraPos.x;
                    worldPos.y -= (dz * dz + dx * dx * 0.12) * _CurveStrength;
                }
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv * 2.0 - 1.0;
                float d = length(centered);
                float inner = saturate(1.0 - _Softness);
                float alpha = 1.0 - smoothstep(inner, 1.0, d);
                alpha = pow(alpha, 1.45) * _Color.a;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}

Shader "GirlPower/OrlaWaterRipple"
{
    Properties
    {
        _MainTex ("Ring Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.94, 0.99, 1.0, 0.85)
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
    }

    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
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
                fixed4 tex = tex2D(_MainTex, i.uv);
                return fixed4(tex.rgb * _Color.rgb, tex.a * _Color.a);
            }
            ENDCG
        }
    }
}

Shader "Hidden/GirlPower/OrlaPostTone"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure", Range(0.5, 1.6)) = 0.92
        _Contrast ("Contrast", Range(0.8, 1.35)) = 1.08
        _Saturation ("Saturation", Range(0.6, 1.4)) = 1.12
        _VignetteStrength ("Vignette Strength", Range(0.0, 0.5)) = 0.18
        _WarmTint ("Warm Highlight Tint", Color) = (1.02, 0.99, 0.95, 1)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Exposure;
            half _Contrast;
            half _Saturation;
            half _VignetteStrength;
            fixed4 _WarmTint;

            // Soft ACES filmic curve eliminates highlight blow-out while preserving pastel vibrancy
            float3 ACESFilmic(float3 x)
            {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // Controlled exposure + ACES filmic tone compression
                col *= _Exposure;
                col = ACESFilmic(col * 1.12);

                // Subtle contrast & cozy saturation boost
                col = saturate((col - 0.5) * _Contrast + 0.5);
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(float3(luma, luma, luma), col, _Saturation);
                col *= _WarmTint.rgb;

                // Gentle framing vignette to focus attention on the promenade & characters
                float2 uvC = i.uv * 2.0 - 1.0;
                float vig = 1.0 - dot(uvC, uvC) * _VignetteStrength;
                col *= saturate(vig);

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
}

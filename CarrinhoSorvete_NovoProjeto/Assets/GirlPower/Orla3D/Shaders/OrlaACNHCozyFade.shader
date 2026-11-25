Shader "GirlPower/OrlaACNHCozyFade"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ShadowColor ("Cozy Shadow Tint", Color) = (0.38, 0.34, 0.52, 1)
        _RimColor ("Sun Rim Color", Color) = (1.0, 0.90, 0.72, 1)
        _RimPower ("Rim Sharpness", Range(1.0, 8.0)) = 4.2
        _RimStrength ("Rim Strength", Range(0.0, 1.0)) = 0.14
        _WrapAmount ("Soft Wrap Diffuse", Range(0.0, 1.0)) = 0.28
        _Glossiness ("Softness Specular", Range(0.0, 1.0)) = 0.22
        _GroundAO ("Ground Contact AO", Range(0.0, 1.0)) = 0.34
        _GroundAOHeight ("Ground AO Height", Range(0.1, 2.5)) = 0.72
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
        _OccluderEnabled ("Smooth House Occlusion", Range(0.0, 1.0)) = 1.0
        _OccluderRadius ("Fade Radius", Range(0.5, 2.5)) = 1.15
        _FadeStartHeight ("Fade Only Above Roofline", Range(0.5, 4.0)) = 1.65
        _UseWorldUV ("World Space Tiling", Range(0.0, 1.0)) = 0.0
        _TileScale ("World Tile Scale", Range(0.05, 4.0)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf CozyACNH fullforwardshadows vertex:vert addshadow noambient alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _ShadowColor;
        fixed4 _RimColor;
        half _RimPower;
        half _RimStrength;
        half _WrapAmount;
        half _Glossiness;
        half _GroundAO;
        half _GroundAOHeight;
        fixed4 _EmissionColor;
        float _CurveStrength;
        half _OccluderEnabled;
        half _OccluderRadius;
        half _FadeStartHeight;
        half _UseWorldUV;
        float _TileScale;
        float4 _BaiaPlayerPos;
        float4 _BaiaCamPos;
        float _BaiaOccluderActive;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        void vert(inout appdata_full v)
        {
            if (_CurveStrength > 0.00001)
            {
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float dz = worldPos.z - _WorldSpaceCameraPos.z;
                float dx = worldPos.x - _WorldSpaceCameraPos.x;
                float distSq = dz * dz + dx * dx * 0.12;
                worldPos.y -= distSq * _CurveStrength;
                v.vertex = mul(unity_WorldToObject, worldPos);
            }
        }

        half4 LightingCozyACNH(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half wrap = saturate((NdotL + _WrapAmount) / (1.0 + _WrapAmount));
            half lit = smoothstep(0.08, 0.74, wrap * atten);
            half hemi = saturate(s.Normal.y * 0.5 + 0.5);
            half3 ambientTint = lerp(UNITY_LIGHTMODEL_AMBIENT.rgb * 0.72, unity_AmbientSky.rgb * 0.62, hemi);
            half3 shadowCol = s.Albedo * lerp(_ShadowColor.rgb, _ShadowColor.rgb * 0.75 + ambientTint * 0.45, 0.55);
            half3 litCol = s.Albedo * (_LightColor0.rgb * 0.94 + ambientTint * 0.22);
            half3 finalDiffuse = lerp(shadowCol, litCol, lit);
            half3 halfVec = normalize(lightDir + viewDir);
            half NdotH = saturate(dot(s.Normal, halfVec));
            half spec = pow(NdotH, 32.0) * _Glossiness * lit * 0.26;
            return half4(finalDiffuse + _LightColor0.rgb * spec, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float alphaFade = 1.0;
            if (_OccluderEnabled > 0.5 && _BaiaOccluderActive > 0.5 && IN.worldPos.y > _FadeStartHeight)
            {
                float3 camToPlayer = _BaiaPlayerPos.xyz - _BaiaCamPos.xyz;
                float camToPlayerLen = max(0.01, length(camToPlayer));
                float3 rayDir = camToPlayer / camToPlayerLen;
                float proj = dot(IN.worldPos - _BaiaCamPos.xyz, rayDir);

                if (proj > 1.5 && proj < camToPlayerLen - 0.45)
                {
                    float3 closest = _BaiaCamPos.xyz + rayDir * proj;
                    float distToRay = length(IN.worldPos - closest);
                    alphaFade = smoothstep(_OccluderRadius * 0.18, _OccluderRadius, distToRay);
                }
            }

            float2 sampleUV = (_UseWorldUV > 0.5)
                ? (IN.worldPos.xz * _TileScale + float2(IN.worldPos.y * _TileScale * 0.5, 0.0))
                : IN.uv_MainTex;
            fixed4 c = tex2D(_MainTex, sampleUV) * _Color;

            float aoH = saturate((IN.worldPos.y + 0.16) / max(0.1, _GroundAOHeight));
            float groundAO = lerp(1.0 - _GroundAO * 0.55, 1.0, smoothstep(0.0, 1.0, aoH));
            float grain = sin(IN.worldPos.x * 3.7 + IN.worldPos.z * 2.9) * 0.018;
            o.Albedo = saturate(c.rgb * groundAO + grain);
            o.Alpha = c.a * alphaFade;

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            half3 rimEmit = _RimColor.rgb * pow(rim, _RimPower) * _RimStrength * groundAO;
            o.Emission = _EmissionColor.rgb + rimEmit;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}

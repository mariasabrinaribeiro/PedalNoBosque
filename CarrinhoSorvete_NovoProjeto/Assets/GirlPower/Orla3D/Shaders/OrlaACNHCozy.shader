Shader "GirlPower/OrlaACNHCozy"
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
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0016
        _OccluderEnabled ("Camera Occlusion Dither", Range(0.0, 1.0)) = 0.0
        _OccluderRadius ("Occluder Cutout Radius", Range(0.8, 4.5)) = 2.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf CozyACNH fullforwardshadows vertex:vert addshadow noambient
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

        // Updated globally by GirlPowerOrla3DOcclusionFader
        float4 _BaiaPlayerPos;
        float4 _BaiaCamPos;
        float _BaiaOccluderActive;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
            float4 screenPos;
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

        // Cross-platform 4x4 Bayer dither (100% compatible with DX11, WebGL & Android GLES)
        float BayerDither4x4(float2 pixelPos)
        {
            float2 p = floor(fmod(abs(pixelPos), 4.0));
            float4x4 bayer = float4x4(
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            );
            float4 row = (p.y < 0.5) ? bayer[0] : ((p.y < 1.5) ? bayer[1] : ((p.y < 2.5) ? bayer[2] : bayer[3]));
            return (p.x < 0.5) ? row.x : ((p.x < 1.5) ? row.y : ((p.x < 2.5) ? row.z : row.w));
        }

        half4 LightingCozyACNH(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half wrap = saturate((NdotL + _WrapAmount) / (1.0 + _WrapAmount));
            half lit = smoothstep(0.08, 0.74, wrap * atten);

            // Grounded hemisphere ambient (controlled so whites/pastels never blow out!)
            half hemi = saturate(s.Normal.y * 0.5 + 0.5);
            half3 ambientTint = lerp(UNITY_LIGHTMODEL_AMBIENT.rgb * 0.72, unity_AmbientSky.rgb * 0.62, hemi);
            half3 shadowCol = s.Albedo * lerp(_ShadowColor.rgb, _ShadowColor.rgb * 0.75 + ambientTint * 0.45, 0.55);
            half3 litCol = s.Albedo * (_LightColor0.rgb * 0.94 + ambientTint * 0.22);
            half3 finalDiffuse = lerp(shadowCol, litCol, lit);

            // Controlled velvety specular
            half3 halfVec = normalize(lightDir + viewDir);
            half NdotH = saturate(dot(s.Normal, halfVec));
            half spec = pow(NdotH, 32.0) * _Glossiness * lit * 0.26;

            half4 c;
            c.rgb = finalDiffuse + _LightColor0.rgb * spec;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Smooth roof-only screen-space fade when upper building sits between camera and player
            if (_OccluderEnabled > 0.5 && _BaiaOccluderActive > 0.5 && IN.worldPos.y > 1.35)
            {
                float3 camToPlayer = _BaiaPlayerPos.xyz - _BaiaCamPos.xyz;
                float camToPlayerLen = max(0.01, length(camToPlayer));
                float3 rayDir = camToPlayer / camToPlayerLen;

                float3 camToFrag = IN.worldPos - _BaiaCamPos.xyz;
                float proj = dot(camToFrag, rayDir);

                if (proj > 1.5 && proj < camToPlayerLen - 0.45)
                {
                    float3 closest = _BaiaCamPos.xyz + rayDir * proj;
                    float distToRay = length(IN.worldPos - closest);
                    if (distToRay < _OccluderRadius)
                    {
                        float fade = smoothstep(0.18, 1.0, saturate(distToRay / max(0.01, _OccluderRadius)));
                        float alphaKeep = lerp(0.30, 1.0, fade);
                        float2 screenUV = IN.screenPos.xy / max(0.0001, IN.screenPos.w);
                        float2 pixelPos = screenUV * _ScreenParams.xy;
                        clip(alphaKeep - BayerDither4x4(pixelPos));
                    }
                }
            }

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Ground-contact Ambient Occlusion: gently darkens bases where objects meet ground
            float aoH = saturate((IN.worldPos.y + 0.16) / max(0.1, _GroundAOHeight));
            float groundAO = lerp(1.0 - _GroundAO * 0.55, 1.0, smoothstep(0.0, 1.0, aoH));
            // Subtle micro-variation so large flat pastel surfaces feel tactile instead of plastic
            float grain = sin(IN.worldPos.x * 3.7 + IN.worldPos.z * 2.9) * 0.018;

            o.Albedo = saturate(c.rgb * groundAO + grain);
            o.Alpha = c.a;

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            half3 rimEmit = _RimColor.rgb * pow(rim, _RimPower) * _RimStrength * groundAO;
            o.Emission = _EmissionColor.rgb + rimEmit;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

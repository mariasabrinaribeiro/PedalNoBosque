Shader "GirlPower/OrlaACNHCozy"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ShadowColor ("Cozy Shadow Tint", Color) = (0.38, 0.34, 0.52, 1)
        _RimColor ("Sun Rim Color", Color) = (1.0, 0.90, 0.72, 1)
        _RimPower ("Rim Sharpness", Range(1.0, 8.0)) = 4.2
        _RimStrength ("Rim Strength", Range(0.0, 1.0)) = 0.06
        _WrapAmount ("Soft Wrap Diffuse", Range(0.0, 1.0)) = 0.28
        _Glossiness ("Softness Specular", Range(0.0, 1.0)) = 0.10
        _GroundAO ("Ground Contact AO", Range(0.0, 1.0)) = 0.34
        _GroundAOHeight ("Ground AO Height", Range(0.1, 2.5)) = 0.72
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0016
        _UseWorldUV ("World Space Tiling", Range(0.0, 1.0)) = 0.0
        _TileScale ("World Tile Scale", Range(0.05, 4.0)) = 0.35
        _TexStrength ("Texture Contrast", Range(0.0, 1.0)) = 0.32
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
        half _UseWorldUV;
        float _TileScale;
        half _TexStrength;

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

            // Grounded hemisphere ambient (controlled so whites/pastels never blow out!)
            half hemi = saturate(s.Normal.y * 0.5 + 0.5);
            half3 ambientTint = lerp(UNITY_LIGHTMODEL_AMBIENT.rgb * 0.72, unity_AmbientSky.rgb * 0.62, hemi);
            half3 shadowCol = s.Albedo * lerp(_ShadowColor.rgb, _ShadowColor.rgb * 0.75 + ambientTint * 0.45, 0.55);
            half3 litCol = s.Albedo * (_LightColor0.rgb * 0.94 + ambientTint * 0.22);
            half3 finalDiffuse = lerp(shadowCol, litCol, lit);

            // Controlled matte-velvety specular (no shiny plastic glare on wood/fabric)
            half3 halfVec = normalize(lightDir + viewDir);
            half NdotH = saturate(dot(s.Normal, halfVec));
            half spec = pow(NdotH, 36.0) * _Glossiness * lit * 0.14;

            half4 c;
            c.rgb = finalDiffuse + _LightColor0.rgb * spec;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Pure XZ world tiling (zero Y-shift seams across sand/boardwalk/plaza slopes!)
            float2 sampleUV = (_UseWorldUV > 0.5)
                ? (IN.worldPos.xz * _TileScale)
                : IN.uv_MainTex;
            fixed4 texCol = tex2D(_MainTex, sampleUV);
            fixed3 blendedTex = lerp(fixed3(1.0, 1.0, 1.0), texCol.rgb, _TexStrength);
            fixed4 c = fixed4(blendedTex * _Color.rgb, _Color.a);

            // Ground-contact Ambient Occlusion: gently darkens bases where objects meet ground
            float aoH = saturate((IN.worldPos.y + 0.16) / max(0.1, _GroundAOHeight));
            float groundAO = lerp(1.0 - _GroundAO * 0.55, 1.0, smoothstep(0.0, 1.0, aoH));

            // Clean, band-free matte surface (no periodic striping or tile seams!)
            o.Albedo = saturate(c.rgb * groundAO);
            o.Alpha = c.a;

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            half3 rimEmit = _RimColor.rgb * pow(rim, _RimPower) * _RimStrength * groundAO;
            o.Emission = _EmissionColor.rgb + rimEmit;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

Shader "GirlPower/OrlaACNHHairWind"
{
    Properties
    {
        _Color ("Hair Color", Color) = (0.45, 0.28, 0.18, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ShadowColor ("Cozy Shadow Tint", Color) = (0.38, 0.34, 0.52, 1)
        _RimColor ("Sun Rim Color", Color) = (1.0, 0.90, 0.72, 1)
        _RimPower ("Rim Sharpness", Range(1.0, 8.0)) = 4.0
        _RimStrength ("Rim Strength", Range(0.0, 1.0)) = 0.18
        _OutlineSoft ("Character Soft Outline", Range(0.0, 1.0)) = 0.68
        _WrapAmount ("Soft Wrap Diffuse", Range(0.0, 1.0)) = 0.28
        _Glossiness ("Hair Sheen", Range(0.0, 1.0)) = 0.36
        _GroundAO ("Ground Contact AO", Range(0.0, 1.0)) = 0.20
        _GroundAOHeight ("Ground AO Height", Range(0.1, 2.5)) = 0.72
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
        _HairWindSpeed ("Coastal Breeze Speed", Range(0.2, 6.0)) = 2.4
        _HairWindAmp ("Hair Tip Sway Amplitude", Range(0.0, 0.14)) = 0.045
        _HairTopY ("Hair Root Local Y", Range(0.5, 2.0)) = 1.28
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
        half _OutlineSoft;
        half _WrapAmount;
        half _Glossiness;
        half _GroundAO;
        half _GroundAOHeight;
        fixed4 _EmissionColor;
        float _CurveStrength;
        float _HairWindSpeed;
        float _HairWindAmp;
        float _HairTopY;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        void vert(inout appdata_full v)
        {
            // Zero-CPU-cost GPU coastal breeze sway applied ONLY to lower tips of long hair!
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float tipMask = saturate((_HairTopY - v.vertex.y) * 1.45);
            tipMask = tipMask * tipMask; // Smooth quadratic bend from scalp down to tips!

            float phase = _Time.y * _HairWindSpeed + worldPos.x * 0.75 + worldPos.z * 0.55;
            float swayX = sin(phase) * _HairWindAmp * tipMask;
            float swayZ = cos(phase * 0.85 + 1.1) * (_HairWindAmp * 0.65) * tipMask;
            worldPos.x += swayX;
            worldPos.z += swayZ;

            if (_CurveStrength > 0.00001)
            {
                float dz = worldPos.z - _WorldSpaceCameraPos.z;
                float dx = worldPos.x - _WorldSpaceCameraPos.x;
                float distSq = dz * dz + dx * dx * 0.12;
                worldPos.y -= distSq * _CurveStrength;
            }
            v.vertex = mul(unity_WorldToObject, worldPos);
        }

        half4 LightingCozyACNH(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half wrap = saturate((NdotL + _WrapAmount) / (1.0 + _WrapAmount));
            half3 halfVec = normalize(lightDir + viewDir);
            half NdotH = saturate(dot(s.Normal, halfVec));

            #ifdef UNITY_PASS_FORWARDADD
                half addAtten = smoothstep(0.02, 0.92, atten);
                half3 addDiffuse = s.Albedo * _LightColor0.rgb * (wrap * addAtten * 0.72);
                half addSpec = pow(NdotH, 28.0) * _Glossiness * addAtten * 0.24;
                return half4(addDiffuse + _LightColor0.rgb * addSpec, s.Alpha);
            #else
                half lit = smoothstep(0.08, 0.74, wrap * atten);

                half hemi = saturate(s.Normal.y * 0.5 + 0.5);
                half3 ambientTint = lerp(UNITY_LIGHTMODEL_AMBIENT.rgb * 0.72, unity_AmbientSky.rgb * 0.62, hemi);
                half3 shadowCol = s.Albedo * lerp(_ShadowColor.rgb, _ShadowColor.rgb * 0.75 + ambientTint * 0.45, 0.55);
                half3 litCol = s.Albedo * (_LightColor0.rgb * 0.94 + ambientTint * 0.22);
                half3 finalDiffuse = lerp(shadowCol, litCol, lit);

                half spec = pow(NdotH, 28.0) * _Glossiness * lit * 0.32;

                return half4(finalDiffuse + _LightColor0.rgb * spec, s.Alpha);
            #endif
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float aoH = saturate((IN.worldPos.y + 0.16) / max(0.1, _GroundAOHeight));
            float groundAO = lerp(1.0 - _GroundAO * 0.45, 1.0, smoothstep(0.0, 1.0, aoH));

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            half edgeInk = smoothstep(0.44, 0.88, rim) * _OutlineSoft;
            fixed3 contoured = lerp(c.rgb * groundAO, c.rgb * groundAO * fixed3(0.44, 0.36, 0.46), edgeInk);

            o.Albedo = saturate(contoured);
            o.Alpha = c.a;

            half innerRim = pow(rim, _RimPower) * _RimStrength * (1.0 - edgeInk * 0.60) * groundAO;
            half3 rimEmit = _RimColor.rgb * innerRim;
            o.Emission = _EmissionColor.rgb + rimEmit;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

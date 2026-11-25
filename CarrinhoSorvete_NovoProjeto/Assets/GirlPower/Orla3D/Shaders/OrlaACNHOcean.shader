Shader "GirlPower/OrlaACNHOcean"
{
    Properties
    {
        _ShallowColor ("Shallow Sand-Aqua", Color) = (0.34, 0.80, 0.80, 1)
        _MidColor ("Mid Turquoise Bay", Color) = (0.14, 0.58, 0.72, 1)
        _DeepColor ("Deep Coastal Blue", Color) = (0.08, 0.34, 0.56, 1)
        _FoamColor ("Surf Foam", Color) = (0.94, 0.98, 0.99, 1)
        _SunColor ("Sun Reflection", Color) = (1.0, 0.88, 0.64, 1)
        _SkyReflectColor ("Sky Reflection Tint", Color) = (0.64, 0.82, 0.95, 1)
        _WaveSpeed ("Wave Speed", Range(0.1, 3.0)) = 1.15
        _WaveHeight ("Wave Height", Range(0.0, 0.4)) = 0.06
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 200
        Offset 1, 1

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert noambient
        #pragma target 3.0

        fixed4 _ShallowColor;
        fixed4 _MidColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        fixed4 _SunColor;
        fixed4 _SkyReflectColor;
        float _WaveSpeed;
        float _WaveHeight;
        float _CurveStrength;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float t = _Time.y * _WaveSpeed;

            // Layered Gerstner-like coastal swells
            float w1 = sin(worldPos.x * 0.45 + worldPos.z * 0.35 + t * 1.35) * 0.50;
            float w2 = cos(worldPos.z * 0.95 - t * 1.65 + sin(worldPos.x * 0.25)) * 0.35;
            float w3 = sin(worldPos.x * 1.15 - worldPos.z * 0.70 + t * 2.10) * 0.15;
            worldPos.y += (w1 + w2 + w3) * _WaveHeight;

            if (_CurveStrength > 0.00001)
            {
                float dz = worldPos.z - _WorldSpaceCameraPos.z;
                float dx = worldPos.x - _WorldSpaceCameraPos.x;
                worldPos.y -= (dz * dz + dx * dx * 0.12) * _CurveStrength;
            }
            v.vertex = mul(unity_WorldToObject, worldPos);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float t = _Time.y * _WaveSpeed;
            float3 wp = IN.worldPos;

            // Multi-band coastal depth gradient (shore -> turquoise -> deep bay)
            float shoreCurve = sin(wp.x * 0.22) * 0.65 + cos(wp.x * 0.53) * 0.25;
            float zFromShore = max(0.0, wp.z - (9.8 + shoreCurve));
            float shallowToMid = smoothstep(0.0, 5.2, zFromShore);
            float midToDeep = smoothstep(4.5, 16.5, zFromShore);
            fixed3 waterCol = lerp(_ShallowColor.rgb, _MidColor.rgb, shallowToMid);
            waterCol = lerp(waterCol, _DeepColor.rgb, midToDeep);

            // Stylized shallow caustic mesh shimmer near the sand
            float c1 = sin(wp.x * 2.3 + wp.z * 1.9 + t * 1.8);
            float c2 = cos(wp.x * 1.7 - wp.z * 2.6 - t * 1.4);
            float caustics = smoothstep(0.55, 0.95, abs(c1 * c2)) * (1.0 - shallowToMid) * 0.24;
            waterCol += fixed3(0.18, 0.30, 0.28) * caustics;

            // Primary shoreline surf break + secondary rolling foam crest
            float wash = sin(t * 1.55 + wp.x * 0.28) * 0.45;
            float foam1 = 1.0 - smoothstep(0.0, 0.72, abs(zFromShore - (0.55 + wash)));
            float foam2Wave = sin(zFromShore * 1.95 - t * 2.4 + sin(wp.x * 0.38) * 1.2);
            float foam2 = smoothstep(0.74, 0.94, foam2Wave) * (1.0 - smoothstep(1.0, 9.0, zFromShore));

            // Organic sea-foam lace in deeper water
            float lacePattern = sin(wp.x * 1.4 + t * 0.9) * cos(wp.z * 1.6 - t * 1.1)
                              + sin(wp.x * 2.9 - wp.z * 2.1 + t * 1.5) * 0.45;
            float openWaveCrest = smoothstep(0.95, 1.32, lacePattern) * 0.28;

            // Perturbed tangent-space water normal (Z is up in Unity surface shaders!)
            float nx = cos(wp.x * 1.3 + t * 1.7) * 0.14 + sin(wp.z * 2.1 - t * 1.3) * 0.08;
            float ny = sin(wp.z * 1.6 - t * 2.0) * 0.14 + cos(wp.x * 1.9 + t * 1.1) * 0.08;
            o.Normal = normalize(float3(nx, ny, 1.0));

            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.2);
            waterCol = lerp(waterCol, _SkyReflectColor.rgb, fresnel * 0.42);

            // Sparkling golden sun glints across the bay
            float glintA = sin(wp.x * 2.8 + t * 2.3 + cos(wp.z * 1.9)) * cos(wp.z * 3.4 - t * 1.8);
            float glintB = sin(wp.x * 4.7 - t * 2.9) * sin(wp.z * 4.1 + t * 2.2);
            float sparkle = (smoothstep(0.84, 0.97, glintA) * 0.55 + smoothstep(0.88, 0.98, glintB) * 0.35);

            float foamMask = saturate(foam1 * 0.88 + foam2 * 0.55 + openWaveCrest);
            fixed3 finalCol = lerp(waterCol, _FoamColor.rgb, foamMask);

            o.Albedo = finalCol * 0.88;
            o.Emission = _SunColor.rgb * sparkle * 0.55 + finalCol * 0.16;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

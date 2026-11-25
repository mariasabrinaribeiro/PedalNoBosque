Shader "GirlPower/OrlaACNHOcean"
{
    Properties
    {
        _ShallowColor ("Shallow Turquoise", Color) = (0.30, 0.80, 0.82, 0.84)
        _DeepColor ("Deep Petrol Teal", Color) = (0.09, 0.36, 0.50, 0.96)
        _FoamColor ("Shoreline Foam", Color) = (0.95, 0.99, 1.0, 1.0)
        _SkyReflectColor ("Sky Reflection Tint", Color) = (0.62, 0.83, 0.95, 1.0)
        _DepthDistance ("Depth Fade Distance", Range(0.4, 12.0)) = 2.5
        _FoamDistance ("Shoreline Foam Width", Range(0.1, 2.5)) = 0.65
        _FoamSpeed ("Foam & Wave Speed", Range(0.05, 2.0)) = 0.35
        _WaveHeight ("Gentle Wave Height", Range(0.0, 0.25)) = 0.055
        _CurveStrength ("ACNH Horizon Curve", Range(0.0, 0.01)) = 0.0008
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-20" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alpha:fade noambient
        #pragma target 3.0

        sampler2D _CameraDepthTexture;

        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        fixed4 _SkyReflectColor;
        float _DepthDistance;
        float _FoamDistance;
        float _FoamSpeed;
        float _WaveHeight;
        float _CurveStrength;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float4 screenPos;
        };

        void vert(inout appdata_full v)
        {
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float t = _Time.y * (_FoamSpeed * 3.1);

            // Gentle, calm coastal swell (keeps surfboard height readable!)
            float w1 = sin(worldPos.x * 0.42 + worldPos.z * 0.34 + t * 1.15) * 0.55;
            float w2 = cos(worldPos.z * 0.82 - t * 1.35 + sin(worldPos.x * 0.22)) * 0.35;
            float w3 = sin(worldPos.x * 0.95 - worldPos.z * 0.55 + t * 1.60) * 0.10;
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
            float t = _Time.y * _FoamSpeed;
            float3 wp = IN.worldPos;

            // 1. Real Depth-Buffer comparison (hardened against unpopulated depth buffer returning 0!)
            float rawSceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos));
            bool validDepth = (rawSceneDepth > 0.0002 && rawSceneDepth < 0.9998);
            float sceneEyeDepth = LinearEyeDepth(rawSceneDepth);
            float surfaceEyeDepth = IN.screenPos.w;
            float depthDiff = validDepth ? max(0.0, sceneEyeDepth - surfaceEyeDepth) : 4.0;

            // Synced gentle coastal curve matching Surf_AreiaCreme in OrlaEnvironmentBase.fbx!
            float shoreCurve = sin(wp.x * 0.045) * 0.95 + cos(wp.x * 0.022) * 0.45;
            float zFromShore = max(0.0, wp.z - (10.15 + shoreCurve * 0.85));
            float geoDepth = smoothstep(0.35, 11.5, zFromShore);
            float depthT = saturate(validDepth ? max(depthDiff / max(0.05, _DepthDistance), geoDepth * 0.88) : geoDepth);

            fixed3 waterCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

            // Subtle soft caustic shimmer ONLY in very shallow water (no open-ocean white dots!)
            float c1 = sin(wp.x * 1.55 + wp.z * 1.35 + t * 2.2);
            float c2 = cos(wp.x * 1.25 - wp.z * 1.75 - t * 1.8);
            float shallowCaustic = smoothstep(0.45, 0.95, abs(c1 * c2)) * (1.0 - depthT) * 0.08;
            waterCol += fixed3(0.12, 0.22, 0.22) * shallowCaustic;

            // 2. Refined, Thin, Interrupted Shoreline Foam hugging the sand curve cleanly!
            float nA = sin(wp.x * 1.55 + t * 2.6) * cos(wp.z * 1.85 - t * 2.1);
            float nB = sin(wp.x * 3.10 - wp.z * 2.40 + t * 3.4) * 0.38;
            float foamNoise = saturate(0.5 + 0.34 * (nA + nB));
            // Natural organic gaps along the coastline so foam never reads as a thick unbroken cream stripe
            float shoreBreaks = smoothstep(0.16, 0.62, 0.5 + 0.5 * sin(wp.x * 0.48 + sin(wp.x * 0.19) * 1.4 + t * 0.45));

            float depthShore = (validDepth && zFromShore < 2.6) ? (1.0 - saturate(depthDiff / max(0.05, _FoamDistance * 0.62))) : 0.0;
            float swashWave = sin(t * 3.1 + wp.x * 0.22) * 0.18;
            float beachEdge = (1.0 - smoothstep(0.0, 0.30, abs(zFromShore - (0.24 + swashWave)))) * shoreBreaks;

            float foamDriver = max(depthShore * 0.82, beachEdge * 0.92) + (foamNoise - 0.5) * 0.28;
            float foamMask = smoothstep(0.50, 0.66, foamDriver) * 0.88;

            // Delicate thin secondary wave line just off the sand curve
            float secondWave = sin(zFromShore * 2.4 - t * 3.8 + sin(wp.x * 0.28) * 1.0);
            float secondRibbon = smoothstep(0.89, 0.97, secondWave) * (1.0 - smoothstep(0.6, 2.8, zFromShore)) * shoreBreaks * 0.42;
            foamMask = saturate(max(foamMask, secondRibbon));

            // 3. Calm Perturbed Tangent Normal & Soft Sky Fresnel
            float nx = cos(wp.x * 1.05 + t * 2.4) * 0.08 + sin(wp.z * 1.45 - t * 1.9) * 0.05;
            float ny = sin(wp.z * 1.25 - t * 2.6) * 0.08 + cos(wp.x * 1.35 + t * 1.7) * 0.05;
            o.Normal = normalize(float3(nx, ny, 1.0));

            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.5);
            waterCol = lerp(waterCol, _SkyReflectColor.rgb, fresnel * 0.28);

            fixed3 finalCol = lerp(waterCol, _FoamColor.rgb, foamMask);

            // Smooth shoreline alpha feather so sand-to-water transition is silky soft
            float waterAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthT);
            float edgeFeather = saturate(depthDiff * 2.4 + 0.34);
            float finalAlpha = saturate(max(waterAlpha * edgeFeather, foamMask * 0.94));

            o.Albedo = finalCol * 0.86;
            o.Emission = finalCol * 0.14;
            o.Alpha = finalAlpha;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}

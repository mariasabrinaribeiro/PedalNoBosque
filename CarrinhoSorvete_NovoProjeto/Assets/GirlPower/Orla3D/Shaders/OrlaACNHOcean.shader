Shader "GirlPower/OrlaACNHOcean"
{
    Properties
    {
        _ShallowColor ("Shallow Turquoise", Color) = (0.20, 0.82, 0.86, 0.84)
        _DeepColor ("Deep Petrol Teal", Color) = (0.06, 0.42, 0.62, 0.97)
        _FoamColor ("Shoreline Foam", Color) = (0.96, 0.99, 1.0, 1.0)
        _SkyReflectColor ("Sky Reflection Tint", Color) = (0.64, 0.88, 0.98, 1.0)
        _DepthDistance ("Depth Fade Distance", Range(0.4, 12.0)) = 2.8
        _FoamDistance ("Shoreline Foam Width", Range(0.1, 2.5)) = 0.65
        _FoamSpeed ("Foam & Wave Speed", Range(0.05, 2.0)) = 0.42
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

            // 1. Analytical coastal depth synced strictly to Surf_AreiaCreme shoreline (z ~ 8.35m) + optional depth-buffer refinement!
            // Never disappears even when terrain has ShadowCastingMode.Off (no _CameraDepthTexture entry)!
            float rawSceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos));
            bool validDepth = (rawSceneDepth > 0.0002 && rawSceneDepth < 0.9998);
            float sceneEyeDepth = LinearEyeDepth(rawSceneDepth);
            float surfaceEyeDepth = IN.screenPos.w;
            float depthDiff = validDepth ? max(0.0, sceneEyeDepth - surfaceEyeDepth) : 4.0;

            float shoreCurve = sin(wp.x * 0.045) * 0.72 + cos(wp.x * 0.022) * 0.35;
            float shoreZ = 8.35 + shoreCurve * 0.80;
            float zFromShore = wp.z - shoreZ;
            float geoDepth = smoothstep(0.15, 9.5, max(0.0, zFromShore));
            float depthT = saturate(validDepth ? max(geoDepth, saturate(depthDiff / max(0.25, _DepthDistance)) * 0.75) : geoDepth);

            fixed3 waterCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

            // Lively sunlit water ripples & gentle shallow caustics so water surface reads unmistakably!
            float c1 = sin(wp.x * 1.45 + wp.z * 1.25 + t * 2.4);
            float c2 = cos(wp.x * 1.15 - wp.z * 1.65 - t * 2.0);
            float shallowCaustic = smoothstep(0.35, 0.92, abs(c1 * c2)) * (1.0 - depthT * 0.65) * 0.16;
            waterCol += fixed3(0.16, 0.30, 0.32) * shallowCaustic;

            // Gentle sunlit open-sea wave bands moving toward shore
            float swellBands = sin(wp.z * 1.35 - t * 2.9 + sin(wp.x * 0.32 + t * 0.6) * 1.1);
            float waveHighlight = smoothstep(0.76, 0.96, swellBands) * smoothstep(0.8, 3.5, zFromShore) * 0.14;
            waterCol += fixed3(0.35, 0.58, 0.65) * waveHighlight;

            // 2. Crisp animated shoreline swash foam hugging the curved sand edge!
            float nA = sin(wp.x * 1.55 + t * 2.6) * cos(wp.z * 1.85 - t * 2.1);
            float nB = sin(wp.x * 3.10 - wp.z * 2.40 + t * 3.4) * 0.38;
            float foamNoise = saturate(0.5 + 0.34 * (nA + nB));
            float shoreBreaks = smoothstep(0.12, 0.56, 0.5 + 0.5 * sin(wp.x * 0.45 + sin(wp.x * 0.18) * 1.3 + t * 0.5));

            float depthShore = (validDepth && zFromShore > -0.2 && zFromShore < 2.2)
                ? (1.0 - saturate(depthDiff / max(0.08, _FoamDistance * 0.65)))
                : 0.0;
            float swashWave = sin(t * 3.0 + wp.x * 0.24) * 0.22;
            float beachEdge = (1.0 - smoothstep(0.0, 0.38, abs(zFromShore - (0.28 + swashWave)))) * (0.45 + 0.55 * shoreBreaks);

            float foamDriver = max(depthShore * 0.78, beachEdge * 0.98) + (foamNoise - 0.5) * 0.26;
            float foamMask = smoothstep(0.46, 0.64, foamDriver) * 0.94;

            // Delicate secondary incoming wave ribbon
            float secondWave = sin(max(0.0, zFromShore) * 2.2 - t * 3.6 + sin(wp.x * 0.28) * 1.0);
            float secondRibbon = smoothstep(0.86, 0.96, secondWave) * smoothstep(0.3, 1.1, zFromShore) * (1.0 - smoothstep(1.2, 4.2, zFromShore)) * (0.35 + 0.65 * shoreBreaks) * 0.52;
            foamMask = saturate(max(foamMask, secondRibbon));

            // 3. Perturbed Tangent Normal & Sky Fresnel Reflection
            float nx = cos(wp.x * 1.05 + t * 2.4) * 0.09 + sin(wp.z * 1.45 - t * 1.9) * 0.06;
            float ny = sin(wp.z * 1.25 - t * 2.6) * 0.09 + cos(wp.x * 1.35 + t * 1.7) * 0.06;
            o.Normal = normalize(float3(nx, ny, 1.0));

            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.2);
            waterCol = lerp(waterCol, _SkyReflectColor.rgb, fresnel * 0.30);

            fixed3 finalCol = lerp(waterCol, _FoamColor.rgb, foamMask);

            // Smooth shoreline feather starting strictly at shoreZ while keeping sea rich & clearly visible!
            float shoreFade = smoothstep(-0.35, 0.65, zFromShore);
            float waterAlpha = lerp(max(0.76, _ShallowColor.a), max(0.95, _DeepColor.a), depthT);
            float finalAlpha = saturate(max(waterAlpha * shoreFade, foamMask * smoothstep(-0.45, 0.15, zFromShore) * 0.96));

            o.Albedo = finalCol * 0.72;
            o.Emission = finalCol * 0.28;
            o.Alpha = finalAlpha;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}

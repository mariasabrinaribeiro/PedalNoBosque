Shader "GirlPower/OrlaACNHOcean"
{
    Properties
    {
        _ShallowColor ("Shallow Turquoise", Color) = (0.16, 0.80, 0.86, 0.82)
        _DeepColor ("Deep Petrol Teal", Color) = (0.05, 0.40, 0.62, 0.96)
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
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            fixed4 _FoamColor;
            fixed4 _SkyReflectColor;
            float _DepthDistance;
            float _FoamDistance;
            float _FoamSpeed;
            float _WaveHeight;
            float _CurveStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 uncurvedWorldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float t = _Time.y * (_FoamSpeed * 3.1);

                // Gentle, calm coastal swell
                float w1 = sin(worldPos.x * 0.42 + worldPos.z * 0.34 + t * 1.15) * 0.55;
                float w2 = cos(worldPos.z * 0.82 - t * 1.35 + sin(worldPos.x * 0.22)) * 0.35;
                float w3 = sin(worldPos.x * 0.95 - worldPos.z * 0.55 + t * 1.60) * 0.10;
                worldPos.y += (w1 + w2 + w3) * _WaveHeight;

                o.uncurvedWorldPos = worldPos.xyz;

                if (_CurveStrength > 0.00001)
                {
                    float dz = worldPos.z - _WorldSpaceCameraPos.z;
                    float dx = worldPos.x - _WorldSpaceCameraPos.x;
                    worldPos.y -= (dz * dz + dx * dx * 0.12) * _CurveStrength;
                }

                o.worldPos = worldPos.xyz;
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _FoamSpeed;
                float3 wp = i.uncurvedWorldPos;

                // 1. Curved coastal depth synced strictly to Surf_AreiaCreme shoreline (z ~ 8.2m)
                float shoreCurve = sin(wp.x * 0.045) * 0.72 + cos(wp.x * 0.022) * 0.35;
                float shoreZ = 8.20 + shoreCurve * 0.80;
                float zFromShore = wp.z - shoreZ;
                float depthT = smoothstep(0.15, 10.0, max(0.0, zFromShore));

                fixed3 waterCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

                // 2. Lively shallow turquoise caustics & gentle incoming sunlit wave ribbons
                float c1 = sin(wp.x * 1.35 + wp.z * 1.20 + t * 2.4);
                float c2 = cos(wp.x * 1.10 - wp.z * 1.55 - t * 2.0);
                float shallowCaustic = smoothstep(0.32, 0.90, abs(c1 * c2)) * (1.0 - depthT * 0.60) * 0.20;
                waterCol += fixed3(0.20, 0.34, 0.36) * shallowCaustic;

                float swellBands = sin(wp.z * 1.25 - t * 2.8 + sin(wp.x * 0.30 + t * 0.6) * 1.1);
                float waveHighlight = smoothstep(0.74, 0.96, swellBands) * smoothstep(0.6, 3.2, zFromShore) * 0.16;
                waterCol += fixed3(0.38, 0.62, 0.70) * waveHighlight;

                // 3. Crisp animated shoreline swash foam hugging the curved sand edge
                float nA = sin(wp.x * 1.50 + t * 2.6) * cos(wp.z * 1.80 - t * 2.1);
                float nB = sin(wp.x * 3.00 - wp.z * 2.35 + t * 3.4) * 0.38;
                float foamNoise = saturate(0.5 + 0.34 * (nA + nB));
                float shoreBreaks = smoothstep(0.12, 0.56, 0.5 + 0.5 * sin(wp.x * 0.44 + sin(wp.x * 0.18) * 1.3 + t * 0.5));

                float swashWave = sin(t * 3.0 + wp.x * 0.24) * 0.24;
                float beachEdge = (1.0 - smoothstep(0.0, 0.44, abs(zFromShore - (0.30 + swashWave)))) * (0.48 + 0.52 * shoreBreaks);
                float foamDriver = beachEdge * 0.98 + (foamNoise - 0.5) * 0.24;
                float foamMask = smoothstep(0.42, 0.62, foamDriver) * 0.94;

                float secondWave = sin(max(0.0, zFromShore) * 2.1 - t * 3.6 + sin(wp.x * 0.28) * 1.0);
                float secondRibbon = smoothstep(0.84, 0.96, secondWave) * smoothstep(0.35, 1.15, zFromShore) * (1.0 - smoothstep(1.3, 4.6, zFromShore)) * (0.38 + 0.62 * shoreBreaks) * 0.56;
                foamMask = saturate(max(foamMask, secondRibbon));

                // 4. World-space wave normal, soft sunlit shading & sky Fresnel (ZERO mesh tangent dependency!)
                float nx = cos(wp.x * 1.05 + t * 2.4) * 0.11 + sin(wp.z * 1.45 - t * 1.9) * 0.07;
                float nz = sin(wp.z * 1.25 - t * 2.6) * 0.11 + cos(wp.x * 1.35 + t * 1.7) * 0.07;
                float3 worldNormal = normalize(float3(nx, 1.0, nz));
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(float3(-0.35, 0.85, 0.38));

                float ndotl = saturate(dot(worldNormal, lightDir) * 0.45 + 0.65);
                float3 halfVec = normalize(lightDir + viewDir);
                float sunSpec = pow(saturate(dot(worldNormal, halfVec)), 36.0) * 0.28;
                float fresnel = pow(1.0 - saturate(dot(viewDir, worldNormal)), 3.2);

                waterCol = waterCol * ndotl + fixed3(1.0, 0.96, 0.86) * sunSpec;
                waterCol = lerp(waterCol, _SkyReflectColor.rgb, fresnel * 0.28);

                fixed3 finalCol = lerp(waterCol, _FoamColor.rgb, foamMask);

                // 5. Silky shoreline blend starting cleanly at shoreZ while keeping sea rich & unmistakable!
                float shoreFade = smoothstep(-0.35, 0.60, zFromShore);
                float waterAlpha = lerp(max(0.76, _ShallowColor.a), max(0.95, _DeepColor.a), depthT);
                float finalAlpha = saturate(max(waterAlpha * shoreFade, foamMask * smoothstep(-0.45, 0.15, zFromShore) * 0.96));

                fixed4 col = fixed4(finalCol, finalAlpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}

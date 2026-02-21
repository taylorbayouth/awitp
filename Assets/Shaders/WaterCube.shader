// WaterCube Shader
// Realistic water effect for rounded cube mesh with animated waves,
// environment reflections, refraction, specular highlights, and subsurface scattering.
// Built-in render pipeline (CGPROGRAM).
Shader "Custom/WaterCube"
{
    Properties
    {
        [Header(Colors)]
        _DeepColor ("Deep Color", Color) = (0.02, 0.08, 0.25, 1)
        _ShallowColor ("Shallow Color", Color) = (0.1, 0.4, 0.6, 1)
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SSSColor ("Subsurface Color", Color) = (0.1, 0.6, 0.3, 1)

        [Header(Waves Primary)]
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.1)) = 0.015
        _WaveFrequency ("Wave Frequency", Range(0.5, 10)) = 3.0
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.2

        [Header(Waves Secondary)]
        _Wave2Amplitude ("Wave 2 Amplitude", Range(0, 0.05)) = 0.008
        _Wave2Frequency ("Wave 2 Frequency", Range(1, 20)) = 7.0
        _Wave2Speed ("Wave 2 Speed", Range(0, 5)) = 0.8

        [Header(Surface Detail)]
        _NormalStrength ("Normal Perturbation", Range(0, 2)) = 0.8
        _NormalScale ("Normal Noise Scale", Range(1, 20)) = 6.0
        _NormalSpeed ("Normal Animation Speed", Range(0, 3)) = 0.5

        [Header(Fresnel and Reflection)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3.0
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.6

        [Header(Refraction)]
        _RefractionStrength ("Refraction Distortion", Range(0, 0.1)) = 0.03

        [Header(Specular)]
        _SpecularPower ("Specular Power", Range(8, 512)) = 128
        _SpecularIntensity ("Specular Intensity", Range(0, 3)) = 1.5

        [Header(Subsurface Scattering)]
        _SSSPower ("SSS Power", Range(1, 10)) = 4.0
        _SSSIntensity ("SSS Intensity", Range(0, 1)) = 0.3

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Transparency)]
        _MinAlpha ("Minimum Alpha", Range(0.3, 1)) = 0.75
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        // ============================================================
        // Pass 0: GrabPass — capture screen for refraction
        // Named grab shares one capture across all water cubes per frame
        // ============================================================
        GrabPass { "_WaterGrabTexture" }

        // ============================================================
        // Pass 1: Main water surface
        // ============================================================
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBitangent : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
                float4 grabPos : TEXCOORD5;
                float3 localPos : TEXCOORD6;
            };

            // Properties
            fixed4 _DeepColor;
            fixed4 _ShallowColor;
            fixed4 _SpecularColor;
            fixed4 _SSSColor;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Wave2Amplitude;
            float _Wave2Frequency;
            float _Wave2Speed;

            float _NormalStrength;
            float _NormalScale;
            float _NormalSpeed;

            float _FresnelPower;
            float _ReflectionStrength;
            float _RefractionStrength;

            float _SpecularPower;
            float _SpecularIntensity;

            float _SSSPower;
            float _SSSIntensity;

            float _MinAlpha;

            fixed4 _EmissionColor;

            sampler2D _WaterGrabTexture;

            // --------------------------------------------------------
            // Noise functions (from FlufflyCloudShader.shader)
            // --------------------------------------------------------
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash(i);
                float n100 = hash(i + float3(1,0,0));
                float n010 = hash(i + float3(0,1,0));
                float n110 = hash(i + float3(1,1,0));
                float n001 = hash(i + float3(0,0,1));
                float n101 = hash(i + float3(1,0,1));
                float n011 = hash(i + float3(0,1,1));
                float n111 = hash(i + float3(1,1,1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            // Fractal Brownian Motion — layered noise for richer detail
            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // --------------------------------------------------------
            // Gerstner-style wave displacement
            // --------------------------------------------------------
            float gerstnerWave(float3 worldPos, float3 dir, float freq, float speed, float amp)
            {
                float phase = dot(worldPos, dir) * freq + _Time.y * speed;
                // Sharp peaks, wide troughs (characteristic of real water)
                return amp * (0.5 * sin(phase) + 0.25 * sin(phase * 2.1 + 0.7));
            }

            // --------------------------------------------------------
            // Vertex shader
            // --------------------------------------------------------
            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                // Two wave layers displaced along mesh normal
                float3 waveDir1 = normalize(float3(1.0, 0.7, 0.3));
                float3 waveDir2 = normalize(float3(-0.5, 1.0, 0.8));

                float wave1 = gerstnerWave(worldPos, waveDir1, _WaveFrequency, _WaveSpeed, _WaveAmplitude);
                float wave2 = gerstnerWave(worldPos, waveDir2, _Wave2Frequency, _Wave2Speed, _Wave2Amplitude);
                float totalDisplacement = wave1 + wave2;

                // Displace along object-space normal (wraps around rounded corners)
                v.vertex.xyz += v.normal * totalDisplacement;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.localPos = v.vertex.xyz;

                return o;
            }

            // --------------------------------------------------------
            // Fragment shader
            // --------------------------------------------------------
            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldNormal = normalize(i.worldNormal);
                float3 worldTangent = normalize(i.worldTangent);
                float3 worldBitangent = normalize(i.worldBitangent);
                float3 viewDir = normalize(i.viewDir);

                // ------- Procedural normal perturbation -------
                float3 noisePos = i.worldPos * _NormalScale + _Time.y * _NormalSpeed;
                float eps = 0.02;
                float h0 = fbm(noisePos);
                float hT = fbm(noisePos + worldTangent * eps);
                float hB = fbm(noisePos + worldBitangent * eps);

                float3 perturbedNormal = normalize(
                    worldNormal
                    + worldTangent * ((hT - h0) / eps * _NormalStrength)
                    + worldBitangent * ((hB - h0) / eps * _NormalStrength)
                );

                // ------- Fresnel -------
                float NdotV = saturate(dot(perturbedNormal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // ------- Cubemap reflection -------
                float3 reflDir = reflect(-viewDir, perturbedNormal);
                float4 envSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflDir);
                float3 envColor = DecodeHDR(envSample, unity_SpecCube0_HDR);
                float3 reflection = envColor * _ReflectionStrength;

                // ------- GrabPass refraction -------
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                grabUV += perturbedNormal.xy * _RefractionStrength;
                float3 refractionColor = tex2D(_WaterGrabTexture, grabUV).rgb;

                // ------- Deep/shallow color tinting -------
                float3 waterTint = lerp(_DeepColor.rgb, _ShallowColor.rgb, fresnel);
                float3 tintedRefraction = refractionColor * waterTint * 2.0;

                // ------- Specular highlights (Blinn-Phong) -------
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfVec = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(perturbedNormal, halfVec)), _SpecularPower);
                float3 specular = _SpecularColor.rgb * _LightColor0.rgb * spec * _SpecularIntensity;

                // ------- Subsurface scattering approximation -------
                float sssBase = pow(saturate(dot(viewDir, -lightDir)), _SSSPower);
                float sssMask = (1.0 - NdotV) * 0.5 + 0.5; // Stronger at edges
                float3 sss = _SSSColor.rgb * _LightColor0.rgb * sssBase * sssMask * _SSSIntensity;

                // ------- Final compositing -------
                float3 color = lerp(tintedRefraction, reflection, fresnel) + specular + sss + _EmissionColor.rgb;

                // Alpha: opaque at glancing angles, slightly transparent at center
                float alpha = lerp(_MinAlpha, 1.0, fresnel);

                return fixed4(color, alpha);
            }
            ENDCG
        }

        // ============================================================
        // Pass 2: Shadow caster — animated to match wave displacement
        // ============================================================
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct appdata_shadow
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f_shadow
            {
                V2F_SHADOW_CASTER;
            };

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Wave2Amplitude;
            float _Wave2Frequency;
            float _Wave2Speed;

            float gerstnerWaveShadow(float3 worldPos, float3 dir, float freq, float speed, float amp)
            {
                float phase = dot(worldPos, dir) * freq + _Time.y * speed;
                return amp * (0.5 * sin(phase) + 0.25 * sin(phase * 2.1 + 0.7));
            }

            v2f_shadow vertShadow(appdata_shadow v)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 waveDir1 = normalize(float3(1.0, 0.7, 0.3));
                float3 waveDir2 = normalize(float3(-0.5, 1.0, 0.8));

                float wave1 = gerstnerWaveShadow(worldPos, waveDir1, _WaveFrequency, _WaveSpeed, _WaveAmplitude);
                float wave2 = gerstnerWaveShadow(worldPos, waveDir2, _Wave2Frequency, _Wave2Speed, _Wave2Amplitude);

                v.vertex.xyz += v.normal * (wave1 + wave2);

                v2f_shadow o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 fragShadow(v2f_shadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    // Fallback without GrabPass for platforms that don't support it
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma vertex vertFallback
            #pragma fragment fragFallback
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata_fb
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f_fb
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBitangent : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
            };

            fixed4 _DeepColor;
            fixed4 _ShallowColor;
            fixed4 _SpecularColor;
            fixed4 _SSSColor;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Wave2Amplitude;
            float _Wave2Frequency;
            float _Wave2Speed;

            float _NormalStrength;
            float _NormalScale;
            float _NormalSpeed;

            float _FresnelPower;
            float _ReflectionStrength;

            float _SpecularPower;
            float _SpecularIntensity;

            float _SSSPower;
            float _SSSIntensity;

            float _MinAlpha;

            fixed4 _EmissionColor;

            float hash_fb(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise_fb(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash_fb(i);
                float n100 = hash_fb(i + float3(1,0,0));
                float n010 = hash_fb(i + float3(0,1,0));
                float n110 = hash_fb(i + float3(1,1,0));
                float n001 = hash_fb(i + float3(0,0,1));
                float n101 = hash_fb(i + float3(1,0,1));
                float n011 = hash_fb(i + float3(0,1,1));
                float n111 = hash_fb(i + float3(1,1,1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            float fbm_fb(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise_fb(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            float gerstnerWaveFB(float3 worldPos, float3 dir, float freq, float speed, float amp)
            {
                float phase = dot(worldPos, dir) * freq + _Time.y * speed;
                return amp * (0.5 * sin(phase) + 0.25 * sin(phase * 2.1 + 0.7));
            }

            v2f_fb vertFallback(appdata_fb v)
            {
                v2f_fb o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 waveDir1 = normalize(float3(1.0, 0.7, 0.3));
                float3 waveDir2 = normalize(float3(-0.5, 1.0, 0.8));

                float wave1 = gerstnerWaveFB(worldPos, waveDir1, _WaveFrequency, _WaveSpeed, _WaveAmplitude);
                float wave2 = gerstnerWaveFB(worldPos, waveDir2, _Wave2Frequency, _Wave2Speed, _Wave2Amplitude);

                v.vertex.xyz += v.normal * (wave1 + wave2);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);

                return o;
            }

            fixed4 fragFallback(v2f_fb i) : SV_Target
            {
                float3 worldNormal = normalize(i.worldNormal);
                float3 worldTangent = normalize(i.worldTangent);
                float3 worldBitangent = normalize(i.worldBitangent);
                float3 viewDir = normalize(i.viewDir);

                // Procedural normals
                float3 noisePos = i.worldPos * _NormalScale + _Time.y * _NormalSpeed;
                float eps = 0.02;
                float h0 = fbm_fb(noisePos);
                float hT = fbm_fb(noisePos + worldTangent * eps);
                float hB = fbm_fb(noisePos + worldBitangent * eps);

                float3 perturbedNormal = normalize(
                    worldNormal
                    + worldTangent * ((hT - h0) / eps * _NormalStrength)
                    + worldBitangent * ((hB - h0) / eps * _NormalStrength)
                );

                // Fresnel
                float NdotV = saturate(dot(perturbedNormal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Cubemap reflection
                float3 reflDir = reflect(-viewDir, perturbedNormal);
                float4 envSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflDir);
                float3 envColor = DecodeHDR(envSample, unity_SpecCube0_HDR);
                float3 reflection = envColor * _ReflectionStrength;

                // No refraction in fallback — use deep color
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, fresnel);

                // Specular
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfVec = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(perturbedNormal, halfVec)), _SpecularPower);
                float3 specular = _SpecularColor.rgb * _LightColor0.rgb * spec * _SpecularIntensity;

                // SSS
                float sssBase = pow(saturate(dot(viewDir, -lightDir)), _SSSPower);
                float sssMask = (1.0 - NdotV) * 0.5 + 0.5;
                float3 sss = _SSSColor.rgb * _LightColor0.rgb * sssBase * sssMask * _SSSIntensity;

                // Composite
                float3 color = lerp(waterColor, reflection, fresnel) + specular + sss + _EmissionColor.rgb;
                float alpha = lerp(_MinAlpha, 1.0, fresnel);

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}

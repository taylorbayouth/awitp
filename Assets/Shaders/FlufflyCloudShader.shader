Shader "Custom/FlufflyCloud"
{
    Properties
    {
        _Color ("Primary Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.7, 0.7, 0.8, 1)
        _ShadowBlend ("Shadow Blend", Range(0, 1)) = 0.2
        _CoreOpacity ("Core Opacity", Range(0, 1)) = 0.95
        _EdgeFalloff ("Edge Falloff", Range(0.5, 5)) = 2.0
        _EdgeBrightness ("Edge Brightness", Range(0, 2)) = 0.3
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 0.5
        _WaveScale ("Wave Scale", Range(0.5, 5)) = 2.0
        _WaveAmount ("Wave Amount", Range(0, 0.1)) = 0.02
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.3
        _PulseAmount ("Pulse Amount", Range(0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Soft blending - spheres add together nicely
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 localPos : TEXCOORD2;
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            float _ShadowBlend;
            float _CoreOpacity;
            float _EdgeFalloff;
            float _EdgeBrightness;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveAmount;
            float _PulseSpeed;
            float _PulseAmount;

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

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Animation
                float wave = noise(worldPos * _WaveScale + _Time.y * _WaveSpeed);
                wave = (wave - 0.5) * 2.0;
                float pulse = sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5;
                float displacement = wave * _WaveAmount + pulse * _PulseAmount;

                v.vertex.xyz += v.normal * displacement;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.localPos = v.vertex.xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);

                // Fresnel: 0 at center (facing camera), 1 at edges
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = 1.0 - NdotV;

                // Soft alpha falloff at edges (unchanged - preserves existing behavior)
                float alpha = pow(NdotV, _EdgeFalloff) * _CoreOpacity;

                // Shadow factor based on normal facing down (underside of blob)
                // 0 = facing up/side (primary color), 1 = facing down (shadow color)
                float shadowFactor = saturate(-normal.y) * _ShadowBlend;

                // Blend between primary and shadow colors
                float3 baseColor = lerp(_Color.rgb, _ShadowColor.rgb, shadowFactor);

                // Brighten edges slightly for that fluffy glow (applied to blended color)
                float3 col = baseColor + fresnel * _EdgeBrightness;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
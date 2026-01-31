Shader "Custom/CursorGlow"
{
    Properties
    {
        _EmissionStrength ("Emission Strength", Range(1, 3)) = 1.5
        _WiggleAmount ("Wiggle Amount", Range(0, 0.2)) = 0.03
        _WiggleSpeed ("Wiggle Speed", Range(0, 10)) = 2.0
        _WiggleFrequency ("Wiggle Frequency", Range(0, 30)) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD0;
            };

            float _EmissionStrength;
            float _WiggleAmount;
            float _WiggleSpeed;
            float _WiggleFrequency;

            v2f vert (appdata v)
            {
                v2f o;

                float time = _Time.y * _WiggleSpeed;
                float3 localPos = v.vertex.xyz;

                // Create wiggle along the line perimeter
                float wiggle = sin(length(localPos.xy) * _WiggleFrequency + time) * _WiggleAmount;
                wiggle += sin(length(localPos.xy) * _WiggleFrequency * 2.3 + time * 1.7) * _WiggleAmount * 0.5;

                // Apply wiggle perpendicular to the border (radial direction from center)
                float2 dir = normalize(localPos.xy);
                v.vertex.xy += dir * wiggle;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use vertex color from LineRenderer (preserves red/green/blue colors)
                fixed4 col = i.color;

                // Add glow/emission
                col.rgb *= _EmissionStrength;

                // Gentle pulse
                float pulse = sin(_Time.y * 2.0) * 0.15 + 0.85;
                col.rgb *= pulse;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

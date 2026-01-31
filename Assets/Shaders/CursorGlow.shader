// CursorGlow Shader
// Adds animated glow and wiggle effects to the grid cursor border
// Preserves vertex colors (red/green/blue) while adding visual enhancements
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
        Blend SrcAlpha OneMinusSrcAlpha  // Standard alpha blending
        ZWrite Off   // Don't write to depth buffer (for transparency)
        Cull Off     // Render both sides

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Input from mesh
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;      // Vertex color from LineRenderer
            };

            // Output from vertex shader to fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD0;
            };

            // Shader properties (set from C# script)
            float _EmissionStrength;
            float _WiggleAmount;
            float _WiggleSpeed;
            float _WiggleFrequency;

            // Vertex Shader: animates vertices to create wiggle effect
            v2f vert (appdata v)
            {
                v2f o;

                float time = _Time.y * _WiggleSpeed;
                float3 localPos = v.vertex.xyz;

                // Create smooth wiggle using two layered sine waves
                // Primary wiggle: travels along the border perimeter
                float wiggle = sin(length(localPos.xy) * _WiggleFrequency + time) * _WiggleAmount;
                // Secondary wiggle: adds organic variation (different frequency and phase)
                wiggle += sin(length(localPos.xy) * _WiggleFrequency * 2.3 + time * 1.7) * _WiggleAmount * 0.5;

                // Apply wiggle perpendicular to the border (radial outward/inward)
                float2 dir = normalize(localPos.xy);
                v.vertex.xy += dir * wiggle;

                // Transform to clip space
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;  // Pass through vertex color
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Fragment Shader: applies glow and pulse effects
            fixed4 frag (v2f i) : SV_Target
            {
                // Start with vertex color (red/green/blue from BorderRenderer)
                fixed4 col = i.color;

                // Brighten for glow effect
                col.rgb *= _EmissionStrength;

                // Gentle pulse animation (15% variation around 85% brightness)
                float pulse = sin(_Time.y * 2.0) * 0.15 + 0.85;
                col.rgb *= pulse;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

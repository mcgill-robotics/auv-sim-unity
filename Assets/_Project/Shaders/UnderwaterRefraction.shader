Shader "Hidden/UnderwaterRefraction"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _RefractionIndex ("Refraction Index", Float) = 1.33333
        [HideInInspector] _FocalLength ("Focal Length (fx, fy)", Vector) = (504.6, 504.6, 0, 0)
        [HideInInspector] _PrincipalPoint ("Principal Point (cx, cy)", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _Resolution ("Resolution (Width, Height)", Vector) = (672, 376, 0, 0)
        [HideInInspector] _FlipY ("Flip Y", Float) = 1.0
        [HideInInspector] _Enabled ("Enabled", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _RefractionIndex;
            float4 _FocalLength;
            float4 _PrincipalPoint;
            float4 _Resolution;
            float _FlipY;
            float _Enabled;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // When _FlipY is active (e.g. for top-to-bottom video encoding), 
                // flip destination V coordinate to sample the top row first.
                float v_in = (_FlipY > 0.5) ? (1.0 - i.uv.y) : i.uv.y;
                float u_norm = i.uv.x - _PrincipalPoint.x;
                float v_norm = v_in - _PrincipalPoint.y;

                float S = 1.0;

                if (_Enabled > 0.5 && _RefractionIndex > 1.0001)
                {
                    // Convert normalized UV offsets to sensor pixel distances from optical center
                    float u_px = u_norm * _Resolution.x;
                    float v_px = v_norm * _Resolution.y;

                    // Compute tangent of ray angle in air inside the camera housing
                    float r_ax = u_px / max(_FocalLength.x, 1e-4);
                    float r_ay = v_px / max(_FocalLength.y, 1e-4);
                    float r_a = sqrt(r_ax * r_ax + r_ay * r_ay); // tan(theta_a)

                    if (r_a > 1e-6)
                    {
                        // sin(theta_a) = tan(theta_a) / sqrt(1 + tan^2(theta_a))
                        float sin_a = r_a / sqrt(1.0 + r_a * r_a);

                        // Snell's Law across flat port interface: sin(theta_w) = sin(theta_a) / n_w
                        float sin_w = sin_a / _RefractionIndex;

                        // cos(theta_w) = sqrt(1 - sin^2(theta_w))
                        float cos_w = sqrt(max(1e-7, 1.0 - sin_w * sin_w));

                        // tan(theta_w) = sin(theta_w) / cos(theta_w)
                        float tan_w = sin_w / cos_w;

                        // Contraction scale factor S = tan(theta_w) / tan(theta_a)
                        S = tan_w / r_a;
                    }
                    else
                    {
                        // Paraxial approximation at optical center: S -> 1 / n_w
                        S = 1.0 / _RefractionIndex;
                    }
                }

                // Sample coordinate in the unrefracted render texture
                float2 sampleUV;
                sampleUV.x = u_norm * S + _PrincipalPoint.x;
                sampleUV.y = v_norm * S + _PrincipalPoint.y;

                // Safety clamp to ensure within valid texture bounds
                sampleUV = clamp(sampleUV, 0.0, 1.0);

                return tex2D(_MainTex, sampleUV);
            }
            ENDCG
        }
    }
    Fallback Off
}

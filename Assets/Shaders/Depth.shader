Shader "Custom/DistanceShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" { }
    }

    SubShader {
        Tags { "Queue" = "Overlay" }

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
        };

        sampler2D _MainTex;

        void surf(Input IN, inout SurfaceOutput o) {
            // Sample the texture for demonstration purposes
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            
            // Calculate the distance from the camera manually
            float3 cameraPos = _WorldSpaceCameraPos.xyz;
            float3 worldPos = IN.worldPos;
            float distance = sqrt(dot(worldPos - cameraPos, worldPos - cameraPos));
            
            // Output the distance as the grayscale color
            o.Albedo = c.rgb * distance;
        }
        ENDCG
    } 
    FallBack "Diffuse"
}

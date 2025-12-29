Shader "Hidden/LinearDepthResampler"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRP" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 texcoord : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Flip Y for ROS compatibility (Unity: bottom-left origin, ROS: top-left)
                float2 flippedUV = float2(input.texcoord.x, 1.0 - input.texcoord.y);
                
                // Sample depth buffer
                uint2 pixelCoords = uint2(flippedUV * _ScreenSize.xy);
                float depthRaw = LoadCameraDepth(pixelCoords);
                
                // Get linear Z depth (perpendicular distance from camera plane)
                float linearZ = LinearEyeDepth(depthRaw, _ZBufferParams);
                
                // Reconstruct view-space position to get TRUE Euclidean distance
                // UV to NDC: (0,1) -> (-1,1)
                float2 ndc = flippedUV * 2.0 - 1.0;
                
                // Use inverse projection to get view-space ray direction
                // _FrustumParams contains frustum info: x=1/tanHalfFOV, y=aspect*1/tanHalfFOV
                // For HDRP, we use the projection matrix directly
                float2 viewRay = ndc / float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]);
                
                // View-space position: ray * linearZ gives us the 3D position
                float3 viewPos = float3(viewRay * linearZ, linearZ);
                
                // True Euclidean distance from camera origin (0,0,0 in view space)
                float trueDistance = length(viewPos);

                return float4(trueDistance, trueDistance, trueDistance, 1);
            }
            ENDHLSL
        }
    }
}
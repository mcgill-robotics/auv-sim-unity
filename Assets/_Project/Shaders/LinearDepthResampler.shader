Shader "Hidden/LinearDepthResampler"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }
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
                
                // Get linear Z depth (perpendicular distance from camera plane along optical axis, REP 118)
                float linearZ = LinearEyeDepth(depthRaw, _ZBufferParams);

                return float4(linearZ, linearZ, linearZ, 1);
            }
            ENDHLSL
        }
    }
}
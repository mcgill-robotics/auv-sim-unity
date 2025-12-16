Shader "Hidden/DepthHeatmap"
{
    Properties
    {
        _MinDist("Min Distance (Red)", Float) = 0.3
        _MaxDist("Max Distance (Blue)", Float) = 20.0
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

            float _MinDist;
            float _MaxDist;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            // ZED-style color ramp: Deep Red/Orange -> Yellow -> Green -> Cyan -> Deep Blue
            float3 GetZEDStyleColor(float t)
            {
                // More saturated colors like ZED2i uses
                float3 c1 = float3(0.8, 0.0, 0.0);   // Deep Red (very close)
                float3 c2 = float3(1.0, 0.3, 0.0);   // Orange-Red
                float3 c3 = float3(1.0, 0.8, 0.0);   // Yellow-Orange  
                float3 c4 = float3(0.5, 1.0, 0.0);   // Yellow-Green
                float3 c5 = float3(0.0, 1.0, 0.3);   // Green
                float3 c6 = float3(0.0, 0.9, 0.9);   // Cyan
                float3 c7 = float3(0.0, 0.4, 1.0);   // Light Blue
                float3 c8 = float3(0.0, 0.0, 0.8);   // Deep Blue (far)

                // 8-step gradient for smoother transitions
                float3 color;
                if (t < 0.143)
                    color = lerp(c1, c2, t / 0.143);
                else if (t < 0.286)
                    color = lerp(c2, c3, (t - 0.143) / 0.143);
                else if (t < 0.429)
                    color = lerp(c3, c4, (t - 0.286) / 0.143);
                else if (t < 0.571)
                    color = lerp(c4, c5, (t - 0.429) / 0.143);
                else if (t < 0.714)
                    color = lerp(c5, c6, (t - 0.571) / 0.143);
                else if (t < 0.857)
                    color = lerp(c6, c7, (t - 0.714) / 0.143);
                else
                    color = lerp(c7, c8, (t - 0.857) / 0.143);
                
                return color;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Sample depth
                float depthRaw = SampleCameraDepth(input.texcoord);
                float linearZ = LinearEyeDepth(depthRaw, _ZBufferParams);
                
                // Reconstruct true distance
                float2 ndc = input.texcoord * 2.0 - 1.0;
                float2 viewRay = ndc / float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]);
                float3 viewPos = float3(viewRay * linearZ, linearZ);
                float trueDistance = length(viewPos);

                // Logarithmic scaling for better contrast (like ZED2i)
                float logMin = log(max(_MinDist, 0.01));
                float logMax = log(_MaxDist);
                float logDist = log(max(trueDistance, 0.01));
                
                float t = saturate((logDist - logMin) / (logMax - logMin));
                
                // Push colors toward warm end (red/yellow/green)
                // Values < 1.0 shift toward warm, > 1.0 shift toward cool
                t = pow(t, 3);

                // Get ZED-style color
                float3 color = GetZEDStyleColor(t);
                
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}

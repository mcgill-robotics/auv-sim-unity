Shader "Custom/TorpedoOutline"
{
    Properties
    {
        [MainColor] _OutlineColor("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRP" "RenderType"="Opaque" "Queue"="Transparent" }
        
        Pass
        {
            Name "OutlinePass"
            Cull Front // Render back faces
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                // Move vertex along normal to create the "hull"
                float3 posOS = input.positionOS + input.normalOS * _OutlineWidth;
                float3 positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

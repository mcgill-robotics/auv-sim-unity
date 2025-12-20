Shader "Hidden/XRayGhost"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _DashScale("Dash Scale", Float) = 300.0
        _FresnelPower("Fresnel Power", Float) = 2.0
        _Opacity("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRP" "RenderType"="Transparent" "Queue"="Transparent+100" }
        
        Pass
        {
            Name "XRayPass"
            
            // Critical: ZTest Greater renders when occluded
            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _DashScale;
                float _FresnelPower;
                float _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // 1. Dash Pattern (Screen-space)
                // In HDRP, we can calculate screen UV from SV_Position (pixel coordinates)
                // input.positionCS.xy is in pixel coordinates (0 to screenWidth/Height)
                float2 screenUV = input.positionCS.xy * _ScreenSize.zw; // _ScreenSize.zw is 1/width, 1/height
                
                // Adjust for aspect ratio to keep dashes square-ish
                screenUV.y *= (_ScreenSize.y * _ScreenSize.z); 
                
                // float dash = step(0.5, frac(screenUV.y * _DashScale));
                
                // Calculate dash for Y
                float dashY = step(0.5, frac(screenUV.y * _DashScale));

                // Calculate dash for X
                float dashX = step(0.5, frac(screenUV.x * _DashScale));

                // Multiply them to get the intersection (dots/blocks)
                // float dash = dashX * dashY;
                float dash = dashX || dashY;

                // 2. Fresnel Edge Glow
                float fresnel = 1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Combine Color, Dash, and Fresnel
                float4 finalColor = _BaseColor;
                finalColor.a = _Opacity * dash * (0.3 + 0.7 * fresnel);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}

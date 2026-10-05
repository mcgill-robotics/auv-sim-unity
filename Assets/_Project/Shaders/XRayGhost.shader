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
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent+100" }
        
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }
            
            // Critical: ZTest Greater renders when occluded
            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _DashScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _FresnelPower)
                UNITY_DEFINE_INSTANCED_PROP(float, _Opacity)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                float dashScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DashScale);
                float fresnelPower = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _FresnelPower);
                float opacity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Opacity);

                // 1. Dash Pattern (Screen-space)
                // In HDRP, we can calculate screen UV from SV_Position (pixel coordinates)
                // input.positionCS.xy is in pixel coordinates (0 to screenWidth/Height)
                float2 screenUV = input.positionCS.xy * _ScreenSize.zw; // _ScreenSize.zw is 1/width, 1/height
                
                // Adjust for aspect ratio to keep dashes square-ish
                screenUV.y *= (_ScreenSize.y * _ScreenSize.z); 
                
                // Calculate dash for Y
                float dashY = step(0.5, frac(screenUV.y * dashScale));

                // Calculate dash for X
                float dashX = step(0.5, frac(screenUV.x * dashScale));

                // Combine dash patterns using max for crosshatch grid
                float dash = max(dashX, dashY);

                // 2. Fresnel Edge Glow
                float fresnel = 1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                fresnel = pow(fresnel, fresnelPower);
                
                // Combine Color, Dash, and Fresnel
                float4 finalColor = baseColor;
                finalColor.a = baseColor.a * opacity * dash * (0.3 + 0.7 * fresnel);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}

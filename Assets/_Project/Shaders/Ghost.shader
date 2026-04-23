// Ghost.shader
Shader "Custom/Ghost"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            // Write nothing to color buffer
            ColorMask 0
            // Donwrite nothing to depth buffer
            ZWrite Off
            // draw on both sides of the mesh
            Cull Off

            CGPROGRAM
            // basic shader implmentation, vertex share is function vert and fragment shader is function frag
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f    { float4 pos : SV_POSITION; };

            // vertex shader, transform raw mesh vertex data into clip space data with built in unity standard matrix functions
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            // fragment shader, fixed4d is RGBA color, so we write 0,0,0,0 to make it fully transparent, but it doesn't matter because of the ColorMask 0
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0); // never actually written due to ColorMask 0
            }
            ENDCG
        }
    }
}
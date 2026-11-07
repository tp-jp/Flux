Shader "Flux/Tests/FluxKernel/VectorAdd"
{
    Properties
    {
        _MainTex ("Input A", 2D) = "black" {}
        _InputB ("Input B", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            sampler2D_float _MainTex;
            sampler2D_float _InputB;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 uv = FluxGetSourceUV(index);
                float4 a = tex2D(_MainTex, uv);
                float4 b = tex2D(_InputB, uv);

                return a + b;
            }

            ENDHLSL
        }
    }
}
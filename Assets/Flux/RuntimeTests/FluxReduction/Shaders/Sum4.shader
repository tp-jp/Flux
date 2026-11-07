Shader "Flux/Tests/Reduction/Sum4"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
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

            float4 frag(v2f_img i) : SV_Target
            {
                uint destinationIndex = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(destinationIndex))
                    return 0;

                uint sourceIndex = destinationIndex * 4;
                float4 result = 0;

                for (uint j = 0; j < 4; j++)
                {
                    uint index = sourceIndex + j;

                    if (FluxIsSourceValid(index))
                    {
                        result += tex2D(_MainTex, FluxGetSourceUV(index));
                    }
                }

                return result;
            }

            ENDHLSL
        }
    }
}
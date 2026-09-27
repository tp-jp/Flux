Shader "Flux/Tests/FluxKernel/InitIndex"
{
    Properties
    {
        _Scale ("Scale", Float) = 1
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

            float _Scale;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float value = index * _Scale;

                return float4(value, value, value, 1);
            }

            ENDHLSL
        }
    }
}
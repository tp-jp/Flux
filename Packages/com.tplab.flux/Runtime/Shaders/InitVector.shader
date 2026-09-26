Shader "Flux/InitVector"
{
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
            #include "FluxCommon.hlsl"

            float _Scale;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float value = index * _Scale;

                return float4(
                    value,
                    value,
                    value,
                    1);
            }

            ENDHLSL
        }
    }
}
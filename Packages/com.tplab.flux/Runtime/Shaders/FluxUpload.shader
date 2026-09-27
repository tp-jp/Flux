Shader "Flux/Upload"
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
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            float4 _FluxUploadData[1024];
            float _FluxUploadCount;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                if (index >= (uint)_FluxUploadCount)
                    return 0;

                return _FluxUploadData[index];
            }

            ENDHLSL
        }
    }
}
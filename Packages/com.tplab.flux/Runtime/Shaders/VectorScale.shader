Shader "Flux/VectorScale"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _Multiplier ("Multiplier", Float) = 1
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
            #include "FluxCommon.hlsl"

            sampler2D _MainTex;
            float _Multiplier;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float4 value = tex2D(_MainTex, i.uv);

                return value * _Multiplier;
            }

            ENDHLSL
        }
    }
}
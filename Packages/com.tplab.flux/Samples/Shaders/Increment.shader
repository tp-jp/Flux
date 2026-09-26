Shader "Flux/Samples/Increment"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _Increment ("Increment", Float) = 1
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

            sampler2D _MainTex;
            float _Increment;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float4 value = tex2D(_MainTex, i.uv);

                value.xyz += _Increment;

                return value;
            }

            ENDHLSL
        }
    }
}
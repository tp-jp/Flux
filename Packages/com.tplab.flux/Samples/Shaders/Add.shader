Shader "Flux/Samples/Add"
{
    Properties
    {
        _MainTex ("Input A", 2D) = "black" {}
        _Other ("Input B", 2D) = "black" {}
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
            sampler2D _Other;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float4 a = tex2D(_MainTex, i.uv);
                float4 b = tex2D(_Other, i.uv);

                return a + b;
            }

            ENDHLSL
        }
    }
}
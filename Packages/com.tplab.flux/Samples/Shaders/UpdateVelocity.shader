Shader "Flux/Samples/Particle/UpdateVelocity"
{
    Properties
    {
        _MainTex ("Velocity", 2D) = "black" {}
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

            float3 _Gravity;
            float _DeltaTime;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float4 velocity = tex2D(_MainTex, i.uv);

                velocity.xyz += _Gravity * _DeltaTime;

                return velocity;
            }

            ENDHLSL
        }
    }
}
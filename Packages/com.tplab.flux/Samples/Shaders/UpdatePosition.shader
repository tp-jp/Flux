Shader "Flux/Samples/Particle/UpdatePosition"
{
    Properties
    {
        _MainTex ("Position", 2D) = "black" {}
        _Velocity ("Velocity", 2D) = "black" {}
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
            sampler2D _Velocity;

            float _DeltaTime;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float4 position = tex2D(_MainTex, i.uv);
                float3 velocity = tex2D(_Velocity, i.uv).xyz;

                position.xyz += velocity * _DeltaTime;

                return position;
            }

            ENDHLSL
        }
    }
}
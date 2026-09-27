Shader "Flux/Samples/InitParticleState"
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

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float column = index % 16;
                float row = index / 16;

                float2 position;
                position.x = -4.0 + column * 0.5;
                position.y = 1.0 + row * 0.3;

                float direction = (index % 2) == 0 ? 1.0 : -1.0;

                float2 velocity;
                velocity.x = direction * (0.5 + (index % 5) * 0.15);
                velocity.y = 2.0 + (index % 7) * 0.2;

                return float4(
                    position.x,
                    position.y,
                    velocity.x,
                    velocity.y);
            }

            ENDHLSL
        }
    }
}
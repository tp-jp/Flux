Shader "Flux/Samples/UpdateParticleState"
{
    Properties
    {
        _MainTex ("State", 2D) = "black" {}
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

            float _DeltaTime;
            float _Gravity;
            float _Bounce;
            float _MinX;
            float _MaxX;
            float _FloorY;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetIndex(i.uv);

                if (!FluxIsValid(index))
                    return 0;

                float4 state = tex2D(_MainTex, i.uv);

                float2 position = state.rg;
                float2 velocity = state.ba;

                velocity.y += _Gravity * _DeltaTime;
                position += velocity * _DeltaTime;

                if (position.y < _FloorY)
                {
                    position.y = _FloorY;
                    velocity.y = abs(velocity.y) * _Bounce;
                }

                if (position.x < _MinX)
                {
                    position.x = _MinX;
                    velocity.x = abs(velocity.x);
                }

                if (position.x > _MaxX)
                {
                    position.x = _MaxX;
                    velocity.x = -abs(velocity.x);
                }

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
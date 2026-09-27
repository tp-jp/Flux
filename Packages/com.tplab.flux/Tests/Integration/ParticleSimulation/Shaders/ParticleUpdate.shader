Shader "Flux/Tests/Integration/ParticleSimulation/ParticleUpdate"
{
    Properties
    {
        _MainTex ("Particle State", 2D) = "black" {}
        _DeltaTime ("Delta Time", Float) = 0
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

            sampler2D_float _MainTex;
            float _DeltaTime;

            float4 frag(v2f_img i) : SV_Target
            {
                uint index = FluxGetDestinationIndex(i.uv);

                if (!FluxIsDestinationValid(index))
                    return 0;

                float2 uv = FluxGetSourceUV(index);
                float4 state = tex2D(_MainTex, uv);

                state.x += state.w * _DeltaTime;

                return state;
            }

            ENDHLSL
        }
    }
}
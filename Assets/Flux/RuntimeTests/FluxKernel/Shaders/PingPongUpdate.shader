Shader "Flux/Tests/PingPongUpdate"
{
    Properties
    {
        _MainTex ("Input data", 2D) = "black" {}
        _Scale ("Scale", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment UpdateData
            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"
            Texture2D<float4> _MainTex;
            float _Scale;

            float4 UpdateData(v2f_img input) : SV_Target
            {
                uint index = FluxGetDestinationIndex(input.uv);
                if (!FluxIsDestinationValid(index)) return 0;
                uint width = (uint)_FluxSourceWidth;
                // Feedback data is addressed by index; screen derivatives/filtering are irrelevant.
                return _MainTex.Load(int3(index % width, index / width, 0)) * _Scale;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

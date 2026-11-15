Shader "Flux/Tests/FluxKernel/BufferContracts"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _Value ("Signed value", Vector) = (0,0,0,0)
        _Start ("Start index", Float) = 0
        _Length ("Element count", Float) = 32768
        _SampleOffset ("Sample UV offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"
        sampler2D_float _MainTex;
        float4 _Value;
        float _Start;
        float _Length;
        float4 _SampleOffset;
        float4 Clear(v2f_img input) : SV_Target { return 0; }
        float4 Pattern(v2f_img input) : SV_Target
        {
            uint index = FluxGetDestinationIndex(input.uv);
            float2 uv = FluxGetDestinationUV(index);
            return float4(index, floor(uv * float2(_FluxDestinationWidth, _FluxDestinationHeight)), -(float)index);
        }
        float4 Copy(v2f_img input) : SV_Target
        {
            uint index = FluxGetDestinationIndex(input.uv);
            return tex2Dlod(_MainTex, float4(FluxGetSourceUV(index) + _SampleOffset.xy, 0, 0));
        }
        float4 Add(v2f_img input) : SV_Target
        {
            uint index = FluxGetDestinationIndex(input.uv);
            if (index < _Start || index >= _Start + _Length) discard;
            return _Value;
        }
        ENDHLSL
        Pass
        {
            Name "CLEAR"
            Blend Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Clear
            ENDHLSL
        }
        Pass
        {
            Name "PATTERN"
            Blend Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Pattern
            ENDHLSL
        }
        Pass
        {
            Name "COPY"
            Blend Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Copy
            ENDHLSL
        }
        Pass
        {
            Name "ADD"
            Blend One One
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Add
            ENDHLSL
        }
    }
    Fallback Off
}

Shader "Flux/Scatter"
{
    Properties
    {
        _FluxScatterPositions ("Cell coordinates (XYZ), valid if W > 0", 2D) = "black" {}
        _FluxScatterValues ("Signed float4 values", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "CLEAR"
            Blend Off
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex vert_img
            #pragma fragment Clear
            #include "UnityCG.cginc"
            float4 Clear(v2f_img input) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "ACCUMULATE"
            Blend One One
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex FluxScatterVertex
            #pragma hull FluxScatterHull
            #pragma domain FluxScatterDomain
            #pragma geometry FluxScatterGeometry
            #pragma fragment FluxScatterFragment
            #include "FluxScatter.hlsl"
            Texture2D<float4> _FluxScatterPositions;
            Texture2D<float4> _FluxScatterValues;

            // Both textures are row-major arrays; dimensions may differ. Load ignores filtering.
            FluxScatterInput FluxScatterRead(uint index)
            {
                FluxScatterInput result = (FluxScatterInput)0;
                uint pw, ph, vw, vh;
                _FluxScatterPositions.GetDimensions(pw, ph);
                _FluxScatterValues.GetDimensions(vw, vh);
                if (index >= pw * ph || index >= vw * vh) return result;
                float4 position = _FluxScatterPositions.Load(int3(index % pw, index / pw, 0));
                result.valid = position.w > 0;
                result.cellPosition = position.xyz;
                result.value = _FluxScatterValues.Load(int3(index % vw, index / vw, 0));
                return result;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

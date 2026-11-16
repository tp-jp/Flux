Shader "Flux/GridFilter"
{
    Properties
    {
        _MainTex ("Grid input", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            Name "FILTER_AXIS"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Filter
            #include "UnityCG.cginc"
            #include "FluxCommon.hlsl"
            #include "FluxGridFilter.hlsl"

            Texture2D<float4> _MainTex;
            float4 _FluxGridSize;
            float4 _FluxGridLayout;
            float4 _FluxGridFilterAxis;
            float4 _FluxGridFilterWeights;
            float _FluxGridFilterBoundary;

            float4 Filter(v2f_img input) : SV_Target
            {
                FluxGrid3DLayout grid = FluxGrid3DMakeLayout(_FluxGridSize, _FluxGridLayout);
                uint index = FluxGetDestinationIndex(input.uv);
                int2 pixel = int2(index % (uint)grid.atlasSize.x, index / (uint)grid.atlasSize.x);
                int3 cell = FluxGrid3DPixelToCell(grid, pixel);
                return FluxGridFilterAxis(_MainTex, grid, cell, (int3)_FluxGridFilterAxis.xyz,
                                          _FluxGridFilterWeights, _FluxGridFilterBoundary != 0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

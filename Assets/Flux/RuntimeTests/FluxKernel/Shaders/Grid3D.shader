Shader "Flux/Tests/FluxGrid3D"
{
    Properties
    {
        _FluxGridTexture ("Grid", 2D) = "black" {}
        _OtherTexture ("Custom prefix grid", 2D) = "black" {}
        _OtherSize ("Custom prefix size", Vector) = (1,1,1,0)
        _OtherLayout ("Custom prefix layout", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"
        #include "Packages/com.tplab.flux/Runtime/Shaders/FluxGrid3D.hlsl"
        Texture2D<float4> _FluxGridTexture;
        float4 _FluxGridSize;
        float4 _FluxGridLayout;
        float4 _Queries[64];

        float4 Fill(v2f_img input) : SV_Target
        {
            FluxGrid3DLayout grid = FluxGrid3DMakeLayout(_FluxGridSize, _FluxGridLayout);
            uint index = FluxGetDestinationIndex(input.uv);
            int2 pixel = int2(index % (uint)grid.atlasSize.x, index / (uint)grid.atlasSize.x);
            int3 c = FluxGrid3DPixelToCell(grid, pixel);
            if (!FluxGrid3DIsCellValid(grid, c)) return float4(12345, -23456, 34567, -45678);
            return float4(c.x + 10*c.y + 100*c.z + 1, -(1+c.x*c.x+3*c.y+7*c.z),
                          (c.x+1)*(c.y+2)*(c.z+3)*0.125, -4-c.z*c.z*0.25);
        }

        float4 Query(v2f_img input) : SV_Target
        {
            uint index = FluxGetDestinationIndex(input.uv);
            if (!FluxIsDestinationValid(index) || index >= 64) return 0;
            FluxGrid3DLayout grid = FluxGrid3DMakeLayout(_FluxGridSize, _FluxGridLayout);
            float4 query = _Queries[index];
            int mode = (int)query.w;
            if (mode == 0) return FluxGrid3DReadZero(_FluxGridTexture, grid, (int3)floor(query.xyz));
            if (mode == 1) return FluxGrid3DSampleLinearZero(_FluxGridTexture, grid, query.xyz);
            if (mode == 2) return FluxGrid3DSampleLinearClamp(_FluxGridTexture, grid, query.xyz);
            if (mode == 3) return FluxGrid3DSampleLinearClamp(_FluxGridTexture, grid, FluxGrid3DNormalizedToCell(grid, query.xyz));
            if (mode == 4) return FluxGrid3DSampleLinearZero(_FluxGridTexture, grid, FluxGrid3DNormalizedToCell(grid, query.xyz));
            if (mode == 5)
            {
                int3 cell = FluxGrid3DPixelToCell(grid, (int2)query.xy);
                return float4(cell, FluxGrid3DIsCellValid(grid, cell) ? 1 : 0);
            }
            int3 cell = (int3)query.xyz;
            return float4(FluxGrid3DCellToPixel(grid, cell), FluxGrid3DCellToUV(grid, cell));
        }
        ENDHLSL
        Pass
        {
            Name "FILL"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Fill
            ENDHLSL
        }
        Pass
        {
            Name "QUERY"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Query
            ENDHLSL
        }
    }
    Fallback Off
}

#ifndef FLUX_GRID_FILTER_INCLUDED
#define FLUX_GRID_FILTER_INCLUDED

#include "FluxGrid3D.hlsl"

// Layout and parameters must be validated by FluxGridFilter (or equivalently by the caller).
// Clamp only the coordinate. Zero-boundary reads never renormalize surviving weights.
float4 FluxGridFilterRead(Texture2D<float4> inputTexture, FluxGrid3DLayout grid, int3 cell, bool clampBoundary)
{
    if (clampBoundary) cell = clamp(cell, 0, grid.size - 1);
    return FluxGrid3DReadZero(inputTexture, grid, cell);
}

// axis is exactly one of (1,0,0), (0,1,0), (0,0,1).
// weights = center, each neighbor at distance 1, 2, 3. No implicit normalization.
// Integer loads prevent atlas tile leakage and make texture filter mode irrelevant.
float4 FluxGridFilterAxis(Texture2D<float4> inputTexture, FluxGrid3DLayout grid, int3 cell,
                          int3 axis, float4 weights, bool clampBoundary)
{
    if (!FluxGrid3DIsCellValid(grid, cell)) return 0;
    float4 result = 0;
    if (weights.x > 0) result = FluxGrid3DReadZero(inputTexture, grid, cell) * weights.x;
    [unroll] for (int distance = 1; distance <= 3; distance++)
    {
        float weight = weights[distance];
        if (weight > 0)
        {
            result += (FluxGridFilterRead(inputTexture, grid, cell - axis * distance, clampBoundary)
                     + FluxGridFilterRead(inputTexture, grid, cell + axis * distance, clampBoundary)) * weight;
        }
    }
    return result;
}

#endif

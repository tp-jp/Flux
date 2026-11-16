#ifndef FLUX_GRID3D_INCLUDED
#define FLUX_GRID3D_INCLUDED

// Use with integer Texture2D.Load support (Unity shader target 3.5 or higher).
// Layout must be validated by FluxGrid3D. No globals, samplers or data semantics are imposed.
// Example: FluxGrid3DMakeLayout(_FluxGridSize, _FluxGridLayout), bound by TryBind.
struct FluxGrid3DLayout
{
    int3 size;
    int columns;
    int2 atlasSize;
};

FluxGrid3DLayout FluxGrid3DMakeLayout(float4 gridSize, float4 atlasLayout)
{
    FluxGrid3DLayout grid;
    grid.size = (int3)gridSize.xyz;
    grid.columns = (int)atlasLayout.x;
    grid.atlasSize = (int2)atlasLayout.zw;
    return grid;
}

bool FluxGrid3DIsCellValid(FluxGrid3DLayout grid, int3 cell)
{
    return all(cell >= 0) && all(cell < grid.size);
}

// Valid cell -> integer atlas pixel. Caller validates the cell before using this mapping.
int2 FluxGrid3DCellToPixel(FluxGrid3DLayout grid, int3 cell)
{
    return int2((cell.z % grid.columns) * grid.size.x + cell.x,
                (cell.z / grid.columns) * grid.size.y + cell.y);
}

// Invalid pixels and unused tiles return (-1,-1,-1); padding is never a valid cell.
int3 FluxGrid3DPixelToCell(FluxGrid3DLayout grid, int2 pixel)
{
    if (any(pixel < 0) || any(pixel >= grid.atlasSize)) return int3(-1, -1, -1);
    int3 cell = int3(pixel.x % grid.size.x, pixel.y % grid.size.y,
                    (pixel.y / grid.size.y) * grid.columns + pixel.x / grid.size.x);
    return FluxGrid3DIsCellValid(grid, cell) ? cell : int3(-1, -1, -1);
}

// Valid cell -> texel center UV. Useful when a caller supplies its own gridTexture access.
float2 FluxGrid3DCellToUV(FluxGrid3DLayout grid, int3 cell)
{
    return (FluxGrid3DCellToPixel(grid, cell) + 0.5) / (float2)grid.atlasSize;
}

// Normalized volume edges [0,1] -> cell coordinates; integer coordinates are cell centers.
// Position/world transformations, units and density normalization belong to the caller.
float3 FluxGrid3DNormalizedToCell(FluxGrid3DLayout grid, float3 position)
{
    return position * (float3)grid.size - 0.5;
}

// Exact signed float4 read, independent of Texture filter mode. Outside cells read zero.
float4 FluxGrid3DReadZero(Texture2D<float4> gridTexture, FluxGrid3DLayout grid, int3 cell)
{
    if (!FluxGrid3DIsCellValid(grid, cell)) return 0;
    return gridTexture.Load(int3(FluxGrid3DCellToPixel(grid, cell), 0));
}

// Explicit eight-cell interpolation prevents 2D atlas filtering from crossing tile edges.
// Outside neighbors contribute zero. Finite coordinates at <=-1 or >=size have no support.
float4 FluxGrid3DSampleLinearZero(Texture2D<float4> gridTexture, FluxGrid3DLayout grid, float3 cellPosition)
{
    // Inspect exponent bits: floating-point isfinite checks can be removed by fast-math.
    if (any((asuint(cellPosition) & 0x7f800000u) == 0x7f800000u)) return 0;
    if (any(cellPosition <= -1) || any(cellPosition >= (float3)grid.size)) return 0;
    int3 lower = (int3)floor(cellPosition);
    float3 t = frac(cellPosition);
    float4 z0 = lerp(
        lerp(FluxGrid3DReadZero(gridTexture, grid, lower), FluxGrid3DReadZero(gridTexture, grid, lower + int3(1,0,0)), t.x),
        lerp(FluxGrid3DReadZero(gridTexture, grid, lower + int3(0,1,0)), FluxGrid3DReadZero(gridTexture, grid, lower + int3(1,1,0)), t.x), t.y);
    float4 z1 = lerp(
        lerp(FluxGrid3DReadZero(gridTexture, grid, lower + int3(0,0,1)), FluxGrid3DReadZero(gridTexture, grid, lower + int3(1,0,1)), t.x),
        lerp(FluxGrid3DReadZero(gridTexture, grid, lower + int3(0,1,1)), FluxGrid3DReadZero(gridTexture, grid, lower + int3(1,1,1)), t.x), t.y);
    return lerp(z0, z1, t.z);
}

// Clamp the coordinate to cell centers, then interpolate. Values themselves are never clamped.
// Nonfinite positions return zero, as in SampleLinearZero.
float4 FluxGrid3DSampleLinearClamp(Texture2D<float4> gridTexture, FluxGrid3DLayout grid, float3 cellPosition)
{
    if (any((asuint(cellPosition) & 0x7f800000u) == 0x7f800000u)) return 0;
    return FluxGrid3DSampleLinearZero(gridTexture, grid, clamp(cellPosition, 0, (float3)(grid.size - 1)));
}

#endif

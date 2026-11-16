#ifndef FLUX_SCATTER_INCLUDED
#define FLUX_SCATTER_INCLUDED

#include "UnityCG.cginc"
#include "FluxGrid3D.hlsl"

// Camera-free Blit backend: requires tessellation + geometry instancing (SM5).
// The supplied Shader targets D3D11 only; this is not a requirement of Flux itself.
// Use target 5.0, vertex FluxScatterVertex, hull FluxScatterHull,
// domain FluxScatterDomain, geometry FluxScatterGeometry, fragment FluxScatterFragment.
// Required render state: Cull Off ZWrite Off ZTest Always Blend One One.
// Clear separately. No input texture layout, selection rule or value unit is imposed here.
struct FluxScatterInput
{
    float3 cellPosition; // Integer positions are cell centers, same convention as FluxGrid3D.
    float4 value;        // Signed, finite data; no saturation, alpha test or normalization.
    bool valid;
};

// Implement after this include. Called only for [inputStart, inputStart + inputCount).
// Read existing GPU data directly; no intermediate packing pass is required.
// Must not sample the destination atlas. Nonfinite positions are ignored.
FluxScatterInput FluxScatterRead(uint index);

float4 _FluxGridSize;
float4 _FluxGridLayout;
float4 _FluxScatterRange; // start, count; validated by FluxScatter (max 131072 per draw).

struct FluxScatterControl { float4 position : INTERNALTESSPOS; };
struct FluxScatterFactors { float edge[4] : SV_TessFactor; float inside[2] : SV_InsideTessFactor; };
struct FluxScatterSlot { float4 position : SV_POSITION; uint index : TEXCOORD0; };
struct FluxScatterPixel { float4 position : SV_POSITION; nointerpolation float4 value : TEXCOORD0; };

uint FluxScatterSide()
{
    // 32 GS instances, each covering a square with 2..64 points per side.
    return max(2u, (uint)ceil(sqrt(ceil(_FluxScatterRange.y / 32.0))));
}

FluxScatterControl FluxScatterVertex(float4 vertex : POSITION)
{
    FluxScatterControl output;
    output.position = vertex;
    return output;
}

FluxScatterFactors FluxScatterPatch(InputPatch<FluxScatterControl, 3> patch, uint patchId : SV_PrimitiveID)
{
    FluxScatterFactors output;
    // Blit supplies two triangles. Only the first patch generates work.
    float segments = patchId == 0 ? FluxScatterSide() - 1 : 0;
    output.edge[0] = output.edge[1] = output.edge[2] = output.edge[3] = segments;
    output.inside[0] = output.inside[1] = segments;
    return output;
}

[domain("quad")]
[partitioning("integer")]
[outputtopology("point")]
[outputcontrolpoints(3)]
[patchconstantfunc("FluxScatterPatch")]
[maxtessfactor(63.0)]
FluxScatterControl FluxScatterHull(InputPatch<FluxScatterControl, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

[domain("quad")]
FluxScatterSlot FluxScatterDomain(FluxScatterFactors factors, float2 uv : SV_DomainLocation,
                                 const OutputPatch<FluxScatterControl, 3> patch)
{
    FluxScatterSlot output;
    uint side = FluxScatterSide();
    uint2 cell = (uint2)round(uv * (side - 1));
    output.position = float4(0, 0, 0, 1);
    output.index = cell.y * side + cell.x;
    return output;
}

[maxvertexcount(24)]
[instance(32)]
void FluxScatterGeometry(point FluxScatterSlot input[1], uint invocation : SV_GSInstanceID,
                         inout TriangleStream<FluxScatterPixel> stream)
{
    uint side = FluxScatterSide();
    uint offset = input[0].index + invocation * side * side;
    if (offset >= (uint)_FluxScatterRange.y) return;
    FluxScatterInput item = FluxScatterRead((uint)_FluxScatterRange.x + offset);
    if (!item.valid) return;
    // Integer exponent test survives fast-math optimizations of floating-point isfinite.
    if (any((asuint(item.cellPosition) & 0x7f800000u) == 0x7f800000u)) return;
    FluxGrid3DLayout grid = FluxGrid3DMakeLayout(_FluxGridSize, _FluxGridLayout);
    if (any(item.cellPosition <= -1) || any(item.cellPosition >= (float3)grid.size)) return;
    int3 lower = (int3)floor(item.cellPosition);
    float3 f = frac(item.cellPosition);
    [unroll] for (int z = 0; z < 2; z++)
    [unroll] for (int y = 0; y < 2; y++)
    [unroll] for (int x = 0; x < 2; x++)
    {
        int3 cell = lower + int3(x, y, z);
        if (!FluxGrid3DIsCellValid(grid, cell)) continue;
        float weight = (x == 0 ? 1-f.x : f.x) * (y == 0 ? 1-f.y : f.y) * (z == 0 ? 1-f.z : f.z);
        if (weight == 0) continue;
        float2 pixel = FluxGrid3DCellToPixel(grid, cell);
        FluxScatterPixel output;
        output.value = item.value * weight;
        // One triangle covers exactly one non-MSAA texel center.
        [unroll] for (int corner = 0; corner < 3; corner++)
        {
            float2 xy = pixel + (corner == 2 ? float2(0.5, 1) : float2(corner, 0));
            float2 ndc = xy / (float2)grid.atlasSize * 2 - 1;
            #if UNITY_UV_STARTS_AT_TOP
                ndc.y = -ndc.y;
            #endif
            output.position = float4(ndc, 0, 1);
            stream.Append(output);
        }
        stream.RestartStrip();
    }
}

float4 FluxScatterFragment(FluxScatterPixel input) : SV_Target { return input.value; }

#endif

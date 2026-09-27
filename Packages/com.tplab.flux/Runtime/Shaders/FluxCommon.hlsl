#ifndef FLUX_COMMON_INCLUDED
#define FLUX_COMMON_INCLUDED

float _FluxCount;
float _FluxWidth;
float _FluxHeight;

uint FluxGetIndex(float2 uv)
{
    uint x = (uint)(uv.x * _FluxWidth);
    uint y = (uint)(uv.y * _FluxHeight);

    return y * (uint)_FluxWidth + x;
}

float2 FluxGetUV(uint index)
{
    uint width = (uint)_FluxWidth;
    uint x = index % width;
    uint y = index / width;

    return float2(
        (x + 0.5) / _FluxWidth,
        (y + 0.5) / _FluxHeight);
}

bool FluxIsValid(uint index)
{
    return index < (uint)_FluxCount;
}

#endif
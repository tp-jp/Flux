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

bool FluxIsValid(uint index)
{
    return index < (uint)_FluxCount;
}

#endif
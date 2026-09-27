#ifndef FLUX_COMMON_INCLUDED
#define FLUX_COMMON_INCLUDED

float _FluxSourceCount;
float _FluxSourceWidth;
float _FluxSourceHeight;

float _FluxDestinationCount;
float _FluxDestinationWidth;
float _FluxDestinationHeight;

uint FluxGetDestinationIndex(float2 uv)
{
    uint x = (uint)(uv.x * _FluxDestinationWidth);
    uint y = (uint)(uv.y * _FluxDestinationHeight);

    return y * (uint)_FluxDestinationWidth + x;
}

float2 FluxGetDestinationUV(uint index)
{
    uint width = (uint)_FluxDestinationWidth;
    uint x = index % width;
    uint y = index / width;

    return float2(
        (x + 0.5) / _FluxDestinationWidth,
        (y + 0.5) / _FluxDestinationHeight);
}

bool FluxIsDestinationValid(uint index)
{
    return index < (uint)_FluxDestinationCount;
}

float2 FluxGetSourceUV(uint index)
{
    uint width = (uint)_FluxSourceWidth;
    uint x = index % width;
    uint y = index / width;

    return float2(
        (x + 0.5) / _FluxSourceWidth,
        (y + 0.5) / _FluxSourceHeight);
}

bool FluxIsSourceValid(uint index)
{
    return index < (uint)_FluxSourceCount;
}

#endif
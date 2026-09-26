#ifndef FLUX_COMMON_INCLUDED
#define FLUX_COMMON_INCLUDED

float _FluxCount;
float4 _MainTex_TexelSize;

uint FluxGetIndex(float2 uv)
{
    uint x = (uint)(uv.x * _MainTex_TexelSize.z);
    uint y = (uint)(uv.y * _MainTex_TexelSize.w);

    return y * (uint)_MainTex_TexelSize.z + x;
}

bool FluxIsValid(uint index)
{
    return index < (uint)_FluxCount;
}

#endif
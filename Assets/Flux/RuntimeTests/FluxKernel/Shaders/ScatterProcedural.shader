Shader "Flux/Tests/ScatterProcedural"
{
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
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxScatter.hlsl"

            // Independent input adapter: no input Texture or packing Pass.
            FluxScatterInput FluxScatterRead(uint index)
            {
                FluxScatterInput item;
                item.cellPosition = float3(index % 11, (index / 11) % 23, (index / 253) % 31) * 0.25 - 0.5;
                item.value = float4(((int)(index % 7) - 3) * 0.125, ((int)(index % 5) - 2) * 0.25,
                                    (index % 3 + 1) * 0.5, index % 2 == 0 ? -0.25 : 0);
                item.valid = index % 13 != 0;
                return item;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

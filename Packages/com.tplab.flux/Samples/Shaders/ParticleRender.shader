Shader "Flux/Samples/ParticleRender"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "black" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"

            sampler2D _PositionTex;
            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 particle : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                uint index = (uint)v.particle.x;
                float2 bufferUV = FluxGetDestinationUV(index);

                float3 particlePosition =
                    tex2Dlod(
                        _PositionTex,
                        float4(bufferUV, 0, 0)).xyz;

                float3 position =
                    particlePosition + v.vertex.xyz;

                o.vertex = UnityObjectToClipPos(
                    float4(position, 1));

                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return _Color;
            }

            ENDHLSL
        }
    }
}
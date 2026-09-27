Shader "Flux/Samples/ParticleStateRender"
{
    Properties
    {
        _StateTex ("State", 2D) = "black" {}
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

            sampler2D _StateTex;
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
                float2 bufferUV = FluxGetUV(index);
                float2 position = tex2Dlod(_StateTex, float4(bufferUV, 0, 0)).rg;

                float3 vertexPosition = v.vertex.xyz;
                vertexPosition.xy += position;

                o.vertex = UnityObjectToClipPos(float4(vertexPosition, 1));
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
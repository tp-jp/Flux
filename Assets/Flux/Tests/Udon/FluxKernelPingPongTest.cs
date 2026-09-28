using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernelPingPongTest : FluxRuntimeTestBase
    {
        const int TestCount = 16;
        const float Scale = 2f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxKernel initKernel;

        [SerializeField]
        FluxKernel vectorScaleKernel;

        [SerializeField]
        FluxReadback readback;

        protected override string TestName => "FluxKernel / Ping Pong";

        public override void Run()
        {
            initKernel.SetFloat("_Scale", 1f);
            initKernel.Dispatch(bufferA);

            vectorScaleKernel.SetFloat("_Scale", Scale);

            vectorScaleKernel.Dispatch(bufferA, bufferB);
            vectorScaleKernel.Dispatch(bufferB, bufferA);
            vectorScaleKernel.Dispatch(bufferA, bufferB);

            readback.Request(bufferB, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            for (var i = 0; i < TestCount; i++)
            {
                var value = i * Scale * Scale * Scale;
                var expected = new Vector4(value, value, value, Scale * Scale * Scale);
                var actual = (Vector4)data[i];

                if (!Approximately(actual, expected))
                {
                    Fail($"Index {i}: expected {expected}, actual {actual}");
                    return;
                }
            }

            Pass();
        }

        bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Abs(a.x - b.x) <= Tolerance &&
                   Mathf.Abs(a.y - b.y) <= Tolerance &&
                   Mathf.Abs(a.z - b.z) <= Tolerance &&
                   Mathf.Abs(a.w - b.w) <= Tolerance;
        }
    }
}
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernelTest : FluxRuntimeTestBase
    {
        const int TestCount = 16;
        const float InitScale = 2f;
        const float VectorScale = 10f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer sourceBuffer;

        [SerializeField]
        FluxBuffer destinationBuffer;

        [SerializeField]
        FluxKernel initKernel;

        [SerializeField]
        FluxKernel vectorScaleKernel;

        [SerializeField]
        FluxReadback readback;

        protected override string TestName => "FluxKernel / Basic";

        public override void Run()
        {
            initKernel.SetFloat("_Scale", InitScale);
            initKernel.Dispatch(sourceBuffer);

            vectorScaleKernel.SetFloat("_Scale", VectorScale);
            vectorScaleKernel.Dispatch(sourceBuffer, destinationBuffer);

            readback.Request(destinationBuffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            for (var i = 0; i < TestCount; i++)
            {
                var value = i * InitScale * VectorScale;
                var expected = new Vector4(value, value, value, VectorScale);
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
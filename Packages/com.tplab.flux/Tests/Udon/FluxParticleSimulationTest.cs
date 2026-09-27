using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    public class FluxParticleSimulationTest : FluxRuntimeTestBase
    {
        const int TestCount = 64;
        const int StepCount = 4;
        const float DeltaTime = 0.5f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxKernel updateKernel;

        [SerializeField]
        FluxReadback readback;

        protected override string TestName => "Integration / Particle Simulation";

        public override void Run()
        {
            var data = new Vector4[TestCount];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(i, 0, 0, i + 1);
            }

            upload.Upload(data, bufferA);

            updateKernel.SetFloat("_DeltaTime", DeltaTime);

            for (var i = 0; i < StepCount; i++)
            {
                if (i % 2 == 0)
                {
                    updateKernel.Dispatch(bufferA, bufferB);
                }
                else
                {
                    updateKernel.Dispatch(bufferB, bufferA);
                }
            }

            readback.Request(bufferA, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            for (var i = 0; i < TestCount; i++)
            {
                var expectedX = i + (i + 1) * DeltaTime * StepCount;
                var expected = new Vector4(expectedX, 0, 0, i + 1);
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
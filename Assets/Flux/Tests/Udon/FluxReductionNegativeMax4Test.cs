using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    public class FluxReductionNegativeMax4Test : FluxRuntimeTestBase
    {
        const int TestCount = 250;
        const float ExpectedResult = -1f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer inputBuffer;

        [SerializeField]
        FluxBuffer resultBuffer;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxReduction reduction;

        [SerializeField]
        FluxReadback readback;

        protected override string TestName => "FluxReduction / Negative Max4";

        public override void Run()
        {
            var data = new Vector4[TestCount];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(-(i + 1), 0, 0, 0);
            }

            upload.Upload(data, inputBuffer);
            reduction.Reduce(inputBuffer, resultBuffer);
            readback.Request(resultBuffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var actual = readback.Data[0].r;

            if (Mathf.Abs(actual - ExpectedResult) <= Tolerance)
            {
                Pass();
                return;
            }

            Fail($"Expected {ExpectedResult}, actual {actual}");
        }
    }
}
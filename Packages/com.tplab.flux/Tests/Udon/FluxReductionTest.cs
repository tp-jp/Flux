using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReductionTest : UdonSharpBehaviour
    {
        const int TestCount = 250;
        const float ExpectedResult = 31375f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxRuntimeTestRunner runner;

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

        public void _RunTest()
        {
            var data = new Vector4[TestCount];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(i + 1, 0, 0, 0);
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
                runner.Pass("Reduction Sum");
                return;
            }

            runner.Fail("Reduction Sum", $"Expected {ExpectedResult}, actual {actual}");
        }
    }
}
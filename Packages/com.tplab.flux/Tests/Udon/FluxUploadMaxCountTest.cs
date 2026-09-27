using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    public class FluxUploadMaxCountTest : FluxRuntimeTestBase
    {
        const int TestCount = 512;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxReadback readback;

        [SerializeField]
        FluxBuffer buffer;

        Vector4[] _expected;

        protected override string TestName => "FluxUpload Max Count";

        public override void Run()
        {
            buffer.SetCount(TestCount);

            _expected = new Vector4[TestCount];

            for (var i = 0; i < TestCount; i++)
            {
                _expected[i] = new Vector4(i, i + 1, i + 2, i + 3);
            }

            upload.Upload(_expected, buffer);
            readback.Request(buffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            if (readback.Count != TestCount)
            {
                Fail($"Count mismatch. Expected={TestCount}, Actual={readback.Count}");
                return;
            }

            for (var i = 0; i < TestCount; i++)
            {
                var actual = (Vector4)data[i];
                if (actual != _expected[i])
                {
                    Fail($"Data mismatch at {i}. Expected={_expected[i]}, Actual={actual}");
                    return;
                }
            }

            Pass();
        }
    }
}
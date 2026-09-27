using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxUploadTest : FluxRuntimeTestBase
    {
        const int TestCount = 16;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer buffer;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxReadback readback;

        Vector4[] _expected;

        protected override string TestName => "FluxUpload / Basic";

        public override void Run()
        {
            _expected = new Vector4[TestCount];

            for (var i = 0; i < TestCount; i++)
            {
                _expected[i] = new Vector4(
                    i,
                    i * 2f,
                    i * 3f,
                    i * 4f);
            }

            upload.Upload(_expected, buffer);
            readback.Request(buffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            for (var i = 0; i < TestCount; i++)
            {
                var expected = _expected[i];
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
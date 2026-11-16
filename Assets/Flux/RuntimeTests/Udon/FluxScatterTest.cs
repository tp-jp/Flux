using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// クリア、分割加算、再加算、再クリアをUdon VMとGPU読み戻しで検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxScatterTest : FluxRuntimeTestBase
    {
        [SerializeField]
        FluxScatter scatter;

        [SerializeField]
        FluxBuffer output;

        [SerializeField]
        FluxReadback readback;

        int _stage;
        Vector4[] _expected;

        protected override string TestName => "FluxScatter / Signed values, split accumulation and clear";

        public override void Run()
        {
            _stage = 0;
            _expected = new Vector4[135];
            // CPU reference: visit all valid cells for each input, not the GPU neighbor loop.
            for (var i = 7; i < 520; i++)
            {
                if (i % 13 == 0) continue;
                var p = new Vector3(i % 11, (i / 11) % 23, (i / 253) % 31) * 0.25f - Vector3.one * 0.5f;
                var value = new Vector4((i % 7 - 3) * 0.125f, (i % 5 - 2) * 0.25f, (i % 3 + 1) * 0.5f, i % 2 == 0 ? -0.25f : 0);
                for (var z = 0; z < 7; z++)
                for (var y = 0; y < 5; y++)
                for (var x = 0; x < 3; x++)
                {
                    var weight = Mathf.Max(0, 1 - Mathf.Abs(p.x - x)) * Mathf.Max(0, 1 - Mathf.Abs(p.y - y)) * Mathf.Max(0, 1 - Mathf.Abs(p.z - z));
                    var index = ((z / 3) * 5 + y) * 9 + (z % 3) * 3 + x;
                    _expected[index] += value * weight;
                }
            }
            if (!scatter.TryClear(output, 0)) { Fail("Initial clear rejected."); return; }
            readback.Request(output, this);
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded || readback.Data.Length < 135) { Fail("Readback failed."); return; }
            var data = readback.Data;
            var multiplier = _stage == 1 ? 1 : (_stage == 2 ? 2 : 0);
            for (var i = 0; i < 135; i++)
            {
                var expected = _expected[i] * multiplier;
                var actual = (Vector4)data[i];
                if (!(Mathf.Abs(actual.x - expected.x) <= 0.002f && Mathf.Abs(actual.y - expected.y) <= 0.002f &&
                    Mathf.Abs(actual.z - expected.z) <= 0.002f && Mathf.Abs(actual.w - expected.w) <= 0.002f))
                {
                    Fail($"Stage {_stage}, texel {i}: expected {expected}, actual {actual}");
                    return;
                }
            }
            if (output.Count != 135) { Fail("Scatter changed Count."); return; }
            if (_stage == 0)
            {
                if (!scatter.TryAccumulate(output, 7, 129, 1) || !scatter.TryAccumulate(output, 136, 384, 1))
                {
                    Fail("Split accumulation rejected.");
                    return;
                }
            }
            else if (_stage == 1)
            {
                if (!scatter.TryAccumulate(output, 0, 0, 1) || !scatter.TryAccumulate(output, 7, 513, 1))
                {
                    Fail("Repeated accumulation rejected.");
                    return;
                }
            }
            else if (_stage == 2)
            {
                if (!scatter.TryClear(output, 0)) { Fail("Final clear rejected."); return; }
            }
            else
            {
                Pass();
                return;
            }
            _stage++;
            readback.Request(output, this);
        }
    }
}

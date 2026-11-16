using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// 各軸の3・5・7点平滑化、Zero／Clamp境界をCPU参照値とUdon VM上で比較します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxGridFilterTest : FluxRuntimeTestBase
    {
        [SerializeField]
        FluxGrid3D grid;

        [SerializeField]
        FluxGridFilter filter;

        [SerializeField]
        FluxKernel seedKernel;

        [SerializeField]
        FluxBuffer source;

        [SerializeField]
        FluxBuffer destination;

        [SerializeField]
        FluxReadback readback;

        int _stage;
        Vector4[] _expected;
        FluxBuffer _current;
        FluxBuffer _next;

        protected override string TestName => "FluxGridFilter / Axis weights and Zero/Clamp boundaries";

        public override void Run()
        {
            _stage = 0;
            _current = source;
            _next = destination;
            _expected = new Vector4[105];
            for (var z = 0; z < 7; z++)
            for (var y = 0; y < 5; y++)
            for (var x = 0; x < 3; x++)
            {
                _expected[(z * 5 + y) * 3 + x] = new Vector4(x + 10 * y + 100 * z + 1, -(1 + x * x + 3 * y + 7 * z),
                    (x + 1) * (y + 2) * (z + 3) * 0.125f, -4 - z * z * 0.25f);
            }
            if (!grid.TryBind(seedKernel, _current)) { Fail("Seed binding failed."); return; }
            seedKernel.Dispatch(_current, 0);
            RunAxis();
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded || readback.Data.Length < 135) { Fail("Readback failed."); return; }
            var data = readback.Data;
            for (var py = 0; py < 15; py++)
            for (var px = 0; px < 9; px++)
            {
                var z = py / 5 * 3 + px / 3;
                var expected = z < 7 ? _expected[(z * 5 + py % 5) * 3 + px % 3] : Vector4.zero;
                var actual = (Vector4)data[py * 9 + px];
                if (!(Mathf.Abs(actual.x - expected.x) <= 0.002f && Mathf.Abs(actual.y - expected.y) <= 0.002f &&
                    Mathf.Abs(actual.z - expected.z) <= 0.002f && Mathf.Abs(actual.w - expected.w) <= 0.002f))
                {
                    Fail($"Stage {_stage}, pixel ({px},{py}): expected {expected}, actual {actual}");
                    return;
                }
            }
            if (source.Count != 135 || destination.Count != 135) { Fail("Filter changed Count."); return; }
            _stage++;
            if (_stage == 6) { Pass(); return; }
            var swap = _current;
            _current = _next;
            _next = swap;
            RunAxis();
        }

        void RunAxis()
        {
            var axis = _stage % 3;
            var boundary = _stage < 3 ? FluxGridBoundaryMode.Zero : FluxGridBoundaryMode.Clamp;
            var weights = axis == 0 ? new Vector4(0.5f, 0.25f, 0, 0) :
                axis == 1 ? new Vector4(6f/16, 4f/16, 1f/16, 0) : new Vector4(20f/64, 15f/64, 6f/64, 1f/64);
            var next = new Vector4[105];
            for (var z = 0; z < 7; z++)
            for (var y = 0; y < 5; y++)
            for (var x = 0; x < 3; x++)
            for (var offset = -3; offset <= 3; offset++)
            {
                var sx = x + (axis == 0 ? offset : 0);
                var sy = y + (axis == 1 ? offset : 0);
                var sz = z + (axis == 2 ? offset : 0);
                if (boundary == FluxGridBoundaryMode.Clamp)
                {
                    sx = Mathf.Clamp(sx, 0, 2); sy = Mathf.Clamp(sy, 0, 4); sz = Mathf.Clamp(sz, 0, 6);
                }
                if (sx < 0 || sy < 0 || sz < 0 || sx >= 3 || sy >= 5 || sz >= 7) continue;
                next[(z * 5 + y) * 3 + x] += _expected[(sz * 5 + sy) * 3 + sx] * weights[Mathf.Abs(offset)];
            }
            _expected = next;
            if (!filter.TryFilter(_current, _next, (FluxGridAxis)axis, boundary, weights, 0))
            {
                Fail("Filter dispatch rejected.");
                return;
            }
            readback.Request(_next, this);
        }
    }
}

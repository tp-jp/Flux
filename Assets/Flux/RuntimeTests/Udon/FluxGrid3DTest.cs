using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// 3Dグリッドの配置・境界・補間をUdon VMとGPU読み戻しで検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxGrid3DTest : FluxRuntimeTestBase
    {
        [SerializeField]
        FluxGrid3D grid;

        [SerializeField]
        FluxBuffer atlas;

        [SerializeField]
        FluxBuffer output;

        [SerializeField]
        FluxKernel kernel;

        [SerializeField]
        FluxReadback readback;

        int _stage;
        Vector4[] _queries;
        int _queryCount;

        protected override string TestName => "FluxGrid3D / Atlas, zero boundary and trilinear clamp";

        public override void Run()
        {
            _stage = 0;
            if (!grid.ValidateLayout() || grid.CellCount != 105 || grid.AtlasWidth != 9 || grid.AtlasHeight != 15 ||
                grid.AtlasCapacity != 135 || grid.AtlasColumns != 3 || grid.AtlasRows != 3)
            {
                Fail("Grid layout must be 3x5x7 cells in a 9x15 atlas.");
                return;
            }
            for (var z = 0; z < 7; z++)
            for (var y = 0; y < 5; y++)
            for (var x = 0; x < 3; x++)
            {
                var pixel = grid.GetPixel(x, y, z);
                var expectedX = (z % 3) * 3 + x;
                var expectedY = (z / 3) * 5 + y;
                var cell = grid.GetCell(pixel.x, pixel.y);
                var uv = grid.GetUV(x, y, z);
                if (pixel.x != expectedX || pixel.y != expectedY || cell.x != x || cell.y != y || cell.z != z ||
                    grid.GetAtlasIndex(x, y, z) != expectedY * 9 + expectedX ||
                    Mathf.Abs(uv.x - (expectedX + 0.5f) / 9) > 0.00001f || Mathf.Abs(uv.y - (expectedY + 0.5f) / 15) > 0.00001f)
                {
                    Fail("CPU atlas mapping did not round-trip.");
                    return;
                }
            }
            if (grid.GetCell(3, 10).x != -1 || grid.GetAtlasIndex(-1, 0, 0) != -1)
            {
                Fail("Padding and outside coordinates must be invalid.");
                return;
            }
            if (!grid.TryBind(kernel, atlas))
            {
                Fail("Grid binding failed.");
                return;
            }
            kernel.Dispatch(atlas, 0);
            readback.Request(atlas, this);
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded)
            {
                Fail("GPU readback failed.");
                return;
            }
            var data = readback.Data;
            var count = _stage == 0 ? 135 : _queryCount;
            for (var i = 0; i < count; i++)
            {
                Vector4 expected;
                if (_stage == 0)
                {
                    var px = i % 9;
                    var py = i / 9;
                    var z = (py / 5) * 3 + px / 3;
                    expected = z < 7 ? Value(px % 3, py % 5, z) : new Vector4(12345, -23456, 34567, -45678);
                }
                else
                {
                    expected = Expected(_queries[i]);
                }
                var actual = (Vector4)data[i];
                if (!(Mathf.Abs(actual.x - expected.x) <= 0.001f && Mathf.Abs(actual.y - expected.y) <= 0.001f &&
                    Mathf.Abs(actual.z - expected.z) <= 0.001f && Mathf.Abs(actual.w - expected.w) <= 0.001f))
                {
                    Fail($"Stage {_stage}, index {i}: expected {expected}, actual {actual}");
                    return;
                }
            }
            if (_stage == 0)
            {
                _stage = 1;
                PrepareQueries();
                output.SetCount(_queryCount);
                kernel.SetVectorArray("_Queries", _queries);
                kernel.Dispatch(output, 1);
                readback.Request(output, this);
                return;
            }
            if (atlas.Count != 135 || output.Count != _queryCount)
            {
                Fail("Grid operations changed Buffer Count.");
                return;
            }
            Pass();
        }

        void PrepareQueries()
        {
            _queries = new Vector4[64];
            _queryCount = 0;
            var positions = new[] { Vector3.zero, new Vector3(2, 4, 6), new Vector3(0.25f, 0.75f, 2.5f),
                new Vector3(2.5f, 4, 6), new Vector3(-0.5f, 0, 0), new Vector3(0, -0.5f, 0),
                new Vector3(0, 0, -0.5f), new Vector3(2, 4.5f, 6), new Vector3(2, 4, 6.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(2.5f, 4.5f, 6.5f),
                new Vector3(-1000, 0, 0), new Vector3(0, 1000, 0), new Vector3(0, 0, 1000) };
            for (var i = 0; i < positions.Length; i++)
            for (var mode = 0; mode <= 2; mode++)
            {
                var p = positions[i];
                _queries[_queryCount++] = new Vector4(p.x, p.y, p.z, mode);
            }
            _queries[_queryCount++] = new Vector4(0, 0, 0, 3);
            _queries[_queryCount++] = new Vector4(1, 1, 1, 3);
            _queries[_queryCount++] = new Vector4(0, 0, 0, 4);
            _queries[_queryCount++] = new Vector4(1, 1, 1, 4);
            _queries[_queryCount++] = new Vector4(0.5f, 0.5f, 0.5f, 3);
        }

        Vector4 Expected(Vector4 query)
        {
            var mode = (int)query.w;
            var p = new Vector3(query.x, query.y, query.z);
            if (mode == 0) return ReadCell(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
            if (mode >= 3) { p = Vector3.Scale(p, new Vector3(3, 5, 7)) - Vector3.one * 0.5f; }
            if (mode == 2 || mode == 3) { p = new Vector3(Mathf.Clamp(p.x, 0, 2), Mathf.Clamp(p.y, 0, 4), Mathf.Clamp(p.z, 0, 6)); }
            var bx = Mathf.FloorToInt(p.x);
            var by = Mathf.FloorToInt(p.y);
            var bz = Mathf.FloorToInt(p.z);
            var f = p - new Vector3(bx, by, bz);
            var result = Vector4.zero;
            for (var dz = 0; dz < 2; dz++)
            for (var dy = 0; dy < 2; dy++)
            for (var dx = 0; dx < 2; dx++)
            {
                var weight = (dx == 0 ? 1 - f.x : f.x) * (dy == 0 ? 1 - f.y : f.y) * (dz == 0 ? 1 - f.z : f.z);
                result += ReadCell(bx + dx, by + dy, bz + dz) * weight;
            }
            return result;
        }

        Vector4 ReadCell(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= 3 || y >= 5 || z >= 7) return Vector4.zero;
            return Value(x, y, z);
        }

        Vector4 Value(int x, int y, int z)
        {
            return new Vector4(x + 10 * y + 100 * z + 1, -(1 + x * x + 3 * y + 7 * z), (x + 1) * (y + 2) * (z + 3) * 0.125f, -4 - z * z * 0.25f);
        }
    }
}

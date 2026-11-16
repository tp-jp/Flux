using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TpLab.Flux.Udon;
using UnityEngine;
using UnityEngine.TestTools;

namespace TpLab.Flux.Tests.Editor
{
    public class FluxGrid3DTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<Material> _materials = new List<Material>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) { Object.DestroyImmediate(go); }
            foreach (var material in _materials) { Object.DestroyImmediate(material); }
            _objects.Clear();
            _materials.Clear();
        }

        [TestCase(64, 64, 64, 8, 512, 512)]
        [TestCase(3, 5, 7, 3, 9, 15)]
        [TestCase(3, 5, 7, 0, 9, 15)]
        [TestCase(3, 5, 7, 1, 3, 35)]
        [TestCase(3, 5, 7, 7, 21, 5)]
        [TestCase(1, 1, 1, 0, 1, 1)]
        public void LayoutAndAllCellMappingsRoundTrip(int x, int y, int z, int columns, int width, int height)
        {
            var grid = Grid(x, y, z, columns);
            Assert.That(grid.ValidateLayout(), Is.True);
            Assert.That(grid.Size, Is.EqualTo(new Vector3Int(x, y, z)));
            Assert.That(grid.CellCount, Is.EqualTo(x * y * z));
            Assert.That(grid.AtlasWidth, Is.EqualTo(width));
            Assert.That(grid.AtlasHeight, Is.EqualTo(height));
            Assert.That(grid.AtlasCapacity, Is.EqualTo(width * height));
            var resolvedColumns = width / x;
            var seen = new HashSet<int>();
            for (var iz = 0; iz < z; iz++)
            for (var iy = 0; iy < y; iy++)
            for (var ix = 0; ix < x; ix++)
            {
                var px = (iz % resolvedColumns) * x + ix;
                var py = (iz / resolvedColumns) * y + iy;
                Assert.That(grid.GetPixel(ix, iy, iz), Is.EqualTo(new Vector2Int(px, py)));
                Assert.That(grid.GetCell(px, py), Is.EqualTo(new Vector3Int(ix, iy, iz)));
                Assert.That(grid.GetAtlasIndex(ix, iy, iz), Is.EqualTo(py * width + px));
                Assert.That(grid.GetUV(ix, iy, iz), Is.EqualTo(new Vector2((px + 0.5f) / width, (py + 0.5f) / height)));
                Assert.That(seen.Add(grid.GetAtlasIndex(ix, iy, iz)), Is.True);
            }
        }

        [Test]
        public void PaddingAndOutsideCoordinatesAreNotCells()
        {
            var grid = Grid(3, 5, 7, 3);
            for (var py = 10; py < 15; py++)
            for (var px = 3; px < 9; px++)
            {
                Assert.That(grid.GetCell(px, py), Is.EqualTo(new Vector3Int(-1, -1, -1)));
            }
            Assert.That(grid.GetCell(-1, 0), Is.EqualTo(new Vector3Int(-1, -1, -1)));
            Assert.That(grid.GetCell(0, 15), Is.EqualTo(new Vector3Int(-1, -1, -1)));
            Assert.That(grid.GetCell(9, 0), Is.EqualTo(new Vector3Int(-1, -1, -1)));
            Assert.That(grid.GetPixel(0, 0, 7), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(grid.GetPixel(3, 0, 0), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(grid.GetPixel(0, 5, 0), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(grid.GetAtlasIndex(-1, 0, 0), Is.EqualTo(-1));
            Assert.That(grid.GetUV(0, -1, 0), Is.EqualTo(new Vector2(-1, -1)));
        }

        [TestCase(0, 1, 1, 0, "Grid dimensions must be between 1 and 16777216.")]
        [TestCase(1, -1, 1, 0, "Grid dimensions must be between 1 and 16777216.")]
        [TestCase(1, 1, 0, 0, "Grid dimensions must be between 1 and 16777216.")]
        [TestCase(1, 1, 16777217, 0, "Grid dimensions must be between 1 and 16777216.")]
        [TestCase(int.MaxValue, int.MaxValue, int.MaxValue, 0, "Grid dimensions must be between 1 and 16777216.")]
        [TestCase(3, 5, 7, -1, "Grid slice columns must be 0 (automatic) or between 1 and sizeZ.")]
        [TestCase(3, 5, 7, 8, "Grid slice columns must be 0 (automatic) or between 1 and sizeZ.")]
        [TestCase(100000, 100000, 1, 0, "Grid cell count must fit in Int32.")]
        [TestCase(1024, 1024, 2048, 0, "Grid cell count must fit in Int32.")]
        [TestCase(32768, 21845, 3, 2, "Grid atlas dimensions must fit exact float integers and capacity must fit in Int32.")]
        public void InvalidLayoutsAreRejectedBeforeAnyAllocation(int x, int y, int z, int columns, string message)
        {
            var grid = Grid(x, y, z, columns);
            Error(message);
            Assert.That(grid.ValidateLayout(), Is.False);
            Error(message);
            Assert.That(grid.CellCount, Is.Zero);
        }

        [Test]
        public void BindingValidatesStorageWithoutChangingItOrOwningIt()
        {
            var grid = Grid(3, 5, 7, 3);
            var buffer = Buffer(9, 15, FluxBufferFilterMode.Point);
            var kernel = Kernel();
            var material = _materials[0];
            Assert.That(grid.TryBind(kernel, buffer, "_Other"), Is.True);
            Assert.That(material.GetTexture("_OtherTexture"), Is.SameAs(buffer.Texture));
            Assert.That(material.GetVector("_OtherSize"), Is.EqualTo(new Vector4(3, 5, 7, 0)));
            Assert.That(material.GetVector("_OtherLayout"), Is.EqualTo(new Vector4(3, 3, 9, 15)));
            Assert.That(buffer.Count, Is.EqualTo(135));
            var texture = buffer.Texture;
            Object.DestroyImmediate(grid);
            Assert.That(texture.IsCreated(), Is.True);
            Assert.That(buffer.Texture, Is.SameAs(texture));
        }

        [Test]
        public void RejectedBindingPreservesPreviousShaderProperties()
        {
            var grid = Grid(3, 5, 7, 3);
            var valid = Buffer(9, 15, FluxBufferFilterMode.Point);
            var wrongShape = Buffer(15, 9, FluxBufferFilterMode.Point);
            var kernel = Kernel();
            Assert.That(grid.TryBind(kernel, valid), Is.True);
            Error("Grid Buffer must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(grid.TryBind(kernel, wrongShape), Is.False);
            valid.SetCount(grid.CellCount);
            Error("Grid Buffer must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(grid.TryBind(kernel, valid), Is.False);
            Assert.That(valid.Count, Is.EqualTo(105));
            Error("Grid binding requires a Kernel, Buffer and nonempty property prefix.");
            Assert.That(grid.TryBind(kernel, valid, ""), Is.False);
            Error("Grid binding requires a Kernel, Buffer and nonempty property prefix.");
            Assert.That(grid.TryBind(null, valid), Is.False);
            Assert.That(_materials[0].GetTexture("_FluxGridTexture"), Is.SameAs(valid.Texture));
            Assert.That(_materials[0].GetVector("_FluxGridSize"), Is.EqualTo(new Vector4(3, 5, 7, 0)));
        }

        [TestCase(3, 5, 7, 3, FluxBufferFilterMode.Point)]
        [TestCase(3, 5, 7, 3, FluxBufferFilterMode.Bilinear)]
        [TestCase(2, 3, 5, 1, FluxBufferFilterMode.Bilinear)]
        [TestCase(1, 2, 3, 2, FluxBufferFilterMode.Point)]
        [TestCase(1, 1, 1, 0, FluxBufferFilterMode.Bilinear)]
        public void GpuMappingAndInterpolationMatchIndependentCpuReference(int x, int y, int z, int columns, FluxBufferFilterMode filter)
        {
            var grid = Grid(x, y, z, columns);
            var atlas = Buffer(grid.AtlasWidth, grid.AtlasHeight, filter);
            var kernel = Kernel();
            Assert.That(grid.TryBind(kernel, atlas), Is.True);
            kernel.Dispatch(atlas, 0);
            var stored = Read(atlas.Texture);
            for (var py = 0; py < grid.AtlasHeight; py++)
            for (var px = 0; px < grid.AtlasWidth; px++)
            {
                var iz = (py / y) * grid.AtlasColumns + px / x;
                var expected = iz < z ? Value(px % x, py % y, iz) : new Vector4(12345, -23456, 34567, -45678);
                Near(stored[py * grid.AtlasWidth + px], expected, $"atlas ({px},{py})");
            }
            var queries = Queries(x, y, z);
            var output = Buffer(8, 8, FluxBufferFilterMode.Point);
            output.SetCount(queries.Count);
            var uniforms = new Vector4[64];
            queries.CopyTo(uniforms);
            kernel.SetVectorArray("_Queries", uniforms);
            kernel.Dispatch(output, 1);
            var actual = Read(output.Texture);
            for (var i = 0; i < queries.Count; i++)
            {
                Near(actual[i], Expected(queries[i], x, y, z), $"query {queries[i]}");
            }
            Assert.That(output.Count, Is.EqualTo(queries.Count));
            Assert.That(atlas.Texture.filterMode, Is.EqualTo(filter == FluxBufferFilterMode.Point ? FilterMode.Point : FilterMode.Bilinear));

            // Exercise GPU coordinate helpers at tile-row transitions and the last slice.
            var coordinates = new[] { new Vector4(0, 0, 0, 6), new Vector4(x - 1, y - 1, z - 1, 6),
                new Vector4(0, 0, Mathf.Min(grid.AtlasColumns, z - 1), 6), new Vector4(grid.AtlasWidth, 0, 0, 5),
                new Vector4(grid.AtlasWidth - 1, grid.AtlasHeight - 1, 0, 5) };
            output.SetCount(coordinates.Length);
            for (var i = 0; i < coordinates.Length; i++) { uniforms[i] = coordinates[i]; }
            kernel.SetVectorArray("_Queries", uniforms);
            kernel.Dispatch(output, 1);
            actual = Read(output.Texture);
            for (var i = 0; i < coordinates.Length; i++)
            {
                var q = coordinates[i];
                if (q.w == 5)
                {
                    var cell = grid.GetCell((int)q.x, (int)q.y);
                    Near(actual[i], new Vector4(cell.x, cell.y, cell.z, cell.x >= 0 ? 1 : 0), "inverse mapping");
                }
                else
                {
                    var pixel = grid.GetPixel((int)q.x, (int)q.y, (int)q.z);
                    var uv = grid.GetUV((int)q.x, (int)q.y, (int)q.z);
                    Near(actual[i], new Vector4(pixel.x, pixel.y, uv.x, uv.y), "forward mapping");
                }
            }
        }

        static List<Vector4> Queries(int x, int y, int z)
        {
            var queries = new List<Vector4>();
            var positions = new[] { Vector3.zero, new Vector3(x - 1, y - 1, z - 1), new Vector3(0.25f, 0.75f, 1.5f),
                new Vector3(x - 0.5f, y - 1, z - 1), new Vector3(-0.5f, 0, 0), new Vector3(0, -0.5f, 0),
                new Vector3(0, 0, -0.5f), new Vector3(x - 1, y - 0.5f, z - 1), new Vector3(x - 1, y - 1, z - 0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
                new Vector3(-1000, 0, 0), new Vector3(0, 1000, 0), new Vector3(0, 0, 1000) };
            foreach (var position in positions)
            {
                for (var mode = 0; mode <= 2; mode++) { queries.Add(new Vector4(position.x, position.y, position.z, mode)); }
            }
            queries.Add(new Vector4(0, 0, 0, 3));
            queries.Add(new Vector4(1, 1, 1, 3));
            queries.Add(new Vector4(0, 0, 0, 4));
            queries.Add(new Vector4(1, 1, 1, 4));
            queries.Add(new Vector4(0.5f, 0.5f, 0.5f, 3));
            queries.Add(new Vector4(float.NaN, 0, 0, 1));
            queries.Add(new Vector4(float.PositiveInfinity, 0, 0, 2));
            queries.Add(new Vector4(0, float.NegativeInfinity, 0, 1));
            queries.Add(new Vector4(0, 0, float.NaN, 3));
            return queries;
        }

        static Vector4 Expected(Vector4 query, int x, int y, int z)
        {
            var mode = (int)query.w;
            var p = new Vector3(query.x, query.y, query.z);
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z)) return Vector4.zero;
            if (mode == 0) return ReadCell(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z), x, y, z);
            if (mode >= 3) { p = Vector3.Scale(p, new Vector3(x, y, z)) - Vector3.one * 0.5f; }
            if (mode == 2 || mode == 3) { p = new Vector3(Mathf.Clamp(p.x, 0, x - 1), Mathf.Clamp(p.y, 0, y - 1), Mathf.Clamp(p.z, 0, z - 1)); }
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
                result += ReadCell(bx + dx, by + dy, bz + dz, x, y, z) * weight;
            }
            return result;
        }

        static Vector4 ReadCell(int ix, int iy, int iz, int x, int y, int z)
        {
            return ix < 0 || iy < 0 || iz < 0 || ix >= x || iy >= y || iz >= z ? Vector4.zero : Value(ix, iy, iz);
        }

        static Vector4 Value(int x, int y, int z)
        {
            return new Vector4(x + 10 * y + 100 * z + 1, -(1 + x * x + 3 * y + 7 * z), (x + 1) * (y + 2) * (z + 3) * 0.125f, -4 - z * z * 0.25f);
        }

        FluxGrid3D Grid(int x, int y, int z, int columns)
        {
            var grid = Component<FluxGrid3D>();
            Set(grid, "sizeX", x); Set(grid, "sizeY", y); Set(grid, "sizeZ", z); Set(grid, "sliceColumns", columns);
            return grid;
        }

        FluxBuffer Buffer(int width, int height, FluxBufferFilterMode filter)
        {
            var buffer = Component<FluxBuffer>();
            Set(buffer, "sizeMode", FluxBufferSizeMode.Explicit);
            Set(buffer, "width", width); Set(buffer, "height", height); Set(buffer, "initialCount", width * height); Set(buffer, "filterMode", filter);
            return buffer;
        }

        FluxKernel Kernel()
        {
            var shader = Shader.Find("Flux/Tests/FluxGrid3D");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            var material = new Material(shader);
            _materials.Add(material);
            var kernel = Component<FluxKernel>();
            Set(kernel, "shader", shader); Set(kernel, "material", material);
            return kernel;
        }

        T Component<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            _objects.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Error(string message) { LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(message) + "$")); }

        static Color[] Read(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            var cpu = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
            try
            {
                RenderTexture.active = texture;
                cpu.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                cpu.Apply();
                return cpu.GetPixels();
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(cpu); }
        }

        static void Near(Color actual, Vector4 expected, string label)
        {
            for (var channel = 0; channel < 4; channel++)
            {
                Assert.That(actual[channel], Is.EqualTo(expected[channel]).Within(0.001f), label + " channel " + channel);
            }
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TpLab.Flux.Udon;
using UnityEngine;
using UnityEngine.TestTools;

namespace TpLab.Flux.Tests.Editor
{
    /// <summary>
    /// 軸別フィルタを密な3D配列のCPU畳み込みと比較し、境界とBufferの契約を検証します。
    /// </summary>
    public class FluxGridFilterTests
    {
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects) { Object.DestroyImmediate(item); }
            _objects.Clear();
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        public void EachAxisBoundaryAndKernelMatchesCpu(int axis, int boundary)
        {
            var grid = Grid(7, 5, 9, 4);
            var filter = Filter(grid);
            var input = Values(7, 5, 9);
            for (var mode = 0; mode < 2; mode++)
            {
                var source = Buffer(grid, (FluxBufferFilterMode)mode);
                var destination = Buffer(grid, (FluxBufferFilterMode)mode);
                Upload(source, input, 7, 5, 9, 4);
                var original = Read(source.Texture);
                for (var taps = 0; taps < 3; taps++)
                {
                    var weights = Weights(taps);
                    Assert.That(filter.TryFilter(source, destination, (FluxGridAxis)axis, (FluxGridBoundaryMode)boundary, weights, 0), Is.True);
                    Compare(destination, CpuFilter(input, 7, 5, 9, axis, boundary, weights), 7, 5, 9, 4);
                }
                CollectionAssert.AreEqual(original, Read(source.Texture));
                Assert.That(source.Count, Is.EqualTo(420));
                Assert.That(destination.Count, Is.EqualTo(420));
                Assert.That(source.Texture.filterMode, Is.EqualTo(mode == 0 ? FilterMode.Point : FilterMode.Bilinear));
            }
        }

        [TestCase(1, 1, 1, 0)]
        [TestCase(1, 3, 2, 0)]
        [TestCase(3, 1, 2, 1)]
        [TestCase(3, 2, 1, 2)]
        public void SingletonAxesAndConstantSignedValuesRespectBoundary(int x, int y, int z, int axis)
        {
            var grid = Grid(x, y, z, 1);
            var filter = Filter(grid);
            var source = Buffer(grid);
            var destination = Buffer(grid);
            var values = new Vector4[x * y * z];
            for (var i = 0; i < values.Length; i++) { values[i] = new Vector4(-8, 4, -2, 0); }
            Upload(source, values, x, y, z, 1);
            for (var boundary = 0; boundary < 2; boundary++)
            {
                Assert.That(filter.TryFilter(source, destination, (FluxGridAxis)axis, (FluxGridBoundaryMode)boundary, Weights(2), 0), Is.True);
                Compare(destination, CpuFilter(values, x, y, z, axis, boundary, Weights(2)), x, y, z, 1);
            }
        }

        [Test]
        public void ImpulsesAcrossTileRowsAndSequentialAxesMatchCpu()
        {
            var grid = Grid(7, 5, 9, 4);
            var filter = Filter(grid);
            var a = Buffer(grid);
            var b = Buffer(grid);
            var expected = new Vector4[315];
            expected[(4 * 5 + 2) * 7 + 3] = new Vector4(16, -8, 4, -2);
            expected[(8 * 5 + 4) * 7] = new Vector4(-4, 2, -1, 0);
            Upload(a, expected, 7, 5, 9, 4);
            for (var axis = 0; axis < 3; axis++)
            {
                var boundary = axis == 1 ? 1 : 0;
                Assert.That(filter.TryFilter(a, b, (FluxGridAxis)axis, (FluxGridBoundaryMode)boundary, Weights(axis), 0), Is.True);
                expected = CpuFilter(expected, 7, 5, 9, axis, boundary, Weights(axis));
                Compare(b, expected, 7, 5, 9, 4);
                var swap = a; a = b; b = swap;
            }
        }

        [Test]
        public void ExplicitWeightsAreNotNormalizedAndOutputIsOverwritten()
        {
            var grid = Grid(7, 5, 9, 4);
            var filter = Filter(grid);
            var source = Buffer(grid);
            var destination = Buffer(grid);
            var values = Values(7, 5, 9);
            Upload(source, values, 7, 5, 9, 4);
            // Destination starts with poison in every texel; padding must also be overwritten.
            Fill(destination, new Color(12345, -23456, 34567, -45678));
            var weights = new[] { new Vector4(1, 0, 0, 0), new Vector4(1, 0.5f, 0, 0),
                Vector4.Lerp(Weights(0), Weights(2), 0.5f), Vector4.zero };
            foreach (var weight in weights)
            {
                Assert.That(filter.TryFilter(source, destination, FluxGridAxis.Z, FluxGridBoundaryMode.Zero, weight, 0), Is.True);
                Compare(destination, CpuFilter(values, 7, 5, 9, 2, 0, weight), 7, 5, 9, 4);
            }
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void InvalidAxisOrBoundaryIsRejectedBeforeAllocation(int axis, int boundary)
        {
            Error("Grid filter requires axis X/Y/Z and boundary Zero/Clamp.");
            Assert.That(Component<FluxGridFilter>().TryFilter(null, null, (FluxGridAxis)axis,
                (FluxGridBoundaryMode)boundary, Weights(0), 0), Is.False);
        }

        [TestCase(-1f, 0f, 0f, 0f)]
        [TestCase(1f, -0.1f, 0f, 0f)]
        [TestCase(1f, 0f, float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f, 0f, 0f)]
        [TestCase(1f, 0f, 0f, float.NegativeInfinity)]
        [TestCase(float.MaxValue, float.MaxValue, 0f, 0f)]
        public void InvalidWeightsAreRejectedBeforeAllocation(float center, float near, float middle, float far)
        {
            Error("Grid filter weights must be finite, nonnegative, and have a finite total.");
            Assert.That(Component<FluxGridFilter>().TryFilter(null, null, FluxGridAxis.X, FluxGridBoundaryMode.Zero,
                new Vector4(center, near, middle, far), 0), Is.False);
        }

        [Test]
        public void InvalidBuffersPassAndAliasingPreserveDestination()
        {
            var grid = Grid(3, 5, 7, 3);
            var filter = Filter(grid);
            var source = Buffer(grid);
            var destination = Buffer(grid);
            Fill(destination, new Color(-3, 4, -5, 0));
            var before = Read(destination.Texture);
            Error("Pass must be between 0 and 0.");
            Assert.That(filter.TryFilter(source, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), -1), Is.False);
            Error("Pass must be between 0 and 0.");
            Assert.That(filter.TryFilter(source, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 1), Is.False);
            Error("Grid filter source Buffer is required.");
            Assert.That(filter.TryFilter(null, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            Error("Destination Buffer is required.");
            Assert.That(filter.TryFilter(source, null, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            Error("Grid filter source and destination must use different Textures.");
            Assert.That(filter.TryFilter(destination, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            var alias = Buffer(grid);
            Set(alias, "_texture", destination.Texture);
            try
            {
                Error("Grid filter source and destination must use different Textures.");
                Assert.That(filter.TryFilter(alias, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            }
            finally { Set(alias, "_texture", null); }
            source.SetCount(105);
            Error("Grid filter Buffers must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(filter.TryFilter(source, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            source.SetCount(135);
            destination.SetCount(105);
            Error("Grid filter Buffers must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(filter.TryFilter(source, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            destination.SetCount(135);
            var wrongShape = Buffer(Grid(5, 3, 7, 3));
            Error("Grid filter Buffers must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(filter.TryFilter(wrongShape, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            Error("Grid filter Buffers must match atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(filter.TryFilter(source, wrongShape, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            Set(filter, "kernel", Component<FluxKernel>());
            Error("Kernel Material is not prepared. Run FluxBuildPass before dispatch.");
            Assert.That(filter.TryFilter(source, destination, FluxGridAxis.X, FluxGridBoundaryMode.Zero, Weights(0), 0), Is.False);
            CollectionAssert.AreEqual(before, Read(destination.Texture));
        }

        static Vector4 Weights(int index)
        {
            return index == 0 ? new Vector4(0.5f, 0.25f, 0, 0) : index == 1 ? new Vector4(6f/16, 4f/16, 1f/16, 0) : new Vector4(20f/64, 15f/64, 6f/64, 1f/64);
        }

        static Vector4[] Values(int x, int y, int z)
        {
            var data = new Vector4[x * y * z];
            for (var iz = 0; iz < z; iz++)
            for (var iy = 0; iy < y; iy++)
            for (var ix = 0; ix < x; ix++)
            {
                data[(iz * y + iy) * x + ix] = new Vector4(ix * ix + 10 * iy + 100 * iz, -1 - ix - 3 * iy * iy - 7 * iz,
                    (ix + 1) * (iy + 2) * (iz + 3) * 0.125f, -4 - iz * iz * 0.25f);
            }
            return data;
        }

        static Vector4[] CpuFilter(Vector4[] input, int x, int y, int z, int axis, int boundary, Vector4 weights)
        {
            var output = new Vector4[input.Length];
            for (var iz = 0; iz < z; iz++)
            for (var iy = 0; iy < y; iy++)
            for (var ix = 0; ix < x; ix++)
            for (var offset = -3; offset <= 3; offset++)
            {
                var sx = ix + (axis == 0 ? offset : 0);
                var sy = iy + (axis == 1 ? offset : 0);
                var sz = iz + (axis == 2 ? offset : 0);
                if (boundary == 1) { sx = Mathf.Clamp(sx, 0, x - 1); sy = Mathf.Clamp(sy, 0, y - 1); sz = Mathf.Clamp(sz, 0, z - 1); }
                if (sx < 0 || sy < 0 || sz < 0 || sx >= x || sy >= y || sz >= z) continue;
                output[(iz * y + iy) * x + ix] += input[(sz * y + sy) * x + sx] * weights[Mathf.Abs(offset)];
            }
            return output;
        }

        void Upload(FluxBuffer buffer, Vector4[] values, int x, int y, int z, int columns)
        {
            var pixels = new Color[buffer.Capacity];
            for (var py = 0; py < buffer.Texture.height; py++)
            for (var px = 0; px < buffer.Texture.width; px++)
            {
                var iz = py / y * columns + px / x;
                pixels[py * buffer.Texture.width + px] = iz < z ? (Color)values[(iz * y + py % y) * x + px % x] : new Color(12345, -23456, 34567, -45678);
            }
            var texture = new Texture2D(buffer.Texture.width, buffer.Texture.height, TextureFormat.RGBAFloat, false, true);
            _objects.Add(texture);
            texture.SetPixels(pixels); texture.Apply();
            Graphics.Blit(texture, buffer.Texture);
        }

        static void Compare(FluxBuffer buffer, Vector4[] expected, int x, int y, int z, int columns)
        {
            var actual = Read(buffer.Texture);
            for (var py = 0; py < buffer.Texture.height; py++)
            for (var px = 0; px < buffer.Texture.width; px++)
            {
                var iz = py / y * columns + px / x;
                var value = iz < z ? expected[(iz * y + py % y) * x + px % x] : Vector4.zero;
                for (var c = 0; c < 4; c++)
                {
                    Assert.That(actual[py * buffer.Texture.width + px][c], Is.EqualTo(value[c]).Within(0.002f), $"pixel ({px},{py}), channel {c}");
                }
            }
        }

        FluxGrid3D Grid(int x, int y, int z, int columns)
        {
            var grid = Component<FluxGrid3D>();
            Set(grid, "sizeX", x); Set(grid, "sizeY", y); Set(grid, "sizeZ", z); Set(grid, "sliceColumns", columns);
            return grid;
        }

        FluxBuffer Buffer(FluxGrid3D grid, FluxBufferFilterMode filter = FluxBufferFilterMode.Point)
        {
            var buffer = Component<FluxBuffer>();
            Set(buffer, "sizeMode", FluxBufferSizeMode.Explicit); Set(buffer, "width", grid.AtlasWidth); Set(buffer, "height", grid.AtlasHeight);
            Set(buffer, "initialCount", grid.AtlasCapacity); Set(buffer, "filterMode", filter);
            return buffer;
        }

        FluxGridFilter Filter(FluxGrid3D grid)
        {
            var shader = Shader.Find("Flux/GridFilter");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            var material = new Material(shader);
            var kernel = Component<FluxKernel>();
            _objects.Add(material);
            Set(kernel, "shader", shader); Set(kernel, "material", material);
            var filter = Component<FluxGridFilter>();
            Set(filter, "grid", grid); Set(filter, "kernel", kernel);
            return filter;
        }

        T Component<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            _objects.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Error(string message) { LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(message) + "$")); }

        static void Fill(FluxBuffer buffer, Color color)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = buffer.Texture;
            GL.Clear(false, true, color);
            RenderTexture.active = previous;
        }

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
    }
}

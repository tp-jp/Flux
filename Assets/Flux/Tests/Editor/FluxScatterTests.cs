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
    /// Scatterの入力範囲、独立したCPU集計とのGPU比較、クリアと分割加算を検証します。
    /// </summary>
    public class FluxScatterTests
    {
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects) { Object.DestroyImmediate(item); }
            _objects.Clear();
        }

        [TestCase(0, 1)]
        [TestCase(7, 2)]
        [TestCase(31, 31)]
        [TestCase(0, 32)]
        [TestCase(9, 33)]
        [TestCase(7, 128)]
        [TestCase(8, 129)]
        [TestCase(3, 289)]
        [TestCase(5, 4095)]
        [TestCase(3, 4096)]
        [TestCase(2, 4097)]
        [TestCase(0, 131072)]
        [TestCase(16777216 - 513, 513)]
        public void ProceduralAdapterMatchesCpuAcrossTessellationAndInstanceBoundaries(int start, int count)
        {
            var grid = Grid();
            var kernel = Kernel("Flux/Tests/ScatterProcedural");
            var scatter = Scatter(grid, kernel);
            var output = Buffer(9, 15);
            Assert.That(scatter.TryClear(output, 0), Is.True);
            Assert.That(scatter.TryAccumulate(output, start, count, 1), Is.True);
            var expected = new Vector4[135];
            for (var i = start; i < start + count; i++)
            {
                if (i % 13 == 0) continue;
                var position = new Vector3(i % 11, (i / 11) % 23, (i / 253) % 31) * 0.25f - Vector3.one * 0.5f;
                var value = new Vector4((i % 7 - 3) * 0.125f, (i % 5 - 2) * 0.25f, (i % 3 + 1) * 0.5f, i % 2 == 0 ? -0.25f : 0);
                Splat(expected, position, value);
            }
            Compare(Read(output.Texture), expected);
            Assert.That(output.Count, Is.EqualTo(135));
        }

        [TestCase(FluxBufferFilterMode.Point)]
        [TestCase(FluxBufferFilterMode.Bilinear)]
        public void TextureAdapterPreservesSignedValuesAndSplitAccumulation(FluxBufferFilterMode filter)
        {
            var points = new[] {
                new Vector4(0,0,0,1), new Vector4(2,4,6,1), new Vector4(0.25f,0.75f,2.5f,1),
                new Vector4(-0.5f,0,0,1), new Vector4(0,-0.5f,0,1), new Vector4(0,0,-0.5f,1),
                new Vector4(2.5f,4,6,1), new Vector4(2,4.5f,6,1), new Vector4(2,4,6.5f,1),
                new Vector4(-0.5f,-0.5f,-0.5f,1), new Vector4(2.5f,4.5f,6.5f,1),
                new Vector4(-1,0,0,1), new Vector4(3,0,0,1), new Vector4(0,5,0,1), new Vector4(0,0,7,1),
                new Vector4(-1000,0,0,1), new Vector4(1000,0,0,1), new Vector4(1,2,3,0),
                new Vector4(1,2,3,-1), new Vector4(float.NaN,0,0,1), new Vector4(0,float.PositiveInfinity,0,1),
                new Vector4(0,0,float.NegativeInfinity,1), new Vector4(1.5f,2.5f,3.5f,1), new Vector4(1,2,3,1)
            };
            var positions = new Color[32];
            var values = new Color[32];
            var expected = new Vector4[135];
            // Sentinel entries outside the requested subrange must never contribute.
            positions[0] = positions[1] = positions[31] = new Color(0, 0, 0, 1);
            values[0] = values[1] = values[31] = Color.white * 10000;
            for (var i = 0; i < points.Length; i++)
            {
                positions[i + 2] = points[i];
                var value = new Vector4(i % 7 - 3, 2 - i % 5, (i % 3 + 1) * 0.25f, i % 2 == 0 ? -2 : 0);
                values[i + 2] = value;
                if (points[i].w > 0) { Splat(expected, (Vector3)points[i], value); }
            }
            var kernel = Kernel("Flux/Scatter");
            var positionTexture = Texture(8, 4, positions);
            var valueTexture = Texture(4, 8, values);
            positionTexture.filterMode = valueTexture.filterMode = FilterMode.Bilinear;
            kernel.SetTexture("_FluxScatterPositions", positionTexture);
            kernel.SetTexture("_FluxScatterValues", valueTexture);
            var scatter = Scatter(Grid(), kernel);
            var whole = Buffer(9, 15, filter);
            var split = Buffer(9, 15, filter);
            Assert.That(scatter.TryClear(whole, 0), Is.True);
            Assert.That(scatter.TryClear(split, 0), Is.True);
            Assert.That(scatter.TryAccumulate(whole, 2, points.Length, 1), Is.True);
            Assert.That(scatter.TryAccumulate(split, 2, 1, 1), Is.True);
            Assert.That(scatter.TryAccumulate(split, 3, 10, 1), Is.True);
            Assert.That(scatter.TryAccumulate(split, 13, points.Length - 11, 1), Is.True);
            Compare(Read(whole.Texture), expected);
            Compare(Read(split.Texture), expected);
            // Out-of-texture reads and zero-count requests must leave accumulated output intact.
            Assert.That(scatter.TryAccumulate(split, 32, 5, 1), Is.True);
            Assert.That(scatter.TryAccumulate(split, 4, 0, 1), Is.True);
            Compare(Read(split.Texture), expected);
            Assert.That(scatter.TryAccumulate(split, 2, points.Length, 1), Is.True);
            for (var i = 0; i < expected.Length; i++) { expected[i] *= 2; }
            Compare(Read(split.Texture), expected);
            Assert.That(scatter.TryClear(split, 0), Is.True);
            Compare(Read(split.Texture), new Vector4[135]);
            Assert.That(whole.Count, Is.EqualTo(135));
            Assert.That(split.Count, Is.EqualTo(135));
        }

        [TestCase(-1, 1)]
        [TestCase(0, -1)]
        [TestCase(0, 131073)]
        [TestCase(16777216, 1)]
        [TestCase(16777217, 0)]
        [TestCase(int.MaxValue, int.MaxValue)]
        public void InvalidRangeIsRejectedBeforeAllocation(int start, int count)
        {
            var scatter = Component<FluxScatter>();
            Error("Scatter range requires start >= 0, count in [0, 131072], and end <= 16777216.");
            Assert.That(scatter.TryAccumulate(null, start, count, 1), Is.False);
        }

        [Test]
        public void EmptyRangeDoesNotRequireStorageOrPreparedMaterial()
        {
            Assert.That(Component<FluxScatter>().TryAccumulate(null, 16777216, 0, -1), Is.True);
        }

        [Test]
        public void InvalidPassStorageAndUnpreparedKernelAreRejectedWithoutDrawing()
        {
            var grid = Grid();
            var kernel = Kernel("Flux/Scatter");
            var scatter = Scatter(grid, kernel);
            var output = Buffer(9, 15);
            var previous = RenderTexture.active;
            RenderTexture.active = output.Texture;
            GL.Clear(false, true, new Color(3, -4, 5, -6));
            RenderTexture.active = previous;
            Error("Pass must be between 0 and 1.");
            Assert.That(scatter.TryClear(output, -1), Is.False);
            Error("Pass must be between 0 and 1.");
            Assert.That(scatter.TryAccumulate(output, 0, 1, 2), Is.False);
            output.SetCount(105);
            Error("Scatter destination must match grid atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(scatter.TryClear(output, 0), Is.False);
            var expected = new Vector4[135];
            for (var i = 0; i < expected.Length; i++) { expected[i] = new Vector4(3, -4, 5, -6); }
            Compare(Read(output.Texture), expected);
            Error("Scatter destination must match grid atlas dimensions and use Count = AtlasCapacity.");
            Assert.That(scatter.TryAccumulate(Buffer(15, 9), 0, 1, 1), Is.False);
            Error("Destination Buffer is required.");
            Assert.That(scatter.TryClear(null, 0), Is.False);
            var unprepared = Scatter(grid, Component<FluxKernel>());
            Error("Kernel Material is not prepared. Run FluxBuildPass before dispatch.");
            Assert.That(unprepared.TryAccumulate(output, 0, 1, 1), Is.False);
        }

        // Reference iterates all valid cells, rather than reproducing the GPU eight-neighbor loop.
        static void Splat(Vector4[] target, Vector3 p, Vector4 value)
        {
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z)) return;
            for (var z = 0; z < 7; z++)
            for (var y = 0; y < 5; y++)
            for (var x = 0; x < 3; x++)
            {
                var weight = Mathf.Max(0, 1 - Mathf.Abs(p.x - x)) * Mathf.Max(0, 1 - Mathf.Abs(p.y - y)) * Mathf.Max(0, 1 - Mathf.Abs(p.z - z));
                var index = ((z / 3) * 5 + y) * 9 + (z % 3) * 3 + x;
                target[index] += value * weight;
            }
        }

        static void Compare(Color[] actual, Vector4[] expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            for (var c = 0; c < 4; c++)
            {
                Assert.That(actual[i][c], Is.EqualTo(expected[i][c]).Within(0.002f), $"texel {i}, channel {c}");
            }
        }

        FluxGrid3D Grid()
        {
            var grid = Component<FluxGrid3D>();
            Set(grid, "sizeX", 3); Set(grid, "sizeY", 5); Set(grid, "sizeZ", 7); Set(grid, "sliceColumns", 3);
            return grid;
        }

        FluxScatter Scatter(FluxGrid3D grid, FluxKernel kernel)
        {
            var scatter = Component<FluxScatter>();
            Set(scatter, "grid", grid); Set(scatter, "kernel", kernel);
            return scatter;
        }

        FluxBuffer Buffer(int width, int height, FluxBufferFilterMode filter = FluxBufferFilterMode.Point)
        {
            var buffer = Component<FluxBuffer>();
            Set(buffer, "sizeMode", FluxBufferSizeMode.Explicit); Set(buffer, "width", width); Set(buffer, "height", height);
            Set(buffer, "initialCount", width * height); Set(buffer, "filterMode", filter);
            return buffer;
        }

        FluxKernel Kernel(string name)
        {
            var shader = Shader.Find(name);
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            var material = new Material(shader);
            var kernel = Component<FluxKernel>();
            _objects.Add(material);
            Set(kernel, "shader", shader); Set(kernel, "material", material);
            return kernel;
        }

        Texture2D Texture(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            _objects.Add(texture);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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

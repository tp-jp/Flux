using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace TpLab.Flux.Tests.Editor
{
    public class FluxBufferKernelTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<Material> _materials = new List<Material>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                var buffer = go.GetComponent<FluxBuffer>();
                if (buffer != null) { buffer.Release(); }
                Object.DestroyImmediate(go);
            }
            foreach (var material in _materials) { Object.DestroyImmediate(material); }
            _objects.Clear();
            _materials.Clear();
        }

        [Test]
        public void LegacyDefaultsCountAndLifetimeArePreserved()
        {
            var buffer = Buffer(250);
            var texture = buffer.Texture;
            Assert.That(texture.width, Is.EqualTo(16));
            Assert.That(texture.height, Is.EqualTo(16));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.format, Is.EqualTo(RenderTextureFormat.ARGBFloat));
            Assert.That(buffer.Count, Is.EqualTo(250));
            Assert.That(buffer.Capacity, Is.EqualTo(256));
            var kernel = Kernel();
            kernel.Dispatch(buffer, 1);
            var before = Read(texture);
            buffer.SetCount(0);
            Assert.That(buffer.Count, Is.Zero);
            buffer.SetCount(256);
            Assert.That(buffer.Texture, Is.SameAs(texture));
            CollectionAssert.AreEqual(before, Read(texture));
            buffer.gameObject.SetActive(false);
            Assert.That(texture.IsCreated(), Is.True, "OnDisable must not release the Texture");
            buffer.Release();
            Assert.That(texture == null || !texture.IsCreated(), Is.True);
            buffer.Release();
            Assert.That(buffer.Texture, Is.Not.SameAs(texture));
            Assert.That(buffer.Count, Is.EqualTo(250));
            Assert.That(buffer.Capacity, Is.EqualTo(256));
        }

        [TestCase(FluxBufferFilterMode.Point, FilterMode.Point)]
        [TestCase(FluxBufferFilterMode.Bilinear, FilterMode.Bilinear)]
        public void ExplicitDimensionsAndFilterSurviveRelease(FluxBufferFilterMode mode, FilterMode expected)
        {
            var buffer = Buffer(32100, 256, 128, mode);
            Assert.That(buffer.Texture.width, Is.EqualTo(256));
            Assert.That(buffer.Texture.height, Is.EqualTo(128));
            Assert.That(buffer.Capacity, Is.EqualTo(32768));
            Assert.That(buffer.Count, Is.EqualTo(32100));
            Assert.That(buffer.Texture.filterMode, Is.EqualTo(expected));
            buffer.SetCount(1);
            buffer.Release();
            Assert.That(buffer.Count, Is.EqualTo(32100));
            Assert.That(buffer.Capacity, Is.EqualTo(32768));
            Assert.That(buffer.Texture.filterMode, Is.EqualTo(expected));
        }

        [TestCase(0, 128)]
        [TestCase(-1, 128)]
        [TestCase(128, 0)]
        [TestCase(128, -1)]
        [TestCase(65536, 65536)]
        [TestCase(int.MaxValue, int.MaxValue)]
        public void InvalidDimensionsAreRejected(int width, int height)
        {
            var buffer = Buffer(1, width, height);
            ExpectError("Buffer dimensions must be positive and capacity must fit in Int32.");
            Assert.That(buffer.Texture, Is.Null);
        }

        [Test]
        public void GpuDimensionLimitIsCheckedBeforeAllocation()
        {
            var buffer = Buffer(1, SystemInfo.maxTextureSize + 1, 1);
            ExpectError($"Buffer dimensions exceed GPU maximum Texture size ({SystemInfo.maxTextureSize}).");
            Assert.That(buffer.Texture, Is.Null);
        }

        [Test]
        public void CapacityShortageAndFailedAccessAreSafe()
        {
            var buffer = Buffer(32769, 256, 128);
            ExpectError("Initial Count exceeds Buffer capacity.");
            Assert.That(buffer.Texture, Is.Null);
            ExpectError("Initial Count exceeds Buffer capacity.");
            Assert.That(buffer.Count, Is.Zero);
            ExpectError("Initial Count exceeds Buffer capacity.");
            Assert.That(buffer.Capacity, Is.Zero);
            ExpectError("Initial Count exceeds Buffer capacity.");
            buffer.SetCount(1);
            buffer.Release();
            Set(buffer, "initialCount", 1);
            Assert.That(buffer.Texture.IsCreated(), Is.True, "A corrected configuration can retry allocation");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidInitialCountIsRejected(int count)
        {
            var buffer = Buffer(count);
            ExpectError("Initial Count must be positive.");
            Assert.That(buffer.Texture, Is.Null);
        }

        [Test]
        public void InvalidModesAreRejected()
        {
            var buffer = Buffer(1);
            Set(buffer, "sizeMode", (FluxBufferSizeMode)99);
            ExpectError("Unknown Buffer size mode.");
            Assert.That(buffer.Texture, Is.Null);
            Set(buffer, "sizeMode", FluxBufferSizeMode.Automatic);
            Set(buffer, "filterMode", (FluxBufferFilterMode)99);
            ExpectError("Buffer filter must be Point or Bilinear.");
            Assert.That(buffer.Texture, Is.Null);
        }

        [TestCase(-1)]
        [TestCase(257)]
        public void InvalidCountPreservesContents(int count)
        {
            var buffer = Buffer(250);
            var kernel = Kernel();
            kernel.Dispatch(buffer, 1);
            var texture = buffer.Texture;
            var before = Read(texture);
            ExpectError("Count must be between 0 and Capacity (256).");
            buffer.SetCount(count);
            Assert.That(buffer.Count, Is.EqualTo(250));
            Assert.That(buffer.Texture, Is.SameAs(texture));
            CollectionAssert.AreEqual(before, Read(texture));
        }

        [Test]
        public void RectangularIndexMappingUsesBothSourceAndDestinationDimensions()
        {
            var source = Buffer(32768, 128, 256);
            var destination = Buffer(123, 256, 128, FluxBufferFilterMode.Bilinear);
            var kernel = Kernel();
            kernel.Dispatch(source, 1);
            kernel.Dispatch(source, destination, 2);
            var data = Read(destination.Texture);
            for (var i = 0; i < data.Length; i++)
            {
                AssertColor(data[i], new Color(i, i % 128, i / 128, -i), i);
            }
            Assert.That(destination.Count, Is.EqualTo(123), "Dispatch must not change Count");
            destination.SetCount(0);
            CollectionAssert.AreEqual(data, Read(destination.Texture));
        }

        [Test]
        public void ClearIsSeparateFromSignedRepeatedPartialAccumulation()
        {
            var destination = Buffer(123, 256, 128);
            var kernel = Kernel();
            kernel.Dispatch(destination, 1);
            kernel.Dispatch(destination, 0);
            foreach (var value in Read(destination.Texture)) { AssertColor(value, Color.clear, 0); }
            kernel.SetFloat("_Start", 0);
            kernel.SetFloat("_Length", 16384);
            kernel.SetVector("_Value", new Vector4(0.25f, -0.5f, 1, -2));
            kernel.Dispatch(destination, 3);
            kernel.Dispatch(destination, 3);
            kernel.SetFloat("_Start", 16384);
            kernel.SetVector("_Value", new Vector4(-1, 2, -4, 8));
            kernel.Dispatch(destination, 3);
            var data = Read(destination.Texture);
            for (var i = 0; i < data.Length; i++)
            {
                AssertColor(data[i], i < 16384 ? new Color(0.5f, -1, 2, -4) : new Color(-1, 2, -4, 8), i);
            }
            Assert.That(destination.Count, Is.EqualTo(123));
        }

        [TestCase(-1)]
        [TestCase(4)]
        [TestCase(int.MaxValue)]
        public void InvalidPassDoesNotWrite(int pass)
        {
            var source = Buffer(16);
            var destination = Buffer(16);
            var kernel = Kernel();
            kernel.Dispatch(destination, 1);
            var before = Read(destination.Texture);
            ExpectError("Pass must be between 0 and 3.");
            kernel.Dispatch(destination, pass);
            ExpectError("Pass must be between 0 and 3.");
            kernel.Dispatch(source, destination, pass);
            CollectionAssert.AreEqual(before, Read(destination.Texture));
        }

        [Test]
        public void AliasedTextureIsRejectedEvenForDistinctBufferComponents()
        {
            var source = Buffer(16);
            var destination = Buffer(16);
            var kernel = Kernel();
            kernel.Dispatch(source, 1);
            var before = Read(source.Texture);
            Set(destination, "_texture", source.Texture);
            try
            {
                ExpectError("Source and destination must use different Textures.");
                kernel.Dispatch(source, destination, 2);
                ExpectError("Source and destination must use different Textures.");
                kernel.Dispatch(source, source, 2);
                ExpectError("Source and destination must use different Textures.");
                kernel.Dispatch(source, source);
                CollectionAssert.AreEqual(before, Read(source.Texture));
            }
            finally { Set(destination, "_texture", null); }
        }

        [Test]
        public void LegacyDispatchStillMatchesBlitWithoutPass()
        {
            var source = Buffer(16);
            var destination = Buffer(16);
            var reference = Buffer(16);
            var kernel = Kernel();
            kernel.Dispatch(source, 1);
            kernel.SetVector("_Value", new Vector4(3, -2, 1, -4));
            kernel.Dispatch(source, destination);
            var material = Get<Material>(kernel, "material");
            VRCGraphics.Blit(source.Texture, reference.Texture, material);
            CollectionAssert.AreEqual(Read(reference.Texture), Read(destination.Texture));
            kernel.Dispatch(destination);
            VRCGraphics.Blit(null, reference.Texture, material);
            CollectionAssert.AreEqual(Read(reference.Texture), Read(destination.Texture));
        }

        [Test]
        public void MissingMaterialIsReportedWithoutRuntimeConstruction()
        {
            var kernel = Component<FluxKernel>();
            var destination = Buffer(16);
            ExpectError("Kernel Material is not prepared. Run FluxBuildPass before dispatch.");
            kernel.Dispatch(destination, 0);
            Assert.That(Get<Material>(kernel, "material"), Is.Null);
        }

        [Test]
        public void FailedBuffersAbortDispatchAndReadback()
        {
            var invalid = Buffer(2, 1, 1);
            var destination = Buffer(16);
            var kernel = Kernel();
            kernel.Dispatch(destination, 1);
            var before = Read(destination.Texture);
            ExpectError("Initial Count exceeds Buffer capacity.");
            kernel.Dispatch(invalid, 0);
            ExpectError("Initial Count exceeds Buffer capacity.");
            kernel.Dispatch(invalid, destination, 2);
            CollectionAssert.AreEqual(before, Read(destination.Texture));
            var readback = Component<FluxReadback>();
            ExpectError("Initial Count exceeds Buffer capacity.");
            Assert.That(readback.TryRequest(invalid, destination), Is.False);
            Assert.That(readback.IsRequesting, Is.False);
            Assert.That(readback.Data, Is.Null);
        }

        [TestCase(FluxBufferFilterMode.Point, 1f)]
        [TestCase(FluxBufferFilterMode.Bilinear, 0.5f)]
        public void FilterAffectsGpuSampling(FluxBufferFilterMode filter, float expected)
        {
            var source = Buffer(2, 2, 1, filter);
            var destination = Buffer(1);
            var kernel = Kernel();
            kernel.Dispatch(source, 1);
            kernel.SetVector("_SampleOffset", new Vector4(0.25f, 0, 0, 0));
            kernel.Dispatch(source, destination, 2);
            AssertColor(Read(destination.Texture)[0], new Color(expected, expected, 0, -expected), 0);
        }

        T Component<T>() where T : UdonSharpBehaviour
        {
            var go = new GameObject(typeof(T).Name);
            _objects.Add(go);
            // These synchronous Editor tests invoke C# directly; Udon backing setup belongs to the runtime scene.
            return go.AddComponent<T>();
        }

        FluxBuffer Buffer(int count, int width = -2, int height = -2, FluxBufferFilterMode filter = FluxBufferFilterMode.Point)
        {
            var buffer = Component<FluxBuffer>();
            Set(buffer, "initialCount", count);
            Set(buffer, "filterMode", filter);
            if (width != -2 || height != -2)
            {
                Set(buffer, "sizeMode", FluxBufferSizeMode.Explicit);
                Set(buffer, "width", width);
                Set(buffer, "height", height);
            }
            return buffer;
        }

        FluxKernel Kernel()
        {
            var kernel = Component<FluxKernel>();
            var shader = Shader.Find("Flux/Tests/FluxKernel/BufferContracts");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            Set(kernel, "shader", shader);
            // Editor-only setup avoids changing other kernels in the user's open scenes.
            // The runtime scene exercises SceneFlow / FluxBuildPass preparation separately.
            var material = new Material(shader);
            Set(kernel, "material", material);
            _materials.Add(material);
            return kernel;
        }

        static void Set(object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }

        static T Get<T>(object instance, string name)
        {
            return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        }

        static void ExpectError(string message) { LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(message) + "$")); }

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
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cpu);
            }
        }

        static void AssertColor(Color actual, Color expected, int index)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f), $"Index {index}, R");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f), $"Index {index}, G");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f), $"Index {index}, B");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f), $"Index {index}, A");
        }
    }
}

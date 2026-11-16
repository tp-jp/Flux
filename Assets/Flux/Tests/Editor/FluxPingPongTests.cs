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
    /// 参照交換の非所有契約と、明示的なGPU更新との組み合わせを検証します。
    /// </summary>
    public class FluxPingPongTests
    {
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects) { Object.DestroyImmediate(item); }
            _objects.Clear();
        }

        [Test]
        public void InspectorReferencesSwapAndResetWithoutAllocatingTextures()
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(5, 3, 7);
            var pair = Component<FluxPingPong>();
            Set(pair, "bufferA", a); Set(pair, "bufferB", b);
            Assert.That(pair.Current, Is.SameAs(a));
            Assert.That(pair.Next, Is.SameAs(b));
            Assert.That(pair.TrySwap(), Is.True);
            Assert.That(pair.Current, Is.SameAs(b));
            Assert.That(pair.Next, Is.SameAs(a));
            pair.ResetOrder();
            pair.ResetOrder();
            Assert.That(pair.Current, Is.SameAs(a));
            Assert.That(pair.Next, Is.SameAs(b));
            Assert.That(Get(a, "_texture"), Is.Null);
            Assert.That(Get(b, "_texture"), Is.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RejectedConfigurationPreservesSwappedPair(int invalid)
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(3, 5, 13);
            var pair = Pair(a, b);
            Assert.That(pair.TrySwap(), Is.True);
            Error();
            Assert.That(pair.TrySetBuffers(invalid == 0 ? null : a, invalid == 1 ? null : a), Is.False);
            Assert.That(pair.Current, Is.SameAs(b));
            Assert.That(pair.Next, Is.SameAs(a));
            pair.ResetOrder();
            Assert.That(pair.Current, Is.SameAs(a));
            Assert.That(Get(a, "_texture"), Is.Null);
            Assert.That(Get(b, "_texture"), Is.Null);
        }

        [Test]
        public void MissingSameAndDestroyedReferencesCannotSwap()
        {
            var pair = Component<FluxPingPong>();
            Assert.That(pair.Current, Is.Null);
            Assert.That(pair.Next, Is.Null);
            Error(); Assert.That(pair.TrySwap(), Is.False);
            var a = Buffer(3, 5, 13);
            Set(pair, "bufferA", a); Set(pair, "bufferB", a);
            Error(); Assert.That(pair.TrySwap(), Is.False);
            var b = Buffer(3, 5, 13);
            Assert.That(pair.TrySetBuffers(a, b), Is.True);
            Assert.That(pair.TrySwap(), Is.True);
            Object.DestroyImmediate(a);
            Error(); Assert.That(pair.TrySwap(), Is.False);
            Assert.That(pair.Current, Is.SameAs(b));
        }

        [Test]
        public void ReconfigurationChangesResetOrderWithoutReleasingOldBuffers()
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(3, 5, 13);
            var c = Buffer(5, 3, 7);
            Fill(a, new Color(-3, 4, -5, 0));
            Fill(b, new Color(7, -8, 9, -10));
            var aTexture = a.Texture;
            var bTexture = b.Texture;
            var pair = Pair(a, b);
            Assert.That(pair.TrySwap(), Is.True);
            Assert.That(pair.TrySetBuffers(c, a), Is.True);
            Assert.That(pair.Current, Is.SameAs(c));
            Assert.That(pair.Next, Is.SameAs(a));
            Assert.That(pair.TrySwap(), Is.True);
            pair.ResetOrder();
            Assert.That(pair.Current, Is.SameAs(c));
            Assert.That(a.Texture, Is.SameAs(aTexture));
            Assert.That(b.Texture, Is.SameAs(bTexture));
            Assert.That(aTexture.IsCreated() && bTexture.IsCreated(), Is.True);
            Assert.That(Read(aTexture)[0], Is.EqualTo(new Color(-3, 4, -5, 0)));
            Assert.That(Read(bTexture)[0], Is.EqualTo(new Color(7, -8, 9, -10)));
            Assert.That(Get(c, "_texture"), Is.Null);
        }

        [Test]
        public void DisableResetAndDestroyDoNotChangeCountsOrData()
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(5, 3, 7);
            Set(b, "filterMode", FluxBufferFilterMode.Bilinear);
            Fill(a, new Color(-3, 4, -5, 0));
            Fill(b, new Color(7, -8, 9, -10));
            a.SetCount(0); b.SetCount(5);
            var pair = Pair(a, b);
            Assert.That(pair.TrySwap(), Is.True);
            pair.gameObject.SetActive(false);
            pair.gameObject.SetActive(true);
            Assert.That(pair.Current, Is.SameAs(b));
            pair.ResetOrder();
            Assert.That(pair.Current, Is.SameAs(a));
            Object.DestroyImmediate(pair);
            Assert.That(a.Texture.IsCreated() && b.Texture.IsCreated(), Is.True);
            Assert.That(a.Count, Is.Zero);
            Assert.That(b.Count, Is.EqualTo(5));
            Assert.That(b.Texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(Read(a.Texture)[0], Is.EqualTo(new Color(-3, 4, -5, 0)));
            Assert.That(Read(b.Texture)[0], Is.EqualTo(new Color(7, -8, 9, -10)));
        }

        [Test]
        public void ReleaseAndSwapDoNotImplicitlyReallocateOrRestoreCounts()
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(3, 5, 13);
            Fill(a, Color.red); Fill(b, Color.blue);
            a.SetCount(3); b.SetCount(7);
            var bTexture = b.Texture;
            var pair = Pair(a, b);
            a.Release();
            Assert.That(pair.Current, Is.SameAs(a));
            Assert.That(pair.TrySwap(), Is.True);
            Assert.That(pair.Current, Is.SameAs(b));
            Assert.That(pair.Next, Is.SameAs(a));
            Assert.That(Get(a, "_texture"), Is.Null);
            Assert.That((int)Get(a, "_count"), Is.Zero);
            Assert.That(b.Count, Is.EqualTo(7));
            Assert.That(b.Texture, Is.SameAs(bTexture));
            // Only the caller's explicit access now reinitializes A, per FluxBuffer's contract.
            Assert.That(a.Texture.IsCreated(), Is.True);
            Assert.That(a.Count, Is.EqualTo(13));
            Assert.That(pair.Current, Is.SameAs(b));
            Assert.That(Read(bTexture)[0], Is.EqualTo(Color.blue));
        }

        [TestCase(FluxBufferFilterMode.Point)]
        [TestCase(FluxBufferFilterMode.Bilinear)]
        public void GpuFeedbackSwapsReferencesWithoutCopyingOrRollingBackData(FluxBufferFilterMode filter)
        {
            var a = Buffer(3, 5, 13);
            var b = Buffer(3, 5, 13);
            Set(b, "filterMode", filter);
            var pair = Pair(a, b);
            var seed = Kernel("Flux/Tests/FluxKernel/BufferContracts");
            var scale = Kernel("Flux/Tests/PingPongUpdate");
            seed.Dispatch(pair.Current, 1);
            var aTexture = a.Texture;
            var bTexture = b.Texture;
            var multiplier = 1f;
            for (var step = 1; step <= 3; step++)
            {
                scale.SetFloat("_Scale", -2f);
                scale.Dispatch(pair.Current, pair.Next, 0);
                Assert.That(pair.TrySwap(), Is.True);
                multiplier *= -2f;
                Compare(pair.Current, multiplier);
                Assert.That(pair.Current, Is.SameAs(step % 2 == 0 ? a : b));
            }
            pair.ResetOrder();
            Compare(pair.Current, 4f);
            Compare(pair.Next, -8f);
            Assert.That(a.Texture, Is.SameAs(aTexture));
            Assert.That(b.Texture, Is.SameAs(bTexture));
            Assert.That(a.Count, Is.EqualTo(13));
            Assert.That(b.Count, Is.EqualTo(13));
        }

        static void Compare(FluxBuffer buffer, float multiplier)
        {
            var actual = Read(buffer.Texture);
            for (var i = 0; i < actual.Length; i++)
            {
                var expected = i < 13 ? new Vector4(i, i % 3, i / 3, -i) * multiplier : Vector4.zero;
                for (var c = 0; c < 4; c++)
                {
                    Assert.That(actual[i][c], Is.EqualTo(expected[c]).Within(0.002f), $"element {i}, channel {c}");
                }
            }
        }

        FluxPingPong Pair(FluxBuffer a, FluxBuffer b)
        {
            var pair = Component<FluxPingPong>();
            Assert.That(pair.TrySetBuffers(a, b), Is.True);
            return pair;
        }

        FluxBuffer Buffer(int width, int height, int count)
        {
            var buffer = Component<FluxBuffer>();
            Set(buffer, "sizeMode", FluxBufferSizeMode.Explicit); Set(buffer, "width", width); Set(buffer, "height", height);
            Set(buffer, "initialCount", count);
            return buffer;
        }

        FluxKernel Kernel(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            var material = new Material(shader);
            var kernel = Component<FluxKernel>();
            _objects.Add(material);
            Set(kernel, "shader", shader); Set(kernel, "material", material);
            return kernel;
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

        static object Get(object target, string field)
        {
            return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void Error()
        {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("Ping-Pong requires two different, non-null Buffer components.") + "$"));
        }

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

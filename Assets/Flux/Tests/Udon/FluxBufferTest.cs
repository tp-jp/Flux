using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxBufferTest : FluxRuntimeTestBase
    {
        const int InitialCount = 250;
        const int ExpectedCapacity = 256;
        const int ExpectedTextureSize = 16;
        const int ChangedCount = 63;

        [SerializeField]
        FluxBuffer buffer;

        protected override string TestName => "FluxBuffer / Basic";

        public override void Run()
        {
            if (buffer.Count != InitialCount)
            {
                Fail($"Initial Count: expected {InitialCount}, actual {buffer.Count}");
                return;
            }

            if (buffer.Capacity != ExpectedCapacity)
            {
                Fail($"Capacity: expected {ExpectedCapacity}, actual {buffer.Capacity}");
                return;
            }

            var texture = buffer.Texture;

            if (texture.width != ExpectedTextureSize || texture.height != ExpectedTextureSize)
            {
                Fail($"Texture Size: expected {ExpectedTextureSize}x{ExpectedTextureSize}, actual {texture.width}x{texture.height}");
                return;
            }

            buffer.SetCount(ChangedCount);

            if (buffer.Count != ChangedCount)
            {
                Fail($"SetCount: expected {ChangedCount}, actual {buffer.Count}");
                return;
            }

            if (buffer.Capacity != ExpectedCapacity)
            {
                Fail($"Capacity after SetCount: expected {ExpectedCapacity}, actual {buffer.Capacity}");
                return;
            }

            if (texture.width != ExpectedTextureSize || texture.height != ExpectedTextureSize)
            {
                Fail($"Texture Size after SetCount: expected {ExpectedTextureSize}x{ExpectedTextureSize}, actual {texture.width}x{texture.height}");
                return;
            }

            Pass();
        }
    }
}
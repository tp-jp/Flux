using TpLab.Flux.Udon;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    public class FluxReadbackTest : FluxRuntimeTestBase
    {
        const int ExpectedCapacity = 256;
        const int ReadbackCount = 63;
        const int ChangedCount = 32;

        [SerializeField]
        FluxBuffer buffer;

        [SerializeField]
        FluxReadback readback;

        protected override string TestName => "FluxReadback / Logical Count";

        public override void Run()
        {
            buffer.SetCount(ReadbackCount);
            readback.Request(buffer, this);
            buffer.SetCount(ChangedCount);
        }

        public void _OnFluxReadbackComplete()
        {
            if (readback.Count != ReadbackCount)
            {
                Fail($"Expected count {ReadbackCount}, actual {readback.Count}");
                return;
            }

            if (buffer.Count != ChangedCount)
            {
                Fail($"Expected buffer count {ChangedCount}, actual {buffer.Count}");
                return;
            }

            if (readback.Data.Length != ExpectedCapacity)
            {
                Fail($"Expected data length {ExpectedCapacity}, actual {readback.Data.Length}");
                return;
            }

            Pass();
        }
    }
}
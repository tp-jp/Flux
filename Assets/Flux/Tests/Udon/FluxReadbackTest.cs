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

        int _phase;

        protected override string TestName => "FluxReadback / Request Lifecycle";

        public override void Run()
        {
            _phase = 0;
            if (readback.TryRequest(null, this) || readback.TryRequest(buffer, null))
            {
                Fail("Null arguments must reject the request.");
                return;
            }
            buffer.SetCount(ReadbackCount);
            if (!readback.TryRequest(buffer, this))
            {
                Fail("The initial request was rejected.");
                return;
            }
            if (!readback.IsRequesting || readback.Succeeded || readback.HasError || readback.Data != null)
            {
                Fail("Pending request state is invalid.");
                return;
            }
            buffer.SetCount(ChangedCount);
            if (readback.TryRequest(buffer, this) || readback.Count != ReadbackCount)
            {
                Fail("A duplicate request must not replace the pending request.");
            }
        }

        public void _OnFluxReadbackComplete()
        {
            if (readback.IsRequesting || readback.Succeeded == readback.HasError)
            {
                Fail("Completion must publish either success or failure.");
                return;
            }
            if (_phase == 1)
            {
                if (readback.Count != ChangedCount)
                {
                    Fail("Released buffer count was not preserved.");
                    return;
                }
                if (buffer.Texture.IsCreated())
                {
                    Fail("Completion must not recreate the released buffer.");
                    return;
                }
                if (readback.Succeeded ? readback.Data.Length != ExpectedCapacity : readback.Data != null)
                {
                    Fail("Completion data must match the request capacity and result.");
                    return;
                }
                _phase = 2;
                buffer.Texture.Create();
                buffer.SetCount(ReadbackCount);
                if (!readback.TryRequest(buffer, this))
                {
                    Fail("A request after buffer reinitialization was rejected.");
                }
                return;
            }
            if (!readback.Succeeded || readback.Count != ReadbackCount || readback.Data.Length != ExpectedCapacity)
            {
                Fail("Readback result, count or capacity is invalid.");
                return;
            }
            if (_phase == 0)
            {
                if (buffer.Count != ChangedCount)
                {
                    Fail("Buffer count must retain the change made after the request.");
                    return;
                }
                _phase = 1;
                readback.Request(buffer, this);
                if (!readback.IsRequesting)
                {
                    Fail("A request from the completion event was rejected.");
                    return;
                }
                buffer.Texture.Release();
                return;
            }
            Pass();
        }
    }
}

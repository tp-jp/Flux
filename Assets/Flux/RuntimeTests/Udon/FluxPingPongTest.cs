using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// 明示的なGPU更新と参照交換、順序リセット、組の再設定をUdon VMで検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxPingPongTest : FluxRuntimeTestBase
    {
        [SerializeField]
        FluxPingPong pair;

        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxKernel seedKernel;

        [SerializeField]
        FluxKernel scaleKernel;

        [SerializeField]
        FluxReadback readback;

        int _stage;
        float _multiplier;

        protected override string TestName => "FluxPingPong / Reference exchange and explicit GPU updates";

        public override void Run()
        {
            _stage = 0;
            _multiplier = 1f;
            pair.ResetOrder();
            if (pair.Current != bufferA || pair.Next != bufferB)
            {
                // Restore the original pair when the test is run again after reconfiguration.
                if (!pair.TrySetBuffers(bufferA, bufferB)) { Fail("Pair configuration failed."); return; }
            }
            seedKernel.Dispatch(pair.Current, 1);
            UpdateNext();
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded || readback.Data.Length != 15) { Fail("Readback failed."); return; }
            var data = readback.Data;
            for (var i = 0; i < data.Length; i++)
            {
                var expected = i < 13 ? new Vector4(i, i % 3, i / 3, -i) * _multiplier : Vector4.zero;
                var actual = (Vector4)data[i];
                if (!(Mathf.Abs(actual.x - expected.x) <= 0.002f && Mathf.Abs(actual.y - expected.y) <= 0.002f &&
                    Mathf.Abs(actual.z - expected.z) <= 0.002f && Mathf.Abs(actual.w - expected.w) <= 0.002f))
                {
                    Fail($"Stage {_stage}, element {i}: expected {expected}, actual {actual}");
                    return;
                }
            }
            if (bufferA.Count != 13 || bufferB.Count != 13) { Fail("Pair operations changed Count."); return; }
            if (_stage < 3) { UpdateNext(); return; }
            if (_stage == 3)
            {
                pair.ResetOrder();
                if (pair.Current != bufferA || pair.Next != bufferB) { Fail("ResetOrder changed the wrong references."); return; }
                _stage = 4;
                _multiplier = 4f;
                readback.Request(pair.Current, this);
                return;
            }
            if (_stage == 4)
            {
                if (!pair.TrySetBuffers(bufferB, bufferA) || !pair.TrySwap()) { Fail("Reconfiguration failed."); return; }
                pair.ResetOrder();
                if (pair.Current != bufferB || pair.Next != bufferA) { Fail("New reset order was not retained."); return; }
                _stage = 5;
                _multiplier = -8f;
                readback.Request(pair.Current, this);
                return;
            }
            Pass();
        }

        void UpdateNext()
        {
            var previous = pair.Current;
            var next = pair.Next;
            if (!scaleKernel.ValidateDispatch(next, 0)) { Fail("Kernel preparation failed."); return; }
            scaleKernel.SetFloat("_Scale", -2f);
            scaleKernel.Dispatch(previous, next, 0);
            if (!pair.TrySwap() || pair.Current != next || pair.Next != previous)
            {
                Fail("Swap failed.");
                return;
            }
            _stage++;
            _multiplier *= -2f;
            readback.Request(pair.Current, this);
        }
    }
}

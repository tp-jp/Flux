using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernelMultipleInputTest : FluxRuntimeTestBase
    {
        const int TestCount = 16;
        const float ScaleA = 1f;
        const float ScaleB = 10f;
        const float Tolerance = 0.01f;

        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxBuffer destinationBuffer;

        [SerializeField]
        FluxKernel initKernelA;

        [SerializeField]
        FluxKernel initKernelB;

        [SerializeField]
        FluxKernel addKernel;

        [SerializeField]
        FluxReadback readback;

        int _phase;

        protected override string TestName => "FluxKernel / Multiple Input and Texture Binding";

        public override void Run()
        {
            _phase = 0;
            initKernelA.SetFloat("_Scale", ScaleA);
            initKernelA.Dispatch(bufferA);

            initKernelB.SetFloat("_Scale", ScaleB);
            initKernelB.Dispatch(bufferB);

            addKernel.SetBuffer("_InputB", bufferB);
            addKernel.Dispatch(bufferA, destinationBuffer);

            readback.Request(destinationBuffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded)
            {
                Fail("GPU readback failed.");
                return;
            }
            var data = readback.Data;

            for (var i = 0; i < TestCount; i++)
            {
                var value = _phase == 0 ? i * (ScaleA + ScaleB)
                    : _phase == 1 ? i * ScaleA * 2 : i * ScaleA + 1;
                var expected = new Vector4(value, value, value, 2f);
                var actual = (Vector4)data[i];

                if (!Approximately(actual, expected))
                {
                    Fail($"Phase {_phase}, index {i}: expected {expected}, actual {actual}");
                    return;
                }
            }

            if (_phase < 2)
            {
                _phase++;
                if (_phase == 1)
                {
                    addKernel.SetTexture("_InputB", bufferA.Texture);
                }
                else
                {
                    addKernel.SetTexture("_InputB", Texture2D.whiteTexture);
                }
                addKernel.Dispatch(bufferA, destinationBuffer);
                readback.Request(destinationBuffer, this);
                return;
            }
            Pass();
        }

        bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Abs(a.x - b.x) <= Tolerance &&
                   Mathf.Abs(a.y - b.y) <= Tolerance &&
                   Mathf.Abs(a.z - b.z) <= Tolerance &&
                   Mathf.Abs(a.w - b.w) <= Tolerance;
        }
    }
}
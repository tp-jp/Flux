using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// 非正方形Buffer、指定Pass、符号付き加算をUdon実行とGPU読み戻しで検証します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxBufferKernelContractTest : FluxRuntimeTestBase
    {
        [SerializeField]
        FluxBuffer source;

        [SerializeField]
        FluxBuffer destination;

        [SerializeField]
        FluxKernel kernel;

        [SerializeField]
        FluxReadback readback;

        [SerializeField]
        [Tooltip("異常系のUdon検証を実行します。意図したエラーログが3件出るため、ConsoleのError Pauseを解除して実行してください。")]
        bool runInvalidInputChecks;

        int _stage;

        protected override string TestName => "FluxBuffer / Explicit dimensions, Pass selection and accumulation";

        public override void Run()
        {
            _stage = 0;
            if (source.Texture.width != 128 || source.Texture.height != 256 ||
                destination.Texture.width != 256 || destination.Texture.height != 128 ||
                source.Texture.filterMode != FilterMode.Point || destination.Texture.filterMode != FilterMode.Bilinear ||
                destination.Capacity != 32768 || destination.Count != 32768)
            {
                Fail("Explicit dimensions, filters or Count/Capacity do not match.");
                return;
            }
            var original = destination.Texture;
            destination.SetCount(123);
            if (destination.Texture != original || destination.Count != 123)
            {
                Fail("SetCount changed the Texture or did not change Count.");
                return;
            }
            kernel.Dispatch(source, 1);
            kernel.Dispatch(source, destination, 2);
            // Error-emitting cases are opt-in; Editor tests verify their expected logs separately.
            if (runInvalidInputChecks)
            {
                kernel.Dispatch(destination, -1);
                kernel.Dispatch(source, destination, 4);
                kernel.Dispatch(destination, destination, 2);
            }
            if (destination.Count != 123)
            {
                Fail("Dispatch changed output Count.");
                return;
            }
            readback.Request(destination, this);
        }

        public void _OnFluxReadbackComplete()
        {
            if (!readback.Succeeded)
            {
                Fail("GPU readback failed.");
                return;
            }
            var data = readback.Data;
            for (var i = 0; i < data.Length; i++)
            {
                var expected = _stage == 0
                    ? new Vector4(i, i % 128, i / 128, -i)
                    : i < 16384 ? new Vector4(0.5f, -1, 2, -4) : new Vector4(-1, 2, -4, 8);
                var actual = (Vector4)data[i];
                if (Mathf.Abs(actual.x - expected.x) > 0.001f || Mathf.Abs(actual.y - expected.y) > 0.001f ||
                    Mathf.Abs(actual.z - expected.z) > 0.001f || Mathf.Abs(actual.w - expected.w) > 0.001f)
                {
                    Fail($"Stage {_stage}, index {i}: expected {expected}, actual {actual}");
                    return;
                }
            }
            if (_stage == 0)
            {
                _stage = 1;
                kernel.Dispatch(destination, 0);
                kernel.SetFloat("_Length", 16384);
                kernel.SetFloat("_Start", 0);
                kernel.SetVector("_Value", new Vector4(0.25f, -0.5f, 1, -2));
                kernel.Dispatch(destination, 3);
                kernel.Dispatch(destination, 3);
                kernel.SetFloat("_Start", 16384);
                kernel.SetVector("_Value", new Vector4(-1, 2, -4, 8));
                kernel.Dispatch(destination, 3);
                readback.Request(destination, this);
                return;
            }
            var original = destination.Texture;
            destination.Release();
            if (destination.Texture == original || destination.Count != 32768 ||
                destination.Capacity != 32768 || destination.Texture.filterMode != FilterMode.Bilinear)
            {
                Fail("Release/reinitialization did not restore the explicit configuration.");
                return;
            }
            Pass();
        }
    }
}

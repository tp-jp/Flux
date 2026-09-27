using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReductionSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer inputBuffer;

        [SerializeField]
        FluxBuffer reduceBuffer1;

        [SerializeField]
        FluxBuffer reduceBuffer2;

        [SerializeField]
        FluxBuffer reduceBuffer3;

        [SerializeField]
        FluxBuffer resultBuffer;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxKernel reductionKernel;

        [SerializeField]
        FluxReadback readback;

        void Start()
        {
            var data = new Vector4[250];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(i + 1, 0, 0, 0);
            }

            upload.Upload(data, inputBuffer);

            reductionKernel.Dispatch(inputBuffer, reduceBuffer1);
            reductionKernel.Dispatch(reduceBuffer1, reduceBuffer2);
            reductionKernel.Dispatch(reduceBuffer2, reduceBuffer3);
            reductionKernel.Dispatch(reduceBuffer3, resultBuffer);
            
            readback.Request(resultBuffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            Debug.Log($"Reduction Result = {readback.Data[0]}");
        }
    }
}
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxMultipleInputSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxBuffer output;

        [SerializeField]
        FluxKernel initAKernel;

        [SerializeField]
        FluxKernel initBKernel;

        [SerializeField]
        FluxKernel addKernel;

        [SerializeField]
        FluxReadback readback;

        void Start()
        {
            initAKernel.SetFloat("_Scale", 1f);
            initAKernel.Dispatch(bufferA);

            initBKernel.SetFloat("_Scale", 10f);
            initBKernel.Dispatch(bufferB);

            addKernel.SetBuffer("_Other", bufferB);
            addKernel.Dispatch(bufferA, output);

            readback.Request(output, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            for (var i = 0; i < readback.Count; i++)
            {
                Debug.Log($"Flux[{i}] = {data[i]}");
            }
        }
    }
}
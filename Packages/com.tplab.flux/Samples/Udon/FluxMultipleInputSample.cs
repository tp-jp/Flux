using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxMultipleInputSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer _bufferA;

        [SerializeField]
        FluxBuffer _bufferB;

        [SerializeField]
        FluxBuffer _output;

        [SerializeField]
        FluxKernel _initAKernel;

        [SerializeField]
        FluxKernel _initBKernel;

        [SerializeField]
        FluxKernel _addKernel;

        [SerializeField]
        FluxReadback _readback;

        void Start()
        {
            _initAKernel.SetFloat("_Scale", 1f);
            _initAKernel.Dispatch(_bufferA);

            _initBKernel.SetFloat("_Scale", 10f);
            _initBKernel.Dispatch(_bufferB);

            _addKernel.SetBuffer("_Other", _bufferB);
            _addKernel.Dispatch(_bufferA, _output);

            _readback.Request(_output, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = _readback.Data;

            for (var i = 0; i < _readback.Count; i++)
            {
                Debug.Log($"Flux[{i}] = {data[i]}");
            }
        }
    }
}
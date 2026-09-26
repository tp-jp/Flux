using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.VectorScale.Samples.VectorScale
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxVectorScaleSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer _input;

        [SerializeField]
        FluxBuffer _output;

        [SerializeField]
        FluxKernel _kernel;

        [SerializeField]
        FluxReadback _readback;

        void Start()
        {
            _kernel.SetFloat("_Multiplier", 2f);
            _kernel.Dispatch(_input, _output);

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
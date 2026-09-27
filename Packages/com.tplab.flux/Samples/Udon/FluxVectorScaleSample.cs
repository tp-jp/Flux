using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxVectorScaleSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer input;

        [SerializeField]
        FluxBuffer output;

        [SerializeField]
        FluxKernel initKernel;

        [SerializeField]
        FluxKernel scaleKernel;

        [SerializeField]
        FluxReadback readback;

        void Start()
        {
            initKernel.SetFloat("_Scale", 0.5f);
            initKernel.Dispatch(input);

            scaleKernel.SetFloat("_Multiplier", 2f);
            scaleKernel.Dispatch(input, output);

            readback.Request(output, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;
            for (var i = 0; i < readback.Count; i++)
            {
                Debug.Log($"Flux[{i}] = {data[i]}");
            }
            // var result = new StringBuilder();
            // var data = _readback.Data;
            // for (var i = 0; i < _readback.Count; i++)
            // {
            //     result.AppendLine($"Flux[{i}] = {data[i]}");
            // }
            //
            // _infoText.text = result.ToString();
        }
    }
}
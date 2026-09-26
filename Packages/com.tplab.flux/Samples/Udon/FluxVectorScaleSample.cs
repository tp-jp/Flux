using System.Text;
using TMPro;
using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxVectorScaleSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer _input;

        [SerializeField]
        FluxBuffer _output;

        [SerializeField]
        FluxKernel _initKernel;

        [SerializeField]
        FluxKernel _scaleKernel;

        [SerializeField]
        FluxReadback _readback;

        [SerializeField]
        TMP_Text _infoText;

        void Start()
        {
            _initKernel.SetFloat("_Scale", 0.5f);
            _initKernel.Dispatch(_input);
            
            _scaleKernel.SetFloat("_Multiplier", 2f);
            _scaleKernel.Dispatch(_input, _output);
            
            _readback.Request(_output, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = _readback.Data;
            for (var i = 0; i < _readback.Count; i++)
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
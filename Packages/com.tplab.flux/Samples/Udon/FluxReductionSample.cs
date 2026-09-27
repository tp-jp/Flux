using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReductionSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer inputBuffer;

        [SerializeField]
        FluxBuffer resultBuffer;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxReduction reduction;

        [SerializeField]
        FluxReadback readback;

        [SerializeField]
        bool useNegativeValues;

        void Start()
        {
            var data = new Vector4[250];

            for (var i = 0; i < data.Length; i++)
            {
                var value = i + 1;

                if (useNegativeValues)
                {
                    value = -value;
                }

                data[i] = new Vector4(value, 0, 0, 0);
            }

            upload.Upload(data, inputBuffer);
            reduction.Reduce(inputBuffer, resultBuffer);
            readback.Request(resultBuffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            Debug.Log($"Reduction Result = {readback.Data[0]}");
        }
    }
}
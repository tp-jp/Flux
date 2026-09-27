using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxUploadSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer buffer;

        [SerializeField]
        FluxUpload upload;

        [SerializeField]
        FluxReadback readback;

        void Start()
        {
            var data = new Vector4[buffer.Count];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(i, i * 10, i * 100, 1);
            }

            upload.Upload(data, buffer);
            readback.Request(buffer, this);
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;
            var count = readback.Count;

            for (var i = 0; i < count; i++)
            {
                Debug.Log($"Flux[{i}] = {data[i]}");
            }
        }
    }
}
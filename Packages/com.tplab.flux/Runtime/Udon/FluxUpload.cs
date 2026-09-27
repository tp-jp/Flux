using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxUpload : UdonSharpBehaviour
    {
        const int MaxUploadCount = 512;
        
        [SerializeField]
        FluxKernel uploadKernel;

        public void Upload(Vector4[] data, FluxBuffer destination)
        {
            if (data.Length != destination.Count)
            {
                Debug.LogError($"[Flux] Upload count must match the destination count ({destination.Count}).");
                return;
            }
            if (data.Length > MaxUploadCount)
            {
                Debug.LogError($"[Flux] Upload count exceeds the maximum of {MaxUploadCount}.");
                return;
            }

            uploadKernel.SetVectorArray("_FluxUploadData", data);
            uploadKernel.Dispatch(destination);
        }
    }
}

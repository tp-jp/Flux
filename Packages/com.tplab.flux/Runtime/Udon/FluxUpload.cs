using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxUpload : UdonSharpBehaviour
    {
        const int MaxUploadCount = 1024;
        
        [SerializeField]
        FluxKernel uploadKernel;

        public void Upload(Vector4[] data, FluxBuffer destination)
        {
            if (data.Length > MaxUploadCount)
            {
                Debug.LogError($"[Flux] Upload count exceeds the maximum of {MaxUploadCount}.");
                return;
            }

            uploadKernel.SetVectorArray("_FluxUploadData", data);
            uploadKernel.SetFloat("_FluxUploadCount", data.Length);
            uploadKernel.Dispatch(destination);
        }
    }
}

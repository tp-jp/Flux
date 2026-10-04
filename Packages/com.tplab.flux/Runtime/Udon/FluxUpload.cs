using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// CPU上のVector配列をFluxBufferへアップロードします。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxUpload : UdonSharpBehaviour
    {
        // Questを含む実機検証で保証するVector配列の最大Upload数。
        const int MaxUploadCount = 512;

        [SerializeField]
        FluxKernel uploadKernel;

        /// <summary>
        /// Vector配列をDestination Bufferへアップロードします。
        /// </summary>
        /// <param name="data">アップロードするデータ</param>
        /// <param name="destination">データを書き込むBuffer</param>
        [PublicAPI]
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
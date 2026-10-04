using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// FluxBufferのGPUデータをCPUへ非同期で読み戻します。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReadback : UdonSharpBehaviour
    {
        const string CompleteEventName = "_OnFluxReadbackComplete";

        FluxBuffer _buffer;
        UdonSharpBehaviour _receiver;
        Color[] _data;
        int _count;
        bool _isRequesting;

        /// <summary>
        /// 最後に読み戻したデータを取得します。
        /// </summary>
        [PublicAPI]
        public Color[] Data => _data;

        /// <summary>
        /// 最後に読み戻したBufferの有効な要素数を取得します。
        /// </summary>
        [PublicAPI]
        public int Count => _count;

        /// <summary>
        /// GPU Readbackを実行中かどうかを取得します。
        /// </summary>
        [PublicAPI]
        public bool IsRequesting => _isRequesting;

        /// <summary>
        /// FluxBufferの非同期GPU Readbackを開始します。
        /// </summary>
        /// <param name="buffer">読み戻すBuffer</param>
        /// <param name="receiver">完了イベントを受け取るUdonSharpBehaviour</param>
        [PublicAPI]
        public void Request(FluxBuffer buffer, UdonSharpBehaviour receiver)
        {
            if (_isRequesting) return;

            _buffer = buffer;
            _receiver = receiver;
            _count = buffer.Count;
            _isRequesting = true;

            VRCAsyncGPUReadback.Request(buffer.Texture, 0, this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            _isRequesting = false;

            if (request.hasError)
            {
                Debug.LogError("[Flux] GPU readback failed.");
                return;
            }

            var capacity = _buffer.Capacity;

            if (_data == null || _data.Length != capacity)
            {
                _data = new Color[capacity];
            }

            if (!request.TryGetData(_data))
            {
                Debug.LogError("[Flux] Failed to copy GPU readback data.");
                return;
            }

            _receiver.SendCustomEvent(CompleteEventName);
        }
    }
}
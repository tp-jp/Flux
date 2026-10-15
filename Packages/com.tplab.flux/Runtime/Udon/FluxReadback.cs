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

        UdonSharpBehaviour _receiver;
        Color[] _data;
        int _count;
        bool _isRequesting;
        bool _succeeded;
        bool _hasError;

        /// <summary>
        /// 完了した要求のデータ。未取得・要求中・失敗時はnull
        /// </summary>
        [PublicAPI]
        public Color[] Data => _succeeded ? _data : null;

        /// <summary>
        /// 最後に受け付けた要求のBuffer有効要素数
        /// </summary>
        [PublicAPI]
        public int Count => _count;

        /// <summary>
        /// GPU Readbackの実行中かどうか
        /// </summary>
        [PublicAPI]
        public bool IsRequesting => _isRequesting;

        /// <summary>
        /// 最後に受け付けた要求が成功したかどうか。未取得・要求中はfalse
        /// </summary>
        [PublicAPI]
        public bool Succeeded => _succeeded;

        /// <summary>
        /// 最後に受け付けた要求が失敗したかどうか。未取得・要求中はfalse
        /// </summary>
        [PublicAPI]
        public bool HasError => _hasError;

        /// <summary>
        /// 非同期GPU Readbackを開始し、成功・失敗の両方で_OnFluxReadbackCompleteを通知します。
        /// 実行中または引数がnullの場合は要求を受け付けません。
        /// </summary>
        /// <param name="buffer">読み戻すBuffer</param>
        /// <param name="receiver">完了イベントの通知先</param>
        [PublicAPI]
        public void Request(FluxBuffer buffer, UdonSharpBehaviour receiver)
        {
            TryRequest(buffer, receiver);
        }

        /// <summary>
        /// 非同期GPU Readbackの要求を試みます。要求時のBuffer容量と有効要素数を保持します。
        /// 受け付けた要求は、成功・失敗の両方で_OnFluxReadbackCompleteを通知します。
        /// </summary>
        /// <param name="buffer">読み戻すBuffer</param>
        /// <param name="receiver">完了イベントの通知先</param>
        /// <returns>要求を受け付けた場合はtrue。実行中または引数がnullの場合はfalse</returns>
        [PublicAPI]
        public bool TryRequest(FluxBuffer buffer, UdonSharpBehaviour receiver)
        {
            if (_isRequesting || buffer == null || receiver == null) return false;

            var texture = buffer.Texture;
            var capacity = texture.width * texture.height;
            if (_data == null || _data.Length != capacity)
            {
                _data = new Color[capacity];
            }
            _receiver = receiver;
            _count = buffer.Count;
            _succeeded = false;
            _hasError = false;
            _isRequesting = true;

            VRCAsyncGPUReadback.Request(texture, 0, this);
            return true;
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            if (!_isRequesting) return;

            _succeeded = !request.hasError && request.TryGetData(_data);
            _hasError = !_succeeded;
            _isRequesting = false;
            var receiver = _receiver;
            _receiver = null;
            if (receiver == null) return;
            receiver.SendCustomEvent(CompleteEventName);
        }
    }
}

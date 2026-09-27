using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReadback : UdonSharpBehaviour
    {
        const string CompleteEventName = "_OnFluxReadbackComplete";

        FluxBuffer _buffer;
        UdonSharpBehaviour _receiver;
        Color[] _data;
        int _count;
        bool _isRequesting;

        public Color[] Data => _data;

        public int Count => _count;

        public bool IsRequesting => _isRequesting;

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
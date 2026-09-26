using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxPingPongSample : UdonSharpBehaviour
    {
        const int IterationCount = 100;

        [SerializeField]
        FluxBuffer _bufferA;

        [SerializeField]
        FluxBuffer _bufferB;

        [SerializeField]
        FluxKernel _initKernel;

        [SerializeField]
        FluxKernel _incrementKernel;

        [SerializeField]
        FluxReadback _readback;

        FluxBuffer _current;
        FluxBuffer _next;
        int _iteration;

        void Start()
        {
            _initKernel.SetFloat("_Scale", 1f);
            _initKernel.Dispatch(_bufferA);

            _incrementKernel.SetFloat("_Increment", 1f);

            _current = _bufferA;
            _next = _bufferB;
        }

        void Update()
        {
            if (_iteration >= IterationCount) return;

            _incrementKernel.Dispatch(_current, _next);

            SwapBuffers();

            _iteration++;

            if (_iteration == IterationCount)
            {
                _readback.Request(_current, this);
            }
        }

        public void _OnFluxReadbackComplete()
        {
            var data = _readback.Data;

            Debug.Log($"[Flux] Ping-Pong completed: {_iteration} iterations");

            for (var i = 0; i < _readback.Count; i++)
            {
                Debug.Log($"Flux[{i}] = {data[i]}");
            }
        }

        void SwapBuffers()
        {
            // ReSharper disable once SwapViaDeconstruction
            var temp = _current;
            _current = _next;
            _next = temp;
        }
    }
}
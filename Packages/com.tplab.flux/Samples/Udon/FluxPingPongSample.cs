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
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        [SerializeField]
        FluxKernel initKernel;

        [SerializeField]
        FluxKernel incrementKernel;

        [SerializeField]
        FluxReadback readback;

        FluxBuffer _current;
        FluxBuffer _next;
        int _iteration;

        void Start()
        {
            initKernel.SetFloat("_Scale", 1f);
            initKernel.Dispatch(bufferA);

            incrementKernel.SetFloat("_Increment", 1f);

            _current = bufferA;
            _next = bufferB;
        }

        void Update()
        {
            if (_iteration >= IterationCount) return;

            incrementKernel.Dispatch(_current, _next);

            SwapBuffers();

            _iteration++;

            if (_iteration == IterationCount)
            {
                readback.Request(_current, this);
            }
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            Debug.Log($"[Flux] Ping-Pong completed: {_iteration} iterations");

            for (var i = 0; i < readback.Count; i++)
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
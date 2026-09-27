using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleStateSample : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer stateA;

        [SerializeField]
        FluxBuffer stateB;

        [SerializeField]
        FluxKernel initKernel;

        [SerializeField]
        FluxKernel updateKernel;

        [SerializeField]
        Material renderMaterial;

        FluxBuffer _currentState;
        FluxBuffer _nextState;

        void Start()
        {
            initKernel.Dispatch(stateA);

            updateKernel.SetFloat("_Gravity", -9.81f);
            updateKernel.SetFloat("_Bounce", 0.8f);
            updateKernel.SetFloat("_MinX", -5f);
            updateKernel.SetFloat("_MaxX", 5f);
            updateKernel.SetFloat("_FloorY", 0f);

            _currentState = stateA;
            _nextState = stateB;

            ApplyRenderBuffer();
        }

        void Update()
        {
            updateKernel.SetFloat("_DeltaTime", Time.deltaTime);
            updateKernel.Dispatch(_currentState, _nextState);

            SwapBuffers();
            ApplyRenderBuffer();
        }

        void SwapBuffers()
        {
            var state = _currentState;
            _currentState = _nextState;
            _nextState = state;
        }

        void ApplyRenderBuffer()
        {
            var texture = _currentState.Texture;

            renderMaterial.SetTexture("_StateTex", texture);
            renderMaterial.SetFloat("_FluxCount", _currentState.Count);
            renderMaterial.SetFloat("_FluxWidth", texture.width);
            renderMaterial.SetFloat("_FluxHeight", texture.height);
        }
    }
}
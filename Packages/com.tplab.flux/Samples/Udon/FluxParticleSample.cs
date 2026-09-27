using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSample : UdonSharpBehaviour
    {
        const float DeltaTime = 0.1f;

        [SerializeField]
        FluxBuffer positionA;

        [SerializeField]
        FluxBuffer positionB;

        [SerializeField]
        FluxBuffer velocityA;

        [SerializeField]
        FluxBuffer velocityB;

        [SerializeField]
        FluxKernel initPositionKernel;

        [SerializeField]
        FluxKernel initVelocityKernel;

        [SerializeField]
        FluxKernel updatePositionKernel;

        [SerializeField]
        FluxKernel updateVelocityKernel;

        [SerializeField]
        Material renderMaterial;

        [SerializeField]
        bool simulate;
        
        FluxBuffer _currentPosition;
        FluxBuffer _nextPosition;
        FluxBuffer _currentVelocity;
        FluxBuffer _nextVelocity;

        void Start()
        {
            initPositionKernel.Dispatch(positionA);
            initVelocityKernel.Dispatch(velocityA);

            updateVelocityKernel.SetVector("_Gravity", new Vector4(0, -1, 0, 0));

            _currentPosition = positionA;
            _nextPosition = positionB;
            _currentVelocity = velocityA;
            _nextVelocity = velocityB;

            ApplyRenderBuffer();
        }

        void Update()
        {
            if (!simulate) return;

            updateVelocityKernel.SetFloat("_DeltaTime", DeltaTime);
            updateVelocityKernel.Dispatch(_currentVelocity, _nextVelocity);

            updatePositionKernel.SetFloat("_DeltaTime", DeltaTime);
            updatePositionKernel.SetBuffer("_Velocity", _nextVelocity);
            updatePositionKernel.Dispatch(_currentPosition, _nextPosition);

            SwapBuffers();
            ApplyRenderBuffer();
        }

        void SwapBuffers()
        {
            var position = _currentPosition;
            _currentPosition = _nextPosition;
            _nextPosition = position;

            var velocity = _currentVelocity;
            _currentVelocity = _nextVelocity;
            _nextVelocity = velocity;
        }

        void ApplyRenderBuffer()
        {
            var texture = _currentPosition.Texture;

            renderMaterial.SetTexture("_PositionTex", texture);
            renderMaterial.SetFloat("_FluxCount", _currentPosition.Count);
            renderMaterial.SetFloat("_FluxWidth", texture.width);
            renderMaterial.SetFloat("_FluxHeight", texture.height);
        }
    }
}
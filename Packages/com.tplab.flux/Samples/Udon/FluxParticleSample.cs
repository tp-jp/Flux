using TpLab.Flux.Udon;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Samples.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxParticleSample : UdonSharpBehaviour
    {
        const int SimulationFrameCount = 100;

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
        FluxReadback readback;

        FluxBuffer _currentPosition;
        FluxBuffer _nextPosition;
        FluxBuffer _currentVelocity;
        FluxBuffer _nextVelocity;
        int _frameCount;

        void Start()
        {
            initPositionKernel.Dispatch(positionA);
            initVelocityKernel.Dispatch(velocityA);

            updateVelocityKernel.SetVector("_Gravity", new Vector4(0, -1, 0, 0));

            _currentPosition = positionA;
            _nextPosition = positionB;
            _currentVelocity = velocityA;
            _nextVelocity = velocityB;
        }

        void Update()
        {
            if (_frameCount >= SimulationFrameCount) return;

            const float deltaTime = 0.1f;

            updateVelocityKernel.SetFloat("_DeltaTime", deltaTime);
            updateVelocityKernel.Dispatch(_currentVelocity, _nextVelocity);

            updatePositionKernel.SetFloat("_DeltaTime", deltaTime);
            updatePositionKernel.SetBuffer("_Velocity", _nextVelocity);
            updatePositionKernel.Dispatch(_currentPosition, _nextPosition);

            SwapBuffers();

            _frameCount++;

            if (_frameCount == SimulationFrameCount)
            {
                readback.Request(_currentPosition, this);
            }
        }

        public void _OnFluxReadbackComplete()
        {
            var data = readback.Data;

            Debug.Log($"[Flux] Particle simulation completed: {_frameCount} frames");

            for (var i = 0; i < readback.Count; i++)
            {
                Debug.Log($"Particle[{i}] Position = {data[i]}");
            }
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
    }
}
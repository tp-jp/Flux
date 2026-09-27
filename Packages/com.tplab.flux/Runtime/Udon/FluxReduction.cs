using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxReduction : UdonSharpBehaviour
    {
        [SerializeField]
        FluxKernel reductionKernel;

        [SerializeField]
        int reductionFactor = 4;

        [SerializeField]
        FluxBuffer workspaceA;

        [SerializeField]
        FluxBuffer workspaceB;

        public void Reduce(FluxBuffer source, FluxBuffer destination)
        {
            var sourceCount = source.Count;

            if (sourceCount <= 0)
            {
                Debug.LogError("[Flux] Reduction source must contain at least one element.");
                return;
            }

            if (reductionFactor <= 1)
            {
                Debug.LogError("[Flux] Reduction factor must be greater than 1.");
                return;
            }

            if (source == destination)
            {
                Debug.LogError("[Flux] Reduction source and destination must be different buffers.");
                return;
            }

            if (workspaceA == workspaceB)
            {
                Debug.LogError("[Flux] Reduction workspaces must be different buffers.");
                return;
            }

            if (source == workspaceA || source == workspaceB || destination == workspaceA || destination == workspaceB)
            {
                Debug.LogError("[Flux] Reduction source and destination must not use the reduction workspaces.");
                return;
            }

            if (destination.Capacity < 1)
            {
                Debug.LogError("[Flux] Reduction destination must have a capacity of at least 1.");
                return;
            }

            var requiredWorkspaceCount = GetReducedCount(sourceCount);

            if (requiredWorkspaceCount > workspaceA.Capacity || requiredWorkspaceCount > workspaceB.Capacity)
            {
                Debug.LogError($"[Flux] Reduction workspace capacity is too small. Required: {requiredWorkspaceCount}.");
                return;
            }

            var currentSource = source;
            var currentDestination = workspaceA;
            var useWorkspaceA = true;

            while (sourceCount > reductionFactor)
            {
                var destinationCount = GetReducedCount(sourceCount);

                currentDestination.SetCount(destinationCount);
                reductionKernel.Dispatch(currentSource, currentDestination);

                currentSource = currentDestination;
                sourceCount = destinationCount;

                useWorkspaceA = !useWorkspaceA;
                currentDestination = useWorkspaceA ? workspaceA : workspaceB;
            }

            destination.SetCount(1);
            reductionKernel.Dispatch(currentSource, destination);
        }

        int GetReducedCount(int count)
        {
            return (count + reductionFactor - 1) / reductionFactor;
        }
    }
}
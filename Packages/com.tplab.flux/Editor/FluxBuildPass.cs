using TpLab.Flux.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UnityEngine;

namespace TpLab.Flux.Editor
{
    public class FluxBuildPass : PassBase 
    {
        public override void Execute(SceneFlowContext context)
        {
            var kernels = Object.FindObjectsOfType<FluxKernel>(true);

            foreach (var kernel in kernels)
            {
                var material = new Material(kernel.Shader);
                material.name = $"{kernel.name} (Flux Kernel)";

                kernel.SetProgramVariable("material", material);
            }
        }
    }
}

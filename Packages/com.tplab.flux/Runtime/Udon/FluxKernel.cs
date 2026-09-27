using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernel : UdonSharpBehaviour
    {
        [SerializeField]
        Material material;

        public void SetFloat(string name, float value)
        {
            material.SetFloat(name, value);
        }

        public void SetVector(string name, Vector4 value)
        {
            material.SetVector(name, value);
        }

        public void SetVectorArray(string name, Vector4[] values)
        {
            material.SetVectorArray(name, values);
        }

        public void SetBuffer(string name, FluxBuffer buffer)
        {
            material.SetTexture(name, buffer.Texture);
        }

        public void Dispatch(FluxBuffer destination)
        {
            PrepareDestination(destination);

            VRCGraphics.Blit(null, destination.Texture, material);
        }

        public void Dispatch(FluxBuffer source, FluxBuffer destination)
        {
            PrepareDestination(destination);
            PrepareSource(source);

            VRCGraphics.Blit(source.Texture, destination.Texture, material);
        }

        void PrepareSource(FluxBuffer source)
        {
            var texture = source.Texture;

            material.SetFloat("_FluxSourceCount", source.Count);
            material.SetFloat("_FluxSourceWidth", texture.width);
            material.SetFloat("_FluxSourceHeight", texture.height);
        }

        void PrepareDestination(FluxBuffer destination)
        {
            var texture = destination.Texture;

            material.SetFloat("_FluxDestinationCount", destination.Count);
            material.SetFloat("_FluxDestinationWidth", texture.width);
            material.SetFloat("_FluxDestinationHeight", texture.height);
        }
    }
}
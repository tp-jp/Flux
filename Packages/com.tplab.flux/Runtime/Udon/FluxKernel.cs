using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
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

        public void SetBuffer(string name, FluxBuffer buffer)
        {
            material.SetTexture(name, buffer.Texture);
        }

        public void Dispatch(FluxBuffer destination)
        {
            Prepare(destination);

            VRCGraphics.Blit(null, destination.Texture, material);
        }

        public void Dispatch(FluxBuffer source, FluxBuffer destination)
        {
            Prepare(destination);

            VRCGraphics.Blit(source.Texture, destination.Texture, material);
        }

        void Prepare(FluxBuffer destination)
        {
            var texture = destination.Texture;

            material.SetFloat("_FluxCount", destination.Count);
            material.SetFloat("_FluxWidth", texture.width);
            material.SetFloat("_FluxHeight", texture.height);
        }
    }
}
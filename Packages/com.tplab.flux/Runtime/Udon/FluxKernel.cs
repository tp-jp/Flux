using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernel : UdonSharpBehaviour
    {
        [SerializeField]
        Material _material;

        public void SetFloat(string name, float value)
        {
            _material.SetFloat(name, value);
        }

        public void SetVector(string name, Vector4 value)
        {
            _material.SetVector(name, value);
        }

        public void SetBuffer(string name, FluxBuffer buffer)
        {
            _material.SetTexture(name, buffer.Texture);
        }

        public void Dispatch(FluxBuffer destination)
        {
            Prepare(destination);

            VRCGraphics.Blit(null, destination.Texture, _material);
        }

        public void Dispatch(FluxBuffer source, FluxBuffer destination)
        {
            Prepare(destination);

            VRCGraphics.Blit(source.Texture, destination.Texture, _material);
        }

        void Prepare(FluxBuffer destination)
        {
            var texture = destination.Texture;

            _material.SetFloat("_FluxCount", destination.Count);
            _material.SetFloat("_FluxWidth", texture.width);
            _material.SetFloat("_FluxHeight", texture.height);
        }
    }
}
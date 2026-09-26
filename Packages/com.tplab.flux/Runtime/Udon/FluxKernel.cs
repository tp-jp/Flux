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

        public void Dispatch(FluxBuffer source, FluxBuffer destination)
        {
            _material.SetFloat("_FluxCount", source.Count);

            VRCGraphics.Blit(source.Texture, destination.Texture, _material);
        }
    }
}
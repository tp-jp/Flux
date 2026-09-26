using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxBuffer : UdonSharpBehaviour
    {
        [SerializeField]
        RenderTexture _texture;

        [SerializeField]
        int _count;

        public RenderTexture Texture => _texture;

        public int Count => _count;

        public int Capacity => _texture.width * _texture.height;
    }
}
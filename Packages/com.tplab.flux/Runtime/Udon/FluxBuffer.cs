using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxBuffer : UdonSharpBehaviour
    {
        [SerializeField]
        int count;

        RenderTexture _texture;

        public RenderTexture Texture
        {
            get
            {
                EnsureInitialized();
                return _texture;
            }
        }

        public int Count => count;

        public int Capacity => _texture.width * _texture.height;

        void Start()
        {
            EnsureInitialized();
        }

        void OnDestroy()
        {
            Release();
        }

        void EnsureInitialized()
        {
            if (_texture != null) return;

            var size = Mathf.CeilToInt(Mathf.Sqrt(count));
            _texture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.useMipMap = false;
            _texture.autoGenerateMips = false;

            _texture.Create();
        }

        void Release()
        {
            if (_texture == null) return;

            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }
    }
}
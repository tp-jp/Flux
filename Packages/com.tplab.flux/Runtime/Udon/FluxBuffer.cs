using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// GPU上のデータをRenderTextureとして保持するBufferです。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxBuffer : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(1)]
        int initialCount = 1;

        RenderTexture _texture;
        int _count;

        /// <summary>
        /// Bufferのデータを保持するRenderTextureを取得します。
        /// </summary>
        [PublicAPI]
        public RenderTexture Texture
        {
            get
            {
                EnsureInitialized();
                return _texture;
            }
        }

        /// <summary>
        /// Bufferの有効な要素数を取得します。
        /// </summary>
        [PublicAPI]
        public int Count
        {
            get
            {
                EnsureInitialized();
                return _count;
            }
        }

        /// <summary>
        /// Bufferが保持できる最大要素数を取得します。
        /// </summary>
        [PublicAPI]
        public int Capacity
        {
            get
            {
                EnsureInitialized();
                return _texture.width * _texture.height;
            }
        }

        /// <summary>
        /// Bufferの有効な要素数を設定します。
        /// </summary>
        /// <param name="count">設定する要素数</param>
        [PublicAPI]
        public void SetCount(int count)
        {
            EnsureInitialized();

            if (count < 0 || count > Capacity)
            {
                Debug.LogError($"[Flux] Count must be between 0 and Capacity ({Capacity}).");
                return;
            }

            _count = count;
        }

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

            var size = Mathf.CeilToInt(Mathf.Sqrt(initialCount));

            _texture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.useMipMap = false;
            _texture.autoGenerateMips = false;
            _texture.Create();

            _count = initialCount;
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
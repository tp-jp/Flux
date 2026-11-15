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

        [SerializeField]
        FluxBufferSizeMode sizeMode;

        [SerializeField]
        [Min(1)]
        int width = 1;

        [SerializeField]
        [Min(1)]
        int height = 1;

        [SerializeField]
        FluxBufferFilterMode filterMode;

        RenderTexture _texture;
        int _count;

        /// <summary>
        /// Bufferのデータを保持するRenderTexture。確保失敗時はnull。初期内容は未定義
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
        /// Bufferの有効な要素数。確保失敗時は0
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
        /// Bufferの容量（幅×高さ）。確保失敗時は0
        /// </summary>
        [PublicAPI]
        public int Capacity
        {
            get
            {
                EnsureInitialized();
                return _texture != null ? _texture.width * _texture.height : 0;
            }
        }

        /// <summary>
        /// Bufferの有効な要素数を設定します。既存Textureの再確保や内容の初期化は行いません。
        /// 未確保の場合は、他のアクセスと同じく初期設定で遅延確保します。
        /// </summary>
        /// <param name="count">設定する要素数</param>
        [PublicAPI]
        public void SetCount(int count)
        {
            if (!EnsureInitialized()) return;

            var capacity = _texture.width * _texture.height;
            if (count < 0 || count > capacity)
            {
                Logger.LogError($"Count must be between 0 and Capacity ({capacity}).");
                return;
            }

            _count = count;
        }

        /// <summary>
        /// 所有するTextureを解放します。次回の利用時に初期設定で再確保します。
        /// </summary>
        [PublicAPI]
        public void Release()
        {
            if (_texture == null) return;

            _texture.Release();
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (Application.isPlaying)
            {
                Destroy(_texture);
            }
            else
            {
                DestroyImmediate(_texture);
            }
#else
            Destroy(_texture);
#endif
            _texture = null;
            _count = 0;
        }

        void Start()
        {
            EnsureInitialized();
        }

        void OnDestroy()
        {
            Release();
        }

        bool EnsureInitialized()
        {
            if (_texture != null) return true;

            if (initialCount < 1)
            {
                Logger.LogError("Initial Count must be positive.");
                return false;
            }
            if (sizeMode != FluxBufferSizeMode.Automatic && sizeMode != FluxBufferSizeMode.Explicit)
            {
                Logger.LogError("Unknown Buffer size mode.");
                return false;
            }
            if (filterMode != FluxBufferFilterMode.Point && filterMode != FluxBufferFilterMode.Bilinear)
            {
                Logger.LogError("Buffer filter must be Point or Bilinear.");
                return false;
            }

            var textureWidth = width;
            var textureHeight = height;
            if (sizeMode == FluxBufferSizeMode.Automatic)
            {
                textureWidth = Mathf.CeilToInt(Mathf.Sqrt(initialCount));
                textureHeight = textureWidth;
            }
            var capacity = (long)textureWidth * textureHeight;
            if (textureWidth < 1 || textureHeight < 1 || capacity > int.MaxValue)
            {
                Logger.LogError("Buffer dimensions must be positive and capacity must fit in Int32.");
                return false;
            }
            if (initialCount > capacity)
            {
                Logger.LogError("Initial Count exceeds Buffer capacity.");
                return false;
            }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            // Udon exposes neither hardware query; it validates Create and the actual Texture below.
            if (textureWidth > SystemInfo.maxTextureSize || textureHeight > SystemInfo.maxTextureSize)
            {
                Logger.LogError($"Buffer dimensions exceed GPU maximum Texture size ({SystemInfo.maxTextureSize}).");
                return false;
            }
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            {
                Logger.LogError("GPU does not support ARGBFloat RenderTextures.");
                return false;
            }
#endif

            _texture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBFloat);
            _texture.filterMode = filterMode == FluxBufferFilterMode.Point ? FilterMode.Point : FilterMode.Bilinear;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.useMipMap = false;
            _texture.autoGenerateMips = false;
            if (!_texture.Create() || !_texture.IsCreated() || _texture.width != textureWidth ||
                _texture.height != textureHeight || _texture.format != RenderTextureFormat.ARGBFloat)
            {
                Logger.LogError($"Failed to allocate {textureWidth}x{textureHeight} ARGBFloat Buffer.");
                Release();
                return false;
            }

            _count = initialCount;
            return true;
        }
    }
}

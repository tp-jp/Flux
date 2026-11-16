using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// XYZセルをZスライス単位で2Dアトラスへ配置するための情報と座標変換を提供します。
    /// Textureは所有せず、確保・解放・初期化はFluxBufferと利用側のKernelに任せます。
    /// BufferはAtlasWidth×AtlasHeight、CountはAtlasCapacityに設定してください。
    /// 有効な3Dセル数はCellCountであり、アトラス末尾の未使用タイルは含みません。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxGrid3D : UdonSharpBehaviour
    {
        [SerializeField]
        [Min(1)]
        int sizeX = 1;

        [SerializeField]
        [Min(1)]
        int sizeY = 1;

        [SerializeField]
        [Min(1)]
        int sizeZ = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Zスライスを横に並べる数。0はceil(sqrt(sizeZ))で自動決定します。")]
        int sliceColumns;

        int _columns;
        int _rows;
        int _width;
        int _height;
        int _cellCount;

        /// <summary>
        /// 設定されたXYZセル数
        /// </summary>
        [PublicAPI]
        public Vector3Int Size => new Vector3Int(sizeX, sizeY, sizeZ);

        /// <summary>
        /// 有効な3Dセル数。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int CellCount => ValidateLayout() ? _cellCount : 0;

        /// <summary>
        /// スライスタイルの列数。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int AtlasColumns => ValidateLayout() ? _columns : 0;

        /// <summary>
        /// スライスタイルの行数。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int AtlasRows => ValidateLayout() ? _rows : 0;

        /// <summary>
        /// アトラスの幅。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int AtlasWidth => ValidateLayout() ? _width : 0;

        /// <summary>
        /// アトラスの高さ。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int AtlasHeight => ValidateLayout() ? _height : 0;

        /// <summary>
        /// 未使用タイルを含むアトラス容量。設定不正時は0
        /// </summary>
        [PublicAPI]
        public int AtlasCapacity => ValidateLayout() ? _width * _height : 0;

        /// <summary>
        /// Shaderへ渡すXYZセル数と予約値0。設定不正時はゼロVector
        /// </summary>
        [PublicAPI]
        public Vector4 GridSize => ValidateLayout() ? new Vector4(sizeX, sizeY, sizeZ, 0) : Vector4.zero;

        /// <summary>
        /// Shaderへ渡す列数・行数・アトラス幅・高さ。設定不正時はゼロVector
        /// </summary>
        [PublicAPI]
        public Vector4 AtlasLayout => ValidateLayout() ? new Vector4(_columns, _rows, _width, _height) : Vector4.zero;

        /// <summary>
        /// セル寸法、列数、容量のオーバーフローを検証します。Textureは確保しません。
        /// Shaderへのfloat転送で整数を正確に表せるよう、各寸法は16777216以下に制限します。
        /// </summary>
        /// <returns>配置が有効ならtrue</returns>
        [PublicAPI]
        public bool ValidateLayout()
        {
            if (sizeX < 1 || sizeY < 1 || sizeZ < 1 || sizeX > 16777216 || sizeY > 16777216 || sizeZ > 16777216)
            {
                Logger.LogError("Grid dimensions must be between 1 and 16777216.");
                return false;
            }
            if (sliceColumns < 0 || sliceColumns > sizeZ)
            {
                Logger.LogError("Grid slice columns must be 0 (automatic) or between 1 and sizeZ.");
                return false;
            }
            var xy = (long)sizeX * sizeY;
            if (xy > int.MaxValue || xy * sizeZ > int.MaxValue)
            {
                Logger.LogError("Grid cell count must fit in Int32.");
                return false;
            }
            var columns = sliceColumns == 0 ? Mathf.CeilToInt(Mathf.Sqrt(sizeZ)) : sliceColumns;
            var rows = sizeZ / columns + (sizeZ % columns == 0 ? 0 : 1);
            var width = (long)sizeX * columns;
            var height = (long)sizeY * rows;
            if (width > 16777216 || height > 16777216 || width * height > int.MaxValue)
            {
                Logger.LogError("Grid atlas dimensions must fit exact float integers and capacity must fit in Int32.");
                return false;
            }
            _columns = columns;
            _rows = rows;
            _width = (int)width;
            _height = (int)height;
            _cellCount = (int)(xy * sizeZ);
            return true;
        }

        /// <summary>
        /// 3Dセル座標をアトラスの整数pixel座標へ変換します。
        /// </summary>
        /// <param name="x">Xセル座標</param>
        /// <param name="y">Yセル座標</param>
        /// <param name="z">Zセル座標</param>
        /// <returns>pixel座標。範囲外または設定不正時は(-1, -1)</returns>
        [PublicAPI]
        public Vector2Int GetPixel(int x, int y, int z)
        {
            if (!ValidateLayout() || !IsCellInRange(x, y, z)) return new Vector2Int(-1, -1);
            return new Vector2Int((z % _columns) * sizeX + x, (z / _columns) * sizeY + y);
        }

        /// <summary>
        /// アトラスの整数pixel座標を3Dセル座標へ変換します。
        /// </summary>
        /// <param name="pixelX">アトラスのX座標</param>
        /// <param name="pixelY">アトラスのY座標</param>
        /// <returns>セル座標。範囲外・未使用タイル・設定不正時は(-1, -1, -1)</returns>
        [PublicAPI]
        public Vector3Int GetCell(int pixelX, int pixelY)
        {
            if (!ValidateLayout() || pixelX < 0 || pixelY < 0 || pixelX >= _width || pixelY >= _height)
                return new Vector3Int(-1, -1, -1);
            var z = (pixelY / sizeY) * _columns + pixelX / sizeX;
            if (z >= sizeZ) return new Vector3Int(-1, -1, -1);
            return new Vector3Int(pixelX % sizeX, pixelY % sizeY, z);
        }

        /// <summary>
        /// セルをアトラスの行優先インデックスへ変換します。3Dの連続インデックスではありません。
        /// </summary>
        /// <param name="x">Xセル座標</param>
        /// <param name="y">Yセル座標</param>
        /// <param name="z">Zセル座標</param>
        /// <returns>アトラス内のインデックス。範囲外または設定不正時は-1</returns>
        [PublicAPI]
        public int GetAtlasIndex(int x, int y, int z)
        {
            var pixel = GetPixel(x, y, z);
            return pixel.x < 0 ? -1 : pixel.y * _width + pixel.x;
        }

        /// <summary>
        /// セルをアトラスのtexel中心UVへ変換します。
        /// </summary>
        /// <param name="x">Xセル座標</param>
        /// <param name="y">Yセル座標</param>
        /// <param name="z">Zセル座標</param>
        /// <returns>texel中心UV。範囲外または設定不正時は(-1, -1)</returns>
        [PublicAPI]
        public Vector2 GetUV(int x, int y, int z)
        {
            var pixel = GetPixel(x, y, z);
            if (pixel.x < 0) return new Vector2(-1, -1);
            return new Vector2((pixel.x + 0.5f) / _width, (pixel.y + 0.5f) / _height);
        }

        /// <summary>
        /// 準備済みKernelへ_FluxGridTexture・_FluxGridSize・_FluxGridLayoutを設定します。
        /// Bufferの内容・Countは変更せず、Textureの所有権も取得しません。
        /// </summary>
        /// <param name="kernel">Material準備済みのKernel</param>
        /// <param name="buffer">全アトラスを有効要素とするBuffer</param>
        /// <returns>設定に成功した場合はtrue</returns>
        [PublicAPI]
        public bool TryBind(FluxKernel kernel, FluxBuffer buffer)
        {
            return TryBind(kernel, buffer, "_FluxGrid");
        }

        /// <summary>
        /// 指定prefixにTexture・Size・Layoutを付けたプロパティへ配置情報を設定します。
        /// KernelのShaderはこれらのプロパティを宣言しておく必要があります。
        /// Bufferは配置と同寸法かつCount=Capacityが必要です。未使用セルの判定はHLSLで行います。
        /// Textureの初期化やDispatchは行いません。追加Texture入力の入出力重複検証は利用側の責務です。
        /// </summary>
        /// <param name="kernel">Material準備済みのKernel</param>
        /// <param name="buffer">全アトラスを有効要素とするBuffer</param>
        /// <param name="propertyPrefix">Shaderプロパティの接頭辞</param>
        /// <returns>設定に成功した場合はtrue。失敗時はKernelの設定を変更しない</returns>
        [PublicAPI]
        public bool TryBind(FluxKernel kernel, FluxBuffer buffer, string propertyPrefix)
        {
            if (!ValidateLayout()) return false;
            if (kernel == null || buffer == null || string.IsNullOrEmpty(propertyPrefix))
            {
                Logger.LogError("Grid binding requires a Kernel, Buffer and nonempty property prefix.");
                return false;
            }
            var texture = buffer.Texture;
            if (texture == null) return false;
            if (texture.width != _width || texture.height != _height || buffer.Count != _width * _height)
            {
                Logger.LogError("Grid Buffer must match atlas dimensions and use Count = AtlasCapacity.");
                return false;
            }
            kernel.SetTexture(propertyPrefix + "Texture", texture);
            kernel.SetVector(propertyPrefix + "Size", new Vector4(sizeX, sizeY, sizeZ, 0));
            kernel.SetVector(propertyPrefix + "Layout", new Vector4(_columns, _rows, _width, _height));
            return true;
        }

        bool IsCellInRange(int x, int y, int z)
        {
            return x >= 0 && y >= 0 && z >= 0 && x < sizeX && y < sizeY && z < sizeZ;
        }
    }
}

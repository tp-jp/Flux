using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// 座標・符号付きfloat4値・有効フラグを3Dグリッドの周囲8セルへ重み付き加算します。
    /// FluxScatter.hlslのテッセレーション／Geometry Shader方式専用です。標準ShaderはD3D11用です。
    /// UdonではGPU対応状況を検出できないため、利用側で対象環境を限定してください。
    /// BufferとMaterialは所有せず、Materialの準備はSceneFlow／FluxBuildPassが行います。
    /// 入力の選別・単位・座標変換は利用側の責務です。境界外の寄与は捨て、再正規化しません。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxScatter : UdonSharpBehaviour
    {
        [SerializeField]
        FluxGrid3D grid;

        [SerializeField]
        FluxKernel kernel;

        /// <summary>
        /// 1回の加算で扱える最大入力件数（64×64点、32 Geometry Shader instances）
        /// </summary>
        [PublicAPI]
        public const int MaxInputCountPerDispatch = 131072;

        /// <summary>
        /// 指定したクリアPassを1回実行します。標準Flux/Scatter ShaderではPass 0です。
        /// カスタムShaderはBlend Offで全出力をゼロにするPassを用意してください。
        /// </summary>
        /// <param name="destination">全アトラスを有効要素とする出力Buffer</param>
        /// <param name="pass">クリア用Pass番号</param>
        /// <returns>検証を通過してDispatchを発行した場合はtrue。GPU完了の通知ではない</returns>
        [PublicAPI]
        public bool TryClear(FluxBuffer destination, int pass)
        {
            if (!ValidateDestination(destination, pass)) return false;
            kernel.Dispatch(destination, pass);
            return true;
        }

        /// <summary>
        /// 指定範囲を1回のDispatchで加算します。標準ShaderではPass 1です。
        /// 先にTryClearを1回呼び、分割範囲ごとに本APIを呼んでください。自動クリア・分割はしません。
        /// Count・値の符号・精度は変更しません。加算の丸め誤差は実行順に依存します。
        /// 入力はKernelに設定したTexture等からFluxScatterReadが直接読み出します。
        /// 入力の実際の長さと出力とのTexture重複は利用側で検証してください。
        /// </summary>
        /// <param name="destination">配置と同寸法かつCount=AtlasCapacityの出力Buffer</param>
        /// <param name="inputStart">入力開始位置。0以上で、開始位置＋件数は16777216以下</param>
        /// <param name="inputCount">0～MaxInputCountPerDispatch。0件は範囲だけを検証し、描画も確保もしない</param>
        /// <param name="pass">Blend One Oneを使用する加算Pass番号</param>
        /// <returns>範囲が有効な0件、または検証を通過してDispatchを発行した場合はtrue</returns>
        [PublicAPI]
        public bool TryAccumulate(FluxBuffer destination, int inputStart, int inputCount, int pass)
        {
            if (inputStart < 0 || inputCount < 0 || inputCount > MaxInputCountPerDispatch ||
                (long)inputStart + inputCount > 16777216)
            {
                Logger.LogError("Scatter range requires start >= 0, count in [0, 131072], and end <= 16777216.");
                return false;
            }
            if (inputCount == 0) return true;
            if (!ValidateDestination(destination, pass)) return false;
            kernel.SetVector("_FluxGridSize", grid.GridSize);
            kernel.SetVector("_FluxGridLayout", grid.AtlasLayout);
            kernel.SetVector("_FluxScatterRange", new Vector4(inputStart, inputCount, 0, 0));
            kernel.Dispatch(destination, pass);
            return true;
        }

        bool ValidateDestination(FluxBuffer destination, int pass)
        {
            if (!grid.ValidateLayout() || !kernel.ValidateDispatch(destination, pass)) return false;
            var texture = destination.Texture;
            if (texture.width != grid.AtlasWidth || texture.height != grid.AtlasHeight || destination.Count != grid.AtlasCapacity)
            {
                Logger.LogError("Scatter destination must match grid atlas dimensions and use Count = AtlasCapacity.");
                return false;
            }
            return true;
        }
    }
}

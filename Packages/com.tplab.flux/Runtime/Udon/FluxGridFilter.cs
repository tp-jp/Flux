using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// 指定軸の中心と左右3セルまでを対称な重みで平滑化します。
    /// 1呼び出しにつき1Passを実行します。軸の順序・反復回数・Buffer交換は利用側が管理します。
    /// BufferとMaterialは所有せず、Materialの準備はSceneFlow／FluxBuildPassが行います。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxGridFilter : UdonSharpBehaviour
    {
        [SerializeField]
        FluxGrid3D grid;

        [SerializeField]
        FluxKernel kernel;

        /// <summary>
        /// 1軸の重み付き和で出力アトラス全体を上書きします。標準Flux/GridFilter ShaderはPass 0です。
        /// 符号付きfloat4をそのまま処理し、Count・精度・重みの総和を変更しません。
        /// weights=(中心, 左右1セルそれぞれ, 左右2セルそれぞれ, 左右3セルそれぞれ)です。
        /// 3点は(1/2,1/4,0,0)、5点は(6/16,4/16,1/16,0)、7点は(20/64,15/64,6/64,1/64)で指定できます。
        /// 自動クリア・追加Buffer確保は行わず、未使用タイルには0を書きます。
        /// カスタムShaderは同じuniform契約とBlend Offを使用してください。追加Texture入力の重複検証は利用側の責務です。
        /// </summary>
        /// <param name="source">配置と同寸法かつCount=AtlasCapacityの入力Buffer。内容は初期化済みであること</param>
        /// <param name="destination">入力とは異なるTextureを持つ、同寸法・同Countの出力Buffer</param>
        /// <param name="axis">平滑化するX・Y・Z軸</param>
        /// <param name="boundary">境界外を0とするか、端のセル座標へClampするかの指定</param>
        /// <param name="weights">有限かつ非負の4つの重み。総和も有限であること。全0も可。自動正規化なし</param>
        /// <param name="pass">フィルタ用Pass番号</param>
        /// <returns>検証を通過してDispatchを発行した場合はtrue。GPU完了の通知ではない</returns>
        [PublicAPI]
        public bool TryFilter(FluxBuffer source, FluxBuffer destination, FluxGridAxis axis,
            FluxGridBoundaryMode boundary, Vector4 weights, int pass)
        {
            if ((int)axis < 0 || (int)axis > 2 || (int)boundary < 0 || (int)boundary > 1)
            {
                Logger.LogError("Grid filter requires axis X/Y/Z and boundary Zero/Clamp.");
                return false;
            }
            var sum = weights.x + 2f * (weights.y + weights.z + weights.w);
            if (weights.x < 0 || weights.y < 0 || weights.z < 0 || weights.w < 0 || float.IsNaN(sum) || float.IsInfinity(sum))
            {
                Logger.LogError("Grid filter weights must be finite, nonnegative, and have a finite total.");
                return false;
            }
            if (!grid.ValidateLayout() || !kernel.ValidateDispatch(destination, pass)) return false;
            if (source == null)
            {
                Logger.LogError("Grid filter source Buffer is required.");
                return false;
            }
            var input = source.Texture;
            if (input == null) return false;
            var output = destination.Texture;
            if (input == output)
            {
                Logger.LogError("Grid filter source and destination must use different Textures.");
                return false;
            }
            if (input.width != grid.AtlasWidth || input.height != grid.AtlasHeight || source.Count != grid.AtlasCapacity ||
                output.width != grid.AtlasWidth || output.height != grid.AtlasHeight || destination.Count != grid.AtlasCapacity)
            {
                Logger.LogError("Grid filter Buffers must match atlas dimensions and use Count = AtlasCapacity.");
                return false;
            }
            kernel.SetVector("_FluxGridSize", grid.GridSize);
            kernel.SetVector("_FluxGridLayout", grid.AtlasLayout);
            kernel.SetVector("_FluxGridFilterAxis", axis == FluxGridAxis.X ? new Vector4(1, 0, 0, 0) :
                axis == FluxGridAxis.Y ? new Vector4(0, 1, 0, 0) : new Vector4(0, 0, 1, 0));
            kernel.SetVector("_FluxGridFilterWeights", weights);
            kernel.SetFloat("_FluxGridFilterBoundary", boundary == FluxGridBoundaryMode.Clamp ? 1f : 0f);
            kernel.Dispatch(source, destination, pass);
            return true;
        }
    }
}

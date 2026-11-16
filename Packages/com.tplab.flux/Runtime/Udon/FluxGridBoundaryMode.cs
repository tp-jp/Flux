namespace TpLab.Flux.Udon
{
    /// <summary>
    /// グリッドの範囲外セルの読み出し方式
    /// </summary>
    public enum FluxGridBoundaryMode
    {
        /// <summary>
        /// 範囲外セルを0として読みます。境界で重みを再正規化しません。
        /// </summary>
        Zero = 0,

        /// <summary>
        /// セル座標をグリッドの端へClampして読みます。値自体はClampしません。
        /// </summary>
        Clamp = 1
    }
}

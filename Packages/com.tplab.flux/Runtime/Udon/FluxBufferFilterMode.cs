namespace TpLab.Flux.Udon
{
    /// <summary>
    /// BufferのTextureサンプリング方式
    /// </summary>
    public enum FluxBufferFilterMode
    {
        /// <summary>
        /// 最も近いtexelの値を補間せずに取得します。
        /// </summary>
        Point = 0,

        /// <summary>
        /// 周囲の4つのtexelの値をバイリニア補間して取得します。
        /// </summary>
        Bilinear = 1
    }
}

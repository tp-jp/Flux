namespace TpLab.Flux.Udon
{
    /// <summary>
    /// Bufferの初回確保時の寸法決定方式
    /// </summary>
    public enum FluxBufferSizeMode
    {
        /// <summary>
        /// 初期有効要素数の平方根を切り上げた値を一辺とする正方形Textureを確保します。
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// 指定した幅と高さでTextureを確保します。
        /// </summary>
        Explicit = 1
    }
}

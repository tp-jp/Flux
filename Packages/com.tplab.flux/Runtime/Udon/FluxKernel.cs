using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// Shaderを使用してFluxBufferにGPU処理を実行するKernelです。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxKernel : UdonSharpBehaviour
    {
        [SerializeField]
        Shader shader;

        [HideInInspector]
        [SerializeField]
        Material material;

#if !COMPILER_UDONSHARP
        /// <summary>
        /// Kernelが使用するShaderを取得します。
        /// </summary>
        [PublicAPI]
        public Shader Shader => shader;
#endif

        /// <summary>
        /// Shaderのfloatパラメーターを設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="value">設定する値</param>
        [PublicAPI]
        public void SetFloat(string name, float value)
        {
            material.SetFloat(name, value);
        }

        /// <summary>
        /// ShaderのVectorパラメーターを設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="value">設定する値</param>
        [PublicAPI]
        public void SetVector(string name, Vector4 value)
        {
            material.SetVector(name, value);
        }

        /// <summary>
        /// ShaderのVector配列パラメーターを設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="values">設定するVector配列</param>
        [PublicAPI]
        public void SetVectorArray(string name, Vector4[] values)
        {
            material.SetVectorArray(name, values);
        }

        /// <summary>
        /// FluxBufferをShaderのTextureパラメーターとして設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="buffer">設定するBuffer</param>
        [PublicAPI]
        public void SetBuffer(string name, FluxBuffer buffer)
        {
            material.SetTexture(name, buffer.Texture);
        }

        /// <summary>
        /// Sourceを使用せずにKernelを実行します。
        /// </summary>
        /// <param name="destination">処理結果を書き込むBuffer</param>
        [PublicAPI]
        public void Dispatch(FluxBuffer destination)
        {
            PrepareDestination(destination);

            VRCGraphics.Blit(null, destination.Texture, material);
        }

        /// <summary>
        /// Source Bufferを入力としてKernelを実行します。
        /// </summary>
        /// <param name="source">入力として使用するBuffer</param>
        /// <param name="destination">処理結果を書き込むBuffer</param>
        [PublicAPI]
        public void Dispatch(FluxBuffer source, FluxBuffer destination)
        {
            PrepareDestination(destination);
            PrepareSource(source);

            VRCGraphics.Blit(source.Texture, destination.Texture, material);
        }

        void PrepareSource(FluxBuffer source)
        {
            var texture = source.Texture;

            material.SetFloat("_FluxSourceCount", source.Count);
            material.SetFloat("_FluxSourceWidth", texture.width);
            material.SetFloat("_FluxSourceHeight", texture.height);
        }

        void PrepareDestination(FluxBuffer destination)
        {
            var texture = destination.Texture;

            material.SetFloat("_FluxDestinationCount", destination.Count);
            material.SetFloat("_FluxDestinationWidth", texture.width);
            material.SetFloat("_FluxDestinationHeight", texture.height);
        }
    }
}
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
        /// 準備済みのKernel Materialへ、別のMaterialの設定を一括コピーします。
        /// UnityのCopyPropertiesFromMaterialに従い、実行時設定を含むプロパティ値・Texture参照・
        /// Shaderキーワード・RenderQueue・GIフラグをコピーします。ShaderとMaterialインスタンスは変更しません。
        /// コピー元は変更せず、以後の設定変更は自動同期しません。Textureの内容は複製しません。
        /// Kernel固有の入力やパラメーターはコピー後に設定してください。
        /// Dispatchが設定する入出力のCount・幅・高さは、Dispatch時に実際のBufferの値で上書きします。
        /// Textureの入出力重複は検証しません。Bufferの確保・クリアやDispatchは行いません。
        /// </summary>
        /// <param name="source">コピー元のMaterial。異なるShaderも指定可能。nullまたはKernel Material未準備の場合はエラーを記録して変更せず終了</param>
        [PublicAPI]
        public void CopyPropertiesFromMaterial(Material source)
        {
            if (source == null)
            {
                Logger.LogError("Source Material is required.");
                return;
            }
            if (material == null)
            {
                Logger.LogError("Kernel Material is not prepared. Run FluxBuildPass before copying properties.");
                return;
            }
            material.CopyPropertiesFromMaterial(source);
        }

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
        /// TextureをShaderのTextureパラメーターとして設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="texture">設定するTexture</param>
        [PublicAPI]
        public void SetTexture(string name, Texture texture)
        {
            material.SetTexture(name, texture);
        }

        /// <summary>
        /// FluxBufferをShaderのTextureパラメーターとして設定します。
        /// </summary>
        /// <param name="name">Shaderプロパティ名</param>
        /// <param name="buffer">設定するBuffer</param>
        [PublicAPI]
        public void SetBuffer(string name, FluxBuffer buffer)
        {
            SetTexture(name, buffer.Texture);
        }

        /// <summary>
        /// Sourceを使用せずにKernelを実行します。
        /// </summary>
        /// <param name="destination">処理結果を書き込むBuffer</param>
        [PublicAPI]
        public void Dispatch(FluxBuffer destination)
        {
            if (!ValidateDestination(destination)) return;
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
            if (!ValidateDestination(destination) || !ValidateSource(source, destination)) return;
            PrepareDestination(destination);
            PrepareSource(source);

            VRCGraphics.Blit(source.Texture, destination.Texture, material);
        }

        /// <summary>
        /// Sourceを使用せず、指定Passだけを実行します。出力のクリアやCountの変更は行いません。
        /// 追加のTexture入力と出力の重複は検証しません。
        /// </summary>
        /// <param name="destination">処理結果を書き込むBuffer</param>
        /// <param name="pass">0以上、MaterialのPass数未満のPass番号</param>
        [PublicAPI]
        public void Dispatch(FluxBuffer destination, int pass)
        {
            if (!ValidatePass(pass) || !ValidateDestination(destination)) return;
            PrepareDestination(destination);
            VRCGraphics.Blit(null, destination.Texture, material, pass);
        }

        /// <summary>
        /// Source Bufferを入力として、指定Passだけを実行します。出力のクリアやCountの変更は行いません。
        /// 明示したSourceと出力の同一Textureを拒否します。追加のTexture入力は検証対象外です。
        /// </summary>
        /// <param name="source">入力として使用するBuffer</param>
        /// <param name="destination">処理結果を書き込むBuffer</param>
        /// <param name="pass">0以上、MaterialのPass数未満のPass番号</param>
        [PublicAPI]
        public void Dispatch(FluxBuffer source, FluxBuffer destination, int pass)
        {
            if (!ValidatePass(pass) || !ValidateDestination(destination) || !ValidateSource(source, destination)) return;
            PrepareDestination(destination);
            PrepareSource(source);
            VRCGraphics.Blit(source.Texture, destination.Texture, material, pass);
        }

        /// <summary>
        /// 指定PassへのDispatch準備を検証します。Textureは必要に応じて確保しますが、描画はしません。
        /// 追加Texture入力のサイズ・入出力重複は検証対象外です。
        /// Shaderの対応状況はEditorのみで検証します。isSupportedはUdonに公開されていません。
        /// </summary>
        /// <param name="destination">出力Buffer</param>
        /// <param name="pass">検証するPass番号</param>
        /// <returns>実行環境で検証可能なMaterial・Pass・出力の条件を満たす場合はtrue</returns>
        [PublicAPI]
        public bool ValidateDispatch(FluxBuffer destination, int pass)
        {
            if (!ValidatePass(pass)) return false;
#if !COMPILER_UDONSHARP
            if (!material.shader.isSupported)
            {
                Logger.LogError("Kernel Shader is not supported on this graphics device.");
                return false;
            }
#endif
            return ValidateDestination(destination);
        }

        bool ValidatePass(int pass)
        {
            if (material == null)
            {
                Logger.LogError("Kernel Material is not prepared. Run FluxBuildPass before dispatch.");
                return false;
            }
            if (pass < 0 || pass >= material.passCount)
            {
                Logger.LogError($"Pass must be between 0 and {material.passCount - 1}.");
                return false;
            }
            return true;
        }

        bool ValidateDestination(FluxBuffer destination)
        {
            if (material == null)
            {
                Logger.LogError("Kernel Material is not prepared. Run FluxBuildPass before dispatch.");
                return false;
            }
            if (destination == null)
            {
                Logger.LogError("Destination Buffer is required.");
                return false;
            }
            return destination.Texture != null;
        }

        bool ValidateSource(FluxBuffer source, FluxBuffer destination)
        {
            if (source == null)
            {
                Logger.LogError("Source Buffer is required.");
                return false;
            }
            var texture = source.Texture;
            if (texture == null) return false;
            if (texture == destination.Texture)
            {
                Logger.LogError("Source and destination must use different Textures.");
                return false;
            }
            return true;
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

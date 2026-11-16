using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Udon
{
    /// <summary>
    /// 2つのFluxBufferの現在／次回の役割と交換を管理します。
    /// Bufferを所有せず、Textureの確保・解放・クリア・コピー・Count変更は行いません。
    /// 時刻・更新頻度・Dispatch・GPU完了待ちは利用側が管理します。無効化や再有効化で順序は変えません。
    /// </summary>
    [PublicAPI]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxPingPong : UdonSharpBehaviour
    {
        [SerializeField]
        FluxBuffer bufferA;

        [SerializeField]
        FluxBuffer bufferB;

        bool _swapped;

        /// <summary>
        /// 現在側のBuffer参照。初期状態はBuffer A。未設定ならnull。Textureは確保しない
        /// </summary>
        [PublicAPI]
        public FluxBuffer Current => _swapped ? bufferB : bufferA;

        /// <summary>
        /// 次回側のBuffer参照。初期状態はBuffer B。未設定ならnull。Textureは確保しない
        /// </summary>
        [PublicAPI]
        public FluxBuffer Next => _swapped ? bufferA : bufferB;

        /// <summary>
        /// 新しい組を設定し、Current=current、Next=nextの順序にします。既存Bufferは解放しません。
        /// ResetOrderで戻る順序もこの組へ更新します。失敗時は組と順序の両方を維持します。
        /// </summary>
        /// <param name="current">新しい現在側のBuffer</param>
        /// <param name="next">新しい次回側のBuffer。currentとは異なるコンポーネントであること</param>
        /// <returns>異なる2つの非null参照を設定した場合はtrue</returns>
        [PublicAPI]
        public bool TrySetBuffers(FluxBuffer current, FluxBuffer next)
        {
            if (!ValidateReferences(current, next)) return false;
            bufferA = current;
            bufferB = next;
            _swapped = false;
            return true;
        }

        /// <summary>
        /// CurrentとNextの役割を1回交換します。処理の成功・GPU完了は判定しません。
        /// 利用側がNextへの必要な書き込みを発行した後で呼んでください。
        /// 検証はコンポーネント参照のみです。寸法・Count・実際のTexture重複は使用するKernel等の契約に従ってください。
        /// BufferをReleaseしても順序は維持されます。再確保後の内容の初期化は利用側の責務です。
        /// </summary>
        /// <returns>異なる2つの非null参照を交換した場合はtrue。失敗時は順序を維持</returns>
        [PublicAPI]
        public bool TrySwap()
        {
            if (!ValidateReferences(bufferA, bufferB)) return false;
            _swapped = !_swapped;
            return true;
        }

        /// <summary>
        /// Current=Buffer A、Next=Buffer Bの順序へ戻します。データは初期化も巻き戻しもしません。
        /// TrySetBuffersを呼んだ場合は、最後に成功した設定の順序へ戻します。
        /// </summary>
        [PublicAPI]
        public void ResetOrder()
        {
            _swapped = false;
        }

        bool ValidateReferences(FluxBuffer current, FluxBuffer next)
        {
            if (current == null || next == null || current == next)
            {
                Logger.LogError("Ping-Pong requires two different, non-null Buffer components.");
                return false;
            }
            return true;
        }
    }
}

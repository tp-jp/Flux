using UnityEditor;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Editor
{
    /// <summary>
    /// EnterPlayModeのDomain ReloadとTest Framework再開の間に発生するエラーも記録します。
    /// 状態はEditorセッション内だけで保持し、テストの終了時に購読を解除します。
    /// </summary>
    [InitializeOnLoad]
    static class FluxRuntimeTestLogCapture
    {
        const string ActiveKey = "TpLab.Flux.RuntimeTests.LogCapture.Active";
        const string ErrorsKey = "TpLab.Flux.RuntimeTests.LogCapture.Errors";

        static FluxRuntimeTestLogCapture()
        {
            if (SessionState.GetBool(ActiveKey, false)) Application.logMessageReceived += OnLog;
        }

        internal static string Errors => SessionState.GetString(ErrorsKey, "");

        internal static void Begin()
        {
            SessionState.SetString(ErrorsKey, "");
            SessionState.SetBool(ActiveKey, true);
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        internal static void End()
        {
            SessionState.SetBool(ActiveKey, false);
            SessionState.EraseString(ErrorsKey);
            Application.logMessageReceived -= OnLog;
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
            SessionState.SetString(ErrorsKey, Errors + $"{type}: {message}\n{stack}\n");
        }
    }
}

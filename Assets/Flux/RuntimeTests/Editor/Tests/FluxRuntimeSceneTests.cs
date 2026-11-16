using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TpLab.Flux.RuntimeTests.Udon;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TpLab.Flux.RuntimeTests.Editor
{
    /// <summary>
    /// Test RunnerのEditModeからPlayへ入り、配布用RuntimeTestシーンをUdon VMで実行します。
    /// テスト本体・SceneFlowによる準備・GPU Readbackは手動Playと共用します。
    /// </summary>
    [Serializable]
    public class FluxRuntimeSceneTests
    {
        // Test Framework serializes these fields across EnterPlayMode's domain reload.
        [SerializeField]
        SceneSetup[] sceneSetup;

        [SerializeField]
        SceneAsset previousPlayModeStartScene;

        [SerializeField]
        bool restoreScenes;

        [SerializeField]
        bool restoreEmptyActiveScene;

        [SerializeField]
        int expectedCount;

        [SerializeField]
        string reportPath;

        [SerializeField]
        string report;

        const string SceneGuid = "9e705c39ed0d887488f4f05257ada360";
        const double TimeoutSeconds = 180;

        /// <summary>
        /// UdonSharpをコンパイルし、Udon Heap上の実行記録と全テストの成功を検証します。
        /// Play移行中もError・Assert・Exceptionを捕捉し、予期しないログで失敗にします。
        /// </summary>
        [UnityTest]
        [Category("UdonRuntime")]
        public IEnumerator RuntimeScenePassesInUdonVm()
        {
            Assert.That(Application.isPlaying, Is.False, "Run this test from the EditMode tab.");
            Assert.That(ClientSimSettings.Instance.enableClientSim, Is.True, "Enable VRChat ClientSim before running Runtime Tests.");
            Assert.That(SystemInfo.graphicsDeviceType, Is.EqualTo(GraphicsDeviceType.Direct3D11),
                "This suite includes the D3D11-only Scatter Shader. A graphics device is required; do not use -nographics.");
            Assert.That(SystemInfo.supportsAsyncGPUReadback, Is.True, "Async GPU Readback is required.");
            Assert.That(EditorSettings.enterPlayModeOptionsEnabled &&
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) != 0, Is.False,
                "Enable Scene Reload so the normal SceneFlow/VRChat build callbacks run.");

            var scenePath = AssetDatabase.GUIDToAssetPath(SceneGuid);
            Assert.That(scenePath, Is.Not.Empty, "Import the Flux Runtime Tests sample first.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                Assert.That(scene.isDirty && scene.rootCount > 0, Is.False,
                    $"Save scene '{scene.name}' before running Runtime Tests; unsaved scenes are not replaced.");
            }
            sceneSetup = EditorSceneManager.GetSceneManagerSetup().Where(scene => !string.IsNullOrEmpty(scene.path)).ToArray();
            restoreEmptyActiveScene = string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
            previousPlayModeStartScene = EditorSceneManager.playModeStartScene;
            restoreScenes = true;
            EditorSceneManager.playModeStartScene = null;
            reportPath = Path.GetFullPath(Path.Combine("Temp", "FluxRuntimeTests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".txt"));
            report = "RuntimeTest preparation started.\nScene: " + scenePath;
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            FluxRuntimeTestLogCapture.Begin();
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = false });
            Assert.That(UdonSharpProgramAsset.AnyUdonSharpScriptHasError(), Is.False, "UdonSharp Player compilation failed.");
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = true });
            Assert.That(UdonSharpProgramAsset.AnyUdonSharpScriptHasError(), Is.False, "UdonSharp Editor compilation failed.");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var runners = Object.FindObjectsOfType<FluxRuntimeTestRunner>();
            Assert.That(runners.Length, Is.EqualTo(1), "The scene must contain one active Runtime Test Runner.");
            var tests = new SerializedObject(runners[0]).FindProperty("tests");
            expectedCount = tests.arraySize;
            Assert.That(expectedCount, Is.GreaterThan(0), "An empty suite must not pass.");
            for (var i = 0; i < expectedCount; i++)
            {
                Assert.That(tests.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, $"Missing test at index {i}.");
            }
            var contractTest = Object.FindObjectOfType<FluxBufferKernelContractTest>();
            Assert.That(new SerializedObject(contractTest).FindProperty("runInvalidInputChecks").boolValue, Is.False,
                "The shared scene must keep error-emitting invalid-input checks disabled; Editor tests cover them separately.");
            report += $"\nC# and UdonSharp Player/Editor compilation passed.\nExpected cases: {expectedCount}\nGPU: {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}\nWaiting for Udon VM.";

            var expectReload = !EditorSettings.enterPlayModeOptionsEnabled ||
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
            yield return new EnterPlayMode(expectReload);

            var deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            UdonBehaviour backing = null;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Assert.That(FluxRuntimeTestLogCapture.Errors, Is.Empty, "Unexpected errors during RuntimeTest, including Play entry.");
                if (backing == null) backing = FindRunner();
                if (backing != null && backing.IsInitialized)
                {
                    Assert.That(backing.HasError, Is.False, "The Runtime Test Runner halted in Udon VM.");
                    // Read the VM heap, not the C# proxy. The execution flag is set only in COMPILER_UDONSHARP.
                    var passed = (int)backing.GetProgramVariable("_passCount");
                    var failed = (int)backing.GetProgramVariable("_failCount");
                    var results = (string)backing.GetProgramVariable("_results");
                    var completed = (bool)backing.GetProgramVariable("_isComplete");
                    var executedInUdon = (bool)backing.GetProgramVariable("_executedInUdon");
                    report = $"Udon VM: {passed} passed / {failed} failed / {expectedCount} cases.\n" +
                        $"Initialized: {backing.IsInitialized}; Start: {backing.HasDoneStart}; Udon execution: {executedInUdon}; Complete: {completed}\n" +
                        $"C# and UdonSharp Player/Editor compilation passed.\nGPU: {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}\n{results}";
                    Assert.That(failed, Is.Zero, report);
                    if (completed)
                    {
                        Assert.That(backing.HasDoneStart && executedInUdon, Is.True, "Completion must originate from Udon VM execution.");
                        Assert.That(passed, Is.EqualTo(expectedCount), report);
                        LogAssert.NoUnexpectedReceived();
                        TestContext.WriteLine(report);
                        yield break;
                    }
                }
                yield return null;
            }
            Assert.Fail($"RuntimeTest did not complete within {TimeoutSeconds} seconds.\n{report}");
        }

        /// <summary>
        /// 成否にかかわらず記録をTempへ保存し、Playを終了してテスト開始前のシーン構成を復元します。
        /// </summary>
        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            Exception reportError = null;
            var errors = "";
            try
            {
                if (Application.isPlaying) yield return new ExitPlayMode();
                if (restoreScenes)
                {
                    EditorSceneManager.playModeStartScene = previousPlayModeStartScene;
                    if (sceneSetup.Length > 0)
                    {
                        // Test Runner can add an untitled active scene beside the user's saved scenes.
                        var savedScenes = (SceneSetup[])sceneSetup.Clone();
                        if (restoreEmptyActiveScene) savedScenes[0].isActive = true;
                        EditorSceneManager.RestoreSceneManagerSetup(savedScenes);
                        if (restoreEmptyActiveScene)
                        {
                            var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                            SceneManager.SetActiveScene(empty);
                        }
                    }
                    else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    restoreScenes = false;
                    Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
                    var restored = EditorSceneManager.GetSceneManagerSetup().Where(scene => !string.IsNullOrEmpty(scene.path))
                        .Select(scene => $"{scene.path}|{scene.isLoaded}|{scene.isActive}");
                    CollectionAssert.AreEqual(sceneSetup.Select(scene => $"{scene.path}|{scene.isLoaded}|{scene.isActive}"), restored);
                    Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(previousPlayModeStartScene));
                    TestContext.WriteLine("Editor restored: Play stopped; scene setup and Play Mode Start Scene restored.");
                }
            }
            finally
            {
                errors = FluxRuntimeTestLogCapture.Errors;
                FluxRuntimeTestLogCapture.End();
                try
                {
                    if (!string.IsNullOrEmpty(reportPath))
                    {
                        File.WriteAllText(reportPath, report + "\nCaptured errors:\n" + errors +
                            "\nTest Runner outcome before teardown assertions: " + TestContext.CurrentContext.Result.Outcome +
                            "\n" + TestContext.CurrentContext.Result.Message);
                        TestContext.WriteLine("RuntimeTest report: " + reportPath);
                    }
                }
                catch (Exception exception) { reportError = exception; }
            }
            Assert.That(reportError, Is.Null, "Could not save the RuntimeTest report.");
            Assert.That(errors, Is.Empty, "Unexpected errors during RuntimeTest or cleanup.");
        }

        static UdonBehaviour FindRunner()
        {
            return Object.FindObjectsOfType<UdonBehaviour>().SingleOrDefault(behaviour =>
                behaviour.programSource is UdonSharpProgramAsset program && program.sourceCsScript != null &&
                program.sourceCsScript.GetClass() == typeof(FluxRuntimeTestRunner));
        }
    }
}

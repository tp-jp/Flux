using TpLab.Flux.RuntimeTests.Udon;
using TpLab.Flux.Udon;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Editor
{
    /// <summary>
    /// 既存Runtime TestsにBuffer・Pass契約の検証を追加したシーンを作成します。
    /// </summary>
    public static class FluxBufferKernelTestScene
    {
        [MenuItem("Tools/Flux/Create Buffer and Kernel Test Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var path = EditorUtility.SaveFilePanelInProject("Create Flux Test Scene", "FluxBufferKernelTests", "unity", "Select a new test scene path.");
            if (string.IsNullOrEmpty(path)) return;
            Create(path);
        }

        /// <summary>
        /// 既存テストを保持した検証用シーンを別ファイルへ保存します。
        /// </summary>
        /// <param name="path">新しいシーンのAssets内の保存先</param>
        public static void Create(string path)
        {
            var original = AssetDatabase.GUIDToAssetPath("9e705c39ed0d887488f4f05257ada360");
            if (path == original) { throw new System.ArgumentException("Choose a new scene path.", nameof(path)); }
            var scene = EditorSceneManager.OpenScene(original);
            if (Object.FindObjectOfType<FluxBufferKernelContractTest>(true) != null)
            {
                EditorSceneManager.SaveScene(scene, path);
                return;
            }
            var source = Buffer("Contract Source", 128, 256, FluxBufferFilterMode.Point);
            var destination = Buffer("Contract Destination", 256, 128, FluxBufferFilterMode.Bilinear);
            var kernel = new GameObject("Contract Kernel").AddUdonSharpComponent<FluxKernel>();
            var shader = Shader.Find("Flux/Tests/FluxKernel/BufferContracts");
            var serialized = new SerializedObject(kernel);
            serialized.FindProperty("shader").objectReferenceValue = shader;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var readback = new GameObject("Contract Readback").AddUdonSharpComponent<FluxReadback>();
            var test = new GameObject("Buffer and Kernel Contract Test").AddUdonSharpComponent<FluxBufferKernelContractTest>();
            serialized = new SerializedObject(test);
            serialized.FindProperty("source").objectReferenceValue = source;
            serialized.FindProperty("destination").objectReferenceValue = destination;
            serialized.FindProperty("kernel").objectReferenceValue = kernel;
            serialized.FindProperty("readback").objectReferenceValue = readback;
            serialized.FindProperty("propertiesSource").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath("991d679e6890a3d44be793c779b6f845"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var runner = Object.FindObjectOfType<FluxRuntimeTestRunner>();
            test.gameObject.name = nameof(FluxBufferKernelContractTest);
            test.transform.SetParent(runner.transform.parent, false);
            source.transform.SetParent(test.transform, false);
            destination.transform.SetParent(test.transform, false);
            kernel.transform.SetParent(test.transform, false);
            readback.transform.SetParent(test.transform, false);
            serialized = new SerializedObject(runner);
            var tests = serialized.FindProperty("tests");
            var index = tests.arraySize;
            tests.arraySize++;
            tests.GetArrayElementAtIndex(index).objectReferenceValue = test;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (var behaviour in Object.FindObjectsOfType<UdonSharpBehaviour>(true))
            {
                UdonSharpEditorUtility.CopyProxyToUdon(behaviour);
            }
            EditorSceneManager.SaveScene(scene, path);
        }

        static FluxBuffer Buffer(string name, int width, int height, FluxBufferFilterMode filter)
        {
            var buffer = new GameObject(name).AddUdonSharpComponent<FluxBuffer>();
            var serialized = new SerializedObject(buffer);
            serialized.FindProperty("initialCount").intValue = width * height;
            serialized.FindProperty("sizeMode").intValue = (int)FluxBufferSizeMode.Explicit;
            serialized.FindProperty("width").intValue = width;
            serialized.FindProperty("height").intValue = height;
            serialized.FindProperty("filterMode").intValue = (int)filter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return buffer;
        }
    }
}

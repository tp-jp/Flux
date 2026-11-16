using TMPro;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.RuntimeTests.Udon
{
    /// <summary>
    /// 手動Playと自動テストで共用するRuntime Testsを順番に実行し、結果を表示します。
    /// 自動テストはC#のRunを直接呼ばず、UdonBehaviourの実行状態とHeap上の結果を監視します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxRuntimeTestRunner : UdonSharpBehaviour
    {
        [SerializeField]
        FluxRuntimeTestBase[] tests;

        [SerializeField]
        TMP_Text resultText;

        int _testIndex;
        int _passCount;
        int _failCount;
        string _results;
        bool _isComplete;
        bool _executedInUdon;

        /// <summary>
        /// 全テストの完了状態
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// UdonコンパイルされたRunの実行記録
        /// </summary>
        public bool ExecutedInUdon => _executedInUdon;

        /// <summary>
        /// 成功したテスト数
        /// </summary>
        public int PassCount => _passCount;

        /// <summary>
        /// 失敗したテスト数
        /// </summary>
        public int FailCount => _failCount;

        /// <summary>
        /// 各テストの結果と失敗理由
        /// </summary>
        public string Results => _results;

        /// <summary>
        /// 結果をリセットし、登録されたテストを先頭から実行します。実行中の再呼び出しは想定しません。
        /// </summary>
        public void Run()
        {
            _isComplete = false;
#if COMPILER_UDONSHARP
            _executedInUdon = true;
#else
            _executedInUdon = false;
#endif
            _testIndex = 0;
            _passCount = 0;
            _failCount = 0;
            _results = "";

            RunCurrentTest();
        }

        /// <summary>
        /// 現在のテストの成功を記録し、次のテストを実行します。
        /// </summary>
        /// <param name="testName">完了したテスト名</param>
        public void Pass(string testName)
        {
            _passCount++;
            _results += $"<color=#4EC9B0>PASS</color> {testName}\n";

            RunNextTest();
        }

        /// <summary>
        /// 現在のテストの失敗を記録し、次のテストを実行します。
        /// </summary>
        /// <param name="testName">完了したテスト名</param>
        /// <param name="message">失敗理由</param>
        public void Fail(string testName, string message)
        {
            _failCount++;
            _results += $"<color=#F44747>FAIL</color> {testName}: {message}\n";

            RunNextTest();
        }

        void Start()
        {
            Run();
        }

        void RunCurrentTest()
        {
            if (_testIndex >= tests.Length)
            {
                Complete();
                return;
            }

            var test = tests[_testIndex];
            test.SetTestRunner(this);
            test.Run();
        }

        void RunNextTest()
        {
            _testIndex++;
            RunCurrentTest();
        }

        void Complete()
        {
            resultText.text = $"Flux Runtime Tests\n\n{_results}\n<color=#4EC9B0>{_passCount} passed</color> / <color=#F44747>{_failCount} failed</color>";
            _isComplete = true;
        }
    }
}

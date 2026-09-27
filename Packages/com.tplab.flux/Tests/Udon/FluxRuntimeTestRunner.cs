using System;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FluxRuntimeTestRunner : UdonSharpBehaviour
    {
        [SerializeField]
        UdonSharpBehaviour[] tests;

        [SerializeField]
        TMP_Text resultText;

        int _testIndex;
        int _passCount;
        int _failCount;
        string _results;

        void Start()
        {
            Run();
        }

        public void Run()
        {
            _testIndex = 0;
            _passCount = 0;
            _failCount = 0;
            _results = "";

            RunCurrentTest();
        }

        public void Pass(string testName)
        {
            _passCount++;
            _results += $"<color=#4EC9B0>PASS</color> {testName}\n";

            RunNextTest();
        }

        public void Fail(string testName, string message)
        {
            _failCount++;
            _results += $"<color=#F44747>FAIL</color> {testName}: {message}\n";

            RunNextTest();
        }

        void RunCurrentTest()
        {
            if (_testIndex >= tests.Length)
            {
                Complete();
                return;
            }

            tests[_testIndex].SendCustomEvent("_RunTest");
        }

        void RunNextTest()
        {
            _testIndex++;
            RunCurrentTest();
        }

        void Complete()
        {
            resultText.text = $"Flux Runtime Tests\n\n{_results}\n<color=#4EC9B0>{_passCount} passed</color> / <color=#F44747>{_failCount} failed</color>";
        }
    }
}
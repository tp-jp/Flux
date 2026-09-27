using UdonSharp;

namespace TpLab.Flux.Tests.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FluxRuntimeTestBase : UdonSharpBehaviour
    {
        FluxRuntimeTestRunner _testRunner;

        protected abstract string TestName { get; }

        public void SetTestRunner(FluxRuntimeTestRunner testRunner)
        {
            _testRunner = testRunner;
        }

        public abstract void Run();
        
        protected void Pass()
        {
            _testRunner.Pass(TestName);
        }

        protected void Fail(string message)
        {
            _testRunner.Fail(TestName, message);
        }
    }
}
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    [InitializeOnLoad]
    internal static class M02TestHarness
    {
        private const string RequestPath = "Logs/m02-test-request.txt";

        static M02TestHarness() => EditorApplication.update += RunIfRequested;

        private static void RunIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

            EditorApplication.update -= RunIfRequested;
            string outputPath = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "SengokuChronicle.EditModeTests" }
            };
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks(outputPath));
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class Callbacks : ICallbacks
        {
            private readonly string _outputPath;
            public Callbacks(string outputPath) => _outputPath = outputPath;
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                string directory = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(_outputPath,
                    "RESULT STATE: " + result.ResultState + "\n" +
                    "PASS: " + result.PassCount + "\n" +
                    "FAIL: " + result.FailCount + "\n" +
                    "SKIP: " + result.SkipCount + "\n" +
                    "DURATION: " + result.Duration);
            }
        }
    }
}

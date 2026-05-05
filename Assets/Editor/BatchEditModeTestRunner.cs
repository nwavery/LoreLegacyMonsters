using System;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    public static class BatchEditModeTestRunner
    {
        sealed class Callback : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                UnityEngine.Debug.Log($"[BATCH-TEST] Run started: {testsToRun?.Name}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var passed = result != null ? result.PassCount : 0;
                var failed = result != null ? result.FailCount : 0;
                var skipped = result != null ? result.SkipCount : 0;
                var inconclusive = result != null ? result.InconclusiveCount : 0;
                var summaryPath = ResolveSummaryPath();
                var xmlPath = ResolveResultsXmlPath();
                var payload = "{\n" +
                              $"  \"passed\": {passed},\n" +
                              $"  \"failed\": {failed},\n" +
                              $"  \"skipped\": {skipped},\n" +
                              $"  \"inconclusive\": {inconclusive},\n" +
                              $"  \"status\": \"{result?.TestStatus}\"\n" +
                              "}\n";
                Directory.CreateDirectory(Path.GetDirectoryName(summaryPath) ?? ".");
                File.WriteAllText(summaryPath, payload);
                WriteNUnitResultsXml(result, xmlPath, passed, failed, skipped, inconclusive);
                UnityEngine.Debug.Log($"[BATCH-TEST] Summary written: {summaryPath}");
                UnityEngine.Debug.Log($"[BATCH-TEST] NUnit xml written: {xmlPath}");
                UnityEngine.Debug.Log($"[BATCH-TEST] passed={passed} failed={failed} skipped={skipped}");
                EditorApplication.Exit(failed > 0 ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callback());
            var filter = new Filter
            {
                testMode = TestMode.EditMode
            };
            var settings = new ExecutionSettings(filter)
            {
                runSynchronously = true
            };

            api.Execute(settings);
        }

        static string ResolveSummaryPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-batchTestSummary", StringComparison.OrdinalIgnoreCase))
                    continue;
                return Path.GetFullPath(args[i + 1]);
            }

            return Path.GetFullPath("Artifacts/TestResults/editmode-batch-summary.json");
        }

        static string ResolveResultsXmlPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-batchTestResults", StringComparison.OrdinalIgnoreCase))
                    continue;
                return Path.GetFullPath(args[i + 1]);
            }

            return Path.GetFullPath("Artifacts/TestResults/editmode-batch-results.xml");
        }

        static void WriteNUnitResultsXml(ITestResultAdaptor result, string destinationPath, int passed, int failed, int skipped, int inconclusive)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");

            if (TryCopyUnitySuppliedResults(destinationPath))
                return;

            if (TryWriteReflectionXml(result, destinationPath))
                return;

            // Final fallback keeps CI consumers stable even if Unity changes result internals.
            var total = Mathf.Max(0, passed + failed + skipped + inconclusive);
            var overall = failed > 0 ? "Failed" : "Passed";
            var now = DateTime.UtcNow.ToString("o");
            var doc = new XmlDocument();
            var run = doc.CreateElement("test-run");
            run.SetAttribute("id", "batch-editmode");
            run.SetAttribute("result", overall);
            run.SetAttribute("total", total.ToString());
            run.SetAttribute("passed", passed.ToString());
            run.SetAttribute("failed", failed.ToString());
            run.SetAttribute("inconclusive", inconclusive.ToString());
            run.SetAttribute("skipped", skipped.ToString());
            run.SetAttribute("testcasecount", total.ToString());
            run.SetAttribute("start-time", now);
            run.SetAttribute("end-time", now);
            run.SetAttribute("duration", "0");
            doc.AppendChild(run);
            doc.Save(destinationPath);
        }

        static bool TryCopyUnitySuppliedResults(string destinationPath)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-testResults", StringComparison.OrdinalIgnoreCase))
                    continue;

                var sourcePath = Path.GetFullPath(args[i + 1]);
                if (!File.Exists(sourcePath))
                    return false;

                File.Copy(sourcePath, destinationPath, true);
                return true;
            }

            var company = string.IsNullOrWhiteSpace(Application.companyName) ? "DefaultCompany" : Application.companyName;
            var product = string.IsNullOrWhiteSpace(Application.productName) ? "LoreLegacyMonsters" : Application.productName;
            var fallbackPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Low",
                company,
                product,
                "TestResults.xml");
            if (File.Exists(fallbackPath))
            {
                File.Copy(fallbackPath, destinationPath, true);
                return true;
            }

            return false;
        }

        static bool TryWriteReflectionXml(ITestResultAdaptor result, string destinationPath)
        {
            if (result == null)
                return false;

            var type = result.GetType();
            var noArgMethod = type.GetMethod("ToXml", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (TryInvokeToXml(noArgMethod, result, destinationPath))
                return true;

            var boolArgMethod = type.GetMethod("ToXml", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
            if (boolArgMethod == null)
                return false;

            return TryInvokeToXml(boolArgMethod, result, destinationPath, true);
        }

        static bool TryInvokeToXml(MethodInfo method, ITestResultAdaptor result, string destinationPath, params object[] args)
        {
            if (method == null)
                return false;

            var xmlObj = method.Invoke(result, args);
            if (xmlObj == null)
                return false;

            switch (xmlObj)
            {
                case XmlNode node:
                    File.WriteAllText(destinationPath, node.OuterXml);
                    return true;
                case string xmlText when !string.IsNullOrWhiteSpace(xmlText):
                    File.WriteAllText(destinationPath, xmlText);
                    return true;
                default:
                    return false;
            }
        }
    }
}

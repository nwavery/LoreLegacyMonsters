using NUnit.Framework;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.Tests
{
    public class LlmBundledBootPolicyTests
    {
        [Test]
        public void BootProbePolicy_Bundled_Deadline_IsFiveMinutes()
        {
            Assert.AreEqual(300f, LlmBootProbePolicy.DeadlineSeconds(true), 0.001f);
            Assert.AreEqual(45f, LlmBootProbePolicy.DeadlineSeconds(false), 0.001f);
        }

        [Test]
        public void BootProbePolicy_Bundled_HttpTimeouts_Escalate()
        {
            Assert.AreEqual(45, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(true, 0));
            Assert.AreEqual(75, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(true, 1));
            Assert.AreEqual(120, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(true, 2));
            Assert.AreEqual(120, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(true, 99));
        }

        [Test]
        public void BootProbePolicy_NonBundled_UsesCompactTimeouts()
        {
            Assert.AreEqual(12, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(false, 0));
            Assert.AreEqual(12, LlmBootProbePolicy.ProbeHttpTimeoutSeconds(false, 50));
        }

        [Test]
        public void BundledProvisioner_TagsBody_DetectsModelName()
        {
            Assert.IsFalse(BundledOllamaModelProvisioner.ListResponseContainsBundled(""));
            Assert.IsFalse(BundledOllamaModelProvisioner.ListResponseContainsBundled("{}"));
            var j =
                "{\"models\":[{\"name\":\"lore-bundled:latest\",\"modified_at\":\"2026-05-06T01:02:03Z\"}]}";
            Assert.IsTrue(BundledOllamaModelProvisioner.ListResponseContainsBundled(j));
        }

        [Test]
        public void LooksLikeLegacyDesktopDefaultModel_TrimsVariants()
        {
            Assert.IsTrue(BundledOllamaModelProvisioner.LooksLikeLegacyDesktopDefaultModel("llama3.2:latest "));
            Assert.IsTrue(BundledOllamaModelProvisioner.LooksLikeLegacyDesktopDefaultModel("Llama3.2:FOO"));
            Assert.IsFalse(BundledOllamaModelProvisioner.LooksLikeLegacyDesktopDefaultModel("mistral:latest"));
            Assert.IsFalse(BundledOllamaModelProvisioner.LooksLikeLegacyDesktopDefaultModel("lore-bundled"));
        }
    }
}

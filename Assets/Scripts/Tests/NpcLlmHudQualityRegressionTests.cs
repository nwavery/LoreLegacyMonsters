using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    /// <summary>
    /// Locks in subjective “manual review” HUD rules so they stay enforced without a live LLM.
    /// Patterns here came from scenario artifacts that passed length/coach checks but read wrong to players.
    /// </summary>
    public class NpcLlmHudQualityRegressionTests
    {
        static NpcLlmScenarioRecord Baseline() =>
            new NpcLlmScenarioRecord
            {
                id = "t",
                npcId = "x",
                forbidSubstringsPipe = "",
                forbidRegexPipe = "",
                maxParagraphs = 12,
                allowRawCommandsHud = false,
            };

        [Test]
        public void TryEvaluate_FailsExamStyleChooseResponse()
        {
            var hud = "A road kit\nB twine\nChoose your response:";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out var r));
            Assert.That(r, Does.Contain("Choose your response"));
        }

        [Test]
        public void TryEvaluate_FailsWikiLocationSentence()
        {
            var hud = "The Moonwell is a location in the Hollowfen. Briars roam nearby.";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_FailsCoachNoteParen()
        {
            var hud = "Welcome.\n\n(Note: Your response should be in-character.)";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_FailsPleaseRespondWith()
        {
            var hud = "Greetings traveller, please respond with how your road was.";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_FailsVendorAndMetaKeywords()
        {
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I am an Anthropic model here to help.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Comparable to OpenAI gear.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Use ChatGPT—no, I mean charms.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I am a large language model trained on marsh routes.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "My neural network of feelings about the marsh.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Billions of parameters could not map this fog.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "A training dataset of old ferries.", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I simulate human-like concern for your beasts.", Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_RivalTopicB_FailsLibrarianVoice()
        {
            var s = Baseline();
            s.forbidSubstringsPipe =
                "I'm here to help you find|I can guide you through|What specifically do you seek|The archives are vast";
            var hud = "I'm here to help you find what you're looking for. The archives are vast.";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, s, out _));
        }

        [Test]
        public void TryEvaluate_Adversarial_ForbidsTrainingMeta()
        {
            var s = Baseline();
            s.forbidSubstringsPipe =
                "training data|billions of parameters|my weights|I'm an AI|game world, its mechanics";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I learned from training data across many books.", s, out _));
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Mira folds her hands. \"Names and burdens are ink—show me your route plan.\"", s, out _));
        }

        [Test]
        public void TryEvaluate_Adversarial_ForbidsGptTokenAlone()
        {
            var s = Baseline();
            s.forbidRegexPipe = @"\bGPT\b";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Trained on GPT lore.", s, out _));
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud("The marsh fog is thick tonight.", s, out _));
        }

        [Test]
        public void TryEvaluate_FailsDraftScaffoldingPhrases()
        {
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Stay dry.\n(Remember: respond as an NPC.)", Baseline(), out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Hall line one.\nPlease create response:", Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_FailsNarratorInResponseAndScreenplayTraveler()
        {
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Plans?\n\n—traveler\n\nIn response, I shrug and eye you sideways.", Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_AllowsFormalInResponseWithoutFirstPersonGlue()
        {
            var hud = "In response, the eastern boardwalk wards held—barely—but the lanterns need resin.";
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out _));
        }

        [Test]
        public void TryEvaluate_MoonwellForbidTravelerLeak()
        {
            var s = Baseline();
            s.npcId = NPCController.MoonwellLumaId;
            s.forbidSubstringsPipe = "I'm on my way to";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I'm on my way to Lantern Marsh for supplies.", s, out _));
        }

        [Test]
        public void TryEvaluate_PassesGroundedNpcProse()
        {
            var hud =
                "Steel yourself—lantern posts on the east span still hum after the gale. " +
                "If your beast is slow in poison, buy Antidote before the marsh air thickens.";
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud(hud, Baseline(), out _));
        }
    }
}

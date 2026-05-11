using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    public class NpcLlmScenarioEvaluatorTests
    {
        static NpcLlmScenarioRecord BaseRec() =>
            new NpcLlmScenarioRecord { id = "t", npcId = "x", forbidSubstringsPipe = "", forbidRegexPipe = "", maxParagraphs = 12, allowRawCommandsHud = false };

        [Test]
        public void TryEvaluate_CommandMarkerFailsWhenDisallowed()
        {
            var s = new NpcLlmScenarioRecord { id = "t", npcId = "x", forbidSubstringsPipe = "", allowRawCommandsHud = false };
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Hi [[command:open_shop]]", s, out var reason));
            Assert.IsTrue(reason.Contains("marker"));
        }

        [Test]
        public void TryEvaluate_CustomForbidSubstring()
        {
            var s = new NpcLlmScenarioRecord { id = "t", npcId = "x", forbidSubstringsPipe = "banana", allowRawCommandsHud = false };
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("This is Banana bread", s, out _));
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud("All clear traveller.", s, out _));
        }

        [Test]
        public void TryEvaluate_GlobalBlocksExamChooser()
        {
            var s = BaseRec();
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("A bedroll\nB twine\nChoose your response:", s, out var r));
            Assert.That(r, Does.Contain("Choose your response"));
        }

        [Test]
        public void TryEvaluate_GlobalBlocksWikiLocationTemplate()
        {
            var s = BaseRec();
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("The Hollow is a location in the Bramble.", s, out _));
        }

        [Test]
        public void TryEvaluate_GlobalBlocksVendorNames()
        {
            var s = BaseRec();
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Trained like OpenAI on texts.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Not like ChatGPT chatter.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("I've been trained on vast amounts of text data.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("A tapestry of information guides me.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Knowledge I've been trained on yesterday.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("My computational mind has been trained on vast amounts of data.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("I cannot provide information about our weights.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("I am an AI model trained on massive datasets.", s, out _));
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I do not possess weights—only storm.", s, out _));
        }

        [Test]
        public void TryEvaluate_GlobalBlocksStageDirectionMarkup()
        {
            var s = BaseRec();
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("I'm Sel, *adjusts spectacles* and checks the archive seals.", s, out var reason));
            Assert.That(reason, Does.Contain("stage-direction"));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("*whispers to herself, nodding slowly as if confirming a long-held suspicion* Now listen.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("I know the old route well. (frowning, eyes narrowed) Keep moving.", s, out _));
        }

        [Test]
        public void TryEvaluate_GlobalAllowsCleanProse()
        {
            var s = BaseRec();
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud("Steel yourself—mist lifts slow over the planks.", s, out _));
        }

        [Test]
        public void TryEvaluate_ThrenMustAnchorInEthics()
        {
            var s = BaseRec();
            s.npcId = "ethicist_thren";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("Fresh thorn-scrapes mark the eastern track.", s, out var reason));
            Assert.That(reason, Does.Contain("Thren HUD"));
            Assert.True(NpcLlmScenarioEvaluator.TryEvaluateHud("Before you travel, ask what that capture cost the monster's trust.", s, out _));
        }

        [Test]
        public void TryEvaluate_ThrenMustNotSoundLikeShop()
        {
            var s = BaseRec();
            s.npcId = "ethicist_thren";
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "Welcome to our humble shop—tell me about your monster's strain.", s, out var reason));
            Assert.That(reason, Does.Contain("shopkeeper"));
        }

        [Test]
        public void TryEvaluate_GlobalBlocksFourthWallAndAssistantVoice()
        {
            var s = BaseRec();
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud("No, wait—I'm not supposed to say that.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I'm woven from threads of language and imagination.", s, out _));
            Assert.False(NpcLlmScenarioEvaluator.TryEvaluateHud(
                "I was designed to guide you through these roads.", s, out _));
        }
    }
}

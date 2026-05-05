using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Dialogue;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class DialogueSystemTests
    {
        [Test]
        public void DialogueSystem_StartDialogue_DoesNotThrow()
        {
            var go = new GameObject("dlg");
            var sys = go.AddComponent<DialogueSystem>();
            sys.StartDialogue(null);
            Assert.Pass();
            Object.DestroyImmediate(go);
        }
    }
}

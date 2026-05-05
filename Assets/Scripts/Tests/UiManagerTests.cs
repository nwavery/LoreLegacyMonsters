using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class UiManagerTests
    {
        [TearDown]
        public void Cleanup()
        {
            if (UIManager.Instance != null)
                Object.DestroyImmediate(UIManager.Instance.gameObject);
        }

        [Test]
        public void ModalState_BlocksWorldInput_WhenBlockingModalOpen()
        {
            var go = new GameObject("ui-manager");
            var ui = go.AddComponent<UIManager>();

            ui.SetModalOpen(UI.UiModal.Party, true);

            Assert.IsTrue(ui.IsModalOpen(UI.UiModal.Party));
            Assert.IsTrue(ui.IsBlockingWorldInput);

            ui.SetModalOpen(UI.UiModal.Party, false);

            Assert.IsFalse(ui.IsBlockingWorldInput);
        }

        [Test]
        public void ShowToast_StoresCurrentToast()
        {
            var go = new GameObject("ui-manager");
            var ui = go.AddComponent<UIManager>();

            ui.ShowToast("Saved.");

            Assert.AreEqual("Saved.", ui.CurrentToast);
        }

        [Test]
        public void BeginLoading_OpensBlockingLoadingModal()
        {
            var go = new GameObject("ui-manager");
            var ui = go.AddComponent<UIManager>();

            ui.BeginLoading("Loading save...");

            Assert.IsTrue(ui.IsModalOpen(UI.UiModal.Loading));
            Assert.IsTrue(ui.IsBlockingWorldInput);
            Assert.AreEqual("Loading save...", ui.LoadingMessage);
        }
    }
}

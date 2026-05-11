using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.UI
{
    /// <summary>Singleton tooltip anchored near the cursor.</summary>
public sealed class TooltipOverlay : MonoBehaviour
{
    RectTransform bubble;
    Text label;
    Canvas parentCanvas;

    void EnsureBubble()
        {
            if (bubble != null) return;
            parentCanvas = UIManager.Instance != null ? UIManager.Instance.Root : null;
            if (parentCanvas == null) return;
            bubble = RuntimeUiFactory.CreatePanel(parentCanvas.transform, "TooltipBubble",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.96f), Vector2.zero, Vector2.zero, new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(320f, 180f));
            label = RuntimeUiFactory.CreateText(bubble, "TooltipBody", "", 13, TextAnchor.UpperLeft, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(-16f, -16f), VerticalWrapMode.Overflow);
            bubble.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (bubble != null)
                bubble.gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            EnsureBubble();
            if (bubble == null) return;
            label.text = message.Trim();
            bubble.gameObject.SetActive(true);
            Reposition();
        }

        void LateUpdate()
        {
            if (bubble == null || !bubble.gameObject.activeSelf) return;
            Reposition();
        }

        void Reposition()
        {
            parentCanvas ??= UIManager.Instance != null ? UIManager.Instance.Root : null;
            if (parentCanvas == null || bubble == null) return;

            Vector2 mouse = Mouse.current != null ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            var canvasRt = parentCanvas.transform as RectTransform;
            if (canvasRt != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, mouse, parentCanvas.renderMode ==
                    RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                    out var local))
                bubble.localPosition = local + new Vector2(12f, -12f);
        }
    }
}

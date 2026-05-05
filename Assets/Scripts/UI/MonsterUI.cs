using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.Monster;
using System.Collections.Generic;

namespace LoreLegacyMonsters.UI
{
    public class MonsterUI : MonoBehaviour
    {
        [SerializeField] OverworldChapterController controller;

        RectTransform compactRoot;
        RectTransform modalRoot;
        RectTransform partyListRoot;
        RectTransform reserveListRoot;
        Text compactText;
        Text detailText;
        Button setLeadButton;
        Button moveUpButton;
        Button moveDownButton;
        Button toReserveButton;
        Button evolveButton;
        Button bringFromReserveButton;
        Button useCureButton;

        int selectedPartyIndex;
        int selectedReserveIndex;
        bool wasModalOpen;
        string listSignature;
        readonly List<Button> partyButtons = new List<Button>();
        readonly List<Button> reserveButtons = new List<Button>();

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            if (controller == null || controller.MonsterSystem == null || UIManager.Instance == null)
            {
                SetVisible(compactRoot, false);
                SetVisible(modalRoot, false);
                return;
            }

            EnsureUi();
            if (compactRoot == null || compactText == null)
                return;

            compactText.text = controller.PartySummary;
            var modalOpen = UIManager.Instance.IsModalOpen(UiModal.Party);
            var dialogOpen = controller.DialogDriver != null &&
                             (controller.DialogDriver.IsConversationOpen || controller.DialogDriver.IsBusy);
            var showCompact = !UIManager.Instance.IsBlockingWorldInput && !dialogOpen;
            SetVisible(compactRoot, showCompact);
            if (showCompact)
                compactRoot.SetAsLastSibling();

            SetVisible(modalRoot, modalOpen);
            if (modalOpen)
                modalRoot.SetAsLastSibling();
            if (!modalOpen)
            {
                wasModalOpen = false;
                return;
            }

            var signature = BuildListSignature();
            if (!wasModalOpen || signature != listSignature)
            {
                ClampSelection();
                RebuildLists();
                listSignature = signature;
            }
            wasModalOpen = true;
            RefreshSelectionVisuals();
            RefreshDetails();
        }

        void OnDestroy()
        {
            if (compactRoot != null) Destroy(compactRoot.gameObject);
            if (modalRoot != null) Destroy(modalRoot.gameObject);
        }

        void EnsureUi()
        {
            if (compactRoot == null && UIManager.Instance != null && UIManager.Instance.Root != null)
            {
                compactRoot = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "MonsterCompactRoot",
                    GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.9f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(270f, 130f), GameVisualTheme.AccentGreen);
                RuntimeUiFactory.CreateText(compactRoot, "CompactTitle", "Party", 18, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(160f, 22f));
                compactText = RuntimeUiFactory.CreateText(compactRoot, "CompactText", string.Empty, 13, TextAnchor.UpperLeft, GameVisualTheme.Text,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -38f), new Vector2(246f, 58f));
                RuntimeUiFactory.CreateText(compactRoot, "CompactHint", "[Tab] Party  [M] Map  [I] Inventory", 12, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(246f, 18f));
            }

            if (modalRoot == null && UIManager.Instance != null && UIManager.Instance.Root != null)
            {
                modalRoot = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "MonsterModalRoot",
                    GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 580f), GameVisualTheme.Accent);
                _ = RuntimeUiFactory.CreateModalWindowChrome(modalRoot, "Party Manager", GameVisualTheme.Accent, "Close [Tab]",
                    () => UIManager.Instance?.SetModalOpen(UiModal.Party, false));
                partyListRoot = RuntimeUiFactory.CreateCard(modalRoot, "PartyListRoot", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f),
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -56f), new Vector2(260f, 468f));
                reserveListRoot = RuntimeUiFactory.CreateCard(modalRoot, "ReserveListRoot", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f),
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(260f, 180f));
                var detailCard = RuntimeUiFactory.CreateCard(modalRoot, "MonsterDetailCard", GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.82f),
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(160f, -56f), new Vector2(-340f, 330f),
                    GameVisualTheme.AccentBlue);
                RuntimeUiFactory.CreateStatusBadge(detailCard, "DetailBadge", "Selected Monster", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(176f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.58f));
                detailText = RuntimeUiFactory.CreateText(detailCard, "DetailText", string.Empty, 17, TextAnchor.UpperLeft, GameVisualTheme.Text,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-36f, -74f),
                    VerticalWrapMode.Truncate);

                setLeadButton = RuntimeUiFactory.CreateButton(modalRoot, "SetLeadButton", "Set Lead",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(310f, 120f), new Vector2(130f, 40f));
                moveUpButton = RuntimeUiFactory.CreateButton(modalRoot, "MoveUpButton", "Move Up",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(450f, 120f), new Vector2(130f, 40f));
                moveDownButton = RuntimeUiFactory.CreateButton(modalRoot, "MoveDownButton", "Move Down",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(590f, 120f), new Vector2(130f, 40f));
                toReserveButton = RuntimeUiFactory.CreateButton(modalRoot, "ToReserveButton", "To Reserve",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(310f, 68f), new Vector2(130f, 40f));
                evolveButton = RuntimeUiFactory.CreateButton(modalRoot, "EvolveButton", "Evolve",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(450f, 68f), new Vector2(130f, 40f));
                bringFromReserveButton = RuntimeUiFactory.CreateButton(modalRoot, "BringReserveButton", "Add Reserve",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(590f, 68f), new Vector2(150f, 40f));
                useCureButton = RuntimeUiFactory.CreateButton(modalRoot, "UseCureButton", "Use matching cure",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(310f, 168f), new Vector2(220f, 36f));
                useCureButton.onClick.AddListener(() => controller?.TryUseMatchingCureForPartyMember(selectedPartyIndex));

                setLeadButton.onClick.AddListener(() => controller?.MonsterSystem?.SetActiveIndex(selectedPartyIndex));
                moveUpButton.onClick.AddListener(() => controller?.MonsterSystem?.ReorderParty(selectedPartyIndex, Mathf.Max(0, selectedPartyIndex - 1)));
                moveDownButton.onClick.AddListener(() => controller?.MonsterSystem?.ReorderParty(selectedPartyIndex, Mathf.Min(controller.MonsterSystem.Party.Count - 1, selectedPartyIndex + 1)));
                toReserveButton.onClick.AddListener(() => controller?.MonsterSystem?.MovePartyToReserve(selectedPartyIndex));
                evolveButton.onClick.AddListener(() => controller?.MonsterSystem?.TryEvolve(selectedPartyIndex, controller.Registry));
                bringFromReserveButton.onClick.AddListener(() => controller?.MonsterSystem?.MoveReserveToParty(selectedReserveIndex));
            }
        }

        void RebuildLists()
        {
            var system = controller.MonsterSystem;
            var registry = controller.Registry;
            RuntimeUiFactory.DestroyChildren(partyListRoot);
            RuntimeUiFactory.DestroyChildren(reserveListRoot);
            partyButtons.Clear();
            reserveButtons.Clear();

            RuntimeUiFactory.CreateText(partyListRoot, "PartyHeader", "Active Party", 20, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(220f, 24f));
            for (var i = 0; i < system.Party.Count; i++)
            {
                var monster = system.Party[i];
                if (monster == null) continue;
                var data = registry != null ? registry.GetMonster(monster.monsterDataId) : null;
                var evo = data != null && system.CanEvolve(monster, data) ? " [Evo]" : string.Empty;
                var label = $"{(i == system.ActiveIndex ? "*" : "-")} {monster.GetDisplayName(data)} Lv{monster.level}{evo}";
                var button = RuntimeUiFactory.CreateButton(partyListRoot, $"PartyButton_{i}", label,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -48f - (i * 48f)), new Vector2(236f, 40f));
                var labelText = button.GetComponentInChildren<Text>();
                if (labelText != null) labelText.alignment = TextAnchor.MiddleLeft;
                var capturedIndex = i;
                button.onClick.AddListener(() =>
                {
                    selectedPartyIndex = capturedIndex;
                    RefreshSelectionVisuals();
                    RefreshDetails();
                });
                partyButtons.Add(button);
            }

            RuntimeUiFactory.CreateText(reserveListRoot, "ReserveHeader", "Reserve", 20, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(220f, 24f));
            for (var i = 0; i < system.Reserve.Count; i++)
            {
                var monster = system.Reserve[i];
                if (monster == null) continue;
                var data = registry != null ? registry.GetMonster(monster.monsterDataId) : null;
                var button = RuntimeUiFactory.CreateButton(reserveListRoot, $"ReserveButton_{i}",
                    $"{monster.GetDisplayName(data)} Lv{monster.level}",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -48f - (i * 44f)), new Vector2(236f, 36f));
                var capturedIndex = i;
                button.onClick.AddListener(() =>
                {
                    selectedReserveIndex = capturedIndex;
                    RefreshSelectionVisuals();
                });
                reserveButtons.Add(button);
            }
        }

        void RefreshDetails()
        {
            var system = controller.MonsterSystem;
            var registry = controller.Registry;
            if (system.Party.Count == 0)
            {
                detailText.text = "No monsters in party.";
                SetActionButtonsInteractable(false);
                return;
            }

            ClampSelection();
            var monster = system.Party[selectedPartyIndex];
            var data = registry != null ? registry.GetMonster(monster.monsterDataId) : null;
            if (monster == null || data == null)
            {
                detailText.text = "Monster data unavailable.";
                SetActionButtonsInteractable(false);
                return;
            }

            var moves = string.Empty;
            foreach (var moveId in monster.GetAvailableMoveIds(data))
            {
                var move = MoveLibrary.Get(moveId);
                moves += move != null ? $"- {move.DisplayName} [{move.Element}]\n" : $"- {moveId}\n";
            }

            var evolutionText = "No evolution available";
            if (data.Evolution != null && data.Evolution.HasEvolution)
            {
                var target = registry != null ? registry.GetMonster(data.Evolution.targetMonsterId) : null;
                var targetName = target != null ? target.DisplayName : data.Evolution.targetMonsterId;
                evolutionText = data.Evolution.method switch
                {
                    EvolutionMethod.Level => $"Evolves into {targetName} at Lv{data.Evolution.requiredLevel}",
                    EvolutionMethod.Item => $"Evolves into {targetName} with {data.Evolution.requiredItemId}",
                    _ => $"Evolves into {targetName}"
                };
            }

            detailText.text =
                $"{monster.GetDisplayName(data)}\n\n" +
                $"Role: {data.Role}\n" +
                $"Type: {data.PrimaryElement}" + (data.SecondaryElement != MonsterElement.None ? $" / {data.SecondaryElement}" : string.Empty) + "\n" +
                $"Level: {monster.level}\n" +
                $"HP: {monster.currentHp}/{monster.maxHp}\n" +
                $"ATK {monster.GetAttackStat(data)}  DEF {monster.GetDefenseStat(data)}  SPD {monster.GetSpeedStat(data)}\n" +
                $"Status: {monster.persistentStatus}\n" +
                $"Reserve Count: {system.Reserve.Count}\n" +
                $"{evolutionText}\n\n" +
                $"Moves:\n{moves}" +
                $"\nNext level in {monster.RequiredExperienceForNextLevel - monster.experience} XP";

            evolveButton.interactable = system.CanEvolve(monster, data);
            bringFromReserveButton.interactable = system.Reserve.Count > 0 && system.Party.Count < 4;
            toReserveButton.interactable = system.Party.Count > 1;
            setLeadButton.interactable = selectedPartyIndex != system.ActiveIndex;
            moveUpButton.interactable = selectedPartyIndex > 0;
            moveDownButton.interactable = selectedPartyIndex < system.Party.Count - 1;

            if (useCureButton != null)
            {
                var inv = controller != null ? controller.Inventory : null;
                var recId = StatusCureCatalog.RecommendedCureItemId(monster.persistentStatus);
                var can = inv != null && inv.Count(recId) > 0 && monster.persistentStatus != MonsterStatusEffect.None &&
                          !string.IsNullOrEmpty(recId);
                useCureButton.gameObject.SetActive(can);
                useCureButton.interactable = can;
            }
        }

        string BuildListSignature()
        {
            if (controller == null || controller.MonsterSystem == null) return string.Empty;
            var system = controller.MonsterSystem;
            var s = $"active:{system.ActiveIndex}|party:{system.Party.Count}|";
            for (var i = 0; i < system.Party.Count; i++)
            {
                var m = system.Party[i];
                if (m == null) continue;
                s += $"{i}:{m.instanceId}:{m.monsterDataId}:{m.level}:{m.currentHp}:{m.persistentStatus};";
            }

            s += $"reserve:{system.Reserve.Count}|";
            for (var i = 0; i < system.Reserve.Count; i++)
            {
                var m = system.Reserve[i];
                if (m == null) continue;
                s += $"{i}:{m.instanceId}:{m.monsterDataId}:{m.level};";
            }

            return s;
        }

        void ClampSelection()
        {
            var system = controller != null ? controller.MonsterSystem : null;
            if (system == null) return;
            selectedPartyIndex = system.Party.Count > 0 ? Mathf.Clamp(selectedPartyIndex, 0, system.Party.Count - 1) : 0;
            selectedReserveIndex = system.Reserve.Count > 0 ? Mathf.Clamp(selectedReserveIndex, 0, system.Reserve.Count - 1) : 0;
        }

        void RefreshSelectionVisuals()
        {
            var system = controller != null ? controller.MonsterSystem : null;
            if (system == null) return;
            for (var i = 0; i < partyButtons.Count; i++)
            {
                var button = partyButtons[i];
                if (button == null) continue;
                var image = button.GetComponent<Image>();
                if (image != null)
                    image.color = i == selectedPartyIndex
                        ? GameVisualTheme.Accent
                        : i == system.ActiveIndex
                            ? GameVisualTheme.WithAlpha(GameVisualTheme.AccentGreen, 0.95f)
                            : GameVisualTheme.PanelInner;
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.color = i == selectedPartyIndex ? GameVisualTheme.TextDark : GameVisualTheme.Text;
            }

            for (var i = 0; i < reserveButtons.Count; i++)
            {
                var button = reserveButtons[i];
                if (button == null) continue;
                var image = button.GetComponent<Image>();
                if (image != null)
                    image.color = i == selectedReserveIndex ? GameVisualTheme.Accent : GameVisualTheme.PanelInner;
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.color = i == selectedReserveIndex ? GameVisualTheme.TextDark : GameVisualTheme.Text;
            }
        }

        void SetActionButtonsInteractable(bool value)
        {
            if (setLeadButton != null) setLeadButton.interactable = value;
            if (moveUpButton != null) moveUpButton.interactable = value;
            if (moveDownButton != null) moveDownButton.interactable = value;
            if (toReserveButton != null) toReserveButton.interactable = value;
            if (evolveButton != null) evolveButton.interactable = value;
            if (bringFromReserveButton != null) bringFromReserveButton.interactable = value;
            if (useCureButton != null) useCureButton.gameObject.SetActive(false);
        }

        static void SetVisible(RectTransform target, bool visible)
        {
            if (target != null && target.gameObject.activeSelf != visible)
                target.gameObject.SetActive(visible);
        }
    }
}

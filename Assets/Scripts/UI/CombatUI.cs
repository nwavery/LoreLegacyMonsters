using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.UI
{
    public class CombatUI : MonoBehaviour
    {
        [SerializeField] CombatManager combat;

        const float HpFillSpeed = 3f;
        const float BannerFlashDuration = 0.35f;
        const float BannerDimAlpha = 0.6f;
        const float BattleIntroDuration = 0.9f;

        RectTransform root;
        Image stageBackdrop;
        Image skyBand;
        Image groundBand;
        Image horizonBand;
        RectTransform enemyPlatform;
        RectTransform playerPlatform;
        RectTransform enemyCluster;
        RectTransform playerCluster;
        Image enemySilhouette;
        Image playerSilhouette;
        Text enemyNameText;
        Text playerNameText;
        Image enemyHpBg;
        Image enemyHpFill;
        Image playerHpBg;
        Image playerHpFill;
        Text enemyHpText;
        Text playerHpText;
        RectTransform enemyChipsRoot;
        RectTransform playerChipsRoot;
        Text enemyStatText;
        Text playerStatText;
        Text[] enemyChipTexts;
        Text[] playerChipTexts;
        Image[] enemyChipBgs;
        Image[] playerChipBgs;
        RectTransform playerGearHud;
        readonly Image[] playerGearHudIcons = new Image[4];
        Text bannerText;
        Image bannerPanel;
        Text logLineA;
        Text logLineB;
        Text feedbackText;
        RectTransform actionPanel;
        Button[] menuButtons;
        Text[] menuLabels;
        RectTransform endgameOverlay;
        Image endgameDim;
        Text endgameTitle;
        Text endgameSummary;
        RectTransform transitionOverlay;
        Image transitionDim;
        Text transitionText;
        float enemyHpDisplayFill = 1f;
        float playerHpDisplayFill = 1f;
        BattlePhase lastTrackedPhase = BattlePhase.Idle;
        float bannerFlashUntil;
        float battleIntroUntil;
        bool lastBattleActive;
        Sprite cachedStageSprite;
        string cachedStageAreaId;

        void Start() => EnsureUi();

        void Update()
        {
            combat ??= FindFirstObjectByType<CombatManager>();
            if (combat == null || UIManager.Instance == null)
            {
                SetVisible(false);
                lastBattleActive = false;
                return;
            }

            EnsureUi();
            var active = combat.IsBattleActive;
            if (active && !lastBattleActive)
            {
                SyncHpFillFromEntities();
                battleIntroUntil = Time.unscaledTime + BattleIntroDuration;
                RefreshStageBackdrop();
            }
            lastBattleActive = active;
            SetVisible(active);
            if (!active)
            {
                lastTrackedPhase = BattlePhase.Idle;
                return;
            }

            root.SetAsLastSibling();

            var phase = combat.Phase;
            if (phase != lastTrackedPhase)
            {
                bannerFlashUntil = AccessibilitySettings.ReduceFlash
                    ? Time.unscaledTime
                    : Time.unscaledTime + BannerFlashDuration;
                lastTrackedPhase = phase;
            }

            UpdateBanner(phase);
            UpdateHpZone(combat.EnemySide, enemySilhouette, enemyHpFill, enemyHpText, ref enemyHpDisplayFill, enemyNameText,
                enemyStatText, enemyChipsRoot, enemyChipTexts, enemyChipBgs);
            UpdateHpZone(combat.PlayerSide, playerSilhouette, playerHpFill, playerHpText, ref playerHpDisplayFill, playerNameText,
                playerStatText, playerChipsRoot, playerChipTexts, playerChipBgs, true);
            UpdateGearHud();
            UpdateLog(combat.BattleLog);
            feedbackText.text = combat.FeedbackSummary ?? string.Empty;
            UpdateFeedbackStyle(feedbackText.text);
            UpdateActionMenu(phase);
            UpdateEndgameOverlay(phase, combat.FeedbackSummary ?? string.Empty);
            UpdateBattleIntro();
            RouteKeyboard(phase);

            var bannerAlpha = AccessibilitySettings.ReduceFlash
                ? BannerDimAlpha
                : (Time.unscaledTime < bannerFlashUntil ? 1f : BannerDimAlpha);
            var c = bannerPanel.color;
            c.a = bannerAlpha;
            bannerPanel.color = c;
        }

        void RouteKeyboard(BattlePhase phase)
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            if (phase == BattlePhase.PlayerTurn)
            {
                if (kb != null && kb.digit1Key.wasPressedThisFrame || pad != null && pad.dpad.up.wasPressedThisFrame) combat.UseMoveSlot(0);
                if (kb != null && kb.digit2Key.wasPressedThisFrame || pad != null && pad.dpad.right.wasPressedThisFrame) combat.UseMoveSlot(1);
                if (kb != null && kb.digit3Key.wasPressedThisFrame || pad != null && pad.leftShoulder.wasPressedThisFrame) combat.Guard();
                if (kb != null && kb.digit4Key.wasPressedThisFrame || pad != null && pad.buttonNorth.wasPressedThisFrame) combat.UsePotion();
                if (kb != null && kb.digit5Key.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame) combat.TryCapture();
                if (kb != null && kb.digit6Key.wasPressedThisFrame || pad != null && pad.buttonWest.wasPressedThisFrame) combat.SwitchToNextMonster();
                if (kb != null && kb.digit7Key.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame) combat.Flee();
            }

            if ((phase == BattlePhase.Victory || phase == BattlePhase.Defeat) &&
                ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                 (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame))))
                combat.FinishBattle();
        }

        void UpdateBanner(BattlePhase phase)
        {
            bannerText.text = phase switch
            {
                BattlePhase.PlayerTurn => "Your Turn",
                BattlePhase.EnemyTurn => "Enemy Turn",
                BattlePhase.Victory => "Victory",
                BattlePhase.Defeat => "Defeat",
                _ => "Battle"
            };
        }

        void UpdateHpZone(CombatEntity entity, Image silhouette, Image hpFill, Text hpText, ref float displayFill,
            Text nameText, Text statText, RectTransform chipsRoot, Text[] chipTexts, Image[] chipBgs, bool isPlayer = false)
        {
            if (entity == null)
            {
                nameText.text = string.Empty;
                hpText.text = string.Empty;
                statText.text = string.Empty;
                SetChips(chipsRoot, chipTexts, chipBgs, MonsterElement.Neutral, MonsterElement.None);
                return;
            }

            nameText.text = entity.DisplayName;
            silhouette.sprite = MonsterElementSprites.For(entity.PrimaryElement);
            silhouette.color = MonsterElementSprites.SilhouetteTint(entity.PrimaryElement);

            var ratio = entity.MaxHp > 0 ? Mathf.Clamp01((float)entity.CurrentHp / entity.MaxHp) : 0f;
            displayFill = Mathf.MoveTowards(displayFill, ratio, Time.deltaTime * HpFillSpeed);
            hpFill.fillAmount = displayFill;
            hpFill.color = HpTierColor(ratio);
            hpText.text = $"HP {entity.CurrentHp}/{entity.MaxHp}";

            statText.text = BuildStatLine(entity, isPlayer);
            SetChips(chipsRoot, chipTexts, chipBgs, entity.PrimaryElement, entity.SecondaryElement);
        }

        static Color HpTierColor(float hpRatio)
        {
            if (hpRatio > 0.5f) return new Color(0.25f, 0.85f, 0.38f, 1f);
            if (hpRatio > 0.25f) return new Color(0.95f, 0.72f, 0.2f, 1f);
            return new Color(0.92f, 0.28f, 0.28f, 1f);
        }

        static string BuildStatLine(CombatEntity entity, bool includeGuard)
        {
            var status = entity.Status != MonsterStatusEffect.None ? $" [{entity.Status}]" : string.Empty;
            var guard = includeGuard && entity.GuardBonus > 0 ? $"  Guard +{entity.GuardBonus}" : string.Empty;
            return $"SPD {entity.Speed}{status}{guard}";
        }

        void SetChips(RectTransform rootRt, Text[] texts, Image[] bgs, MonsterElement primary, MonsterElement secondary)
        {
            void ApplySlot(int i, MonsterElement el, bool on)
            {
                bgs[i].gameObject.SetActive(on);
                if (!on) return;
                var label = MonsterElementSprites.ChipLabel(el);
                if (string.IsNullOrEmpty(label)) label = el.ToString();
                texts[i].text = label;
                bgs[i].color = MonsterElementSprites.ChipColor(el);
            }

            var showPrimary = primary == MonsterElement.None ? MonsterElement.Neutral : primary;
            ApplySlot(0, showPrimary, true);
            var sec = secondary != MonsterElement.None;
            ApplySlot(1, secondary, sec);
            if (!sec)
                texts[1].text = string.Empty;
        }

        void UpdateLog(string fullLog)
        {
            var lines = string.IsNullOrEmpty(fullLog)
                ? new string[0]
                : fullLog.Replace("\r\n", "\n").Split('\n');
            var n = lines.Length;
            if (n == 0)
            {
                logLineA.text = string.Empty;
                logLineB.text = string.Empty;
                return;
            }

            if (n == 1)
            {
                logLineA.text = lines[0];
                logLineB.text = string.Empty;
                return;
            }

            logLineA.text = lines[n - 2];
            logLineB.text = lines[n - 1];
        }

        void UpdateActionMenu(BattlePhase phase)
        {
            var playerTurn = phase == BattlePhase.PlayerTurn;
            var end = phase == BattlePhase.Victory || phase == BattlePhase.Defeat;

            void Label(int i, string s)
            {
                if (menuLabels[i] != null) menuLabels[i].text = s;
            }

            var m0 = combat.PlayerMoves.Count > 0 ? combat.PlayerMoves[0] : null;
            var m1 = combat.PlayerMoves.Count > 1 ? combat.PlayerMoves[1] : null;
            Label(0, m0 != null
                ? $"1  {m0.DisplayName} — {FormatMoveElement(m0.Element)}  P{m0.Power}"
                : "1  (empty)");
            Label(1, m1 != null
                ? $"2  {m1.DisplayName} — {FormatMoveElement(m1.Element)}  P{m1.Power}"
                : "2  (empty)");
            Label(2, "3  Guard (reduce next hit)");
            Label(3, "4  Potion (+18 HP)");
            Label(4, "5  Capture (best on low HP)");
            Label(5, BuildSwitchLabel());
            Label(6, "7  Flee (wild only)");
            Label(7, "Continue");

            menuButtons[0].interactable = playerTurn && m0 != null;
            menuButtons[1].interactable = playerTurn && m1 != null;
            menuButtons[2].interactable = playerTurn;
            menuButtons[3].interactable = playerTurn;
            menuButtons[4].interactable = playerTurn;
            menuButtons[5].interactable = playerTurn;
            menuButtons[6].interactable = playerTurn;
            menuButtons[7].interactable = end;

            ApplyActionButtonStyle(0, m0 != null ? MonsterElementSprites.ChipColor(m0.Element) : GameVisualTheme.PanelInner, playerTurn && m0 != null);
            ApplyActionButtonStyle(1, m1 != null ? MonsterElementSprites.ChipColor(m1.Element) : GameVisualTheme.PanelInner, playerTurn && m1 != null);
            ApplyActionButtonStyle(2, GameVisualTheme.AccentGreen, playerTurn);
            ApplyActionButtonStyle(3, GameVisualTheme.Danger, playerTurn);
            ApplyActionButtonStyle(4, GameVisualTheme.Accent, playerTurn);
            ApplyActionButtonStyle(5, GameVisualTheme.AccentBlue, playerTurn);
            ApplyActionButtonStyle(6, GameVisualTheme.ParchmentDark, playerTurn);
            ApplyActionButtonStyle(7, GameVisualTheme.Accent, end);

            if (feedbackText != null && !playerTurn && !end && string.IsNullOrWhiteSpace(feedbackText.text))
                feedbackText.text = "Hold steady. Your commands open when the turn banner says Your Turn.";
        }

        void UpdateFeedbackStyle(string summary)
        {
            if (feedbackText == null)
                return;
            if (string.IsNullOrWhiteSpace(summary))
            {
                feedbackText.color = GameVisualTheme.MutedText;
                return;
            }

            if (summary.IndexOf("Type: Advantage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf("Capture succeeded", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf("Victory", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                feedbackText.color = GameVisualTheme.Accent;
                return;
            }

            if (summary.IndexOf("Type: Resisted", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf("Capture failed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf("Defeat", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                feedbackText.color = GameVisualTheme.Danger;
                return;
            }

            if (summary.IndexOf("Status:", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                summary.IndexOf("None", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                feedbackText.color = GameVisualTheme.AccentBlue;
                return;
            }

            feedbackText.color = GameVisualTheme.MutedText;
        }

        void ApplyActionButtonStyle(int index, Color color, bool enabled)
        {
            if (menuButtons == null || index < 0 || index >= menuButtons.Length || menuButtons[index] == null) return;
            var image = menuButtons[index].GetComponent<Image>();
            if (image != null)
                image.color = enabled ? GameVisualTheme.WithAlpha(color, 0.94f) : GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.58f);
            var label = menuLabels != null && index < menuLabels.Length ? menuLabels[index] : null;
            if (label != null)
                label.color = enabled && (index == 0 || index == 1 || index == 4 || index == 7)
                    ? GameVisualTheme.TextDark
                    : GameVisualTheme.Text;
        }

        string BuildSwitchLabel()
        {
            var monsters = FindFirstObjectByType<Monster.MonsterSystem>();
            var partyCount = monsters != null ? monsters.Party.Count : 0;
            return partyCount > 1 ? "6  Switch (healthy ally)" : "6  Switch (no backup)";
        }

        void SyncHpFillFromEntities()
        {
            var e = combat.EnemySide;
            if (e != null && e.MaxHp > 0)
                enemyHpDisplayFill = Mathf.Clamp01((float)e.CurrentHp / e.MaxHp);
            var p = combat.PlayerSide;
            if (p != null && p.MaxHp > 0)
                playerHpDisplayFill = Mathf.Clamp01((float)p.CurrentHp / p.MaxHp);
        }

        static string FormatMoveElement(MonsterElement el)
        {
            var s = MonsterElementSprites.ChipLabel(el);
            return string.IsNullOrEmpty(s) ? el.ToString() : s;
        }

        void UpdateEndgameOverlay(BattlePhase phase, string summary)
        {
            var show = phase == BattlePhase.Victory || phase == BattlePhase.Defeat;
            endgameOverlay.gameObject.SetActive(show);
            if (!show) return;
            endgameTitle.text = phase == BattlePhase.Victory ? "VICTORY" : "DEFEATED";
            endgameTitle.color = phase == BattlePhase.Victory ? GameVisualTheme.Accent : GameVisualTheme.Danger;
            endgameSummary.text = string.IsNullOrWhiteSpace(summary)
                ? "Press Space or Enter to continue."
                : $"{summary}\n\nPress Space or Enter to continue.";
        }

        void RefreshStageBackdrop()
        {
            var id = GameManager.Instance != null && GameManager.Instance.World != null
                ? GameManager.Instance.World.CurrentAreaId
                : DefaultGameContent.RouteId;
            if (id == cachedStageAreaId && cachedStageSprite != null && stageBackdrop != null &&
                stageBackdrop.sprite == cachedStageSprite)
                return;
            cachedStageAreaId = id;
            cachedStageSprite = CombatStageVisuals.BackdropForArea(id);
            if (stageBackdrop != null)
            {
                stageBackdrop.sprite = cachedStageSprite;
                stageBackdrop.color = Color.white;
            }
        }

        void UpdateBattleIntro()
        {
            var show = Time.unscaledTime < battleIntroUntil && combat != null && combat.IsBattleActive;
            transitionOverlay.gameObject.SetActive(show);
            if (!show) return;

            var t = Mathf.Clamp01((battleIntroUntil - Time.unscaledTime) / BattleIntroDuration);
            transitionDim.color = GameVisualTheme.WithAlpha(GameVisualTheme.Ink, Mathf.Lerp(0f, 0.35f, t));
            transitionText.text = combat.EnemySide != null
                ? $"A wild {combat.EnemySide.DisplayName} appeared!"
                : "Battle!";
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;

            root = RuntimeUiFactory.CreatePanel(UIManager.Instance.Root.transform, "CombatRoot",
                GameVisualTheme.SkyTop, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            var backdropGo = new GameObject("StageBackdrop", typeof(RectTransform), typeof(Image));
            backdropGo.transform.SetParent(root, false);
            var bdRt = backdropGo.GetComponent<RectTransform>();
            bdRt.anchorMin = new Vector2(0f, 0f);
            bdRt.anchorMax = new Vector2(1f, 0.65f);
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            stageBackdrop = backdropGo.GetComponent<Image>();
            stageBackdrop.type = Image.Type.Simple;
            stageBackdrop.preserveAspect = false;
            stageBackdrop.raycastTarget = false;

            skyBand = RuntimeUiFactory.CreatePanel(root, "SkyBand",
                GameVisualTheme.SkyTop, new Vector2(0f, 0.45f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            skyBand.color = GameVisualTheme.WithAlpha(GameVisualTheme.SkyTop, 0.38f);
            skyBand.raycastTarget = false;
            RuntimeUiFactory.CreatePanel(root, "SkyGlow",
                GameVisualTheme.SkyBottom, new Vector2(0f, 0.47f), new Vector2(1f, 0.74f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            horizonBand = RuntimeUiFactory.CreatePanel(root, "HorizonBand",
                GameVisualTheme.Hex(0x78, 0xA5, 0x68), new Vector2(0f, 0.42f), new Vector2(1f, 0.54f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            horizonBand.color = GameVisualTheme.WithAlpha(horizonBand.color, 0.55f);
            horizonBand.raycastTarget = false;
            groundBand = RuntimeUiFactory.CreatePanel(root, "GroundBand",
                GameVisualTheme.Hex(0xB7, 0x86, 0x56), new Vector2(0f, 0f), new Vector2(1f, 0.45f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            groundBand.color = GameVisualTheme.WithAlpha(GameVisualTheme.Road, 0.4f);
            groundBand.raycastTarget = false;
            RuntimeUiFactory.CreatePanel(root, "GroundTrim",
                GameVisualTheme.RoadDark, new Vector2(0f, 0.43f), new Vector2(1f, 0.45f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            enemyPlatform = RuntimeUiFactory.CreatePanel(root, "EnemyPlatform",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.22f), new Vector2(0.63f, 0.56f), new Vector2(0.98f, 0.64f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playerPlatform = RuntimeUiFactory.CreatePanel(root, "PlayerPlatform",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.28f), new Vector2(0.02f, 0.13f), new Vector2(0.4f, 0.22f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            bannerPanel = RuntimeUiFactory.CreatePanel(root, "TurnBanner",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, BannerDimAlpha), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(720f, 56f)).GetComponent<Image>();
            bannerText = RuntimeUiFactory.CreateText(bannerPanel.transform, "BannerLabel", "Battle", 26,
                TextAnchor.MiddleCenter, GameVisualTheme.Accent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                VerticalWrapMode.Truncate);

            var logPanel = RuntimeUiFactory.CreatePanel(root, "LogRibbon",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.78f), new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 58f));
            logLineA = RuntimeUiFactory.CreateText(logPanel, "LogA", string.Empty, 15,
                TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(10f, -4f), new Vector2(-20f, 22f),
                VerticalWrapMode.Truncate);
            logLineB = RuntimeUiFactory.CreateText(logPanel, "LogB", string.Empty, 15,
                TextAnchor.LowerLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(10f, 4f), new Vector2(-20f, 22f),
                VerticalWrapMode.Truncate);
            var feedbackPanel = RuntimeUiFactory.CreatePanel(root, "FeedbackStrip",
                GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.72f), new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 30f));
            feedbackText = RuntimeUiFactory.CreateText(feedbackPanel, "Feedback", string.Empty, 14,
                TextAnchor.MiddleLeft, GameVisualTheme.MutedText,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -4f),
                VerticalWrapMode.Truncate);

            enemyCluster = RuntimeUiFactory.CreatePanel(root, "EnemyZone",
                new Color(0f, 0f, 0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-52f, -42f), new Vector2(420f, 500f));
            BuildFighterColumn(enemyCluster, true, out enemySilhouette, out enemyNameText, out enemyHpBg, out enemyHpFill,
                out enemyHpText, out enemyChipsRoot, out enemyChipTexts, out enemyChipBgs, out enemyStatText);

            playerCluster = RuntimeUiFactory.CreatePanel(root, "PlayerZone",
                new Color(0f, 0f, 0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(52f, 42f), new Vector2(420f, 500f));
            BuildFighterColumn(playerCluster, false, out playerSilhouette, out playerNameText, out playerHpBg, out playerHpFill,
                out playerHpText, out playerChipsRoot, out playerChipTexts, out playerChipBgs, out playerStatText);
            BuildPlayerGearHud();

            actionPanel = RuntimeUiFactory.CreatePanel(root, "ActionMenu",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.96f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(420f, 284f));
            RuntimeUiFactory.CreatePanel(actionPanel, "ActionTrim", GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 5f));

            menuButtons = new Button[8];
            menuLabels = new Text[8];
            var gridW = 198f;
            var gridH = 60f;
            var gapX = 12f;
            var gapY = 8f;
            var baseX = 10f;
            var baseY = -10f;
            for (var row = 0; row < 4; row++)
            for (var col = 0; col < 2; col++)
            {
                var idx = row * 2 + col;
                var x = baseX + col * (gridW + gapX);
                var y = baseY - row * (gridH + gapY);
                var btn = RuntimeUiFactory.CreateButton(actionPanel, $"Menu{idx}", "-",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, y), new Vector2(gridW, gridH));
                menuButtons[idx] = btn;
                menuLabels[idx] = btn.GetComponentInChildren<Text>();
            }

            menuButtons[0].onClick.AddListener(() => combat?.UseMoveSlot(0));
            menuButtons[1].onClick.AddListener(() => combat?.UseMoveSlot(1));
            menuButtons[2].onClick.AddListener(() => combat?.Guard());
            menuButtons[3].onClick.AddListener(() => combat?.UsePotion());
            menuButtons[4].onClick.AddListener(() => combat?.TryCapture());
            menuButtons[5].onClick.AddListener(() => combat?.SwitchToNextMonster());
            menuButtons[6].onClick.AddListener(() => combat?.Flee());
            menuButtons[7].onClick.AddListener(() => combat?.FinishBattle());

            endgameOverlay = RuntimeUiFactory.CreatePanel(root, "EndgameOverlay",
                new Color(0f, 0f, 0f, 0f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(880f, 220f));
            endgameDim = RuntimeUiFactory.CreatePanel(endgameOverlay, "Dim",
                new Color(0f, 0f, 0f, 0.65f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            endgameDim.raycastTarget = true;
            endgameTitle = RuntimeUiFactory.CreateText(endgameOverlay, "EndTitle", string.Empty, 42,
                TextAnchor.MiddleCenter, GameVisualTheme.Accent, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 72f), VerticalWrapMode.Truncate);
            endgameSummary = RuntimeUiFactory.CreateText(endgameOverlay, "EndSummary", string.Empty, 20,
                TextAnchor.MiddleCenter, GameVisualTheme.Text, new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 120f), VerticalWrapMode.Truncate);
            endgameOverlay.gameObject.SetActive(false);

            transitionOverlay = RuntimeUiFactory.CreatePanel(root, "BattleIntro",
                new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            transitionDim = RuntimeUiFactory.CreatePanel(transitionOverlay, "Dim",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.35f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero).GetComponent<Image>();
            transitionDim.raycastTarget = false;
            var introCard = RuntimeUiFactory.CreatePanel(transitionOverlay, "IntroCard",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.96f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 76f));
            transitionText = RuntimeUiFactory.CreateText(introCard, "IntroText", "Battle!", 28, TextAnchor.MiddleCenter, GameVisualTheme.Accent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                VerticalWrapMode.Truncate);
            transitionOverlay.gameObject.SetActive(false);

            RefreshStageBackdrop();
        }

        void BuildFighterColumn(RectTransform cluster, bool enemyOnTop, out Image silhouette, out Text nameText,
            out Image hpBg, out Image hpFill, out Text hpText, out RectTransform chipsRoot, out Text[] chipTexts, out Image[] chipBgs,
            out Text statText)
        {
            chipTexts = new Text[2];
            chipBgs = new Image[2];

            var silPanel = RuntimeUiFactory.CreatePanel(cluster, "Silhouette",
                new Color(0f, 0f, 0f, 0f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(-12f, -12f) : new Vector2(12f, 12f),
                new Vector2(320f, 320f));
            var silGo = new GameObject("SilImage", typeof(RectTransform), typeof(Image));
            silGo.transform.SetParent(silPanel, false);
            var silRt = silGo.GetComponent<RectTransform>();
            silRt.anchorMin = Vector2.zero;
            silRt.anchorMax = Vector2.one;
            silRt.offsetMin = Vector2.zero;
            silRt.offsetMax = Vector2.zero;
            silhouette = silGo.GetComponent<Image>();
            silhouette.preserveAspect = true;

            var nameplate = RuntimeUiFactory.CreatePanel(cluster, "Nameplate",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.94f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(1f, 1f) : new Vector2(0f, 0f),
                enemyOnTop ? new Vector2(-12f, -348f) : new Vector2(12f, 348f),
                new Vector2(340f, 168f));

            nameText = RuntimeUiFactory.CreateText(nameplate, "Name", string.Empty, 20,
                TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(-16f, 28f),
                VerticalWrapMode.Truncate);

            chipsRoot = RuntimeUiFactory.CreatePanel(nameplate, "Chips",
                new Color(0f, 0f, 0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -36f), new Vector2(200f, 22f));
            for (var i = 0; i < 2; i++)
            {
                var chip = RuntimeUiFactory.CreatePanel(chipsRoot, $"Chip{i}",
                    MonsterElementSprites.ChipColor(MonsterElement.Neutral),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(4f + i * 78f, 0f), new Vector2(72f, 22f));
                chipBgs[i] = chip.GetComponent<Image>();
                chipTexts[i] = RuntimeUiFactory.CreateText(chip, "Lbl", string.Empty, 13,
                    TextAnchor.MiddleCenter, GameVisualTheme.TextDark,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                    VerticalWrapMode.Truncate);
            }

            var hpBarRt = RuntimeUiFactory.CreatePanel(nameplate, "HpBarBg",
                GameVisualTheme.Ink, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(12f, -64f), new Vector2(-24f, 18f));
            hpBg = hpBarRt.GetComponent<Image>();
            var fillGo = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(hpBarRt, false);
            var fr = fillGo.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            hpFill = fillGo.GetComponent<Image>();
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.color = new Color(0.25f, 0.85f, 0.38f, 1f);

            hpText = RuntimeUiFactory.CreateText(nameplate, "HpNums", string.Empty, 15,
                TextAnchor.UpperRight, GameVisualTheme.MutedText,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -86f), new Vector2(180f, 22f),
                VerticalWrapMode.Truncate);

            statText = RuntimeUiFactory.CreateText(nameplate, "Stats", string.Empty, 14,
                TextAnchor.LowerLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(12f, 10f), new Vector2(-12f, 28f),
                VerticalWrapMode.Truncate);
        }

        void BuildPlayerGearHud()
        {
            if (playerCluster == null) return;
            var plate = playerCluster.Find("Nameplate") as RectTransform;
            if (plate == null) return;
            playerGearHud = RuntimeUiFactory.CreatePanel(plate, "GearHudStrip",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.12f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(12f, -108f), new Vector2(-22f, 20f));
            for (var i = 0; i < 4; i++)
            {
                var cell = RuntimeUiFactory.CreatePanel(playerGearHud, $"GearCell{i}",
                    GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.35f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(6f + i * 72f, 0f), new Vector2(64f, 16f)).GetComponent<RectTransform>();
                playerGearHudIcons[i] = cell.GetComponent<Image>();
            }
        }

        void UpdateGearHud()
        {
            var lo = GameManager.Instance != null ? GameManager.Instance.Loadout : LoadoutSystem.FindOrResolve();
            var reg = GameManager.Instance?.Assets;

            Color ColorForSlot(string itemId)
            {
                if (string.IsNullOrEmpty(itemId) || reg?.GetItem(itemId) is not GearItemData g)
                    return GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.4f);
                return GameVisualTheme.WithAlpha(g.Rarity.AccentColor(), 0.85f);
            }

            string[] ids =
            {
                lo?.OutfitEquippedId ?? "",
                lo?.GetCharmEquippedId(0) ?? "",
                lo?.GetCharmEquippedId(1) ?? "",
                lo?.GetCharmEquippedId(2) ?? ""
            };

            for (var i = 0; i < 4 && i < playerGearHudIcons.Length; i++)
                if (playerGearHudIcons[i] != null)
                    playerGearHudIcons[i].color = ColorForSlot(ids[i]);

            if (playerSilhouette != null && combat != null && combat.PlayerSide != null)
            {
                var mods = lo?.Snapshot ?? LoadoutModifiers.Empty;
                var bc = MonsterElementSprites.SilhouetteTint(combat.PlayerSide.PrimaryElement);
                playerSilhouette.color = Color.Lerp(bc, mods.AuraTint, Mathf.Clamp01(mods.AuraTint.a + 0.2f) * 0.35f);
            }
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}

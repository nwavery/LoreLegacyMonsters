using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;
using UnityEngine;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    /// <summary>
    /// Runtime map overlay: discovered route nodes, current area marker, and active quest destination hint.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        static readonly string[] AreaOrder =
        {
            DefaultGameContent.TownId,
            DefaultGameContent.RouteId,
            DefaultGameContent.ForestId,
            DefaultGameContent.GroveId,
            DefaultGameContent.MarshId,
            DefaultGameContent.RuinsId,
            DefaultGameContent.DeltaId,
            DefaultGameContent.RidgeId,
            DefaultGameContent.SpireId,
            DefaultGameContent.BramblewoodNorthId,
            DefaultGameContent.MarshBasinId,
            DefaultGameContent.StonewakeId,
            DefaultGameContent.MoonwellId,
            DefaultGameContent.QuarryId,
            DefaultGameContent.CrossingId,
            DefaultGameContent.StarfallId
        };

        static readonly Color[] NodeColors =
        {
            GameVisualTheme.Parchment,
            GameVisualTheme.GrassLight,
            GameVisualTheme.Forest,
            GameVisualTheme.AccentGreen,
            GameVisualTheme.Water,
            GameVisualTheme.Stone,
            GameVisualTheme.WaterDeep,
            GameVisualTheme.Hex(0x8E, 0x93, 0xA0),
            GameVisualTheme.Hex(0xA6, 0x99, 0xC9),
            GameVisualTheme.Forest,
            GameVisualTheme.Water,
            GameVisualTheme.GrassLight,
            GameVisualTheme.AccentBlue,
            GameVisualTheme.Stone,
            GameVisualTheme.WaterDeep,
            GameVisualTheme.Hex(0x95, 0x89, 0xC4)
        };

        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        RectTransform[] nodeRoots;
        Image[] nodeImages;
        Image[] connectorImages;
        WorldMapEdge[] connectorEdges;
        Text[] nodeLabels;
        Text[] nodeSubLabels;
        Text[] playerMarkers;
        Text[] questMarkers;
        Text hintText;
        Text questText;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            if (UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            var open = UIManager.Instance.IsModalOpen(UiModal.Map);
            SetVisible(open);
            if (!open || controller == null || controller.World == null) return;

            root.SetAsLastSibling();
            Refresh();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;

            root = RuntimeUiFactory.CreatePanel(UIManager.Instance.Root.transform, "MapRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 560f));
            RuntimeUiFactory.CreateModalWindowChrome(root, "Map", GameVisualTheme.AccentBlue, "Close [M/Esc]",
                () => UIManager.Instance?.SetModalOpen(UiModal.Map, false));

            var legend = RuntimeUiFactory.CreateCard(root, "Legend", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.72f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-48f, 52f));
            RuntimeUiFactory.CreateText(legend, "LegendText",
                "YOU marks your current area. ! marks your active quest lead. Gold routes are safer roads; danger patches are usually off-road.",
                17, TextAnchor.MiddleCenter, GameVisualTheme.MutedText, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -8f), VerticalWrapMode.Truncate);

            var mapArea = RuntimeUiFactory.CreateCard(root, "MapArea", GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.16f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f),
                new Vector2(960f, 360f), GameVisualTheme.AccentBlue);

            nodeRoots = new RectTransform[AreaOrder.Length];
            nodeImages = new Image[AreaOrder.Length];
            nodeLabels = new Text[AreaOrder.Length];
            nodeSubLabels = new Text[AreaOrder.Length];
            playerMarkers = new Text[AreaOrder.Length];
            questMarkers = new Text[AreaOrder.Length];
            connectorImages = new Image[WorldMapLayout.MapEdges.Count];
            connectorEdges = new WorldMapEdge[WorldMapLayout.MapEdges.Count];

            for (var i = 0; i < WorldMapLayout.MapEdges.Count; i++)
            {
                var edge = WorldMapLayout.MapEdges[i];
                var fromPos = WorldMapLayout.MapPosition(edge.FromAreaId);
                var toPos = WorldMapLayout.MapPosition(edge.ToAreaId);
                var delta = toPos - fromPos;
                var line = RuntimeUiFactory.CreatePanel(mapArea, $"Connector_{edge.FromAreaId}_{edge.ToAreaId}", GameVisualTheme.WithAlpha(GameVisualTheme.MutedText, 0.28f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    (fromPos + toPos) * 0.5f, new Vector2(Mathf.Max(24f, delta.magnitude - 48f), 6f));
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                connectorImages[i] = line.GetComponent<Image>();
                connectorEdges[i] = edge;
            }

            for (var i = 0; i < AreaOrder.Length; i++)
            {
                var pos = WorldMapLayout.MapPosition(AreaOrder[i]);
                var node = RuntimeUiFactory.CreatePanel(mapArea, $"Node_{AreaOrder[i]}", NodeColors[i],
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos,
                    new Vector2(68f, 68f));
                nodeRoots[i] = node;
                nodeImages[i] = node.GetComponent<Image>();
                nodeLabels[i] = RuntimeUiFactory.CreateText(node, "Name", string.Empty, 13, TextAnchor.UpperCenter,
                    GameVisualTheme.TextDark, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(118f, 48f), VerticalWrapMode.Truncate);
                nodeSubLabels[i] = RuntimeUiFactory.CreateText(node, "Sub", string.Empty, 12, TextAnchor.MiddleCenter,
                    GameVisualTheme.TextDark, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(-8f, -8f), VerticalWrapMode.Truncate);
                playerMarkers[i] = RuntimeUiFactory.CreateText(node, "PlayerMarker", "YOU", 14, TextAnchor.MiddleCenter,
                    GameVisualTheme.Accent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 22f), new Vector2(60f, 24f), VerticalWrapMode.Truncate);
                questMarkers[i] = RuntimeUiFactory.CreateText(node, "QuestMarker", "!", 30, TextAnchor.MiddleCenter,
                    GameVisualTheme.Danger, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -22f), new Vector2(44f, 44f), VerticalWrapMode.Truncate);
            }

            questText = RuntimeUiFactory.CreateText(root, "QuestText", string.Empty, 19, TextAnchor.UpperLeft,
                GameVisualTheme.Text, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(44f, 78f), new Vector2(940f, 54f), VerticalWrapMode.Truncate);
            hintText = RuntimeUiFactory.CreateText(root, "HintText", "Press M or Esc to close. Follow bright connectors toward !.", 16, TextAnchor.LowerLeft,
                GameVisualTheme.MutedText, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(44f, 28f), new Vector2(-88f, 32f), VerticalWrapMode.Truncate);
            RuntimeUiFactory.ApplyHintTextStyle(hintText);

            root.gameObject.SetActive(false);
        }

        void Refresh()
        {
            var world = controller.World;
            var currentIndex = AreaIndex(world.CurrentAreaId);
            var questArea = ResolveQuestAreaId(controller.Quests);
            var questIndex = AreaIndex(questArea);
            var questMarkerIndex = ResolveQuestMarkerIndex(world, currentIndex, questIndex);

            for (var i = 0; i < AreaOrder.Length; i++)
            {
                var areaId = AreaOrder[i];
                var discovered = world.IsAreaDiscovered(areaId);
                var area = world.GetArea(areaId);
                var isCurrent = i == currentIndex;
                var isQuest = i == questMarkerIndex;

                nodeImages[i].color = discovered
                    ? (isCurrent ? GameVisualTheme.Accent : NodeColors[i])
                    : GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.75f);
                nodeLabels[i].text = discovered ? WrapName(area != null ? area.DisplayName : areaId) : "???";
                nodeLabels[i].color = discovered ? GameVisualTheme.TextDark : GameVisualTheme.MutedText;
                nodeSubLabels[i].text = discovered ? AreaIcon(areaId) : "?";
                nodeSubLabels[i].color = discovered ? GameVisualTheme.TextDark : GameVisualTheme.MutedText;
                playerMarkers[i].gameObject.SetActive(isCurrent);
                questMarkers[i].gameObject.SetActive(isQuest);
                nodeRoots[i].localScale = isCurrent ? Vector3.one * 1.12f : isQuest ? Vector3.one * 1.06f : Vector3.one;
            }

            for (var i = 0; i < connectorImages.Length; i++)
            {
                var edge = connectorEdges[i];
                var fromDiscovered = world.IsAreaDiscovered(edge.FromAreaId);
                var toDiscovered = world.IsAreaDiscovered(edge.ToAreaId);
                var onQuestLead = !string.IsNullOrEmpty(questArea) && (edge.Contains(world.CurrentAreaId) || edge.Contains(questArea));
                connectorImages[i].color = fromDiscovered && toDiscovered
                    ? GameVisualTheme.WithAlpha(onQuestLead ? GameVisualTheme.Danger : GameVisualTheme.Accent, onQuestLead ? 0.88f : 0.72f)
                    : (fromDiscovered || toDiscovered
                        ? GameVisualTheme.WithAlpha(GameVisualTheme.MutedText, 0.24f)
                        : GameVisualTheme.WithAlpha(GameVisualTheme.MutedText, 0.08f));
            }

            questText.text = BuildQuestText(world, questArea, questMarkerIndex, questIndex);
        }

        string BuildQuestText(WorldManager world, string questArea, int markerIndex, int questIndex)
        {
            if (controller.Quests == null || string.IsNullOrEmpty(controller.Quests.GetPrimaryQuestId()))
                return "Quest: No active quest.";

            var title = controller.Quests.GetPrimaryQuestTitle();
            var objective = controller.Quests.GetNextObjectiveText(controller.Quests.GetPrimaryQuestId());
            if (string.IsNullOrEmpty(questArea) || questIndex < 0)
                return $"Quest: {title} - {objective}\nTip: X appears when this objective maps to a region.";

            var exact = markerIndex == questIndex && world.IsAreaDiscovered(questArea);
            var markerAreaId = markerIndex >= 0 ? AreaOrder[markerIndex] : world.CurrentAreaId;
            var markerArea = world.GetArea(markerAreaId);
            var markerName = markerArea != null ? markerArea.DisplayName : markerAreaId;
            var suffix = exact ? markerName : $"{markerName}; the lead continues into undiscovered country";
            return $"Quest: {title} - {objective}\n!: {suffix}\nTip: Use WASD to travel and E to resolve NPC objectives.";
        }

        static int ResolveQuestMarkerIndex(WorldManager world, int currentIndex, int questIndex)
        {
            if (questIndex < 0) return -1;
            if (world.IsAreaDiscovered(AreaOrder[questIndex])) return questIndex;
            if (currentIndex < 0) currentIndex = 0;

            var direction = questIndex >= currentIndex ? 1 : -1;
            var marker = currentIndex;
            for (var i = currentIndex; i >= 0 && i < AreaOrder.Length; i += direction)
            {
                if (!world.IsAreaDiscovered(AreaOrder[i]))
                    break;
                marker = i;
                if (i == questIndex) break;
            }

            return marker;
        }

        static string ResolveQuestAreaId(QuestManager quests)
        {
            var objective = quests != null ? quests.GetPrimaryQuestObjectiveId() : string.Empty;
            return QuestObjectiveTargetMap.ResolveAreaId(objective);
        }

        static int AreaIndex(string areaId)
        {
            for (var i = 0; i < AreaOrder.Length; i++)
                if (AreaOrder[i] == areaId)
                    return i;
            return -1;
        }

        static string AreaIcon(string areaId)
        {
            return areaId switch
            {
                DefaultGameContent.TownId => "Town",
                DefaultGameContent.RouteId => "Road",
                DefaultGameContent.ForestId => "Woods",
                DefaultGameContent.GroveId => "Grove",
                DefaultGameContent.MarshId => "Marsh",
                DefaultGameContent.RuinsId => "Ruins",
                DefaultGameContent.DeltaId => "Delta",
                DefaultGameContent.RidgeId => "Ridge",
                DefaultGameContent.SpireId => "Spire",
                DefaultGameContent.BramblewoodNorthId => "North",
                DefaultGameContent.MarshBasinId => "Basin",
                DefaultGameContent.StonewakeId => "Hamlet",
                DefaultGameContent.MoonwellId => "Moon",
                DefaultGameContent.QuarryId => "Quarry",
                DefaultGameContent.CrossingId => "Bridge",
                DefaultGameContent.StarfallId => "Hollow",
                _ => string.Empty
            };
        }

        static string WrapName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name.Replace(" ", "\n");
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}

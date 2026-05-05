using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters
{
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] protected List<string> activeQuests = new List<string>();
        [SerializeField] protected List<string> completedQuests = new List<string>();

        readonly Dictionary<string, QuestData> questDefinitions = new Dictionary<string, QuestData>();
        readonly Dictionary<string, List<int>> objectiveProgress = new Dictionary<string, List<int>>();

        public void RegisterQuestDefinition(QuestData data)
        {
            if (data == null || string.IsNullOrEmpty(data.QuestId)) return;
            if (questDefinitions.ContainsKey(data.QuestId)) return;
            questDefinitions[data.QuestId] = data;
        }

        public void LoadFromSave(List<string> active, List<string> completed,
            List<QuestSaveEntry> questSnapshots = null)
        {
            activeQuests = active != null ? new List<string>(active) : new List<string>();
            completedQuests = completed != null ? new List<string>(completed) : new List<string>();
            objectiveProgress.Clear();
            if (questSnapshots != null)
            {
                foreach (var e in questSnapshots)
                {
                    if (e == null || string.IsNullOrEmpty(e.questId) || e.objectiveProgress == null) continue;
                    objectiveProgress[e.questId] = new List<int>(e.objectiveProgress);
                }
            }

            foreach (var id in activeQuests)
                EnsureObjectiveProgress(id);
        }

        public List<QuestSaveEntry> ExportQuestProgress()
        {
            var list = new List<QuestSaveEntry>();
            foreach (var id in activeQuests)
            {
                if (!objectiveProgress.TryGetValue(id, out var p)) continue;
                list.Add(new QuestSaveEntry { questId = id, objectiveProgress = new List<int>(p) });
            }

            return list;
        }

        public List<string> GetActiveIds() => new List<string>(activeQuests);

        public List<string> GetCompletedIds() => new List<string>(completedQuests);

        public bool IsActive(string id) => !string.IsNullOrEmpty(id) && activeQuests.Contains(id);

        public bool HasActive(string id) => IsActive(id);

        public bool IsCompleted(string id) => !string.IsNullOrEmpty(id) && completedQuests.Contains(id);

        public QuestData GetDefinition(string id) =>
            !string.IsNullOrEmpty(id) && questDefinitions.TryGetValue(id, out var q) ? q : null;

        public string GetPrimaryQuestSummary()
        {
            var id = GetPrimaryQuestId();
            return string.IsNullOrWhiteSpace(id) ? "No active quest" : GetQuestSummary(id);
        }

        public string GetPrimaryQuestId()
        {
            if (activeQuests.Count == 0) return null;

            string bestId = null;
            var bestScore = int.MinValue;
            foreach (var id in activeQuests)
            {
                var score = GetQuestPriorityScore(id);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = id;
                }
            }

            return bestId;
        }

        public string GetPrimaryQuestTitle()
        {
            var id = GetPrimaryQuestId();
            if (string.IsNullOrWhiteSpace(id)) return "No active quest";
            var def = GetDefinition(id);
            return def != null ? def.DisplayName : id;
        }

        public string GetPrimaryQuestTrackerText()
        {
            var id = GetPrimaryQuestId();
            return string.IsNullOrWhiteSpace(id) ? "No active quest" : BuildTrackerText(id);
        }

        public string GetPrimaryQuestObjectiveId()
        {
            var id = GetPrimaryQuestId();
            return string.IsNullOrWhiteSpace(id) ? string.Empty : GetNextObjectiveId(id);
        }

        public List<string> GetPrioritizedActiveIds()
        {
            var ids = new List<string>(activeQuests);
            ids.Sort((a, b) =>
            {
                var scoreCompare = GetQuestPriorityScore(b).CompareTo(GetQuestPriorityScore(a));
                if (scoreCompare != 0) return scoreCompare;
                return string.CompareOrdinal(a, b);
            });
            return ids;
        }

        public int GetQuestChapter(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            if (id.StartsWith("quest_ch"))
            {
                var start = "quest_ch".Length;
                var digits = string.Empty;
                while (start < id.Length && char.IsDigit(id[start]))
                {
                    digits += id[start];
                    start++;
                }

                if (int.TryParse(digits, out var chapter) && chapter > 0)
                    return chapter;
            }

            return id.StartsWith("quest_") ? 1 : 0;
        }

        public string GetQuestChapterLabel(string id)
        {
            var chapter = GetQuestChapter(id);
            return chapter > 0 ? $"Chapter {chapter}" : "Other";
        }

        public string GetQuestSummary(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Unknown quest";
            if (!questDefinitions.TryGetValue(id, out var def) || def == null)
                return id;
            var summary = def.DisplayName;
            if (objectiveProgress.TryGetValue(id, out var prog) && def.Objectives != null && def.Objectives.Length > 0)
            {
                var parts = new List<string>();
                for (var i = 0; i < def.Objectives.Length; i++)
                {
                    var cur = i < prog.Count ? prog[i] : 0;
                    parts.Add($"{def.Objectives[i].description} ({cur}/{def.Objectives[i].requiredCount})");
                }

                summary += "\n" + string.Join("\n", parts);
            }

            return summary;
        }

        string BuildTrackerText(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "No active quest";
            if (!questDefinitions.TryGetValue(id, out var def) || def == null)
                return id;

            var nextObjective = GetNextObjectiveText(id);
            if (string.IsNullOrWhiteSpace(nextObjective))
                return def.DisplayName;

            return $"{def.DisplayName}\n{nextObjective}";
        }

        public string GetNextObjectiveText(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            if (!questDefinitions.TryGetValue(id, out var def) || def == null || def.Objectives == null)
                return string.Empty;

            EnsureObjectiveProgress(id);
            objectiveProgress.TryGetValue(id, out var prog);
            for (var i = 0; i < def.Objectives.Length; i++)
            {
                var current = prog != null && i < prog.Count ? prog[i] : 0;
                if (current < def.Objectives[i].requiredCount)
                    return $"{def.Objectives[i].description} ({current}/{def.Objectives[i].requiredCount})";
            }

            return "Ready to turn in";
        }

        public string GetNextObjectiveId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            if (!questDefinitions.TryGetValue(id, out var def) || def == null || def.Objectives == null)
                return string.Empty;

            EnsureObjectiveProgress(id);
            objectiveProgress.TryGetValue(id, out var prog);
            for (var i = 0; i < def.Objectives.Length; i++)
            {
                var current = prog != null && i < prog.Count ? prog[i] : 0;
                if (current < def.Objectives[i].requiredCount)
                    return def.Objectives[i].objectiveId;
            }

            return string.Empty;
        }

        int GetQuestPriorityScore(string id)
        {
            var chapter = GetQuestChapter(id);
            var score = chapter * 1000;
            if (IsOptionalQuestId(id))
                score -= 400;
            if (id == GetNewestActiveQuestId())
                score += 150;
            if (id == GetMostProgressedQuestId())
                score += 50;
            if (id.StartsWith("quest_", System.StringComparison.Ordinal))
                score += 25;
            return score;
        }

        string GetNewestActiveQuestId() => activeQuests.Count > 0 ? activeQuests[activeQuests.Count - 1] : null;

        string GetMostProgressedQuestId()
        {
            string bestId = null;
            var bestProgress = float.MinValue;
            foreach (var id in activeQuests)
            {
                var progress = GetQuestProgressRatio(id);
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                    bestId = id;
                }
            }

            return bestId;
        }

        float GetQuestProgressRatio(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0f;
            if (!questDefinitions.TryGetValue(id, out var def) || def == null || def.Objectives == null || def.Objectives.Length == 0)
                return 0f;

            EnsureObjectiveProgress(id);
            objectiveProgress.TryGetValue(id, out var prog);
            var current = 0f;
            var required = 0f;
            for (var i = 0; i < def.Objectives.Length; i++)
            {
                current += prog != null && i < prog.Count ? prog[i] : 0;
                required += def.Objectives[i].requiredCount;
            }

            return required <= 0f ? 0f : current / required;
        }

        static bool IsOptionalQuestId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return id.Contains("_collector") || id.Contains("_mentor") || id.Contains("_rumor") ||
                   id.Contains("_jessa_") || id.Contains("_luma_") || id.Contains("_sable_rematch");
        }

        public void StartQuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (completedQuests.Contains(id)) return;
            if (!activeQuests.Contains(id))
            {
                activeQuests.Add(id);
                if (questDefinitions.TryGetValue(id, out var def) && def != null)
                    GameEvents.RaiseToast($"New quest: {def.DisplayName}");
            }
            EnsureObjectiveProgress(id);
        }

        void EnsureObjectiveProgress(string id)
        {
            if (objectiveProgress.ContainsKey(id)) return;
            if (!questDefinitions.TryGetValue(id, out var def) || def.Objectives == null)
            {
                objectiveProgress[id] = new List<int>();
                return;
            }

            var list = new List<int>();
            for (var i = 0; i < def.Objectives.Length; i++) list.Add(0);
            objectiveProgress[id] = list;
        }

        /// <summary>Advance any active quest that has an objective with this id (e.g. win_battle).</summary>
        public void ReportObjectiveEvent(string objectiveId, int delta = 1)
        {
            if (string.IsNullOrEmpty(objectiveId)) return;
            var copy = new List<string>(activeQuests);
            foreach (var qid in copy)
            {
                if (!questDefinitions.TryGetValue(qid, out var def) || def.Objectives == null) continue;
                EnsureObjectiveProgress(qid);
                var prog = objectiveProgress[qid];
                for (var i = 0; i < def.Objectives.Length; i++)
                {
                    if (def.Objectives[i].objectiveId != objectiveId) continue;
                    while (prog.Count <= i) prog.Add(0);
                    var cap = def.Objectives[i].requiredCount;
                    prog[i] = Mathf.Min(prog[i] + delta, cap);
                }

                if (IsQuestObjectivesComplete(qid)) CompleteQuest(qid);
            }
        }

        bool IsQuestObjectivesComplete(string questId)
        {
            if (!questDefinitions.TryGetValue(questId, out var def) || def.Objectives == null ||
                def.Objectives.Length == 0) return true;
            if (!objectiveProgress.TryGetValue(questId, out var prog)) return false;
            for (var i = 0; i < def.Objectives.Length; i++)
            {
                var cur = i < prog.Count ? prog[i] : 0;
                if (cur < def.Objectives[i].requiredCount) return false;
            }

            return true;
        }

        public void CompleteQuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!activeQuests.Remove(id)) return;
            completedQuests.Add(id);
            objectiveProgress.Remove(id);
            GameEvents.RaiseQuestCompleted(id);
            if (questDefinitions.TryGetValue(id, out var def) && def != null)
                GameEvents.RaiseToast($"Quest complete: {def.DisplayName}");
            if (GameManager.Instance != null) GameManager.Instance.PlayerGold += 25;
            GameEvents.RaiseGoldChanged(GameManager.Instance != null ? GameManager.Instance.PlayerGold : 0);
        }

        public void CancelQuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            activeQuests.Remove(id);
            objectiveProgress.Remove(id);
        }
    }
}

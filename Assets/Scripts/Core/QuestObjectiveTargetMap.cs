using System.Collections.Generic;

namespace LoreLegacyMonsters.Core
{
    public static class QuestObjectiveTargetMap
    {
        static readonly HashSet<string> ExplicitNonMapObjectives = new HashSet<string>
        {
            "win_battle"
        };

        public static string ResolveAreaId(string objectiveId)
        {
            return ObjectiveRegistry.ResolveAreaId(objectiveId);
        }

        public static bool IsExplicitNonMapObjective(string objectiveId) =>
            !string.IsNullOrEmpty(objectiveId) && ExplicitNonMapObjectives.Contains(objectiveId);

        public static bool IsMappedOrExplicitNonMap(string objectiveId) =>
            !string.IsNullOrEmpty(ResolveAreaId(objectiveId)) || IsExplicitNonMapObjective(objectiveId);
    }
}

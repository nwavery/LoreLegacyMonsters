using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters.Quest
{
    public class QuestDatabase : ScriptableObject
    {
        [SerializeField] List<Questing.QuestData> quests = new List<Questing.QuestData>();

        public IReadOnlyList<Questing.QuestData> All => quests;
    }
}

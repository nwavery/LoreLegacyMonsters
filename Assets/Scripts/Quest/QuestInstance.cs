using UnityEngine;

namespace LoreLegacyMonsters.Quest
{
    public class QuestInstance : IQuest
    {
        public string Id { get; }
        public QuestStatus Status { get; private set; }

        public QuestInstance(string id, QuestStatus status = QuestStatus.Active)
        {
            Id = id;
            Status = status;
        }

        public void Complete() => Status = QuestStatus.Completed;

        public void Fail() => Status = QuestStatus.Failed;
    }
}

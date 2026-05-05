namespace LoreLegacyMonsters.Quest
{
    public interface IQuest
    {
        string Id { get; }
        QuestStatus Status { get; }
    }
}

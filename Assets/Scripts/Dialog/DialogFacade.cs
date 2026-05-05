using LoreLegacyMonsters.Dialogue;

namespace LoreLegacyMonsters.Dialog
{
    /// <summary>Single entry points for the parallel Dialog / Dialogue stacks.</summary>
    public static class DialogFacade
    {
        public static void StartLineBased(DialogSystem system, DialogData data) => system?.Begin(data);

        public static void StartGraph(DialogueSystem system, DialogueData data) => system?.StartDialogue(data);
    }
}

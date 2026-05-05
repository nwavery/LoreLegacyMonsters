namespace LoreLegacyMonsters.Dialog.Llm
{
    public enum NpcLlmCommandType
    {
        None,
        OfferHint,
        SuggestDestination,
        OpenShop,
        OfferHeal,
        OfferBattle
    }

    public sealed class NpcLlmValidatedCommand
    {
        public NpcLlmCommandType Type;
        public string Payload;
        public bool IsValid => Type != NpcLlmCommandType.None;
    }
}

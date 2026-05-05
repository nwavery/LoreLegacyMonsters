using System.Collections.Generic;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    public static class PhaseTwoWorldContent
    {
        public static DialogData BuildDialog(string npcId, QuestManager quests)
        {
            return npcId switch
            {
                NPCController.CartographerJessaId => StoryFlags.HasFlag(PhaseTwoIds.FlagHelpedJessaLandmarks)
                    ? CreateRuntimeDialog(PhaseTwoIds.CartographerDialog, "Jessa Vale, Cartographer",
                        StoryState.GetAdvisor() == StoryState.AdvisorJessa
                            ? "You chose truth-first. Then we finish the map and cut the network where it hides."
                            : "Moonwell, Ironroot, Starfall. Three hard places, marked cleanly because you brought back proof.",
                        StoryFlags.HasFlag(StoryState.JessaFormerMiraKnown)
                            ? "Now that you know my old ties to Mira, you also know why I do not tolerate half-truth maps."
                            : "Some roads were hidden on purpose. You are the first trainer who forced the full chart back into public hands.",
                        StoryState.GetOutcome(StoryState.CorinOutcomeKey) == StoryState.CorinSideWithCorin
                            ? "Corin's line through the archive changed who still trusts our maps. We mark that risk openly."
                            : "Corin's archive gamble shook the north, but a clear map still steadies frightened travelers.",
                        "A map grows teeth when travelers trust it. Hollowfen can travel farther now because of you.")
                    : CreateRuntimeDialog(PhaseTwoIds.CartographerDialog, "Jessa Vale, Cartographer",
                    "The old east road was never the whole map. It was just the safest line Hollowfen remembered.",
                    "Stonewake is awake again, the northwood trails are passable, and your map should grow with every place you dare to mark.",
                    "Bring me landmarks, not guesses. A good map is a promise travelers can stand on."),
                NPCController.QuartermasterBramId => CreateRuntimeDialog(PhaseTwoIds.QuartermasterDialog, "Bram Kettle, Quartermaster",
                    StoryState.GetAdvisor() == StoryState.AdvisorBram
                        ? "You picked me as your advisor. Good. Start with supplies, then heroics."
                        : "If you are walking north, carry cures and charms. The Wilderward does not care how heroic your pockets feel.",
                    StoryState.GetOutcome(StoryState.VaroOutcomeKey) == StoryState.VaroAlly
                        ? "Since you allied with Varo, patrol kits now include containment markers before spare rope."
                        : "Since the spire conflict, everyone buying from me asks for one extra tonic.",
                    "Stonewake can keep you supplied, but the roads need reopening before trade feels normal again.",
                    "I packed extra salves by the door. That means I expect you back, not that I expect trouble to be polite."),
                NPCController.RunnerNiaId => CreateRuntimeDialog(PhaseTwoIds.RunnerDialog, "Nia Reed, Marsh Runner",
                    "The basin boardwalks still hold if you step where the reeds bend. Follow my markers and you will keep your boots.",
                    StoryFlags.HasFlag(StoryState.NetworkAware)
                        ? "Word spread fast after your spire call. Travelers now ask which routes avoid network dead-zones."
                        : "No one agrees what woke the hollow signal yet. Until they do, I mark routes by what keeps people breathing.",
                    "I cleared one hazard already. The next is yours: prove the northern road can carry more than rumors.",
                    "If a lantern goes blue, slow down. If it goes out, run toward the nearest stone post."),
                NPCController.ForemanOrloId => CreateRuntimeDialog(PhaseTwoIds.ForemanDialog, "Orlo Flint, Quarry Foreman",
                    "Ironroot has been shaking like something underneath learned to breathe.",
                    "Win a few fights among the stone nests and I will believe this is a monster problem, not the hill itself waking up.",
                    "Mind the cart rails. Monsters love ambushes, but loose ore loves ankles."),
                NPCController.EthicistThrenId => CreateRuntimeDialog(PhaseTwoIds.EthicistDialog, "Thren, Monster Ethicist",
                    StoryState.GetAdvisor() == StoryState.AdvisorThren
                        ? "Then let us be clear: if you choose me, consent is the line we do not cross."
                        : "The archive network does not merely record monsters. It nudges them, remembers them, and perhaps edits what they become.",
                    StoryState.GetOutcome(StoryState.VaroOutcomeKey) == StoryState.VaroRefuseSpire
                        ? "Refusing the spire proved power can be denied. That matters as much as any victory."
                        : "Your spire decision is now case law for every elder council north of Hollowfen.",
                    "Before Hollowfen binds that power, someone needs to ask whether the monsters agreed.",
                    "Power that sounds kind can still be a cage if nobody can refuse it."),
                NPCController.MoonwellLumaId => CreateRuntimeDialog(PhaseTwoIds.MoonwellDialog, "Luma, Moonwell Keeper",
                    StoryState.GetAdvisor() == StoryState.AdvisorLuma
                        ? "Then let trust guide the decision, not fear and not hunger for control."
                        : "The Moonwell reflects bonds more clearly than faces. Your party leaves ripples before you speak.",
                    StoryState.GetOutcome(StoryState.IonaOutcomeKey) == StoryState.IonaSpare
                        ? "Your mercy in the grove still echoes here. Monsters remember restraint."
                        : "Your grove choice still shadows these waters. Bonds remember how power was first used.",
                    "If the lore network is pulling monsters out of balance, trust may be the only thing it cannot imitate.",
                    "Rest here when the road feels loud. Even brave monsters need quiet water."),
                NPCController.SableRivalId => BuildSableDialog(quests),
                _ => null
            };
        }

        static DialogData BuildSableDialog(QuestManager quests)
        {
            if (StoryFlags.HasFlag(PhaseTwoIds.FlagSablePeaceResolution))
                return CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "You chose words at the crossing. Do not expect me to mock that.",
                    StoryFlags.HasFlag(StoryState.NetworkAware)
                        ? "After your network decision, half the wardens call that restraint and half call it surrender. I call it courage."
                        : "Convincing a rival to stand down is harder than winning one more clean fight.");
            if (StoryFlags.HasFlag(PhaseTwoIds.FlagSableBattleResolution))
                return CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "You beat me clean. I will hate that until it makes me better.",
                    "The Wilderward saw strength from you. Now prove strength can listen.");
            return quests != null && quests.IsCompleted(PhaseTwoIds.BindingChoiceQuest)
                ? CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "You made your choice. I still do not know if it was right, but at least it was yours.",
                    "Next time we fight, I want it to be because we both know what we are defending.")
                : CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "Everyone in the Wilderward wants you to choose carefully. I want to know if you can choose under pressure.",
                    "Meet me on Tideglass Crossing and prove your conviction can survive a real fight.",
                    "Do not bring speeches. Bring a party that believes you.");
        }

        static DialogData CreateRuntimeDialog(string id, string speaker, params string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogData>();
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var entries = new List<DialogEntry>();
            foreach (var line in lines)
                entries.Add(new DialogEntry { speaker = speaker, line = line });
            data.Configure(id, entries.ToArray());
            return data;
        }
    }
}

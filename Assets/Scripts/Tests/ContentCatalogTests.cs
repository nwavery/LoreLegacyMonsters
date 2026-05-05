using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Tests
{
    public class ContentCatalogTests
    {
        [Test]
        public void Resources_LoadsCurrentChapterContent()
        {
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_slime"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_emberfox"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_voltjay"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_blazelynx"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_reedfang"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_lanternmoth"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_bogwyrm"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_tidehorn"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_stormling"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_shardraptor"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_lanternoracle"));
            Assert.IsNotNull(Resources.Load<MonsterData>("Monsters/monster_deltaking"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_flame_bite"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_thunder_peck"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_tide_lash"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_lantern_flash"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_delta_crash"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_storm_spear"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_glass_slice"));
            Assert.IsNotNull(Resources.Load<MoveData>("Moves/move_oracle_glow"));
            Assert.IsNotNull(Resources.Load<ItemData>("Items/item_capture_charm"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/town"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/forest"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/marsh"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/ruins"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/delta"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/ridge"));
            Assert.IsNotNull(Resources.Load<WorldArea>("Areas/spire"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_boss"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch2_signal"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch2_archive"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch2_rival"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch2_return"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_beacon"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_delta"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_ridge"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_spire"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_return"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_collector"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_mentor"));
            Assert.IsNotNull(Resources.Load<QuestData>("Quests/quest_ch3_rumor"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_elder_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_archivist_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_rival_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_warden_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_mentor_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_storm_boss_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_collector_story"));
            Assert.IsNotNull(Resources.Load<DialogData>("Dialogs/dlg_rumor_story"));
            Assert.IsNotNull(Resources.Load<ShopData>("Shops/shop_general"));
            Assert.IsNotNull(Resources.Load<NpcLlmSettings>("Llm/NpcLlmSettings"));
        }

        [Test]
        public void ContentCatalogValidator_FindsNoDuplicateCoreIds()
        {
            CollectionAssert.IsEmpty(ContentCatalogValidator.ValidateResources());
        }
    }
}

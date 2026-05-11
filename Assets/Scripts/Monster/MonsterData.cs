using UnityEngine;
using System.Collections.Generic;

namespace LoreLegacyMonsters.Monster
{
    [CreateAssetMenu(menuName = "LLM/Monster Data", fileName = "MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [SerializeField] string monsterId;
        [SerializeField] string displayName;
        [SerializeField] int baseHp = 20;
        [SerializeField] int baseAttack = 5;
        [SerializeField] int baseDefense = 3;
        [SerializeField] int baseSpeed = 5;
        [SerializeField] MonsterRole role = MonsterRole.Striker;
        [SerializeField] MonsterElement primaryElement = MonsterElement.Neutral;
        [SerializeField] MonsterElement secondaryElement = MonsterElement.None;
        [SerializeField] GrowthBias growthBias = GrowthBias.Balanced;
        [SerializeField] [Range(0.05f, 0.95f)] float catchRate = 0.45f;
        [SerializeField] int rarity = 1;
        [SerializeField] string defaultMoveId = "move_strike";
        [SerializeField] string signatureMoveId = "move_focus";
        [SerializeField] List<MonsterMoveLearnEntry> moveLearnset = new List<MonsterMoveLearnEntry>();
        [SerializeField] MonsterEvolutionRule evolution = new MonsterEvolutionRule();
        [SerializeField] GearDropTable gearDropTable;

        public string MonsterId => monsterId;
        public string DisplayName => displayName;
        public int BaseHp => baseHp;
        public int BaseAttack => baseAttack;
        public int BaseDefense => baseDefense;
        public int BaseSpeed => baseSpeed;
        public MonsterRole Role => role;
        public MonsterElement PrimaryElement => primaryElement;
        public MonsterElement SecondaryElement => secondaryElement;
        public GrowthBias GrowthBias => growthBias;
        public float CatchRate => catchRate;
        public int Rarity => rarity;
        public string DefaultMoveId => defaultMoveId;
        public string SignatureMoveId => signatureMoveId;
        public IReadOnlyList<MonsterMoveLearnEntry> MoveLearnset => moveLearnset;
        public MonsterEvolutionRule Evolution => evolution;
        public GearDropTable GearDropTable => gearDropTable;

        /// <summary>Runtime registration hook (starter monsters / seeded tables).</summary>
        public void BindGearDropRuntime(GearDropTable table)
        {
            gearDropTable = table;
        }

        public void Configure(string id, string name, int hp, int atk, int def)
        {
            monsterId = id;
            displayName = name;
            baseHp = hp;
            baseAttack = atk;
            baseDefense = def;
            baseSpeed = 5;
            role = MonsterRole.Striker;
            primaryElement = MonsterElement.Neutral;
            secondaryElement = MonsterElement.None;
            growthBias = GrowthBias.Balanced;
            catchRate = 0.45f;
            rarity = 1;
            defaultMoveId = "move_strike";
            signatureMoveId = "move_focus";
            moveLearnset = new List<MonsterMoveLearnEntry>
            {
                new MonsterMoveLearnEntry { moveId = defaultMoveId, unlockLevel = 1 },
                new MonsterMoveLearnEntry { moveId = signatureMoveId, unlockLevel = 3 }
            };
            evolution = new MonsterEvolutionRule();
        }

        public void ConfigureIdentity(
            MonsterRole monsterRole,
            MonsterElement primary,
            GrowthBias bias,
            string basicMoveId,
            string specialMoveId,
            float capture,
            int baseSpd = 5,
            int monsterRarity = 1,
            MonsterElement secondary = MonsterElement.None,
            MonsterEvolutionRule evolutionRule = null,
            params MonsterMoveLearnEntry[] learnEntries)
        {
            role = monsterRole;
            primaryElement = primary;
            secondaryElement = secondary;
            growthBias = bias;
            defaultMoveId = string.IsNullOrWhiteSpace(basicMoveId) ? "move_strike" : basicMoveId;
            signatureMoveId = string.IsNullOrWhiteSpace(specialMoveId) ? defaultMoveId : specialMoveId;
            catchRate = Mathf.Clamp(capture, 0.05f, 0.95f);
            baseSpeed = Mathf.Max(1, baseSpd);
            rarity = Mathf.Max(1, monsterRarity);
            evolution = evolutionRule ?? new MonsterEvolutionRule();
            moveLearnset = new List<MonsterMoveLearnEntry>();
            if (learnEntries != null)
            {
                foreach (var entry in learnEntries)
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.moveId))
                        moveLearnset.Add(entry);
            }

            if (moveLearnset.Count == 0)
            {
                moveLearnset.Add(new MonsterMoveLearnEntry { moveId = defaultMoveId, unlockLevel = 1 });
                if (signatureMoveId != defaultMoveId)
                    moveLearnset.Add(new MonsterMoveLearnEntry { moveId = signatureMoveId, unlockLevel = 3 });
            }
        }
    }
}


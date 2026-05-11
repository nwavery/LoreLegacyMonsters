using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LoreLegacyMonsters.Combat;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Platform.Steam;
using LoreLegacyMonsters.Audio;

namespace LoreLegacyMonsters
{
    public enum BattlePhase
    {
        Idle,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    public class CombatManager : MonoBehaviour
    {
        [SerializeField] CombatEntity playerSide;
        [SerializeField] CombatEntity enemySide;
        [SerializeField] int victoryGold = 8;
        [SerializeField] int victoryExperience = 16;
        [Tooltip("Non-zero uses a fixed seed for reproducible combat (debug / tests).")]
        [SerializeField] int combatRngSeed;
        [SerializeField] MonsterSystem monsterSystem;
        [SerializeField] InventorySystem inventorySystem;
        [SerializeField] QuestManager questManager;
        [SerializeField] AchievementSystem achievementSystem;

        CombatBattleRunner _battleRunner;
        readonly List<MoveData> playerMoves = new List<MoveData>();
        readonly List<MoveData> enemyMoves = new List<MoveData>();
        MonsterInstance activePlayerInstance;
        MonsterData activePlayerData;
        MonsterData activeEnemyData;
        bool enemyWasBoss;
        string activeBossObjectiveId;
        string captureOutcomeLog;

        public CombatSystem Logic => _battleRunner != null ? _battleRunner.Logic : _fallbackLogic;
        public IRandomSource BattleRng => _battleRunner != null ? _battleRunner.Rng : UnityRandomSource.Default;

        static readonly CombatSystem _fallbackLogic = new CombatSystem(UnityRandomSource.Default);
        public BattlePhase Phase { get; private set; } = BattlePhase.Idle;
        public string BattleLog { get; private set; } = "Battle idle.";
        public string FeedbackSummary { get; private set; } = "Choose a move.";
        public bool IsBattleActive => Phase == BattlePhase.PlayerTurn || Phase == BattlePhase.EnemyTurn ||
                                      Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat;

        public CombatEntity PlayerSide => playerSide;
        public CombatEntity EnemySide => enemySide;
        public IReadOnlyList<MoveData> PlayerMoves => playerMoves;

        void Awake()
        {
            if (playerSide == null || enemySide == null)
            {
                var entities = GetComponentsInChildren<CombatEntity>(true);
                if (entities.Length > 0 && playerSide == null) playerSide = entities[0];
                if (entities.Length > 1 && enemySide == null) enemySide = entities[1];
            }

            var rng = combatRngSeed != 0
                ? (IRandomSource)new SeededRandomSource(combatRngSeed)
                : UnityRandomSource.Default;
            _battleRunner = new CombatBattleRunner(rng);
            ResolveCombatServices();
        }

        void ResolveCombatServices()
        {
            monsterSystem ??= FindFirstObjectByType<MonsterSystem>();
            inventorySystem ??= FindFirstObjectByType<InventorySystem>();
            questManager ??= FindFirstObjectByType<QuestManager>();
            achievementSystem ??= FindFirstObjectByType<AchievementSystem>();
        }

        public void ConfigureEntities(CombatEntity playerEntity, CombatEntity enemyEntity)
        {
            playerSide = playerEntity;
            enemySide = enemyEntity;
        }

        public void BeginBattle(MonsterData enemy, MonsterData playerMonster = null, bool bossBattle = false, string bossObjectiveId = null)
        {
            ResolveCombatServices();
            var gm = GameManager.Instance;
            var registry = gm != null ? gm.Assets : null;
            var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
            activePlayerInstance = monsters != null ? monsters.GetActiveMonster() : null;
            activePlayerData = playerMonster ?? (monsters != null ? monsters.GetActiveMonsterData(registry) : null);
            activeEnemyData = enemy;
            enemyWasBoss = bossBattle;
            activeBossObjectiveId = string.IsNullOrWhiteSpace(bossObjectiveId) ? ChapterOneIds.DefeatBoss : bossObjectiveId;
            captureOutcomeLog = null;

            if (activeEnemyData == null || activePlayerData == null || playerSide == null || enemySide == null)
            {
                Phase = BattlePhase.Idle;
                BattleLog = "Battle could not start.";
                return;
            }

            AudioManager.EnsureExists().PlayCombatMusic();
            activePlayerInstance?.RefreshLearnedMoves(activePlayerData);
            playerSide.ConfigureFromMonster(activePlayerData, activePlayerInstance, false);
            enemySide.ConfigureFromMonster(activeEnemyData, true);
            BuildMoveLists();

            var mods = gm != null ? gm.Loadout?.Snapshot : null;
            mods ??= LoreLegacyMonsters.Inventory.LoadoutModifiers.Empty;
            var aggMult = Mathf.Max(0.1f, mods.MonsterAggressionMult);
            if (!enemyWasBoss)
            {
                if (aggMult < 0.62f)
                {
                    FeedbackSummary =
                        $"{enemySide.DisplayName} hangs back—it might listen if you flee or keep your distance.";
                }
                else if (aggMult > 1.4f)
                {
                    FeedbackSummary = $"{enemySide.DisplayName} rushes forward, spoiling for a fight!";
                }
            }

            var playerFirstChance = Mathf.Clamp01(0.5f +
                                                 Mathf.Clamp(mods.InitiativeBonus * 0.14f, -0.3f, 0.42f));
            if (!enemyWasBoss && BattleRng.Next01() > playerFirstChance)
            {
                Phase = BattlePhase.EnemyTurn;
                BattleLog = $"A wild {enemySide.DisplayName} catches you off guard.";
                EnemyStrikeBack();
                return;
            }

            Phase = BattlePhase.PlayerTurn;
            BattleLog = $"A wild {enemySide.DisplayName} appeared.";
            FeedbackSummary = $"Enemy elements: {enemySide.PrimaryElement}{BuildSecondaryElementText(enemySide.SecondaryElement)}. {BuildCaptureReadinessHint()}";
        }

        public void PlayerAttack()
        {
            UseMoveSlot(0);
        }

        public void PlayerSkillAttack()
        {
            UseMoveSlot(1);
        }

        public void Guard()
        {
            if (Phase != BattlePhase.PlayerTurn) return;
            playerSide.SetGuardBonus(4);
            BattleLog = $"{playerSide.DisplayName} braces for the next hit.";
            FeedbackSummary = $"Guarding raises defense by {playerSide.GuardBonus}. Use this to survive before a potion, switch, or capture attempt.";
            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
        }

        public bool UsePotion()
        {
            if (Phase != BattlePhase.PlayerTurn || playerSide == null) return false;
            var gm = GameManager.Instance;
            var inventory = inventorySystem != null ? inventorySystem : FindFirstObjectByType<InventorySystem>();
            if (gm == null || inventory == null || inventory.Count(DefaultGameContent.PotionId) <= 0) return false;

            if (!inventory.TryRemove(DefaultGameContent.PotionId, 1)) return false;
            playerSide.Heal(18);
            SyncPlayerInstanceHp();
            BattleLog = $"{playerSide.DisplayName} used a Potion.";
            FeedbackSummary = $"Recovered 18 HP. Remaining HP: {playerSide.CurrentHp}/{playerSide.MaxHp}. Potions left: {inventory.Count(DefaultGameContent.PotionId)}.";
            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
            return true;
        }

        public bool TryUseCureConsumable(string itemId)
        {
            if (Phase != BattlePhase.PlayerTurn || playerSide == null) return false;
            var gm = GameManager.Instance;
            var inventory = inventorySystem != null ? inventorySystem : FindFirstObjectByType<InventorySystem>();
            if (gm == null || inventory == null || string.IsNullOrEmpty(itemId) || inventory.Count(itemId) <= 0) return false;
            var itemData = gm.Assets.GetItem(itemId) as ConsumableData;
            if (itemData == null || itemData.Effect != EffectType.CureStatus) return false;
            if (playerSide.Status == MonsterStatusEffect.None) return false;
            var target = itemData.CureTarget;
            if (target != MonsterStatusEffect.None && playerSide.Status != target) return false;
            if (!inventory.TryRemove(itemId, 1)) return false;
            playerSide.ClearStatus();
            if (activePlayerInstance != null)
                activePlayerInstance.persistentStatus = MonsterStatusEffect.None;
            BattleLog = $"{playerSide.DisplayName} used {itemData.DisplayName}.";
            FeedbackSummary = "Status cleared.";
            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
            return true;
        }

        public bool TryCapture()
        {
            if (Phase != BattlePhase.PlayerTurn || enemySide == null || activeEnemyData == null || enemyWasBoss) return false;
            var inventory = inventorySystem != null ? inventorySystem : FindFirstObjectByType<InventorySystem>();
            var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
            if (inventory == null || monsters == null || inventory.Count(DefaultGameContent.CaptureCharmId) <= 0) return false;
            if (!inventory.TryRemove(DefaultGameContent.CaptureCharmId, 1)) return false;

            var gm = GameManager.Instance;
            var gearCap = gm?.Loadout?.Snapshot.CaptureRateBonus ?? 0f;
            var result = CaptureRules.Roll(activeEnemyData, enemySide.CurrentHp, enemySide.MaxHp, enemySide.Status, 1.1f,
                enemyWasBoss, BattleRng, gearCap);
            if (result.Success)
            {
                var caught = monsters.AddMonster(activeEnemyData);
                var sentToReserve = caught != null && monsters.Reserve.Contains(caught);
                var ownedCount = monsters.CountOwnedByMonsterId(activeEnemyData.MonsterId);
                captureOutcomeLog = sentToReserve
                    ? $"{enemySide.DisplayName} was captured and sent to reserve."
                    : $"{enemySide.DisplayName} was captured and joined your party.";
                BattleLog = $"{enemySide.DisplayName} was captured ({Mathf.RoundToInt(result.Chance * 100f)}%).";
                FeedbackSummary = sentToReserve
                    ? $"Capture succeeded ({Mathf.RoundToInt(result.Chance * 100f)}%). Party full, so the monster went to reserve. Owned copies: {ownedCount}."
                    : $"Capture succeeded ({Mathf.RoundToInt(result.Chance * 100f)}%). The monster joined your active roster. Owned copies: {ownedCount}.";
                var captures = StoryFlags.AddInt("captures_total", 1, 0, 99999);
                achievementSystem ??= FindFirstObjectByType<AchievementSystem>();
                if (captures >= 10)
                    achievementSystem?.Unlock(SteamAchievementIds.CaptureTen);
                if (captures >= 50)
                    achievementSystem?.Unlock(SteamAchievementIds.CaptureFifty);
                EndBattleVictory(true);
                return true;
            }

            BattleLog = $"{enemySide.DisplayName} broke free ({Mathf.RoundToInt(result.Chance * 100f)}% chance).";
            FeedbackSummary = $"Capture failed ({Mathf.RoundToInt(result.Chance * 100f)}%). Lower HP and status effects increase odds. {BuildCaptureReadinessHint()}";
            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
            return false;
        }

        public bool SwitchToNextMonster()
        {
            if (Phase != BattlePhase.PlayerTurn || activeEnemyData == null) return false;
            var gm = GameManager.Instance;
            var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
            if (gm == null || monsters == null || !monsters.SwitchToNextHealthy(gm.Assets)) return false;

            activePlayerInstance = monsters.GetActiveMonster();
            activePlayerData = monsters.GetActiveMonsterData(gm.Assets);
            if (activePlayerData == null || activePlayerInstance == null) return false;
            playerSide.ConfigureFromMonster(activePlayerData, activePlayerInstance, false);
            BuildMoveLists();
            BattleLog = $"{playerSide.DisplayName} enters the fight.";
            var typePressure = TypeChart.GetMultiplier(enemySide.PrimaryElement, playerSide.PrimaryElement, playerSide.SecondaryElement);
            var pressureLabel = typePressure > 1.05f ? "Warning: enemy has type pressure." : "Type matchup is stable.";
            FeedbackSummary = $"Switched to {playerSide.DisplayName}. Status: {playerSide.Status}. {pressureLabel}";
            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
            return true;
        }

        void EnemyStrikeBack()
        {
            if (Phase != BattlePhase.EnemyTurn || playerSide == null || enemySide == null) return;
            var move = PickEnemyMove();
            ResolveMove(enemySide, activeEnemyData, null, playerSide, activePlayerData, activePlayerInstance, move, true);
            if (playerSide.IsDefeated)
            {
                HandlePlayerDefeat();
                return;
            }

            ResolveStatusTicks();
            Phase = BattlePhase.PlayerTurn;
        }

        void HandlePlayerDefeat()
        {
            var gm = GameManager.Instance;
            var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
            if (gm != null && monsters != null && monsters.SwitchToNextHealthy(gm.Assets))
            {
                activePlayerInstance = monsters.GetActiveMonster();
                activePlayerData = monsters.GetActiveMonsterData(gm.Assets);
                playerSide.ConfigureFromMonster(activePlayerData, activePlayerInstance, false);
                BuildMoveLists();
                Phase = BattlePhase.PlayerTurn;
                BattleLog = $"{playerSide.DisplayName} takes over the battle.";
                FeedbackSummary = $"Your previous monster fainted. {playerSide.DisplayName} is now active.";
                return;
            }

            Phase = BattlePhase.Defeat;
            BattleLog = "Your party has been defeated.";
            FeedbackSummary = "All available party members are down.";
            GameEvents.RaiseBattleEnded();
        }

        void EndBattleVictory(bool captured = false)
        {
            Phase = BattlePhase.Victory;
            var gm = GameManager.Instance;
            var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
            var quests = questManager != null ? questManager : FindFirstObjectByType<QuestManager>();
            var achievements = achievementSystem != null ? achievementSystem : FindFirstObjectByType<AchievementSystem>();
            var summary = CombatPostBattleReconciler.Reconcile(
                gm,
                monsters,
                quests,
                achievements,
                captured,
                enemyWasBoss,
                activeBossObjectiveId,
                victoryGold,
                victoryExperience,
                activeEnemyData);

            BattleLog = captured
                ? (string.IsNullOrWhiteSpace(captureOutcomeLog) ? $"{enemySide.DisplayName} joined your party." : captureOutcomeLog)
                : $"{enemySide.DisplayName} was defeated. You earned {summary.GoldAwarded} gold.";
            FeedbackSummary = BuildVictorySummary(summary);
            GameEvents.RaiseBattleEnded();
        }

        public void Flee()
        {
            Phase = BattlePhase.Idle;
            activeBossObjectiveId = null;
            captureOutcomeLog = null;
            BattleLog = "You fled from battle.";
            FeedbackSummary = "Retreated safely from battle.";
            GameEvents.RaiseBattleEnded();
        }

        public void FinishBattle()
        {
            if (Phase == BattlePhase.Defeat)
            {
                var gm = GameManager.Instance;
                var monsters = monsterSystem != null ? monsterSystem : FindFirstObjectByType<MonsterSystem>();
                monsters?.HealAll(gm != null ? gm.Assets : null);
                gm?.World?.SetCurrentArea(DefaultGameContent.TownId);
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    player.transform.position = new Vector3(2f, -1f, 0f);
            }

            if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
            {
                Phase = BattlePhase.Idle;
                activeBossObjectiveId = null;
                captureOutcomeLog = null;
                FeedbackSummary = "Battle closed.";
            }
        }

        void SyncPlayerInstanceHp()
        {
            if (activePlayerInstance != null && playerSide != null)
            {
                activePlayerInstance.currentHp = playerSide.CurrentHp;
                activePlayerInstance.persistentStatus = playerSide.Status;
            }
        }

        public void UseMoveSlot(int index)
        {
            if (Phase != BattlePhase.PlayerTurn || enemySide == null || index < 0 || index >= playerMoves.Count) return;
            var move = playerMoves[index];
            ResolveMove(playerSide, activePlayerData, activePlayerInstance, enemySide, activeEnemyData, null, move, false);
            enemySide.ClearGuardBonus();
            if (enemySide.IsDefeated)
            {
                EndBattleVictory();
                return;
            }

            ResolveStatusTicks();
            if (enemySide.IsDefeated)
            {
                EndBattleVictory();
                return;
            }

            Phase = BattlePhase.EnemyTurn;
            EnemyStrikeBack();
        }

        float ResolveOutgoingTypeMultiplier(CombatEntity attackerSide, MoveData move, bool enemyAction)
        {
            if (enemyAction || playerSide == null || move == null) return 1f;
            if (attackerSide != playerSide) return 1f;
            var gm = GameManager.Instance;
            return gm?.Loadout?.Snapshot.TypeDamageMult(move.Element) ?? 1f;
        }

        void ResolveMove(CombatEntity attackerSide, MonsterData attackerData, MonsterInstance attackerInstance,
            CombatEntity defenderSide, MonsterData defenderData, MonsterInstance defenderInstance, MoveData move, bool enemyAction)
        {
            if (attackerSide == null || defenderSide == null) return;
            move ??= BuildFallbackAttack();

            if (!Logic.RollHit(move.Accuracy))
            {
                BattleLog = $"{attackerSide.DisplayName} used {move.DisplayName}, but it missed.";
                FeedbackSummary = $"Missed. Accuracy was {Mathf.RoundToInt(move.Accuracy * 100f)}%.";
                return;
            }

            if (move.EffectType == MoveEffectType.Guard)
            {
                attackerSide.SetGuardBonus(Mathf.Max(2, move.GuardBonus));
                BattleLog = $"{attackerSide.DisplayName} used {move.DisplayName} and fortified its stance.";
                FeedbackSummary = $"Guard bonus set to {attackerSide.GuardBonus}.";
            }
            else if (move.EffectType == MoveEffectType.HealSelf)
            {
                attackerSide.Heal(Mathf.Max(6, move.HealAmount));
                BattleLog = $"{attackerSide.DisplayName} restored health with {move.DisplayName}.";
                FeedbackSummary = $"Recovered health up to {attackerSide.CurrentHp}/{attackerSide.MaxHp}.";
            }
            else
            {
                var defense = defenderSide.DefenseValue + defenderSide.GuardBonus;
                if (defenderSide.Status == MonsterStatusEffect.GuardBreak)
                    defense = Mathf.Max(0, defense - 2);
                var damage = Logic.CalculateMoveDamage(attackerSide.AttackPower, defense, move, attackerSide.PrimaryElement,
                    defenderSide.PrimaryElement, defenderSide.SecondaryElement, out var wasCrit, out var typeText,
                    ResolveOutgoingTypeMultiplier(attackerSide, move, enemyAction));
                defenderSide.ApplyDamage(damage);
                BattleLog = $"{attackerSide.DisplayName} used {move.DisplayName} for {damage} damage.";
                if (typeText > 1.05f) BattleLog += " It's strong against that target.";
                if (typeText < 0.95f) BattleLog += " It was resisted.";
                if (wasCrit) BattleLog += " Critical hit!";
                FeedbackSummary = BuildAttackFeedback(move, damage, typeText, wasCrit, defenderSide, false);

                var statusProb = Mathf.Clamp01(move.StatusChance);
                var gmCombat = GameManager.Instance;
                if (enemyAction && defenderSide != null && defenderSide == playerSide &&
                    gmCombat?.Loadout?.Snapshot != null)
                    statusProb /= Mathf.Max(1f, gmCombat.Loadout.Snapshot.StatusResistFor(move.InflictedStatus));
                statusProb = Mathf.Clamp01(statusProb);

                if (move.InflictedStatus != MonsterStatusEffect.None &&
                    BattleRng.Next01() <= statusProb)
                {
                    defenderSide.SetStatus(move.InflictedStatus);
                    if (defenderInstance != null)
                        defenderInstance.persistentStatus = move.InflictedStatus;
                    BattleLog += $" {defenderSide.DisplayName} is now {move.InflictedStatus}.";
                    FeedbackSummary = BuildAttackFeedback(move, damage, typeText, wasCrit, defenderSide, true);
                }
            }

            if (!enemyAction)
                SyncPlayerInstanceHp();
            else if (activePlayerInstance != null)
                SyncPlayerInstanceHp();
        }

        void ResolveStatusTicks()
        {
            ApplyStatusTick(playerSide, activePlayerInstance);
            ApplyStatusTick(enemySide, null);
        }

        void ApplyStatusTick(CombatEntity entity, MonsterInstance backingInstance)
        {
            if (entity == null || entity.Status == MonsterStatusEffect.None || entity.IsDefeated) return;
            var tick = Logic.GetStatusTickDamage(entity.MaxHp, entity.Status);
            if (tick <= 0) return;
            entity.ApplyDamage(tick);
            if (backingInstance != null)
            {
                backingInstance.currentHp = entity.CurrentHp;
                backingInstance.persistentStatus = entity.Status;
            }
            BattleLog += $" {entity.DisplayName} suffers {tick} from {entity.Status}.";
            FeedbackSummary = $"{entity.DisplayName} took {tick} damage from {entity.Status}.";
        }

        void BuildMoveLists()
        {
            playerMoves.Clear();
            enemyMoves.Clear();

            if (activePlayerInstance != null && activePlayerData != null)
                playerMoves.AddRange(MoveLibrary.GetMoves(activePlayerInstance.GetAvailableMoveIds(activePlayerData)));
            if (playerMoves.Count == 0)
                playerMoves.Add(BuildFallbackAttack());

            if (activeEnemyData != null)
                enemyMoves.AddRange(MoveLibrary.GetMoves(GetEnemyMoveIds(activeEnemyData)));
            if (enemyMoves.Count == 0)
                enemyMoves.Add(BuildFallbackAttack());
        }

        IEnumerable<string> GetEnemyMoveIds(MonsterData data)
        {
            if (data == null) yield break;
            if (!string.IsNullOrWhiteSpace(data.DefaultMoveId))
                yield return data.DefaultMoveId;
            if (data.MoveLearnset != null)
            {
                foreach (var entry in data.MoveLearnset)
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.moveId))
                        yield return entry.moveId;
            }
            if (!string.IsNullOrWhiteSpace(data.SignatureMoveId))
                yield return data.SignatureMoveId;
        }

        MoveData PickEnemyMove()
        {
            if (enemyMoves.Count == 0) return BuildFallbackAttack();

            if (activeEnemyData != null && activeEnemyData.Role == MonsterRole.Support && enemySide.CurrentHp <= enemySide.MaxHp / 2)
            {
                foreach (var move in enemyMoves)
                    if (move != null && move.EffectType == MoveEffectType.HealSelf)
                        return move;
            }

            if (activeEnemyData != null && activeEnemyData.Role == MonsterRole.Trickster && playerSide.Status == MonsterStatusEffect.None)
            {
                foreach (var move in enemyMoves)
                    if (move != null && move.InflictedStatus != MonsterStatusEffect.None)
                        return move;
            }

            if (activeEnemyData != null && activeEnemyData.Role == MonsterRole.Tank && enemySide.CurrentHp <= enemySide.MaxHp / 3)
            {
                foreach (var move in enemyMoves)
                    if (move != null && move.EffectType == MoveEffectType.Guard)
                        return move;
            }

            MoveData best = null;
            var bestScore = int.MinValue;
            foreach (var move in enemyMoves)
            {
                if (move == null) continue;
                var score = move.Power;
                if (move.EffectType == MoveEffectType.ApplyStatus) score += 2;
                if (move.EffectType == MoveEffectType.Attack && move.Element == activeEnemyData.PrimaryElement) score += 2;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }

            return best ?? enemyMoves[0];
        }

        static MoveData fallbackAttack;

        static MoveData BuildFallbackAttack()
        {
            if (fallbackAttack != null) return fallbackAttack;
            fallbackAttack = ScriptableObject.CreateInstance<MoveData>();
            fallbackAttack.Configure("move_strike", "Strike", MonsterElement.Neutral, MoveEffectType.Attack, 6);
            fallbackAttack.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return fallbackAttack;
        }

        static string BuildSecondaryElementText(MonsterElement secondary) =>
            secondary != MonsterElement.None ? $"/{secondary}" : string.Empty;

        string BuildCaptureReadinessHint()
        {
            if (activeEnemyData == null || enemySide == null || enemyWasBoss)
                return "Capture unavailable for this target.";
            var chance = CaptureRules.CalculateChance(activeEnemyData, enemySide.CurrentHp, enemySide.MaxHp, enemySide.Status,
                1.1f, false, GameManager.Instance?.Loadout?.Snapshot.CaptureRateBonus ?? 0f);
            var pct = Mathf.RoundToInt(chance * 100f);
            var band = pct >= 65 ? "high" : pct >= 40 ? "moderate" : "low";
            return $"Capture odds are currently {band} ({pct}%).";
        }

        static string BuildAttackFeedback(MoveData move, int damage, float typeMultiplier, bool wasCrit,
            CombatEntity defenderSide, bool appliedStatus)
        {
            var typeLabel = typeMultiplier > 1.05f ? "Advantage" : typeMultiplier < 0.95f ? "Resisted" : "Neutral";
            var statusLabel = appliedStatus && defenderSide != null && defenderSide.Status != MonsterStatusEffect.None
                ? defenderSide.Status.ToString()
                : "None";
            var hpText = defenderSide != null
                ? $"{defenderSide.DisplayName} HP {defenderSide.CurrentHp}/{defenderSide.MaxHp}"
                : "Target HP unknown";
            return $"{move.DisplayName}: {damage} dmg | Type: {typeLabel} | Crit: {(wasCrit ? "Yes" : "No")} | Status: {statusLabel} | {hpText}";
        }

        static string BuildVictorySummary(BattleVictorySummary summary)
        {
            var bossNote = summary.BossBattle && !string.IsNullOrWhiteSpace(summary.BossObjectiveId)
                ? $" | Objective progressed: {summary.BossObjectiveId}"
                : string.Empty;
            if (summary.Captured)
                return $"Capture succeeded | Rewards: +{summary.GoldAwarded}g, +{summary.ExperienceAwarded} XP{bossNote}";
            return $"Victory | Rewards: +{summary.GoldAwarded}g, +{summary.ExperienceAwarded} XP{bossNote}";
        }
    }
}

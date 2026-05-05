using UnityEngine;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters
{
    public class CombatEntity : MonoBehaviour
    {
        [SerializeField] string displayName = "Fighter";
        [SerializeField] int maxHp = 20;
        [SerializeField] int currentHp = 20;
        [SerializeField] int attackPower = 5;
        [SerializeField] int defenseValue = 2;
        [SerializeField] int speed = 5;
        [SerializeField] MonsterElement primaryElement = MonsterElement.Neutral;
        [SerializeField] MonsterElement secondaryElement = MonsterElement.None;
        [SerializeField] MonsterStatusEffect status;
        [SerializeField] int guardBonus;

        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public int AttackPower => attackPower;
        public int DefenseValue => defenseValue;
        public int Speed => speed;
        public MonsterElement PrimaryElement => primaryElement;
        public MonsterElement SecondaryElement => secondaryElement;
        public MonsterStatusEffect Status => status;
        public int GuardBonus => guardBonus;

        public void ResetHp() => currentHp = maxHp;

        public void ApplyDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, amount));
        }

        public void Heal(int amount) => currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(0, amount));

        public bool IsDefeated => currentHp <= 0;

        public void SetStatus(MonsterStatusEffect next) => status = next;

        public void ClearStatus() => status = MonsterStatusEffect.None;

        public void SetGuardBonus(int bonus) => guardBonus = Mathf.Max(0, bonus);

        public void ClearGuardBonus() => guardBonus = 0;

        public void ConfigureFromMonster(MonsterData data, bool isEnemy)
        {
            if (data == null) return;
            displayName = data.DisplayName;
            maxHp = data.BaseHp;
            currentHp = maxHp;
            attackPower = data.BaseAttack;
            defenseValue = data.BaseDefense;
            speed = data.BaseSpeed;
            primaryElement = data.PrimaryElement;
            secondaryElement = data.SecondaryElement;
            status = MonsterStatusEffect.None;
            guardBonus = 0;
            var weather = GameManager.Instance != null ? GameManager.Instance.Weather : null;
            if (isEnemy && weather != null && weather.Current == WeatherType.Stormy)
                attackPower += 1;
        }

        public void ConfigureFromMonster(MonsterData data, MonsterInstance instance, bool isEnemy)
        {
            if (data == null) return;
            displayName = instance != null ? instance.GetDisplayName(data) : data.DisplayName;
            var level = instance != null ? Mathf.Max(1, instance.level) : 1;
            maxHp = instance != null ? instance.maxHp : data.BaseHp + (level - 1) * 3;
            currentHp = instance != null ? Mathf.Clamp(instance.currentHp <= 0 ? maxHp : instance.currentHp, 0, maxHp) : maxHp;
            attackPower = instance != null ? instance.GetAttackStat(data) : data.BaseAttack + Mathf.Max(0, level - 1);
            defenseValue = instance != null ? instance.GetDefenseStat(data) : data.BaseDefense + Mathf.Max(0, (level - 1) / 2);
            speed = instance != null ? instance.GetSpeedStat(data) : data.BaseSpeed + Mathf.Max(0, (level - 1) / 2);
            primaryElement = data.PrimaryElement;
            secondaryElement = data.SecondaryElement;
            status = instance != null ? instance.persistentStatus : MonsterStatusEffect.None;
            guardBonus = 0;
            var weather = GameManager.Instance != null ? GameManager.Instance.Weather : null;
            if (isEnemy && weather != null && weather.Current == WeatherType.Stormy)
                attackPower += 1;
        }
    }
}

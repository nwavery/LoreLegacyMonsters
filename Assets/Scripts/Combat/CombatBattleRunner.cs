using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.Combat
{
    /// <summary>
    /// Bundles combat rules math with a shared RNG for testable battle resolution under <see cref="CombatManager"/>.
    /// </summary>
    public sealed class CombatBattleRunner
    {
        public CombatSystem Logic { get; }
        public IRandomSource Rng { get; }

        public CombatBattleRunner(IRandomSource rng = null)
        {
            Rng = rng ?? UnityRandomSource.Default;
            Logic = new CombatSystem(Rng);
        }
    }
}

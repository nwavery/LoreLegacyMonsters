namespace LoreLegacyMonsters.Core
{
    /// <summary>
    /// Abstraction over UnityEngine.Random for deterministic combat/tests.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Returns a float in [0, 1).</summary>
        float Next01();

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }

    /// <summary>Delegates to UnityEngine.Random (runtime default).</summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public static readonly UnityRandomSource Default = new UnityRandomSource();

        public float Next01() => UnityEngine.Random.value;

        public int NextInt(int minInclusive, int maxExclusive) =>
            UnityEngine.Random.Range(minInclusive, maxExclusive);
    }

    /// <summary>Deterministic RNG for tests or replay/debug builds.</summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        readonly System.Random _rng;

        public SeededRandomSource(int seed) => _rng = new System.Random(seed);

        public float Next01() => (float)_rng.NextDouble();

        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
    }
}

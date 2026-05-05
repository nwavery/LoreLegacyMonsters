using UnityEngine;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Tests
{
    public class MockMonsterSystem : MonoBehaviour
    {
        public void PrimeParty(params string[] ids)
        {
            var sys = GetComponent<MonsterSystem>();
            if (sys != null) sys.SetParty(new System.Collections.Generic.List<string>(ids));
        }
    }
}

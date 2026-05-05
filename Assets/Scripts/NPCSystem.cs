using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters
{
    public class NPCSystem : MonoBehaviour
    {
        readonly List<NPCController> registered = new List<NPCController>();

        public void Register(NPCController npc)
        {
            if (npc != null && !registered.Contains(npc)) registered.Add(npc);
        }

        public IReadOnlyList<NPCController> All => registered;
    }
}

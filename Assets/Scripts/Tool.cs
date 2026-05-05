using UnityEngine;

namespace LoreLegacyMonsters
{
    public class Tool : MonoBehaviour, IUsable
    {
        public bool TryUse()
        {
            Debug.Log("Tool used");
            return true;
        }
    }
}

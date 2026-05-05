using System;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog
{
    public class DialogSystem : MonoBehaviour
    {
        [SerializeField] DialogData current;
        int index;

        public DialogData Current => current;

        /// <summary>Fired when the active line text mutates (e.g. streaming LLM).</summary>
        public event Action LineContentChanged;

        public void Begin(DialogData data)
        {
            current = data;
            index = 0;
        }

        public bool TryGetLine(out DialogEntry entry)
        {
            entry = null;
            if (current?.Entries == null || index >= current.Entries.Length) return false;
            entry = current.Entries[index];
            return true;
        }

        public void Advance() => index++;

        public bool JumpTo(int targetIndex)
        {
            if (current?.Entries == null) return false;
            if (targetIndex < 0 || targetIndex >= current.Entries.Length) return false;
            index = targetIndex;
            return true;
        }

        public void NotifyLineContentChanged() => LineContentChanged?.Invoke();
    }
}

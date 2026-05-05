using UnityEngine;

namespace LoreLegacyMonsters.Dialog
{
    [CreateAssetMenu(menuName = "LLM/Dialog", fileName = "DialogData")]
    public class DialogData : ScriptableObject
    {
        [SerializeField] string dialogId;
        [SerializeField] DialogEntry[] entries;

        public string DialogId => dialogId;
        public DialogEntry[] Entries => entries;

        public void Configure(string id, DialogEntry[] lines)
        {
            dialogId = id;
            entries = lines;
        }
    }
}

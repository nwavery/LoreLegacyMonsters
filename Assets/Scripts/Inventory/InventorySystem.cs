using System;
using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters
{
    public partial class InventorySystem : MonoBehaviour
    {
        [Serializable]
        public class Stack
        {
            public string itemId;
            public int quantity;
        }

        [SerializeField] List<Stack> stacks = new List<Stack>();

        public void LoadFromSave(List<ItemStackDto> dto)
        {
            stacks.Clear();
            if (dto == null) return;
            foreach (var e in dto)
            {
                if (string.IsNullOrEmpty(e.itemId) || e.quantity <= 0) continue;
                stacks.Add(new Stack { itemId = e.itemId, quantity = e.quantity });
            }
        }

        public List<ItemStackDto> ToSaveDto()
        {
            var list = new List<ItemStackDto>();
            foreach (var s in stacks)
                list.Add(new ItemStackDto { itemId = s.itemId, quantity = s.quantity });
            return list;
        }

        public bool AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            var s = FindStack(itemId);
            if (s != null) s.quantity += quantity;
            else stacks.Add(new Stack { itemId = itemId, quantity = quantity });
            GameEvents.RaiseInventoryItemAdded(itemId, quantity);
            return true;
        }

        public int Count(string itemId)
        {
            var s = FindStack(itemId);
            return s?.quantity ?? 0;
        }

        public List<Stack> GetStacksSnapshot()
        {
            var list = new List<Stack>();
            foreach (var stack in stacks)
            {
                if (stack == null || string.IsNullOrEmpty(stack.itemId) || stack.quantity <= 0) continue;
                list.Add(new Stack { itemId = stack.itemId, quantity = stack.quantity });
            }

            return list;
        }

        public bool TryRemove(string itemId, int quantity)
        {
            var s = FindStack(itemId);
            if (s == null || s.quantity < quantity) return false;
            s.quantity -= quantity;
            if (s.quantity <= 0) stacks.Remove(s);
            return true;
        }

        Stack FindStack(string id)
        {
            foreach (var x in stacks)
                if (x.itemId == id)
                    return x;
            return null;
        }
    }
}

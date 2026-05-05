using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    public static class MoveLibrary
    {
        static readonly Dictionary<string, MoveData> Cache = new Dictionary<string, MoveData>();
        static bool loaded;

        public static MoveData Get(string moveId)
        {
            if (!loaded) LoadAll();
            return !string.IsNullOrWhiteSpace(moveId) && Cache.TryGetValue(moveId, out var move) ? move : null;
        }

        public static List<MoveData> GetMoves(IEnumerable<string> moveIds)
        {
            var list = new List<MoveData>();
            if (moveIds == null) return list;
            foreach (var id in moveIds)
            {
                var move = Get(id);
                if (move != null) list.Add(move);
            }
            return list;
        }

        public static void Reload()
        {
            loaded = false;
            Cache.Clear();
            LoadAll();
        }

        static void LoadAll()
        {
            loaded = true;
            Cache.Clear();
            foreach (var move in Resources.LoadAll<MoveData>("Moves"))
            {
                if (move != null && !string.IsNullOrWhiteSpace(move.MoveId))
                    Cache[move.MoveId] = move;
            }
        }
    }
}

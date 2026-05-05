using System;
using System.IO;
using UnityEngine;

namespace LoreLegacyMonsters.SaveSystem
{
    public class SaveSystem
    {
        const int CurrentVersion = 8;
        static string SaveDir => Path.Combine(Application.persistentDataPath, "Saves");

        public static string GetSlotPath(int slot) =>
            Path.Combine(SaveDir, $"save_{slot}.json");

        public static bool TryLoad(int slot, out SaveInfo data, out string error)
        {
            data = null;
            error = null;
            try
            {
                var path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    error = "Missing file";
                    return false;
                }

                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveInfo>(json);
                if (data == null)
                {
                    error = "Parse failed";
                    return false;
                }

                if (data.Version > CurrentVersion)
                {
                    error = "Save from newer game version";
                    return false;
                }

                MigrateInPlace(data);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TrySave(int slot, SaveInfo data, out string error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(SaveDir);
                MigrateInPlace(data);
                data.Version = CurrentVersion;
                var json = JsonUtility.ToJson(data, true);
                var finalPath = GetSlotPath(slot);
                var tempPath = finalPath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(finalPath))
                    File.Replace(tempPath, finalPath, null);
                else
                    File.Move(tempPath, finalPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool SlotExists(int slot) => File.Exists(GetSlotPath(slot));

        public static void MigrateInPlace(SaveInfo data)
        {
            if (data == null) return;

            data.Weather ??= new WeatherTypeDto();
            data.PartyMonsterIds ??= new System.Collections.Generic.List<string>();
            data.Party ??= new System.Collections.Generic.List<MonsterSaveEntry>();
            data.Reserve ??= new System.Collections.Generic.List<MonsterSaveEntry>();
            data.Inventory ??= new System.Collections.Generic.List<ItemStackDto>();
            data.CompletedQuestIds ??= new System.Collections.Generic.List<string>();
            data.ActiveQuestIds ??= new System.Collections.Generic.List<string>();
            data.UnlockedAchievementIds ??= new System.Collections.Generic.List<string>();
            data.ActiveQuestProgress ??= new System.Collections.Generic.List<QuestSaveEntry>();
            data.NpcMemories ??= new System.Collections.Generic.List<NpcMemorySaveEntry>();
            data.StoryFlags ??= new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(data.SaveSchemaTag))
                data.SaveSchemaTag = "v1.0";

            if (data.Party.Count == 0 && data.PartyMonsterIds.Count > 0)
            {
                foreach (var id in data.PartyMonsterIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    data.Party.Add(new MonsterSaveEntry
                    {
                        instanceId = Guid.NewGuid().ToString("N"),
                        monsterDataId = id,
                        level = 1,
                        currentHp = 0,
                        status = 0,
                        learnedMoveIds = new System.Collections.Generic.List<string>()
                    });
                }
            }
        }
    }
}

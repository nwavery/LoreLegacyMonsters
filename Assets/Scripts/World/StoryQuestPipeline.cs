using UnityEngine;
using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.Quests;

namespace LoreLegacyMonsters.World
{
    /// <summary>
    /// Single registration path for campaign quest definitions (Resources + runtime-built arcs).
    /// </summary>
    public static class StoryQuestPipeline
    {
        static readonly HashSet<int> RegisteredManagers = new HashSet<int>();

        /// <summary>
        /// Registers authored and runtime quest definitions once per quest-manager instance.
        /// Safe to call repeatedly from bootstrap paths.
        /// </summary>
        public static bool RegisterAll(QuestManager questManager)
        {
            if (questManager == null) return false;
            var managerId = questManager.GetInstanceID();
            if (!RegisteredManagers.Add(managerId))
                return false;

            foreach (var q in Resources.LoadAll<QuestData>("Quests"))
                questManager.RegisterQuestDefinition(q);

            var runtimeDefinitions = BuildRuntimeQuestDefinitions();
            for (var i = 0; i < runtimeDefinitions.Count; i++)
                questManager.RegisterQuestDefinition(runtimeDefinitions[i]);

            return true;
        }

        public static List<QuestData> BuildRuntimeQuestDefinitions()
        {
            return new List<QuestData>
            {
                DefaultGameContent.CreateIntroQuest(),
                BuildScoutQuest(),
                BuildBossQuest(),
                BuildReturnQuest(),
                BuildChapterTwoSignalQuest(),
                BuildChapterTwoArchiveQuest(),
                BuildChapterTwoRivalQuest(),
                BuildChapterTwoReturnQuest(),
                BuildChapterThreeBeaconQuest(),
                BuildChapterThreeDeltaQuest(),
                BuildChapterThreeRidgeQuest(),
                BuildChapterThreeSpireQuest(),
                BuildChapterThreeReturnQuest(),
                BuildCollectorQuest(),
                BuildMentorQuest(),
                BuildRumorQuest(),
                BuildPhaseTwoWiderMapQuest(),
                BuildPhaseTwoRoadsQuest(),
                BuildPhaseTwoMoonwellQuest(),
                BuildPhaseTwoQuarryQuest(),
                BuildPhaseTwoHollowSignalQuest(),
                BuildPhaseTwoBindingChoiceQuest(),
                BuildJessaLandmarksQuest(),
                BuildLumaBondsQuest(),
                BuildSableRematchQuest()
            };
        }

        public static bool TryGetDuplicateRuntimeQuestId(out string duplicateQuestId)
        {
            duplicateQuestId = string.Empty;
            var seen = new HashSet<string>();
            var definitions = BuildRuntimeQuestDefinitions();
            for (var i = 0; i < definitions.Count; i++)
            {
                var id = definitions[i] != null ? definitions[i].QuestId : string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (seen.Add(id))
                    continue;
                duplicateQuestId = id;
                return true;
            }

            return false;
        }

        static QuestData BuildScoutQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterOneIds.ScoutQuest, "Eastern Warnings", "Reach the route and speak with Scout Rin.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterOneIds.VisitRoute,
                        description = "Reach the eastern route",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterOneIds.TalkScout,
                        description = "Speak with Scout Rin",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildBossQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterOneIds.BossQuest, "Briar Warden", "Push into the forest and resolve Iona's challenge by force, mercy, or withdrawal.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterOneIds.VisitForest,
                        description = "Enter the deep forest",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterOneIds.DefeatBoss,
                        description = "Resolve Iona's challenge",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildReturnQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterOneIds.ReturnQuest, "Return To Hollowfen", "Report back to Elder Mira.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterOneIds.ReturnToElder,
                        description = "Return to Elder Mira",
                        requiredCount = 1
                    }
                });
            q.SetGearRewards(DefaultGameContent.GearOutfitScholarCoatId);
            return q;
        }

        static QuestData BuildChapterTwoSignalQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterTwoIds.SignalQuest, "Lantern Signal", "Speak with Elder Mira, then investigate the beacon beyond the grove.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.TalkElder,
                        description = "Speak with Elder Mira about the marsh signal",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.VisitMarsh,
                        description = "Reach Lantern Marsh",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterTwoArchiveQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterTwoIds.ArchiveQuest, "Echoes In Stone", "Find Archivist Sel and reach the Sunken Archive.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.TalkArchivist,
                        description = "Speak with Archivist Sel",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.VisitRuins,
                        description = "Enter the Sunken Archive",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterTwoRivalQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterTwoIds.RivalQuest, "Rival At The Archive", "Resolve Corin's archive gamble before the relic is forced open.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.DefeatRival,
                        description = "Resolve Corin in the archive",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterTwoReturnQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterTwoIds.ReturnQuest, "Words For Hollowfen", "Bring Sel's warning and the archive news back to Elder Mira.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterTwoIds.ReturnToElder,
                        description = "Return to Elder Mira",
                        requiredCount = 1
                    }
                });
            q.SetGearRewards(DefaultGameContent.GearOutfitForagerGreensId);
            return q;
        }

        static QuestData BuildChapterThreeBeaconQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.BeaconQuest, "Storm Beyond The Archive", "Speak with Mira, then push beyond the ruins into the Flooded Delta.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkElder,
                        description = "Speak with Elder Mira about the spire storm",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.VisitDelta,
                        description = "Reach the Flooded Delta",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterThreeDeltaQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.DeltaQuest, "Delta Under Glass", "Find Warden Neris and secure the road toward Stormbreak Ridge.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkWarden,
                        description = "Speak with Warden Neris",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.VisitRidge,
                        description = "Reach Stormbreak Ridge",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterThreeRidgeQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.RidgeQuest, "Lessons Of Stormbreak", "Train with Cael on the ridge, then earn passage to the spire.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkMentor,
                        description = "Speak with Mentor Cael",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.WinRidgeBattles,
                        description = "Win battles on Stormbreak Ridge",
                        requiredCount = 2
                    }
                });
            return q;
        }

        static QuestData BuildChapterThreeSpireQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.SpireQuest, "Skyglass Reckoning", "Climb into Skyglass Spire and resolve Varo's storm gambit before it spreads west.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.VisitSpire,
                        description = "Reach Skyglass Spire",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.DefeatSpireBoss,
                        description = "Resolve Varo at Skyglass Spire",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildChapterThreeReturnQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.ReturnQuest, "After The Storm", "Bring news from the Skyglass Spire back to Elder Mira.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.ReturnToElder,
                        description = "Return to Elder Mira",
                        requiredCount = 1
                    }
                });
            q.SetGearRewards(DefaultGameContent.GearOutfitRoyalMantleId);
            return q;
        }

        static QuestData BuildCollectorQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.CollectorQuest, "Delta Specimens", "Help Veya catalog the rare creatures being pushed out by the stormline.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkCollector,
                        description = "Speak with Veya in the delta",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.CatchDeltaMonsters,
                        description = "Capture monsters beyond the archive",
                        requiredCount = 2
                    }
                });
            return q;
        }

        static QuestData BuildMentorQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.MentorQuest, "Long Campaign Lessons", "Train under Cael and evolve one team member before the spire assault.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkMentor,
                        description = "Study with Mentor Cael",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = "evolve_monster",
                        description = "Evolve one monster for the climb",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildRumorQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(ChapterThreeIds.RumorQuest, "Voices On The Floodwind", "Collect Iris's leads and verify them out in the growing storm frontier.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.TalkRumorKeeper,
                        description = "Speak with Iris about local rumors",
                        requiredCount = 1
                    },
                    new QuestObjective
                    {
                        objectiveId = ChapterThreeIds.VisitRidge,
                        description = "Reach Stormbreak Ridge",
                        requiredCount = 1
                    }
                });
            return q;
        }

        static QuestData BuildPhaseTwoWiderMapQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.WiderMapQuest, "The Wider Map", "Meet Jessa Vale and discover the Stonewake crossroads.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.DiscoverStonewake, description = "Discover Stonewake Hamlet", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkCartographer, description = "Speak with Jessa Vale", requiredCount = 1 }
                });
            return q;
        }

        static QuestData BuildPhaseTwoRoadsQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.RoadsQuest, "Roads Reopened", "Work with Nia Reed to reopen the northern route.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkRunner, description = "Speak with Nia Reed in the marsh basin", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.ClearRoadHazards, description = "Clear road hazards", requiredCount = 2 }
                });
            return q;
        }

        static QuestData BuildPhaseTwoMoonwellQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.MoonwellQuest, "The Moonwell Oath", "Investigate the sanctuary where bonds change monster behavior.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.VisitMoonwell, description = "Reach Moonwell Grove", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkMoonwellKeeper, description = "Speak with Luma", requiredCount = 1 }
                });
            return q;
        }

        static QuestData BuildPhaseTwoQuarryQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.QuarryQuest, "Quarry Tremors", "Help Orlo Flint prove what is waking Ironroot Quarry.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.VisitQuarry, description = "Reach Ironroot Quarry", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkForeman, description = "Speak with Orlo Flint", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.WinQuarryBattles, description = "Win battles in the quarry", requiredCount = 2 }
                });
            return q;
        }

        static QuestData BuildPhaseTwoHollowSignalQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.HollowSignalQuest, "The Hollow Signal", "Follow the archive echoes to Starfall Hollow.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.VisitStarfall, description = "Reach Starfall Hollow", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkEthicist, description = "Speak with Thren", requiredCount = 1 }
                });
            return q;
        }

        static QuestData BuildPhaseTwoBindingChoiceQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.BindingChoiceQuest, "Binding Choice", "Choose a guiding advisor, resolve Sable, then decide how Hollowfen answers the lore network.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.DefeatSable, description = "Resolve Sable at Tideglass Crossing", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.ReturnToMira, description = "Return to Elder Mira", requiredCount = 1 }
                });
            return q;
        }

        static QuestData BuildJessaLandmarksQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.JessaLandmarksQuest, "Landmarks, Not Guesses",
                "Help Jessa confirm the new northern map with real landmarks.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.JessaEscortStonewake, description = "Walk Jessa through Stonewake's survey loop", requiredCount = 25 },
                    new QuestObjective { objectiveId = PhaseTwoIds.JessaVisitMoonwell, description = "Mark Moonwell Grove", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.JessaVisitQuarry, description = "Mark Ironroot Quarry", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.JessaVisitStarfall, description = "Mark Starfall Hollow", requiredCount = 1 }
                });
            return q;
        }

        static QuestData BuildLumaBondsQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.LumaBondsQuest, "Moonwell Proof",
                "Show Luma that strong bonds still change monsters without the archive network forcing them.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.TalkMoonwellKeeper, description = "Speak with Luma at the Moonwell", requiredCount = 1 },
                    new QuestObjective { objectiveId = PhaseTwoIds.LumaEvolveMonsters, description = "Bring two evolved monsters to the Moonwell", requiredCount = 2 }
                });
            return q;
        }

        static QuestData BuildSableRematchQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            q.Configure(PhaseTwoIds.SableRematchQuest, "Crossing Rematch",
                "Resolve Sable's challenge by talking her down or beating her in a rematch.",
                new[]
                {
                    new QuestObjective { objectiveId = PhaseTwoIds.SableBattleOutcome, description = "Resolve Sable's challenge", requiredCount = 1 }
                });
            return q;
        }
    }
}

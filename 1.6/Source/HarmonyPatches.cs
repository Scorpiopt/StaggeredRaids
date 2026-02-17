using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace StaggeredRaids
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class IncidentWorker_Raid_TryExecuteWorker_Patch
    {
        public static bool Prefix(IncidentWorker_Raid __instance, IncidentParms parms, ref bool __result)
        {
            if (StaggeredRaidsUtility.processingParms.Contains(parms))
            {
                return true;
            }

            if (parms.target is Map map)
            {
                __instance.ResolveRaidPoints(parms);
                if (!__instance.TryResolveRaidFaction(parms))
                {
                    return true;
                }
                PawnGroupKindDef groupKind = parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat;
                PawnGroupMaker groupMaker = parms.faction.def.pawnGroupMakers.FirstOrDefault(
                    x => x.kindDef == PawnGroupKindDefOf.Combat);
                if (__instance is IncidentWorker_ShamblerAssault)
                {
                    groupMaker = parms.faction.def.pawnGroupMakers.FirstOrDefault(
                    x => x.kindDef == PawnGroupKindDefOf.Shamblers);
                }
                if (groupMaker == null)
                {
                    return true;
                }

                float totalCost = 0f;
                int optionCount = 0;
                foreach (PawnGenOption option in groupMaker.options)
                {
                    totalCost += option.Cost;
                    optionCount++;
                }
                float averageCost = totalCost / optionCount;
                int numRaiders = Mathf.RoundToInt(parms.points / averageCost);

                if (numRaiders > StaggeredRaidsUtility.MaxRaidersPerWave)
                {
                    StaggeredRaidsUtility.AddRaidWaves(map, __instance.def, parms, numRaiders, parms.raidArrivalMode, parms.raidStrategy);
                    __result = StaggeredRaidsUtility.ExecuteFirstWave(map);
                    return false;
                }

            }
            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_EntitySwarm), "TryExecuteWorker")]
    public static class IncidentWorker_EntitySwarm_TryExecuteWorker_Patch
    {
        public static void Prefix(IncidentWorker_EntitySwarm __instance, ref IncidentParms parms)
        {
            if (!ModsConfig.AnomalyActive || !(parms.target is Map map) ||
                __instance is IncidentWorker_ShamblerSwarmSmall)
                return;
            PawnGroupKindDef groupKindDef = __instance.GroupKindDef;
            FloatRange swarmSizeVariance = __instance.SwarmSizeVariance;
            float adjustedPoints = parms.points * swarmSizeVariance.RandomInRange;
            adjustedPoints = Mathf.Max(adjustedPoints,
                Faction.OfEntities.def.MinPointsToGeneratePawnGroup(groupKindDef) * 1.05f);
            PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms
            {
                groupKind = groupKindDef,
                tile = map.Tile,
                faction = Faction.OfEntities,
                points = adjustedPoints
            };
            int estimatedEntities = PawnGroupMakerUtility.GeneratePawnKindsExample(pawnGroupMakerParms).Count();
            if (estimatedEntities > StaggeredRaidsUtility.MaxRaidersPerWave)
            {
                int numWaves = Mathf.CeilToInt((float)estimatedEntities / StaggeredRaidsUtility.MaxRaidersPerWave);
                float pointsPerWave = adjustedPoints / numWaves;
                parms.points = pointsPerWave;
                for (int i = 1; i < numWaves; i++)
                {
                    int delay = i * StaggeredRaidsUtility.TicksBetweenWaves;
                    IncidentParms waveParms = new IncidentParms
                    {
                        target = map,
                        points = pointsPerWave,
                        faction = parms.faction,
                        forced = true,
                        questTag = parms.questTag
                    };
                    if (!StaggeredRaidsUtility.pendingRaidWaves.ContainsKey(map))
                    {
                        StaggeredRaidsUtility.pendingRaidWaves[map] = new RaidGroup();
                    }

                    StaggeredRaidsUtility.pendingRaidWaves[map].waves.Add(new RaidWaveInfo(__instance.def, waveParms, delay, parms.raidArrivalMode, parms.raidStrategy));
                }
                if (numWaves > 1)
                {
                    StaggeredRaidsUtility.ShowEntitySwarmNotification(map, numWaves);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Map), "MapPostTick")]
    public static class Map_MapPostTick_Patch
    {
        public static void Postfix(Map __instance)
        {
            StaggeredRaidsUtility.ProcessRaidWaves(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), "ExposeData")]
    public static class Map_ExposeData_Patch
    {
        public static void Postfix()
        {
            StaggeredRaidsUtility.ExposeData();
        }
    }
}

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
            if (parms.target is Map map)
            {
                if (parms.faction is null && __instance.TryResolveRaidFaction(parms) is false)
                {
                    Log.Error("Could not resolve raid faction.");
                    return true;
                }

                __instance.ResolveRaidPoints(parms);
                if (!__instance.TryResolveRaidFaction(parms))
                {
                    return true;
                }
                PawnGroupKindDef groupKind = parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat;
                __instance.ResolveRaidStrategy(parms, groupKind);
                __instance.ResolveRaidArriveMode(parms);
                __instance.ResolveRaidAgeRestriction(parms);
                if (!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
                {
                    return true;
                }
                parms.points = IncidentWorker_Raid.AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, groupKind, parms.raidAgeRestriction);

                PawnGroupMaker groupMaker = parms.faction.def.pawnGroupMakers.FirstOrDefault(
                    x => x.kindDef == PawnGroupKindDefOf.Combat);

                if (groupMaker == null)
                    return true;

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
                    StaggeredRaidsUtility.AddRaidWaves(map, __instance.def, parms, numRaiders);
                    __result = StaggeredRaidsUtility.ExecuteFirstWave(map);
                    return false;
                }
            }

            return true;
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

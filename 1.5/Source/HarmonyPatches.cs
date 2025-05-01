using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace StaggeredRaids
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class IncidentWorker_RaidEnemy_TryExecuteWorker_Patch
    {
        public static bool Prefix(IncidentWorker_RaidEnemy __instance, IncidentParms parms, ref bool __result)
        {
            if (parms.target is Map map)
            {
                PawnGroupMaker groupMaker = parms.faction.def.pawnGroupMakers.FirstOrDefault(
                    x => x.kindDef == PawnGroupKindDefOf.Combat);

                if (groupMaker == null)
                    return true;

                float points = parms.points;
                float totalCost = 0f;
                int optionCount = 0;
                foreach (PawnGenOption option in groupMaker.options)
                {
                    totalCost += option.Cost;
                    optionCount++;
                }
                float averageCost = totalCost / optionCount;
                int numRaiders = Mathf.RoundToInt(points / averageCost);
                if (numRaiders > StaggeredRaidsUtility.MaxRaidersPerWave)
                {
                    StaggeredRaidsUtility.AddRaidWaves(map, parms, numRaiders);
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

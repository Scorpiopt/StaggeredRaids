using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StaggeredRaids
{
    public static class StaggeredRaidsUtility
    {
        public static Dictionary<Map, RaidGroup> pendingRaidWaves = new Dictionary<Map, RaidGroup>();
        public static int TicksBetweenWaves => (int)(StaggeredRaidsMod.settings.hoursBetweenWaves * GenDate.TicksPerHour);
        public static int MaxRaidersPerWave => StaggeredRaidsMod.settings.maxRaidersPerWave;
        public static HashSet<IncidentParms> processingParms = new HashSet<IncidentParms>();

        public static void AddRaidWaves(Map map, IncidentDef def, IncidentParms originalParms, int totalRaiders, PawnsArrivalModeDef arrivalMode, RaidStrategyDef raidStrategy)
        {
            if (!pendingRaidWaves.ContainsKey(map))
            {
                pendingRaidWaves[map] = new RaidGroup();
            }
            int numWaves = (int)System.Math.Ceiling((float)totalRaiders / MaxRaidersPerWave);
            for (int i = 0; i < numWaves; i++)
            {
                int remainingRaiders = totalRaiders - (i * MaxRaidersPerWave);
                int waveSize = System.Math.Min(MaxRaidersPerWave, remainingRaiders);
                IncidentParms waveParms = new IncidentParms
                {
                    target = originalParms.target,
                    faction = originalParms.faction,
                    raidStrategy = raidStrategy,
                    raidArrivalMode = arrivalMode,
                    forced = originalParms.forced,
                    points = originalParms.points * ((float)waveSize / totalRaiders)
                };
                int delay = i * TicksBetweenWaves;
                pendingRaidWaves[map].waves.Add(new RaidWaveInfo(def, waveParms, delay, arrivalMode, raidStrategy));
            }
            ShowRaidSplitNotification(map, numWaves);
        }

        private static void ShowRaidSplitNotification(Map map, int numWaves)
        {
            if (!pendingRaidWaves.ContainsKey(map))
                return;

            RaidGroup raidGroup = pendingRaidWaves[map];
            if (!raidGroup.initialNotificationShown)
            {
                raidGroup.initialNotificationShown = true;
                if (map.IsPlayerHome)
                {
                    Messages.Message("StaggeredRaids.RaidSplitNotification".Translate(),
                        MessageTypeDefOf.ThreatBig, false);
                }
            }
        }

        public static void ShowEntitySwarmNotification(Map map, int numWaves)
        {
            if (!pendingRaidWaves.ContainsKey(map))
                return;

            RaidGroup raidGroup = pendingRaidWaves[map];
            if (!raidGroup.initialNotificationShown)
            {
                raidGroup.initialNotificationShown = true;
                if (map.IsPlayerHome)
                {
                    Messages.Message("StaggeredRaids.RaidSplitNotification".Translate(),
                        MessageTypeDefOf.ThreatBig, false);
                }
            }
        }

        public static void ProcessRaidWaves(Map map)
        {
            pendingRaidWaves ??= new Dictionary<Map, RaidGroup>();
            if (!pendingRaidWaves.ContainsKey(map) || pendingRaidWaves[map].waves.Count == 0)
                return;

            List<RaidWaveInfo> waves = pendingRaidWaves[map].waves;
            RaidWaveInfo nextWave = null;
            int nextWaveIndex = -1;

            for (int i = 0; i < waves.Count; i++)
            {
                RaidWaveInfo wave = waves[i];
                wave.ticksUntilNextWave--;
                if (wave.ticksUntilNextWave <= 0 && (nextWave == null || wave.ticksUntilNextWave < nextWave.ticksUntilNextWave))
                {
                    nextWave = wave;
                    nextWaveIndex = i;
                }
            }
            if (nextWave != null)
            {
                ExecuteRaidWave(nextWave);
                waves.RemoveAt(nextWaveIndex);
                if (waves.Count == 0)
                {
                    pendingRaidWaves.Remove(map);
                }
            }
        }

        private static void ExecuteRaidWave(RaidWaveInfo wave)
        {
            if (wave.arrivalMode != null)
            {
                wave.parms.raidArrivalMode = wave.arrivalMode;
            }
            if (wave.raidStrategy != null)
            {
                wave.parms.raidStrategy = wave.raidStrategy;
            }
            processingParms.Add(wave.parms);
            try
            {
                wave.def.Worker.TryExecute(wave.parms);
            }
            finally
            {
                processingParms.Remove(wave.parms);
            }
        }

        public static bool ExecuteFirstWave(Map map)
        {
            if (!pendingRaidWaves.ContainsKey(map) || pendingRaidWaves[map].waves.Count == 0)
                return false;

            List<RaidWaveInfo> waves = pendingRaidWaves[map].waves;
            for (int i = 0; i < waves.Count; i++)
            {
                RaidWaveInfo wave = waves[i];
                if (wave.ticksUntilNextWave == 0)
                {
                    processingParms.Add(wave.parms);
                    bool result;
                    try
                    {
                        result = wave.def.Worker.TryExecute(wave.parms);
                    }
                    finally
                    {
                        processingParms.Remove(wave.parms);
                    }
                    PawnsArrivalModeDef arrivalMode = wave.parms.raidArrivalMode;
                    RaidStrategyDef raidStrategy = wave.parms.raidStrategy;                    
                    for (int j = 0; j < waves.Count; j++)
                    {
                        if (j != i && waves[j].parms.faction == wave.parms.faction)
                        {
                            waves[j].arrivalMode = arrivalMode;
                            waves[j].raidStrategy = raidStrategy;
                        }
                    }

                    waves.RemoveAt(i);
                    return result;
                }
            }

            return false;
        }

        public static void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<Map> mapsToRemove = new List<Map>();
                foreach (KeyValuePair<Map, RaidGroup> kvp in pendingRaidWaves)
                {
                    if (kvp.Key == null || kvp.Value == null || kvp.Value.waves.Count == 0)
                    {
                        mapsToRemove.Add(kvp.Key);
                    }
                }

                foreach (Map map in mapsToRemove)
                {
                    pendingRaidWaves.Remove(map);
                }
            }
            mapKeys = mapKeys ?? new List<Map>();
            raidGroupValues = raidGroupValues ?? new List<RaidGroup>();

            Scribe_Collections.Look(ref pendingRaidWaves, "pendingRaidWaves", LookMode.Reference, LookMode.Deep, ref mapKeys, ref raidGroupValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pendingRaidWaves ??= new Dictionary<Map, RaidGroup>();
            }
        }

        private static List<Map> mapKeys;
        private static List<RaidGroup> raidGroupValues;
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StaggeredRaids
{
    public static class StaggeredRaidsUtility
    {
        public static Dictionary<Map, RaidGroup> pendingRaidWaves = new Dictionary<Map, RaidGroup>();
        public const int TicksBetweenWaves = 3 * GenDate.TicksPerHour;
        public const int MaxRaidersPerWave = 25;
        public const int RaidSplitThreshold = 50;

        public static void AddRaidWaves(Map map, IncidentParms originalParms, int totalRaiders)
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
                    raidStrategy = originalParms.raidStrategy,
                    raidArrivalMode = originalParms.raidArrivalMode,
                    forced = originalParms.forced,
                    points = originalParms.points * ((float)waveSize / totalRaiders)
                };
                int delay = i * TicksBetweenWaves;

                pendingRaidWaves[map].waves.Add(new RaidWaveInfo(waveParms, delay));
            }
        }

        public static void ProcessRaidWaves(Map map)
        {
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
            }
        }

        private static void ExecuteRaidWave(RaidWaveInfo wave)
        {
            IncidentDefOf.RaidEnemy.Worker.TryExecute(wave.parms);
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
                    bool result = IncidentDefOf.RaidEnemy.Worker.TryExecute(wave.parms);
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
        }

        private static List<Map> mapKeys;
        private static List<RaidGroup> raidGroupValues;
    }
}

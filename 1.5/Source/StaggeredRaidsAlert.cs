using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StaggeredRaids
{
    public class Alert_StaggeredRaids : Alert
    {
        private static Alert_StaggeredRaids instance;

        public static Alert_StaggeredRaids Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Alert_StaggeredRaids();
                }
                return instance;
            }
        }

        public Alert_StaggeredRaids()
        {
            defaultLabel = "StaggeredRaids.RaidWavesAlert".Translate();
            defaultExplanation = "StaggeredRaids.RaidWavesAlertDesc".Translate();
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            if (StaggeredRaidsUtility.pendingRaidWaves == null)
                return AlertReport.Inactive;

            Map playerMap = Find.CurrentMap;
            if (playerMap == null)
                return AlertReport.Inactive;
            foreach (var kvp in StaggeredRaidsUtility.pendingRaidWaves)
            {
                Map map = kvp.Key;
                RaidGroup raidGroup = kvp.Value;

                if (map != null && map.IsPlayerHome && raidGroup != null && raidGroup.waves.Count > 0)
                {
                    int remainingWaves = raidGroup.waves.Count;
                    defaultLabel = "StaggeredRaids.RaidWavesAlert".Translate(remainingWaves);
                    return AlertReport.Active;
                }
            }

            return AlertReport.Inactive;
        }
    }
    public static class StaggeredRaidsAlertManager
    {
        private static bool alertRegistered = false;

        public static void EnsureAlertRegistered()
        {
            if (!alertRegistered && Find.UIRoot != null)
            {
                try
                {
                    var uiRoot = Find.UIRoot;
                    var alertsReadoutField = uiRoot.GetType().GetField("alertsReadout",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (alertsReadoutField != null)
                    {
                        var alertsReadout = alertsReadoutField.GetValue(uiRoot);
                        var alertsField = alertsReadout.GetType().GetField("alerts",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (alertsField != null)
                        {
                            var alertsList = (List<Alert>)alertsField.GetValue(alertsReadout);
                            if (alertsList != null && !alertsList.Any(a => a is Alert_StaggeredRaids))
                            {
                                alertsList.Add(Alert_StaggeredRaids.Instance);
                                alertRegistered = true;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"StaggeredRaids: Failed to register alert: {ex.Message}");
                }
            }
        }
    }
}

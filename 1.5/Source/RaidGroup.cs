using System.Collections.Generic;
using Verse;

namespace StaggeredRaids
{
    public class RaidGroup : IExposable
    {
        public List<RaidWaveInfo> waves = new List<RaidWaveInfo>();
        public bool initialNotificationShown = false;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref waves, "waves", LookMode.Deep);
            Scribe_Values.Look(ref initialNotificationShown, "initialNotificationShown", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                waves ??= new List<RaidWaveInfo>();
            }
        }
    }
}

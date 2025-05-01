using System.Collections.Generic;
using Verse;

namespace StaggeredRaids
{
    public class RaidGroup : IExposable
    {
        public List<RaidWaveInfo> waves = new List<RaidWaveInfo>();
        
        public RaidGroup() { }
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref waves, "waves", LookMode.Deep);
        }
    }
}

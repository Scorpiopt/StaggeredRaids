using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StaggeredRaids
{
    public class RaidWaveInfo : IExposable
    {
        public IncidentParms parms;
        public int ticksUntilNextWave;
        
        public RaidWaveInfo() { }
        
        public RaidWaveInfo(IncidentParms parms, int ticksUntilNextWave)
        {
            this.parms = parms;
            this.ticksUntilNextWave = ticksUntilNextWave;
        }
        
        public void ExposeData()
        {
            Scribe_Deep.Look(ref parms, "parms");
            Scribe_Values.Look(ref ticksUntilNextWave, "ticksUntilNextWave");
        }
    }
}
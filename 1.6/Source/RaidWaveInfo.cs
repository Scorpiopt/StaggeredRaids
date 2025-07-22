using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StaggeredRaids
{
    public class RaidWaveInfo : IExposable
    {
        public IncidentParms parms;
        public int ticksUntilNextWave;
        public IncidentDef def;

        public RaidWaveInfo() { }
        
        public RaidWaveInfo(IncidentDef def, IncidentParms parms, int ticksUntilNextWave)
        {
            this.def = def;
            this.parms = parms;
            this.ticksUntilNextWave = ticksUntilNextWave;
        }
        
        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Deep.Look(ref parms, "parms");
            Scribe_Values.Look(ref ticksUntilNextWave, "ticksUntilNextWave");
        }
    }
}

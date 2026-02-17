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
        public PawnsArrivalModeDef arrivalMode;
        public RaidStrategyDef raidStrategy;

        public RaidWaveInfo() { }
        
        public RaidWaveInfo(IncidentDef def, IncidentParms parms, int ticksUntilNextWave, PawnsArrivalModeDef arrivalMode, RaidStrategyDef raidStrategy)
        {
            this.def = def;
            this.parms = parms;
            this.ticksUntilNextWave = ticksUntilNextWave;
            this.arrivalMode = arrivalMode;
            this.raidStrategy = raidStrategy;
        }
        
        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Deep.Look(ref parms, "parms");
            Scribe_Values.Look(ref ticksUntilNextWave, "ticksUntilNextWave");
            Scribe_Defs.Look(ref arrivalMode, "arrivalMode");
            Scribe_Defs.Look(ref raidStrategy, "raidStrategy");
        }
    }
}

using HarmonyLib;
using Verse;

namespace StaggeredRaids
{
    public class StaggeredRaidsMod : Mod
    {
        public StaggeredRaidsMod(ModContentPack pack) : base(pack)
        {
            new Harmony("StaggeredRaidsMod").PatchAll();
        }
    }
}
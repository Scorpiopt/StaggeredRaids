using UnityEngine;
using Verse;

namespace StaggeredRaids
{
    public class StaggeredRaidsSettings : ModSettings
    {
        public int maxRaidersPerWave = 50;
        public float hoursBetweenWaves = 3f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxRaidersPerWave, "maxRaidersPerWave", 50);
            Scribe_Values.Look(ref hoursBetweenWaves, "hoursBetweenWaves", 3f);
        }
    }

    public class StaggeredRaidsMod : Mod
    {
        public static StaggeredRaidsSettings settings;

        public StaggeredRaidsMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<StaggeredRaidsSettings>();
            new HarmonyLib.Harmony("StaggeredRaidsMod").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "Staggered Raids";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            settings.maxRaidersPerWave = (int)Widgets.HorizontalSlider(
                listing.GetRect(28f),
                settings.maxRaidersPerWave,
                10, 100,
                false,
                "Maximum raiders per wave: " + settings.maxRaidersPerWave,
                "10", "100");

            listing.Gap(12f);
            string hoursLabel = "Hours between waves: " + settings.hoursBetweenWaves.ToString("0.0");
            settings.hoursBetweenWaves = Widgets.HorizontalSlider(
                listing.GetRect(28f),
                settings.hoursBetweenWaves,
                0.5f, 12f,
                false,
                hoursLabel,
                "0.5", "12.0");

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}

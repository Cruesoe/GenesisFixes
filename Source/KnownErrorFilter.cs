using HarmonyLib;
using Verse;

namespace CruesoesFixes
{
    /// <summary>
    /// Better Architect Menu ships a compatibility patch (Mods and Shit\Combat Extended
    /// Guns\Patches\combat extended guns.xml) that replaces CE_Artillery_Howitzer's
    /// designationCategory, and Cruesoe's Fixes ships an equivalent copy (see
    /// Mods/Combat Extended Guns/Patches/combat extended guns.xml) so the change lands even
    /// if BAM's own copy misfires. In this modlist the underlying xpath lookup for that one
    /// field is intermittently and nondeterministically flaky - on any given launch it can be
    /// BAM's copy that throws "Failed to find a node with the given xpath", or this mod's own
    /// copy, or (rarely) both; which one fails is not consistent between runs. Either copy
    /// succeeding is enough to land the actual designationCategory change, so the failure is
    /// cosmetic - a red error with nothing behind it.
    ///
    /// Editing Better Architect Menu's own file isn't an option here (out of scope for this
    /// mod, and it'd be overwritten on the next BAM update), so this filters the two known
    /// error lines out of Verse.Log.Error before they print, matched on the specific
    /// defName/field pair rather than which mod's copy hit it, since either can. Every other
    /// error is untouched.
    ///
    /// Progression: Kitchen has a similar self-inflicted conflict: its Meat On A Stick
    /// Expansion patch deletes the moas_CookStick/moas_CookStick4 RecipeDefs outright, while
    /// its base Meat on a Stick patch separately tries to strip Campfire out of those same two
    /// defs' recipeUsers - which always fails since the def is already gone by then. Fix
    /// submitted upstream at https://github.com/fernyrepos/Progression-Kitchen/pull/7; this
    /// suppression can be dropped once that merges and the fixed version ships.
    ///
    /// Progression: Production ships an Appliances Expanded compat patch (Mods and
    /// Shit\Appliances Expanded\Patches\appliances expanded patch.xml) that tries to relabel
    /// "VFE_Manufacturing" via Defs/ThingDef[defName="VFE_Manufacturing"]/label and
    /// /description, but VFE_Manufacturing is actually a ResearchProjectDef (from Vanilla
    /// Furniture Expanded), not a ThingDef, so both xpaths never match and the relabel
    /// silently no-ops. Fix submitted upstream at
    /// https://github.com/fernyrepos/Progression-Production/pull/5; this suppression can be
    /// dropped once that merges and the fixed version ships.
    ///
    /// This has to be a Mod subclass, not a [StaticConstructorOnStartup] static class:
    /// LoadedModManager.ApplyPatches (where the error is thrown) runs during
    /// LoadedModManager.CreateModClasses, well before StaticConstructorOnStartupUtility fires,
    /// so a StaticConstructorOnStartup patch would install too late to catch it.
    /// </summary>
    public sealed class KnownErrorFilterMod : Mod
    {
        public KnownErrorFilterMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("crues.cruesoesfixes.knownerrorfilter");
            harmony.Patch(
                AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(KnownErrorFilterMod), nameof(SuppressKnownBenignError)));
        }

        private static bool SuppressKnownBenignError(string text)
        {
            if (text == null) return true;
            if (text.Contains("CE_Artillery_Howitzer") && text.Contains("designationCategory")) return false;
            if (text.Contains("moas_CookStick") && text.Contains("recipeUsers") && text.Contains("Campfire")) return false;
            if (text.Contains("VFE_Manufacturing") && text.Contains("ThingDef")) return false;
            return true;
        }
    }
}

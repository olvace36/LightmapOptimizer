using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace LightmapOptimizer
{
    public class ModEntry : Mod
    {
        // Vanilla formula (Game1.allocateLightmap):
        //   lightmapWidth  = (screenWidth  / zoom + 64) / (lightingQuality / 2)
        //   lightmapHeight = (screenHeight / zoom + 64) / (lightingQuality / 2)
        //
        // Vanilla hardcodes lightingQuality to 8 (the in-game option to change this was
        // removed; Options.lightingQuality is now a getter that always returns 8 — see
        // StardewValley/Options.cs). Raising it here shrinks the lightmap render target,
        // which is redrawn and blended over the whole screen every single frame.
        //
        //   8  (vanilla) -> divisor 4  -> 100% baseline area
        //   12            -> divisor 6  -> ~44% of baseline area
        //   16            -> divisor 8  -> ~25% of baseline area
        //   24            -> divisor 12 -> ~11% of baseline area
        //
        // Start conservative (12) and raise if it still looks fine. Because the lightmap
        // is later scaled up and alpha-blended, minor resolution loss is usually not very
        // noticeable — it's already a soft glow effect, not sharp detail.
        private const int OverriddenLightingQuality = 12;

        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(Options), nameof(Options.lightingQuality)),
                postfix: new HarmonyMethod(typeof(LightingQualityPatch), nameof(LightingQualityPatch.Postfix))
            );

            this.Monitor.Log($"LightmapOptimizer active: lightingQuality overridden to {OverriddenLightingQuality} (vanilla locked value: 8).", LogLevel.Info);
        }

        internal static class LightingQualityPatch
        {
            internal static void Postfix(ref int __result)
            {
                __result = OverriddenLightingQuality;
            }
        }
    }
}

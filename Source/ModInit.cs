using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TSA.CE.ShootingSkillAffectsWarmup
{
    public class Settings : ModSettings
    {
        // Directional, player-centric toggles (defaults ON)
        public bool protectFromNeutral = true; // neutral -> player/hosted protected
        public bool protectToNeutral = true; // player/hosted -> neutral protected
        public bool protectFromAlly = true; // ally -> player/hosted protected
        public bool protectToAlly = true; // player/hosted -> ally protected

        private const string KeyPrefix = "TSA_CE_SSAW";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref protectFromNeutral, KeyPrefix + "_protectFromNeutral", true);
            Scribe_Values.Look(ref protectToNeutral, KeyPrefix + "_protectToNeutral", true);
            Scribe_Values.Look(ref protectFromAlly, KeyPrefix + "_protectFromAlly", true);
            Scribe_Values.Look(ref protectToAlly, KeyPrefix + "_protectToAlly", true);
            base.ExposeData();
        }

        public void RestoreDefaults()
        {
            protectFromNeutral = true;
            protectToNeutral = true;
            protectFromAlly = true;
            protectToAlly = true;
        }
    }

    public class ShootingSkillAffectsWarmupMod : Mod
    {
        public static Settings Settings;

        public ShootingSkillAffectsWarmupMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
            var harmony = new Harmony("TSA.CE.ShootingSkillAffectsWarmup");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "No Friendly Fire";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            DrawResetButton(ls, () => Settings.RestoreDefaults());

            DrawCheckbox(ls, "Deactivate friendly fire from neutral factions (protect your pawns from neutral fire)",
                ref Settings.protectFromNeutral,
                "When enabled, fire from neutral factions toward your pawns (or hosted pawns) is blocked.",
                defaultValue: true);
            DrawCheckbox(ls, "Deactivate friendly fire to neutral factions (protect neutrals from your fire)",
                ref Settings.protectToNeutral,
                "When enabled, fire from your pawns (or hosted pawns) toward neutrals is blocked.",
                defaultValue: true);

            ls.Gap(6f);

            DrawCheckbox(ls, "Deactivate friendly fire from allied factions (protect your pawns from ally fire)",
                ref Settings.protectFromAlly,
                "When enabled, fire from allied factions toward your pawns (or hosted pawns) is blocked.",
                defaultValue: true);
            DrawCheckbox(ls, "Deactivate friendly fire to allied factions (protect allies from your fire)",
                ref Settings.protectToAlly,
                "When enabled, fire from your pawns (or hosted pawns) toward allies is blocked.",
                defaultValue: true);

            ls.GapLine();
            ls.Label("Members of the same faction shooting each other is always protected");
            ls.Gap(6f);
            ls.Label("Note: This mod applies to bullets only!");
            ls.End();
        }

        // ----- WD-style helpers (kept in ModInit; no separate library file) -----

        private static void DrawResetButton(Listing_Standard l, Action onReset)
        {
            if (onReset == null) return;
            l.Gap(4f);
            Rect row = l.GetRect(30f);
            float resetW = Mathf.Min(250f, row.width);
            if (resetW < 120f) resetW = Mathf.Max(100f, row.width * 0.35f);
            Rect resetRect = new Rect(row.x, row.y, resetW, row.height);
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (Widgets.ButtonText(resetRect, "Restore defaults")) onReset();
            GUI.color = prev;
            l.Gap(6f);
        }

        private static void DrawCheckbox(Listing_Standard l, string label, ref bool value, string tooltip = null, float? rowHeight = null, bool? defaultValue = null)
        {
            l.Gap(2f);
            float h = rowHeight ?? 24f;
            Rect r = l.GetRect(h);
            if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(r, TooltipWithDefault(tooltip, defaultValue));
            Rect inner = h > 24f ? new Rect(r.x, r.y + (h - 24f) / 2f, r.width, 24f) : r;
            Widgets.CheckboxLabeled(inner, label, ref value);
        }

        private static string TooltipWithDefault(string tooltip, bool? defaultValue)
        {
            if (!defaultValue.HasValue) return tooltip;
            return AppendDefault(tooltip, defaultValue.Value ? "On" : "Off");
        }

        private static string AppendDefault(string tooltip, string def)
        {
            string line = $"Default: {def}";
            return tooltip.NullOrEmpty() ? line : tooltip + "\n" + line;
        }
    }
}

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TSA.CE.ShootingSkillAffectsWarmup.Patches
{
    /// <summary>
    /// Friendly-fire absorption for bullets based purely on true Faction relationships.
    /// Key guarantees:
    ///  - Same-faction (true faction) is ALWAYS protected.
    ///  - If NEITHER side is the player and factions DIFFER, we NEVER absorb (ally vs enemy, neutral vs enemy, enemy vs enemy).
    ///  - Prisoners of colony and aggressive mental states (manhunter/berserk) are NEVER protected.
    ///  - HostFaction is IGNORED entirely to avoid quest/guest edge cases.
    /// Scope: pawns only, DamageDefOf.Bullet only.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    public static class Patch_Pawn_HealthTracker_PreApplyDamage
    {
        // Flip to true to log decisions to the console.
        private const bool DEBUG_NFF = false;

        private static readonly Func<Pawn_HealthTracker, Pawn> GetPawn = BuildPawnGetter();

        public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, ref bool absorbed)
        {
            if (absorbed) { DLog("already absorbed upstream"); return true; }
            if (dinfo.Def != DamageDefOf.Bullet) { DLog("not a bullet"); return true; }

            Pawn victim = GetPawnSafe(__instance);
            if (victim == null) { DLog("no victim"); return true; }

            // Never protect these cases
            if (victim.IsPrisonerOfColony) { DLog("victim is prisoner -> allow damage"); return true; }
            if (victim.InAggroMentalState) { DLog("victim in aggro mental state -> allow damage"); return true; }

            Thing instigator = dinfo.Instigator;
            if (instigator == null) { DLog("no instigator"); return true; }

            // TRUE factions only (ignore HostFaction entirely)
            Faction shooterFac = instigator.Faction;
            Faction victimFac = victim.Faction;

            // If a side lacks a faction -> treat as hostile; never absorb
            if (shooterFac == null || victimFac == null)
            {
                DLog($"factionless side (shooterFac={NameOf(shooterFac)} victimFac={NameOf(victimFac)}) -> allow damage");
                return true;
            }

            // Same-faction: ALWAYS absorb
            if (shooterFac == victimFac)
            {
                absorbed = true;
                DLog($"same-faction {NameOf(shooterFac)} -> ABSORB");
                return false;
            }

            // *** HARD STOPPER ***
            // If neither side is the player and factions differ -> NEVER absorb
            bool shooterIsPlayer = shooterFac == Faction.OfPlayer;
            bool victimIsPlayer = victimFac == Faction.OfPlayer;
            if (!shooterIsPlayer && !victimIsPlayer)
            {
                DLog($"neither side is player, factions differ ({NameOf(shooterFac)} vs {NameOf(victimFac)}) -> allow damage");
                return true;
            }

            // Player-centric directional Ally/Neutral toggles
            var s = TSA.CE.ShootingSkillAffectsWarmup.ShootingSkillAffectsWarmupMod.Settings;
            if (s == null) { DLog("no settings"); return true; }

            if (shooterIsPlayer && !victimIsPlayer)
            {
                var rel = Faction.OfPlayer.RelationKindWith(victimFac);
                if (rel == FactionRelationKind.Ally && s.protectToAlly) { absorbed = true; DLog("player->ally ABSORB"); return false; }
                if (rel == FactionRelationKind.Neutral && s.protectToNeutral) { absorbed = true; DLog("player->neutral ABSORB"); return false; }
                DLog($"player->{rel} toggled off or enemy -> allow damage");
                return true;
            }

            if (victimIsPlayer && !shooterIsPlayer)
            {
                var rel = Faction.OfPlayer.RelationKindWith(shooterFac);
                if (rel == FactionRelationKind.Ally && s.protectFromAlly) { absorbed = true; DLog("ally->player ABSORB"); return false; }
                if (rel == FactionRelationKind.Neutral && s.protectFromNeutral) { absorbed = true; DLog("neutral->player ABSORB"); return false; }
                DLog($"{rel}->player toggled off or enemy -> allow damage");
                return true;
            }

            // Fallback (shouldn't hit)
            DLog("fallback -> allow damage");
            return true;
        }

        // ---------------- helpers ----------------

        private static string NameOf(Faction f) => f?.def?.defName ?? f?.Name ?? "null";

        private static void DLog(string msg)
        {
            if (DEBUG_NFF) Log.Message($"[NFF] {msg}");
        }

        private static Pawn GetPawnSafe(Pawn_HealthTracker h)
        {
            try { return GetPawn?.Invoke(h); } catch { return null; }
        }

        private static Func<Pawn_HealthTracker, Pawn> BuildPawnGetter()
        {
            var propGetter = AccessTools.PropertyGetter(typeof(Pawn_HealthTracker), "Pawn");
            if (propGetter != null)
            {
                try
                {
                    return (Func<Pawn_HealthTracker, Pawn>)Delegate.CreateDelegate(
                        typeof(Func<Pawn_HealthTracker, Pawn>), propGetter);
                }
                catch { /* fall through */ }
            }

            try
            {
                var fieldRef = AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");
                return ht => fieldRef(ht);
            }
            catch
            {
                return _ => null;
            }
        }
    }
}

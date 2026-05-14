using MelonLoader;

namespace SovereignBoons
{
    /// <summary>
    /// Central registry for every MelonPreferences entry. Boon files only ever
    /// read from these properties — they never create their own entries. All
    /// toggles default to false: this is an opt-in power-spike pack.
    ///
    /// Layout convention:
    ///   ----- &lt;Boon Name&gt; (folded from &lt;mod&gt; by &lt;author&gt;) -----
    ///   public static MelonPreferences_Entry&lt;...&gt; Enable&lt;Boon&gt;  { get; private set; } = null!;
    ///   public static MelonPreferences_Entry&lt;...&gt; &lt;Tunable&gt;     { get; private set; } = null!;
    ///
    /// Then in Initialize():
    ///   Enable&lt;Boon&gt; = _root.CreateEntry(...);
    ///   &lt;Tunable&gt;    = _root.CreateEntry(...);
    ///
    /// Each fold also appends a registration block in
    /// KeepClarityIntegration.RegisterEntries() under its bucket
    /// (Economy / Workforce / Buildings / Weather / Combat / Misc).
    /// </summary>
    public static class Config
    {
        private static MelonPreferences_Category _root = null!;

        // ===== Economy bucket =====
        // (entries added per boon — see _research/IMPLEMENTATION_PLAN.md Phase 1)

        // ===== Workforce bucket =====
        // (entries added per boon)

        // ===== Buildings bucket =====
        // (entries added per boon)

        // ===== Weather bucket =====
        // (entries added per boon)

        // ===== Combat bucket =====
        // (entries added per boon)

        // ===== Misc bucket =====
        // (entries added per boon)

        public static void Initialize()
        {
            _root = MelonPreferences.CreateCategory("SovereignBoons", "Sovereign Boons");

            // Master toggle — always present so the user can disable the whole
            // pack from one switch without uninstalling.
            // (re-add when first boon lands; left commented to keep boilerplate
            // out of git history before any feature is folded.)
            //
            // Per-boon entries get added below as boons land. Each fold also
            // registers itself with Keep Clarity via KeepClarityIntegration.

            MelonLogger.Msg("[SovereignBoons] Config initialized (no boons folded yet)");
        }
    }
}

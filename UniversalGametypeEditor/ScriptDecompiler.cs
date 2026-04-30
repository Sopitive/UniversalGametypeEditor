
// ScriptDecompiler.cs
// Decompile Reach Megalo script bitstream into a readable, structured text format.
// Focus: Reach gametypes. This is a first-pass decompiler aimed at correctness of parsing,
// not perfect round-trip high-level syntax reconstruction.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UniversalGametypeEditor.Megalo;

namespace UniversalGametypeEditor
{
    public static class ScriptDecompiler
    {
        // ---- Public API ----------------------------------------------------

        /// <summary>
        /// Decompile a Megalo script from a binary string (sequence of '0'/'1').
        ///
        /// IMPORTANT: Reach script layout (from mpvr + your older reader):
        ///   ConditionCount(10)
        ///   ActionCount(11)
        ///   TriggerCount(8)
        ///   Conditions[ConditionCount]
        ///   Actions[ActionCount]
        ///   Triggers[TriggerCount]
        ///   (then remaining bits for globals/other sections)
        /// </summary>
        public sealed class DecompileResult
        {
            public string Text = string.Empty;
            public int BitsConsumed;
            // Bit offset (within scriptBits) of the boundary between the
            // compiled section (conds + actions + triggers) and the
            // post-trigger tail (stats, MegaloVars, widgets, entries, MP
            // types, forge labels, …). Used by Compile-Script to overwrite
            // ONLY the compiled section while preserving the tail bit-aligned.
            public int TriggerSectionBits;
            public int ConditionCount;
            public int ActionCount;
            public int TriggerCount;
            public List<string>? Labels;
        }

        public static string Decompile(string scriptBits, List<string>? labelNames = null)
            => DecompileDetailed(scriptBits, labelNames).Text;

        /// <summary>
        /// Parse the script header + per-condition / per-action / per-trigger
        /// entries, printing bit positions and read values for each step.
        /// Lets us see exactly which entry desyncs.
        /// </summary>
        /// <summary>
        /// Probe: for each candidate extra-bit width (0..10), re-run the
        /// decoder assuming Unlabelled NumericTypeRef variants carry that
        /// many extra bits of payload. Report how many actions land on a
        /// known (non-None, non-Unlabelled) ID. The right width is the
        /// one that maxes out the "known" count.
        /// </summary>
        public static string ProbeTempNumberWidth(string scriptBits)
        {
            var sb = new StringBuilder();
            for (int extra = 0; extra <= 12; extra++)
            {
                _tempNumberExtraBits = extra;
                var br = new BitReader(scriptBits);
                var ctx = new DecompileContext();
                int cc = br.ReadUInt(10);
                for (int i = 0; i < cc; i++) ParseCondition(br, ctx);
                int ac = br.ReadUInt(11);
                int namedCount = 0;
                string? firstBad = null;
                for (int i = 0; i < ac; i++)
                {
                    int id = br.ReadUInt(7);
                    var def = MegaloLookup.GetAction(id);
                    bool known = def.HasValue
                                 && !def.Value.Name.StartsWith("Unlabelled")
                                 && def.Value.Name != "None";
                    if (known) namedCount++;
                    else if (firstBad == null) firstBad = $"act[{i}]=id{id} ({def?.Name ?? "?"})";
                    if (def.HasValue)
                        foreach (var p in def.Value.Params)
                            ReadParam(br, p.TypeRef, ctx);
                }
                int trig = br.ReadUInt(9);
                sb.AppendLine($"extra={extra,2}  named={namedCount}/{ac}  triggerCount={trig,4}  firstBad={firstBad ?? "(none)"}");
            }
            _tempNumberExtraBits = 4; // restore default
            return sb.ToString();
        }

        // Extra bits to read after the 6-bit NumericTypeRef tag when the
        // tag lands in the Unlabelled range (44-63). These variants ARE
        // the scratch/temporary numbers. Empirically determined via
        // --probe-temp: 4-bit sub-index matches both "all actions are named"
        // and "triggerCount=8" on gametype_testing1_mod.bin (expected 8
        // triggers). The XML author left these variants un-described but
        // the real format has a 4-bit index identical to globalnumbers.
        private static int _tempNumberExtraBits = 4;

        // For each offset shift from -8 to +8 around my current act[0]..act[N]
        // endpoints, check whether the action Type value would land on a
        // known (non-None, non-Unlabelled) ID. If one shift lines up ALL
        // N actions on known IDs, that shift is the true per-action width
        // correction.
        public static string ProbeActionWidths(string scriptBits)
        {
            var sb = new StringBuilder();
            var br0 = new BitReader(scriptBits);
            var ctx = new DecompileContext();
            int cc = br0.ReadUInt(10);
            for (int i = 0; i < cc; i++) ParseCondition(br0, ctx);
            int ac = br0.ReadUInt(11);
            int actStart = br0.Position;
            sb.AppendLine($"actionStart={actStart} actionCount={ac}");

            // Bit reader helper to peek N bits at absolute pos.
            int Peek(int pos, int n)
            {
                int v = 0;
                for (int i = 0; i < n; i++)
                {
                    if (pos + i >= scriptBits.Length) return -1;
                    v = (v << 1) | (scriptBits[pos + i] == '1' ? 1 : 0);
                }
                return v;
            }

            // Walk actions with the current schema-driven reader, but at
            // each act[i] show: current id / id that would be read with
            // ±1..±3 bit shifts. Helps pin the actual width bug.
            var br = new BitReader(scriptBits);
            var ctx2 = new DecompileContext();
            br.ReadUInt(10);
            for (int i = 0; i < cc; i++) ParseCondition(br, ctx2);
            br.ReadUInt(11);

            for (int i = 0; i < ac; i++)
            {
                int pos = br.Position;
                int curId = Peek(pos, 7);
                var def = MegaloLookup.GetAction(curId);
                string curName = def?.Name ?? "?";
                bool curKnown = def.HasValue && !curName.StartsWith("Unlabelled") && curName != "None";

                sb.Append($"act[{i}] @{pos} id={curId} ({curName}) {(curKnown ? "OK" : "BAD")}");
                if (!curKnown)
                {
                    for (int shift = -3; shift <= 3; shift++)
                    {
                        if (shift == 0) continue;
                        int v = Peek(pos + shift, 7);
                        if (v < 0) continue;
                        var sdef = MegaloLookup.GetAction(v);
                        string sn = sdef?.Name ?? "?";
                        bool sk = sdef.HasValue && !sn.StartsWith("Unlabelled") && sn != "None";
                        if (sk)
                            sb.Append($" | shift{(shift > 0 ? "+" : "")}{shift}=id{v}({sn})");
                    }
                }
                sb.AppendLine();

                // advance by whatever the current (possibly wrong) decoder consumes
                int id = br.ReadUInt(7);
                var rdef = MegaloLookup.GetAction(id);
                if (rdef.HasValue)
                    foreach (var p in rdef.Value.Params)
                        ReadParam(br, p.TypeRef, ctx2);
            }
            return sb.ToString();
        }

        public static string TraceParseActions(string scriptBits, int maxEntries = 100)
        {
            var sb = new StringBuilder();
            var br = new BitReader(scriptBits);
            var ctx = new DecompileContext();
            int cc = br.ReadUInt(10);
            for (int i = 0; i < cc; i++)
            {
                br.ReadUInt(5); br.ReadUInt(1); br.ReadUInt(9); br.ReadUInt(10);
                var def = MegaloLookup.GetCondition(i % 32);
            }
            // Re-read correctly: skip all conditions via the real parser
            br = new BitReader(scriptBits);
            ctx = new DecompileContext();
            int condCount = br.ReadUInt(10);
            for (int i = 0; i < condCount; i++) ParseCondition(br, ctx);
            int actCount = br.ReadUInt(11);
            sb.AppendLine($"actionCount={actCount}");
            for (int i = 0; i < actCount && i < maxEntries; i++)
            {
                int start = br.Position;
                int id = br.ReadUInt(7);
                var def = MegaloLookup.GetAction(id);
                sb.Append($"act[{i}] @{start} id={id} ({def?.Name ?? "?"})");
                if (def.HasValue)
                    foreach (var p in def.Value.Params)
                        ReadParam(br, p.TypeRef, ctx);
                sb.AppendLine($" end@{br.Position}");
            }
            int trigCount = br.ReadUInt(9);
            sb.AppendLine($"triggerCount={trigCount}");
            return sb.ToString();
        }

        public static string TraceParse(string scriptBits, int maxEntries = 30)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(scriptBits)) return "(empty)";

            var br = new BitReader(scriptBits);
            var ctx = new DecompileContext();

            int condCount = br.ReadUInt(10);
            sb.AppendLine($"[@0] conditionCount(10) = {condCount}");

            for (int i = 0; i < condCount && i < maxEntries; i++)
            {
                int start = br.Position;
                int id = br.ReadUInt(5);
                int isNot = br.ReadUInt(1);
                int orSeq = br.ReadUInt(9);
                int coff = br.ReadUInt(10);
                var def = MegaloLookup.GetCondition(id);
                sb.Append($"  cond[{i}] start@{start} id={id} ({def?.Name ?? "?"}) not={isNot} or={orSeq} off={coff}");

                if (def.HasValue)
                {
                    foreach (var p in def.Value.Params)
                    {
                        int pStart = br.Position;
                        var n = ReadParam(br, p.TypeRef, ctx);
                        sb.Append($" | {p.Name}({p.TypeRef})@{pStart}..{br.Position} = {n.Render(ctx)}");
                    }
                }
                sb.AppendLine($" | end@{br.Position}");
            }

            int actCountPos = br.Position;
            int actCount = br.ReadUInt(11);
            sb.AppendLine($"[@{actCountPos}] actionCount(11) = {actCount}");

            for (int i = 0; i < Math.Min(actCount, maxEntries); i++)
            {
                int start = br.Position;
                int id = br.ReadUInt(7);
                var def = MegaloLookup.GetAction(id);
                sb.Append($"  act[{i}] start@{start} id={id} ({def?.Name ?? "?"})");

                if (def.HasValue)
                {
                    foreach (var p in def.Value.Params)
                    {
                        int pStart = br.Position;
                        var n = ReadParam(br, p.TypeRef, ctx);
                        sb.Append($" | {p.Name}({p.TypeRef})@{pStart}..{br.Position} = {n.Render(ctx)}");
                    }
                }
                sb.AppendLine($" | end@{br.Position}");
            }

            int trigCountPos = br.Position;
            int trigCount = br.ReadUInt(9);
            sb.AppendLine($"[@{trigCountPos}] triggerCount(9) = {trigCount}");
            return sb.ToString();
        }

        /// <summary>
        /// Search the full bit stream for a header matching known counts.
        /// Returns every offset where the header width combination produces
        /// exactly the expected (cond, action, trigger) triple. Only the
        /// combinations we actually want to discriminate are tested —
        /// common Reach widths for the three counters.
        /// </summary>
        public static string FindKnownHeader(string scriptBits, int expectCond, int expectAct, int expectTrg)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(scriptBits))
            { sb.AppendLine("(empty)"); return sb.ToString(); }

            sb.AppendLine($"Searching for cond={expectCond}, action={expectAct}, trigger={expectTrg}");
            sb.AppendLine($"Stream length: {scriptBits.Length} bits");
            sb.AppendLine();

            // Tested header width combinations.
            (int cB, int aB, int tB)[] combos =
            {
                (10, 11, 8), (10, 11, 9),
                (9,  11, 8), (9,  11, 9),
                (10, 10, 8), (10, 10, 9),
                (8,  11, 8), (9,  10, 8),
                (10, 12, 8), (11, 11, 8),
            };

            int found = 0;
            foreach (var (cB, aB, tB) in combos)
            {
                int total = cB + aB + tB;
                for (int off = 0; off + total <= scriptBits.Length; off++)
                {
                    int c = ReadUIntAt(scriptBits, off, cB);
                    if (c != expectCond) continue;
                    int a = ReadUIntAt(scriptBits, off + cB, aB);
                    if (a != expectAct) continue;
                    int t = ReadUIntAt(scriptBits, off + cB + aB, tB);
                    if (t != expectTrg) continue;

                    sb.AppendLine($"MATCH widths=({cB},{aB},{tB}) offset={off} "
                                 + $"nextBits={scriptBits.Substring(off + total, Math.Min(32, scriptBits.Length - off - total))}");
                    found++;
                    if (found > 50) { sb.AppendLine("…(truncated, > 50 matches)"); return sb.ToString(); }
                }
            }

            if (found == 0) sb.AppendLine("No match found — maybe layout differs from assumed header widths.");
            return sb.ToString();
        }

        private static int ReadUIntAt(string bits, int offset, int count)
        {
            int val = 0;
            for (int i = 0; i < count; i++)
            {
                val <<= 1;
                if (bits[offset + i] == '1') val |= 1;
            }
            return val;
        }

        /// <summary>
        /// Diagnostic: tries bit offsets in [-range, +range] and reports
        /// every offset at which reading the standard (condCount 10,
        /// actionCount 11, triggerCount 8) header yields plausible counts.
        /// Lets us pin down where the real script header lives when
        /// upstream ReadBinary's handoff is misaligned.
        /// </summary>
        public static string ScanForScriptStart(string scriptBits, int range = 64)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(scriptBits))
            {
                sb.AppendLine("(empty scriptBits)");
                return sb.ToString();
            }

            sb.AppendLine($"-- scanning ±{range} bits for plausible header ---");
            sb.AppendLine($"-- scriptBits length: {scriptBits.Length} bits");
            sb.AppendLine();
            sb.AppendLine("offset | cond | action | trigger | first 64 bits");
            sb.AppendLine("-------|------|--------|---------|----------------");

            int best = 0;
            double bestScore = -1;
            for (int offset = -range; offset <= range; offset++)
            {
                if (offset < 0 || offset + 29 > scriptBits.Length) continue;
                string slice = scriptBits.Substring(offset);
                var br = new BitReader(slice);
                int cond = br.ReadUInt(10);
                int act = br.ReadUInt(11);
                int trg = br.ReadUInt(8);

                // Score: triggers > 0, counts in sane range, cond:act ratio ~< 5.
                double score = 0;
                if (trg > 0 && trg < 256) score += 10;
                if (cond >= 0 && cond < 256) score += 1;
                if (act >= 0 && act < 512) score += 1;
                if (cond > 0 && act > 0 && trg > 0) score += 5;

                if (score > bestScore) { bestScore = score; best = offset; }

                string preview = scriptBits.Substring(offset, Math.Min(64, scriptBits.Length - offset));
                sb.AppendLine($"{offset,6} | {cond,4} | {act,6} | {trg,7} | {preview}");
            }

            sb.AppendLine();
            sb.AppendLine($"best offset (by plausibility score {bestScore}): {best}");
            return sb.ToString();
        }

        /// <summary>
        /// C#-style script render for round-trip compile/edit via
        /// ScriptCompiler. The output uses inline variable declarations
        /// with Priority/Static attributes (matching the compiler's
        /// VariableInfo model):
        ///
        ///   [Attribute("OnPregame")]
        ///   void Pregame() {
        ///       [Priority("high"), Static] Object cam;
        ///       cam = CreateBetween(cam, cam, initial_loadout_camera, 0);
        ///       cam.SetHidden(false);
        ///       [Priority("low")] Number counter;
        ///       if (counter &lt; 600) {
        ///           counter += 1;
        ///           trigger_0();
        ///       }
        ///   }
        ///
        /// Rules for which variables are declared inline vs at top:
        ///   - Variables used in exactly ONE trigger are declared inline
        ///     inside that trigger (local scope, can be reused).
        ///   - Variables used across MULTIPLE triggers are declared at
        ///     file scope as static (the slot must persist).
        ///   - Network priority is read from the parsed global-pool
        ///     records (when available; defaults to "low" if unknown).
        ///
        /// Forge labels emit as `label "name";` at the top of the file.
        /// </summary>
        // Optional progress log — when set, each major stage of
        // DecompileAsScript / RenderAsCSharpScript writes a one-line
        // progress message. Useful for diagnosing freezes (the WPF UI
        // hooks this up to write to %TEMP%\uge_decompile.log).
        public static Action<string>? ProgressLog;
        private static void Prog(string msg)
        {
            try { ProgressLog?.Invoke(msg); } catch { }
            System.Diagnostics.Debug.WriteLine("[ScriptDecompiler] " + msg);
        }

        public static string DecompileAsScript(string scriptBits, List<string>? labelNames = null,
                                               GameVariant game = GameVariant.Reach,
                                               List<string>? stringTable = null)
        {
            if (string.IsNullOrWhiteSpace(scriptBits)) return string.Empty;

            Prog($"DecompileAsScript: ParseScript on {scriptBits.Length} bits (game={game})");
            var (ctx, conditions, actions, triggers, vars, parseEnd) = ParseScript(scriptBits, labelNames, game, stringTable);
            Prog($"DecompileAsScript: parsed {conditions.Count} conds / {actions.Count} acts / {triggers.Count} trigs (parseEnd={parseEnd})");
            return RenderAsCSharpScript(ctx, conditions, actions, triggers, vars);
        }

        // Shared rendering for the C#-style script output. Used by both
        // DecompileAsScript (parses and renders) and DecompileDetailed
        // (parses for walker bit-advance, then renders the same way so the
        // UI gets the auto-variable-managed C# form).
        private static string RenderAsCSharpScript(
            DecompileContext ctx,
            List<ConditionEntry> conditions,
            List<ActionEntry> actions,
            List<TriggerEntry> triggers,
            MegaloVarsInfo vars)
        {
            Prog("Render: BuildVariableUsage");
            var usage = BuildVariableUsage(triggers, conditions, actions, ctx);

            Prog("Render: CountTriggerReferences");
            var triggerRefs = CountTriggerReferences(actions, ctx);

            // Per-render inline-expansion budget. Bounds total recursive
            // EmitInlinedTrigger calls; once exhausted, surplus inlines
            // fall back to a plain `triggerN()` call. 4× the trigger count
            // is generous for any reasonable script and still cheap.
            _inlineBudget = Math.Max(1024, triggers.Count * 4);

            var sb = new StringBuilder(64 * 1024);

            // (Forge-label name table is communicated via the binary's
            // post-megl Labels section, not via top-of-script declarations.
            // Use sites render the resolved name inline.)

            Prog("Render: EmitStaticGlobals");
            EmitStaticGlobals(sb, vars);

            int topLevelCount = 0;
            const int MaxOutputBytes = 2_000_000;

            // Partition top-level triggers into:
            //   • functions  — refCount >= 2, emitted FIRST so the script
            //     reads top-down (definitions, then the events that call them).
            //   • events     — refCount == 0 entry points (Do+OnTick etc.).
            var functionsFirst = new List<int>();
            var eventsLast = new List<int>();
            int emptySkipped = 0;
            for (int ti = 0; ti < triggers.Count; ti++)
            {
                if (!IsTopLevelTrigger(triggers[ti], ti, triggerRefs)) continue;

                // Skip top-level triggers that contain no conditions and no
                // actions. Real Reach gametypes commonly carry dozens of
                // empty Do+OnTick trigger slots (orphan event handlers that
                // exist for the engine's sake but contain no script logic).
                // Rendering them as `void { }` floods the output with noise
                // and hides the meaningful content. Empty trigger slots
                // contribute nothing to script semantics, so suppress them.
                if (triggers[ti].ConditionCount == 0 && triggers[ti].ActionCount == 0)
                {
                    emptySkipped++;
                    continue;
                }

                triggerRefs.TryGetValue(ti, out int rc);
                (rc >= 2 ? functionsFirst : eventsLast).Add(ti);
            }

            void EmitOne(int ti)
            {
                if (sb.Length > MaxOutputBytes) return;
                Prog($"Render: EmitTriggerCSharp[{ti}] (type={triggers[ti].TypeName} attr={triggers[ti].AttributeName} condCount={triggers[ti].ConditionCount} actCount={triggers[ti].ActionCount})");
                EmitTriggerCSharp(sb, triggers[ti], ti, conditions, actions, ctx, usage, triggers, triggerRefs);
                sb.AppendLine();
                topLevelCount++;
            }

            foreach (var ti in functionsFirst) EmitOne(ti);
            foreach (var ti in eventsLast)     EmitOne(ti);

            if (emptySkipped > 0)
                sb.AppendLine($"// {emptySkipped} empty top-level trigger slot(s) omitted.");

            if (sb.Length > MaxOutputBytes)
                sb.AppendLine($"// TRUNCATED: emitted {topLevelCount} of {functionsFirst.Count + eventsLast.Count} top-level triggers; output cap reached.");

            Prog($"Render: emitted {topLevelCount} top-level triggers ({functionsFirst.Count} functions, {eventsLast.Count} events), sb.Length={sb.Length}");

            // WrapLongLines is O(N) per line but with regex-y inner loops;
            // for outputs over a few MB it dominates wall time. Skip it
            // entirely for large variants — the editor wraps visually
            // anyway, and the round-trip compiler doesn't care.
            string raw = sb.ToString();
            // Variable redesign Turn 4a: annotate global-slot LHS
            // assignments with their `<priority>? <type>` from MegaloVars.
            raw = AnnotateGlobalAssignments(raw, vars);
            // Long-line wrapping disabled — calls like `place_at_me(...)`
            // read more naturally on a single line, and the editor wraps
            // visually anyway.
            return raw;
        }

        // Variable redesign Turn 4a/4b: prefix any slot-assignment LHS
        // with `<priority>? <type>` so the user can read each variable's
        // network priority at the assignment site without scrolling back
        // to the top of the script. Low priority is omitted for terseness.
        // Covers global LHS (Turn 4a) and per-scope sub-pool LHS (Turn 4b).
        // Condition operands and call args are deferred to Turn 4c.
        private static string AnnotateGlobalAssignments(string src, MegaloVarsInfo vars)
        {
            if (vars == null || vars.Pools.Count == 0) return src;

            // Build per-scope name → (priority, type) maps. Each scope's
            // sub-pools share the same kind-prefix naming, but live in
            // separate pools so their priority/type are looked up by scope.
            var globalLookup = new Dictionary<string, (string pri, string typeKw)>(StringComparer.Ordinal);
            var playerLookup = new Dictionary<string, (string pri, string typeKw)>(StringComparer.Ordinal);
            var objectLookup = new Dictionary<string, (string pri, string typeKw)>(StringComparer.Ordinal);
            var teamLookup   = new Dictionary<string, (string pri, string typeKw)>(StringComparer.Ordinal);

            void AddPool(Dictionary<string, (string, string)> dst,
                         string poolName, string typeKw, string namePrefix)
            {
                if (!vars.Pools.TryGetValue(poolName, out var pool)) return;
                for (int i = 0; i < pool.Count; i++)
                {
                    string name = $"{namePrefix}{i + 1}";
                    string pri = LocalityName(pool[i].Locality);
                    dst[name] = (pri, typeKw);
                }
            }
            AddPool(globalLookup, "globalnumbers", "int",    "num");
            AddPool(globalLookup, "globaltimers",  "timer",  "tmr");
            AddPool(globalLookup, "globalplayers", "player", "plr");
            AddPool(globalLookup, "globalobjects", "object", "obj");
            AddPool(globalLookup, "globalteams",   "team",   "tm");
            AddPool(playerLookup, "playernumbers", "int",    "num");
            AddPool(playerLookup, "playertimers",  "timer",  "tmr");
            AddPool(playerLookup, "playerplayers", "player", "plr");
            AddPool(playerLookup, "playerobjects", "object", "obj");
            AddPool(playerLookup, "playerteams",   "team",   "tm");
            AddPool(objectLookup, "objectnumbers", "int",    "num");
            AddPool(objectLookup, "objecttimers",  "timer",  "tmr");
            AddPool(objectLookup, "objectplayers", "player", "plr");
            AddPool(objectLookup, "objectobjects", "object", "obj");
            AddPool(objectLookup, "objectteams",   "team",   "tm");
            AddPool(teamLookup,   "teamnumbers",   "int",    "num");
            AddPool(teamLookup,   "teamtimers",    "timer",  "tmr");
            AddPool(teamLookup,   "teamplayers",   "player", "plr");
            AddPool(teamLookup,   "teamobjects",   "object", "obj");
            AddPool(teamLookup,   "teamteams",     "team",   "tm");

            // Turn 4c: mid-line annotations. Walk lines with brace-depth
            // tracking — annotate ONLY inside trigger bodies so top-of-script
            // decls (which already carry their priority+type as part of the
            // declaration syntax) aren't double-annotated. Within trigger
            // bodies, prepend `<priority>? <type> ` to every slot reference
            // at any expression position: assignment LHS, condition operands,
            // method-call receivers, call arguments, etc.
            //
            // Lookbehind `(?<![\w.])` skips matches preceded by a word char
            // or a dot — so `myObj1` and `current_player.foo.num1` don't
            // false-match (the latter's `num1` is preceded by `.`, but the
            // FULL `current_player.foo.num1` doesn't fit our pattern either
            // since `foo` isn't a slot, so it's correctly skipped). Lookahead
            // `(?!\w)` on bare patterns prevents matching `num12abc`.
            var rxPlayer = new System.Text.RegularExpressions.Regex(
                @"(?<![\w.])((?:current_player|globalplayer\d+|plr\d+|temp_player_\d+)\.(?<kind>num|tmr|plr|obj|tm)(?<idx>\d+))(?!\w)");
            var rxObject = new System.Text.RegularExpressions.Regex(
                @"(?<![\w.])((?:current_object|globalobject\d+|obj\d+|temp_obj_\d+)\.(?<kind>num|tmr|plr|obj|tm)(?<idx>\d+))(?!\w)");
            var rxTeam = new System.Text.RegularExpressions.Regex(
                @"(?<![\w.])((?:current_team|globalteam\d+|tm\d+|temp_team_\d+)\.(?<kind>num|tmr|plr|obj|tm)(?<idx>\d+))(?!\w)");
            var rxBare = new System.Text.RegularExpressions.Regex(
                @"(?<![\w.])(?<kind>num|tmr|plr|obj|tm)(?<idx>\d+)(?!\w)");

            string AnnotateMatches(string line, System.Text.RegularExpressions.Regex rx,
                                   Dictionary<string, (string pri, string typeKw)> lookup)
            {
                return rx.Replace(line, m =>
                {
                    string kind = m.Groups["kind"].Value;
                    string idx = m.Groups["idx"].Value;
                    string name = $"{kind}{idx}";
                    if (!lookup.TryGetValue(name, out var info)) return m.Value;
                    string priKw = info.pri == "low" ? "" : info.pri + " ";
                    // For receiver-prefixed regexes the WHOLE `recv.name`
                    // is captured by group 1; for bare regex group 1 is
                    // the slot name itself. Replacement uses m.Value so
                    // both paths work uniformly.
                    return $"{priKw}{info.typeKw} {m.Value}";
                });
            }

            var lines = src.Replace("\r\n", "\n").Split('\n');
            int depth = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int startDepth = depth;
                foreach (char c in lines[i])
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                if (startDepth > 0)
                {
                    // Order matters: receiver-prefixed first so the bare
                    // regex's lookbehind reliably skips slots already
                    // claimed by a sub-pool match.
                    lines[i] = AnnotateMatches(lines[i], rxPlayer, playerLookup);
                    lines[i] = AnnotateMatches(lines[i], rxObject, objectLookup);
                    lines[i] = AnnotateMatches(lines[i], rxTeam,   teamLookup);
                    lines[i] = AnnotateMatches(lines[i], rxBare,   globalLookup);
                }
            }
            return string.Join("\n", lines);
        }

        // Post-processor: any line longer than `maxWidth` gets split at the
        // outermost argument-list commas so long method calls read as
        //   `recv.method(`
        //   `    arg1,`
        //   `    arg2);`
        // Keeps string literals intact; only splits top-level commas.
        private static string WrapLongLines(string src, int maxWidth)
        {
            var lines = src.Replace("\r\n", "\n").Split('\n');
            var outSb = new StringBuilder(src.Length + 256);
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                if (line.Length <= maxWidth) { outSb.AppendLine(line); continue; }

                // Compute leading indent.
                int ind = 0;
                while (ind < line.Length && line[ind] == ' ') ind++;
                string indent = new string(' ', ind);

                // Find FIRST '(' that starts a breakable call. Skip parens in strings.
                int openParen = FindFirstTopLevelChar(line, '(');
                if (openParen < 0) { outSb.AppendLine(line); continue; }

                // Match its closing ')'.
                int close = FindMatchingClose(line, openParen);
                if (close < 0) { outSb.AppendLine(line); continue; }

                string head = line.Substring(0, openParen + 1);            // "...foo("
                string tail = line.Substring(close);                        // ")...;"
                string argsBlob = line.Substring(openParen + 1, close - openParen - 1);

                var args = SplitTopLevelCommas(argsBlob);
                if (args.Count < 2) { outSb.AppendLine(line); continue; }

                string contIndent = indent + "    ";
                outSb.AppendLine(head);
                for (int i = 0; i < args.Count; i++)
                {
                    string a = args[i].Trim();
                    string sep = (i < args.Count - 1) ? "," : "";
                    outSb.AppendLine(contIndent + a + sep);
                }
                outSb.AppendLine(indent + tail.TrimStart());
            }
            return outSb.ToString();
        }

        // Scans `s` and returns index of first `target` char at paren/bracket
        // depth 0 and outside of a string literal; -1 if not found.
        private static int FindFirstTopLevelChar(string s, char target)
        {
            bool inStr = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' && (i == 0 || s[i - 1] != '\\')) inStr = !inStr;
                if (inStr) continue;
                if (c == target) return i;
            }
            return -1;
        }

        private static int FindMatchingClose(string s, int openIdx)
        {
            bool inStr = false;
            int depth = 0;
            for (int i = openIdx; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' && (i == 0 || s[i - 1] != '\\')) inStr = !inStr;
                if (inStr) continue;
                if (c == '(') depth++;
                else if (c == ')') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        private static List<string> SplitTopLevelCommas(string s)
        {
            var result = new List<string>();
            bool inStr = false;
            int depth = 0;
            int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' && (i == 0 || s[i - 1] != '\\')) inStr = !inStr;
                if (inStr) continue;
                if (c == '(' || c == '[' || c == '{') depth++;
                else if (c == ')' || c == ']' || c == '}') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(s.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(s.Substring(start));
            return result;
        }

        // Counts how many RunTrigger calls target each trigger index.
        // Looks at action args rendered through ctx so the result reflects
        // the same "trigger_N" identifiers used in the final output.
        private static Dictionary<int, int> CountTriggerReferences(
            List<ActionEntry> actions, DecompileContext ctx)
        {
            var refs = new Dictionary<int, int>();
            foreach (var a in actions)
            {
                if (!string.Equals(a.Name, "Megl_RunTrigger", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(a.Name, "RunTrigger", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (a.Args.Count == 0) continue;
                string tok = a.Args[0].Render(ctx);
                var m = System.Text.RegularExpressions.Regex.Match(tok, @"trigger_?(\d+)");
                if (!m.Success) continue;
                int idx = int.Parse(m.Groups[1].Value);
                refs.TryGetValue(idx, out int cur);
                refs[idx] = cur + 1;
            }
            return refs;
        }

        // A trigger is top-level when:
        //   - Type is Do and the Attribute is an event wrapper (any attr
        //     except OnCall), OR
        //   - The trigger is referenced 2+ times by RunTrigger (needs a
        //     callable symbol), OR
        //   - The trigger is referenced 0 times (orphan: nothing to inline it into).
        // Do+OnCall triggers called exactly once are inlined, so they're
        // NOT top-level.
        private static bool IsTopLevelTrigger(TriggerEntry t, int index, Dictionary<int, int> refs)
        {
            refs.TryGetValue(index, out int refCount);

            // Per user spec + RVT compiler model:
            //
            //   Do+OnTick     — compiler-generated "scoping" subtrigger
            //                   wrapping a single condition block. Always
            //                   inlined at every call site so the script
            //                   reads as 2 sequential if-blocks rather than
            //                   `RunTrigger(N)` cluttering the body. With
            //                   refCount==0 it's an entry point (top-level).
            //   Do+OnCall     — user-defined function. refCount==1 inlines;
            //                   refCount>=2 emits as `void name() { ... }`;
            //                   refCount==0 is an orphan (top-level).
            //   Do+otherEvent — event entry point (top-level always).
            //   Foreach*      — refCount==1 inlines as `foreach … { … }`
            //                   inside the caller; otherwise top-level.
            if (t.TypeValue == 0)
            {
                // OnTick is a compiler-emitted scoping subtrigger — inline at
                // every call site; only top-level when nothing references it.
                if (string.Equals(t.AttributeName, "OnTick", StringComparison.Ordinal))
                    return refCount == 0;

                // OnCall is a user-defined function:
                //   refCount == 0 → orphan, emit as top-level (callable).
                //   refCount == 1 → sole caller inlines; not top-level.
                //   refCount >= 2 → real function (includes recursive self-
                //                   calls), MUST emit as top-level
                //                   `void trigger_N()` so RunTrigger calls
                //                   resolve and recursion works.
                if (string.Equals(t.AttributeName, "OnCall", StringComparison.Ordinal))
                    return refCount != 1;

                return true;                       // OnInit/OnLocal/etc → entry point
            }

            // Non-Do triggers (Foreach*): single-use → inline.
            return refCount != 1;
        }

        // C# identifier for a top-level trigger. Event wrappers get bare
        // lowercase event names (init, local, pregame, …); other top-level
        // triggers get synthesized identifiers based on the index.
        private static string TopLevelTriggerName(TriggerEntry t, int index)
        {
            if (t.TypeValue == 0)
            {
                switch (t.AttributeName)
                {
                    case "OnInit":          return "init";
                    case "OnLocalInit":     return "local_init";
                    case "OnHostMigration": return "host_migration";
                    case "OnObjectDeath":   return "object_death";
                    case "OnLocal":         return "local";
                    case "OnPregame":       return "pregame";
                    case "OnTick":          return string.Empty; // anonymous: rendered as `void {`
                    case "OnCall":          return $"trigger_{index}";
                }
            }
            return $"trigger_{index}";
        }

        // ---- Variable usage analysis ------------------------------------

        private sealed class VarSlot
        {
            public string Scope = "";        // "global.object", "global.number", etc.
            public int Index;                // slot index
            public string CsType = "";       // "Object", "Number", etc.
            public string Priority = "low";  // "low" / "high" / "local"
            public readonly HashSet<int> UsedInTriggers = new();
            public string DeclaredName = ""; // synthesized identifier
        }

        private static Dictionary<string, VarSlot> BuildVariableUsage(
            List<TriggerEntry> triggers,
            List<ConditionEntry> conditions,
            List<ActionEntry> actions,
            DecompileContext ctx)
        {
            var slots = new Dictionary<string, VarSlot>(StringComparer.OrdinalIgnoreCase);

            void Register(string rendered, int triggerIndex)
            {
                // Parse "GlobalObject[3]" / "globalnumbers[0]" / "Player.Number(Player0, playernumbers[0])" etc.
                // into a slot key.
                foreach (System.Text.RegularExpressions.Match m in
                    System.Text.RegularExpressions.Regex.Matches(rendered ?? "",
                        @"(GlobalObject|GlobalPlayer|GlobalTeam|GlobalTimer|globalnumbers|globaltimers|globalteams|globalplayers|globalobjects|playernumbers|playertimers|playerteams|playerplayers|playerobjects|objectnumbers|objecttimers|objectteams|objectplayers|objectobjects|teamnumbers|teamtimers|teamteams|teamplayers|teamobjects)\[(\d+)\]"))
                {
                    string kind = m.Groups[1].Value;
                    int idx = int.Parse(m.Groups[2].Value);
                    string key = $"{kind}[{idx}]";
                    if (!slots.TryGetValue(key, out var slot))
                    {
                        slot = new VarSlot
                        {
                            Scope = NormalizeScope(kind),
                            Index = idx,
                            CsType = CsTypeForScope(kind),
                            Priority = "low",
                            DeclaredName = SynthName(kind, idx),
                        };
                        slots[key] = slot;
                    }
                    slot.UsedInTriggers.Add(triggerIndex);
                }

                // Temporaries appear in rendered output as flat identifiers
                // `temp_num_N`, `temp_obj_N`, `temp_player_N`, `temp_team_N`.
                // Register them under their own scope so they get included
                // in the declaration block alongside global slots.
                foreach (System.Text.RegularExpressions.Match m in
                    System.Text.RegularExpressions.Regex.Matches(rendered ?? "",
                        @"\btemp_(num|obj|player|team)_(\d+)\b"))
                {
                    string scopeKind = m.Groups[1].Value;  // num/obj/player/team
                    int idx = int.Parse(m.Groups[2].Value);
                    string key = $"temp_{scopeKind}_{idx}";
                    if (!slots.TryGetValue(key, out var slot))
                    {
                        slot = new VarSlot
                        {
                            Scope = "temp." + scopeKind,
                            Index = idx,
                            CsType = scopeKind switch
                            {
                                "num"    => "Number",
                                "obj"    => "Object",
                                "player" => "Player",
                                "team"   => "Team",
                                _        => "Object",
                            },
                            Priority = "low",
                            DeclaredName = key,
                        };
                        slots[key] = slot;
                    }
                    slot.UsedInTriggers.Add(triggerIndex);
                }
            }

            for (int ti = 0; ti < triggers.Count; ti++)
            {
                var t = triggers[ti];
                for (int i = 0; i < t.ConditionCount; i++)
                {
                    int idx = t.ConditionOffset + i;
                    if (idx < 0 || idx >= conditions.Count) continue;
                    foreach (var a in conditions[idx].Args) Register(a.Render(ctx), ti);
                }
                for (int i = 0; i < t.ActionCount; i++)
                {
                    int idx = t.ActionOffset + i;
                    if (idx < 0 || idx >= actions.Count) continue;
                    foreach (var a in actions[idx].Args) Register(a.Render(ctx), ti);
                }
            }
            return slots;
        }

        private static string NormalizeScope(string kind) => kind switch
        {
            "GlobalObject" or "globalobjects"  => "global.object",
            "GlobalPlayer" or "globalplayers"  => "global.player",
            "GlobalTeam"   or "globalteams"    => "global.team",
            "GlobalTimer"  or "globaltimers"   => "global.timer",
            "globalnumbers"                    => "global.number",
            "playerplayers"                    => "player.player",
            "playerobjects"                    => "player.object",
            "playerteams"                      => "player.team",
            "playertimers"                     => "player.timer",
            "playernumbers"                    => "player.number",
            "objectplayers"                    => "object.player",
            "objectobjects"                    => "object.object",
            "objectteams"                      => "object.team",
            "objecttimers"                     => "object.timer",
            "objectnumbers"                    => "object.number",
            "teamplayers"                      => "team.player",
            "teamobjects"                      => "team.object",
            "teamteams"                        => "team.team",
            "teamtimers"                       => "team.timer",
            "teamnumbers"                      => "team.number",
            _ => kind
        };

        private static string CsTypeForScope(string kind) => kind switch
        {
            "GlobalObject" or "globalobjects" or "playerobjects" or "objectobjects" or "teamobjects" => "Object",
            "GlobalPlayer" or "globalplayers" or "playerplayers" or "objectplayers" or "teamplayers" => "Player",
            "GlobalTeam"   or "globalteams"   or "playerteams"   or "objectteams"   or "teamteams"   => "Team",
            "GlobalTimer"  or "globaltimers"  or "playertimers"  or "objecttimers"  or "teamtimers"  => "Timer",
            "globalnumbers"or "playernumbers" or "objectnumbers" or "teamnumbers"                    => "Number",
            _ => "Object",
        };

        private static string SynthName(string kind, int idx) => kind switch
        {
            // Globals — short kind-prefixed names (1-based) so the user
            // never has to think in terms of slot indices. The compiler's
            // PreprocessDialect rewrites these back to the internal
            // `globalNUMBER0`-style names for the rest of the encode
            // pipeline. `num`/`tmr`/`plr`/`obj`/`tm` prefixes avoid
            // colliding with the C# `int` keyword in source positions
            // like `int num1` (type + name).
            "GlobalObject" or "globalobjects" => $"obj{idx + 1}",
            "GlobalPlayer" or "globalplayers" => $"plr{idx + 1}",
            "GlobalTeam"   or "globalteams"   => $"tm{idx + 1}",
            "GlobalTimer"  or "globaltimers"  => $"tmr{idx + 1}",
            "globalnumbers"                   => $"num{idx + 1}",
            // Per-scope sub-pools (Turn 3): same kind-prefixed names as
            // the global pools — the receiver makes the scope unambiguous,
            // so `current_player.num1`, `current_object.num1`, and
            // `current_team.num1` each refer to a different sub-pool.
            // The dotted-accessor flattening pass produces forms like
            // `current_player.num1`, `current_object.tmr2`, etc.
            "playerplayers" => $"plr{idx + 1}",
            "playerobjects" => $"obj{idx + 1}",
            "playerteams"   => $"tm{idx + 1}",
            "playernumbers" => $"num{idx + 1}",
            "playertimers"  => $"tmr{idx + 1}",
            "objectplayers" => $"plr{idx + 1}",
            "objectobjects" => $"obj{idx + 1}",
            "objectteams"   => $"tm{idx + 1}",
            "objectnumbers" => $"num{idx + 1}",
            "objecttimers"  => $"tmr{idx + 1}",
            "teamplayers"   => $"plr{idx + 1}",
            "teamobjects"   => $"obj{idx + 1}",
            "teamteams"     => $"tm{idx + 1}",
            "teamnumbers"   => $"num{idx + 1}",
            "teamtimers"    => $"tmr{idx + 1}",
            _ => $"{kind}_{idx}",
        };

        // Categorize a VarSlot into a top-level declaration group for
        // ordering: globals first, then per-player, per-object, per-team
        // sub-pools. Temporaries are NOT declared (the script doesn't own
        // them) and so are never emitted here.
        private static int GroupOrder(VarSlot v)
        {
            string s = v.Scope ?? "";
            if (s.StartsWith("temp.", StringComparison.Ordinal)) return -1; // skip
            if (s.StartsWith("global.", StringComparison.Ordinal)) return 0;
            if (s.StartsWith("player.", StringComparison.Ordinal)) return 1;
            if (s.StartsWith("object.", StringComparison.Ordinal)) return 2;
            if (s.StartsWith("team.",   StringComparison.Ordinal)) return 3;
            return 4;
        }

        private static string GroupHeader(int g) => g switch
        {
            0 => "// global variables",
            1 => "// player variables",
            2 => "// object variables",
            3 => "// team variables",
            _ => "// other variables",
        };

        // Emit the script's variable declaration block from the parsed
        // MegaloVars info. Every declared slot gets a line:
        //   [Priority("local")] Number globalnumber0;          (numbers/players/objects/teams)
        //   Timer  globaltimer0 = 5;                           (timers — no priority)
        // Pools are grouped by scope (global / player / object / team) so
        // the script reads as a clean declaration block. Temporaries are
        // never declared.
        private static void EmitStaticGlobals(StringBuilder sb, MegaloVarsInfo vars)
        {
            if (vars == null || vars.Pools.Count == 0) return;

            // Pool emission order grouped by scope. Inside each scope:
            // numbers, timers, teams, players, objects.
            (string Group, string[] PoolNames)[] groups =
            {
                ("// global variables",
                    new[] { "globalnumbers", "globaltimers", "globalteams", "globalplayers", "globalobjects" }),
                ("// player variables",
                    new[] { "playernumbers", "playertimers", "playerteams", "playerplayers", "playerobjects" }),
                ("// object variables",
                    new[] { "objectnumbers", "objecttimers", "objectteams", "objectplayers", "objectobjects" }),
                ("// team variables",
                    new[] { "teamnumbers",   "teamtimers",   "teamteams",   "teamplayers",   "teamobjects"   }),
            };

            bool wroteAny = false;
            foreach (var (header, names) in groups)
            {
                bool groupHasAny = names.Any(n => vars.Pools.TryGetValue(n, out var p) && p.Count > 0);
                if (!groupHasAny) continue;

                if (wroteAny) sb.AppendLine();
                sb.AppendLine(header);
                wroteAny = true;

                foreach (var poolName in names)
                {
                    if (!vars.Pools.TryGetValue(poolName, out var pool) || pool.Count == 0) continue;
                    string csType = poolName.EndsWith("numbers") ? "Number"
                                  : poolName.EndsWith("timers")  ? "Timer"
                                  : poolName.EndsWith("teams")   ? "Team"
                                  : poolName.EndsWith("players") ? "Player"
                                  : "Object";
                    // User-facing keyword: lowercase across the board —
                    // `int`, `timer`, `player`, `object`, `team`. Internal
                    // `csType` stays PascalCase for downstream conditionals.
                    string displayType = csType switch
                    {
                        "Number" => "int",
                        "Timer"  => "timer",
                        "Player" => "player",
                        "Object" => "object",
                        "Team"   => "team",
                        _ => csType.ToLowerInvariant(),
                    };
                    bool isTimer = csType == "Timer";

                    // Decl-side names disambiguate scopes via dotted
                    // receiver syntax: globals stay bare (`num1`, `tmr1`)
                    // while per-scope sub-pool decls get a `<scope>.`
                    // prefix that mirrors the use-site form
                    // (`current_player.num1` ↔ `player.num1`). The
                    // top-of-script symbol table can't collide because
                    // `player.num1` and bare `num1` are distinct names.
                    string DeclScopePrefix(string pool) => pool switch
                    {
                        var p when p.StartsWith("global", StringComparison.Ordinal) => "",
                        var p when p.StartsWith("player", StringComparison.Ordinal) => "player.",
                        var p when p.StartsWith("object", StringComparison.Ordinal) => "object.",
                        var p when p.StartsWith("team",   StringComparison.Ordinal) => "team.",
                        _ => "",
                    };
                    for (int i = 0; i < pool.Count; i++)
                    {
                        var s = pool[i];
                        string baseName = SynthName(poolName, i);
                        string scopePrefix = DeclScopePrefix(poolName);
                        string varName = scopePrefix + baseName;
                        if (isTimer)
                        {
                            string init = s.InitialValue ?? "0";
                            sb.AppendLine($"timer {varName} = {init};");
                        }
                        else
                        {
                            // Variable redesign Turn 2: priority moves from
                            // a `[Priority("X")]` attribute to a leading
                            // keyword (`low`/`high`/`local`). `low` is the
                            // implicit default — omitted for terseness, so
                            // a low-priority slot reads exactly like a C#
                            // local: `int num1 = 0;`. `high` and `local`
                            // are emitted explicitly because the user
                            // needs to see them at the declaration site.
                            string priority = LocalityName(s.Locality);
                            string priorityKeyword = priority == "low" ? "" : priority + " ";
                            string suffix = !string.IsNullOrEmpty(s.InitialValue) && csType == "Number"
                                ? $" = {s.InitialValue}"
                                : (csType == "Team" && !string.IsNullOrEmpty(s.InitialValue)
                                    ? $" = {s.InitialValue}"
                                    : string.Empty);
                            sb.AppendLine($"{priorityKeyword}{displayType} {varName}{suffix};");
                        }
                    }
                }
            }
            if (wroteAny) sb.AppendLine();
        }

        // ---- Trigger emission (C# method form) --------------------------

        private static void EmitTriggerCSharp(
            StringBuilder sb,
            TriggerEntry t,
            int index,
            List<ConditionEntry> allConds,
            List<ActionEntry> allActs,
            DecompileContext ctx,
            Dictionary<string, VarSlot> usage,
            List<TriggerEntry> allTriggers,
            Dictionary<int, int> triggerRefs)
        {
            // Foreach* triggers (type 1..6) render as a bare iteration
            // block — `foreach player { ... }`, `foreach object with label
            // X { ... }`, etc. — never wrapped in `void name() { ... }`.
            // Only Do triggers (type==0) become functions or event wrappers.
            if (t.TypeValue >= 1 && t.TypeValue <= 6)
            {
                string? loopHeader = t.TypeValue switch
                {
                    1 => "foreach player",
                    2 => "foreach player randomly",
                    3 => "foreach team",
                    4 => "foreach object",
                    5 => "foreach object with label" + (string.IsNullOrWhiteSpace(t.LabelText) || t.LabelText == "None" ? "" : " " + t.LabelText.Trim()),
                    6 => "foreach object with filter" + (string.IsNullOrWhiteSpace(t.LabelText) || t.LabelText == "None" ? "" : " " + t.LabelText.Trim()),
                    _ => null,
                };
                sb.Append(loopHeader).AppendLine(" {");
                EmitTriggerBodyCSharp(sb, t, index, allConds, allActs, ctx, usage,
                                      allTriggers, triggerRefs, indent: 1,
                                      inlinedFrom: new HashSet<int>());
                sb.AppendLine("}");
                return;
            }

            // Do trigger (type=0): function (refCount>=2) or event wrapper
            // (refCount==0). Anonymous "OnTick" wrappers emit as `void {`
            // (no name); named ones emit as `void name() {`.
            string methodName = TopLevelTriggerName(t, index);
            sb.AppendLine(string.IsNullOrEmpty(methodName)
                ? "void {"
                : $"void {methodName}() {{");

            var reachable = CollectReachableTriggers(index, allActs, allTriggers, triggerRefs, ctx);
            var localVars = usage.Values
                .Where(v => reachable.Contains(v.UsedInTriggers.First())
                            && v.UsedInTriggers.Count == 1
                            && reachable.IsSupersetOf(v.UsedInTriggers)
                            // Temporaries aren't declared — the script
                            // doesn't own them and they don't take a
                            // networking priority.
                            && !(v.Scope ?? "").StartsWith("temp.", StringComparison.Ordinal))
                .OrderBy(v => v.Scope).ThenBy(v => v.Index)
                .ToList();
            foreach (var v in localVars)
                sb.AppendLine($"    [Priority(\"{v.Priority}\")] {v.CsType} {v.DeclaredName};");
            if (localVars.Count > 0) sb.AppendLine();

            EmitTriggerBodyCSharp(sb, t, index, allConds, allActs, ctx, usage,
                                  allTriggers, triggerRefs, indent: 1,
                                  inlinedFrom: new HashSet<int>());
            sb.AppendLine("}");
        }

        // Walks the trigger's body + recursively any inlined single-use
        // triggers it calls. Returns the set of trigger indexes whose
        // actions/conditions appear in the final rendered output.
        private static HashSet<int> CollectReachableTriggers(
            int rootIndex,
            List<ActionEntry> allActs,
            List<TriggerEntry> allTriggers,
            Dictionary<int, int> triggerRefs,
            DecompileContext ctx)
        {
            var result = new HashSet<int> { rootIndex };
            var stack = new Stack<int>();
            stack.Push(rootIndex);

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (i < 0 || i >= allTriggers.Count) continue;
                var t = allTriggers[i];
                for (int k = 0; k < t.ActionCount; k++)
                {
                    int idx = t.ActionOffset + k;
                    if (idx < 0 || idx >= allActs.Count) continue;
                    var a = allActs[idx];
                    if (!string.Equals(a.Name, "Megl_RunTrigger", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(a.Name, "RunTrigger", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (a.Args.Count == 0) continue;
                    string tok = a.Args[0].Render(ctx);
                    var m = System.Text.RegularExpressions.Regex.Match(tok, @"trigger_?(\d+)");
                    if (!m.Success) continue;
                    int tgt = int.Parse(m.Groups[1].Value);
                    // Recurse into any target that will end up inlined.
                    triggerRefs.TryGetValue(tgt, out int rc);
                    bool willInline = rc == 1;
                    if (!willInline && tgt >= 0 && tgt < allTriggers.Count)
                    {
                        var tt = allTriggers[tgt];
                        willInline = tt.TypeValue == 0
                            && (string.Equals(tt.AttributeName, "OnTick", StringComparison.Ordinal)
                             || string.Equals(tt.AttributeName, "OnCall", StringComparison.Ordinal));
                    }
                    if (willInline && result.Add(tgt)) stack.Push(tgt);
                }
            }
            return result;
        }

        // Emits a trigger's conditions+actions as C#. If an action is a
        // RunTrigger to a single-use target, that target's body is
        // inlined recursively (wrapped in foreach for non-Do types).
        private static void EmitTriggerBodyCSharp(
            StringBuilder sb,
            TriggerEntry t,
            int index,
            List<ConditionEntry> allConds,
            List<ActionEntry> allActs,
            DecompileContext ctx,
            Dictionary<string, VarSlot> usage,
            List<TriggerEntry> allTriggers,
            Dictionary<int, int> triggerRefs,
            int indent,
            HashSet<int> inlinedFrom)
        {
            var conds = new List<ConditionEntry>();
            for (int i = 0; i < t.ConditionCount; i++)
            {
                int idx = t.ConditionOffset + i;
                if (idx >= 0 && idx < allConds.Count) conds.Add(allConds[idx]);
            }
            var acts = new List<ActionEntry>();
            for (int i = 0; i < t.ActionCount; i++)
            {
                int idx = t.ActionOffset + i;
                if (idx >= 0 && idx < allActs.Count) acts.Add(allActs[idx]);
            }

            // Group conditions by the action index they gate.
            var gates = new SortedDictionary<int, List<ConditionEntry>>();
            foreach (var c in conds)
                (gates.TryGetValue(c.ActionOffset, out var l) ? l : (gates[c.ActionOffset] = new List<ConditionEntry>())).Add(c);

            // For each action, emit its gating conditions as nested
            // brace-delimited `if (cond) { ... }` blocks. Conditions
            // sharing an OrSequence become a single `if (a || b)` group;
            // AND-chained groups nest. Each block is closed at the end.
            for (int i = 0; i < acts.Count; i++)
            {
                int condLines = 0;
                if (gates.TryGetValue(i, out var gs))
                {
                    var orGroups = new SortedDictionary<int, List<ConditionEntry>>();
                    foreach (var c in gs)
                        (orGroups.TryGetValue(c.OrSequence, out var l)
                            ? l
                            : (orGroups[c.OrSequence] = new List<ConditionEntry>())).Add(c);

                    foreach (var kv in orGroups)
                    {
                        string cPad = new string(' ', (indent + condLines) * 4);
                        string condExpr;
                        if (kv.Value.Count == 1)
                        {
                            condExpr = FormatConditionCSharp(kv.Value[0], ctx, usage);
                        }
                        else
                        {
                            var orParts = new List<string>(kv.Value.Count);
                            foreach (var cc in kv.Value)
                                orParts.Add(FormatConditionCSharp(cc, ctx, usage));
                            condExpr = string.Join(" || ", orParts);
                        }
                        sb.Append(cPad).Append("if (").Append(condExpr).AppendLine(") {");
                        condLines++;
                    }
                }

                string actPad = new string(' ', (indent + condLines) * 4);
                if (acts[i].IsInline)
                {
                    // Inline action — expand its (cond_range, act_range) as
                    // if it were a tiny subtrigger. Reuses the body-emit
                    // path so OR-grouping, nesting and budget caps apply.
                    EmitInlineActionBody(sb, acts[i], allConds, allActs, ctx, usage,
                                         allTriggers, triggerRefs, indent + condLines, inlinedFrom);
                }
                else if (TryGetInlineableRunTrigger(acts[i], ctx, triggerRefs, inlinedFrom, out int inlineTarget, allTriggers)
                    && inlineTarget >= 0 && inlineTarget < allTriggers.Count)
                {
                    EmitInlinedTrigger(sb, allTriggers[inlineTarget], inlineTarget,
                                       allConds, allActs, ctx, usage,
                                       allTriggers, triggerRefs, indent + condLines, inlinedFrom);
                }
                else
                {
                    string body = FormatActionCSharp(acts[i], ctx, usage);
                    sb.Append(actPad).Append(body).AppendLine(";");
                }

                // Close every `if {` we opened for this action, in reverse.
                for (int k = condLines - 1; k >= 0; k--)
                {
                    string cPad = new string(' ', (indent + k) * 4);
                    sb.Append(cPad).AppendLine("}");
                }
            }
        }

        // Expands a Reach `Inline` action (ID 99) — a "smaller trigger"
        // body with its own (condRange, actRange) — by treating those
        // ranges as a synthetic Do trigger and re-using the per-trigger
        // body emitter. This produces the same `if (cond) { action; }`
        // structure as a real subtrigger expansion, scoping the inline's
        // conditions to its actions and nothing else.
        private static void EmitInlineActionBody(
            StringBuilder sb,
            ActionEntry inlineAct,
            List<ConditionEntry> allConds,
            List<ActionEntry> allActs,
            DecompileContext ctx,
            Dictionary<string, VarSlot> usage,
            List<TriggerEntry> allTriggers,
            Dictionary<int, int> triggerRefs,
            int indent,
            HashSet<int> inlinedFrom)
        {
            if (_inlineBudget <= 0)
            {
                sb.Append(new string(' ', indent * 4))
                  .AppendLine("/* inline (budget exhausted) */");
                return;
            }
            _inlineBudget--;

            // Synthesize a TriggerEntry over the inline's slice and let
            // EmitTriggerBodyCSharp do the regular condition-grouping.
            var synthetic = new TriggerEntry(
                typeVal: 0,
                attrVal: 0,
                typeName: "Do",
                attrName: "OnTick",
                condOffset: inlineAct.InlineCondOffset,
                condCount:  inlineAct.InlineCondCount,
                actOffset:  inlineAct.InlineActOffset,
                actCount:   inlineAct.InlineActCount,
                unk1: 0, unk2: 0);

            EmitTriggerBodyCSharp(sb, synthetic, /*index*/-1,
                                  allConds, allActs, ctx, usage,
                                  allTriggers, triggerRefs, indent, inlinedFrom);
        }

        // True if the action is a RunTrigger whose target should be inlined
        // (single-use AND not currently being inlined to avoid recursion loops).
        private static bool TryGetInlineableRunTrigger(
            ActionEntry a, DecompileContext ctx,
            Dictionary<int, int> triggerRefs, HashSet<int> inlinedFrom, out int targetIdx,
            List<TriggerEntry>? allTriggers = null)
        {
            targetIdx = -1;
            if (!string.Equals(a.Name, "Megl_RunTrigger", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(a.Name, "RunTrigger", StringComparison.OrdinalIgnoreCase))
                return false;
            if (a.Args.Count == 0) return false;

            string tok = a.Args[0].Render(ctx);
            var m = System.Text.RegularExpressions.Regex.Match(tok, @"trigger_?(\d+)");
            if (!m.Success) return false;
            int idx = int.Parse(m.Groups[1].Value);
            triggerRefs.TryGetValue(idx, out int rc);
            if (inlinedFrom.Contains(idx)) return false; // recursion guard

            // Inlining rules — must mirror IsTopLevelTrigger so a single-
            // emit trigger is always inlined at every call site:
            //   • Foreach*: only refCount==1.
            //   • Do+OnTick: ALWAYS inline (compiler-generated scoping subtrigger).
            //   • Do+OnCall, refCount==1: inline at sole call site.
            //   • Do+OnCall, refCount>=2: NOT inlined — emit as a real function
            //                   (preserves recursion + multiple call sites).
            if (allTriggers != null && idx >= 0 && idx < allTriggers.Count)
            {
                var t = allTriggers[idx];
                if (t.TypeValue == 0
                    && string.Equals(t.AttributeName, "OnTick", StringComparison.Ordinal))
                {
                    targetIdx = idx;
                    return true;
                }
                // OnCall falls through to the rc==1 check below: only the
                // sole-caller case inlines.
            }
            if (rc != 1) return false;
            targetIdx = idx;
            return true;
        }

        // Emits a single-use trigger's body inline at the call site, wrapped
        // in foreach for non-Do trigger types.
        // Per-render budget for trigger inline expansions. Each EmitInlinedTrigger
        // call decrements this counter; once exhausted, further inlines stop
        // emitting the target body and just emit a `triggerN()` call instead.
        // Depth alone isn't enough — fanout (an inlined body containing many
        // RunTriggers) multiplies work exponentially even at depth 4 or 5.
        [ThreadStatic] private static int _inlineBudget;

        private static void EmitInlinedTrigger(
            StringBuilder sb,
            TriggerEntry t,
            int index,
            List<ConditionEntry> allConds,
            List<ActionEntry> allActs,
            DecompileContext ctx,
            Dictionary<string, VarSlot> usage,
            List<TriggerEntry> allTriggers,
            Dictionary<int, int> triggerRefs,
            int indent,
            HashSet<int> inlinedFrom)
        {
            // If we've exhausted the per-render expansion budget, fall back
            // to emitting a plain call. This bounds total work to the budget.
            if (_inlineBudget <= 0)
            {
                sb.Append(new string(' ', indent * 4))
                  .AppendLine($"trigger_{index}();  // (inline budget exhausted)");
                return;
            }
            _inlineBudget--;
            string pad = new string(' ', indent * 4);
            // Short foreach syntax — `current_object` / `current_player` /
            // `current_team` are always the implicit iteration variable
            // for each kind, so the decompiled form just says
            //   `foreach object { ... }` etc.
            // Append the trigger's optional Label/Filter arg so RVT-style
            // `for each object with label "ffa_only"` round-trips properly.
            string? labelSuffix = null;
            if (!string.IsNullOrWhiteSpace(t.LabelText) && t.LabelText != "None")
                labelSuffix = " " + t.LabelText.Trim();

            string? header = null;
            switch (t.TypeValue)
            {
                case 1: header = "foreach player"; break;
                case 2: header = "foreach player randomly"; break;
                case 3: header = "foreach team"; break;
                case 4: header = "foreach object"; break;
                case 5: header = "foreach object with label" + labelSuffix; break;
                case 6: header = "foreach object with filter" + labelSuffix; break;
            }

            // Hard depth cap. Even with the inlinedFrom HashSet, deeply
            // nested cascades on malformed bins can recurse hundreds of
            // levels and explode output — bail with a placeholder instead.
            if (inlinedFrom.Count > 16)
            {
                sb.Append(pad).AppendLine($"trigger_{index}();  // (inline recursion cap reached)");
                return;
            }

            var nextInlined = new HashSet<int>(inlinedFrom) { index };

            if (header != null)
            {
                sb.Append(pad).Append(header).AppendLine(" {");
                EmitTriggerBodyCSharp(sb, t, index, allConds, allActs, ctx, usage,
                                      allTriggers, triggerRefs, indent + 1, nextInlined);
                sb.Append(pad).AppendLine("}");
            }
            else
            {
                EmitTriggerBodyCSharp(sb, t, index, allConds, allActs, ctx, usage,
                                      allTriggers, triggerRefs, indent, nextInlined);
            }
        }

        // ---- Condition / action emitters (C# expression form) ----------

        private static string FormatConditionsCSharp(List<ConditionEntry> conds, DecompileContext ctx, Dictionary<string, VarSlot> usage)
        {
            var groups = new SortedDictionary<int, List<ConditionEntry>>();
            foreach (var c in conds)
                (groups.TryGetValue(c.OrSequence, out var l) ? l : (groups[c.OrSequence] = new List<ConditionEntry>())).Add(c);

            var andParts = new List<string>();
            foreach (var kv in groups)
            {
                var orParts = new List<string>();
                foreach (var c in kv.Value) orParts.Add(FormatConditionCSharp(c, ctx, usage));
                andParts.Add(orParts.Count == 1 ? orParts[0] : "(" + string.Join(" || ", orParts) + ")");
            }
            return string.Join(" && ", andParts);
        }

        private static string FormatConditionCSharp(ConditionEntry c, DecompileContext ctx, Dictionary<string, VarSlot> usage)
        {
            string prefix = c.IsNot ? "!" : "";
            if (c.Id == 1)  // RVT "Compare" — intrinsic comparison
            {
                if (c.Args.Count >= 3)
                {
                    string lhs = ReplaceSlotRefs(c.Args[0].Render(ctx), usage);
                    string rhs = ReplaceSlotRefs(c.Args[1].Render(ctx), usage);
                    string op = MapComparisonOp(c.Args[2].Render(ctx));
                    return prefix + $"{lhs} {op} {rhs}";
                }
            }
            if (c.Args.Count >= 1)
            {
                string receiver = ReplaceSlotRefs(c.Args[0].Render(ctx), usage);
                string name = GetConditionScriptName(c.Id, c.Name);
                var rest = new List<string>();
                for (int i = 1; i < c.Args.Count; i++) rest.Add(ReplaceSlotRefs(c.Args[i].Render(ctx), usage));
                return prefix + $"{receiver}.{name}({string.Join(", ", rest)})";
            }
            return prefix + GetConditionScriptName(c.Id, c.Name) + "()";
        }

        private static string FormatActionCSharp(ActionEntry a, DecompileContext ctx, Dictionary<string, VarSlot> usage)
        {
            if (a.IsInline) return "/* inline block */";

            // Megl_SET (action 9) → `lhs op= rhs` intrinsic assignment.
            if (a.Id == 9)
            {
                if (a.Args.Count >= 3)
                {
                    string lhs = ReplaceSlotRefs(a.Args[0].Render(ctx), usage);
                    string rhs = ReplaceSlotRefs(a.Args[1].Render(ctx), usage);
                    string op = StripParens(a.Args[2].Render(ctx));
                    return $"{lhs} {MapSetterOp(op)} {rhs}";
                }
            }

            // Megl_RunTrigger (20) → bare call `triggerN()`.
            if (a.Id == 20 && a.Args.Count >= 1)
                return $"{a.Args[0].Render(ctx)}()";

            // Megl_Create / place_at_me (action 2) — special arg layout:
            //   (Type1, OUTObject, PlaceAt, Label, SpawnFlags, LocationOffset, Variant)
            // The OUT slot is param 1 (not the last), so the generic GET-
            // style "last param == OUT*" detection misses it. Render as
            //   OUTObject = PlaceAt.place_at_me(Type1, Label, SpawnFlags, LocationOffset, Variant)
            // so the receiver is the location and the type spawned reads
            // as the first method argument — matching the natural script
            // semantics ("place a warthog at the player's biped").
            if (a.Id == 2 && a.Args.Count >= 3)
            {
                string typeArg = a.Args[0].Render(ctx);
                string outArg  = ReplaceSlotRefs(a.Args[1].Render(ctx), usage);
                string placeAt = ReplaceSlotRefs(a.Args[2].Render(ctx), usage);
                var rest = new List<string> { typeArg };
                for (int i = 3; i < a.Args.Count; i++)
                    rest.Add(ReplaceSlotRefs(a.Args[i].Render(ctx), usage));
                return $"{outArg} = {placeAt}.place_at_me({string.Join(", ", rest)})";
            }

            // Setter form per RVT — action writes a property like .score / .shields.
            //   target.prop op= operand   (for arg layout: target, op, operand)
            //   action 74 (grenades) has layout: player, type, op, operand
            //                                → player.grenades(type) op= operand
            if (_rvtActionSetters.TryGetValue(a.Id, out var propName))
            {
                if (a.Id == 74 && a.Args.Count >= 4)
                {
                    string rcv74 = ReplaceSlotRefs(a.Args[0].Render(ctx), usage);
                    string ty   = a.Args[1].Render(ctx);
                    string op74 = StripParens(a.Args[2].Render(ctx));
                    string operand74 = ReplaceSlotRefs(a.Args[3].Render(ctx), usage);
                    return $"{rcv74}.grenades({ty}) {MapSetterOp(op74)} {operand74}";
                }
                if (a.Args.Count >= 3)
                {
                    string rcv = ReplaceSlotRefs(a.Args[0].Render(ctx), usage);
                    string op = StripParens(a.Args[1].Render(ctx));
                    string operand = ReplaceSlotRefs(a.Args[2].Render(ctx), usage);
                    return $"{rcv}.{propName} {MapSetterOp(op)} {operand}";
                }
            }

            string fn = GetActionScriptName(a.Id, a.Name);
            if (a.Args.Count == 0) return $"{fn}()";

            // GET-style actions: when the LAST param is named "OUT*", emit
            // as `out = receiver.method(other_args)` so the result variable
            // reads as an assignment instead of being buried in the arg list.
            var defForGet = MegaloLookup.GetAction(a.Id, ctx);
            if (defForGet.HasValue && defForGet.Value.Params.Length >= 2)
            {
                int last = defForGet.Value.Params.Length - 1;
                string lastParamName = defForGet.Value.Params[last].Name ?? string.Empty;
                if (lastParamName.StartsWith("OUT", StringComparison.Ordinal)
                    && a.Args.Count == defForGet.Value.Params.Length)
                {
                    string outArg = ReplaceSlotRefs(a.Args[last].Render(ctx), usage);
                    string recv = ReplaceSlotRefs(a.Args[0].Render(ctx), usage);
                    var middle = new List<string>();
                    for (int i = 1; i < last; i++)
                        middle.Add(ReplaceSlotRefs(a.Args[i].Render(ctx), usage));
                    string call = middle.Count == 0
                        ? $"{recv}.{fn}()"
                        : $"{recv}.{fn}({string.Join(", ", middle)})";
                    return $"{outArg} = {call}";
                }
            }

            // Free-function form for actions whose first arg is NOT a
            // method receiver (e.g. send_incident's first param is the
            // Incident index, not a self). Detect by checking the param's
            // declared TypeRef on the action definition.
            var def = MegaloLookup.GetAction(a.Id, ctx);
            bool firstIsReceiver = true;
            if (def.HasValue && def.Value.Params.Length > 0)
            {
                string t = def.Value.Params[0].TypeRef ?? string.Empty;
                if (t.Equals("Enumref:Incident", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Enumref:IncidentIndex", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Enumref:Sound", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Enumref:SoundIndex", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Enumref:NameIndex", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Enumref:NameIndex32", StringComparison.OrdinalIgnoreCase))
                    firstIsReceiver = false;
            }

            if (!firstIsReceiver)
            {
                var allArgs = new List<string>(a.Args.Count);
                foreach (var ar in a.Args)
                    allArgs.Add(ReplaceSlotRefs(ar.Render(ctx), usage));
                return $"{fn}({string.Join(", ", allArgs)})";
            }

            string receiver = ReplaceSlotRefs(a.Args[0].Render(ctx), usage);
            var rest2 = new List<string>();
            for (int i = 1; i < a.Args.Count; i++) rest2.Add(ReplaceSlotRefs(a.Args[i].Render(ctx), usage));
            if (rest2.Count == 0) return $"{receiver}.{fn}()";
            return $"{receiver}.{fn}({string.Join(", ", rest2)})";
        }

        private static string MapSetterOp(string op) => op switch
        {
            "+=" or "Add" => "+=",
            "-=" or "Subtract" => "-=",
            "·=" or "*=" or "Multiply" => "*=",
            "/=" or "Divide" => "/=",
            "=" or "Set" => "=",
            "%=" or "Modulo" => "%=",
            "&=" or "BinaryAND" => "&=",
            "|=" or "BinaryOR" => "|=",
            "^=" or "BinaryXOR" => "^=",
            "<<" or "LeftShift" => "<<=",
            ">>" or "RightShift" => ">>=",
            _ => $"= /*{op}*/",
        };

        // Rewrites "GlobalObject[3]" / "globalnumbers[0]" / etc. as their
        // synthesized C# identifier ("obj3" / "num0") so the body reads
        // naturally with the inline declarations.
        private static string ReplaceSlotRefs(string s, Dictionary<string, VarSlot> usage)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return System.Text.RegularExpressions.Regex.Replace(s,
                @"(GlobalObject|GlobalPlayer|GlobalTeam|GlobalTimer|globalnumbers|globaltimers|globalteams|globalplayers|globalobjects|playernumbers|playertimers|playerteams|playerplayers|playerobjects|objectnumbers|objecttimers|objectteams|objectplayers|objectobjects|teamnumbers|teamtimers|teamteams|teamplayers|teamobjects)\[(\d+)\]",
                m => usage.TryGetValue($"{m.Groups[1].Value}[{m.Groups[2].Value}]", out var slot)
                    ? slot.DeclaredName
                    : m.Value);
        }

        // RVT-documented script names per action ID (sourced from
        // ReachVariantEditor's game_variants/components/megalo/actions.cpp
        // mapping calls). These are the names Reach scripters know and use,
        // so rendering with them keeps the decompile output familiar and
        // documented. Actions that RVT maps as "setters" ("score", "money",
        // "shields", "health") render as property assignments rather than
        // method calls — handled in FormatActionCSharp.
        // Public read-only view so ScriptCompiler can resolve script-form
        // action names back to their canonical action IDs without duplicating
        // the table. Same data as `_rvtActionNames`.
        public static IReadOnlyDictionary<int, string> RvtActionNames => _rvtActionNames;

        private static readonly Dictionary<int, string> _rvtActionNames = new()
        {
            [2]  = "place_at_me",                [3]  = "delete",
            [4]  = "set_waypoint_visibility",    [5]  = "set_waypoint_icon",
            [6]  = "set_waypoint_priority",      [7]  = "set_waypoint_timer",
            [8]  = "set_waypoint_range",         [10] = "set_shape",
            [11] = "apply_traits",               [12] = "set_pickup_permissions",
            [13] = "set_spawn_location_permissions",
            [14] = "set_spawn_location_fireteams",
            [15] = "set_progress_bar",           [16] = "show_message_to",
            [17] = "set_rate",                   [18] = "debug_print",
            [19] = "get_carrier",                [21] = "end_round",
            [22] = "set_shape_visibility",       [23] = "kill",
            [24] = "set_invincibility",          [25] = "rand",
            [26] = "debug_break",                [27] = "get_orientation",
            [28] = "get_speed",                  [29] = "try_get_killer",
            [30] = "try_get_death_damage_type",  [31] = "try_get_death_damage_mod",
            [32] = "debug_set_tracing_enabled",  [33] = "attach_to",
            [34] = "detach",                     [35] = "get_scoreboard_pos",
            [36] = "get_scoreboard_pos",         [37] = "get_spree_count",
            [39] = "set_req_purchase_modes",
            [40] = "get_vehicle",                [41] = "force_into_vehicle",
            [42] = "set_biped",                  [43] = "reset",
            [44] = "set_weapon_pickup_priority", [45] = "push_upward",
            [46] = "set_text",                   [47] = "set_value_text",
            [48] = "set_meter_params",           [49] = "set_icon",
            [50] = "set_visibility",             [51] = "play_sound_for",
            [52] = "set_scale",                  [53] = "set_waypoint_text",
            [56] = "set_objective_text",         [57] = "set_objective_allegiance_name",
            [58] = "set_objective_allegiance_icon",
            [59] = "set_co_op_spawning",         [60] = "set_primary_respawn_object",
            [61] = "set_primary_respawn_object", [62] = "get_fireteam",
            [63] = "set_fireteam",               [66] = "get_distance_to",
            [69] = "set_requisition_palette",    [70] = "set_device_power",
            [71] = "get_device_power",           [72] = "set_device_position",
            [73] = "get_device_position",        [75] = "send_incident",
            [76] = "send_incident",              [77] = "set_loadout_palette",
            [78] = "set_device_animation_position",
            [79] = "animate_device_position",    [80] = "set_device_actual_position",
            [81] = "insert_theater_film_marker", [82] = "enable_spawn_zone",
            [83] = "get_weapon",                 [84] = "get_armor_ability",
            [85] = "set_garbage_collection_disabled",
            [86] = "get_crosshair_target",       [87] = "place_between_me_and",
            [88] = "debug_force_player_view_count",
            [89] = "add_weapon",                 [90] = "set_co_op_spawning",
            [91] = "copy_rotation_from",         [92] = "face_toward",
            [93] = "add_weapon",                 [94] = "remove_weapon",
            [95] = "set_scenario_interpolator_state",
            [96] = "get_random_object",          [97] = "record_griefer_penalty",
            [100] = "hs_function_call",          [101] = "get_button_press_duration",
            [102] = "set_vehicle_spawning_enabled",
            [103] = "set_vehicle_spawning_enabled",
            [104] = "set_respawn_vehicle",       [105] = "set_respawn_vehicle",
            [106] = "set_hidden",
        };

        // Actions where RVT uses a SETTER (property assignment) rather than
        // a method. Renders as `receiver.prop op= operand` in FormatActionCSharp.
        //   1  → target.score   op= operand  (Modify Score)
        //   38 → player.money   op= operand
        //   64 → object.shields op= operand
        //   65 → object.health  op= operand
        //   67 → object.max_shields op= operand
        //   68 → object.max_health  op= operand
        //   74 → player.grenades(type) op= operand  (Modify Player Grenades)
        private static readonly Dictionary<int, string> _rvtActionSetters = new()
        {
            [1]  = "score",
            [38] = "money",
            [64] = "shields",
            [65] = "health",
            [67] = "max_shields",
            [68] = "max_health",
            [74] = "grenades",
        };

        // Public read-only view so ScriptCompiler can resolve script-form
        // condition names back to their canonical condition IDs.
        public static IReadOnlyDictionary<int, string> RvtConditionNames => _rvtConditionNames;

        private static readonly Dictionary<int, string> _rvtConditionNames = new()
        {
            [2]  = "shape_contains",
            [3]  = "killer_type_is",
            [4]  = "has_alliance_status",
            [5]  = "is_zero",
            [6]  = "is_of_type",
            [7]  = "has_any_players",
            [8]  = "is_out_of_bounds",
            [9]  = "is_fireteam_leader",
            [10] = "assisted_kill_of",
            [11] = "has_forge_label",
            [12] = "is_not_respawning",
            [13] = "is_in_use",
            [14] = "is_spartan",
            [15] = "is_elite",
            [16] = "is_monitor",
            [17] = "is_in_forge",
        };

        // Lookup an action's user-facing script name. Prefer RVT's
        // documented names; fall back to stripping the XML "Obj_"/"Megl_"
        // prefix for actions RVT doesn't cover.
        private static string GetActionScriptName(int id, string fallbackName)
        {
            if (_rvtActionNames.TryGetValue(id, out var n)) return n;
            return StripPrefix(fallbackName);
        }
        private static string GetConditionScriptName(int id, string fallbackName)
        {
            if (_rvtConditionNames.TryGetValue(id, out var n)) return n;
            return StripPrefix(fallbackName);
        }

        // Strip XML prefixes like "Obj_" / "Megl_" / "Player_" so the
        // rendered name reads naturally: `Obj_NavVisPerms_SET` → `NavVisPerms_SET`.
        private static string StripPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
            int u = name.IndexOf('_');
            if (u > 0 && u < 7)
            {
                string p = name.Substring(0, u);
                if (p == "Obj" || p == "Megl" || p == "Player" || p == "Players"
                    || p == "Team" || p == "Timer" || p == "Widget" || p == "Game"
                    || p == "DEBUG")
                    return name.Substring(u + 1);
            }
            return name;
        }

        // ---- Declarations block ------------------------------------------

        private static void EmitDeclarationsFromCtx(StringBuilder sb, DecompileContext ctx)
        {
            // Global pool records are stashed in ctx.Tables by the tail walker
            // (CountSection populates both ctx.Tables and per-record scope
            // entries). We inspect them here to emit `declare global.X[N]`
            // lines with network priority.
            EmitGlobalPool(sb, ctx, "GlobalNumbers", "global.number");
            EmitGlobalPool(sb, ctx, "GlobalTimers",  "global.timer");
            EmitGlobalPool(sb, ctx, "GlobalTeams",   "global.team");
            EmitGlobalPool(sb, ctx, "GlobalPlayers", "global.player");
            EmitGlobalPool(sb, ctx, "GlobalObjects", "global.object");

            EmitGlobalPool(sb, ctx, "playernumbers", "player.number");
            EmitGlobalPool(sb, ctx, "playertimers",  "player.timer");
            EmitGlobalPool(sb, ctx, "playerteams",   "player.team");
            EmitGlobalPool(sb, ctx, "playerplayers", "player.player");
            EmitGlobalPool(sb, ctx, "playerobjects", "player.object");

            EmitGlobalPool(sb, ctx, "objectnumbers", "object.number");
            EmitGlobalPool(sb, ctx, "objecttimers",  "object.timer");
            EmitGlobalPool(sb, ctx, "objectteams",   "object.team");
            EmitGlobalPool(sb, ctx, "objectplayers", "object.player");
            EmitGlobalPool(sb, ctx, "objectobjects", "object.object");

            EmitGlobalPool(sb, ctx, "teamnumbers", "team.number");
            EmitGlobalPool(sb, ctx, "teamtimers",  "team.timer");
            EmitGlobalPool(sb, ctx, "teamteams",   "team.team");
            EmitGlobalPool(sb, ctx, "teamplayers", "team.player");
            EmitGlobalPool(sb, ctx, "teamobjects", "team.object");
        }

        private static void EmitGlobalPool(StringBuilder sb, DecompileContext ctx, string tableKey, string declPrefix)
        {
            if (!ctx.Tables.TryGetValue(tableKey, out var entries)) return;
            for (int i = 0; i < entries.Count; i++)
            {
                // Tail walker stored records with field values; we only have
                // the rendered-name list in Tables. Priority information
                // isn't round-tripped here yet — emit "low" as a safe
                // default until we expose the record field map.
                sb.AppendLine($"declare {declPrefix}[{i}] with network priority low");
            }
        }

        // ---- Trigger emission --------------------------------------------

        private static void EmitTriggerMegalo(
            StringBuilder sb,
            TriggerEntry t,
            int index,
            List<ConditionEntry> allConds,
            List<ActionEntry> allActs,
            DecompileContext ctx)
        {
            var conds = new List<ConditionEntry>();
            for (int i = 0; i < t.ConditionCount; i++)
            {
                int idx = t.ConditionOffset + i;
                if (idx >= 0 && idx < allConds.Count) conds.Add(allConds[idx]);
            }
            var acts = new List<ActionEntry>();
            for (int i = 0; i < t.ActionCount; i++)
            {
                int idx = t.ActionOffset + i;
                if (idx >= 0 && idx < allActs.Count) acts.Add(allActs[idx]);
            }

            // Preamble: pick the construct based on TriggerType.
            string header;
            string footer = "end";

            // TriggerType Do wrapping an event attribute.
            if (t.TypeValue == 0)
            {
                string evt = t.AttributeName switch
                {
                    "OnInit" => "init",
                    "OnLocalInit" => "local init",
                    "OnLocal" => "local",
                    "OnHostMigration" => "host migration",
                    "OnObjectDeath" => "object death",
                    "OnPregame" => "pregame",
                    "OnCall" => null!,   // compile-only wrapper, treat as plain do
                    "OnTick" => null!,
                    _ => t.AttributeName,
                };
                header = evt == null ? "do" : $"on {evt}: do";
            }
            else
            {
                // for-each style triggers + labeled + named.
                header = t.TypeName switch
                {
                    "Player" => "for each player do",
                    "RandomPlayer" => "for each random player do",
                    "Team" => "for each team do",
                    "Object" => "for each object do",
                    "Labeled" => $"function trigger_{index}() -- labeled",
                    _ => $"function trigger_{index}() -- type={t.TypeName}",
                };
                if (t.TypeName == "Labeled" || t.TypeName.StartsWith("Unlabel"))
                    footer = "end";

                // Non-default attribute note in a comment.
                if (!string.Equals(t.AttributeName, "OnTick", StringComparison.Ordinal))
                    header += $" -- attr={t.AttributeName}";
            }

            sb.AppendLine(header);
            EmitBody(sb, conds, acts, ctx, indent: 1);
            sb.AppendLine(footer);
        }

        // Emits a trigger body. OR/AND is encoded via each condition's
        // OrSequence: same OrSequence value → OR, different value → AND.
        // Each condition's local ActionOffset gates the actions that follow
        // it within this container.
        private static void EmitBody(
            StringBuilder sb,
            List<ConditionEntry> conds,
            List<ActionEntry> acts,
            DecompileContext ctx,
            int indent)
        {
            // Build a map: actionIndex → condition group that gates it.
            // A condition with ActionOffset=K means "gate actions at K..end".
            // Simpler heuristic: group consecutive conditions by ActionOffset;
            // actions whose local index is >= the first such offset run under
            // that if-block.

            // Bucket conditions by their ActionOffset.
            var gates = new SortedDictionary<int, List<ConditionEntry>>();
            foreach (var c in conds)
            {
                if (!gates.TryGetValue(c.ActionOffset, out var list))
                    gates[c.ActionOffset] = list = new List<ConditionEntry>();
                list.Add(c);
            }

            string pad = new string(' ', indent * 3);

            // Walk actions in order; when the action index hits a gated
            // offset, open an `if ... then` block that runs to the end of
            // the trigger (matches Reach's "no explicit end" condition model).
            bool openIf = false;
            int currentPad = indent;
            for (int i = 0; i < acts.Count; i++)
            {
                if (gates.TryGetValue(i, out var gs))
                {
                    // Close previous if (shouldn't normally happen — if an
                    // earlier if was "to end of trigger"). For safety flush.
                    if (openIf)
                    {
                        sb.Append(new string(' ', currentPad * 3)); sb.AppendLine("end");
                        currentPad--;
                        openIf = false;
                    }

                    sb.Append(pad);
                    sb.Append("if ");
                    sb.Append(FormatConditions(gs, ctx));
                    sb.AppendLine(" then");
                    openIf = true;
                    currentPad = indent + 1;
                    pad = new string(' ', currentPad * 3);
                }

                sb.Append(pad);
                EmitAction(sb, acts[i], ctx);
                sb.AppendLine();
            }

            if (openIf)
            {
                currentPad--;
                sb.Append(new string(' ', currentPad * 3)); sb.AppendLine("end");
            }
        }

        private static string FormatConditions(List<ConditionEntry> conds, DecompileContext ctx)
        {
            // Group by OrSequence: same group = OR, different groups = AND.
            var groups = new SortedDictionary<int, List<ConditionEntry>>();
            foreach (var c in conds)
            {
                if (!groups.TryGetValue(c.OrSequence, out var list))
                    groups[c.OrSequence] = list = new List<ConditionEntry>();
                list.Add(c);
            }

            var andParts = new List<string>();
            foreach (var kv in groups)
            {
                var orParts = new List<string>();
                foreach (var c in kv.Value) orParts.Add(FormatCondition(c, ctx));
                andParts.Add(orParts.Count == 1 ? orParts[0] : "(" + string.Join(" or ", orParts) + ")");
            }
            return string.Join(" and ", andParts);
        }

        // Formats a single condition call in Megalo style. Known binary
        // comparisons (Megl.If) render as `lhs op rhs`; everything else
        // becomes a receiver-style call `obj.is_x(...)` or a plain predicate.
        private static string FormatCondition(ConditionEntry c, DecompileContext ctx)
        {
            string prefix = c.IsNot ? "not " : "";

            // Megl.If: Var1 OP Var2, where args are [Var1, Var2, GetterOperator].
            if (string.Equals(c.Name, "If", StringComparison.OrdinalIgnoreCase) && c.Args.Count >= 3)
            {
                string lhs = c.Args[0].Render(ctx);
                string rhs = c.Args[1].Render(ctx);
                string op = MapComparisonOp(c.Args[2].Render(ctx));
                return prefix + $"{lhs} {op} {rhs}";
            }

            // Predicates like IsOfType(obj, type), HasForgeLabel(obj, label),
            // WasKilled(player, flags) render as `obj.name(rest_args)`.
            if (c.Args.Count >= 1)
            {
                string receiver = c.Args[0].Render(ctx);
                string name = ToSnake(c.Name);
                if (c.Args.Count == 1)
                    return prefix + $"{receiver}.{name}()";
                var rest = new List<string>();
                for (int i = 1; i < c.Args.Count; i++) rest.Add(c.Args[i].Render(ctx));
                return prefix + $"{receiver}.{name}({string.Join(", ", rest)})";
            }

            return prefix + ToSnake(c.Name) + "()";
        }

        private static string MapComparisonOp(string enumToken)
        {
            enumToken = (enumToken ?? "").Replace("(", "").Replace(")", "").Trim();
            if (enumToken.StartsWith("<") && !enumToken.StartsWith("<=")) return "<";
            if (enumToken.StartsWith(">") && !enumToken.StartsWith(">=")) return ">";
            if (enumToken.StartsWith("==")) return "==";
            if (enumToken.StartsWith("<=")) return "<=";
            if (enumToken.StartsWith(">=")) return ">=";
            if (enumToken.StartsWith("!=")) return "!=";
            // Token like "LessThan" / "GreaterThanEquals" etc.
            return enumToken switch
            {
                "LessThan" => "<",
                "GreaterThan" => ">",
                "Equals" => "==",
                "LessThanEquals" => "<=",
                "GreaterThanEquals" => ">=",
                "NotEquals" => "!=",
                _ => $"/*op:{enumToken}*/=="
            };
        }

        private static readonly HashSet<string> _assignSetterOps =
            new(StringComparer.OrdinalIgnoreCase) {
                "Add", "Subtract", "Multiply", "Divide", "Set", "Modulo",
                "BinaryAND", "BinaryOR", "BinaryXOR", "BinaryNOT",
                "LeftShift", "RightShift", "Absolute" };

        // Formats a single action statement.
        //
        //   SetVar(base, operand, op) → "base op= operand"    (Megl.Set)
        //   Kill(obj, suppress)       → "obj.kill(suppress)"
        //   Create(type, OUT, ...)    → "OUT = OUT.create(type, ...)"  (best-effort)
        //
        // Unrecognized shapes fall back to `obj.name(rest_args)` when the
        // first arg is an object reference, else `name(args)`.
        private static void EmitAction(StringBuilder sb, ActionEntry a, DecompileContext ctx)
        {
            if (a.IsInline)
            {
                sb.Append("-- inline action block");
                return;
            }

            // Set: Base, Operand, Operator → "base op= operand" or "base = operand"
            if (string.Equals(a.Name, "Set", StringComparison.OrdinalIgnoreCase) && a.Args.Count >= 3)
            {
                string lhs = a.Args[0].Render(ctx);
                string rhs = a.Args[1].Render(ctx);
                string op = StripParens(a.Args[2].Render(ctx));
                string infix = op switch
                {
                    "+=" or "Add" => "+=",
                    "-=" or "Subtract" => "-=",
                    "·=" or "Multiply" => "*=",
                    "*=" => "*=",
                    "/=" or "Divide" => "/=",
                    "=" or "Set" => "=",
                    "%=" or "Modulo" => "%=",
                    "&=" or "BinaryAND" => "&=",
                    "|=" or "BinaryOR" => "|=",
                    "^=" or "BinaryXOR" => "^=",
                    "~=" or "BinaryNOT" => "~=",
                    "<<" or "LeftShift" => "<<=",
                    ">>" or "RightShift" => ">>=",
                    "abs" or "Absolute" => "= abs",
                    _ => "= /*op:" + op + "*/"
                };
                sb.Append(lhs).Append(' ').Append(infix).Append(' ').Append(rhs);
                return;
            }

            // SetScore(targets, op, operand)  →  targets.score op= operand
            if (string.Equals(a.Name, "SetScore", StringComparison.OrdinalIgnoreCase) && a.Args.Count >= 3)
            {
                string tgt = a.Args[0].Render(ctx);
                string op = StripParens(a.Args[1].Render(ctx));
                string operand = a.Args[2].Render(ctx);
                string infix = op switch
                {
                    "+=" or "Add" => "+=",
                    "-=" or "Subtract" => "-=",
                    "=" or "Set" => "=",
                    _ => "= /*op:" + op + "*/"
                };
                sb.Append(tgt).Append(".score ").Append(infix).Append(' ').Append(operand);
                return;
            }

            // Generic: first arg is likely the receiver.
            string fnName = ToSnake(a.Name);
            if (a.Args.Count == 0) { sb.Append(fnName).Append("()"); return; }

            string receiver0 = a.Args[0].Render(ctx);
            if (a.Args.Count == 1)
            {
                sb.Append(receiver0).Append('.').Append(fnName).Append("()");
                return;
            }
            var rest = new List<string>();
            for (int i = 1; i < a.Args.Count; i++) rest.Add(a.Args[i].Render(ctx));
            sb.Append(receiver0).Append('.').Append(fnName).Append('(').Append(string.Join(", ", rest)).Append(')');
        }

        private static string StripParens(string s)
        {
            s = (s ?? "").Trim();
            if (s.StartsWith("(") && s.Contains(")"))
            {
                int close = s.IndexOf(')');
                if (close >= 0) return s.Substring(1, close - 1);
            }
            return s;
        }

        // Converts PascalCase/MixedCase action names to snake_case.
        private static string ToSnake(string name)
        {
            if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
            var sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0
                        && (char.IsLower(name[i - 1])
                            || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        // Internal shared parse — returns the raw parsed structures plus
        // the populated DecompileContext (ctx.Labels etc.) so either
        // renderer can consume them.
        private static (DecompileContext ctx,
                        List<ConditionEntry> conditions,
                        List<ActionEntry> actions,
                        List<TriggerEntry> triggers,
                        MegaloVarsInfo vars,
                        int bitsConsumed) ParseScript(string scriptBits, List<string>? labelNames,
                                                       GameVariant game = GameVariant.Reach,
                                                       List<string>? stringTable = null)
        {
            var br = new BitReader(scriptBits);
            var ctx = new DecompileContext { Labels = labelNames, Game = game, StringTable = stringTable };

            int conditionCount = br.ReadUInt(10);
            var conditions = new List<ConditionEntry>(conditionCount);
            for (int i = 0; i < conditionCount; i++)
                conditions.Add(ParseCondition(br, ctx));

            int actionCount = br.ReadUInt(11);
            var actions = new List<ActionEntry>(actionCount);
            for (int i = 0; i < actionCount; i++)
                actions.Add(ParseAction(br, ctx));

            int triggerCount = br.ReadUInt(ctx.Game == GameVariant.H2A ? 8 : 9);
            var triggers = new List<TriggerEntry>(triggerCount);
            for (int i = 0; i < triggerCount; i++)
                triggers.Add(ParseTrigger(br, ctx));

            // Per RVT (multiplayer.cpp), the post-trigger read order is:
            //   triggers → stats → MegaloVars → widgets → entryPoints
            //           → usedMPObjectTypes → forgeLabels → …
            // Stats sit BETWEEN triggers and MegaloVars, so we have to
            // consume them first or our MegaloVars cursor ends up inside
            // the stats records.
            //
            // Stats: count = bitcount(max_script_stats=4) = 3 bits.
            //        Per record (per Reach enums.xml:1283-1288):
            //            stringindex(7) + format(2) + sortorder(2) + groupbyteam(1) = 12 bits.
            if (ctx.Game != GameVariant.H2A)
            {
                int statsCount = br.ReadUInt(3);
                for (int i = 0; i < statsCount * 12; i++) br.ReadUInt(1);
            }

            // MegaloVars (Reach only for now). Populates each pool's slot
            // count, locality (network priority), and timer initial values.
            var vars = ctx.Game == GameVariant.H2A
                ? new MegaloVarsInfo()
                : ParseReachMegaloVars(br, ctx);

            ScriptTailReader.Read(br, MegaloSchema.ReachScriptTail, ctx);

            return (ctx, conditions, actions, triggers, vars, br.Position);
        }

        // Parsed MegaloVars block (Reach). Each pool holds N declared slots,
        // each with a locality (network priority) — except timers, which
        // have an initial value but no locality. 2-bit locality codes:
        //   0=default, 1=low, 2=high, 3=local.
        public sealed class MegaloVarsInfo
        {
            public sealed class Slot
            {
                public int Locality = -1;       // -1 = not applicable (timers)
                public string? InitialValue;    // rendered initializer (numbers/timers/teams)
            }
            public readonly Dictionary<string, List<Slot>> Pools =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private static MegaloVarsInfo ParseReachMegaloVars(BitReader br, DecompileContext ctx)
        {
            // Pool layouts per Reach enums.xml:1290-1367.
            (string Name, int CountBits, string Kind)[] pools =
            {
                ("globalnumbers", 4, "number"),
                ("globaltimers",  4, "timer"),
                ("globalteams",   4, "team"),
                ("globalplayers", 4, "player"),
                ("globalobjects", 5, "object"),
                ("playernumbers", 4, "number"),
                ("playertimers",  3, "timer"),
                ("playerteams",   3, "team"),
                ("playerplayers", 3, "player"),
                ("playerobjects", 3, "object"),
                ("objectnumbers", 4, "number"),
                ("objecttimers",  3, "timer"),
                ("objectteams",   2, "team"),
                ("objectplayers", 3, "player"),
                ("objectobjects", 3, "object"),
                ("teamnumbers",   4, "number"),
                ("teamtimers",    3, "timer"),
                ("teamteams",     3, "team"),
                ("teamplayers",   3, "player"),
                ("teamobjects",   3, "object"),
            };

            // Per-slot layout per RVT VariableDeclaration::read:
            //   if has_initial_value:
            //       team    : team.read()              — single 4-bit team index
            //       else    : OpcodeArgValueScalar::read() — full NumericTypeRef variant
            //   if has_network_type:
            //       networking.read()                  — 2-bit locality
            //
            //   has_initial_value ⇔ scalar/timer/team
            //   has_network_type  ⇔ scalar/team/player/object  (NOT timer)
            var info = new MegaloVarsInfo();
            foreach (var (name, countBits, kind) in pools)
            {
                int n = br.ReadUInt(countBits);
                var list = new List<MegaloVarsInfo.Slot>(n);
                for (int i = 0; i < n; i++)
                {
                    var s = new MegaloVarsInfo.Slot();
                    if (kind == "number")
                    {
                        s.InitialValue = ReadSchemaEnum(br, "NumericTypeRef", ctx).Render(ctx);
                        s.Locality = br.ReadUInt(2);
                    }
                    else if (kind == "timer")
                    {
                        s.InitialValue = ReadSchemaEnum(br, "NumericTypeRef", ctx).Render(ctx);
                    }
                    else if (kind == "team")
                    {
                        s.InitialValue = $"team_{br.ReadUInt(4)}";
                        s.Locality = br.ReadUInt(2);
                    }
                    else
                    {
                        s.Locality = br.ReadUInt(2);
                    }
                    list.Add(s);
                }
                info.Pools[name] = list;
            }
            return info;
        }

        // Per RVT: variable_network_priority { none=0, low=1, high=2 }.
        // Reach's UI/source terminology renders `none` as `local` (the
        // variable doesn't sync over the network — local-only).
        private static string LocalityName(int loc) => loc switch
        {
            0 => "local",
            1 => "low",
            2 => "high",
            _ => "low",
        };

        /// <summary>
        /// Locate the MegaloVars segment within the scriptBits stream.
        /// Returns (start, end) bit offsets where end-exclusive is the
        /// first bit AFTER the 20-pool MegaloVars block. Reach-only.
        /// Caller uses this to splice in re-encoded MegaloVars while
        /// preserving everything before (triggers, stats) and after
        /// (HUD widgets, entries, MP types, forge labels, string table)
        /// verbatim.
        /// </summary>
        /// <summary>
        /// Reads the Reach forge Labels section by walking forward from the
        /// MegaloVars end through widgets / entryPoints / usedMPObjectTypes
        /// per RVT's authoritative mpvr layout (multiplayer.cpp:357-403).
        /// Returns the resolved label-name list (English from stringTable,
        /// or empty string for undefined entries). Returns null if the
        /// stream desyncs.
        ///
        /// Bit-widths source: ReachVariantTool/native/src/...
        ///   max_script_widgets = 4 → count = bitcount(4) = 3 bits
        ///   HUDWidgetDeclaration: position(4) = 4 bits/record
        ///   TriggerEntryPoints: 7 × bitcount(max_triggers=320)=9 = 63 bits
        ///   ReachGameVariantUsedMPObjectTypeList: 2048-bit bitset
        ///   max_script_labels = 16 → count = bitcount(16) = 5 bits
        ///   ReachForgeLabel record:
        ///     MegaloStringIndexOptional (8 bits: 7 + 1 presence)
        ///     requirements flags (3 bits): 0x01 obj_type, 0x02 team, 0x04 num
        ///       conditional: requiredObjectType (12 bits) if 0x01
        ///       conditional: requiredTeam (5 bits, signed) if 0x02
        ///       conditional: requiredNumber (16 bits) if 0x04
        ///     mapMustHaveAtLeast (7 bits)
        /// </summary>
        public static List<string>? ReadReachForgeLabels(string scriptBits, List<string>? stringTable, GameVariant game = GameVariant.Reach)
        {
            if (game == GameVariant.H2A) return null;
            var (_, varsEnd) = LocateReachMegaloVars(scriptBits, game);
            if (varsEnd <= 0 || varsEnd >= scriptBits.Length) return null;

            int pos = varsEnd;
            int Read(int n)
            {
                int v = 0;
                for (int i = 0; i < n; i++)
                {
                    if (pos >= scriptBits.Length) throw new InvalidOperationException("EOF");
                    v = (v << 1) | (scriptBits[pos++] - '0');
                }
                return v;
            }

            try
            {
                // Widgets: 3-bit count + N × 4-bit position.
                int widgetCount = Read(3);
                pos += widgetCount * 4;

                // EntryPoints: 7 trigger indices × 9 bits each = 63 bits.
                pos += 63;

                // UsedMPObjectTypes: 2048-bit bitset (64 dwords × 32 bits).
                pos += 2048;

                // Forge labels: 5-bit count + N variable-length records.
                int labelCount = Read(5);
                if (labelCount < 0 || labelCount > 16) return null;

                var names = new List<string>(labelCount);
                for (int i = 0; i < labelCount; i++)
                {
                    // MegaloStringIndexOptional is `bitnumber<7, uint8_t,
                    // offset=true>` — NO presence bit; the third template
                    // parameter is `offset`, which means the stored value
                    // is incremented by 1 before save. Read 7 bits raw and
                    // subtract 1: stored 0 → memory -1 (undefined sentinel),
                    // stored N → memory N-1 (real StringTable index).
                    int raw = Read(7);
                    int sid = raw - 1;

                    int reqs = Read(3);
                    if ((reqs & 0x01) != 0) pos += 12; // requiredObjectType
                    if ((reqs & 0x02) != 0) pos += 5;  // requiredTeam
                    if ((reqs & 0x04) != 0) pos += 16; // requiredNumber
                    pos += 7;                          // mapMustHaveAtLeast

                    string name = string.Empty;
                    if (sid >= 0 && stringTable != null && sid < stringTable.Count)
                        name = stringTable[sid] ?? string.Empty;
                    names.Add(name);
                }
                return names;
            }
            catch
            {
                return null;
            }
        }

        public static (int Start, int End) LocateReachMegaloVars(string scriptBits, GameVariant game = GameVariant.Reach)
        {
            if (string.IsNullOrWhiteSpace(scriptBits)) return (0, 0);
            var br = new BitReader(scriptBits);
            var ctx = new DecompileContext { Game = game };

            int conditionCount = br.ReadUInt(10);
            for (int i = 0; i < conditionCount; i++) ParseCondition(br, ctx);
            int actionCount = br.ReadUInt(11);
            for (int i = 0; i < actionCount; i++) ParseAction(br, ctx);
            int triggerCount = br.ReadUInt(game == GameVariant.H2A ? 8 : 9);
            for (int i = 0; i < triggerCount; i++) ParseTrigger(br, ctx);

            // Stats (Reach only): 3-bit count + 12 bits/record.
            if (game != GameVariant.H2A)
            {
                int statsCount = br.ReadUInt(3);
                for (int i = 0; i < statsCount * 12; i++) br.ReadUInt(1);
            }

            int start = br.Position;
            // Walk MegaloVars using the same pool-by-pool layout as
            // ParseReachMegaloVars. We discard the data — we only want
            // the resulting bit position.
            ParseReachMegaloVars(br, ctx);
            int end = br.Position;
            return (start, end);
        }


        /// <summary>
        /// Full decompile producing the rendered text plus the number of
        /// bits consumed (so GametypeReader can resume the outer walk past
        /// the megl block) and the discovered label table.
        /// </summary>
        public static DecompileResult DecompileDetailed(string scriptBits, List<string>? labelNames = null, GameVariant game = GameVariant.Reach, List<string>? stringTable = null)
        {
            var rr = new DecompileResult();
            if (string.IsNullOrWhiteSpace(scriptBits))
                return rr;

            var br = new BitReader(scriptBits);
            var ctx = new DecompileContext { Labels = labelNames, Game = game, StringTable = stringTable };

            // Header counts (Reach)
            // Counts per authoritative Reach megalo spec (var enums.xml):
            //   ConditionCount : 10 bits (max 512)    — at MegaloScript start
            //   ActionCount    : 11 bits (max 1024)   — AFTER all conditions
            //   TriggerCount   :  9 bits (max 320)    — AFTER all actions
            //
            // NOT stored contiguously — each count is immediately followed
            // by that many records, so the three count headers are
            // separated by the records themselves.
            int conditionCount = br.ReadUInt(10);
            var conditions = new List<ConditionEntry>(conditionCount);
            for (int i = 0; i < conditionCount; i++)
                conditions.Add(ParseCondition(br, ctx));

            int actionCount = br.ReadUInt(11);
            var actions = new List<ActionEntry>(actionCount);
            for (int i = 0; i < actionCount; i++)
                actions.Add(ParseAction(br, ctx));

            // H2A TriggerCount is 8 bits (max 128); Reach is 9 bits (max 320).
            int triggerCount = br.ReadUInt(ctx.Game == GameVariant.H2A ? 8 : 9);
            var triggers = new List<TriggerEntry>(triggerCount);
            for (int i = 0; i < triggerCount; i++)
                triggers.Add(ParseTrigger(br, ctx));

            int triggerEndPos = br.Position;
            rr.TriggerSectionBits = triggerEndPos;
            int remaining = br.RemainingBits;

            // Walk the post-trigger tail using the declarative ReachScriptTail
            // layout. Any UnverifiedSection halts the walk and records a
            // diagnostic; anything parsed up to that point populates ctx
            // (labels, hud widgets, script options, …) and can be rendered.
            var tail = ScriptTailReader.Read(br, MegaloSchema.ReachScriptTail, ctx);

            // Render — fast path, used by the legacy `Decompile()` wrapper
            // and the GametypeReader walker. Uses the lightweight per-
            // trigger renderer; the heavy C#/auto-variable pipeline is
            // only invoked via the explicit DecompileAsScript / "Decompile
            // Script" button, never on every file selection (which would
            // freeze the UI thread on large Reach gametypes).
            var sb = new StringBuilder(64 * 1024);
            string gameLabel = ctx.Game == GameVariant.H2A ? "H2A" : "Reach";
            sb.AppendLine($"// Decompiled Megalo Script ({gameLabel})");
            sb.AppendLine($"// Conditions: {conditionCount}, Actions: {actionCount}, Triggers: {triggerCount}");
            sb.AppendLine($"// Bits after triggers: {remaining}");
            if (tail.Diagnostics.Count > 0)
            {
                foreach (var d in tail.Diagnostics)
                    sb.AppendLine($"// [tail] {d}");
            }
            sb.AppendLine($"// Tail consumed {br.Position - triggerEndPos} bits of {remaining}.");
            sb.AppendLine();

            if (ctx.Labels != null && ctx.Labels.Count > 0)
            {
                sb.AppendLine("// Forge labels:");
                for (int i = 0; i < ctx.Labels.Count; i++)
                {
                    var name = ctx.Labels[i];
                    sb.AppendLine($"//   [{i}] {(string.IsNullOrEmpty(name) ? "<unnamed>" : name)}");
                }
                sb.AppendLine();
            }

            for (int i = 0; i < triggers.Count; i++)
            {
                RenderTrigger(sb, triggers[i], i, conditions, actions, ctx);
                sb.AppendLine();
            }

            rr.Text = sb.ToString();
            rr.BitsConsumed = br.Position;
            rr.ConditionCount = conditionCount;
            rr.ActionCount = actionCount;
            rr.TriggerCount = triggerCount;
            rr.Labels = ctx.Labels;
            return rr;
        }

        // ---- Parsing -------------------------------------------------------

        // When non-null, ParseCondition appends a one-line trace per record
        // so callers can diff bit consumption against the authoritative XML.
        public static StringBuilder? ConditionTrace;

        private static ConditionEntry ParseCondition(BitReader br, DecompileContext ctx)
        {
            // Layout per authoritative var enums.xml ConditionCount record.
            // Note: Type has offset="3" — per the Megalograph XML reader
            // (code/BitReader.cs case "Enum" with offset != -1), this
            // DEFERS the variant's children fields so they are read AFTER
            // the other inline fields of the record. So the correct order
            // for one condition is:
            //
            //   Type            5 bits
            //   NOT             1 bit
            //   ORsequence      9 bits (Reach) / 10 (H2A)
            //   ConditionOffset 10 bits (Reach) / 11 (H2A)
            //   (deferred) variant params — Var1, Var2, Operator for Megl.If, etc.
            int start = br.Position;
            int id = br.ReadUInt(H2ABits("ConditionType", 5, ctx));
            bool isNot = br.ReadUInt(1) != 0;
            bool h2a = ctx.Game == GameVariant.H2A;
            int orSeq = br.ReadUInt(h2a ? 10 : 9);
            int actionOffset = br.ReadUInt(h2a ? 11 : 10);
            int headerBits = br.Position - start;

            var def = MegaloLookup.GetCondition(id, ctx);
            var args = new List<ParamNode>();
            int payloadStart = br.Position;
            if (def.HasValue)
            {
                foreach (var p in def.Value.Params)
                    args.Add(ReadParam(br, p.TypeRef, ctx));
            }
            int payloadBits = br.Position - payloadStart;

            ConditionTrace?.AppendLine(
                $"  @{start,6} id={id,2} ({def?.Name ?? "?"}) not={(isNot?1:0)} or={orSeq} coff={actionOffset}"
                + $"  hdr={headerBits}b payload={payloadBits}b total={br.Position - start}b");

            return new ConditionEntry(id, def?.Name ?? $"Cond{id}", isNot, orSeq, actionOffset, args);
        }

        // When non-null, ParseAction appends a one-line trace per record so
        // callers can diff action bit consumption against the authoritative XML.
        public static StringBuilder? ActionTrace;

        private static ActionEntry ParseAction(BitReader br, DecompileContext ctx)
        {
            int start = br.Position;
            int id = br.ReadUInt(H2ABits("ActionType", 7, ctx));
            int hdrBits = br.Position - start;

            // Inline / VirtualTrigger — "smaller trigger" body that
            // references its own (cond_range, act_range) into the global
            // conditions/actions pools.
            //   Reach: action ID 99 with 9+10+10+11 = 40-bit body
            //          (per ScriptCompiler.BuildInlineBinary).
            //   H2A:   action ID 32 with 10+10+11+11 = 42-bit body
            //          (per Halo 2A var enums.xml VirtualTriggerRef).
            // The renderer expands both as synthetic subtriggers so the
            // compartmentalized conditions stay scoped to their actions.
            bool h2a = ctx.Game == GameVariant.H2A;
            if (!h2a && id == 99)
            {
                int condOff   = br.ReadUInt(9);
                int condCount = br.ReadUInt(10);
                int actOff    = br.ReadUInt(10);
                int actCount  = br.ReadUInt(11);
                ActionTrace?.AppendLine(
                    $"  @{start,6} id={id,3} (Inline)  condOff={condOff} condCount={condCount} actOff={actOff} actCount={actCount}");
                return ActionEntry.Inline(id, condOff, condCount, actOff, actCount);
            }
            if (h2a && id == 32)
            {
                int condOff   = br.ReadUInt(10);
                int condCount = br.ReadUInt(10);
                int actOff    = br.ReadUInt(11);
                int actCount  = br.ReadUInt(11);
                ActionTrace?.AppendLine(
                    $"  @{start,6} id={id,3} (VirtualTrigger)  condOff={condOff} condCount={condCount} actOff={actOff} actCount={actCount}");
                return ActionEntry.Inline(id, condOff, condCount, actOff, actCount);
            }

            var def = MegaloLookup.GetAction(id, ctx);
            var args = new List<ParamNode>();

            int payloadStart = br.Position;
            if (def.HasValue)
            {
                foreach (var p in def.Value.Params)
                    args.Add(ReadParam(br, p.TypeRef, ctx));
            }
            int payloadBits = br.Position - payloadStart;

            ActionTrace?.AppendLine(
                $"  @{start,6} id={id,3} ({def?.Name ?? "?"})  hdr={hdrBits}b payload={payloadBits}b total={br.Position - start}b");

            return new ActionEntry(id, def?.Name ?? $"Act{id}", args);
        }

        private static TriggerEntry ParseTrigger(BitReader br, DecompileContext ctx)
        {
            // Layout per authoritative var enums.xml TriggerCount record +
            // Megalograph runstack semantics: offset="1" on Type means the
            // variant's children (LabelRef for ForeachObjectWithLabel) are
            // read after the FIRST sibling following Type — i.e. between
            // Attribute and ConditionOffset. My earlier "at end" placement
            // was incorrect.
            //
            //   Type             3 bits
            //   Attribute        3 bits
            //   (deferred) LabelRef if Type == 101   ← HERE per runstack
            //   ConditionOffset  9 bits
            //   ConditionCount  10 bits
            //   ActionOffset    10 bits
            //   ActionCount     11 bits
            // H2A widens Attribute 3→4, adds FilterRef variant (type 6),
            // widens CondOff 9→10, ActOff 10→11, and appends Unknown1+Unknown2
            // (8 bits each). Halo 2A/var enums.xml:1966-2003.
            // ManagedMegalo.dll c_trigger::decode @ RVA 0x707A0 (verified):
            //   1st read: 3 bits → struct +8 = execution_mode (Attribute)
            //                       (range 0-6: OnTick..OnPregame)
            //   2nd read: 4 bits → struct +9 = trigger_type
            //                       (range 0-8: Do..ForEachObjectWithFilter)
            // Megalograph XML labels are SWAPPED — wire format reads
            // Attribute FIRST then Type, opposite of XML's visual order.
            // Wire-format read order differs per game:
            //   Reach: Type(3) → Attribute(3) → [LabelRef if type=5] → cond/act fields.
            //          Confirmed by enums.xml (TriggerCount, lines 1253-1280) and
            //          ScriptCompiler.WriteTrigger (line 2254-2263) which emits
            //          triggerTypeBinary FIRST then triggerAttributeBinary.
            //   H2A:   Attribute(3) → Type(4) → [LabelRef|FilterRef] → … per
            //          ManagedMegalo c_trigger::decode IL (verified).
            bool h2a = ctx.Game == GameVariant.H2A;
            int typeVal, attrVal;
            if (h2a)
            {
                attrVal = br.ReadUInt(3);
                typeVal = br.ReadUInt(4);
            }
            else
            {
                typeVal = br.ReadUInt(3);
                attrVal = br.ReadUInt(3);
            }

            // Capture the trigger's optional Label/Filter ref so the
            // foreach header can render `with label "name"`.
            string? labelText = null;
            if (typeVal == 5)
                labelText = ReadSchemaEnum(br, "LabelRef", ctx)?.Render(ctx);
            else if (h2a && typeVal == 6)
                labelText = ReadSchemaEnum(br, "FilterRef", ctx)?.Render(ctx);

            // Empirical: H2A trigger record stores COUNT before OFFSET
            // (opposite of Megalograph XML's Var ordering). Verified on
            // decomptest: 1 cond/2 acts/2 triggers, where trigger 0 uses
            // cond[0] (count=1, off=0) and acts[0..1] (count=2, off=0).
            // Empirical from decomptest: Count is read BEFORE Offset (XML
            // listing order is misleading), with widths 9/10/11/10.
            // ManagedMegalo c_trigger::decode body confirms 2 trailing
            // 8-bit reads at end (read_index<255>-1).
            int condCount, condOffset, actCount, actOffset;
            if (h2a)
            {
                // Per ManagedMegalo c_static_stack<T,N> sizes:
                //   c_static_stack<c_condition, 576>  → cond fields = 10 bits
                //   c_static_stack<c_action, 1088>    → act  fields = 11 bits
                // Each offset uses read_index<MAX>-1 (subtract 1 after read,
                // 0 means "none"/-1). Counts are raw uints. Order: offset,
                // count, offset, count.
                condOffset = br.ReadUInt(10) - 1;
                condCount  = br.ReadUInt(10);
                actOffset  = br.ReadUInt(11) - 1;
                actCount   = br.ReadUInt(11);
            }
            else
            {
                condOffset = br.ReadUInt(9);
                condCount  = br.ReadUInt(10);
                actOffset  = br.ReadUInt(10);
                actCount   = br.ReadUInt(11);
            }
            int unk1 = h2a ? br.ReadUInt(8) : 0;
            int unk2 = h2a ? br.ReadUInt(8) : 0;

            string typeName = typeVal switch
            {
                0 => "Do",
                1 => "ForeachPlayer",
                2 => "ForeachPlayerRandomly",
                3 => "ForeachTeam",
                4 => "ForeachObject",
                5 => "ForeachObjectWithLabel",
                6 => h2a ? "ForeachObjectWithFilter" : $"Unlabelled{typeVal}",
                _ => $"Unlabelled{typeVal}",
            };
            string attrName = attrVal switch
            {
                0 => "OnTick",
                1 => "OnCall",
                2 => "OnInit",
                3 => "OnLocalInit",
                4 => "OnHostMigration",
                5 => "OnObjectDeath",
                6 => "OnLocal",
                7 => "OnPregame",
                8 => h2a ? "OnIncident" : $"attr{attrVal}",
                _ => $"attr{attrVal}",
            };

            return new TriggerEntry(typeVal, attrVal, typeName, attrName,
                                    condOffset, condCount, actOffset, actCount,
                                    unk1, unk2, labelText);
        }

        // ---- Rendering -----------------------------------------------------

        // RenderTrigger is the legacy debug renderer — produces the
        // splattering-of-comments form with cond/act offset diagnostics.
        // The user-facing UI text now goes through RenderAsCSharpScript
        // (the auto-variable-managed C# pipeline). Keep this around so the
        // standalone `Decompile()` debug helper still produces its raw form.
        private static void RenderTrigger(StringBuilder sb, TriggerEntry t, int index, List<ConditionEntry> allConds, List<ActionEntry> allActs, DecompileContext ctx)
        {
            sb.AppendLine($"Trigger {t.TypeName}()  // attr: {t.AttributeName}");
            sb.AppendLine("{");
            sb.AppendLine($"    // cond: off={t.ConditionOffset}, count={t.ConditionCount}");
            sb.AppendLine($"    // act : off={t.ActionOffset}, count={t.ActionCount}");
            sb.AppendLine($"    // unk : {t.Unknown1}, {t.Unknown2}");
            sb.AppendLine();

            sb.AppendLine("    // Conditions:");
            for (int i = 0; i < t.ConditionCount; i++)
            {
                int idx = t.ConditionOffset + i;
                if (idx < 0 || idx >= allConds.Count)
                {
                    sb.AppendLine($"    //  [{idx}] <out of range>");
                    continue;
                }
                var c = allConds[idx];
                sb.Append("    //  [").Append(idx).Append("] ");
                sb.Append(c.IsNot ? "NOT " : "");
                sb.Append(c.Name);
                if (c.Args.Count > 0)
                    sb.Append("(").Append(RenderArgs(c.Args, ctx)).Append(")");
                sb.Append($"  // or={c.OrSequence} actionOff={c.ActionOffset}");
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("    // Actions:");
            for (int i = 0; i < t.ActionCount; i++)
            {
                int idx = t.ActionOffset + i;
                if (idx < 0 || idx >= allActs.Count)
                {
                    sb.AppendLine($"    //  [{idx}] <out of range>");
                    continue;
                }
                var a = allActs[idx];
                sb.Append("    ");
                RenderActionStatement(sb, a, indent: 1, ctx);
                sb.AppendLine();
            }

            sb.AppendLine("}");
        }

        private static void RenderActionStatement(StringBuilder sb, ActionEntry a, int indent, DecompileContext ctx)
        {
            if (a.IsInline)
            {
                sb.AppendLine($"// inline (id={a.Id}) condOff={a.InlineCondOffset} condCount={a.InlineCondCount} actOff={a.InlineActOffset} actCount={a.InlineActCount}");
                return;
            }
            sb.Append(a.Name);
            sb.Append("(");
            sb.Append(RenderArgs(a.Args, ctx));
            sb.Append(");");
        }

        private static string RenderArgs(List<ParamNode> args, DecompileContext ctx)
        {
            if (args.Count == 0) return string.Empty;
            var parts = new string[args.Count];
            for (int i = 0; i < args.Count; i++)
                parts[i] = args[i].Render(ctx);
            return string.Join(", ", parts);
        }

        // ---- Param decoding ------------------------------------------------
        //
        // Every composite "TypeRef" (VarType, NumericTypeRef, PlayerTypeRef,
        // ObjectTypeRef, TeamTypeRef, TimerTypeRef, LabelRef, …) has a
        // variant-dependent bit layout. MegaloSchema.Enums is the ground
        // truth for those widths and for which sub-fields each variant owns,
        // so we walk it instead of hand-coding widths (which drifted from
        // the schema and desynced on anything beyond the trivial subset).
        //
        // Parsing is DEFERRED: ReadParam returns a ParamNode tree that keeps
        // Ref0 indices unresolved until render time. This lets the tail reader
        // populate lookup tables (Labels, hud widgets, script options, …)
        // before the condition/action arg lists are stringified.

        private static ParamNode ReadParam(BitReader br, string typeRef, DecompileContext ctx)
        {
            if (string.IsNullOrWhiteSpace(typeRef))
                return new LiteralNode("0");

            string tr = typeRef.Trim();

            // Vector3 is not represented in MegaloSchema; compiler writes 8s+8s+8s.
            if (Eq(tr, "Enumref:Vector3")) return ReadVector3(br);

            if (tr.StartsWith("Enumref:", StringComparison.OrdinalIgnoreCase))
            {
                string enumName = tr.Substring("Enumref:".Length).Trim();
                return ReadSchemaEnum(br, enumName, ctx);
            }

            // Unknown TypeRef prefix — consume 1 bit so we don't infinite-loop.
            int fallback = br.ReadUInt(1);
            return new LiteralNode($"{fallback}/*{tr}*/");
        }

        // Reads a value of an enum named by the schema. Uses MegaloSchema first
        // (correctly handles variant-dependent widths), falls back to reflection
        // over project enums (byte-width from max value) for simple enums not in
        // the schema.
        // Public read-only view so ScriptCompiler can resolve bit widths
        // for TypeRefs that aren't in MegaloSchema.Enums (Sound, Incident,
        // ObjectType, NameIndex, …). Same data as `_externalEnumBits`.
        public static IReadOnlyDictionary<string, int> ExternalEnumBits => _externalEnumBits;

        // Authoritative bit widths for TypeRefs not in MegaloSchema.Enums —
        // sourced from the canonical var enums.xml (Halo reach folder of the
        // Megalograph repo). Some of these are `type="External"` in the XML
        // (ObjectType, NameIndex) so the width has to come from the external
        // reference; others are simple `type="Int"` at the referenced scope.
        private static readonly Dictionary<string, int> _externalEnumBits =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TriggerRef"]      = 9,
                ["Sound"]           = 7,
                ["WidgetIcon"]      = 7,
                ["Incident"]        = 10,
                ["ObjectType"]      = 12,   // objects.xml — Reach standard width
                ["NameIndex"]       = 8,    // names.xml  — Reach standard width
                ["ScriptTraits"]    = 4,    // Ref0 into ScriptedPlayerTraits
                ["LoadoutRef"]      = 3,   // Reach enums.xml:594 — Int bits=3 (had 5; +2-bit drift)
                ["ReqPalette"]      = 4,
                ["DeathFlags"]      = 5,   // RVT KillerTypeFlags — bitfield of 5 flags (guardians, suicide, kill, betrayal, quit)
                ["TeamDisposition"] = 2,
                ["GameIcon"]        = 8,   // Reach enums.xml:593 — Int bits=8 (had 5; +3-bit drift)
                ["FireteamFilter"]  = 8,   // Reach enums.xml:489 — Container of 8 Bools = 8 bits (had 3; +5-bit drift)
                ["PurchaseMode"]    = 5,   // Reach enums.xml:559 — Container of 5 Bools = 5 bits (had 1; +4-bit drift)
            };

        private static ParamNode ReadSchemaEnum(BitReader br, string enumName, DecompileContext ctx)
        {
            // H2A-specific enum shapes (notably NumericTypeRef, whose
            // variants shift vs Reach) take precedence.
            if (ctx.Game == GameVariant.H2A
                && MegaloSchema.H2AEnums.TryGetValue(enumName, out var h2aEd))
                return ReadSchemaEnum(br, h2aEd, ctx);

            if (MegaloSchema.TryGetEnum(enumName, out var ed))
                return ReadSchemaEnum(br, ed, ctx);

            // Tokens{1,2,3} are Containers per var enums.xml:
            //   Tokens1 = String + StringVars1
            //   Tokens2 = String + StringVars2
            //   Tokens3 = String + StringVars3
            // H2A: String = 8 bits (Ref1:Stringtable); Reach: 7 bits.
            // StringVars1 = 1 bit (H2A), StringVars2 = 2 bits, StringVars3 = 2 bits.
            // StringToken is a 3-bit tag with variants carrying
            // PlayerTypeRef / TeamTypeRef / ObjectTypeRef / NumericTypeRef /
            // TimerTypeRef (variable width). Missing these representations
            // caused downstream action drift.
            bool isTok1 = enumName.Equals("Tokens1", StringComparison.OrdinalIgnoreCase);
            bool isTok2 = enumName.Equals("Tokens2", StringComparison.OrdinalIgnoreCase);
            bool isTok3 = enumName.Equals("Tokens3", StringComparison.OrdinalIgnoreCase);
            if (isTok1 || isTok2 || isTok3)
            {
                int stringBits = ctx.Game == GameVariant.H2A ? 8 : 7;
                int varsBits   = isTok1 ? 1 : 2;
                int stringIndex = br.ReadUInt(stringBits);
                int svTag = br.ReadUInt(varsBits);
                // Resolve to the actual string content when ctx.StringTable
                // has it — emit as a regular C# string literal (escaped).
                // Falls back to `str[N]` when no name is known so untagged
                // entries still round-trip.
                string strToken;
                if (ctx.StringTable != null && stringIndex >= 0
                    && stringIndex < ctx.StringTable.Count
                    && !string.IsNullOrEmpty(ctx.StringTable[stringIndex]))
                    strToken = "\"" + EscapeStringLiteral(ctx.StringTable[stringIndex]) + "\"";
                else
                    strToken = $"str[{stringIndex}]";
                var parts = new List<string> { strToken };
                // StringVars{1,2,3} token counts per Halo 2A/var enums.xml
                // lines 889-922:
                //   StringVars1: tag 0 = None (0), tag 1 = Vars1 (1)
                //   StringVars2: tag 0 = None, 1 = Vars1, 2 = Vars2, 3 = Unlabelled (0)
                //   StringVars3: tag 0 = None, 1 = Vars1, 2 = Vars2, 3 = Vars3
                int tokens = 0;
                if (isTok1) tokens = svTag == 1 ? 1 : 0;
                else if (isTok2) tokens = svTag switch { 1 => 1, 2 => 2, _ => 0 };
                else /* isTok3 */ tokens = svTag; // 0/1/2/3
                for (int i = 0; i < tokens; i++)
                {
                    string tok = ReadStringToken(br, ctx);
                    if (!string.IsNullOrEmpty(tok)) parts.Add(tok);
                }
                // Inline the Tokens param: emit a bare comma-separated list
                // (`"text", score_to_win, current_player`) that gets spliced
                // directly into the parent action's argument list. The
                // compiler reverses this by recognizing actions whose
                // signature has a Tokens{1,2,3} parameter and consuming
                // all trailing args after the string into that one param.
                return new LiteralNode(string.Join(", ", parts));
            }

            // SpawnObjectFlags is a Container of 3 individual Bool bits.
            if (enumName.Equals("SpawnObjectFlags", StringComparison.OrdinalIgnoreCase))
            {
                int nevergc = br.ReadUInt(1);
                int suppress = br.ReadUInt(1);
                int abs = br.ReadUInt(1);
                var flags = new List<string>();
                if (nevergc != 0) flags.Add("never_garbage_collect");
                if (suppress != 0) flags.Add("suppress_effect");
                if (abs != 0) flags.Add("absolute_orientation");
                return new LiteralNode(flags.Count == 0 ? "0" : string.Join("|", flags));
            }

            // Vector3 is a 3×Int(8) Container — ReadParam handles it earlier
            // but add here for safety if anything references Enumref:Vector3
            // without going through that fast path.
            if (enumName.Equals("Vector3", StringComparison.OrdinalIgnoreCase))
                return ReadVector3(br);

            // Canonical width from the XML spec; H2A overrides first.
            bool haveBits = _externalEnumBits.TryGetValue(enumName, out var bits);
            int effBits = H2ABits(enumName, haveBits ? bits : 0, ctx);
            if (effBits > 0)
            {
                int v = br.ReadUInt(effBits);
                if (enumName.Equals("TriggerRef", StringComparison.OrdinalIgnoreCase))
                    return new LiteralNode($"trigger_{v}");

                // ObjectType (12 bits): prefer RVT's documented names
                // so the decompile matches RVT/MegaloEdit conventions
                // (e.g. `initial_loadout_camera` instead of
                // `MpCinematicCamera` from the project's local enum).
                if (enumName.Equals("ObjectType", StringComparison.OrdinalIgnoreCase)
                    && RvtObjectTypes.Names.TryGetValue(v, out var rvtName))
                    return new LiteralNode(rvtName);

                string? nm = EnumNameOrValue(enumName, v);
                return new LiteralNode(nm ?? v.ToString());
            }

            // Last-resort: reflection over project enums (byte-width inferred
            // from max declared value). Unsafe for sparse enums.
            int fbBits = EnumBitsByName(enumName);
            int fbVal = br.ReadUInt(fbBits);
            return new LiteralNode(EnumNameOrValue(enumName, fbVal) ?? fbVal.ToString());
        }

        private static ParamNode ReadSchemaEnum(BitReader br, MegaloSchema.EnumDef ed, DecompileContext ctx)
        {
            int tagBits = H2ABits(ed.Name, ed.Bits, ctx);
            int tag = br.ReadUInt(tagBits);

            MegaloSchema.VariantDef? variant = null;
            foreach (var v in ed.Variants)
                if (v.Id == tag) { variant = v; break; }

            if (variant == null)
                return new LiteralNode($"{ed.Name}#{tag}");

            // Leaf variant — emit the variant name with known script-side synonyms.
            if (variant.Fields.Length == 0)
            {
                // Unlabelled NumericTypeRef variants (tag 44-63) are the
                // scratch/temporary number slots. They apparently carry
                // a sub-index the XML doesn't document. The _tempNumberExtraBits
                // probe is used to find the right width empirically.
                if (_tempNumberExtraBits > 0
                    && ed.Name.Equals("NumericTypeRef", StringComparison.OrdinalIgnoreCase)
                    && tag >= 44)
                {
                    int idx = br.ReadUInt(_tempNumberExtraBits);
                    return new LiteralNode($"temp_num_{idx}");
                }
                return new LiteralNode(RenderLeafVariantName(ed.Name, variant));
            }

            var children = new ParamNode[variant.Fields.Length];
            for (int i = 0; i < variant.Fields.Length; i++)
                children[i] = ReadSchemaField(br, variant.Fields[i], ctx);

            return new VariantNode(ed.Name, variant.Key, variant.Name, children);
        }

        private static ParamNode ReadSchemaField(BitReader br, MegaloSchema.FieldDef f, DecompileContext ctx)
        {
            if (f.Type.StartsWith("Enumref:", StringComparison.OrdinalIgnoreCase))
            {
                string nestedEnum = f.Type.Substring("Enumref:".Length).Trim();
                return ReadSchemaEnum(br, nestedEnum, ctx);
            }

            if (f.Type.StartsWith("Ref0:", StringComparison.OrdinalIgnoreCase))
            {
                // For H2A, Ref0 widths declared inside type variants can
                // differ from Reach's schema. Prefer an H2A-specific key
                // like "Ref:playerplayers"; fall back to the Reach-defined
                // f.Bits. Pool-count widths (e.g. "playerplayers" by itself)
                // are unrelated to the Ref0 width and must NOT be used here.
                int slash = f.Type.LastIndexOf('/');
                string poolName = slash >= 0 ? f.Type.Substring(slash + 1) : f.Type;
                int bits = f.Bits;
                if (ctx.Game == GameVariant.H2A
                    && MegaloSchema.H2AEnumBits.TryGetValue("Ref:" + poolName, out var rb))
                    bits = rb;
                bits = Math.Max(bits, 1);
                int val = br.ReadUInt(bits);
                return new Ref0Node(f.Type, val);
            }

            if (f.Type.Equals("Int", StringComparison.OrdinalIgnoreCase))
                return new LiteralNode(br.ReadSInt(f.Bits).ToString());

            int fbits = Math.Max(f.Bits, 1);
            return new LiteralNode(br.ReadUInt(fbits).ToString());
        }

        private static string RenderLeafVariantName(string enumName, MegaloSchema.VariantDef v)
        {
            string name = v.Name.Replace("·", "").Trim();

            // Compiler/script-side synonyms.
            if (enumName.Equals("ObjectRef", StringComparison.OrdinalIgnoreCase)
                && name.Equals("CurrentObject", StringComparison.OrdinalIgnoreCase))
                return "current_object";
            if (enumName.Equals("PlayerRef", StringComparison.OrdinalIgnoreCase)
                && name.Equals("CurrentPlayer", StringComparison.OrdinalIgnoreCase))
                return "current_player";
            if (enumName.Equals("TeamRef", StringComparison.OrdinalIgnoreCase)
                && name.Equals("CurrentTeam", StringComparison.OrdinalIgnoreCase))
                return "current_team";

            // Temporary numbers (NumericTypeRef "Unlabelled" tag 44-63 = 0-19).
            if (enumName.Equals("NumericTypeRef", StringComparison.OrdinalIgnoreCase)
                && name.StartsWith("Unlabelled", StringComparison.OrdinalIgnoreCase))
            {
                int scratchIdx = v.Id - 44;
                return scratchIdx >= 0 ? $"temp_num_{scratchIdx}" : $"temp_num_q{v.Id}";
            }

            // Temporary objects / players / teams per RVE's VariableScope tuples.
            // Flattened to single identifiers so they participate in the
            // unified variable-management system instead of being addressed
            // by index (`temporaries.object[N]`).
            if (enumName.Equals("ObjectRef", StringComparison.OrdinalIgnoreCase)
                && name.StartsWith("Unlabelled", StringComparison.OrdinalIgnoreCase)
                && v.Id >= 22 && v.Id <= 29)
                return $"temp_obj_{v.Id - 22}";
            if (enumName.Equals("PlayerRef", StringComparison.OrdinalIgnoreCase)
                && name.StartsWith("Unlabelled", StringComparison.OrdinalIgnoreCase)
                && v.Id >= 29 && v.Id <= 31)
                return $"temp_player_{v.Id - 29}";
            if (enumName.Equals("TeamRef", StringComparison.OrdinalIgnoreCase)
                && name.StartsWith("Unlabelled", StringComparison.OrdinalIgnoreCase)
                && v.Id >= 23 && v.Id <= 28)
                return $"temp_team_{v.Id - 23}";

            // PascalCase / Word-boundary names → snake_case (NoPlayer →
            // no_player, AllPlayers → all_players, NoShape → no_shape, …)
            // so the decompile reads consistently with the rest of the
            // C# script style.
            return ToSnakeCase(name);
        }

        // Escape a string for use inside a C# double-quoted literal —
        // backslash, quote, newline, tab. Keeps decompiled `tokens(...)`
        // calls Roslyn-parseable when the resolved string contains
        // special characters (game messages frequently embed `\n`).
        private static string EscapeStringLiteral(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var sb = new StringBuilder(s.Length + 4);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"':  sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        // Convert PascalCase / camelCase / multi-word names to snake_case.
        // Always produces a Roslyn-parseable identifier so the decompile
        // round-trips through the C# compile pipeline.
        //   "TeamsEnabled" → "teams_enabled"
        //   "Teams Enabled" → "teams_enabled"
        //   "-100%" → "rate_minus_100"
        //   "25%"  → "rate_25"
        //   "current_player" → "current_player"  (already snake)
        //   "Player.Number" → "Player.Number"   (composite expr, untouched)
        private static string ToSnakeCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            // Composite expressions (already-formed receiver.member, args, etc.)
            // pass through.
            if (s.IndexOfAny(new[] { '.', '(', '[', '#' }) >= 0) return s;
            // Percent-suffixed rate tokens (TimerRate variants like "-100%")
            // pass through verbatim. The compiler's PreprocessDialect
            // rewrites them into the internal `rate_minus_100` form before
            // Roslyn parses, so the user sees `-100%` but the encoder
            // pipeline keeps working unchanged.
            if (s.EndsWith("%"))
            {
                return s;
            }
            // Multi-word with space → join with `_` then snake-case.
            if (s.Contains(' ')) s = s.Replace(' ', '_');
            // Already snake-cased.
            if (s.IndexOf('_') >= 0 && !ContainsUpper(s)) return s.ToLowerInvariant();
            var sb = new StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(s[i - 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static bool ContainsUpper(string s)
        {
            for (int i = 0; i < s.Length; i++) if (char.IsUpper(s[i])) return true;
            return false;
        }

        // StringToken per var enums.xml — 3-bit tag whose variants each
        // carry one type-ref sub-field. Render as the bare TypeRef
        // expression (no `number(...)` / `player(...)` / etc. wrapper) —
        // the compiler infers the payload type from the arg's identifier
        // class. tag=0 ("none") emits nothing so callers can filter.
        private static string ReadStringToken(BitReader br, DecompileContext ctx)
        {
            int tag = br.ReadUInt(3);
            switch (tag)
            {
                case 0: return string.Empty;
                case 1: return ReadSchemaEnum(br, "PlayerTypeRef",  ctx).Render(ctx);
                case 2: return ReadSchemaEnum(br, "TeamTypeRef",    ctx).Render(ctx);
                case 3: return ReadSchemaEnum(br, "ObjectTypeRef",  ctx).Render(ctx);
                case 4: return ReadSchemaEnum(br, "NumericTypeRef", ctx).Render(ctx);
                case 5: return ReadSchemaEnum(br, "TimerTypeRef",   ctx).Render(ctx);
                case 6: return ReadSchemaEnum(br, "TimerTypeRef",   ctx).Render(ctx); // TimerVarAswell
                default: return $"tok{tag}";
            }
        }

        private static ParamNode ReadVector3(BitReader br)
        {
            int x = br.ReadSInt(8);
            int y = br.ReadSInt(8);
            int z = br.ReadSInt(8);
            return new LiteralNode($"{x},{y},{z}");
        }

        // ---- Param node tree (deferred render) ----------------------------

        private abstract class ParamNode
        {
            public abstract string Render(DecompileContext ctx);
        }

        private sealed class LiteralNode : ParamNode
        {
            public readonly string Value;
            public LiteralNode(string value) { Value = value; }
            public override string Render(DecompileContext ctx) => Value;
        }

        private sealed class Ref0Node : ParamNode
        {
            public readonly string TableType;  // e.g. "Ref0:Gametype/base/mpvr/Labels"
            public readonly int Index;
            public Ref0Node(string tableType, int index) { TableType = tableType; Index = index; }

            public override string Render(DecompileContext ctx)
            {
                if (TableType.EndsWith("/Labels", StringComparison.OrdinalIgnoreCase))
                {
                    // Named labels render as `"name"` (regular quoted —
                    // forge label names are simple identifiers, no need
                    // for verbatim `@"..."`). Unnamed labels render as a
                    // bare integer index. Both forms round-trip through
                    // EncodeLabelRef which accepts quoted, bare int, and
                    // legacy `label[N]` syntaxes.
                    if (ctx.Labels != null && Index >= 0 && Index < ctx.Labels.Count
                        && !string.IsNullOrEmpty(ctx.Labels[Index]))
                        return $"\"{ctx.Labels[Index]}\"";
                    return Index.ToString();
                }

                if (TableType.EndsWith("/ScriptOptions", StringComparison.OrdinalIgnoreCase))
                    return $"script_option[{Index}]";

                int slash = TableType.LastIndexOf('/');
                string tail = slash >= 0 ? TableType.Substring(slash + 1) : TableType;
                return $"{tail}[{Index}]";
            }
        }

        private sealed class VariantNode : ParamNode
        {
            public readonly string EnumName;
            public readonly string VariantKey;
            public readonly string VariantDisplayName;
            public readonly ParamNode[] Children;

            public VariantNode(string enumName, string variantKey, string variantDisplayName, ParamNode[] children)
            {
                EnumName = enumName;
                VariantKey = variantKey;
                VariantDisplayName = variantDisplayName;
                Children = children;
            }

            public override string Render(DecompileContext ctx)
            {
                // Single-field variant flattens to its child — but add known script-side
                // sugar for the PlayerBiped variants of ObjectTypeRef (player.biped).
                if (Children.Length == 1)
                {
                    string child = Children[0].Render(ctx);
                    if (EnumName.Equals("ObjectTypeRef", StringComparison.OrdinalIgnoreCase)
                        && (VariantKey.StartsWith("Player.Biped", StringComparison.OrdinalIgnoreCase)
                            || VariantKey.Equals("PlayerBiped", StringComparison.OrdinalIgnoreCase)))
                    {
                        return $"{child}.biped";
                    }
                    return child;
                }

                var parts = new string[Children.Length];
                for (int i = 0; i < Children.Length; i++)
                    parts[i] = Children[i].Render(ctx);

                string display = VariantDisplayName.Replace("·", "").Trim();

                // Dotted scoped accessors like `Player.Number(plr, idx)` /
                // `Object.Player(obj, plr_idx)` / `Team.Object(team, idx)` /
                // `Player.Timer(plr, t)` flatten to `plr.idx` so the script
                // reads `current_player.playerobjects_0` instead of the
                // awkward `Player.Object(current_player, playerobjects_0)`.
                if (display.Contains('.') && Children.Length == 2)
                    return $"{parts[0]}.{parts[1]}";

                return $"{display}({string.Join(", ", parts)})";
            }
        }

        // DecompileContext is defined in ScriptTailReader.cs so the tail walker
        // can populate it without touching decoder internals.

        // ---- Lookup helpers ------------------------------------------------

        private static class MegaloLookup
        {
            private static readonly Dictionary<int, MegaloAction> _reachActions;
            private static readonly Dictionary<int, MegaloCondition> _reachConditions;
            private static readonly Dictionary<int, MegaloAction> _h2aActions;
            private static readonly Dictionary<int, MegaloCondition> _h2aConditions;

            static MegaloLookup()
            {
                _reachActions = MegaloTables.Actions.ToDictionary(a => a.Id, a => a);
                _reachConditions = MegaloTables.Conditions.ToDictionary(c => c.Id, c => c);
                _h2aActions = MegaloTables_H2A.ActionDefs.ToDictionary(a => a.Id, a => a);
                _h2aConditions = MegaloTables_H2A.ConditionDefs.ToDictionary(c => c.Id, c => c);
            }

            public static MegaloAction? GetAction(int id, DecompileContext? ctx = null)
            {
                var d = (ctx?.Game == GameVariant.H2A) ? _h2aActions : _reachActions;
                return d.TryGetValue(id, out var a) ? a : null;
            }

            public static MegaloCondition? GetCondition(int id, DecompileContext? ctx = null)
            {
                var d = (ctx?.Game == GameVariant.H2A) ? _h2aConditions : _reachConditions;
                return d.TryGetValue(id, out var c) ? c : null;
            }
        }

        // Helper: resolves an enum/ref bit-width, preferring the H2A override
        // table when the context says we're decoding an H2A variant.
        // Falls back to the Reach-era hardcoded width.
        private static int H2ABits(string name, int reachFallback, DecompileContext ctx)
        {
            if (ctx.Game == GameVariant.H2A
                && MegaloSchema.H2AEnumBits.TryGetValue(name, out var b))
                return b;
            return reachFallback;
        }

        // ---- Enum reflection helpers --------------------------------------

        private static int EnumBitsByName(string enumName)
        {
            var t = FindEnumType(enumName);
            if (t == null) return 1;

            // Determine the max numeric value (assumes non-negative)
            long max = 0;
            foreach (var v in Enum.GetValues(t))
            {
                long vv = Convert.ToInt64(v);
                if (vv > max) max = vv;
            }

            int bits = 1;
            while ((1L << bits) <= max) bits++;
            return bits;
        }

        private static string? EnumNameOrValue(string enumName, int value)
        {
            var t = FindEnumType(enumName);
            if (t == null) return null;

            try
            {
                string? n = Enum.GetName(t, value);
                return n;
            }
            catch
            {
                return null;
            }
        }

        private static int? EnumValueByName(string enumName, string member)
        {
            var t = FindEnumType(enumName);
            if (t == null) return null;

            try
            {
                object v = Enum.Parse(t, member, ignoreCase: true);
                return Convert.ToInt32(v);
            }
            catch
            {
                return null;
            }
        }

        private static Type? FindEnumType(string enumName)
        {
            // Try exact enum type name first
            var asm = typeof(MegaloTables).Assembly;

            // Common patterns in your project:
            //  - PlayerRefEnum
            //  - TriggerTypeEnum
            //  - TriggerAttributeEnum
            //  - NumericTypeRefEnum
            //  - ObjectTypeRefEnum
            //  - ObjectRef
            //  - TeamRef
            // and sometimes the XML name without "Enum" suffix.
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsEnum) continue;

                if (t.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase))
                    return t;

                if ((t.Name + "Enum").Equals(enumName, StringComparison.OrdinalIgnoreCase))
                    return t;

                if (t.Name.Equals(enumName + "Enum", StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            // Fallback: fuzzy match contains
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsEnum) continue;
                if (t.Name.IndexOf(enumName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }

            return null;
        }

        // ---- Small utilities ----------------------------------------------

        private static bool Eq(string a, string b)
            => a.Equals(b, StringComparison.OrdinalIgnoreCase);

        // ---- Data models ---------------------------------------------------

        private readonly struct ConditionEntry
        {
            public readonly int Id;
            public readonly string Name;
            public readonly bool IsNot;
            public readonly int OrSequence;
            public readonly int ActionOffset;
            public readonly List<ParamNode> Args;

            public ConditionEntry(int id, string name, bool isNot, int orSequence, int actionOffset, List<ParamNode> args)
            {
                Id = id;
                Name = name;
                IsNot = isNot;
                OrSequence = orSequence;
                ActionOffset = actionOffset;
                Args = args;
            }
        }

        private readonly struct ActionEntry
        {
            public readonly int Id;
            public readonly string Name;
            public readonly List<ParamNode> Args;

            public readonly bool IsInline;
            public readonly int InlineCondOffset;
            public readonly int InlineCondCount;
            public readonly int InlineActOffset;
            public readonly int InlineActCount;

            public ActionEntry(int id, string name, List<ParamNode> args)
            {
                Id = id;
                Name = name;
                Args = args;

                IsInline = false;
                InlineCondOffset = InlineCondCount = InlineActOffset = InlineActCount = 0;
            }

            private ActionEntry(int id, int condOff, int condCount, int actOff, int actCount)
            {
                Id = id;
                Name = "Inline";
                Args = new List<ParamNode>();

                IsInline = true;
                InlineCondOffset = condOff;
                InlineCondCount = condCount;
                InlineActOffset = actOff;
                InlineActCount = actCount;
            }

            public static ActionEntry Inline(int id, int condOff, int condCount, int actOff, int actCount)
                => new ActionEntry(id, condOff, condCount, actOff, actCount);
        }

        private readonly struct TriggerEntry
        {
            public readonly int TypeValue;
            public readonly int AttributeValue;
            public readonly string TypeName;
            public readonly string AttributeName;

            public readonly int ConditionOffset;
            public readonly int ConditionCount;
            public readonly int ActionOffset;
            public readonly int ActionCount;

            public readonly int Unknown1;
            public readonly int Unknown2;

            // Optional label/filter text — populated only when the trigger
            // type is ForeachObjectWithLabel (5) or ForeachObjectWithFilter (6).
            public readonly string? LabelText;

            public TriggerEntry(
                int typeVal,
                int attrVal,
                string typeName,
                string attrName,
                int condOffset,
                int condCount,
                int actOffset,
                int actCount,
                int unk1,
                int unk2,
                string? labelText = null)
            {
                TypeValue = typeVal;
                AttributeValue = attrVal;
                TypeName = typeName;
                AttributeName = attrName;
                ConditionOffset = condOffset;
                ConditionCount = condCount;
                ActionOffset = actOffset;
                ActionCount = actCount;
                Unknown1 = unk1;
                Unknown2 = unk2;
                LabelText = labelText;
            }
        }


        // ---- Bit reader ----------------------------------------------------

        private sealed class BitReader : ScriptTailReader.IBitStream
        {
            private readonly string _bits;
            private int _pos;

            public BitReader(string bits)
            {
                _bits = bits;
                _pos = 0;
            }

            public int Position => _pos;
            public int RemainingBits => Math.Max(0, _bits.Length - _pos);

            public int ReadUInt(int bitCount)
            {
                if (bitCount <= 0) return 0;
                if (_pos + bitCount > _bits.Length)
                    bitCount = Math.Max(0, _bits.Length - _pos);

                int val = 0;
                for (int i = 0; i < bitCount; i++)
                {
                    val <<= 1;
                    char c = _bits[_pos++];
                    if (c == '1') val |= 1;
                }
                return val;
            }

            public int ReadSInt(int bitCount)
            {
                int u = ReadUInt(bitCount);
                if (bitCount <= 0) return 0;

                int signBit = 1 << (bitCount - 1);
                if ((u & signBit) == 0)
                    return u;

                // two's complement
                int mask = (1 << bitCount) - 1;
                int twos = -(((~u) + 1) & mask);
                return twos;
            }
        }
    }
}

// GametypeReader.cs
//
// Schema-driven replacement for ReadGametype.ReadBinary. Loads a Reach
// game variant .bin (or .mglo) and walks MegaloSchema.ReachMpvrLayout
// from bit 752 of the file through WeaponTunings, producing a structured
// GametypeData tree.
//
// When the walker reaches the "megl" marker, GametypeReader hands the
// remaining bits to ScriptDecompiler.Decompile, which consumes the
// megalo script block (conditions/actions/triggers + MegaloVars globals).
// GametypeReader then resumes the walk at the post-script offset that
// ScriptDecompiler advanced to.
//
// Strict desync policy: any UnverifiedSection or unknown tag aborts the
// walk with a diagnostic. Silent fallbacks were how the legacy
// ReadBinary accumulated layout bugs — we refuse to do that here.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib.Zip.Compression;

using UniversalGametypeEditor.Megalo;

namespace UniversalGametypeEditor
{
    public sealed class GametypeReader
    {
        // The MCC wrapper on .bin variants before the real mpvr container
        // starts. Legacy ReadBinary passes 752 as a BYTE offset to
        // GetBinaryString, so that's 752 * 8 = 6016 bits. Fallback wrapper
        // on some containers (GVAR) starts at byte 128 = 1024 bits.
        private const int MccWrapperBits = 752 * 8;
        private const int FallbackWrapperBits = 128 * 8;

        public sealed class Result
        {
            public bool Ok;
            public readonly List<string> Diagnostics = new();
            public readonly Dictionary<string, object> Values =
                new(StringComparer.OrdinalIgnoreCase);
            public string? DecompiledScript;
            public string? ScriptBits; // raw megl bit stream for explicit-pipeline callers
            public GameVariant Game;
            public int TotalFileBits;
            public int BitsConsumed;
            public List<string>? Labels; // forge label names (Reach Labels section)
            public List<string>? StringTable; // Reach main StringTable (52 entries on azsassinations)
        }

        public Result Read(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var bits = BytesToBitString(bytes);

            var r = new Result
            {
                TotalFileBits = bits.Length,
            };

            // If the first 32 bits at MccWrapperBits aren't "mpvr", this is
            // probably a raw mpvr block stored at a different offset
            // (GVAR / H4 container). Keep the legacy fallback.
            int startOffset = MccWrapperBits;
            string firstFourChars = ReadAsciiAt(bits, MccWrapperBits, 32);
            if (firstFourChars != "mpvr" && firstFourChars != "gvar")
            {
                r.Diagnostics.Add(
                    $"No 'mpvr'/'gvar' magic at bit {MccWrapperBits} (byte {MccWrapperBits/8}); "
                    + $"trying bit {FallbackWrapperBits} (byte {FallbackWrapperBits/8}).");
                startOffset = FallbackWrapperBits;
                firstFourChars = ReadAsciiAt(bits, FallbackWrapperBits, 32);
            }

            // Game detection. The mpvr chunk starts with 4-char magic + u32
            // length + u16 major-version. Reach = 0x0036, H2A = 0x0089.
            // Byte offset into the file = startOffset/8 + 6.
            GameVariant game = DetectGame(bytes, startOffset / 8);
            bool isGvar = string.Equals(firstFourChars, "gvar", StringComparison.Ordinal);
            r.Diagnostics.Add($"Detected game: {game}" + (isGvar ? " (gvar container)" : ""));

            // Stripping down to bits starting at startOffset.
            string stream = bits.Substring(startOffset);
            var bitStream = new StringBitStream(stream);

            var scope = new Dictionary<string, object>();
            var ctx = new DecompileContext { Game = game };

            WalkResult walk;
            try
            {
                MegaloSchema.MegaloSection[] layout;
                if (game == GameVariant.H2A)
                    layout = isGvar ? MegaloSchema.H2AGvarLayout : MegaloSchema.H2AMpvrLayout;
                else
                    layout = MegaloSchema.ReachMpvrLayout;
                walk = WalkLayout(bitStream, layout, scope, ctx, path);
            }
            catch (Exception ex)
            {
                r.Diagnostics.Add($"walker threw: {ex.Message}");
                r.Ok = false;
                r.BitsConsumed = bitStream.Position;
                return r;
            }

            foreach (var kv in scope) r.Values[kv.Key] = kv.Value;
            r.Diagnostics.AddRange(walk.Diagnostics);
            r.DecompiledScript = walk.DecompiledScript;
            r.ScriptBits = walk.ScriptBits;
            r.Game = walk.Game;
            r.Labels = walk.Labels;
            r.StringTable = walk.StringTable;
            r.Ok = !walk.Desynced;
            r.BitsConsumed = bitStream.Position + startOffset;
            return r;
        }

        // ---- Walker ------------------------------------------------------

        private sealed class WalkResult
        {
            public bool Desynced;
            public readonly List<string> Diagnostics = new();
            public string? DecompiledScript;
            public string? ScriptBits;
            public GameVariant Game;
            public List<string>? Labels;
            public List<string>? StringTable;
        }

        private WalkResult WalkLayout(
            StringBitStream br,
            MegaloSchema.MegaloSection[] layout,
            Dictionary<string, object> scope,
            DecompileContext ctx,
            string path)
        {
            var result = new WalkResult();
            WalkSections(br, layout, scope, ctx, result, path);
            // Surface forge label names from the post-megl Labels section
            // so callers (DecompileAsScript) can render LabelRefs by name.
            result.Labels = ctx.Labels;
            result.StringTable = ctx.StringTable;
            return result;
        }

        // Set to true to emit per-section bit-position diagnostics. Useful
        // for hunting H2A pre-megl layout drift.
        public static bool TraceBitPositions = false;

        // Debugging: when non-zero, shifts the megl handoff by this many
        // bits. Used to probe alignment while hunting H2A layout drift.
        public static int MeglShiftProbe = 0;

        private void WalkSections(
            StringBitStream br,
            MegaloSchema.MegaloSection[] sections,
            Dictionary<string, object>? scope,
            DecompileContext ctx,
            WalkResult result,
            string path)
        {
            foreach (var s in sections)
            {
                if (result.Desynced) return;
                int posBefore = br.Position;
                WalkOne(br, s, scope, ctx, result, path);
                if (TraceBitPositions)
                    result.Diagnostics.Add($"[walk@{posBefore,7}] {s.GetType().Name,-22} {s.Name,-40} consumed {br.Position - posBefore}");
            }
        }

        private void WalkOne(
            StringBitStream br,
            MegaloSchema.MegaloSection section,
            Dictionary<string, object>? scope,
            DecompileContext ctx,
            WalkResult result,
            string path)
        {
            switch (section)
            {
                case MegaloSchema.BlankSection b:
                    SafeSkip(br, b.Bits, result, b.Name); return;

                case MegaloSchema.UIntSection u:
                    if (TryRead(br, u.Bits, out int uv, result, u.Name))
                        scope?[u.Name] = uv;
                    return;

                case MegaloSchema.SIntSection si:
                    if (br.RemainingBits < si.Bits)
                    {
                        Desync(result, $"Ran out of bits reading SInt '{si.Name}'.");
                        return;
                    }
                    scope?[si.Name] = br.ReadSInt(si.Bits);
                    return;

                case MegaloSchema.HexSection h:
                    if (TryRead(br, h.Bits, out int hv, result, h.Name))
                        scope?[h.Name] = hv;
                    return;

                case MegaloSchema.StringTableRefSection str:
                    if (TryRead(br, str.Bits, out int strv, result, str.Name))
                        scope?[str.Name] = strv;
                    return;

                case MegaloSchema.AsciiStringSection asc:
                    if (TryReadAscii(br, asc.Bits, out string av, result, asc.Name))
                        scope?[asc.Name] = av;
                    return;

                case MegaloSchema.UString8Section u8:
                    if (TryReadUString8(br, out string s8, result, u8.Name))
                        scope?[u8.Name] = s8;
                    return;

                case MegaloSchema.UString16Section u16:
                    if (TryReadUString16(br, out string s16, result, u16.Name))
                        scope?[u16.Name] = s16;
                    return;

                case MegaloSchema.GroupSection g:
                    WalkSections(br, g.Children, scope, ctx, result, path);
                    return;

                case MegaloSchema.SectionRefSection sr:
                    MegaloSchema.MegaloSection[]? referenced = null;
                    if (ctx.Game == GameVariant.H2A)
                        MegaloSchema.H2ALayouts.TryGetValue(sr.LayoutKey, out referenced);
                    if (referenced == null)
                        MegaloSchema.Layouts.TryGetValue(sr.LayoutKey, out referenced);
                    if (referenced == null)
                    {
                        Desync(result, $"SectionRef '{sr.Name}' → unknown layout '{sr.LayoutKey}'.");
                        return;
                    }
                    // Sub-scope so nested names don't collide with the top level.
                    var subScope = new Dictionary<string, object>();
                    WalkSections(br, referenced, subScope, ctx, result, path);
                    if (scope != null) scope[sr.Name] = subScope;
                    return;

                case MegaloSchema.CountSection c:
                    WalkCount(br, c, scope, ctx, result, path);
                    return;

                case MegaloSchema.VariantSection v:
                    WalkVariant(br, v, scope, ctx, result, path);
                    return;

                case MegaloSchema.UnverifiedSection uvm:
                {
                    // Special case: the "megl" marker routes into ScriptDecompiler.
                    if (string.Equals(uvm.Name, "megl", StringComparison.OrdinalIgnoreCase))
                    {
                        // Hand the remaining bits to ScriptDecompiler. It
                        // consumes the megalo script block (conditions +
                        // actions + triggers + MegaloVars) and returns the
                        // number of bits it consumed, which we apply to our
                        // stream cursor so subsequent ReachMpvrLayout sections
                        // (requiredobjects → unknown1824 → Labels → WeaponTunings)
                        // read from the correct position.
                        string remainingBits = br.RemainingBitString();

                        // Debug hook — lets CLI flag `--megl-shift N` force
                        // a handoff shift while investigating drift.
                        if (MeglShiftProbe != 0)
                        {
                            int s = MeglShiftProbe;
                            if (s < 0) br.Rewind(-s); else br.Advance(s);
                            remainingBits = br.RemainingBitString();
                            result.Diagnostics.Add($"MeglShiftProbe applied: {s:+#;-#;0} bits");
                        }

                        // Pass the outer-walker's StringTable into the script
                        // decoder so its tail walker resolves Labels' StringID
                        // → name lookups against the real strings (decoded
                        // earlier in the layout, before megl).
                        var decomp = ScriptDecompiler.DecompileDetailed(
                            remainingBits, labelNames: null, game: ctx.Game,
                            stringTable: ctx.StringTable);
                        result.DecompiledScript = decomp.Text;
                        result.ScriptBits = remainingBits;
                        result.Game = ctx.Game;

                        // Surface any labels resolved during the script's
                        // own tail walk into the outer ctx. The outer walker
                        // currently desyncs after megl (MegaloGlobals widths
                        // not fully verified) so its post-megl Labels
                        // CountSection never runs — without this propagation
                        // r.Labels would always be null.
                        if (decomp.Labels != null && decomp.Labels.Count > 0)
                            ctx.Labels = decomp.Labels;
                        if (ctx.Labels == null && ctx.Game == GameVariant.Reach)
                        {
                            // Forward-walk to the Labels section using
                            // RVT-authoritative widths for widgets /
                            // entryPoints / usedMPObjectTypes. See
                            // ScriptDecompiler.ReadReachForgeLabels for
                            // the layout reference.
                            var fl = ScriptDecompiler.ReadReachForgeLabels(remainingBits, ctx.StringTable, ctx.Game);
                            if (fl != null) ctx.Labels = fl;
                        }

                        // Advance the outer cursor past whatever the decoder
                        // consumed. NOTE: ScriptDecompiler currently halts at
                        // the MegaloGlobals UnverifiedSection, so BitsConsumed
                        // reflects only the triggers block. Until the
                        // MegaloVars/globals schema is nailed down, we stop
                        // here rather than misalign the post-megl walk.
                        br.Advance(decomp.BitsConsumed);
                        if (scope != null)
                            scope["megl"] = new Dictionary<string, object>
                            {
                                ["ConditionCount"] = decomp.ConditionCount,
                                ["ActionCount"] = decomp.ActionCount,
                                ["TriggerCount"] = decomp.TriggerCount,
                                ["BitsConsumed"] = decomp.BitsConsumed,
                            };

                        // ScriptDecompiler's current tail walker hits an
                        // UnverifiedSection (MegaloGlobals) so it hasn't
                        // consumed the full megl block. Record that — the
                        // outer walk will desync for Labels until we fill
                        // in globals widths.
                        Desync(result,
                            $"megl decoder consumed {decomp.BitsConsumed} bits and stopped at MegaloGlobals. "
                            + "Post-megl sections (requiredobjects, unknown1824, Labels, WeaponTunings) "
                            + "will not parse correctly until the MegaloVars/globals section widths are verified.");
                        return;
                    }
                    Desync(result, $"Unverified section '{uvm.Name}': {uvm.Note}");
                    return;
                }

                case MegaloSchema.StringTableSection stbl:
                    WalkStringTable(br, stbl, scope, ctx, result);
                    return;

                case MegaloSchema.EnumRefSection er:
                    Desync(result,
                        $"EnumRefSection '{er.Name}' (Enumref:{er.EnumName}) is not yet wired "
                        + "to the shared TypeRef reader.");
                    return;

                default:
                    Desync(result, $"Unknown MegaloSection type: {section.GetType().Name}");
                    return;
            }
        }

        private void WalkCount(
            StringBitStream br,
            MegaloSchema.CountSection c,
            Dictionary<string, object>? scope,
            DecompileContext ctx,
            WalkResult result,
            string path)
        {
            if (!TryRead(br, c.CountBits, out int count, result, c.Name + ".count")) return;
            if (count < 0 || count > 4096)
            {
                Desync(result, $"Count '{c.Name}' is {count} — upstream desync.");
                return;
            }

            var records = new List<Dictionary<string, object>>(count);
            var tablePopulate = !string.IsNullOrEmpty(c.TableKey) ? new List<string>(count) : null;

            for (int i = 0; i < count; i++)
            {
                var rec = new Dictionary<string, object>();
                WalkSections(br, c.RecordFields, rec, ctx, result, path);
                if (result.Desynced) return;
                records.Add(rec);

                if (tablePopulate != null)
                {
                    string? name = null;
                    if (!string.IsNullOrEmpty(c.NameFromField)
                        && rec.TryGetValue(c.NameFromField, out var sidObj)
                        && sidObj is int sid
                        && ctx.StringTable != null
                        && sid >= 0 && sid < ctx.StringTable.Count)
                    {
                        name = ctx.StringTable[sid];
                    }
                    // Empty string when no real name resolves — the LabelRef
                    // renderer treats null/empty as "fall back to bare index"
                    // so unnamed labels emit `with label 3` instead of a
                    // synthetic `with label "labels_3"`.
                    tablePopulate.Add(name ?? string.Empty);
                }
            }

            if (tablePopulate != null && !string.IsNullOrEmpty(c.TableKey))
            {
                ctx.Tables[c.TableKey!] = tablePopulate;
                if (c.TableKey!.EndsWith("/Labels", StringComparison.OrdinalIgnoreCase))
                    ctx.Labels = tablePopulate;
            }

            if (scope != null) scope[c.Name] = records;
        }

        // Walks an "HCount" hash-coded string block per legacy
        // ReadGametype.ReadLangStringsDecoded. Decodes each entry's
        // English string and pushes the resolved name list into
        // ctx.StringTable for the FIRST string table in the layout
        // (the canonical "Stringtable" — same one Labels' StringID
        // indexes into). Meta string tables (metaname/metadesc/…)
        // are decoded for cursor advancement but don't overwrite
        // ctx.StringTable.
        private void WalkStringTable(
            StringBitStream br,
            MegaloSchema.StringTableSection stbl,
            Dictionary<string, object>? scope,
            DecompileContext ctx,
            WalkResult result)
        {
            if (!TryRead(br, stbl.CountBits, out int stringPresent, result, stbl.Name + ".count")) return;
            if (stringPresent < 0 || stringPresent > 4096)
            {
                Desync(result, $"StringTable '{stbl.Name}' present count {stringPresent} out of range — upstream desync.");
                return;
            }

            // Per-entry per-language presence + offset. We only need
            // English (the first language) to surface label names; the
            // rest just advance the cursor.
            var englishOffsets = new int[stringPresent]; // -1 means "not present"
            for (int i = 0; i < stringPresent; i++)
            {
                for (int l = 0; l < stbl.NumLanguages; l++)
                {
                    if (br.RemainingBits < 1) { Desync(result, $"EOF in {stbl.Name} lang present."); return; }
                    int present = br.ReadUInt(1);
                    if (present == 1)
                    {
                        if (br.RemainingBits < stbl.ValueBits)
                        { Desync(result, $"EOF in {stbl.Name} lang value."); return; }
                        int offset = br.ReadUInt(stbl.ValueBits);
                        if (l == 0) englishOffsets[i] = offset;
                    }
                    else if (l == 0)
                    {
                        englishOffsets[i] = -1;
                    }
                }
            }

            // Compressed chunk header + body. m3 = uncompressed byte
            // size; d = compressed flag; m1 = compressed byte size.
            byte[]? decoded = null;
            if (stringPresent > 0)
            {
                int m3Bits = stbl.IsTeamString ? stbl.ValueBits + 1 : stbl.ValueBits;
                if (!TryRead(br, m3Bits, out int m3, result, stbl.Name + ".m3")) return;
                if (!TryRead(br, 1, out int d, result, stbl.Name + ".d")) return;

                if (d == 0)
                {
                    decoded = new byte[m3];
                    for (int b = 0; b < m3; b++)
                    {
                        if (br.RemainingBits < 8) { Desync(result, $"EOF in {stbl.Name}.raw"); return; }
                        decoded[b] = (byte)br.ReadUInt(8);
                    }
                }
                else
                {
                    if (!TryRead(br, stbl.ValueBits, out int m1, result, stbl.Name + ".m1")) return;
                    if (m1 < 0 || m1 > 1 << 20)
                    { Desync(result, $"{stbl.Name}.m1 size {m1} out of range."); return; }
                    var compressed = new byte[m1];
                    for (int b = 0; b < m1; b++)
                    {
                        if (br.RemainingBits < 8) { Desync(result, $"EOF in {stbl.Name}.zlib"); return; }
                        compressed[b] = (byte)br.ReadUInt(8);
                    }
                    try
                    {
                        decoded = new byte[m3];
                        var inflater = new Inflater();
                        // Skip the 4-byte uncompressed-size header that prefixes
                        // the zlib stream (legacy LowLevelDecompress convention).
                        int skip = Math.Min(4, compressed.Length);
                        inflater.SetInput(compressed, skip, compressed.Length - skip);
                        inflater.Inflate(decoded);
                    }
                    catch (Exception ex)
                    {
                        result.Diagnostics.Add($"{stbl.Name}: zlib inflate failed ({ex.Message}); names will fall back to bare indices.");
                        decoded = null;
                    }
                }
            }

            // Build the resolved name list from English offsets into the
            // decoded buffer. Each string is null-terminated UTF-8 at the
            // given byte offset. Empty/missing entries become empty strings
            // so downstream renderers fall back to bare indices.
            if (stringPresent > 0 && decoded != null)
            {
                var names = new List<string>(stringPresent);
                for (int i = 0; i < stringPresent; i++)
                {
                    int off = englishOffsets[i];
                    if (off < 0 || off >= decoded.Length) { names.Add(string.Empty); continue; }
                    int end = off;
                    while (end < decoded.Length && decoded[end] != 0) end++;
                    names.Add(end > off
                        ? Encoding.UTF8.GetString(decoded, off, end - off)
                        : string.Empty);
                }

                // First string table in the layout is the canonical one
                // — meta tables come later and shouldn't overwrite it.
                if (ctx.StringTable == null) ctx.StringTable = names;
            }

            if (scope != null) scope[stbl.Name] = $"present={stringPresent}";
        }

        private void WalkVariant(
            StringBitStream br,
            MegaloSchema.VariantSection v,
            Dictionary<string, object>? scope,
            DecompileContext ctx,
            WalkResult result,
            string path)
        {
            if (!TryRead(br, v.TagBits, out int tag, result, v.Name + ".tag")) return;
            foreach (var (id, key, fields) in v.Variants)
            {
                if (id == tag)
                {
                    var sub = scope == null ? null : new Dictionary<string, object>();
                    WalkSections(br, fields, sub, ctx, result, path);
                    if (scope != null && sub != null)
                    {
                        sub["_variant"] = key;
                        scope[v.Name] = sub;
                    }
                    return;
                }
            }
            Desync(result, $"Variant '{v.Name}' got unknown tag {tag}.");
        }

        // ---- Anchored Labels finder --------------------------------------
        //
        // The Reach mpvr layout ends with `Labels` (variable) + `unknown26`
        // (26 bits) + `WeaponTunings` (62 bits) = 88-bit fixed suffix. Both
        // the script-tail walker and the outer walker currently desync
        // before reaching Labels (HudWidgetsAndStats / MegaloGlobals widths
        // are RE'd but not fully wired). Until that lands, this anchored
        // finder brute-forces the Labels start position by trying every
        // candidate offset whose bit-length math could fit Labels + the
        // 88-bit suffix, parsing each candidate as a CountSection, and
        // accepting the one that consumes exactly to `length - 88` AND
        // produces StringIDs that all resolve into ctx.StringTable.
        //
        // Multiple candidates can technically parse cleanly (zero-padded
        // unknown1824 produces a lot of count=0 false starts). The string-
        // table-validity check disambiguates: in practice only one
        // candidate has every StringID in range AND yields non-empty names.
        // Scan a rendered decompile for the maximum label index referenced
        // — gives us a floor on the Labels CountSection size when we have
        // to brute-force the section's start position. Recognizes both
        // forms of LabelRef rendering: the C#-script form (`with label N`,
        // `forge_label(N)`, `label[N]`) and the legacy comment form
        // (`Ref0:Gametype/base/mpvr/Labels/N`).
        private static int MaxLabelIndexInScript(string? text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            int max = -1;
            var rx = new System.Text.RegularExpressions.Regex(
                @"(?:label\s*\[\s*(\d+)\s*\]|with\s+label\s+(\d+)|forge_label\s*\(\s*(\d+)\s*\)|/Labels/(\d+))");
            foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
            {
                for (int g = 1; g <= 4; g++)
                {
                    if (m.Groups[g].Success
                        && int.TryParse(m.Groups[g].Value, out int n)
                        && n > max) max = n;
                }
            }
            return max;
        }

        private static List<string>? FindLabelsAnchored(string scriptBits, List<string>? stringTable, int suffixBits = 88, int requireMinCount = 0, bool requireAllNamed = false)
        {
            if (string.IsNullOrEmpty(scriptBits)) return null;
            if (stringTable == null || stringTable.Count == 0) return null;
            int total = scriptBits.Length;
            int labelsEnd = total - suffixBits;
            if (labelsEnd < 5) return null;

            int ReadBits(int pos, int n)
            {
                int v = 0;
                for (int i = 0; i < n; i++) v = (v << 1) | (scriptBits[pos + i] - '0');
                return v;
            }

            // Per-record min/max widths: 7+1+1+1+7 = 17 (all variants Null);
            // 7+1+16+1+4+1+12+7 = 49 (all InUse).
            const int maxRecordBits = 49;
            const int maxCount = 31; // 5-bit count
            int maxLabelsBits = 5 + maxCount * maxRecordBits;
            int minStart = Math.Max(0, labelsEnd - maxLabelsBits);
            int maxStart = labelsEnd - 5;

            List<int>? bestStringIds = null;
            int bestStart = -1;

            for (int start = maxStart; start >= minStart; start--)
            {
                int pos = start;
                int count = ReadBits(pos, 5); pos += 5;
                if (count < 0 || count > maxCount) continue;

                var stringIds = new List<int>(count);
                bool ok = true;
                for (int i = 0; i < count; i++)
                {
                    if (pos + 17 > labelsEnd) { ok = false; break; }
                    int sid = ReadBits(pos, 7); pos += 7;
                    int useNum = ReadBits(pos, 1); pos += 1;
                    if (useNum == 1) { if (pos + 16 > labelsEnd) { ok = false; break; } pos += 16; }
                    int useTeam = ReadBits(pos, 1); pos += 1;
                    if (useTeam == 1) { if (pos + 4 > labelsEnd) { ok = false; break; } pos += 4; }
                    int useObj = ReadBits(pos, 1); pos += 1;
                    if (useObj == 1) { if (pos + 12 > labelsEnd) { ok = false; break; } pos += 12; }
                    if (pos + 7 > labelsEnd) { ok = false; break; }
                    pos += 7; // MapMinimum
                    stringIds.Add(sid);
                }
                if (!ok || pos != labelsEnd) continue;
                if (count < requireMinCount) continue;

                // All StringIDs must be in StringTable range. With
                // requireAllNamed, every record's name must also be
                // non-empty — eliminates the count=N candidates that
                // happen to fit numerically but have garbage StringIDs.
                bool allValid = true;
                int namedCount = 0;
                foreach (int sid in stringIds)
                {
                    if (sid < 0 || sid >= stringTable.Count) { allValid = false; break; }
                    if (!string.IsNullOrEmpty(stringTable[sid])) namedCount++;
                }
                if (!allValid) continue;
                if (requireAllNamed && namedCount != stringIds.Count) continue;

                if (bestStringIds == null || stringIds.Count > bestStringIds.Count)
                {
                    bestStringIds = stringIds;
                    bestStart = start;
                }
            }

            if (bestStringIds == null) return null;
            var names = new List<string>(bestStringIds.Count);
            foreach (int sid in bestStringIds)
            {
                names.Add(stringTable != null && sid >= 0 && sid < stringTable.Count
                    ? (stringTable[sid] ?? string.Empty)
                    : string.Empty);
            }
            return names;
        }

        // ---- Readers ------------------------------------------------------

        private static bool TryRead(StringBitStream br, int bits, out int value, WalkResult result, string name)
        {
            if (br.RemainingBits < bits)
            {
                value = 0;
                Desync(result, $"Ran out of bits reading '{name}' ({bits} bits, {br.RemainingBits} remain).");
                return false;
            }
            value = br.ReadUInt(bits);
            return true;
        }

        private static void SafeSkip(StringBitStream br, int bits, WalkResult result, string name)
        {
            if (br.RemainingBits < bits)
            {
                Desync(result, $"Ran out of bits skipping '{name}' ({bits} bits, {br.RemainingBits} remain).");
                return;
            }
            int left = bits;
            while (left > 0)
            {
                int chunk = Math.Min(left, 32);
                br.ReadUInt(chunk);
                left -= chunk;
            }
        }

        private static bool TryReadAscii(StringBitStream br, int bits, out string value, WalkResult result, string name)
        {
            value = string.Empty;
            if (bits % 8 != 0)
            {
                Desync(result, $"AsciiString '{name}' non-byte width {bits}.");
                return false;
            }
            if (br.RemainingBits < bits)
            {
                Desync(result, $"Short read for Ascii '{name}'.");
                return false;
            }
            var sb = new StringBuilder(bits / 8);
            for (int i = 0; i < bits / 8; i++)
                sb.Append((char)br.ReadUInt(8));
            value = sb.ToString();
            return true;
        }

        private static bool TryReadUString8(StringBitStream br, out string value, WalkResult result, string name)
        {
            value = string.Empty;
            var sb = new StringBuilder();
            while (true)
            {
                if (br.RemainingBits < 8)
                {
                    Desync(result, $"UString8 '{name}' unterminated.");
                    return false;
                }
                int c = br.ReadUInt(8);
                if (c == 0) break;
                sb.Append((char)c);
            }
            value = sb.ToString();
            return true;
        }

        private static bool TryReadUString16(StringBitStream br, out string value, WalkResult result, string name)
        {
            value = string.Empty;
            var sb = new StringBuilder();
            while (true)
            {
                if (br.RemainingBits < 16)
                {
                    Desync(result, $"UString16 '{name}' unterminated.");
                    return false;
                }
                int hi = br.ReadUInt(8);
                int lo = br.ReadUInt(8);
                if (hi == 0 && lo == 0) break;
                // Mirror legacy ReadUStringFromBits behaviour (keep hi byte).
                if (hi != 0) sb.Append((char)hi);
            }
            value = sb.ToString();
            return true;
        }

        private static void Desync(WalkResult r, string diag)
        {
            r.Desynced = true;
            r.Diagnostics.Add(diag);
        }

        // ---- Bit utilities -----------------------------------------------

        // Peeks the mpvr chunk's big-endian u16 major-version at byte
        // mpvrByteOffset+6 and maps it to a GameVariant. Unknown versions
        // default to Reach (the existing, battle-tested path).
        private static GameVariant DetectGame(byte[] bytes, int mpvrByteOffset)
        {
            int versionByteOffset = mpvrByteOffset + 6; // mpvr(4) + length(4) ... minus 2 because major is at offset 8 actually
            // Layout at mpvrByteOffset: 'm' 'p' 'v' 'r' [len u32 BE] [major u16 BE] [minor u16 BE] ...
            // Major is at +8. We observed 0x0036=Reach, 0x0089=H2A.
            int majorOffset = mpvrByteOffset + 8;
            if (majorOffset + 2 > bytes.Length) return GameVariant.Reach;
            int major = (bytes[majorOffset] << 8) | bytes[majorOffset + 1];
            return major switch
            {
                0x36 => GameVariant.Reach,
                0x89 => GameVariant.H2A,
                _ => GameVariant.Reach,
            };
        }

        private static string BytesToBitString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 8);
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                for (int bit = 7; bit >= 0; bit--)
                    sb.Append(((b >> bit) & 1) == 1 ? '1' : '0');
            }
            return sb.ToString();
        }

        private static string ReadAsciiAt(string bits, int offset, int widthBits)
        {
            if (offset < 0 || offset + widthBits > bits.Length) return string.Empty;
            if (widthBits % 8 != 0) return string.Empty;
            var sb = new StringBuilder(widthBits / 8);
            for (int i = 0; i < widthBits / 8; i++)
            {
                int ch = 0;
                for (int b = 0; b < 8; b++)
                {
                    ch = (ch << 1) | (bits[offset + i * 8 + b] == '1' ? 1 : 0);
                }
                sb.Append((char)ch);
            }
            return sb.ToString();
        }

        // Stream over a "0101"-style bit string. Matches ScriptDecompiler's
        // BitReader semantics but lives here to keep the reader self-contained.
        private sealed class StringBitStream
        {
            private readonly string _bits;
            private int _pos;

            public StringBitStream(string bits) { _bits = bits; _pos = 0; }

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
                    if (_bits[_pos++] == '1') val |= 1;
                }
                return val;
            }

            public int ReadSInt(int bitCount)
            {
                int u = ReadUInt(bitCount);
                if (bitCount <= 0) return 0;
                int signBit = 1 << (bitCount - 1);
                if ((u & signBit) == 0) return u;
                int mask = (1 << bitCount) - 1;
                return -(((~u) + 1) & mask);
            }

            public string RemainingBitString() => _bits.Substring(_pos);

            public void Advance(int bits)
            {
                _pos = Math.Min(_bits.Length, _pos + Math.Max(0, bits));
            }

            public void Rewind(int bits)
            {
                _pos = Math.Max(0, _pos - Math.Max(0, bits));
            }

            public string FullBits() => _bits;
        }

        // Actually invokes ScriptDecompiler.DecompileDetailed at candidate
        // offsets ±128 bits around the current walker position. Returns the
        // signed shift whose decoder reads the most bits AND produces
        // plausible counts. Real alignment consumes the whole script tree;
        // garbage offsets bail out almost immediately.
        private static int FindRealMeglOffset(string fullBits, int curPos, GameVariant game)
        {
            int bestShift = 0;
            int bestScore = -1;
            for (int shift = -128; shift <= 128; shift++)
            {
                int start = curPos + shift;
                if (start < 0 || start >= fullBits.Length) continue;
                string candidate = fullBits.Substring(start);

                var r = ScriptDecompiler.DecompileDetailed(candidate, labelNames: null, game: game);
                if (r.ConditionCount == 0 && r.ActionCount == 0 && r.TriggerCount == 0) continue;
                if (r.ConditionCount > 640 || r.ActionCount > 2047 || r.TriggerCount > 128) continue;

                // Prefer more bits consumed; tie-break toward shift=0.
                int score = r.BitsConsumed * 10000 - Math.Abs(shift);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestShift = shift;
                }
            }
            return bestShift;
        }

        // Scans a small bit window around the current megl handoff for the
        // offset that yields a plausible MegaloScript header — where reading
        // condCount(10), stepping minimum condition-record sizes, then
        // actCount(11), minimum action sizes, then trigCount(8) all produce
        // small-enough integers. Returns the (signed) shift in bits needed.
        //
        // If nothing plausible is found inside the window, returns 0 so the
        // caller just falls through with the existing position.
        private static int FindPlausibleMeglOffset(string bits)
        {
            // Plausibility thresholds — generous but bounded.
            const int MaxCond = 640;
            const int MaxAct = 2047;
            const int MaxTrig = 128;
            // Minimum bits per record (H2A). These are lower bounds; real
            // records with variant payloads are usually much bigger, so this
            // just filters out obviously wrong offsets.
            const int CondMin = 27; // Type(5)+NOT(1)+OR(10)+CondOff(11)
            const int ActMin = 8;   // Type(8) opcode
            const int TrigMin = 65; // Type(3)+Attr(4)+CondOff(10)+CondCnt(10)+ActOff(11)+ActCnt(11)+2×8

            int best = 0;
            int bestScore = -1;
            for (int shift = -512; shift <= 512; shift++)
            {
                int p = shift;
                if (p < 0 || p + 10 > bits.Length) continue;
                int cc = ReadUInt(bits, p, 10);
                if (cc == 0 || cc > MaxCond) continue;
                int p1 = p + 10 + cc * CondMin;
                if (p1 + 11 > bits.Length) continue;
                int ac = ReadUInt(bits, p1, 11);
                if (ac == 0 || ac > MaxAct) continue;
                int p2 = p1 + 11 + ac * ActMin;
                if (p2 + 8 > bits.Length) continue;
                int tc = ReadUInt(bits, p2, 8);
                if (tc > MaxTrig) continue;

                // Prefer offsets that "fill" the stream — where total
                // consumed bits (counts + minimum record payloads + trig
                // count) accounts for a large fraction of the remaining
                // stream. A noise-like offset reads tiny counts and stops
                // immediately with lots of stream left; a real megl-start
                // offset consumes most of what's there. Shift-distance is
                // only a tie-breaker.
                int minConsumed = p2 + 8 + tc * TrigMin - shift;
                int score = minConsumed * 1000 - Math.Abs(shift);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = shift;
                }
            }
            return best;
        }

        private static int ReadUInt(string bits, int pos, int width)
        {
            int v = 0;
            for (int i = 0; i < width; i++)
                v = (v << 1) | (bits[pos + i] == '1' ? 1 : 0);
            return v;
        }
    }
}

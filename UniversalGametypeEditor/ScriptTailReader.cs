// ScriptTailReader.cs
//
// Walks an array of MegaloSchema.MegaloSection records over a bit
// stream. Used by ScriptDecompiler on the post-trigger "tail" layout,
// and by GametypeReader on the full-file ReachMpvrLayout.
//
// Populates DecompileContext.Tables / Labels as it walks, so deferred
// ParamNode trees can resolve Ref0 arguments by name at render time.
//
// Any UnverifiedSection is a hard stop: the walk records a diagnostic
// and returns. This is deliberate — silently guessing widths on
// unverified territory would mis-align every section after it and
// produce garbage. The first real .bin test points at the exact
// section whose layout still needs to be nailed down.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UniversalGametypeEditor.Megalo;

namespace UniversalGametypeEditor
{
    internal static class ScriptTailReader
    {
        public sealed class TailResult
        {
            public bool Desynced;
            public readonly List<string> Diagnostics = new();
        }

        // The BitReader lives inside ScriptDecompiler (private). Rather than
        // leak its internals we accept a minimal interface — the walker only
        // needs read/position/remaining.
        public interface IBitStream
        {
            int Position { get; }
            int RemainingBits { get; }
            int ReadUInt(int bits);
            int ReadSInt(int bits);
        }

        public static TailResult Read(IBitStream br, MegaloSchema.MegaloSection[] layout, DecompileContext ctx)
        {
            var result = new TailResult();
            ReadSections(br, layout, ctx, result, scope: null);
            return result;
        }

        private static void ReadSections(
            IBitStream br,
            MegaloSchema.MegaloSection[] sections,
            DecompileContext ctx,
            TailResult result,
            Dictionary<string, object>? scope)
        {
            foreach (var s in sections)
            {
                if (result.Desynced) return;
                ReadSection(br, s, ctx, result, scope);
            }
        }

        private static void ReadSection(
            IBitStream br,
            MegaloSchema.MegaloSection section,
            DecompileContext ctx,
            TailResult result,
            Dictionary<string, object>? scope)
        {
            switch (section)
            {
                case MegaloSchema.BlankSection b:
                    SafeSkip(br, b.Bits, result, b.Name);
                    return;

                case MegaloSchema.UIntSection u:
                {
                    if (!TryRead(br, u.Bits, out int v, result, u.Name)) return;
                    scope?.Add(u.Name, v);
                    return;
                }

                case MegaloSchema.SIntSection s:
                {
                    if (br.RemainingBits < s.Bits)
                    {
                        Desync(result, $"Ran out of bits reading SInt '{s.Name}' ({s.Bits} bits needed, {br.RemainingBits} remain).");
                        return;
                    }
                    int v = br.ReadSInt(s.Bits);
                    scope?.Add(s.Name, v);
                    return;
                }

                case MegaloSchema.HexSection h:
                {
                    if (!TryRead(br, h.Bits, out int v, result, h.Name)) return;
                    scope?.Add(h.Name, v);
                    return;
                }

                case MegaloSchema.StringTableRefSection str:
                {
                    if (!TryRead(br, str.Bits, out int v, result, str.Name)) return;
                    scope?.Add(str.Name, v);
                    return;
                }

                case MegaloSchema.StringTableSection stbl:
                {
                    // Unused in the current ReachScriptTail (the script block
                    // has no HCount strings in it). Leave as a desync marker
                    // so if someone adds one to the tail layout without the
                    // walker impl we hear about it.
                    Desync(result,
                        $"StringTableSection '{stbl.Name}' reached in tail walker but not implemented here. "
                        + "Use GametypeReader, or port WalkStringTable into ScriptTailReader.");
                    return;
                }

                case MegaloSchema.CountSection c:
                {
                    if (!TryRead(br, c.CountBits, out int count, result, c.Name + ".count")) return;

                    // Sanity threshold: individual script tables in Reach top out
                    // below a few hundred. If we see an absurd count, we've almost
                    // certainly desynced upstream.
                    if (count < 0 || count > 2048)
                    {
                        Desync(result, $"Count for '{c.Name}' is {count} (out of sane range) — upstream desync.");
                        return;
                    }

                    var tablePopulate = !string.IsNullOrEmpty(c.TableKey)
                        ? new List<string>(count) : null;

                    for (int i = 0; i < count; i++)
                    {
                        var recordScope = new Dictionary<string, object>();
                        ReadSections(br, c.RecordFields, ctx, result, recordScope);
                        if (result.Desynced) return;

                        if (tablePopulate != null)
                        {
                            string? name = null;
                            if (!string.IsNullOrEmpty(c.NameFromField)
                                && recordScope.TryGetValue(c.NameFromField, out var stringIdObj)
                                && stringIdObj is int stringId)
                            {
                                name = LookupStringById(ctx, stringId);
                            }
                            // Empty when no real name resolves; the LabelRef
                            // renderer treats null/empty as a cue to emit the
                            // bare index instead of a synthetic name.
                            tablePopulate.Add(name ?? string.Empty);
                        }
                    }

                    if (tablePopulate != null && !string.IsNullOrEmpty(c.TableKey))
                    {
                        ctx.Tables[c.TableKey!] = tablePopulate;
                        // Special-case Labels so ParamNode rendering picks them up.
                        if (c.TableKey!.EndsWith("/Labels", StringComparison.OrdinalIgnoreCase))
                            ctx.Labels = tablePopulate;
                    }
                    return;
                }

                case MegaloSchema.VariantSection vs:
                {
                    if (!TryRead(br, vs.TagBits, out int tag, result, vs.Name + ".tag")) return;
                    foreach (var (id, key, fields) in vs.Variants)
                    {
                        if (id == tag)
                        {
                            var subScope = scope == null ? null : new Dictionary<string, object>();
                            ReadSections(br, fields, ctx, result, subScope);
                            if (scope != null && subScope != null)
                            {
                                foreach (var kv in subScope)
                                    scope[$"{vs.Name}.{kv.Key}"] = kv.Value;
                            }
                            return;
                        }
                    }
                    Desync(result, $"VariantSection '{vs.Name}' got unknown tag {tag}.");
                    return;
                }

                case MegaloSchema.UnverifiedSection uv:
                    Desync(result, $"Unverified section '{uv.Name}': {uv.Note}");
                    return;

                case MegaloSchema.AsciiStringSection asc:
                {
                    if (!TryReadAscii(br, asc.Bits, out string asciiVal, result, asc.Name)) return;
                    scope?.Add(asc.Name, asciiVal);
                    return;
                }

                case MegaloSchema.UString8Section u8:
                {
                    if (!TryReadUString8(br, out string u8val, result, u8.Name)) return;
                    scope?.Add(u8.Name, u8val);
                    return;
                }

                case MegaloSchema.UString16Section u16:
                {
                    if (!TryReadUString16(br, out string u16val, result, u16.Name)) return;
                    scope?.Add(u16.Name, u16val);
                    return;
                }

                case MegaloSchema.GroupSection grp:
                {
                    // Inline group — walk its children in the current scope.
                    ReadSections(br, grp.Children, ctx, result, scope);
                    return;
                }

                case MegaloSchema.SectionRefSection sr:
                {
                    if (!MegaloSchema.Layouts.TryGetValue(sr.LayoutKey, out var referenced))
                    {
                        Desync(result, $"SectionRefSection '{sr.Name}' references unknown layout '{sr.LayoutKey}'.");
                        return;
                    }
                    ReadSections(br, referenced, ctx, result, scope);
                    return;
                }

                case MegaloSchema.EnumRefSection er:
                {
                    Desync(result,
                        $"EnumRefSection '{er.Name}' (Enumref:{er.EnumName}) is not yet wired to "
                        + "the shared TypeRef reader — needs a hook into ScriptDecompiler.ReadSchemaEnum.");
                    return;
                }

                default:
                    Desync(result, $"Unknown MegaloSection type: {section.GetType().Name}");
                    return;
            }
        }

        // ---- ASCII / UString helpers --------------------------------------

        private static bool TryReadAscii(IBitStream br, int bits, out string value, TailResult result, string name)
        {
            value = string.Empty;
            if (bits % 8 != 0)
            {
                Desync(result, $"AsciiStringSection '{name}' has non-byte-aligned width {bits}.");
                return false;
            }
            if (br.RemainingBits < bits)
            {
                Desync(result, $"Ran out of bits reading Ascii '{name}' ({bits} bits needed, {br.RemainingBits} remain).");
                return false;
            }
            var sb = new StringBuilder(bits / 8);
            for (int i = 0; i < bits / 8; i++)
                sb.Append((char)br.ReadUInt(8));
            value = sb.ToString();
            return true;
        }

        private static bool TryReadUString8(IBitStream br, out string value, TailResult result, string name)
        {
            value = string.Empty;
            var sb = new StringBuilder();
            while (true)
            {
                if (br.RemainingBits < 8)
                {
                    Desync(result, $"Ran out of bits reading UString8 '{name}' (no terminator found).");
                    return false;
                }
                int c = br.ReadUInt(8);
                if (c == 0) break;
                sb.Append((char)c);
            }
            value = sb.ToString();
            return true;
        }

        private static bool TryReadUString16(IBitStream br, out string value, TailResult result, string name)
        {
            // Legacy ReadUStringFromBits semantics: null-terminated string of
            // 16-bit chars, where each char is read as an upper 8 / lower 8
            // pair and upper-zero bytes are elided from the output.
            value = string.Empty;
            var sb = new StringBuilder();
            while (true)
            {
                if (br.RemainingBits < 16)
                {
                    Desync(result, $"Ran out of bits reading UString16 '{name}' (no terminator found).");
                    return false;
                }
                int hi = br.ReadUInt(8);
                int lo = br.ReadUInt(8);
                if (hi == 0 && lo == 0) break;
                // Legacy reader appends hi only when hi != 0; mirror that.
                if (hi != 0) sb.Append((char)hi);
                // Legacy also always appends lo? Actually legacy skips lo when lo==0.
                // Re-read legacy: it only ever appended hi (ConvertToASCII(binaryChar))
                // where binaryChar was the first 8 bits, and skipped 8 bits each loop.
                // The BungieCode here treats the 16-bit pair as (high-byte, low-byte)
                // of a UTF-16 char, but legacy only kept the high byte.
            }
            value = sb.ToString();
            return true;
        }

        private static bool TryRead(IBitStream br, int bits, out int value, TailResult result, string name)
        {
            if (br.RemainingBits < bits)
            {
                value = 0;
                Desync(result, $"Ran out of bits reading '{name}' ({bits} bits needed, {br.RemainingBits} remain).");
                return false;
            }
            value = br.ReadUInt(bits);
            return true;
        }

        private static void SafeSkip(IBitStream br, int bits, TailResult result, string name)
        {
            if (br.RemainingBits < bits)
            {
                Desync(result, $"Ran out of bits skipping blank '{name}' ({bits} bits needed, {br.RemainingBits} remain).");
                return;
            }
            // Drain by reading 32-bit chunks.
            int left = bits;
            while (left > 0)
            {
                int chunk = Math.Min(left, 32);
                br.ReadUInt(chunk);
                left -= chunk;
            }
        }

        private static void Desync(TailResult result, string diag)
        {
            result.Desynced = true;
            result.Diagnostics.Add(diag);
        }

        private static string? LookupStringById(DecompileContext ctx, int stringId)
        {
            if (ctx.StringTable != null && stringId >= 0 && stringId < ctx.StringTable.Count)
                return ctx.StringTable[stringId];
            return null;
        }
    }

    // Which game's schema applies to the variant currently being parsed.
    // Set by GametypeReader from the mpvr version word (byte offset 758
    // within the mpvr chunk): 0x36 = Reach, 0x89 = H2A. Halo 4 observed
    // at 0x08-ish in the wild but we don't support it yet.
    public enum GameVariant
    {
        Reach,
        H2A,
    }

    // Parallel to ScriptDecompiler.DecompileContext but visible outside the
    // decoder so ScriptTailReader can populate it without leaking decoder
    // internals. ScriptDecompiler aliases its context to this type.
    internal sealed class DecompileContext
    {
        public GameVariant Game = GameVariant.Reach;
        public List<string>? Labels;
        public List<string>? StringTable;
        public readonly Dictionary<string, List<string>> Tables =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }
}

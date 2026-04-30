using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace UniversalGametypeEditor
{
    public partial class App : Application
    {
        // Headless "--decompile <path>" mode. Runs the new schema-driven
        // GametypeReader on a Reach .bin/.mglo and prints the walker's
        // per-section values, diagnostics, bits-consumed, and the
        // ScriptDecompiler output when the walker reaches the 'megl'
        // marker.
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--decompile", StringComparison.OrdinalIgnoreCase))
            {
                int exit = RunDecompileCli(e.Args);
                Shutdown(exit);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--decompile-script", StringComparison.OrdinalIgnoreCase))
            {
                int exit = RunDecompileScriptCli(e.Args);
                Shutdown(exit);
                return;
            }

            // --round-trip <path-to-.bin>
            // Decompile → recompile → diff bit-strings of the script section
            // so we can verify the inline/trigger compilation matches the
            // original encoding.
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--round-trip", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    string path = e.Args[1];
                    var reader = new GametypeReader();
                    var r = reader.Read(path);
                    if (string.IsNullOrEmpty(r.ScriptBits))
                    {
                        Console.Error.WriteLine("error: no script bits captured.");
                        Shutdown(1); return;
                    }
                    Console.WriteLine($"original script bits: {r.ScriptBits.Length}");
                    Console.WriteLine($"walker ok={r.Ok}, game={r.Game}");

                    string script = ScriptDecompiler.DecompileAsScript(r.ScriptBits, game: r.Game);
                    if (string.IsNullOrEmpty(script))
                    {
                        Console.Error.WriteLine("error: DecompileAsScript produced no output.");
                        Shutdown(1); return;
                    }
                    Console.WriteLine($"decompiled script: {script.Length} chars");

                    var sc = new ScriptCompiler();
                    var cr = sc.TryCompileScript(script);
                    if (!cr.Success || string.IsNullOrEmpty(cr.BinaryString))
                    {
                        Console.WriteLine("compile FAILED. diagnostics:");
                        foreach (var d in cr.Diagnostics)
                            Console.WriteLine($"  L{d.Line}:{d.Column} {d.Severity} {d.Message}");
                        Shutdown(2); return;
                    }
                    string recompiled = cr.BinaryString;
                    Console.WriteLine($"recompiled bits  : {recompiled.Length}");

                    // Read the headline counts (cond/action/trigger) from
                    // both bit-strings so we can see count-fidelity at a
                    // glance. Reach layout: cond(10) + condRecords + act(11)
                    // + actRecords + trig(9). Action/trigger counts require
                    // walking the records to know the offsets, so for the
                    // cheap diagnostic we just decode the leading 10 bits
                    // (conditionCount) which is fixed-position.
                    static int ReadBits(string s, int off, int n)
                    {
                        if (off + n > s.Length) return -1;
                        int v = 0;
                        for (int i = 0; i < n; i++) { v = (v << 1) | (s[off + i] - '0'); }
                        return v;
                    }
                    // Walk the original to extract its cond/action/trigger
                    // counts so we can report orig-vs-recomp side by side.
                    static (int cond, int act, int trig) DecodeOrigCounts(string bits)
                    {
                        int pos = 0;
                        int cond = ReadBits(bits, pos, 10); pos += 10;
                        if (cond < 0) return (-1, -1, -1);
                        // Skip cond records by re-running the decoder up to actionCount.
                        // We only need a rough start position for action count, which
                        // matches the decompiler's ParseScript ordering. The cheapest
                        // reliable way is to run DecompileDetailed once and read the
                        // counts off the result.
                        return (cond, -1, -1);
                    }
                    int origCondCount = ReadBits(r.ScriptBits, 0, 10);
                    int recompCondCount = ReadBits(recompiled, 0, 10);
                    var origDetailed = ScriptDecompiler.DecompileDetailed(r.ScriptBits, labelNames: null, game: r.Game);
                    Console.WriteLine($"cond  : orig={origDetailed.ConditionCount,4}  recomp={recompCondCount,4}");
                    Console.WriteLine($"action: orig={origDetailed.ActionCount,4}  recomp={sc.DiagActionCount,4}  (delta={sc.DiagActionCount - origDetailed.ActionCount:+#;-#;0})");
                    Console.WriteLine($"trig  : orig={origDetailed.TriggerCount,4}  recomp={sc.DiagTriggerCount,4}  (delta={sc.DiagTriggerCount - origDetailed.TriggerCount:+#;-#;0}, lower is expected — inline optimization)");

                    int common = Math.Min(r.ScriptBits.Length, recompiled.Length);
                    int firstDiff = -1;
                    int diffCount = 0;
                    for (int i = 0; i < common; i++)
                    {
                        if (r.ScriptBits[i] != recompiled[i])
                        {
                            if (firstDiff < 0) firstDiff = i;
                            diffCount++;
                        }
                    }
                    Console.WriteLine($"first diff @ bit : {(firstDiff < 0 ? "(none in common prefix)" : firstDiff.ToString())}");
                    Console.WriteLine($"differing bits   : {diffCount}/{common} ({(common == 0 ? 0 : 100.0 * diffCount / common):F2}%)");
                    Console.WriteLine($"length delta     : {recompiled.Length - r.ScriptBits.Length}");
                    if (firstDiff >= 0)
                    {
                        int show = Math.Min(64, common - firstDiff);
                        Console.WriteLine($"orig    @{firstDiff,5}: {r.ScriptBits.Substring(firstDiff, show)}");
                        Console.WriteLine($"recomp  @{firstDiff,5}: {recompiled.Substring(firstDiff, show)}");
                    }
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); Console.Error.WriteLine(ex.StackTrace); }
                Shutdown(0);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--scan-script", StringComparison.OrdinalIgnoreCase))
            {
                int exit = RunScanScriptCli(e.Args);
                Shutdown(exit);
                return;
            }

            // --diff-trace <path> [outdir]
            // Dump per-condition + per-action bit positions for the original
            // ScriptBits AND the recompiled ScriptBits side-by-side, so the
            // first divergent record is obvious. Used to track encoder
            // fidelity bugs.
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--diff-trace", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    string path = e.Args[1];
                    string outDir = e.Args.Length >= 3 ? e.Args[2] : System.IO.Path.GetTempPath();
                    Directory.CreateDirectory(outDir);

                    var reader = new GametypeReader();
                    var r = reader.Read(path);
                    string pass1 = ScriptDecompiler.DecompileAsScript(r.ScriptBits, labelNames: r.Labels, game: r.Game, stringTable: r.StringTable) ?? "";
                    var p1Det = ScriptDecompiler.DecompileDetailed(r.ScriptBits, labelNames: r.Labels, game: r.Game);

                    var sc = new ScriptCompiler();
                    var cr = sc.TryCompileScript(pass1);
                    string recompiled = cr.BinaryString ?? "";
                    int compiledLen = sc.GetCompiledSectionBits();
                    string compiledOnly = compiledLen > 0 ? recompiled.Substring(0, Math.Min(compiledLen, recompiled.Length)) : recompiled;
                    string origTail = p1Det.TriggerSectionBits > 0 ? r.ScriptBits.Substring(p1Det.TriggerSectionBits) : "";
                    string spliced = compiledOnly + origTail;

                    // Trace original
                    var trOrig = new System.Text.StringBuilder();
                    ScriptDecompiler.ConditionTrace = trOrig;
                    ScriptDecompiler.ActionTrace = trOrig;
                    var _o = ScriptDecompiler.DecompileDetailed(r.ScriptBits, labelNames: null, game: r.Game);
                    ScriptDecompiler.ConditionTrace = null;
                    ScriptDecompiler.ActionTrace = null;
                    File.WriteAllText(System.IO.Path.Combine(outDir, "trace_orig.txt"), trOrig.ToString());

                    // Encoder-side trace (per Megl_If payload widths) on the
                    // recompile path, so we can pinpoint which compiled cond
                    // emits a different payload width than the decoder reads.
                    var encTrace = new System.Text.StringBuilder();
                    ScriptCompiler.CompilerCondTrace = encTrace;
                    ScriptCompiler.CompilerActionTrace = encTrace;
                    var sc2 = new ScriptCompiler();
                    var cr2 = sc2.TryCompileScript(pass1);
                    ScriptCompiler.CompilerCondTrace = null;
                    ScriptCompiler.CompilerActionTrace = null;
                    File.WriteAllText(System.IO.Path.Combine(outDir, "trace_encoder.txt"), encTrace.ToString());
                    string recompiled2 = cr2.BinaryString ?? recompiled;

                    // Trace spliced (recompiled compiled-section + orig tail)
                    int compiledLen2 = sc2.GetCompiledSectionBits();
                    string compiledOnly2 = compiledLen2 > 0 ? recompiled2.Substring(0, Math.Min(compiledLen2, recompiled2.Length)) : recompiled2;
                    string spliced2 = compiledOnly2 + (p1Det.TriggerSectionBits > 0 ? r.ScriptBits.Substring(p1Det.TriggerSectionBits) : "");

                    var trNew = new System.Text.StringBuilder();
                    ScriptDecompiler.ConditionTrace = trNew;
                    ScriptDecompiler.ActionTrace = trNew;
                    var _n = ScriptDecompiler.DecompileDetailed(spliced2, labelNames: null, game: r.Game);
                    ScriptDecompiler.ConditionTrace = null;
                    ScriptDecompiler.ActionTrace = null;
                    File.WriteAllText(System.IO.Path.Combine(outDir, "trace_recomp.txt"), trNew.ToString());

                    Console.WriteLine($"orig section bits: {p1Det.TriggerSectionBits}  c/a/t: {p1Det.ConditionCount}/{p1Det.ActionCount}/{p1Det.TriggerCount}");
                    Console.WriteLine($"recomp section bits: {compiledLen}  c/a/t: {sc.DiagConditionCount}/{sc.DiagActionCount}/{sc.DiagTriggerCount}");

                    var origLines = trOrig.ToString().Replace("\r", "").Split('\n');
                    var newLines = trNew.ToString().Replace("\r", "").Split('\n');
                    int lim = Math.Min(origLines.Length, newLines.Length);
                    int firstDiff = -1;
                    for (int i = 0; i < lim; i++)
                    {
                        if (origLines[i] != newLines[i]) { firstDiff = i; break; }
                    }
                    if (firstDiff < 0)
                        Console.WriteLine("traces equal up to common length.");
                    else
                    {
                        Console.WriteLine($"first trace diff @ line {firstDiff}");
                        for (int i = Math.Max(0, firstDiff - 1); i < Math.Min(lim, firstDiff + 6); i++)
                        {
                            Console.WriteLine($"  orig {i,4}: {origLines[i]}");
                            Console.WriteLine($"  new  {i,4}: {newLines[i]}");
                        }
                    }
                    Console.WriteLine($"Artifacts in: {outDir}");
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); Console.Error.WriteLine(ex.StackTrace); }
                Shutdown(0);
                return;
            }

            // --visual-rt <path> [outdir]
            // Visual-equality round-trip: decompile original -> compile -> decompile
            // recompiled bits DIRECTLY (bypassing file-stitching). Reports
            // pass1==pass2 over the script source text. This is the property
            // the user actually cares about: editing the source and saving
            // should produce a file that re-decompiles to the same source,
            // even if the underlying .bin bits differ from Bungie's encoding
            // (we use Inline-action optimization, which compresses sibling
            // triggers into single Inline records — different bits, same logic).
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--visual-rt", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                int exit = 0;
                try
                {
                    string path = e.Args[1];
                    string outDir = e.Args.Length >= 3 ? e.Args[2] : System.IO.Path.GetTempPath();
                    Directory.CreateDirectory(outDir);

                    var reader = new GametypeReader();
                    var r = reader.Read(path);
                    if (string.IsNullOrEmpty(r.ScriptBits))
                    {
                        Console.Error.WriteLine("error: ScriptBits empty"); Shutdown(1); return;
                    }
                    string pass1 = ScriptDecompiler.DecompileAsScript(r.ScriptBits, labelNames: r.Labels, game: r.Game, stringTable: r.StringTable) ?? "";
                    var p1Det = ScriptDecompiler.DecompileDetailed(r.ScriptBits, labelNames: r.Labels, game: r.Game);
                    File.WriteAllText(System.IO.Path.Combine(outDir, "pass1.txt"), pass1);
                    Console.WriteLine($"pass1 chars     : {pass1.Length}");
                    Console.WriteLine($"pass1 c/a/t     : {p1Det.ConditionCount}/{p1Det.ActionCount}/{p1Det.TriggerCount}");
                    Console.WriteLine($"pass1 sectionBit: {p1Det.TriggerSectionBits}");
                    Console.WriteLine($"r.Labels        : {(r.Labels?.Count ?? -1)}");
                    Console.WriteLine($"r.StringTable   : {(r.StringTable?.Count ?? -1)}");

                    var sc = new ScriptCompiler();
                    if (r.Labels != null) { sc.LabelTable.Clear(); sc.LabelTable.AddRange(r.Labels); }
                    if (r.StringTable != null) { sc.StringTable.Clear(); sc.StringTable.AddRange(r.StringTable); }
                    var cr = sc.TryCompileScript(pass1);
                    if (!cr.Success || string.IsNullOrEmpty(cr.BinaryString))
                    {
                        Console.WriteLine("compile FAILED:");
                        foreach (var d in cr.Diagnostics) Console.WriteLine($"  L{d.Line}:{d.Column} {d.Severity} {d.Message}");
                        Shutdown(2); return;
                    }
                    string recompiled = cr.BinaryString;
                    Console.WriteLine($"compile bits    : {recompiled.Length} (vs orig section {p1Det.TriggerSectionBits})");
                    Console.WriteLine($"compile c/a/t   : {sc.DiagConditionCount}/{sc.DiagActionCount}/{sc.DiagTriggerCount}");
                    var warns = sc.EncoderDiagnostics.Where(d => d.Severity == CompilerDiagnosticSeverity.Warning).ToList();
                    Console.WriteLine($"encoder Warnings: {warns.Count}");
                    foreach (var w in warns.Take(20))
                        Console.WriteLine($"  L{w.Line}:{w.Column} {w.Message}");
                    if (warns.Count > 20) Console.WriteLine($"  ... +{warns.Count - 20} more");

                    // ---- Pass 2: splice compiled section into ORIGINAL ScriptBits, decompile that ----
                    // The compiler synthesizes a placeholder tail (stats=0, MegaloVars,
                    // weapon tunings, padding) which doesn't represent the real
                    // post-script data. The real workflow keeps the original's tail
                    // intact. So: truncate `recompiled` to just the compiled section
                    // (cond/act/trig records) and append the ORIGINAL ScriptBits' tail
                    // — this is the same splice MainWindow.CompileScript performs,
                    // INCLUDING the MegaloVars re-encode so variable-declaration
                    // edits round-trip too.
                    int compiledLen = sc.GetCompiledSectionBits();
                    string compiledOnly = compiledLen > 0 && compiledLen <= recompiled.Length
                        ? recompiled.Substring(0, compiledLen)
                        : recompiled;
                    var (varsStartRT, varsEndRT) = ScriptDecompiler.LocateReachMegaloVars(r.ScriptBits, r.Game);
                    string statsRT = r.ScriptBits.Substring(p1Det.TriggerSectionBits, varsStartRT - p1Det.TriggerSectionBits);
                    string newVarsRT = ScriptCompiler.EncodeReachMegaloVars(pass1);
                    string postVarsRT = r.ScriptBits.Substring(varsEndRT);
                    string origTail = statsRT + newVarsRT + postVarsRT;
                    string pass2Bits = compiledOnly + origTail;
                    Console.WriteLine($"compiledOnly    : {compiledOnly.Length}  origTail: {origTail.Length}  spliced: {pass2Bits.Length}");
                    string pass2 = ScriptDecompiler.DecompileAsScript(pass2Bits, labelNames: r.Labels, game: r.Game, stringTable: r.StringTable) ?? "";
                    File.WriteAllText(System.IO.Path.Combine(outDir, "pass2.txt"), pass2);
                    var p2Det = ScriptDecompiler.DecompileDetailed(pass2Bits, labelNames: null, game: r.Game);
                    Console.WriteLine($"pass2 chars     : {pass2.Length}");
                    Console.WriteLine($"pass2 c/a/t     : {p2Det.ConditionCount}/{p2Det.ActionCount}/{p2Det.TriggerCount}");

                    bool eq = string.Equals(pass1, pass2, StringComparison.Ordinal);
                    Console.WriteLine();
                    Console.WriteLine(eq ? "VERDICT: PASS (visually identical)" : "VERDICT: FAIL");
                    if (!eq)
                    {
                        var p1Lines = pass1.Replace("\r", "").Split('\n');
                        var p2Lines = pass2.Replace("\r", "").Split('\n');
                        Console.WriteLine($"line counts: p1={p1Lines.Length}  p2={p2Lines.Length}");
                        int n = Math.Min(p1Lines.Length, p2Lines.Length);
                        int shown = 0;
                        for (int i = 0; i < n && shown < 8; i++)
                        {
                            if (p1Lines[i] != p2Lines[i])
                            {
                                Console.WriteLine($"  L{i + 1}:");
                                Console.WriteLine($"    p1: {p1Lines[i]}");
                                Console.WriteLine($"    p2: {p2Lines[i]}");
                                shown++;
                            }
                        }
                    }
                    Console.WriteLine($"Artifacts in: {outDir}");
                    exit = eq ? 0 : 3;
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); Console.Error.WriteLine(ex.StackTrace); exit = 1; }
                Shutdown(exit);
                return;
            }

            // --two-pass <path> [outdir]
            // Full round-trip-verify harness:
            //   Pass 1: decompile original -> pass1.txt -> recompile -> bin1
            //   Pass 2: decompile bin1     -> pass2.txt -> recompile -> bin2
            // Reports scriptOffset, ScriptBits.Length, decompile chars,
            // c/a/t counts for each pass; binaryOutput.Length, origCompiledLen,
            // delta + encoder Warning diagnostics; and equality pass1==pass2.
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--two-pass", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                int exit = 0;
                try
                {
                    string path = e.Args[1];
                    string outDir = e.Args.Length >= 3 ? e.Args[2] : System.IO.Path.GetTempPath();
                    Directory.CreateDirectory(outDir);

                    // ---- Pass 1: read original -> decompile ----
                    var reader1 = new GametypeReader();
                    var r1 = reader1.Read(path);
                    if (string.IsNullOrEmpty(r1.ScriptBits))
                    {
                        Console.Error.WriteLine("error: Pass-1 ScriptBits empty");
                        Shutdown(1); return;
                    }
                    var rg1 = new ReadGametype();
                    rg1.ReadBinary(path);
                    int p1ScriptOffset = rg1.gt.scriptOffset;
                    int p1ScriptBitsLen = r1.ScriptBits.Length;
                    string pass1 = ScriptDecompiler.DecompileAsScript(r1.ScriptBits, game: r1.Game);
                    int p1Chars = pass1?.Length ?? 0;
                    var p1Detailed = ScriptDecompiler.DecompileDetailed(r1.ScriptBits, labelNames: null, game: r1.Game);
                    string pass1Path = System.IO.Path.Combine(outDir, "pass1.txt");
                    File.WriteAllText(pass1Path, pass1 ?? "");

                    // ---- Pass 1 compile ----
                    var sc1 = new ScriptCompiler();
                    var cr1 = sc1.TryCompileScript(pass1 ?? "");
                    string bin1 = cr1.Success ? (cr1.BinaryString ?? "") : "";
                    int p1BinLen = bin1.Length;
                    int p1OrigCompiledLen = p1Detailed.TriggerSectionBits;
                    int p1Delta = p1BinLen - p1OrigCompiledLen;

                    // ---- Pass 2: decompile bin1 (need full file context) ----
                    // Strategy: build a synthetic file by replacing the compiled
                    // section in the original file with bin1, then re-read with
                    // GametypeReader. This is what the UI does on Compile Script.
                    string pass2 = "";
                    int p2ScriptOffset = -1, p2ScriptBitsLen = -1, p2Chars = 0;
                    ScriptDecompiler.DecompileResult? p2Detailed = null;
                    int p2BinLen = -1, p2OrigCompiledLen = -1, p2Delta = -1;
                    var enc2Diags = new List<CompilerDiagnostic>();
                    bool equality = false;

                    if (cr1.Success && p1OrigCompiledLen > 0)
                    {
                        // Re-stitch: head + bin1 + tail
                        byte[] orig = File.ReadAllBytes(path);
                        var sb = new System.Text.StringBuilder(orig.Length * 8);
                        foreach (byte b in orig)
                            for (int bit = 7; bit >= 0; bit--)
                                sb.Append(((b >> bit) & 1) == 1 ? '1' : '0');
                        string fileBits = sb.ToString();
                        if (p1ScriptOffset >= 0 && p1ScriptOffset + p1OrigCompiledLen <= fileBits.Length)
                        {
                            string head = fileBits.Substring(0, p1ScriptOffset);
                            string tail = fileBits.Substring(p1ScriptOffset + p1OrigCompiledLen);
                            string newBits = head + bin1 + tail;
                            // Pad to byte boundary
                            int rem = newBits.Length % 8;
                            if (rem != 0) newBits = newBits + new string('0', 8 - rem);
                            byte[] newBytes = new byte[newBits.Length / 8];
                            for (int i = 0; i < newBytes.Length; i++)
                            {
                                int v = 0;
                                for (int bit = 0; bit < 8; bit++) v = (v << 1) | (newBits[i * 8 + bit] - '0');
                                newBytes[i] = (byte)v;
                            }
                            string p1OutBin = System.IO.Path.Combine(outDir, "pass1_out.bin");
                            File.WriteAllBytes(p1OutBin, newBytes);

                            var reader2 = new GametypeReader();
                            var r2 = reader2.Read(p1OutBin);
                            if (!string.IsNullOrEmpty(r2.ScriptBits))
                            {
                                var rg2 = new ReadGametype();
                                rg2.ReadBinary(p1OutBin);
                                p2ScriptOffset = rg2.gt.scriptOffset;
                                p2ScriptBitsLen = r2.ScriptBits.Length;
                                pass2 = ScriptDecompiler.DecompileAsScript(r2.ScriptBits, game: r2.Game) ?? "";
                                p2Chars = pass2.Length;
                                p2Detailed = ScriptDecompiler.DecompileDetailed(r2.ScriptBits, labelNames: null, game: r2.Game);

                                var sc2 = new ScriptCompiler();
                                var cr2 = sc2.TryCompileScript(pass2);
                                if (cr2.Success && cr2.BinaryString != null)
                                {
                                    p2BinLen = cr2.BinaryString.Length;
                                    p2OrigCompiledLen = p2Detailed.TriggerSectionBits;
                                    p2Delta = p2BinLen - p2OrigCompiledLen;
                                }
                                enc2Diags.AddRange(sc2.EncoderDiagnostics);
                                File.WriteAllText(System.IO.Path.Combine(outDir, "pass2.txt"), pass2);
                                equality = string.Equals(pass1 ?? "", pass2, StringComparison.Ordinal);
                            }
                        }
                    }

                    // ---- Report ----
                    var enc1Warns = sc1.EncoderDiagnostics.Where(d => d.Severity == CompilerDiagnosticSeverity.Warning).ToList();
                    var enc2Warns = enc2Diags.Where(d => d.Severity == CompilerDiagnosticSeverity.Warning).ToList();

                    Console.WriteLine(equality ? "VERDICT: PASS" : "VERDICT: FAIL");
                    Console.WriteLine();
                    Console.WriteLine("== Pass 1 ==");
                    Console.WriteLine($"  scriptOffset    : {p1ScriptOffset}");
                    Console.WriteLine($"  ScriptBits.Len  : {p1ScriptBitsLen}");
                    Console.WriteLine($"  decompile chars : {p1Chars}");
                    Console.WriteLine($"  c/a/t (orig)    : {p1Detailed.ConditionCount}/{p1Detailed.ActionCount}/{p1Detailed.TriggerCount}");
                    Console.WriteLine($"  c/a/t (recomp)  : {sc1.DiagConditionCount}/{sc1.DiagActionCount}/{sc1.DiagTriggerCount}");
                    Console.WriteLine($"  binaryOutput.Len: {p1BinLen}");
                    Console.WriteLine($"  origCompiledLen : {p1OrigCompiledLen}");
                    Console.WriteLine($"  delta           : {p1Delta:+#;-#;0}");
                    Console.WriteLine($"  encoder Warnings: {enc1Warns.Count}");
                    foreach (var w in enc1Warns.Take(20))
                        Console.WriteLine($"    L{w.Line}:{w.Column} {w.Message}");

                    Console.WriteLine();
                    Console.WriteLine("== Pass 2 ==");
                    if (p2ScriptOffset < 0)
                    {
                        Console.WriteLine("  (Pass 2 not reached — see Pass 1 failure or stitch error)");
                    }
                    else
                    {
                        Console.WriteLine($"  scriptOffset    : {p2ScriptOffset}");
                        Console.WriteLine($"  ScriptBits.Len  : {p2ScriptBitsLen}");
                        Console.WriteLine($"  decompile chars : {p2Chars}");
                        Console.WriteLine($"  c/a/t           : {p2Detailed?.ConditionCount}/{p2Detailed?.ActionCount}/{p2Detailed?.TriggerCount}");
                        Console.WriteLine($"  binaryOutput.Len: {p2BinLen}");
                        Console.WriteLine($"  origCompiledLen : {p2OrigCompiledLen}");
                        Console.WriteLine($"  delta           : {p2Delta:+#;-#;0}");
                        Console.WriteLine($"  encoder Warnings: {enc2Warns.Count}");
                        foreach (var w in enc2Warns.Take(20))
                            Console.WriteLine($"    L{w.Line}:{w.Column} {w.Message}");
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Equality pass1==pass2 : {equality}");
                    if (!equality && !string.IsNullOrEmpty(pass2))
                    {
                        // Drift hunt: dump first 3 actions from each side.
                        Console.WriteLine();
                        Console.WriteLine("== Drift (top 3 differing lines) ==");
                        var p1Lines = (pass1 ?? "").Replace("\r", "").Split('\n');
                        var p2Lines = pass2.Replace("\r", "").Split('\n');
                        int shown = 0;
                        int n = Math.Min(p1Lines.Length, p2Lines.Length);
                        for (int i = 0; i < n && shown < 3; i++)
                        {
                            if (p1Lines[i] != p2Lines[i])
                            {
                                Console.WriteLine($"  L{i + 1}:");
                                Console.WriteLine($"    p1: {p1Lines[i]}");
                                Console.WriteLine($"    p2: {p2Lines[i]}");
                                shown++;
                            }
                        }
                        if (shown == 0 && p1Lines.Length != p2Lines.Length)
                            Console.WriteLine($"  (line count differs: p1={p1Lines.Length} p2={p2Lines.Length})");
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Artifacts in: {outDir}");
                    exit = equality ? 0 : 3;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                    exit = 1;
                }
                Shutdown(exit);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--find-header", StringComparison.OrdinalIgnoreCase))
            {
                int exit = RunFindHeaderCli(e.Args);
                Shutdown(exit);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--trace-parse", StringComparison.OrdinalIgnoreCase))
            {
                int exit = RunTraceParseCli(e.Args);
                Shutdown(exit);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--trace-walk", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    GametypeReader.TraceBitPositions = true;
                    var reader = new GametypeReader();
                    var r = reader.Read(e.Args[1]);
                    foreach (var d in r.Diagnostics) Console.WriteLine(d);
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                Shutdown(0);
                return;
            }

            // --trace-actions <path> — dump per-action bit consumption.
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--trace-actions", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    var tr = new System.Text.StringBuilder();
                    ScriptDecompiler.ActionTrace = tr;
                    var reader = new GametypeReader();
                    var r = reader.Read(e.Args[1]);
                    foreach (var d in r.Diagnostics) Console.WriteLine("// " + d);
                    Console.WriteLine();
                    Console.Write(tr.ToString());
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                finally { ScriptDecompiler.ActionTrace = null; }
                Shutdown(0);
                return;
            }

            // --trace-conditions <path> — dump per-condition bit consumption
            // (start position, id, header and payload widths). Used to hunt
            // variant payload drift in condition records.
            if (e.Args.Length >= 2 && string.Equals(e.Args[0], "--trace-conditions", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    var tr = new System.Text.StringBuilder();
                    ScriptDecompiler.ConditionTrace = tr;
                    var reader = new GametypeReader();
                    var r = reader.Read(e.Args[1]);
                    foreach (var d in r.Diagnostics) Console.WriteLine("// " + d);
                    Console.WriteLine();
                    Console.Write(tr.ToString());
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                finally { ScriptDecompiler.ConditionTrace = null; }
                Shutdown(0);
                return;
            }

            // --megl-shift <N> <path> — force a pre-megl bit shift of N and
            // print the decompile. Used to probe alignment while tracking
            // down H2A pre-megl layout drift.
            if (e.Args.Length >= 3 && string.Equals(e.Args[0], "--megl-shift", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    GametypeReader.MeglShiftProbe = int.Parse(e.Args[1]);
                    var reader = new GametypeReader();
                    var r = reader.Read(e.Args[2]);
                    Console.WriteLine($"// MeglShiftProbe={e.Args[1]}");
                    foreach (var d in r.Diagnostics) Console.WriteLine("// " + d);
                    Console.WriteLine();
                    Console.Write(r.DecompiledScript ?? "");
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                Shutdown(0);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--trace-actions", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    var rg = new ReadGametype();
                    rg.ReadBinary(e.Args[1]);
                    Console.Write(ScriptDecompiler.TraceParseActions(rg.LastScriptBits ?? ""));
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                Shutdown(0);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--probe-actions", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    var rg = new ReadGametype();
                    rg.ReadBinary(e.Args[1]);
                    Console.Write(ScriptDecompiler.ProbeActionWidths(rg.LastScriptBits ?? ""));
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                Shutdown(0);
                return;
            }

            if (e.Args.Length >= 1 && string.Equals(e.Args[0], "--probe-temp", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                try
                {
                    var rg = new ReadGametype();
                    rg.ReadBinary(e.Args[1]);
                    Console.Write(ScriptDecompiler.ProbeTempNumberWidth(rg.LastScriptBits ?? ""));
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                Shutdown(0);
                return;
            }

            base.OnStartup(e);
        }

        private int RunTraceParseCli(string[] args)
        {
            AttachConsole(-1);
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: UniversalGametypeEditor.exe --trace-parse <path-to-.bin>");
                return 2;
            }
            try
            {
                var rg = new ReadGametype();
                rg.ReadBinary(args[1]);
                Console.Write(ScriptDecompiler.TraceParse(rg.LastScriptBits ?? string.Empty, 200));
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }

        // --find-header <path> <cond> <act> <trg>
        // Searches the whole bitstream for a header matching known counts.
        private int RunFindHeaderCli(string[] args)
        {
            AttachConsole(-1);
            if (args.Length < 5)
            {
                Console.Error.WriteLine("usage: UniversalGametypeEditor.exe --find-header <path> <cond> <act> <trg>");
                return 2;
            }
            try
            {
                var rg = new ReadGametype();
                rg.ReadBinary(args[1]);
                int c = int.Parse(args[2]), a = int.Parse(args[3]), t = int.Parse(args[4]);
                // Scan the WHOLE file bits, not just LastScriptBits, so we
                // can find headers upstream of the legacy handoff.
                byte[] bytes = System.IO.File.ReadAllBytes(args[1]);
                var fullBits = new System.Text.StringBuilder(bytes.Length * 8);
                foreach (byte b in bytes)
                    for (int bit = 7; bit >= 0; bit--)
                        fullBits.Append(((b >> bit) & 1) == 1 ? '1' : '0');
                Console.Write(ScriptDecompiler.FindKnownHeader(fullBits.ToString(), c, a, t));
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }

        private int RunScanScriptCli(string[] args)
        {
            AttachConsole(-1);
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: UniversalGametypeEditor.exe --scan-script <path-to-.bin> [range]");
                return 2;
            }
            int range = 128;
            if (args.Length >= 3 && int.TryParse(args[2], out var r)) range = r;
            try
            {
                var rg = new ReadGametype();
                rg.ReadBinary(args[1]);
                Console.Write(ScriptDecompiler.ScanForScriptStart(rg.LastScriptBits ?? string.Empty, range));
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }

        // --decompile-script <path-to-.bin>
        // Runs ReadGametype + ScriptDecompiler.DecompileAsScript and prints
        // the round-trip-friendly C# form to stdout.
        private int RunDecompileScriptCli(string[] args)
        {
            AttachConsole(-1);
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: UniversalGametypeEditor.exe --decompile-script <path-to-.bin>");
                return 2;
            }

            try
            {
                // Use the schema-driven walker so H2A files pick up their
                // layout + bit-widths; the walker hands off to
                // ScriptDecompiler.DecompileDetailed at the megl boundary
                // with ctx.Game already set.
                var reader = new GametypeReader();
                var r = reader.Read(args[1]);
                // Always render through DecompileAsScript so the CLI matches
                // the WPF "Decompile Script" button output (no comment-form).
                string s = !string.IsNullOrEmpty(r.ScriptBits)
                    ? ScriptDecompiler.DecompileAsScript(r.ScriptBits, game: r.Game)
                    : string.Empty;
                Console.Write(s);
                return r.Ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                return 1;
            }
        }

        private int RunDecompileCli(string[] args)
        {
            AttachConsole(-1);
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: UniversalGametypeEditor.exe --decompile <path-to-.bin>");
                return 2;
            }

            string path = args[1];
            try
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file not found: {path}");
                    return 1;
                }

                var reader = new GametypeReader();
                var r = reader.Read(path);

                Console.WriteLine($"// === GametypeReader output for {Path.GetFileName(path)} ===");
                Console.WriteLine($"// File bits: {r.TotalFileBits}, walker consumed: {r.BitsConsumed}");
                Console.WriteLine($"// Walker status: {(r.Ok ? "ok" : "desynced")}");
                if (r.Diagnostics.Count > 0)
                {
                    Console.WriteLine("// Diagnostics:");
                    foreach (var d in r.Diagnostics)
                        Console.WriteLine($"//   - {d}");
                }
                Console.WriteLine();
                Console.WriteLine("// Parsed values (top-level):");
                foreach (var kv in r.Values)
                {
                    Console.WriteLine($"//   {kv.Key} = {FormatValue(kv.Value)}");
                }
                Console.WriteLine();

                if (!string.IsNullOrEmpty(r.DecompiledScript))
                {
                    Console.WriteLine("// === ScriptDecompiler output ===");
                    Console.WriteLine(r.DecompiledScript);
                }

                return r.Ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static string FormatValue(object? v)
        {
            if (v == null) return "null";
            if (v is string s) return "\"" + s + "\"";
            if (v is System.Collections.IDictionary d) return "{" + d.Count + " fields}";
            if (v is System.Collections.IList l) return "[" + l.Count + "]";
            return v.ToString() ?? "";
        }

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);
    }
}

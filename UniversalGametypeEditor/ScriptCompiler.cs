using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static UniversalGametypeEditor.ReadGametype;
using static UniversalGametypeEditor.ScriptCompiler;
using static UniversalGametypeEditor.Enums.Enums;
using System.Diagnostics;
using UniversalGametypeEditor.Megalo;


namespace UniversalGametypeEditor
{




    // ------------------------------------------------------------
    // Diagnostics + transactional compile API (used by the UI)
    // ------------------------------------------------------------
    public enum CompilerDiagnosticSeverity { Info, Warning, Error }

    public sealed record CompilerDiagnostic(
        CompilerDiagnosticSeverity Severity,
        string Message,
        int Line,
        int Column
    );

    public sealed record CompileResult(
        bool Success,
        string? BinaryString,
        List<CompilerDiagnostic> Diagnostics
    );

    public class Player
    {

        public string Name { get; set; }

        public Player(string name)
        {
            Name = name;
        }


    }


    public class EntityManager
    {
        private List<Player> _players = new List<Player>();
        private List<GameObject> _objects = new List<GameObject>();
        private List<int> _numbers = new List<int>();
        private Stack<int> _availablePlayerIndices = new Stack<int>();
        private Stack<int> _availableObjectIndices = new Stack<int>();
        private Stack<int> _availableNumberIndices = new Stack<int>();

        public EntityManager()
        {
            // Initialize with some default players, objects, or numbers if needed
        }

        public Player CreatePlayer(string name)
        {
            int index;
            if (_availablePlayerIndices.Count > 0)
            {
                index = _availablePlayerIndices.Pop();
                Debug.WriteLine($"Recycled player index: {index}");
            }
            else
            {
                index = _players.Count;
                Debug.WriteLine($"New player index: {index}");
            }

            var player = new Player(name);

            // Ensure the list is large enough to hold the new player
            if (index >= _players.Count)
            {
                _players.Add(player);
            }
            else
            {
                _players[index] = player;
            }

            return player;
        }

        public GameObject CreateObject(string name)
        {
            int index;
            if (_availableObjectIndices.Count > 0)
            {
                index = _availableObjectIndices.Pop();
                Debug.WriteLine($"Recycled object index: {index}");
            }
            else
            {
                index = _objects.Count;
                Debug.WriteLine($"New object index: {index}");
            }

            var gameObject = new GameObject(name);

            // Ensure the list is large enough to hold the new object
            if (index >= _objects.Count)
            {
                _objects.Add(gameObject);
            }
            else
            {
                _objects[index] = gameObject;
            }

            return gameObject;
        }

        public int CreateNumber(int value)
        {
            int index;
            if (_availableNumberIndices.Count > 0)
            {
                index = _availableNumberIndices.Pop();
                Debug.WriteLine($"Recycled number index: {index}");
            }
            else
            {
                index = _numbers.Count;
                Debug.WriteLine($"New number index: {index}");
            }

            // Ensure the list is large enough to hold the new number
            if (index >= _numbers.Count)
            {
                _numbers.Add(value);
            }
            else
            {
                _numbers[index] = value;
            }

            return index;
        }

        public void RemovePlayer(int index)
        {
            if (index >= 0 && index < _players.Count)
            {
                _players[index] = null!;
                _availablePlayerIndices.Push(index);
                Debug.WriteLine($"Recycled player index: {index}");
            }
        }

        public void RemoveObject(int index)
        {
            if (index >= 0 && index < _objects.Count)
            {
                _objects[index] = null!;
                _availableObjectIndices.Push(index);
                Debug.WriteLine($"Recycled object index: {index}");
            }
        }

        public void RemoveNumber(int index)
        {
            if (index >= 0 && index < _numbers.Count)
            {
                _numbers[index] = 0;
                _availableNumberIndices.Push(index);
                Debug.WriteLine($"Recycled number index: {index}");
            }
        }

        public Player GetPlayer(int index)
        {
            if (index >= 0 && index < _players.Count)
            {
                return _players[index];
            }
            throw new Exception($"Player at index '{index}' not found.");
        }

        public GameObject GetObject(int index)
        {
            if (index >= 0 && index < _objects.Count)
            {
                return _objects[index];
            }
            throw new Exception($"Object at index '{index}' not found.");
        }

        public int GetNumber(int index)
        {
            if (index >= 0 && index < _numbers.Count)
            {
                return _numbers[index];
            }
            throw new Exception($"Number at index '{index}' not found.");
        }

        public int GetPlayerIndex(Player player)
        {
            return _players.IndexOf(player);
        }

        public int GetObjectIndex(GameObject gameObject)
        {
            return _objects.IndexOf(gameObject);
        }
    }


    internal partial class ScriptCompiler

    {


        // ------------------------------------------------------------
        // Public compile entry used by MainWindow.xaml.cs
        // - Returns diagnostics and does NOT throw for script errors.
        // - UI should only write the file when Success == true.
        // ------------------------------------------------------------
        // Translate the decompiler's user-facing dialect into Roslyn-
        // parseable C# so the existing compile pipeline can read it.
        //   `void {`               → `void __OnTick() {`
        //   `foreach player {`     → `foreach (var current_player in __players) {`
        //   `foreach player randomly {` → ` … in __playersRandomly`
        //   `foreach team {`       → `foreach (var current_team in __teams) {`
        //   `foreach object {`     → `foreach (var current_object in __objects) {`
        //   `foreach object with label "X" {` → `foreach (var current_object in __objectsWithLabel("X")) {`
        //   `foreach object with label label[N] {` → `foreach (var current_object in __objectsWithLabel(N)) {`
        //   `foreach object with filter F {` → `foreach (var current_object in __objectsWithFilter(F)) {`
        // The original script-side meaning is preserved as the
        // collection-expression token, which downstream compilation
        // recognizes via name matching.
        public static string PreprocessDialect(string script)
        {
            if (string.IsNullOrEmpty(script)) return script ?? string.Empty;
            string s = script;

            // Rate literals: the decompiler emits TimerRate values verbatim
            // (`-100%`, `25%`, `0%`). Roslyn can't parse those, so rewrite
            // them into the internal snake-case enum form (`rate_minus_100`,
            // `rate_25`, `rate_0`) BEFORE Roslyn touches the source. The
            // surrounding context is always a call argument or comma-list,
            // and we anchor on `(`, `,`, or whitespace so we don't catch
            // arbitrary `-100%` in identifiers (none exist in megalo).
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"(?<=[(,\s])(-?\d+)\s*%",
                m =>
                {
                    string body = m.Groups[1].Value;
                    bool neg = body.StartsWith("-");
                    if (neg) body = body.Substring(1);
                    return neg ? $"rate_minus_{body}" : $"rate_{body}";
                });
            // Anonymous OnTick wrappers — make each name unique so multiple
            // `void { … }` blocks in the same script don't collide.
            int onTickIdx = 0;
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"^\s*void\s*\{",
                _ => $"void __OnTick_{onTickIdx++}() {{",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            // foreach object with label X {  — X is "name", label[N], or a
            // bare integer index. The decompiler emits "name" for resolved
            // forge labels and bare ints when no name is known; the legacy
            // `label[N]` form is accepted for backward compat.
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+object\s+with\s+label\s+(""[^""]*""|label\[\d+\]|\d+)\s*\{",
                m => $"foreach (var current_object in __objectsWithLabel({m.Groups[1].Value})) {{");
            // foreach object with filter <expr> {
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+object\s+with\s+filter\s+([^\{]+)\{",
                m => $"foreach (var current_object in __objectsWithFilter({m.Groups[1].Value.Trim()})) {{");
            // foreach player randomly {
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+player\s+randomly\s*\{",
                "foreach (var current_player in __playersRandomly) {");
            // foreach player|team|object {
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+player\s*\{",
                "foreach (var current_player in __players) {");
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+team\s*\{",
                "foreach (var current_team in __teams) {");
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"foreach\s+object\s*\{",
                "foreach (var current_object in __objects) {");

            // ----- Variable redesign Turn 4c (must run BEFORE Turn 2) -------
            // Decompiler annotates slot references inside trigger bodies
            // at every expression position (assignment LHS, condition
            // operands, call args, receivers) with `<priority>? <type>`.
            // The annotation is informational; strip it before Turn 2's
            // priority-keyword rewrite (which matches the same surface
            // pattern) so trigger-body annotations don't get converted
            // into `[Priority(...)]` attribute decls. Brace-depth tracking
            // restricts the strip to lines whose start position is inside
            // a trigger body (depth > 0); top-level decls are left intact.
            //
            // Pattern is mid-line — uses `\b` instead of `^` so it matches
            // annotations like `if (int num1 == int num2)` and
            // `Timer current_player.tmr1.set_rate(100)`. Lookahead
            // `(?=\w)` requires an identifier to follow.
            {
                var lines = s.Replace("\r\n", "\n").Split('\n');
                int depth = 0;
                var stripRx = new System.Text.RegularExpressions.Regex(
                    @"\b(low\s+|high\s+|local\s+)?(int|number|timer|player|object|team|Number|Timer|Player|Object|Team)\s+(?=\w)");
                for (int i = 0; i < lines.Length; i++)
                {
                    int startDepth = depth;
                    foreach (char c in lines[i])
                    {
                        if (c == '{') depth++;
                        else if (c == '}') depth--;
                    }
                    if (startDepth > 0)
                        lines[i] = stripRx.Replace(lines[i], "");
                }
                s = string.Join("\n", lines);
            }

            // ----- Variable redesign Turn 2 ---------------------------------
            // Decompiler emits decls in the form
            //     <priority>? <type> <name> = <init>;
            // where a missing priority means `low`. Internal encoder still
            // wants the legacy `[Priority("X")] <type> <name> = <init>;`
            // form because ProcessLocalDeclaration parses the attribute.
            // Rewrite the leading-keyword decl back to attribute form
            // BEFORE the lowercase→Pascal type rewrites below so both
            // type cases are accepted. The bare-low form (`int num1 = 0;`)
            // doesn't need rewriting — ProcessLocalDeclaration defaults
            // priority to `low` when no [Priority] attribute is present.
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"^(\s*)(low|high|local)\s+(int|number|timer|player|object|team|Number|Timer|Player|Object|Team)\b",
                "$1[Priority(\"$2\")] $3",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            // ----- Variable redesign Turn 3 ---------------------------------
            // Per-scope sub-pool rewrites. Decompiler emits forms like
            //   current_player.num1   →  playernumber0  (receiver-disambiguated)
            //   current_object.tmr2   →  objecttimer1
            //   current_team.plr3     →  teamplayer2
            // Receivers can be `current_<scope>`, `globalplayer<N>` /
            // `globalobject<N>` / `globalteam<N>` (long form), the Turn 1
            // short forms `plr<N>` / `obj<N>` / `tm<N>`, or `temp_<kind>_<N>`.
            //
            // Order matters: sub-pool rewrites MUST run before the global
            // short-name rewrite below, because that step rewrites the
            // receiver (`plr1` → `globalplayer0`) and the receiver pattern
            // here matches the short form.
            string[] kindShort = { "num", "tmr", "plr", "obj", "tm" };
            string[] kindLong  = { "number", "timer", "player", "object", "team" };
            string RewriteScopedSubpool(string src, string receiverPattern, string scopePrefix)
            {
                for (int i = 0; i < kindShort.Length; i++)
                {
                    src = System.Text.RegularExpressions.Regex.Replace(
                        src,
                        $@"\b({receiverPattern})\.{kindShort[i]}(\d+)\b",
                        m => $"{m.Groups[1].Value}.{scopePrefix}{kindLong[i]}{int.Parse(m.Groups[2].Value) - 1}");
                }
                return src;
            }
            s = RewriteScopedSubpool(s, @"current_player|globalplayer\d+|plr\d+|temp_player_\d+", "player");
            s = RewriteScopedSubpool(s, @"current_object|globalobject\d+|obj\d+|temp_obj_\d+",    "object");
            s = RewriteScopedSubpool(s, @"current_team|globalteam\d+|tm\d+|temp_team_\d+",         "team");

            // Sub-pool DECL names use receiver-qualified dotted form
            // (`player.num1`, `object.tmr2`, `team.plr3`) so scope is
            // explicit at the decl site and mirrors the use-site
            // syntax (`current_player.num1`). Rewrite to internal long
            // form 0-indexed:
            //   player.num<N>  →  playernumber<N-1>
            //   object.tmr<N>  →  objecttimer<N-1>
            //   team.plr<N>    →  teamplayer<N-1>
            // Word boundary `\b` prevents matching receivers like
            // `current_player.num1` (the `_` before `player` is a word
            // char, killing the boundary). Order before short-name
            // rewrite so the bare scope prefix gets caught first.
            string RewriteScopedDecl(string src, string scopePrefix)
            {
                for (int i = 0; i < kindShort.Length; i++)
                {
                    src = System.Text.RegularExpressions.Regex.Replace(
                        src,
                        $@"\b{scopePrefix}\.{kindShort[i]}(\d+)\b",
                        m => $"{scopePrefix}{kindLong[i]}{int.Parse(m.Groups[1].Value) - 1}");
                }
                return src;
            }
            s = RewriteScopedDecl(s, "player");
            s = RewriteScopedDecl(s, "object");
            s = RewriteScopedDecl(s, "team");

            // ----- Variable redesign Turn 1 ---------------------------------
            // The decompiler emits 1-based short names for global pool slots
            // (`num1, tmr1, plr1, obj1, tm1`). Internally the encode pipeline
            // still works in 0-based long names (`globalnumber0`, …). Rewrite
            // here so the rest of the compiler doesn't have to know about
            // the new naming scheme. Use word boundaries so we don't match
            // identifiers like `mynum1` or `temp_obj_0`.
            //
            // After the sub-pool rewrites above, any remaining `num<N>` /
            // `tmr<N>` / `plr<N>` / `obj<N>` / `tm<N>` is a global slot
            // reference (bare or as a member-access receiver).
            string RewriteShort(string src, string shortPrefix, string longPrefix)
                => System.Text.RegularExpressions.Regex.Replace(
                    src,
                    $@"\b{shortPrefix}(\d+)\b",
                    m => longPrefix + (int.Parse(m.Groups[1].Value) - 1).ToString());
            s = RewriteShort(s, "num", "globalnumber");
            s = RewriteShort(s, "tmr", "globaltimer");
            s = RewriteShort(s, "plr", "globalplayer");
            s = RewriteShort(s, "obj", "globalobject");
            s = RewriteShort(s, "tm",  "globalteam");

            // Lowercase type keywords → internal PascalCase (variable
            // redesign Turn 1 + Turn 5). Match the type-keyword slot:
            // each must be preceded by line-start, `]` + whitespace
            // (attribute end), or whitespace, and followed by whitespace
            // + identifier. Captures preserve the prefix so we don't
            // dissolve attribute brackets. The lookahead `(\s+\w)` skips
            // dotted scope receivers (`player.num1`) — they're caught
            // earlier by RewriteScopedDecl.
            (string lo, string hi)[] typeAliases =
            {
                ("int",    "Number"),
                ("timer",  "Timer"),
                ("player", "Player"),
                ("object", "Object"),
                ("team",   "Team"),
            };
            foreach (var (lo, hi) in typeAliases)
            {
                s = System.Text.RegularExpressions.Regex.Replace(
                    s,
                    $@"(^|\]\s+|\s){lo}(\s+\w)",
                    "$1" + hi + "$2",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
            }

            return s;
        }

        public CompileResult TryCompileScript(string script)
        {
            var diags = new List<CompilerDiagnostic>();

            // Pre-process the decompiler's dialect into valid C# so
            // Roslyn can parse it. The transformations are reversible
            // and downstream code recognizes the synthetic collection
            // identifiers by name to emit the right foreach trigger type.
            script = PreprocessDialect(script);

            // 1) Syntax (parse) diagnostics from Roslyn
            var tree = CSharpSyntaxTree.ParseText(script);
            foreach (var d in tree.GetDiagnostics())
            {
                if (d.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    continue;

                var span = d.Location.GetLineSpan();
                diags.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Error,
                    d.GetMessage(),
                    span.StartLinePosition.Line + 1,
                    span.StartLinePosition.Character + 1
                ));
            }

            if (diags.Count > 0)
                return new CompileResult(false, null, diags);

            // 2) Semantic/encoding errors inside the compiler:
            //    catch and surface as a diagnostic (best-effort line/col if available).
            try
            {
                Compile(script);
                // Merge encoder warnings collected during Compile() so the
                // caller sees which actions / TypeRefs fell back to None
                // placeholders. These don't make compilation FAIL (we still
                // emit something), but they cause re-decompile to desync.
                diags.AddRange(_encoderDiagnostics);
                string bin = GetBinaryString();
                if (string.IsNullOrWhiteSpace(bin))
                {
                    diags.Add(new CompilerDiagnostic(
                        CompilerDiagnosticSeverity.Error,
                        "Compilation produced no output.",
                        1, 1
                    ));
                    return new CompileResult(false, null, diags);
                }

                return new CompileResult(true, bin, diags);
            }
            catch (Exception ex)
            {
                diags.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Error,
                    ex.Message,
                    1, 1
                ));
                return new CompileResult(false, null, diags);
            }
        }

        // Convenience overload (older call style)
        public bool TryCompileScript(string script, out string binaryString, out List<CompilerDiagnostic> diagnostics)
        {
            var r = TryCompileScript(script);
            diagnostics = r.Diagnostics;
            binaryString = r.BinaryString ?? "";
            return r.Success;
        }

        private EntityManager _entityManager = new EntityManager();
        private Dictionary<string, VariableInfo> _variableToIndexMap = new Dictionary<string, VariableInfo>();
        private Stack<Dictionary<string, VariableInfo>> _scopeStack = new Stack<Dictionary<string, VariableInfo>>();
        private List<ActionObject> _actions = new List<ActionObject>();
        private List<ConditionObject> _conditions = new List<ConditionObject>();
        private List<TriggerObject> _triggers = new List<TriggerObject>();

        // Encoder-side warnings collected during Compile(). Surfaced through
        // TryCompileScript's diagnostics so the user sees exactly which
        // action / TypeRef / param caused a fall-back to a None placeholder
        // instead of just seeing gibberish on re-decompile.
        private readonly List<CompilerDiagnostic> _encoderDiagnostics = new();
        public IReadOnlyList<CompilerDiagnostic> EncoderDiagnostics => _encoderDiagnostics;

        private readonly record struct PendingTrigger(LocalFunctionStatementSyntax Syntax, int Index, string DefaultAttribute);

        // Triggers can be declared inside other triggers (local functions returning Trigger).
        // We reserve an index immediately (so RunTrigger can reference it), then compile later.
        private readonly Queue<PendingTrigger> _pendingTriggers = new();

        // Best-effort guard: only one wrapper per event attribute.
        private readonly HashSet<string> _eventWrapperAttributes = new(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, VariableInfo> _allDeclaredVariables = new Dictionary<string, VariableInfo>();

        // ============================================================
        // MegaloTables integration (IDs + canonical names)
        // ============================================================

        // ============================================================
        // MegaloTables integration (IDs + canonical names)
        //   - Accept any canonical action/condition name from MegaloTables
        //   - Provide a few friendly aliases (CreateObject, Delete, etc.)
        //   - Encode parameters from the MegaloTables schema
        // ============================================================

        private sealed class InlinePatch
        {
            public int InlineActionIndex;                 // where the placeholder Inline lives in _actions
            public List<ConditionObject> Conditions = new();
            public List<ActionObject> Actions = new();
        }

        private string BuildInlineBinary(int conditionOffset, int conditionCount, int actionOffset, int actionCount)
        {
            string idBits = ConvertToBinary(99, 7); // Inline
            return idBits
                + ConvertToBinary(conditionOffset, 9)
                + ConvertToBinary(conditionCount, 10)
                + ConvertToBinary(actionOffset, 10)
                + ConvertToBinary(actionCount, 11);
        }

        // Per-trigger deferred storage
        private readonly List<InlinePatch> _deferredInlines = new();

        // Per-trigger OrSequence counter. Advanced on each new nested-if's
        // condition emission so that two ifs gating the same action (e.g.
        // `if (a) { if (b) { X } }`) get DIFFERENT OrSequences, which the
        // decoder treats as an AND across groups (vs. the OR within a
        // group). Reset at trigger / inline boundaries.
        private int _orSeqCounter = 0;

        // Condition.actionOffset is LOCAL within the current container (trigger or inline).
        // We track a global base (startActionOffset / inline action start) so we can encode local offsets.
        private readonly Stack<int> _actionBaseStack = new();
        private int CurrentActionBase => _actionBaseStack.Count > 0 ? _actionBaseStack.Peek() : 0;
        private int GetLocalActionOffset(int globalActionOffset)
            => globalActionOffset <= CurrentActionBase ? 0 : (globalActionOffset - CurrentActionBase);

        private static readonly Dictionary<string, MegaloAction> _megaloActionsByName =
            MegaloTables.Actions
                .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Action lookup by numeric ID — used by RVT-script-name resolution
        // below. The decompiler's _rvtActionNames table maps action IDs to
        // their RVT-script-form names; we invert it and look up the ID here.
        private static readonly Dictionary<int, MegaloAction> _megaloActionsById =
            MegaloTables.Actions.ToDictionary(a => a.Id, a => a);

        // Inverse of ScriptDecompiler.RvtActionNames: snake_case script
        // name → first action ID that decompiles to it. Several IDs may
        // share a script name (e.g. 35/36 → "get_scoreboard_pos"); the
        // first wins. The decompiler always emits these snake-case names
        // for known actions, so this is the authoritative resolver path.
        private static readonly Dictionary<string, int> _rvtScriptNameToActionId =
            ScriptDecompiler.RvtActionNames
                .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<int, MegaloCondition> _megaloConditionsById =
            MegaloTables.Conditions.ToDictionary(c => c.Id, c => c);

        private static readonly Dictionary<string, int> _rvtScriptNameToConditionId =
            ScriptDecompiler.RvtConditionNames
                .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, MegaloCondition> _megaloConditionsByName =
            MegaloTables.Conditions
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Script-friendly aliases -> canonical Megalo name
        private static readonly Dictionary<string, string[]> _actionAliases =
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Friendly / short names -> possible table names (first match wins)
                        ["CreateObject"] = new[] { "CreateObject", "Create", "Megl.Create" },
                        ["Create"] = new[] { "Create", "CreateObject", "Megl.Create" },

                        ["SetVar"] = new[] { "SetVar", "Set", "Megl.SET" },
                        ["Set"] = new[] { "Set", "SetVar", "Megl.SET" },

                        ["Attach"] = new[] { "Attach", "Obj.Attach" },
                        ["Detach"] = new[] { "Detach", "Obj.Detach" },

                        ["Delete"] = new[] { "Delete", "Obj.Delete" },
                        ["Kill"] = new[] { "Kill", "Obj.Kill", "Obj.kill" },

                        ["GetSpeed"] = new[] { "GetSpeed", "Obj.Speed_GET" },
                        ["DropWeapon"] = new[] { "DropWeapon", "Obj.DropWeapon" },

                        ["GetWeapon"] = new[] { "GetWeapon", "Player.Weapon_GET" },
                        ["SetBiped"] = new[] { "SetBiped", "Player.Biped_SET" },

                        // Back-compat: old full names -> likely short names
                        ["Megl.Create"] = new[] { "CreateObject", "Create", "Megl.Create" },
                        ["Megl.SET"] = new[] { "SetVar", "Set", "Megl.SET" },
                        ["Obj.Attach"] = new[] { "Attach", "Obj.Attach" },
                        ["Obj.Detach"] = new[] { "Detach", "Obj.Detach" },
                        ["Obj.Delete"] = new[] { "Delete", "Obj.Delete" },
                        ["Obj.kill"] = new[] { "Kill", "Obj.kill", "Obj.Kill" },
                        ["Obj.Speed_GET"] = new[] { "GetSpeed", "Obj.Speed_GET" },
                        ["Obj.DropWeapon"] = new[] { "DropWeapon", "Obj.DropWeapon" },
                        ["Player.Weapon_GET"] = new[] { "GetWeapon", "Player.Weapon_GET" },
                        ["Player.Biped_SET"] = new[] { "SetBiped", "Player.Biped_SET" },
                    };


        private static readonly Dictionary<string, string[]> _conditionAliases =
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Friendly / short names -> possible table names
                        ["IsOfType"] = new[] { "IsOfType", "Obj.IsOfType" },
                        ["IsElite"] = new[] { "IsElite", "Player.IsElite" },
                        ["IsSpartan"] = new[] { "IsSpartan", "Player.IsSpartan" },
                        ["IsAlive"] = new[] { "IsAlive", "Player.IsAlive" },

                        // Back-compat
                        ["Obj.IsOfType"] = new[] { "IsOfType", "Obj.IsOfType" },
                        ["Player.IsElite"] = new[] { "IsElite", "Player.IsElite" },
                        ["Player.IsSpartan"] = new[] { "IsSpartan", "Player.IsSpartan" },
                        ["Player.IsAlive"] = new[] { "IsAlive", "Player.IsAlive" },
                    };


        private static string CanonicalizeActionName(string scriptName)
        {
            // For display/debug only: prefer the actual table name if we can resolve it.
            try
            {
                return ResolveActionByName(scriptName).Name;
            }
            catch
            {
                return scriptName;
            }
        }

        private static string CanonicalizeConditionName(string scriptName)
        {
            try
            {
                return ResolveConditionByName(scriptName).Name;
            }
            catch
            {
                return scriptName;
            }
        }

        private static string TitleCaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static IEnumerable<string> BuildActionCandidates(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                yield break;

            // 1) Exact
            yield return name;

            // 2) Common sanitization (tables sometimes had '·' you can't type)
            var noDot = name.Replace("·", "");
            if (!string.Equals(noDot, name, StringComparison.OrdinalIgnoreCase))
                yield return noDot;

            // 3) Alias table
            if (_actionAliases.TryGetValue(name, out var aliasList))
            {
                foreach (var a in aliasList)
                    if (!string.IsNullOrWhiteSpace(a))
                        yield return a;
            }

            // 4) Derived from dotted name (Obj.Attach -> Attach, Player.Biped_SET -> SetBiped)
            var lastDot = name.LastIndexOf('.');
            var suffix = lastDot >= 0 ? name.Substring(lastDot + 1) : null;
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                yield return suffix;

                var suffixNoDot = suffix.Replace("·", "");
                if (!string.Equals(suffixNoDot, suffix, StringComparison.OrdinalIgnoreCase))
                    yield return suffixNoDot;

                if (suffix.EndsWith("_SET", StringComparison.OrdinalIgnoreCase))
                {
                    var baseName = suffix.Substring(0, suffix.Length - 4);
                    yield return "Set" + TitleCaseFirst(baseName);
                }
                if (suffix.EndsWith("_GET", StringComparison.OrdinalIgnoreCase))
                {
                    var baseName = suffix.Substring(0, suffix.Length - 4);
                    yield return "Get" + TitleCaseFirst(baseName);
                }
            }

            // 5) Special cases
            if (string.Equals(name, "Megl.Create", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(suffix, "Create", StringComparison.OrdinalIgnoreCase))
            {
                yield return "CreateObject";
                yield return "Create";
            }

            if (string.Equals(name, "Megl.SET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(suffix, "SET", StringComparison.OrdinalIgnoreCase))
            {
                yield return "SetVar";
                yield return "Set";
                yield return "Assign";
            }

            if (string.Equals(name, "Obj.kill", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(suffix, "kill", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Kill";
            }

            // 6) Snake-case → Pascal-prefixed table names
            //    set_loadout_palette → SetLoadoutPalette / Player_SetLoadoutPalette / Player_LoadoutPalette_SET
            foreach (var c in BuildActionCandidatesExtra(name))
                yield return c;
            if (!string.IsNullOrEmpty(suffix))
                foreach (var c in BuildActionCandidatesExtra(suffix))
                    yield return c;
        }

        private static IEnumerable<string> BuildConditionCandidates(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                yield break;

            yield return name;

            var noDot = name.Replace("·", "");
            if (!string.Equals(noDot, name, StringComparison.OrdinalIgnoreCase))
                yield return noDot;

            if (_conditionAliases.TryGetValue(name, out var aliasList))
            {
                foreach (var a in aliasList)
                    if (!string.IsNullOrWhiteSpace(a))
                        yield return a;
            }

            var lastDot = name.LastIndexOf('.');
            var suffix = lastDot >= 0 ? name.Substring(lastDot + 1) : null;
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                yield return suffix;

                var suffixNoDot = suffix.Replace("·", "");
                if (!string.Equals(suffixNoDot, suffix, StringComparison.OrdinalIgnoreCase))
                    yield return suffixNoDot;
            }

            // Snake-case → "Obj_PascalCase" / "Player_PascalCase" / etc.
            // The decompiler emits is_of_type, killer_type_is, has_forge_label,
            // is_zero, is_elite, etc., and the table names are Obj_IsOfType,
            // Player_KillerTypeIs, Obj_HasForgeLabel, Timer_IsZero, …
            string pascal = SnakeToPascal(name);
            if (!string.Equals(pascal, name, StringComparison.OrdinalIgnoreCase))
                yield return pascal;
            foreach (var prefix in new[] { "Obj", "Player", "Team", "Timer", "Var", "Megl" })
            {
                yield return $"{prefix}_{pascal}";
                yield return $"{prefix}.{pascal}";
            }
        }

        private static IEnumerable<string> BuildActionCandidatesExtra(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) yield break;
            string pascal = SnakeToPascal(name);
            if (!string.Equals(pascal, name, StringComparison.OrdinalIgnoreCase))
                yield return pascal;
            foreach (var prefix in new[] { "Obj", "Player", "Team", "Timer", "Var", "Megl", "Hud", "Game" })
            {
                yield return $"{prefix}_{pascal}";
                yield return $"{prefix}.{pascal}";
                // Setter form (snake-case suffix _set / _get → trailing _SET/_GET)
                if (pascal.EndsWith("Get", StringComparison.OrdinalIgnoreCase))
                    yield return $"{prefix}_{pascal.Substring(0, pascal.Length - 3)}_GET";
                if (pascal.EndsWith("Set", StringComparison.OrdinalIgnoreCase))
                    yield return $"{prefix}_{pascal.Substring(0, pascal.Length - 3)}_SET";
            }
            // Trailing _SET / _GET fallbacks if name itself was set_x / get_x
            if (name.StartsWith("set_", StringComparison.OrdinalIgnoreCase))
                yield return SnakeToPascal(name.Substring(4)) + "_SET";
            if (name.StartsWith("get_", StringComparison.OrdinalIgnoreCase))
                yield return SnakeToPascal(name.Substring(4)) + "_GET";
        }

        private static string SnakeToPascal(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var parts = s.Split('_');
            var sb = new System.Text.StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1));
            }
            return sb.ToString();
        }

        private static MegaloAction ResolveActionByName(string name)
        {
            var tried = new List<string>();

            // PRIORITY 1: RVT-script-name → action ID lookup. The decompiler
            // emits these snake_case names; this is the authoritative path
            // that handles cases the candidate generator misses (e.g.
            // `set_hidden` → Obj_Hidden_SET, `attach_to` → Obj_Attach,
            // `place_between_me_and` → Megl_CreateBetween).
            // Strip any "receiver." prefix the decompiler adds for member
            // calls (e.g. `current_object.set_hidden`).
            var stripped = name;
            int dotIdx = stripped.LastIndexOf('.');
            if (dotIdx >= 0) stripped = stripped.Substring(dotIdx + 1);

            foreach (var probe in new[] { name, stripped })
            {
                if (string.IsNullOrWhiteSpace(probe)) continue;
                if (_rvtScriptNameToActionId.TryGetValue(probe, out int id)
                    && _megaloActionsById.TryGetValue(id, out var byId))
                {
                    return byId;
                }
                tried.Add($"rvt:{probe}");
            }

            foreach (var cand in BuildActionCandidates(name))
            {
                var key = cand.Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                // De-dup (case-insensitive) while preserving order
                if (tried.Any(t => string.Equals(t, key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                tried.Add(key);

                if (_megaloActionsByName.TryGetValue(key, out var action))
                    return action;
            }

            throw new InvalidOperationException(
                $"Action '{name}' not found. Tried: {string.Join(", ", tried)}");
        }

        private static MegaloCondition ResolveConditionByName(string name)
        {
            var tried = new List<string>();

            // PRIORITY 1: RVT-script-name → condition ID lookup. Mirrors the
            // action-resolution path. Strip "receiver." prefix the
            // decompiler adds for member calls (e.g.
            // `current_object.is_of_type` → `is_of_type`).
            var stripped = name;
            int dotIdx = stripped.LastIndexOf('.');
            if (dotIdx >= 0) stripped = stripped.Substring(dotIdx + 1);

            foreach (var probe in new[] { name, stripped })
            {
                if (string.IsNullOrWhiteSpace(probe)) continue;
                if (_rvtScriptNameToConditionId.TryGetValue(probe, out int id)
                    && _megaloConditionsById.TryGetValue(id, out var byId))
                {
                    return byId;
                }
                tried.Add($"rvt:{probe}");
            }

            foreach (var cand in BuildConditionCandidates(name))
            {
                var key = cand.Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (tried.Any(t => string.Equals(t, key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                tried.Add(key);

                if (_megaloConditionsByName.TryGetValue(key, out var cond))
                    return cond;
            }

            throw new InvalidOperationException(
                $"Condition '{name}' not found. Tried: {string.Join(", ", tried)}");
        }


        private static string TrimArg(ExpressionSyntax expr)
        {
            // Preserve quotes around string literals so token lookup
            // (StringTable / LabelTable reverse-resolve) can distinguish
            // a quoted name from a bare identifier. Other expressions get
            // the legacy unquote behavior for back-compat with the
            // identifier-flavored encoders.
            if (expr is LiteralExpressionSyntax lit
                && lit.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return lit.ToString().Trim();
            }
            return expr.ToString().Trim().Trim('"');
        }

        private static bool TryParseBoolLoose(string s, out bool value)
        {
            s = (s ?? string.Empty).Trim();
            if (s.Equals("1", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (s.Equals("0", StringComparison.OrdinalIgnoreCase) || s.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }
        private static string? GetInvokedName(ExpressionSyntax expr)
        {
            // Supports both:
            //   CreateObject(...)
            //   Obj.Delete(...)
            //   Megl.Create(...)
            return expr switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                MemberAccessExpressionSyntax ma => ma.ToString(),
                QualifiedNameSyntax qn => qn.ToString(),
                _ => null
            };
        }



        private static string ToSigned8Binary(int v)
        {
            // Two's complement 8-bit
            int b = v & 0xFF;
            return Convert.ToString(b, 2).PadLeft(8, '0');
        }

        private string ConvertVector3ToBinary(int x, int y, int z)
        {
            return ToSigned8Binary(x) + ToSigned8Binary(y) + ToSigned8Binary(z);
        }


        private HashSet<StatementSyntax> _processedStatements = new HashSet<StatementSyntax>();

        public ScriptCompiler() { }

        public string CompileScript(string script)
        {
            Compile(script);
            return GetBinaryString();
        }

        public int DiagConditionCount => _conditions.Count;
        public int DiagActionCount => _actions.Count;
        public int DiagTriggerCount => _triggers.Count;

        /// <summary>
        /// Bit length of just the compiled script section (cond count +
        /// cond records + act count + act records + trig count + trig
        /// records). GetBinaryString() appends a placeholder tail (stats=0,
        /// MegaloVars, weapon tunings, padding) which is NOT what the
        /// in-place file write wants — that callsite needs to splice the
        /// compiled section into the original file in place of the
        /// original section, leaving the original tail (string table,
        /// forge labels, …) intact.
        /// </summary>
        public int GetCompiledSectionBits()
        {
            int condBits = _conditions.Sum(c =>
                c.Parameters.Count > 0 ? (c.Parameters[0]?.Length ?? 0) : 0);
            int actBits = _actions.Sum(a =>
                a.Parameters.Count > 0 ? (a.Parameters[0]?.Length ?? 0) : 0);
            int trigBits = _triggers.Sum(t =>
                t.Parameters.Count > 0 ? (t.Parameters[0]?.Length ?? 0) : 0);
            return 10 + condBits + 11 + actBits + 9 + trigBits;
        }

        /// <summary>
        /// Encode the MegaloVars segment (20 variable pools) from the
        /// declarations at the top of the script. Layout mirrors
        /// ScriptDecompiler.ParseReachMegaloVars exactly: 20 pools in a
        /// fixed order, each with a count (variable count-bits) followed
        /// by N slots, where each slot's per-type fields are:
        ///   number  : NumericTypeRef variant + 2-bit locality
        ///   timer   : NumericTypeRef variant (no locality)
        ///   team    : 4-bit team index + 2-bit locality
        ///   player  : 2-bit locality
        ///   object  : 2-bit locality
        /// `local` priority maps to 0, `low`→1, `high`→2.
        /// Slot ordering is by the `<scope><type><N>` index (e.g.
        /// globalnumber0..3, then globalnumber4..). Missing slots are
        /// filled with default zeroes so downstream indices remain stable.
        /// </summary>
        public static string EncodeReachMegaloVars(string script)
        {
            // Pool order and count-widths must match ParseReachMegaloVars.
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

            // Parse declarations from the script. Each top-level
            // FieldDeclaration with a `[Priority(...)]` attribute (or a
            // bare `Timer x = N;`) names a slot whose pool we infer from
            // the prefix portion of the identifier.
            //   global<type><N>   → globalNs pool
            //   player<type><N>   → player<type>s pool, etc.
            // Slot index N maps directly to the pool's slot position.
            var slots = new Dictionary<string, Dictionary<int, (int locality, string init)>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, _, _) in pools)
                slots[name] = new Dictionary<int, (int, string)>();

            var preprocessed = PreprocessDialect(script);
            var tree = CSharpSyntaxTree.ParseText(preprocessed);
            var root = tree.GetCompilationUnitRoot();

            // Walk all top-level field/local declarations under any
            // namespace/class scaffolding (PreprocessDialect wraps the
            // user's script in synthetic class scopes).
            var allFieldDecls = root.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .ToList();
            var allLocalDecls = root.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .ToList();

            foreach (var fd in allFieldDecls)
                CaptureDecl(fd.AttributeLists, fd.Declaration, slots, pools);
            foreach (var ld in allLocalDecls)
                CaptureDecl(ld.AttributeLists, ld.Declaration, slots, pools);

            // Emit the 20 pools.
            var sb = new System.Text.StringBuilder();
            foreach (var (name, countBits, kind) in pools)
            {
                var pool = slots[name];
                int maxIdx = pool.Count == 0 ? -1 : pool.Keys.Max();
                int count = maxIdx + 1;
                // Cap to the pool's maximum (count-bits says how many slots can be addressed).
                int cap = (1 << countBits) - 1;
                if (count > cap) count = cap;
                sb.Append(Convert.ToString(count, 2).PadLeft(countBits, '0'));

                for (int i = 0; i < count; i++)
                {
                    pool.TryGetValue(i, out var slot);
                    int loc = slot.locality;
                    string init = slot.init ?? "0";
                    switch (kind)
                    {
                        case "number":
                            sb.Append(EncodeNumericTypeRef(init));
                            sb.Append(Convert.ToString(loc & 0x3, 2).PadLeft(2, '0'));
                            break;
                        case "timer":
                            sb.Append(EncodeNumericTypeRef(init));
                            break;
                        case "team":
                        {
                            int teamIdx = 0;
                            if (!int.TryParse(init, out teamIdx))
                            {
                                var tm = System.Text.RegularExpressions.Regex.Match(init,
                                    @"team_(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (tm.Success) int.TryParse(tm.Groups[1].Value, out teamIdx);
                            }
                            sb.Append(Convert.ToString(teamIdx & 0xF, 2).PadLeft(4, '0'));
                            sb.Append(Convert.ToString(loc & 0x3, 2).PadLeft(2, '0'));
                            break;
                        }
                        case "player":
                        case "object":
                            sb.Append(Convert.ToString(loc & 0x3, 2).PadLeft(2, '0'));
                            break;
                    }
                }
            }
            return sb.ToString();
        }

        // Helpers used by EncodeReachMegaloVars.
        private static void CaptureDecl(
            SyntaxList<AttributeListSyntax> attrs,
            VariableDeclarationSyntax decl,
            Dictionary<string, Dictionary<int, (int locality, string init)>> slots,
            (string Name, int CountBits, string Kind)[] pools)
        {
            int locality = 1; // low
            foreach (var al in attrs)
            {
                foreach (var a in al.Attributes)
                {
                    if (!a.Name.ToString().Equals("Priority", StringComparison.OrdinalIgnoreCase)) continue;
                    if (a.ArgumentList == null || a.ArgumentList.Arguments.Count == 0) continue;
                    string arg = a.ArgumentList.Arguments[0].ToString().Trim('"').ToLowerInvariant();
                    locality = arg switch
                    {
                        "local" => 0,
                        "low" => 1,
                        "high" => 2,
                        _ => 1,
                    };
                }
            }

            string typeName = decl.Type.ToString();
            // Map C# Type name → pool kind suffix.
            string? kindSuffix = typeName switch
            {
                "Number" => "number",
                "Timer"  => "timer",
                "Team"   => "team",
                "Player" => "player",
                "Object" => "object",
                _ => null,
            };
            if (kindSuffix == null) return;

            foreach (var v in decl.Variables)
            {
                string ident = v.Identifier.Text;
                // Identifier shape: `<scope><kindplural-stem><N>` where the
                // stem matches the pool name minus its final "s". Example:
                //   globalnumber0   → globalnumbers   (idx 0)
                //   playernumber3   → playernumbers   (idx 3)
                //   teamtimer1      → teamtimers      (idx 1)
                string poolName = null!;
                int idx = -1;
                foreach (var (pName, _, pKind) in pools)
                {
                    if (pKind != kindSuffix) continue;
                    string stem = pName.EndsWith("s") ? pName.Substring(0, pName.Length - 1) : pName;
                    if (ident.StartsWith(stem, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(ident.Substring(stem.Length), out int n))
                    {
                        poolName = pName;
                        idx = n;
                        break;
                    }
                }
                if (poolName == null || idx < 0) continue;

                string init = v.Initializer?.Value.ToString().Trim() ?? "0";
                slots[poolName][idx] = (locality, init);
            }
        }

        private static string EncodeNumericTypeRef(string token)
        {
            // Lightweight standalone encoder for MegaloVars initial values.
            // Mirrors ConvertNumericTypeRefToBinary's literal path: emit
            // Tag6(Int16) + 16-bit signed payload. The decoder renders
            // initial values from the schema enum so any plain integer
            // round-trips here. Non-numeric tokens (e.g. "no_number")
            // map to zero — matches the decoder's rendering for fresh slots.
            string Tag6(int t) => Convert.ToString(t, 2).PadLeft(6, '0');
            string ToS16(int v) => Convert.ToString(v & 0xFFFF, 2).PadLeft(16, '0');
            if (int.TryParse(token, out int lit))
                return Tag6(0) + ToS16(lit);                 // 0 = Int16 variant
            // Strip a leading minus on a numeric.
            if (token.StartsWith("-") && int.TryParse(token.Substring(1), out int neg))
                return Tag6(0) + ToS16(-neg);
            return Tag6(0) + ToS16(0);
        }

        private int ConvertToObjectType(string typeName)
        {
            if (Enum.TryParse(typeof(ObjectType), typeName, true, out var result) && result != null)
            {
                return (int)result;
            }
            throw new ArgumentException($"Invalid object type: {typeName}");
        }

        public void Compile(string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // Run the analyzer to process triggers
            RunAnalyzer(tree);

            // Pre-prepass: process top-level variable declarations BEFORE
            // any function body is compiled. The decompiler emits its
            // global declaration block at the top of the file (e.g.
            // `[Priority("low")] Number globalnumber0 = 0;`) and then
            // re-declares those same names inside trigger bodies as a
            // documentation hint. Without this pass, the inline-function
            // prepass would compile trigger bodies first, register slots
            // 0..N for the inner shadow decls, EndScope() would remove
            // them, and the top-level decls that follow would re-allocate
            // fresh slots starting at N+1 — duplicating storage.
            Prepass_RegisterTopLevelVariables(root);

            Prepass_RegisterInlineFunctions(root);


            // Traverse the syntax tree
            foreach (var member in root.Members)
            {
                ProcessMember(member);
                // Drain any nested-foreach triggers reserved while walking
                // this member's statements. We drain after EACH member so
                // per-trigger action ranges remain contiguous.
                DrainPendingForeachTriggers();
            }

            // Compile any nested triggers reserved during statement processing.
            DrainPendingTriggers();
            DrainPendingForeachTriggers();
        }

        private void RunAnalyzer(SyntaxTree tree)
        {
            var compilation = CSharpCompilation.Create("TriggerAnalysis", new[] { tree });
            var analyzer = new TriggerAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

            foreach (var diagnostic in diagnostics)
            {
                Debug.WriteLine(diagnostic);
            }
        }


        private void ProcessMember(MemberDeclarationSyntax member)
        {
            if (member is ClassDeclarationSyntax classDecl)
            {
                Debug.WriteLine($"Class: {classDecl.Identifier.Text}");
                foreach (var classMember in classDecl.Members)
                {
                    ProcessMember(classMember);
                }
            }
            else if (member is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    string varName = variable.Identifier.Text;
                    string type = field.Declaration.Type.ToString();
                    int networkingPriority = 1; // Default to low priority

                    // Check for access modifiers to set networking priority
                    var modifiers = field.Modifiers;
                    if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    {
                        networkingPriority = 2; // High priority
                    }
                    else if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
                    {
                        networkingPriority = 0; // Local priority
                    }
                    else if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                    {
                        networkingPriority = 1; // Low priority
                    }

                    if (type == "Player")
                    {
                        var player = _entityManager.CreatePlayer(varName);
                        int index = _entityManager.GetPlayerIndex(player);
                        var variableInfo = new VariableInfo(type, index, networkingPriority);
                        _variableToIndexMap[varName] = variableInfo;
                        _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                        Debug.WriteLine($"Initialized Player variable '{varName}' at global.player[{index}] with networking priority {networkingPriority}");
                    }
                    else if (type == "Object")
                    {
                        var gameObject = _entityManager.CreateObject(varName);
                        int index = _entityManager.GetObjectIndex(gameObject);
                        var variableInfo = new VariableInfo(type, index, networkingPriority);
                        _variableToIndexMap[varName] = variableInfo;
                        _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                        Debug.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {networkingPriority}");
                    }
                    else if (type == "Number")
                    {
                        int value = 0; // Default value for numbers
                        var numberIndex = _entityManager.CreateNumber(value);
                        var variableInfo = new VariableInfo(type, numberIndex, networkingPriority);
                        _variableToIndexMap[varName] = variableInfo;
                        _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                        Debug.WriteLine($"Initialized Number variable '{varName}' at global.number[{numberIndex}] with networking priority {networkingPriority}");
                    }
                    else
                    {
                        Debug.WriteLine($"Unhandled global variable type: {type}");
                    }
                }
            }
            else if (member is MethodDeclarationSyntax method)
            {
                Debug.WriteLine($"Method: {method.Identifier.Text}");
                _scopeStack.Push(new Dictionary<string, VariableInfo>());
                int actionCount = 0;
                if (method.Body != null)
                {
                    foreach (var statement in method.Body.Statements)
                    {
                        int conditionCount = 0; // Initialize conditionCount
                        int conditionOffset = 0; // Initialize conditionOffset
                        int actionOffset = 0; // Initialize actionOffset
                        ProcessStatement(statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset);
                    }
                }
                EndScope();
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                Debug.WriteLine($"Property: {property.Identifier.Text}");
                _scopeStack.Push(new Dictionary<string, VariableInfo>());
                int actionCount = 0;
                if (property.AccessorList != null)
                {
                    foreach (var accessor in property.AccessorList.Accessors)
                    {
                        if (accessor.Body != null)
                        {
                            foreach (var statement in accessor.Body.Statements)
                            {
                                int conditionCount = 0; // Initialize conditionCount
                                int conditionOffset = 0; // Initialize conditionOffset
                                int actionOffset = 0;
                                ProcessStatement(statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset);
                            }
                        }
                        else if (accessor.ExpressionBody != null)
                        {
                            int actionOffset = 0;
                            ProcessExpression(accessor.ExpressionBody.Expression, ref actionCount, ref actionOffset);
                        }

                    }
                }
                EndScope();
            }
            else if (member is ConstructorDeclarationSyntax constructor)
            {
                Debug.WriteLine($"Constructor: {constructor.Identifier.Text}");
                if (constructor.Body != null)
                {
                    _scopeStack.Push(new Dictionary<string, VariableInfo>());
                    int actionCount = 0;
                    foreach (var statement in constructor.Body.Statements)
                    {
                        int conditionCount = 0; // Initialize conditionCount
                        int conditionOffset = 0; // Initialize conditionOffset
                        int actionOffset = 0; // Initialize actionOffset
                        ProcessStatement(statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset);
                    }
                    EndScope();
                }
            }
            else if (member is GlobalStatementSyntax globalStatement)
            {
                Debug.WriteLine("Global Statement");
                _scopeStack.Push(new Dictionary<string, VariableInfo>()); // Ensure a new scope is pushed
                int actionCount = 0;
                if (globalStatement.Statement is LocalFunctionStatementSyntax localFunction)
                {
                    Debug.WriteLine($"Local Function: {localFunction.Identifier.Text}");
                    if (localFunction.Body != null)
                    {
                        // Call ProcessTrigger for top-level local function declarations
                        if (IsTriggerLocalFunction(localFunction))
                        {
                            ProcessTrigger(localFunction);
                        }
                        else if (localFunction.ReturnType?.ToString() == "void"
                                 && TryMapEventWrapper(localFunction.Identifier.Text, out _))
                        {
                            // Top-level event wrapper (void __OnTick_N(),
                            // void local(), etc.) — emit as a Do trigger
                            // with the matching attribute. Note: this kind
                            // of function was excluded from the inline-func
                            // prepass, so we own its compilation.
                            ProcessTrigger(localFunction);
                        }
                        else if (localFunction.ReturnType?.ToString() == "void")
                        {
                            // inline function definition: already compiled in pre-pass
                        }
                        else
                        {
                            throw new Exception($"Unsupported top-level local function '{localFunction.Identifier.Text}' return type '{localFunction.ReturnType}'.");
                        }

                    }
                }
                else if (globalStatement.Statement is LocalDeclarationStatementSyntax localDeclaration)
                {
                    // Dispatch to ProcessLocalDeclaration so all variable
                    // types (Object/Number/Timer/Player/Team) and the
                    // [Priority(...)] attribute are handled uniformly.
                    int dummyActionCount = 0;
                    int dummyActionOffset = 0;
                    ProcessLocalDeclaration(localDeclaration, ref dummyActionCount, ref dummyActionOffset);
                }
                else
                {
                    int conditionCount = 0; // Initialize conditionCount
                    int conditionOffset = 0; // Initialize conditionOffset
                    int actionOffset = 0; // Initialize actionOffset
                    ProcessStatement(globalStatement.Statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset);
                }
                EndScope();
            }
            else
            {
                Debug.WriteLine($"Unhandled member type: {member.GetType().Name}");
            }
        }

        private void ProcessExpression(ExpressionSyntax expression, ref int actionCount, ref int actionOffset)
        {
            if (expression is AssignmentExpressionSyntax assignment)
            {
                Debug.WriteLine($"Assignment: {assignment}");
                int before = _actions.Count;
                ProcessAssignment(assignment, ref actionOffset);
                actionCount += (_actions.Count - before);
            }
            else if (expression is InvocationExpressionSyntax invocation)
            {
                Debug.WriteLine($"Invocation: {invocation}");
                int before = _actions.Count;
                ProcessInvocation(invocation, ref actionOffset);
                int delta = _actions.Count - before;
                // ProcessInvocation always inserts at least one action now
                // (the resolved one or a None-placeholder). Use the actual
                // delta so callers' actionCount stays accurate.
                actionCount += delta > 0 ? delta : 1;
            }
            else if (expression is BinaryExpressionSyntax binaryExpression)
            {
                Debug.WriteLine($"Binary Expression: {binaryExpression}");
                int conditionCount = 0;
                ProcessCondition(binaryExpression, ref conditionCount, ref actionOffset, ref actionCount);
            }
            else
            {
                // Bare identifier / element access / member access etc. used
                // as a statement — treat as a no-op placeholder action so the
                // count stays aligned.
                Debug.WriteLine($"Unhandled expression form '{expression}' — emitting placeholder.");
                EmitPlaceholderAction(expression.ToString());
                actionOffset++;
                actionCount++;
            }
        }

        private int triggerActionOffset = 0;
        private (int, int) CompileInlineAction(int conditionOffset, int conditionCount, int actionOffset, int actionCount)
        {
            // Create the inline action referencing the processed condition and actions
            string conditionOffsetBinary = ConvertToBinary(conditionOffset, 9);
            string conditionCountBinary = ConvertToBinary(conditionCount, 10);
            string actionOffsetBinary = ConvertToBinary(actionOffset, 10);
            string actionCountBinary = ConvertToBinary(actionCount, 11);

            string binaryInlineAction = "1100011" + conditionOffsetBinary + conditionCountBinary + actionOffsetBinary + actionCountBinary;

            // Insert at the correct position in the actions list
            _actions.Insert(actionOffset, new ActionObject("Inline", new List<string> { binaryInlineAction }));
            Debug.WriteLine($"Added inline action: Inline({binaryInlineAction}) at index {actionOffset}");

            // Return the number of actions added and conditions processed
            return (1, conditionCount);
        }

        // Add this method to the ScriptCompiler class
        private void ProcessIfStatement(
    IfStatementSyntax ifStatement,
    ref int actionCount,
    ref int conditionCount,
    ref int actionOffset,
    ref int conditionOffset,
    bool isTopLevel,
    bool forceInline)
        {
            Debug.WriteLine($"Processing If Statement: {ifStatement.Condition}");

            // Non-last `if` in a block: compile as an Inline (action 99)
            // record. This is MCC's compiler-optimization path — the
            // resulting .bin bits won't byte-match vanilla Bungie files (which
            // use sibling triggers + RunTrigger), but on re-decompile it
            // produces VISUALLY-identical script source, which is the
            // round-trip property we care about for the editor.
            if (forceInline)
            {
                int condStart = _conditions.Count;
                int actStart = _actions.Count;

                _actionBaseStack.Push(actStart);
                try
                {
                    int ifCondCount = 0;
                    int bodyActionCount = 0;
                    int bodyConditionCount = 0;

                    ProcessCondition(ifStatement.Condition, ref ifCondCount, ref actionOffset, ref conditionOffset, 0);

                    if (ifStatement.Statement is BlockSyntax block)
                    {
                        ProcessStatementList(block.Statements, ref bodyActionCount, ref bodyConditionCount, ref actionOffset, ref conditionOffset, false);
                    }
                    else
                    {
                        ProcessStatement(ifStatement.Statement, ref bodyActionCount, ref bodyConditionCount, ref actionOffset, ref conditionOffset, false, true);
                    }
                }
                finally
                {
                    _actionBaseStack.Pop();
                }

                // Extract the conds/acts compiled into the body so we can stash
                // them for the deferred-Inline patch. The placeholder Inline
                // action goes into the parent's action stream now; offsets
                // are patched after the parent's range is finalized.
                var extractedConds = _conditions.GetRange(condStart, _conditions.Count - condStart);
                _conditions.RemoveRange(condStart, _conditions.Count - condStart);

                var extractedActs = _actions.GetRange(actStart, _actions.Count - actStart);
                _actions.RemoveRange(actStart, _actions.Count - actStart);

                conditionOffset = condStart;
                actionOffset = actStart;

                string placeholder = BuildInlineBinary(0, 0, 0, 0);
                _actions.Insert(actStart, new ActionObject("Inline", new List<string> { placeholder }));
                int inlineIndex = actStart;

                actionCount += 1;
                actionOffset += 1;

                _deferredInlines.Add(new InlinePatch
                {
                    InlineActionIndex = inlineIndex,
                    Conditions = extractedConds,
                    Actions = extractedActs
                });

                Debug.WriteLine($"Non-last if compiled as Inline (action 99) at index {inlineIndex}");
                return;
            }

            // ---- Non-top-level if: keep your existing behavior (nested-if semantics) ----
            // Allocate a fresh OrSequence for this if's condition. The
            // decoder groups conditions by OrSequence and OR's within a
            // group / AND's across groups — so two ifs gating the same
            // action (`if(a){if(b){X}}`, both with coff=X) need DIFFERENT
            // OrSequences to AND, otherwise they collapse into `if(a||b){X}`.
            // Use a per-trigger counter advanced on every fresh top-level
            // cond emission.
            int nestedIfCondCount = 0;
            int orSeq = _orSeqCounter++;
            ProcessCondition(ifStatement.Condition, ref nestedIfCondCount, ref actionOffset, ref conditionOffset, orSeq);
            conditionCount += nestedIfCondCount;

            int nestedBodyActions = 0;
            int nestedBodyConds = 0;

            if (ifStatement.Statement is BlockSyntax nestedBlock)
            {
                ProcessStatementList(nestedBlock.Statements, ref nestedBodyActions, ref nestedBodyConds, ref actionOffset, ref conditionOffset, false);
            }
            else
            {
                ProcessStatement(ifStatement.Statement, ref nestedBodyActions, ref nestedBodyConds, ref actionOffset, ref conditionOffset, false, true);
            }

            actionCount += nestedBodyActions;
            conditionCount += nestedBodyConds;
        }




        private int actionsAdded = 0;
        private int inlineActionOffset = 1;
        private int inlineActionsOffsetDiff = 0;
        // Replace the ProcessStatement method with this improved version

        /// <summary>
        /// Compile a list of statements while knowing which statement is the last in its block.
        /// This enables the rule: compile an if as a plain condition only if it is the last statement in its block;
        /// otherwise compile it as an Inline to prevent it from swallowing subsequent statements.
        /// </summary>
        private void ProcessStatementList(
            SyntaxList<StatementSyntax> statements,
            ref int actionCount,
            ref int conditionCount,
            ref int actionOffset,
            ref int conditionOffset,
            bool isTopLevel)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                int a = 0;
                int c = 0;
                // Per user-confirmed encoding rule: an `if` statement only
                // needs to be wrapped in an Inline action when ANOTHER `if`
                // appears later in the same block. A lone if (no following
                // if) can be encoded as a plain trigger-scoped condition
                // gating only the actions in its body — the megalo runtime
                // attaches each condition to a specific action via the
                // condition's actionOffset field, so subsequent unrelated
                // statements aren't bled into. Wrapping in an Inline only
                // matters when two if-blocks would otherwise share the
                // same condition range.
                // NEVER force-inline. Conditions target a SPECIFIC action via
                // their coff field (the local action offset), so an `if`
                // emitted as a plain condition only gates the first action
                // of its body — it does NOT leak into subsequent siblings.
                // The previous "wrap if N in Inline if any if follows" rule
                // was based on a misunderstanding and reordered the cond
                // pool (Inline-deferral appends conds AFTER the parent's
                // run), breaking re-decompile alignment. Always emit as a
                // plain nested-if; pass `isLastInBlock=true` so
                // ProcessIfStatement takes the nested-if path.
                ProcessStatement(statements[i], ref a, ref c, ref actionOffset, ref conditionOffset, isTopLevel, isLastInBlock: true);
                actionCount += a;
                conditionCount += c;
            }
        }

        private void ProcessStatement(StatementSyntax statement, ref int actionCount, ref int conditionCount, ref int actionOffset, ref int conditionOffset, bool isTopLevel = true, bool isLastInBlock = true)
        {
            if (_scopeStack.Count == 0)
            {
                throw new InvalidOperationException("Scope stack is empty.");
            }

            // Early check for already processed statements
            if (_processedStatements.Contains(statement))
            {
                return;
            }

            // Mark the statement as processed
            _processedStatements.Add(statement);

            // Handle different statement types
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    // Process local variable declarations
                    ProcessLocalDeclaration(localDeclaration, ref actionCount, ref actionOffset);
                    break;

                case ExpressionStatementSyntax expressionStatement:
                    // Process expressions (assignments, method calls)
                    ProcessExpression(expressionStatement.Expression, ref actionCount, ref actionOffset);

                    break;

                case IfStatementSyntax ifStatement:
                    {
                        // Always nested-if path. ProcessStatementList passes
                        // isLastInBlock=true unconditionally; the per-cond
                        // coff field points to the body's first action so no
                        // bleed occurs.
                        ProcessIfStatement(ifStatement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, isTopLevel, forceInline: false);
                        break;
                    }


                case LocalFunctionStatementSyntax localFunction:
                    Debug.WriteLine($"Local Function: {localFunction.Identifier.Text}");
                    if (IsTriggerLocalFunction(localFunction))
                    {
                        // Nested trigger declaration inside another trigger:
                        // Reserve a trigger slot, enqueue compilation, and emit RunTrigger(<index>) here.
                        int trigIndex = ReservePendingTrigger(localFunction, defaultAttribute: "OnCall");
                        var inv = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName("RunTrigger"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"Trigger{trigIndex}"))
                                )
                            )
                        );
                        ProcessInvocation(inv, ref actionOffset);
                        break;
                    }

                    // Process local function statements
                    _scopeStack.Push(new Dictionary<string, VariableInfo>());
                    int localFunctionActionCount = 0;
                    int localFunctionConditionCount = 0;

                    if (localFunction.Body != null)
                    {
                        foreach (var stmt in localFunction.Body.Statements)
                        {
                            ProcessStatement(stmt, ref localFunctionActionCount, ref localFunctionConditionCount, ref actionOffset, ref conditionOffset, false);
                        }
                    }

                    actionCount += localFunctionActionCount;
                    conditionCount += localFunctionConditionCount;
                    EndScope();
                    break;

                case BlockSyntax blockStmt:
                    // Process all statements in a block
                    ProcessStatementList(blockStmt.Statements, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                    break;

                case ForEachStatementSyntax foreachStmt:
                    // `foreach (var current_player in __players) { ... }`
                    // (and the __players/__teams/__objects/__objectsWithLabel
                    // /__objectsWithFilter/__playersRandomly variants emitted
                    // by PreprocessDialect) compile as a Foreach* trigger.
                    // Always treat this as a "nested" foreach when called
                    // from ProcessStatement — top-level foreach goes through
                    // ProcessMember's GlobalStatement branch, which routes
                    // here too but nests inside an empty outer scope. To
                    // ensure we still emit a proper Foreach trigger record
                    // either way, use nested=true and emit a RunTrigger
                    // action; it'll be a top-level RunTrigger with no
                    // wrapping trigger when called outside a parent body.
                    {
                        bool isNested = _actionBaseStack.Count > 0;
                        if (isNested)
                        {
                            ProcessForeachStatement(foreachStmt, nested: true, outerActionOffset: actionOffset);
                            // Nested foreach emits a RunTrigger action into
                            // the outer pool. ProcessForeachStatement uses a
                            // local `dummyOffset` for the RunTrigger emit so
                            // the parent's cursor isn't bumped automatically
                            // — bump it here so subsequent conditions in the
                            // parent compute their coff against the right
                            // action index.
                            actionCount++;
                            actionOffset++;
                        }
                        else
                        {
                            ProcessForeachStatement(foreachStmt, nested: false);
                        }
                    }
                    break;

                default:
                    Debug.WriteLine($"Unhandled statement type: {statement.GetType().Name}");
                    break;
            }
        }

        // Foreach triggers reserved while walking parent trigger bodies.
        // Each entry remembers the BlockSyntax body, the trigger record
        // it'll fill, the trigger type/attr, and (for Labeled) the LabelRef
        // token. They're drained by DrainPendingForeachTriggers() AFTER the
        // outer trigger finishes so its action range stays contiguous.
        private readonly record struct PendingForeachTrigger(
            BlockSyntax? Body,
            int Index,
            string TriggerType,
            string TriggerAttribute,
            string? LabelToken);

        private readonly Queue<PendingForeachTrigger> _pendingForeachTriggers = new();

        /// <summary>
        /// Compile a `foreach (var X in __collection) { body }` statement
        /// as a Foreach* trigger (Reach TriggerType = Player/RandomPlayer/
        /// Team/Object/Labeled). The collection identifier is the sentinel
        /// emitted by PreprocessDialect — recognized by name:
        ///   __players              → Player
        ///   __playersRandomly      → RandomPlayer
        ///   __teams                → Team
        ///   __objects              → Object
        ///   __objectsWithLabel(N)  → Labeled (LabelRef N)
        ///   __objectsWithFilter(F) → Labeled (FilterRef F) — placeholder
        ///
        /// When `nested` is true, the body is NOT compiled now — instead a
        /// trigger slot is reserved, a `RunTrigger(N)` action is emitted
        /// in the outer container, and the body is queued for later.
        /// This matches LocalFunctionStatementSyntax's nested-trigger model
        /// and keeps the outer trigger's action range contiguous.
        /// </summary>
        private void ProcessForeachStatement(ForEachStatementSyntax foreachStmt, bool nested = false, int outerActionOffset = 0)
        {
            var coll = foreachStmt.Expression;
            string triggerType = "Object"; // default fallback
            string? labelToken = null;

            if (coll is IdentifierNameSyntax id)
            {
                triggerType = id.Identifier.Text switch
                {
                    "__players"          => "Player",
                    "__playersRandomly"  => "RandomPlayer",
                    "__teams"            => "Team",
                    "__objects"          => "Object",
                    _                    => "Object",
                };
            }
            else if (coll is InvocationExpressionSyntax inv && inv.Expression is IdentifierNameSyntax invName)
            {
                if (invName.Identifier.Text == "__objectsWithLabel")
                {
                    triggerType = "Labeled";
                    if (inv.ArgumentList.Arguments.Count > 0)
                    {
                        // Accept "label[N]", a numeric literal, a quoted
                        // name, or a bare identifier.
                        string raw = inv.ArgumentList.Arguments[0].ToString().Trim();
                        var labelMatch = System.Text.RegularExpressions.Regex.Match(raw, @"label\[(\d+)\]");
                        if (labelMatch.Success) labelToken = labelMatch.Groups[1].Value;
                        else labelToken = raw;
                    }
                }
                else if (invName.Identifier.Text == "__objectsWithFilter")
                {
                    // Reach has no FilterRef — emit a Labeled trigger with no
                    // label so counts still align. (H2A would use type 6 +
                    // FilterRef, not handled here.)
                    triggerType = "Labeled";
                    labelToken = "none";
                }
            }

            BlockSyntax? body = foreachStmt.Statement is BlockSyntax b ? b : null;

            if (!nested)
            {
                CompileTriggerFromBody(body, triggerType, triggerAttribute: "OnTick", labelToken: labelToken);
                return;
            }

            // Nested foreach: reserve a slot, emit RunTrigger in the outer
            // action stream, queue body for later.
            int idx = _triggers.Count;
            _triggers.Add(new TriggerObject(triggerType, new List<string> { "" }));

            _pendingForeachTriggers.Enqueue(new PendingForeachTrigger(
                body, idx, triggerType, "OnTick", labelToken));

            // Emit a synthetic RunTrigger(<idx>) action in the OUTER pool.
            // Reuse the existing invocation path so action-encoding stays
            // consistent with the manual `Trigger trigger_N()` model.
            var inv2 = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("RunTrigger"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"Trigger{idx}"))
                    )
                )
            );
            int dummyOffset = outerActionOffset;
            ProcessInvocation(inv2, ref dummyOffset);
        }

        /// <summary>
        /// Compile the bodies of any foreach triggers that were reserved
        /// while walking outer trigger bodies. Run AFTER each top-level
        /// trigger's compilation so the outer's action range stays
        /// contiguous in the global pool.
        /// </summary>
        private void DrainPendingForeachTriggers()
        {
            while (_pendingForeachTriggers.Count > 0)
            {
                var p = _pendingForeachTriggers.Dequeue();
                CompileTriggerFromBody(
                    p.Body,
                    p.TriggerType,
                    p.TriggerAttribute,
                    p.LabelToken,
                    fixedIndex: p.Index);
            }
        }



        public class InlineActionCache
        {
            public int ConditionStartOffset { get; set; }
            public int InlineConditionCount { get; set; }
            public int ActionStartOffset { get; set; }
            public int InlineActionCount { get; set; }
        }
        private List<InlineActionCache> _inlineActionCaches = new List<InlineActionCache>();



        private void ProcessInlineActions()
        {
            // Iterate through all triggers to handle inline actions
            foreach (var trigger in _triggers)
            {
                int conditionOffset = Convert.ToInt32(trigger.Parameters[0].Substring(6, 9), 2);
                int conditionCount = Convert.ToInt32(trigger.Parameters[0].Substring(15, 10), 2);
                int actionOffset = Convert.ToInt32(trigger.Parameters[0].Substring(25, 10), 2);
                int actionCount = Convert.ToInt32(trigger.Parameters[0].Substring(35, 11), 2);

                int totalActionsAdded = 0;

                // Process conditions and actions within the trigger
                for (int i = conditionOffset; i < conditionOffset + conditionCount; i++)
                {
                    var condition = _conditions[i];
                    string binaryCondition = condition.Parameters.First();
                    string updatedBinaryCondition = UpdateConditionActionOffset(binaryCondition, actionOffset, true);
                    _conditions[i] = new ConditionObject(condition.ConditionType, new List<string> { updatedBinaryCondition });
                }


                // Insert inline actions into the action list above any actions that reside in the trigger
                for (int i = 0; i < totalActionsAdded; i++)
                {
                    _actions.Insert(actionOffset + i, new ActionObject("Inline", new List<string> { "InlineActionBinaryData" }));
                }

                // Update the total action count for the trigger
                actionCount += totalActionsAdded;

                // Update the trigger parameters with the new action count
                string updatedTriggerParameters = trigger.Parameters[0].Substring(0, 35) + ConvertToBinary(actionCount, 11);
                trigger.Parameters[0] = updatedTriggerParameters;
            }
        }





        private string UpdateConditionActionOffset(string binaryCondition, int actionOffset, bool isSetter)
        {
            // Extract the parts of the binary condition
            string conditionNumberBinary = binaryCondition.Substring(0, 5);
            string notBinary = binaryCondition.Substring(5, 1);
            string orSequenceBinary = binaryCondition.Substring(6, 9);
            string currentActionOffsetBinary = binaryCondition.Substring(15, 10);
            string parametersBinary = binaryCondition.Substring(25);

            // Convert the current action offset from binary to integer
            int currentActionOffset = Convert.ToInt32(currentActionOffsetBinary, 2);
            int newActionOffset;
            if (isSetter)
            {
                newActionOffset = actionOffset;
            }
            else
            {
                // Increment the current action offset by the number of actions added
                newActionOffset = actionOffset + currentActionOffset;
            }

            // Convert the new action offset to a binary string
            string newActionOffsetBinary = ConvertToBinary(newActionOffset, 10);

            // Concatenate the parts to form the updated binary condition
            string updatedBinaryCondition = conditionNumberBinary + notBinary + orSequenceBinary + newActionOffsetBinary + parametersBinary;

            return updatedBinaryCondition;
        }







        private bool IsBottomLevelBlock(IfStatementSyntax ifStatement)
        {
            // Get the parent block of the current if statement
            var parentBlock = ifStatement.Parent as BlockSyntax;

            if (parentBlock != null)
            {
                // Get the index of the current if statement within the parent block
                var index = parentBlock.Statements.IndexOf(ifStatement);

                // Check if there is a next statement in the parent block
                if (index >= 0 && index < parentBlock.Statements.Count - 1)
                {
                    // Get the next statement in the parent block
                    var nextStatement = parentBlock.Statements[index + 1];

                    // If the next statement is an if statement, return false
                    if (nextStatement is IfStatementSyntax)
                    {
                        return false;
                    }
                }
            }

            // If no non-nested if statements were found, it is a bottom level block
            return true;
        }





        private void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration, ref int actionCount, ref int actionOffset)
        {
            string networkingPriority = "low"; // Default networking priority

            // Check for priority attribute
            foreach (var attributeList in localDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString().Equals("Priority", StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0)
                        {
                            var argument = attribute.ArgumentList.Arguments[0];
                            networkingPriority = argument.ToString().Trim('"').ToLower(); // Extracts the priority level
                        }
                    }
                }
            }

            // Process the variable declarations
            string actualType = localDeclaration.Declaration.Type.ToString();

            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                string varName = variable.Identifier.Text;

                // Determine the priority value
                int priorityValue = networkingPriority switch
                {
                    "high" => 2,
                    "local" => 0,
                    _ => 1, // Default to "low"
                };

                Debug.WriteLine($"Variable '{varName}' of type '{actualType}' with priority '{networkingPriority}'");

                // A name that was already registered (top-level or earlier
                // scope) is a SHADOW redeclaration — the decompiler emits
                // these inside trigger bodies as documentation. Reuse the
                // existing slot instead of allocating a new one. Make the
                // existing global slot visible inside this scope so
                // references resolve.
                if (_variableToIndexMap.TryGetValue(varName, out var existingVariable)
                    || _allDeclaredVariables.TryGetValue(varName, out existingVariable))
                {
                    if (existingVariable.Type == actualType
                        && existingVariable.Priority != priorityValue
                        && _scopeStack.Count == 0)
                    {
                        throw new InvalidOperationException($"Variable '{varName}' of type '{actualType}' already exists with a different priority.");
                    }
                    _variableToIndexMap[varName] = existingVariable;
                    // DO NOT push shadow re-declarations onto the scope stack.
                    // Doing so would cause EndScope() to remove the entry from
                    // _variableToIndexMap when the trigger body finishes,
                    // wiping out the top-level prepass binding and forcing
                    // subsequent triggers to fall through to Int16/22b literal
                    // encoding (~12 extra bits per VarType reference).
                    // The global map already owns this name; the trigger body
                    // just needs to RESOLVE the name, not own its lifetime.
                }
                else
                {
                    if (actualType == "Object")
                    {
                        var initializer = variable.Initializer?.Value as InvocationExpressionSyntax;
                        if (initializer != null)
                        {
                            // Add the variable to the map before processing the invocation
                            var gameObject = _entityManager.CreateObject(varName);
                            int index = _entityManager.GetObjectIndex(gameObject);
                            var variableInfo = new VariableInfo(actualType, index, priorityValue);
                            _variableToIndexMap[varName] = variableInfo;
                            if (_scopeStack.Count > 0) _scopeStack.Peek()[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {priorityValue}");

                            // Process the invocation with the varOut parameter
                            ProcessInvocation(initializer, ref actionOffset, varName);
                            actionCount++;
                        }
                        else
                        {
                            var gameObject = _entityManager.CreateObject(varName);
                            int index = _entityManager.GetObjectIndex(gameObject);
                            var variableInfo = new VariableInfo(actualType, index, priorityValue);
                            _variableToIndexMap[varName] = variableInfo;
                            if (_scopeStack.Count > 0) _scopeStack.Peek()[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {priorityValue}");
                        }
                    }
                    else if (actualType == "Number")
                    {
                        var initializer = variable.Initializer?.Value as InvocationExpressionSyntax;
                        if (initializer != null)
                        {
                            // Add the variable to the map before processing the invocation
                            int value = 0; // Default value for numbers
                            var numberIndex = _entityManager.CreateNumber(value);
                            var variableInfo = new VariableInfo(actualType, numberIndex, priorityValue);
                            _variableToIndexMap[varName] = variableInfo;
                            if (_scopeStack.Count > 0) _scopeStack.Peek()[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized Number variable '{varName}' at global.number[{numberIndex}] with networking priority {priorityValue}");

                            // Process the invocation with the varOut parameter
                            ProcessInvocation(initializer, ref actionOffset, varName);
                            actionCount++;
                        }
                        else
                        {
                            int value = 0; // Default value for numbers
                            var numberIndex = _entityManager.CreateNumber(value);
                            var variableInfo = new VariableInfo(actualType, numberIndex, priorityValue);
                            _variableToIndexMap[varName] = variableInfo;
                            if (_scopeStack.Count > 0) _scopeStack.Peek()[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized Number variable '{varName}' at global.number[{numberIndex}] with networking priority {priorityValue}");
                        }
                    }
                    else if (actualType == "Player" || actualType == "Team" || actualType == "Timer")
                    {
                        // Track the variable in the symbol table so references
                        // resolve, but don't allocate engine slots — those
                        // are emitted via ProcessGlobalVariables (which iterates
                        // _allDeclaredVariables by Type).
                        int slotIdx;
                        switch (actualType)
                        {
                            case "Player":
                                var p = _entityManager.CreatePlayer(varName);
                                slotIdx = _entityManager.GetPlayerIndex(p);
                                break;
                            case "Team":
                                slotIdx = _allDeclaredVariables.Count(kv => kv.Value.Type == "Team");
                                break;
                            default: // Timer
                                slotIdx = _allDeclaredVariables.Count(kv => kv.Value.Type == "Timer");
                                break;
                        }
                        var info = new VariableInfo(actualType, slotIdx, priorityValue);
                        _variableToIndexMap[varName] = info;
                        if (_scopeStack.Count > 0) _scopeStack.Peek()[varName] = info;
                        _allDeclaredVariables[varName] = info;
                        Debug.WriteLine($"Initialized {actualType} variable '{varName}' at slot {slotIdx} with networking priority {priorityValue}");
                    }
                    else
                    {
                        Debug.WriteLine($"Unhandled local variable type: {actualType}");
                    }
                }
            }
        }





        private
void ProcessAssignment(AssignmentExpressionSyntax assignment, ref int actionOffset)
        {
            // Special form: `<receiver>.grenades(<type>) op= <operand>`
            //   → action 74 Player_Grenades_SET(Player, Type1, Operator, Operand)
            // The decoder emits this assignment shape (LHS is an
            // InvocationExpression `.grenades(type)`), so we mirror it here.
            if (assignment.Left is InvocationExpressionSyntax grenInv
                && grenInv.Expression is MemberAccessExpressionSyntax grenMa
                && grenMa.Name.Identifier.Text.Equals("grenades", StringComparison.OrdinalIgnoreCase))
            {
                string opTokG = assignment.Kind() switch
                {
                    SyntaxKind.SimpleAssignmentExpression => "Set",
                    SyntaxKind.AddAssignmentExpression => "Add",
                    SyntaxKind.SubtractAssignmentExpression => "Subtract",
                    SyntaxKind.MultiplyAssignmentExpression => "Multiply",
                    SyntaxKind.DivideAssignmentExpression => "Divide",
                    SyntaxKind.ModuloAssignmentExpression => "Modulo",
                    _ => "Set",
                };
                if (_megaloActionsById.TryGetValue(74, out var grenAction))
                {
                    string receiver = grenMa.Expression.ToString();
                    string typeArg  = grenInv.ArgumentList.Arguments.Count > 0
                        ? grenInv.ArgumentList.Arguments[0].ToString()
                        : "frag";
                    string operandTok = assignment.Right.ToString();
                    var args = new List<string> { receiver, typeArg, opTokG, operandTok };
                    try
                    {
                        var pBits = EncodeMegaloActionParams(grenAction, args, varOut: null);
                        string actionIdBits = ConvertToBinary(grenAction.Id, 7);
                        string binaryAction = actionIdBits + string.Join("", pBits);
                        _actions.Add(new ActionObject(grenAction.Name, new List<string> { binaryAction }));
                        actionOffset++;
                        return;
                    }
                    catch (Exception ex)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"grenades-setter '{assignment}' encoding failed: {ex.Message}; emitting placeholder.",
                            1, 1));
                        EmitPlaceholderAction(grenAction.Name);
                        actionOffset++;
                        return;
                    }
                }
            }

            // Support: <lhs> = SomeActionThatHasOutParam(...)
            // Examples:
            //   weap = GetWeapon(current_player, GetPrimary);
            //   current_player.playerobject0 = current_player.biped.place_at_me(warthog, ...);
            //
            // For dotted LHS (member access) the OUT slot encoder (e.g.
            // ConvertObjectTypeRefToBinary) accepts the full dotted form and
            // routes to the right ObjectTypeRef variant — we just stringify
            // the LHS and pass it as varOut.
            if (assignment.Right is InvocationExpressionSyntax rightInv
                && (assignment.Left is IdentifierNameSyntax
                    || assignment.Left is MemberAccessExpressionSyntax))
            {
                string varName = assignment.Left.ToString();
                Debug.WriteLine($"Processing assignment-as-out: {varName} = {rightInv}");
                ProcessInvocation(rightInv, ref actionOffset, varName);
                return;
            }

            // Support: var = some_var_type_expression;
            // Example: newBip = current_player.biped;
            // Also handles member-access LHS (e.g. current_player.score = X)
            // and element-access LHS (e.g. hudwidgets[0] = X) by stringifying
            // the full LHS as the base token of the Set action. This compiles
            // to the Megalo "Set" action (formerly Megl.SET):
            //   Set(Base=<left>, Operand=<right>, Operator=Set)
            if (assignment.Left is IdentifierNameSyntax
                || assignment.Left is MemberAccessExpressionSyntax
                || assignment.Left is ElementAccessExpressionSyntax)
            {
                string baseTok = assignment.Left.ToString();
                string operandTok = assignment.Right.ToString();

                // Map assignment operators to SetterOperator tokens (must match enum names)
                string opTok = assignment.Kind() switch
                {
                    SyntaxKind.SimpleAssignmentExpression => "Set",
                    SyntaxKind.AddAssignmentExpression => "Add",
                    SyntaxKind.SubtractAssignmentExpression => "Subtract",
                    SyntaxKind.MultiplyAssignmentExpression => "Multiply",
                    SyntaxKind.DivideAssignmentExpression => "Divide",
                    SyntaxKind.ModuloAssignmentExpression => "Modulo",
                    SyntaxKind.AndAssignmentExpression => "BinaryAND",
                    SyntaxKind.OrAssignmentExpression => "BinaryOR",
                    SyntaxKind.ExclusiveOrAssignmentExpression => "BinaryXOR",
                    SyntaxKind.LeftShiftAssignmentExpression => "LeftShift",
                    SyntaxKind.RightShiftAssignmentExpression => "RightShift",
                    _ => "Set",
                };

                try
                {
                    // Setter routing: when the LHS is `target.<prop>` and
                    // <prop> is one of the canonical RVT setter names
                    // (score/money/shields/health/max_shields/max_health/
                    // grenades), the assignment compiles to a SPECIFIC
                    // megalo action — NOT generic Megl_SET (id=9). The
                    // decompiler's _rvtActionSetters table is the inverse
                    // mapping: id → property-name.
                    //
                    // Layout for these setters (from MegaloTables):
                    //   target (param 0 — the receiver typeref)
                    //   operator (param 1 — SetterOperator)
                    //   operand (param 2 — NumericTypeRef)
                    // Action 74 (grenades) has 4 params: player, type, op, operand.
                    string? setterProp = null;
                    if (assignment.Left is MemberAccessExpressionSyntax setterMa)
                    {
                        var name = setterMa.Name.Identifier.Text;
                        if (name.Equals("score", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("money", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("shields", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("health", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("max_shields", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("max_health", StringComparison.OrdinalIgnoreCase))
                            setterProp = name.ToLowerInvariant();
                    }

                    int setterActionId = setterProp switch
                    {
                        "score"       => 1,
                        "money"       => 38,
                        "shields"     => 64,
                        "health"      => 65,
                        "max_shields" => 67,
                        "max_health"  => 68,
                        _             => 0,
                    };

                    MegaloAction setAction;
                    if (setterActionId != 0 && _megaloActionsById.TryGetValue(setterActionId, out var byId))
                        setAction = byId;
                    else
                        setAction = ResolveActionByName("Set");

                    // For score/money/shields/health/max_shields/max_health,
                    // the receiver is the LHS's expression (`target` part of
                    // `target.score`), NOT the full LHS string. Strip the
                    // member name to get just the receiver.
                    string receiverTok = baseTok;
                    if (setterProp != null && assignment.Left is MemberAccessExpressionSyntax setterMa2)
                        receiverTok = setterMa2.Expression.ToString();

                    // Schema arg order differs between Megl_SET and the
                    // dedicated setter actions:
                    //   Megl_SET (id=9):           Base, Operand, Operator
                    //   Players_Score_SET (id=1):  Targets, Operator, Operand
                    //   Player_ReqMoney_SET (38):  Player,  Operator, Operand
                    //   Obj_Shields_SET (64) etc.: Object,  Operator, Operand
                    // Match the schema by ordering args per action.
                    var args = setterActionId == 0
                        ? new List<string> { receiverTok, operandTok, opTok }
                        : new List<string> { receiverTok, opTok, operandTok };

                    List<string> paramBits;
                    try
                    {
                        paramBits = EncodeMegaloActionParams(setAction, args, varOut: null);
                    }
                    catch (Exception encEx)
                    {
                        Debug.WriteLine($"Set() encoding failed for '{assignment}': {encEx.Message}; emitting placeholder.");
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"Setter assignment '{assignment}' (id={setAction.Id} {setAction.Name}) param encoding failed: {encEx.Message}; emitting placeholder None.",
                            1, 1));
                        EmitPlaceholderAction(setAction.Name);
                        actionOffset++;
                        return;
                    }
                    string actionIdBits = ConvertToBinary(setAction.Id, 7);
                    string binaryAction = actionIdBits + string.Join("", paramBits);

                    _actions.Add(new ActionObject(setAction.Name, new List<string> { binaryAction }));

                    if (CompilerActionTrace != null)
                    {
                        int payBits = paramBits.Sum(p => p?.Length ?? 0);
                        var pbDetail = string.Join(",",
                            setAction.Params.Select((p, idx) => $"{p.Name}:{p.TypeRef}({(idx < paramBits.Count ? paramBits[idx]?.Length ?? 0 : -1)}b)"));
                        CompilerActionTrace.AppendLine(
                            $"  enc act #{_actions.Count - 1} {setAction.Name}(id={setAction.Id}) hdr=7b pay={payBits}b total={7 + payBits}b" +
                            $" args=[{string.Join(", ", args)}] params=[{pbDetail}] [setter]");
                    }

                    actionOffset++;

                    Debug.WriteLine($"Compiled assignment via {setAction.Name} (id={setAction.Id}): {baseTok} {assignment.OperatorToken} {operandTok}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to compile assignment '{assignment}': {ex.Message}; emitting placeholder.");
                    EmitPlaceholderAction("Set");
                    actionOffset++;
                }
                return;
            }

            Debug.WriteLine($"Unhandled assignment form: {assignment} — emitting placeholder.");
            EmitPlaceholderAction("<assign>");
            actionOffset++;
        }





        private void ProcessInvocation(
            InvocationExpressionSyntax invocation,
            ref int actionOffset,
            string? varOut = "NoObject",
            int inlineActionOffset2 = -1)
        {

            if (TryEmitInlineFunctionCall(invocation, ref actionOffset))
                return;


            string? scriptName = GetInvokedName(invocation.Expression);
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                Debug.WriteLine("Unsupported invocation form.");
                EmitPlaceholderAction("<no-name>");
                actionOffset++;
                return;
            }

            // Member-access calls (e.g. `current_player.set_loadout_palette(2)`)
            // come in as "receiver.method". Pull the trailing method name as
            // a candidate action and reroute the receiver as the first arg.
            string? memberMethod = null;
            string? memberReceiver = null;
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                memberMethod = ma.Name.Identifier.Text;
                memberReceiver = ma.Expression.ToString();
            }

            var args = invocation.ArgumentList.Arguments.Select(a => TrimArg(a.Expression)).ToList();

            // Member-access calls of the form `receiver.method(args...)`
            // need the receiver injected into the positional args list
            // because most action schemas have the receiver as their first
            // (or near-first) param. The schema walker just consumes args
            // in order, so we need the receiver AT THE RIGHT INDEX.
            //
            // Default position: args[0] (receiver is the action's "self"
            // target — applies to set_hidden, set_waypoint_text,
            // set_garbage_collection_disabled, attach_to, detach, delete,
            // is_of_type, …).
            //
            // Special case: place_at_me (Megl_Create, id=2) renders as
            //   OUT = PlaceAt.place_at_me(Type1, Label, …)
            // so receiver goes to args[1] (after Type1).
            if (!string.IsNullOrEmpty(memberReceiver))
            {
                bool isPlaceAtMe =
                    string.Equals(memberMethod, "place_at_me", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(scriptName, "place_at_me", StringComparison.OrdinalIgnoreCase);
                int insertAt = isPlaceAtMe ? 1 : 0;
                if (insertAt > args.Count) insertAt = args.Count;
                args.Insert(insertAt, memberReceiver);
            }

            MegaloAction action;
            try
            {
                action = ResolveActionByName(scriptName);
            }
            catch
            {
                // Fall through to member-method resolution if applicable.
                action = default;
                bool resolved = false;
                if (!string.IsNullOrEmpty(memberMethod))
                {
                    try
                    {
                        action = ResolveActionByName(memberMethod);
                        resolved = true;
                        // Receiver was already injected by the
                        // member-access pre-pass above — don't prepend
                        // again here, it would double-up.
                    }
                    catch
                    {
                        resolved = false;
                    }
                }
                if (!resolved)
                {
                    string msg = $"Action '{scriptName}' not found — emitting placeholder None action. Compiled section will be short and re-decompile will desync.";
                    Debug.WriteLine(msg);
                    _encoderDiagnostics.Add(new CompilerDiagnostic(
                        CompilerDiagnosticSeverity.Warning, msg, 1, 1));
                    EmitPlaceholderAction(scriptName);
                    actionOffset++;
                    return;
                }
            }

            // Encode action id + params based on MegaloTables schema
            List<string> paramBits;
            try
            {
                paramBits = EncodeMegaloActionParams(action, args, varOut);
            }
            catch (Exception ex)
            {
                string msg = $"Action '{action.Name}' (id={action.Id}) param encoding failed: {ex.Message}; emitting placeholder None.";
                Debug.WriteLine(msg);
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning, msg, 1, 1));
                EmitPlaceholderAction(action.Name);
                actionOffset++;
                return;
            }
            string actionNumberBinary = ConvertToBinary(action.Id, 7);
            string binaryAction = actionNumberBinary + string.Join("", paramBits);

            _actions.Add(new ActionObject(action.Name, new List<string> { binaryAction }));
            Debug.WriteLine($"Added action: {scriptName}({binaryAction})");

            if (CompilerActionTrace != null)
            {
                int payBits = paramBits.Sum(p => p?.Length ?? 0);
                var pbDetail = string.Join(",",
                    action.Params.Select((p, idx) => $"{p.Name}:{p.TypeRef}({(idx < paramBits.Count ? paramBits[idx]?.Length ?? 0 : -1)}b)"));
                CompilerActionTrace.AppendLine(
                    $"  enc act #{_actions.Count - 1} {action.Name}(id={action.Id}) hdr=7b pay={payBits}b total={7 + payBits}b" +
                    $" args=[{string.Join(", ", args)}] params=[{pbDetail}]");
            }

            actionOffset++;
        }

        // Per-action encoder trace (collects per-call widths). Set by the
        // CLI harness, ignored otherwise.
        public static System.Text.StringBuilder? CompilerActionTrace { get; set; }

        // Emit a 7-bit action header for the "None" action (id=0). Used as
        // a placeholder when an invocation can't be resolved or its params
        // can't be encoded — we still need to bump the global action count
        // so trigger and condition offsets stay aligned with the source.
        private void EmitPlaceholderAction(string sourceName)
        {
            string idBits = ConvertToBinary(0, 7); // action id 0 == "None"
            _actions.Add(new ActionObject($"None /* {sourceName} */", new List<string> { idBits }));
            if (CompilerActionTrace != null)
                CompilerActionTrace.AppendLine($"  enc act #{_actions.Count - 1} None /* {sourceName} */ hdr=7b pay=0b total=7b (PLACEHOLDER)");
        }

        private List<string> EncodeMegaloActionParams(MegaloAction action, List<string> args, string? varOut)
        {
            var bits = new List<string>();
            int i = 0;

            foreach (var p in action.Params)
            {
                string? token;

                if (p.Name.StartsWith("OUT", StringComparison.OrdinalIgnoreCase))
                {
                    token = ResolveOutToken(varOut, p.TypeRef);
                }
                else if (IsVector3Type(p.TypeRef))
                {
                    token = ConsumeVector3Token(args, ref i);
                }
                else if (p.TypeRef.StartsWith("Enumref:Tokens", StringComparison.OrdinalIgnoreCase))
                {
                    // Tokens{1,2,3} composite: the new flat decompile syntax
                    // splices the (string, payload, payload, …) directly
                    // into the parent action's arg list, so we consume
                    // ALL remaining args here and rewrap them as the
                    // legacy `tokens(...)` form that EncodeTokensParam
                    // expects. Backward-compat: if the caller still wrote
                    // `tokens(...)` explicitly, pass it through unchanged.
                    if (i < args.Count
                        && args[i].TrimStart().StartsWith("tokens(", StringComparison.OrdinalIgnoreCase))
                    {
                        token = args[i++];
                    }
                    else
                    {
                        var rest = new List<string>(args.Count - i);
                        while (i < args.Count) rest.Add(args[i++]);
                        token = "tokens(" + string.Join(", ", rest) + ")";
                    }
                }
                else
                {
                    token = i < args.Count ? args[i++] : null;
                }

                token = RemapVariableTokenForTypeRef(token, p.TypeRef);

                bits.Add(EncodeParamByTypeRef(p.TypeRef, token));
            }

            return bits;
        }

        private List<string> EncodeMegaloConditionParams(MegaloCondition cond, List<string> args)
        {
            var bits = new List<string>();
            int i = 0;

            foreach (var p in cond.Params)
            {
                string? token;

                if (IsVector3Type(p.TypeRef))
                    token = ConsumeVector3Token(args, ref i);
                else
                    token = i < args.Count ? args[i++] : null;

                token = RemapVariableTokenForTypeRef(token, p.TypeRef);
                bits.Add(EncodeParamByTypeRef(p.TypeRef, token));
            }

            return bits;
        }

        private static bool IsVector3Type(string typeRef)
            => typeRef.Equals("Enumref:Vector3", StringComparison.OrdinalIgnoreCase);

        private static string? ConsumeVector3Token(List<string> args, ref int i)
        {
            // Accept:
            //   (x, y, z) as 3 separate args
            //   "x,y,z" as one arg
            if (i < args.Count)
            {
                string one = args[i];
                if (one.Contains(","))
                {
                    i++;
                    return one;
                }
            }

            if (i + 2 < args.Count
                && int.TryParse(args[i], out _)
                && int.TryParse(args[i + 1], out _)
                && int.TryParse(args[i + 2], out _))
            {
                string tok = $"{args[i]},{args[i + 1]},{args[i + 2]}";
                i += 3;
                return tok;
            }

            return null;
        }

        private string? ResolveOutToken(string? varOut, string typeRef)
        {
            if (string.IsNullOrWhiteSpace(varOut) || varOut.Equals("NoObject", StringComparison.OrdinalIgnoreCase))
                return null;

            // If user explicitly passed a constant, allow it
            if (!_variableToIndexMap.TryGetValue(varOut!, out var info))
                return varOut;

            // Map declared variables to the appropriate "GlobalX" token the encoders understand.
            // Note: indices here are *your* allocation indices, not the engine's "NoX" slot.
            if (typeRef.Equals("Enumref:ObjectTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Object", StringComparison.OrdinalIgnoreCase))
                return $"GlobalObject{info.Index}";
            if (typeRef.Equals("Enumref:NumericTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Number", StringComparison.OrdinalIgnoreCase))
                return $"GlobalNumber{info.Index}";
            if (typeRef.Equals("Enumref:PlayerTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Player", StringComparison.OrdinalIgnoreCase))
                return $"GlobalPlayer{info.Index}";
            if (typeRef.Equals("Enumref:TeamTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Team", StringComparison.OrdinalIgnoreCase))
                return $"GlobalTeam{info.Index}";
            if (typeRef.Equals("Enumref:TimerTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Timer", StringComparison.OrdinalIgnoreCase))
                return $"GlobalTimer{info.Index}";

            // VarType OUT can be anything: pass the original name and let EncodeVarType resolve by declared type
            if (typeRef.Equals("Enumref:VarType", StringComparison.OrdinalIgnoreCase))
                return varOut;

            return varOut;
        }

        private string? RemapVariableTokenForTypeRef(string? token, string typeRef)
        {
            if (string.IsNullOrWhiteSpace(token))
                return token;

            // Remap "weap" -> GlobalObjectN if action expects ObjectTypeRef, etc.
            if (_variableToIndexMap.TryGetValue(token, out var info))
            {
                if (typeRef.Equals("Enumref:ObjectTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Object", StringComparison.OrdinalIgnoreCase))
                    return $"GlobalObject{info.Index}";

                if (typeRef.Equals("Enumref:NumericTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Number", StringComparison.OrdinalIgnoreCase))
                    return $"GlobalNumber{info.Index}";

                if (typeRef.Equals("Enumref:PlayerTypeRef", StringComparison.OrdinalIgnoreCase) && info.Type.Equals("Player", StringComparison.OrdinalIgnoreCase))
                    return $"GlobalPlayer{info.Index}";

                if (typeRef.Equals("Enumref:VarType", StringComparison.OrdinalIgnoreCase))
                    return token; // EncodeVarType resolves using _variableToIndexMap

                // Fallthrough: return original token for other Enumrefs
            }

            // Normalize common spellings
            if (token.Equals("current_player", StringComparison.OrdinalIgnoreCase))
                return "current_player";
            if (token.Equals("current_object", StringComparison.OrdinalIgnoreCase))
                return "current_object";

            return token;
        }

        private string EncodeParamByTypeRef(string typeRef, string? token)
        {
            // IncidentPlayers — 2-bit variant + sub-field payload:
            //   00 Team       : TeamTypeRef sub-field
            //   01 Players    : PlayerTypeRef sub-field
            //   10 AllPlayers : no sub-field
            //   11 Unlabelled : no sub-field
            // Decoder ScriptDecompiler.cs:969-983 reads these variants;
            // we mirror the encode side here.
            if (typeRef.Equals("Enumref:IncidentPlayers", StringComparison.OrdinalIgnoreCase))
            {
                string ip = (token ?? string.Empty).Trim();
                string ipNorm = ip.Replace("__", "_").Replace(" ", "_");
                // No-payload variant names.
                if (ipNorm.Equals("all_players", StringComparison.OrdinalIgnoreCase)
                    || ipNorm.Equals("AllPlayers", StringComparison.OrdinalIgnoreCase)
                    || ipNorm.Equals("everyone", StringComparison.OrdinalIgnoreCase))
                    return "10";
                if (ipNorm.Equals("Unlabelled", StringComparison.OrdinalIgnoreCase)
                    || ipNorm.Equals("no__players", StringComparison.OrdinalIgnoreCase)
                    || ipNorm.Equals("no_players", StringComparison.OrdinalIgnoreCase))
                    return "11";
                // Player-shaped operand → Players(01) + PlayerTypeRef.
                if (ip.Equals("current_player", StringComparison.OrdinalIgnoreCase)
                    || ip.Equals("no_player", StringComparison.OrdinalIgnoreCase)
                    || ip.Equals("NoPlayer", StringComparison.OrdinalIgnoreCase)
                    || ip.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase)
                    || ip.StartsWith("GlobalPlayer", StringComparison.OrdinalIgnoreCase))
                {
                    return "01" + ConvertPlayerTypeRefToBinary(ip, 0);
                }
                // Team-shaped operand → Team(00) + TeamTypeRef.
                if (ip.Equals("current_team", StringComparison.OrdinalIgnoreCase)
                    || ip.Equals("no_team", StringComparison.OrdinalIgnoreCase)
                    || ip.Equals("NoTeam", StringComparison.OrdinalIgnoreCase)
                    || ip.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase)
                    || ip.StartsWith("GlobalTeam", StringComparison.OrdinalIgnoreCase))
                {
                    return "00" + ConvertTeamTypeRefToBinary(ip, 0);
                }
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"IncidentPlayers token '{ip}' didn't match any variant — emitting AllPlayers (10).",
                    1, 1));
                return "10";
            }

            // SetterOperator — 4-bit enum per the canonical MegaloSchema.
            // The decompiler emits these as op tokens like Set/Add/Subtract.
            // ID values come from MegaloSchema's SetterOperator EnumDef:
            //   0=Add, 1=Subtract, 2=Multiply, 3=Divide, 4=Set,
            //   5=Modulo, 6=AND, 7=OR, 8=XOR, 9=NOT, 10=LeftShift,
            //   11=RightShift, 12=Absolute.
            if (typeRef.Equals("Enumref:SetterOperator", StringComparison.OrdinalIgnoreCase))
            {
                string op = (token ?? "Set").Trim();
                int opVal = op switch
                {
                    "Add" or "+="           => 0,
                    "Subtract" or "-="      => 1,
                    "Multiply" or "*="      => 2,
                    "Divide" or "/="        => 3,
                    "Set" or "="            => 4,
                    "Modulo" or "%="        => 5,
                    "BinaryAND" or "&="     => 6,
                    "BinaryOR"  or "|="     => 7,
                    "BinaryXOR" or "^="     => 8,
                    "BinaryNOT" or "~="     => 9,
                    "LeftShift" or "<<="    => 10,
                    "RightShift" or ">>="   => 11,
                    "Absolute" or "abs"     => 12,
                    _                        => 4,
                };
                return ConvertToBinary(opVal, 4);
            }

            // GetterOperator — 3-bit enum (Megl.If comparator). Decompiler
            // emits operator tokens "<", ">", "==", "<=", ">=", "!=".
            if (typeRef.Equals("Enumref:GetterOperator", StringComparison.OrdinalIgnoreCase))
            {
                string op = (token ?? "==").Trim();
                int opVal = op switch
                {
                    "<"  or "LessThan"          => 0,
                    ">"  or "GreaterThan"       => 1,
                    "==" or "Equals"            => 2,
                    "<=" or "LessThanEquals"    => 3,
                    ">=" or "GreaterThanEquals" => 4,
                    "!=" or "NotEquals"         => 5,
                    _                            => 2,
                };
                return ConvertToBinary(opVal, 3);
            }

            // Tokens1 / Tokens2 / Tokens3 — Container of (String index + StringVarsN tag + N×StringToken).
            // Decoder emits `tokens(str[N], ...)`. Only handle simple single-arg
            // `tokens(str[N])` (svTag=0) for now — that covers Reach
            // set_waypoint_text round-trip. Multi-token (player/team/object/
            // number/timer references) is encoded with svTag=N + N×3-bit
            // StringToken tags + variable-width payloads.
            if (typeRef.StartsWith("Enumref:Tokens", StringComparison.OrdinalIgnoreCase))
            {
                bool isTok1 = typeRef.Equals("Enumref:Tokens1", StringComparison.OrdinalIgnoreCase);
                int stringBits = 7;  // Reach; H2A is 8 — current compiler is Reach-only.
                int varsBits   = isTok1 ? 1 : 2;
                return EncodeTokensParam(token, stringBits, varsBits);
            }

            // SpawnObjectFlags is a Container of 3 individual Bool bits
            // (NeverGarbageCollect, SuppressEffect, AbsoluteOrientation).
            // Decoder emits `0` (none set) or pipe-delimited names.
            if (typeRef.Equals("Enumref:SpawnObjectFlags", StringComparison.OrdinalIgnoreCase))
            {
                int n = 0, s = 0, a = 0;
                var t = (token ?? string.Empty).Trim();
                // Accept both new lowercase snake_case forms emitted by
                // the decompiler AND legacy PascalCase forms for back-compat.
                if (t.IndexOf("never_garbage_collect", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("NeverGarbageCollect", StringComparison.OrdinalIgnoreCase) >= 0) n = 1;
                if (t.IndexOf("suppress_effect", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("SuppressEffect", StringComparison.OrdinalIgnoreCase) >= 0) s = 1;
                if (t.IndexOf("absolute_orientation", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("AbsoluteOrientation", StringComparison.OrdinalIgnoreCase) >= 0) a = 1;
                return $"{n}{s}{a}";
            }

            // Handle known composite encodings first
            if (typeRef.Equals("Enumref:Bool", StringComparison.OrdinalIgnoreCase))
            {
                if (token != null && TryParseBoolLoose(token, out bool b))
                    return ConvertToBinary(b ? 1 : 0, 1);
                return ConvertToBinary(0, 1);
            }

            if (typeRef.Equals("Enumref:Vector3", StringComparison.OrdinalIgnoreCase))
            {
                if (token == null) return ConvertVector3ToBinary(0, 0, 0);

                // "x,y,z"
                var parts = token.Split(',');
                if (parts.Length == 3
                    && int.TryParse(parts[0].Trim(), out int x)
                    && int.TryParse(parts[1].Trim(), out int y)
                    && int.TryParse(parts[2].Trim(), out int z))
                {
                    return ConvertVector3ToBinary(x, y, z);
                }
                return ConvertVector3ToBinary(0, 0, 0);
            }

            if (typeRef.Equals("Enumref:ObjectTypeRef", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertObjectTypeRefToBinary(token ?? "NoObject", 0, 1);
            }

            if (typeRef.Equals("Enumref:PlayerTypeRef", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertPlayerTypeRefToBinary(token ?? "current_player", 0);
            }

            if (typeRef.Equals("Enumref:TeamTypeRef", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertTeamTypeRefToBinary(token ?? "NoTeam", 0);
            }

            if (typeRef.Equals("Enumref:TimerTypeRef", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertTimerTypeRefToBinary(token ?? "NoTimer", 0);
            }

            if (typeRef.Equals("Enumref:NumericTypeRef", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertNumericTypeRefToBinary(token ?? "0", 0);
            }

            if (typeRef.Equals("Enumref:VarType", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertVarTypeToBinary(token ?? "0", 0);
            }

            if (typeRef.Equals("Enumref:ObjectType", StringComparison.OrdinalIgnoreCase))
            {
                // 12-bit ObjectType value. The decompiler renders these via
                // the RvtObjectTypes table (id -> name), so the encoder must
                // do the inverse lookup or otherwise we'd silently emit 0
                // (=spartan) for everything that isn't in the legacy
                // ObjectType enum.
                if (token == null) return ConvertToBinary(0, 12);
                if (int.TryParse(token, out int litN))
                    return ConvertToBinary(litN, 12);
                foreach (var kv in RvtObjectTypes.Names)
                {
                    if (string.Equals(kv.Value, token, StringComparison.OrdinalIgnoreCase))
                        return ConvertToBinary(kv.Key, 12);
                }
                if (Enum.TryParse(typeof(ObjectType), token, true, out var objEnum) && objEnum != null)
                    return ConvertToBinary((int)objEnum, 12);
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"ObjectType '{token}' not found in RvtObjectTypes table — emitting 0 (spartan).",
                    1, 1));
                return ConvertToBinary(0, 12);
            }

            if (typeRef.Equals("Enumref:LabelRef", StringComparison.OrdinalIgnoreCase))
            {
                // Keep your existing label encoding (1-bit "no label" flag + optional 4-bit label id)
                return EncodeLabelRef(token);
            }

            // WidgetRef: 1-bit tag + variant.
            //   variant 0 (Widget): 1 + 2-bit hudwidgets slot
            //   variant 1 (NoWidget): 1 bit only
            // Decompiler emits `hudwidgets[N]` for slot references; bare
            // identifiers fall through to NoWidget.
            if (typeRef.Equals("Enumref:WidgetRef", StringComparison.OrdinalIgnoreCase))
            {
                string t = (token ?? "").Trim();
                var hwM = System.Text.RegularExpressions.Regex.Match(t,
                    @"^hudwidgets\s*\[\s*(\d+)\s*\]$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (hwM.Success && int.TryParse(hwM.Groups[1].Value, out int hwIdx))
                    return "0" + Convert.ToString(hwIdx & 0x3, 2).PadLeft(2, '0');
                if (t.Equals("NoWidget", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(t))
                    return "1";
                if (int.TryParse(t, out int hwLit))
                    return "0" + Convert.ToString(hwLit & 0x3, 2).PadLeft(2, '0');
                return "1";
            }

            // Fallback: encode as plain enum (bit-width inferred from max value)
            if (typeRef.StartsWith("Enumref:", StringComparison.OrdinalIgnoreCase))
            {
                string enumName = typeRef.Substring("Enumref:".Length);
                return EncodeEnumByName(enumName, token);
            }

            throw new InvalidOperationException($"Unsupported param typeRef: '{typeRef}'.");
        }

        // Reverse of ScriptDecompiler.EscapeStringLiteral — unescape a
        // C# double-quoted literal's body (the contents between the quotes).
        private static string UnescapeStringLiteral(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    switch (n)
                    {
                        case '\\': sb.Append('\\'); break;
                        case '"':  sb.Append('"');  break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 < s.Length && int.TryParse(s.Substring(i + 1, 4),
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out int u))
                            {
                                sb.Append((char)u);
                                i += 4;
                            }
                            else sb.Append(n);
                            break;
                        default: sb.Append(n); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Encode `tokens(str[N] [, payload, ...])` as the megalo Tokens
        // container layout: [stringIndex (7 or 8 bits)][svTag (varsBits)]
        // [N×StringToken]. The decoder emits this token form at
        // ScriptDecompiler.cs:2744. We support the no-payload case fully and
        // best-effort emit svTag=0 + zero tokens when payloads are present
        // but unparseable — in that case we surface a diagnostic so the
        // miss is visible.
        private string EncodeTokensParam(string? token, int stringBits, int varsBits)
        {
            string raw = (token ?? string.Empty).Trim();
            // Strip the helper prefix `tokens(` and trailing `)`
            if (raw.StartsWith("tokens(", StringComparison.OrdinalIgnoreCase) && raw.EndsWith(")"))
                raw = raw.Substring("tokens(".Length, raw.Length - "tokens(".Length - 1);

            // Split top-level by commas (no nested parens at this level for str[N]).
            var parts = SplitTopLevelCommas(raw);
            int stringIndex = 0;
            if (parts.Count > 0)
            {
                var first = parts[0].Trim();
                // Accept all three forms:
                //   str[N]              — index, legacy decompile output
                //   N                   — bare numeric index
                //   "actual content"    — resolved string literal
                //                         (reverse-lookup via StringTable)
                var m = System.Text.RegularExpressions.Regex.Match(first, @"^str\s*\[\s*(\d+)\s*\]$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    int.TryParse(m.Groups[1].Value, out stringIndex);
                }
                else if (first.Length >= 2 && first[0] == '"' && first[first.Length - 1] == '"')
                {
                    string content = UnescapeStringLiteral(first.Substring(1, first.Length - 2));
                    stringIndex = -1;
                    for (int i = 0; i < StringTable.Count; i++)
                    {
                        if (string.Equals(StringTable[i], content, StringComparison.Ordinal))
                        {
                            stringIndex = i;
                            break;
                        }
                    }
                    if (stringIndex < 0)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"tokens(\"{content}\"): string not found in StringTable; encoding as index 0.",
                            0, 0));
                        stringIndex = 0;
                    }
                }
                else if (!int.TryParse(first, out stringIndex))
                {
                    stringIndex = 0;
                }
            }

            string idxBits = ConvertToBinary(stringIndex, stringBits);
            int payloadCount = Math.Max(0, parts.Count - 1);
            // svTag picks how many StringToken bodies follow:
            //   Tokens1: 0=None, 1=Vars1 (1 token)
            //   Tokens2: 0=None, 1=Vars1, 2=Vars2 (2 tokens), 3=Unlabelled (0)
            //   Tokens3: 0=None, 1=Vars1, 2=Vars2, 3=Vars3 (3 tokens)
            int svTag = payloadCount switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => 3,
            };
            string tagBits = ConvertToBinary(svTag, varsBits);
            // Encode each StringToken body. Each is a 3-bit tag + variant
            // payload, mirroring ScriptDecompiler.ReadStringToken at L2956:
            //   0 → none (no payload)
            //   1 → player(<PlayerTypeRef>)
            //   2 → team(<TeamTypeRef>)
            //   3 → object(<ObjectTypeRef>)
            //   4 → number(<NumericTypeRef>)
            //   5 → timer(<TimerTypeRef>)
            //   6 → timer(<TimerTypeRef>)  (TimerVarAswell)
            var sb = new System.Text.StringBuilder();
            sb.Append(idxBits).Append(tagBits);
            for (int p = 0; p < payloadCount; p++)
            {
                string st = parts[p + 1].Trim();
                sb.Append(EncodeStringTokenBody(st));
            }
            return sb.ToString();
        }

        // Infer the StringToken payload kind from a bare token expression.
        // Uses identifier shape: built-in receivers, dotted-receiver
        // chains, short slot prefixes (`tmr<N>` → timer, etc.). Defaults
        // to "number" when nothing else matches — most user variables in
        // tokens(...) calls are score / counter values.
        private static string InferStringTokenKind(string token)
        {
            string t = token.Trim();
            // Dotted-receiver: kind is determined by the last segment's
            // short prefix (.tmr → timer, .plr → player, .obj → object,
            // .num → number, .tm → team, .player.X → player, …).
            int lastDot = t.LastIndexOf('.');
            if (lastDot >= 0)
            {
                string tail = t.Substring(lastDot + 1);
                if (System.Text.RegularExpressions.Regex.IsMatch(tail, @"^tmr\d*$"))  return "timer";
                if (System.Text.RegularExpressions.Regex.IsMatch(tail, @"^plr\d*$"))  return "player";
                if (System.Text.RegularExpressions.Regex.IsMatch(tail, @"^obj\d*$"))  return "object";
                if (System.Text.RegularExpressions.Regex.IsMatch(tail, @"^tm\d*$"))   return "team";
                if (System.Text.RegularExpressions.Regex.IsMatch(tail, @"^num\d*$"))  return "number";
                // Receiver-disambiguated long forms (player.num1 etc.) —
                // rewrite ran first, but on raw user input we may still
                // see them.
            }
            // Built-in references.
            if (System.Text.RegularExpressions.Regex.IsMatch(t,
                @"^(current_player|no_player|hud_player|hud_target_player|object_killer|globalplayer\d+|temp_player_\d+|player\d+)$"))
                return "player";
            if (System.Text.RegularExpressions.Regex.IsMatch(t,
                @"^(current_team|no_team|neutral_team|globalteam\d+|temp_team_\d+|team\d+)$"))
                return "team";
            if (System.Text.RegularExpressions.Regex.IsMatch(t,
                @"^(current_object|no_object|target_object|killed_object|killer_object|globalobject\d+|temp_obj_\d+)$"))
                return "object";
            // Short slot bare form.
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^tmr\d+$"))  return "timer";
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^plr\d+$"))  return "player";
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^obj\d+$"))  return "object";
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^tm\d+$"))   return "team";
            return "number";
        }

        // Encode a single StringToken — 3-bit tag + variant payload. Mirrors
        // the decompiler's ReadStringToken render forms. Two arg shapes
        // are accepted:
        //   • Wrapped legacy form: `player(...)`, `team(...)`, `object(...)`,
        //     `number(...)`, `timer(...)`, `none`.
        //   • Bare flat form: `current_player`, `score_to_win`, `tmr1`, …
        //     The kind is inferred from the identifier shape (built-in
        //     receiver names / dotted-receiver chains / short prefixes).
        private string EncodeStringTokenBody(string token)
        {
            string Tag3(int t) => Convert.ToString(t, 2).PadLeft(3, '0');
            if (string.IsNullOrEmpty(token) || token.Equals("none", StringComparison.OrdinalIgnoreCase))
                return Tag3(0);

            string head, inner;
            int paren = token.IndexOf('(');
            if (paren < 0 || !token.EndsWith(")"))
            {
                // Bare payload — infer the kind from the expression.
                head  = InferStringTokenKind(token);
                inner = token;
            }
            else
            {
                head  = token.Substring(0, paren).Trim();
                inner = token.Substring(paren + 1, token.Length - paren - 2).Trim();
            }

            try
            {
                switch (head.ToLowerInvariant())
                {
                    case "player":  return Tag3(1) + ConvertPlayerTypeRefToBinary(inner, 0);
                    case "team":    return Tag3(2) + ConvertTeamTypeRefToBinary(inner, 0);
                    case "object":  return Tag3(3) + ConvertObjectTypeRefToBinary(inner, 0, 1);
                    case "number":  return Tag3(4) + ConvertNumericTypeRefToBinary(inner, 0);
                    case "timer":   return Tag3(5) + ConvertTimerTypeRefToBinary(inner, 0);
                }
            }
            catch (Exception ex)
            {
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"StringToken '{token}' payload encoding failed: {ex.Message}; emitting tag 0 (none).",
                    1, 1));
                return Tag3(0);
            }

            _encoderDiagnostics.Add(new CompilerDiagnostic(
                CompilerDiagnosticSeverity.Warning,
                $"StringToken kind '{head}' not recognized; emitting tag 0 (none).",
                1, 1));
            return Tag3(0);
        }

        private static List<string> SplitTopLevelCommas(string s)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(s)) return result;
            int depth = 0;
            int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(s.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(s.Substring(start));
            return result;
        }

        private string EncodeEnumByName(string enumName, string? token)
        {
            // Special-case: TriggerRef is a Var (bits=9 in your XML) but is referenced as Enumref:TriggerRef in action tables.
            // Allow both TriggerN identifiers (e.g. Trigger3) and raw integers (e.g. 3).
            if (enumName.Equals("TriggerRef", StringComparison.OrdinalIgnoreCase))
            {
                var t = (token ?? "").Trim();
                if (t.StartsWith("Trigger", StringComparison.OrdinalIgnoreCase))
                    t = t.Substring("Trigger".Length);

                if (!int.TryParse(t, out var trigIndex) || trigIndex < 0)
                    throw new Exception($"Invalid TriggerRef literal: '{token}'");

                return ConvertToBinary(trigIndex, 9);
            }

            // Try to locate the enum type by short name across loaded assemblies.
            Type? enumType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.IsEnum && t.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase));

            if (enumType == null)
            {
                // No C# enum type with this name. Fall back to the schema-
                // driven tables the decompiler uses, in priority order:
                //   1) ScriptDecompiler.ExternalEnumBits — flat-int widths
                //      for `type="External"` / `type="Int"` refs (Sound,
                //      Incident, NameIndex, ObjectType, …). Encode the
                //      token as a raw N-bit unsigned integer.
                //   2) MegaloSchema.Enums — variant tables. Match the
                //      token against variant Names; if matched, emit the
                //      variant tag bits. (Sub-fields of variants aren't
                //      encoded here yet — those need a per-variant
                //      encoder. For now we only round-trip variants with
                //      no sub-fields, which covers AllPlayers, common
                //      Bool-like enums, etc.)
                if (ScriptDecompiler.ExternalEnumBits.TryGetValue(enumName, out int extBits))
                {
                    if (string.IsNullOrWhiteSpace(token))
                        return ConvertToBinary(0, extBits);
                    if (int.TryParse(token, out int extInt))
                        return ConvertToBinary(extInt, extBits);
                    return ConvertToBinary(0, extBits);
                }
                if (MegaloSchema.Enums.TryGetValue(enumName, out var schemaEnum))
                {
                    if (string.IsNullOrWhiteSpace(token))
                        return ConvertToBinary(0, schemaEnum.Bits);
                    // Reverse the decoder's ToSnakeCase rate-token rewrite:
                    //   "-100%" → "rate_minus_100", "25%" → "rate_25".
                    // We try to map a `rate_…` token back to the percent-form
                    // variant name before falling through to plain match.
                    string? ratePercent = null;
                    if (token.StartsWith("rate_", StringComparison.OrdinalIgnoreCase))
                    {
                        string body = token.Substring("rate_".Length);
                        if (body.StartsWith("minus_", StringComparison.OrdinalIgnoreCase))
                            ratePercent = "-" + body.Substring("minus_".Length) + "%";
                        else
                            ratePercent = body + "%";
                    }
                    // Try variant name match (e.g. "AllPlayers", "all_players").
                    string norm = token.Trim().Replace("_", "");
                    foreach (var v in schemaEnum.Variants)
                    {
                        if (string.Equals(v.Name, token, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(v.Name?.Replace("_",""), norm, StringComparison.OrdinalIgnoreCase)
                            || (ratePercent != null && string.Equals(v.Name, ratePercent, StringComparison.OrdinalIgnoreCase)))
                        {
                            return ConvertToBinary(v.Id, schemaEnum.Bits);
                        }
                    }
                    if (int.TryParse(token, out int variantInt))
                        return ConvertToBinary(variantInt, schemaEnum.Bits);
                    return ConvertToBinary(0, schemaEnum.Bits);
                }

                throw new InvalidOperationException($"Enum type '{enumName}' not found. Add it or add a custom encoder for '{enumName}'.");
            }

            int bits = BitsNeededForEnum(enumType);
            if (bits <= 0) bits = 1;

            if (string.IsNullOrWhiteSpace(token))
                return ConvertToBinary(0, bits);

            try
            {
                object value;
                if (int.TryParse(token, out int asInt))
                {
                    value = Enum.ToObject(enumType, asInt);
                }
                else
                {
                    // Allow tokens like "sound_emitter_alarm_2" or "needle_rifle"
                    value = Enum.Parse(enumType, token, ignoreCase: true);
                }

                int intVal = Convert.ToInt32(value);
                return ConvertToBinary(intVal, bits);
            }
            catch
            {
                // Unknown token -> default 0 (safer than throwing for non-critical enums)
                return ConvertToBinary(0, bits);
            }
        }

        private static int BitsNeededForEnum(Type enumType)
        {
            try
            {
                var values = Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt64).ToArray();
                long max = values.Length == 0 ? 0 : values.Max();
                if (max <= 0) return 1;
                int bits = 0;
                while (max > 0) { bits++; max >>= 1; }
                return Math.Max(bits, 1);
            }
            catch
            {
                return 1;
            }
        }

        private static bool ParseGetPrimaryToken(string token, bool defaultValue)
        {
            token = (token ?? string.Empty).Trim();

            // allow plain bools
            if (TryParseBoolLoose(token, out bool b))
                return b;

            // allow Megalo-ish tokens
            if (token.Equals("GetPrimary", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Primary", StringComparison.OrdinalIgnoreCase))
                return true;

            if (token.Equals("GetSecondary", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Secondary", StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }

        private List<string> EncodeAction_PlayerWeaponGet(List<string> args, string? varOut)
        {
            // Player.Weapon_GET(Player, GetPrimary, OUTWeapon)

            string player = args.Count > 0 ? args[0] : "current_player";

            bool getPrimary = true;
            if (args.Count > 1)
                getPrimary = ParseGetPrimaryToken(args[1], defaultValue: true);

            // OUT param inferred from assignment/declaration name (varOut)
            string outObj = "NoObject";
            if (!string.IsNullOrWhiteSpace(varOut))
                outObj = RemapVariableToObjectRef(varOut);

            // Encode params
            return new List<string>
    {
        ConvertPlayerTypeRefToBinary(player, 7),      // 2+5 bits (as your converter builds)
        ConvertToBinary(getPrimary, 1),               // Bool
        ConvertObjectTypeRefToBinary(outObj, 0, 1)    // ObjectTypeRef
    };
        }



        // ============================================================
        // Action encoders (canonical Megalo layout for supported subset)
        // ============================================================

        // Megl.Create(Type1, OUTObject, PlaceAt, Label, SpawnFlags, LocationOffset, Variant)
        private List<string> EncodeAction_Create(List<string> args, string? varOut)
        {
            // Args (script alias): CreateObject(type, placeAt [, label])
            string typeStr = args.Count > 0 ? args[0] : "none";
            string placeAt = args.Count > 1 ? args[1] : "current_object";
            string label = args.Count > 2 ? args[2] : "none";

            // Resolve OUTObject from varOut (same behavior as old compiler's var_out handling)
            string outObj = "NoObject";
            int outPriority = 1;
            if (!string.IsNullOrWhiteSpace(varOut) && _variableToIndexMap.TryGetValue(varOut, out VariableInfo varInfo))
            {
                outObj = $"GlobalObject{varInfo.Index}";
                outPriority = varInfo.Priority;
            }

            var p = new List<string>();

            // Type1: ObjectType (12 bits in existing compiler)
            int typeValue = ConvertToObjectType(typeStr);
            p.Add(ConvertToBinary(typeValue, 12));

            // OUTObject: ObjectTypeRef
            p.Add(ConvertObjectTypeRefToBinary(outObj, 0, outPriority));

            // PlaceAt: ObjectTypeRef
            placeAt = RemapVariableToObjectRef(placeAt);
            p.Add(ConvertObjectTypeRefToBinary(placeAt, 0, 1));

            // Label: LabelRef (keep existing "none vs specified" framing used previously)
            p.Add(EncodeLabelRef(label));

            // SpawnFlags: default 0 (best-effort; bits are schema-driven but not currently embedded here)
            p.Add(ConvertToBinary(0, 3));

            // LocationOffset: default (0,0,0) unless user gave 3 extra ints after placeAt/label
            int x = 0, y = 0, z = 0;
            if (args.Count >= 5 && int.TryParse(args[args.Count - 3], out int px) && int.TryParse(args[args.Count - 2], out int py) && int.TryParse(args[args.Count - 1], out int pz))
            {
                x = px; y = py; z = pz;
            }
            p.Add(ConvertVector3ToBinary(x, y, z));

            // Variant: NameIndex (best-effort 8 bits; "none" => 0)
            string variant = "none";
            p.Add(EncodeNameIndex(variant, 8));

            return p;
        }

        // Obj.Attach(Base, Attach, LocationOffset, Relative)
        // Script alias: Attach(child, parent, x, y, z [, relative])
        private List<string> EncodeAction_Attach(List<string> args)
        {
            string child = args.Count > 0 ? args[0] : "NoObject";
            string parent = args.Count > 1 ? args[1] : "NoObject";

            int x = 0, y = 0, z = 0;
            if (args.Count > 2) int.TryParse(args[2], out x);
            if (args.Count > 3) int.TryParse(args[3], out y);
            if (args.Count > 4) int.TryParse(args[4], out z);

            bool relative = true;
            if (args.Count > 5 && TryParseBoolLoose(args[5], out bool rel))
                relative = rel;

            // Megalo expects Base=parent, Attach=child
            parent = RemapVariableToObjectRef(parent);
            child = RemapVariableToObjectRef(child);

            var p = new List<string>
            {
                ConvertObjectTypeRefToBinary(parent, 0, 1),
                ConvertObjectTypeRefToBinary(child, 0, 1),
                ConvertVector3ToBinary(x, y, z),
                ConvertToBinary(relative, 1)
            };
            return p;
        }

        // Obj.Speed_GET(Object, OUTSpeed)
        // Script alias: GetSpeed(object)
        private List<string> EncodeAction_SpeedGet(List<string> args, string? varOut)
        {
            string obj = args.Count > 0 ? args[0] : "current_object";
            obj = RemapVariableToObjectRef(obj);

            string outNum = "Int16";
            if (!string.IsNullOrWhiteSpace(varOut) && _variableToIndexMap.TryGetValue(varOut, out VariableInfo varInfo))
                outNum = $"GlobalNumber{varInfo.Index}";

            var p = new List<string>
            {
                ConvertObjectTypeRefToBinary(obj, 0, 1),
                ConvertNumericTypeRefToBinary(outNum, 0)
            };
            return p;
        }

        // Obj.DropWeapon(Object, Mode, DeleteOnDrop)
        // Script alias: DropWeapon(object)
        private List<string> EncodeAction_DropWeapon(List<string> args)
        {
            string obj = args.Count > 0 ? args[0] : "current_object";
            obj = RemapVariableToObjectRef(obj);

            // Best-effort defaults
            int mode = 0;
            bool deleteOnDrop = false;

            // Allow optional args: DropWeapon(obj, mode, delete)
            if (args.Count > 1) int.TryParse(args[1], out mode);
            if (args.Count > 2 && TryParseBoolLoose(args[2], out bool del)) deleteOnDrop = del;

            var p = new List<string>
            {
                ConvertObjectTypeRefToBinary(obj, 0, 1),
                ConvertToBinary(mode, 1),            // DropWeaponMode bits per XML appear small; keep 1 for now
                ConvertToBinary(deleteOnDrop, 1)
            };
            return p;
        }

        // Obj.kill(Object, SuppressStats)
        // Script alias: Kill(object [, suppressStats])
        private List<string> EncodeAction_Kill(List<string> args)
        {
            string obj = args.Count > 0 ? args[0] : "current_object";
            obj = RemapVariableToObjectRef(obj);

            bool suppress = false;
            if (args.Count > 1 && TryParseBoolLoose(args[1], out bool sup))
                suppress = sup;

            var p = new List<string>
            {
                ConvertObjectTypeRefToBinary(obj, 0, 1),
                ConvertToBinary(suppress, 1)
            };
            return p;
        }

        // Obj.Delete(Object)
        // Script alias: Delete(object)
        private List<string> EncodeAction_Delete(List<string> args)
        {
            string obj = args.Count > 0 ? args[0] : "current_object";
            obj = RemapVariableToObjectRef(obj);

            return new List<string>
            {
                ConvertObjectTypeRefToBinary(obj, 0, 1)
            };
        }

        private string RemapVariableToObjectRef(string maybeVar)
        {
            if (!string.IsNullOrWhiteSpace(maybeVar) && _variableToIndexMap.TryGetValue(maybeVar, out VariableInfo varInfo))
                return $"GlobalObject{varInfo.Index}";
            return maybeVar;
        }

        /// <summary>
        /// Forge label table used to resolve named label references in the
        /// script (e.g. <c>@"flag_home"</c> → index 3). Populated by the
        /// caller before compiling — typically copied from whatever
        /// GametypeReader parsed out of the original .bin's Labels section.
        ///
        /// Matching is case-insensitive. Unknown names fall back to the
        /// "label not present" encoding (1-bit flag = 1) with a diagnostic
        /// so the scripter sees the typo.
        /// </summary>
        public List<string> LabelTable { get; } = new List<string>();

        // Main string table for reverse-lookup: when the script uses
        // `tokens("Score Win")` instead of `tokens(str[N])`, encoder
        // needs to find which N maps to "Score Win". Populated by
        // callers from GametypeReader.Result.StringTable before compile.
        public List<string> StringTable { get; } = new List<string>();

        public readonly List<string> LabelResolutionWarnings = new List<string>();

        /// Accepts any of:
        ///   none / empty           → "not present" (1 bit: 1)
        ///   numeric literal (0-15) → direct index
        ///   @"name" / bareword     → resolved via LabelTable, case-insensitive
        private string EncodeLabelRef(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Equals("none", StringComparison.OrdinalIgnoreCase))
                return ConvertToBinary(1, 1);

            string token = label.Trim();

            // @"flag_home" syntax — strip the @ prefix and quotes.
            if (token.StartsWith("@\"", StringComparison.Ordinal) && token.EndsWith("\""))
                token = token.Substring(2, token.Length - 3);
            else if (token.StartsWith("@") && token.Length > 1)
                token = token.Substring(1);
            else if (token.StartsWith("\"") && token.EndsWith("\"") && token.Length >= 2)
                token = token.Substring(1, token.Length - 2);

            // `label[N]` syntax emitted by the decompiler when no name table
            // is available. Encode directly as 0 (has-label) + 4-bit index.
            var labelIdxMatch = System.Text.RegularExpressions.Regex.Match(
                token, @"^label\s*\[\s*(\d+)\s*\]$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (labelIdxMatch.Success && int.TryParse(labelIdxMatch.Groups[1].Value, out int labelIdx))
            {
                if (labelIdx < 0 || labelIdx > 15)
                    LabelResolutionWarnings.Add($"LabelRef index {labelIdx} out of range (0..15).");
                return ConvertToBinary(0, 1) + ConvertToBinary(labelIdx & 0xF, 4);
            }

            if (int.TryParse(token, out int labelId))
            {
                if (labelId < 0 || labelId > 15)
                    LabelResolutionWarnings.Add($"LabelRef index {labelId} out of range (0..15).");
                return ConvertToBinary(0, 1) + ConvertToBinary(labelId & 0xF, 4);
            }

            // Name-based lookup.
            if (LabelTable.Count > 0)
            {
                for (int i = 0; i < LabelTable.Count; i++)
                {
                    if (string.Equals(LabelTable[i], token, StringComparison.OrdinalIgnoreCase))
                        return ConvertToBinary(0, 1) + ConvertToBinary(i, 4);
                }
                LabelResolutionWarnings.Add(
                    $"Forge label '{token}' not found in LabelTable (known: [{string.Join(", ", LabelTable)}]).");
            }
            else
            {
                LabelResolutionWarnings.Add(
                    $"Forge label '{token}' referenced but LabelTable is empty; "
                    + "populate ScriptCompiler.LabelTable from the source .bin's Labels section.");
            }

            // Unknown name, no table — encode as "not present" so the compiled
            // script doesn't reference an index we can't verify.
            return ConvertToBinary(1, 1);
        }



        // ============================================================
        // Condition encoders
        // ============================================================

        // Obj.IsOfType(Object, Type1)
        // Script alias: ObjectIsType(object, type)
        private List<string> EncodeCondition_IsOfType(List<string> args)
        {
            string obj = args.Count > 0 ? args[0] : "current_object";
            string typeStr = args.Count > 1 ? args[1] : "none";

            int priority = 1;
            if (!string.IsNullOrWhiteSpace(obj) && _variableToIndexMap.TryGetValue(obj, out VariableInfo vi))
            {
                obj = $"GlobalObject{vi.Index}";
                priority = vi.Priority;
            }

            var p = new List<string>
            {
                ConvertObjectTypeRefToBinary(obj, 0, priority),
                ConvertToBinary((ObjectType)Enum.Parse(typeof(ObjectType), typeStr, true), 12),
            };
            return p;
        }
        private string EncodeNameIndex(string name, int bits)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Equals("none", StringComparison.OrdinalIgnoreCase))
                return ConvertToBinary(0, bits);

            // If your project defines NameIndex enum, ConvertToBinary(string, bits) will try to parse it.
            return ConvertToBinary(name, bits);
        }

        private static bool StatementMayEmitMegalo(StatementSyntax st)
        {
            // Keep this conservative: return true unless you're confident it emits nothing.
            return st switch
            {
                EmptyStatementSyntax => false,

                // These definitely emit code:
                IfStatementSyntax => true,
                ExpressionStatementSyntax => true,
                ForStatementSyntax => true,
                WhileStatementSyntax => true,
                DoStatementSyntax => true,
                SwitchStatementSyntax => true,
                ReturnStatementSyntax => true,

                // Locals: only emit if initializer exists (e.g. Object x = Player.Weapon_GET(...);)
                LocalDeclarationStatementSyntax lds => lds.Declaration.Variables.Any(v => v.Initializer != null),

                // Default: assume it might emit
                _ => true
            };
        }

        private static bool HasFollowingMegaloEmittingStatements(IReadOnlyList<StatementSyntax> list, int index)
        {
            for (int i = index + 1; i < list.Count; i++)
            {
                if (StatementMayEmitMegalo(list[i]))
                    return true;
            }
            return false;
        }





        private static bool TryMapEventWrapper(string name, out string attribute)
        {
            // Decompiler-emitted event-wrapper names (lowercased / synthetic).
            // Anonymous OnTick wrappers are emitted as `__OnTick_N` by the
            // dialect preprocessor.
            if (!string.IsNullOrEmpty(name) && name.StartsWith("__OnTick", StringComparison.Ordinal))
            {
                attribute = "OnTick";
                return true;
            }
            switch (name)
            {
                // Decompiler form (lowercased / snake_case)
                case "local":           attribute = "OnLocal"; return true;
                case "init":            attribute = "OnInit"; return true;
                case "local_init":      attribute = "OnLocalInit"; return true;
                case "host_migration":  attribute = "OnHostMigration"; return true;
                case "object_death":    attribute = "OnObjectDeath"; return true;
                case "pregame":         attribute = "OnPregame"; return true;
                // PascalCase (legacy script form)
                case "Local":           attribute = "OnLocal"; return true;
                case "Init":            attribute = "OnInit"; return true;
                case "LocalInit":       attribute = "OnLocalInit"; return true;
                case "HostMigration":   attribute = "OnHostMigration"; return true;
                case "ObjectDeath":     attribute = "OnObjectDeath"; return true;
                case "Pregame":         attribute = "OnPregame"; return true;
                case "Call":            attribute = "OnCall"; return true;
                default: attribute = ""; return false;
            }
        }

        private static bool IsTriggerLocalFunction(LocalFunctionStatementSyntax lf)
            => lf.ReturnType.ToString() == "Trigger";

        private int ReservePendingTrigger(LocalFunctionStatementSyntax lf, string defaultAttribute)
        {
            int idx = _triggers.Count;

            // Placeholder; replaced when we compile this trigger in DrainPendingTriggers()
            _triggers.Add(new TriggerObject(lf.Identifier.Text, new List<string> { "" }));

            _pendingTriggers.Enqueue(new PendingTrigger(lf, idx, defaultAttribute));
            return idx;
        }

        private void DrainPendingTriggers()
        {
            while (_pendingTriggers.Count > 0)
            {
                var p = _pendingTriggers.Dequeue();
                ProcessTrigger(p.Syntax, fixedIndex: p.Index, defaultAttribute: p.DefaultAttribute);
            }
        }

        private void ProcessTrigger(LocalFunctionStatementSyntax method, int? fixedIndex = null, string? defaultAttribute = null)
        {
            // Default trigger type and attribute
            string triggerType = method.Identifier.Text;
            string triggerAttribute = defaultAttribute ?? "OnTick";

            // Wrapper triggers (Local/Init/etc) compile as Do + mapped attribute.
            if (TryMapEventWrapper(method.Identifier.Text, out var wrapperAttr))
            {
                triggerType = "Do";
                triggerAttribute = wrapperAttr;

                // Note: ALLOW multiple OnTick wrappers (`__OnTick_0`,
                // `__OnTick_1` …) — the original Megalo file frequently
                // contains many. Only enforce uniqueness for the once-
                // per-game events (Init, LocalInit, …) where the engine
                // requires a single root trigger.
                if (!string.Equals(wrapperAttr, "OnTick", StringComparison.Ordinal)
                    && !string.Equals(wrapperAttr, "OnCall", StringComparison.Ordinal))
                {
                    if (!_eventWrapperAttributes.Add(wrapperAttr))
                        throw new InvalidOperationException($"Only one root trigger is allowed for event '{wrapperAttr}'.");
                }
            }


            // Check for specific trigger attribute
            if (method.AttributeLists.Count > 0)
            {
                var attribute = method.AttributeLists[0].Attributes[0];
                if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0)
                    triggerAttribute = attribute.ArgumentList.Arguments[0].ToString().Trim('"');
            }

            CompileTriggerFromBody(
                method.Body,
                triggerType,
                triggerAttribute,
                labelToken: null,
                fixedIndex: fixedIndex);
        }

        /// <summary>
        /// Shared trigger-emission core. Walks `body`, captures every
        /// condition/action it generated into a trigger record with the
        /// supplied (type, attribute, optional LabelRef token). Used by:
        ///   • ProcessTrigger (Do triggers + event wrappers)
        ///   • ProcessForeachStatement (Foreach* triggers)
        /// `labelToken` is only honored for triggerType == "Labeled" and is
        /// passed through EncodeLabelRef (accepts numeric, "label[N]", or
        /// quoted name).
        /// </summary>
        private void CompileTriggerFromBody(
            BlockSyntax? body,
            string triggerType,
            string triggerAttribute,
            string? labelToken,
            int? fixedIndex = null)
        {
            _deferredInlines.Clear();
            _orSeqCounter = 0; // fresh OrSequence allocation per trigger.

            // IMPORTANT:
            // These are the start offsets that the trigger must reference.
            int startConditionOffset = _conditions.Count;
            int startActionOffset = _actions.Count;


            // Base for LOCAL condition action offsets in this trigger
            _actionBaseStack.Push(startActionOffset);
            // These are cursors that move while we compile statements.
            int conditionOffsetCursor = startConditionOffset;
            int actionOffsetCursor = startActionOffset;

            int conditionCount = 0;
            int actionCount = 0;

            // Push a new scope onto the stack
            _scopeStack.Push(new Dictionary<string, VariableInfo>());

            if (body != null)
            {
                ProcessStatementList(body.Statements, ref actionCount, ref conditionCount, ref actionOffsetCursor, ref conditionOffsetCursor, true);
            }

            // Compute counts from the *start offsets*, not the mutated cursors
            int finalConditionCount = _conditions.Count - startConditionOffset;
            int finalActionCount = _actions.Count - startActionOffset;

            EndScope();

            // Trigger-record bit widths (haloreachnew FUN_1801f700c):
            //   condStart=9b condCount=10b actStart=10b actCount=11b
            // Engine caps: 512 conditions, 1024 actions; the wider count
            // fields don't help because the pool arrays themselves are
            // fixed-size with no bounds checks (see GetBinaryString).
            if (startConditionOffset > EngineMaxConditions - 1)
                throw new InvalidOperationException(
                    $"Trigger condition-start offset {startConditionOffset} would index past condition pool (max {EngineMaxConditions - 1}).");
            if (startActionOffset > EngineMaxActions - 1)
                throw new InvalidOperationException(
                    $"Trigger action-start offset {startActionOffset} would index past action pool (max {EngineMaxActions - 1}).");
            if (startConditionOffset + finalConditionCount > EngineMaxConditions)
                throw new InvalidOperationException(
                    $"Trigger condition range [{startConditionOffset}..{startConditionOffset + finalConditionCount}) overflows condition pool (cap {EngineMaxConditions}).");
            if (startActionOffset + finalActionCount > EngineMaxActions)
                throw new InvalidOperationException(
                    $"Trigger action range [{startActionOffset}..{startActionOffset + finalActionCount}) overflows action pool (cap {EngineMaxActions}).");

            // Create the trigger binary representation
            string conditionOffsetBinary = ConvertToBinary(startConditionOffset, 9);
            string conditionCountBinary = ConvertToBinary(finalConditionCount, 10);
            string actionOffsetBinary = ConvertToBinary(startActionOffset, 10);
            string actionCountBinary = ConvertToBinary(finalActionCount, 11);

            string triggerTypeBinary = ConvertToBinary(Enum.Parse(typeof(TriggerTypeEnum), triggerType), 3);
            string triggerAttributeBinary = ConvertToBinary(Enum.Parse(typeof(TriggerAttributeEnum), triggerAttribute), 3);

            // Reach trigger layout (per ScriptDecompiler.ParseTrigger):
            //   Type(3) + Attribute(3) + [LabelRef if Type=Labeled]
            //     + condOff(9) + condCount(10) + actOff(10) + actCount(11)
            string labelBits = string.Empty;
            if (string.Equals(triggerType, "Labeled", StringComparison.Ordinal))
                labelBits = EncodeLabelRef(labelToken ?? "none");

            string binaryTrigger =
                triggerTypeBinary +
                triggerAttributeBinary +
                labelBits +
                conditionOffsetBinary +
                conditionCountBinary +
                actionOffsetBinary +
                actionCountBinary;

            var triggerObj = new TriggerObject(triggerType, new List<string> { binaryTrigger });

            if (fixedIndex.HasValue)
                _triggers[fixedIndex.Value] = triggerObj;
            else
                _triggers.Add(triggerObj);


            // After _triggers.Add(...)
            foreach (var patch in _deferredInlines)
            {
                int condOffset = _conditions.Count;
                int condCount = patch.Conditions.Count;

                // Append conditions
                _conditions.AddRange(patch.Conditions);

                int actOffset = _actions.Count;
                int actCount = patch.Actions.Count;

                // Append actions
                _actions.AddRange(patch.Actions);

                // Patch the Inline action now that we know final offsets
                string fixedInline = BuildInlineBinary(condOffset, condCount, actOffset, actCount);
                _actions[patch.InlineActionIndex].Parameters[0] = fixedInline;

                Debug.WriteLine($"Patched Inline at {patch.InlineActionIndex}: condOff={condOffset} condCount={condCount} actOff={actOffset} actCount={actCount}");
            }
            _actionBaseStack.Pop();



            Debug.WriteLine($"Created trigger: {triggerType}({triggerAttribute}) with {finalConditionCount} conditions and {finalActionCount} actions");
        }




        // Engine pool caps — confirmed via RE of haloreachnew FUN_180055044
        // (the megalo pool decoder). The arrays are fixed-size with NO bounds
        // checks; going past these caps writes past the array end, corrupts
        // the next pool's count field, and crashes. The wider file-format
        // count fields (10/11/9 bits) are NOT usable — exceeding the engine
        // cap is undefined behavior, not soft-rejection.
        //
        // Pool struct layout (haloreachnew):
        //   triggers   @ +0x008..+0xF08  stride 0x0C  =  320 slots
        //   conditions @ +0xF10..+0x2F10 stride 0x10  =  512 slots
        //   actions    @ +0x2F18..+0x7F18 stride 0x14 = 1024 slots
        private const int EngineMaxConditions = 512;
        private const int EngineMaxActions    = 1024;
        private const int EngineMaxTriggers   = 320;

        // Soft-warn thresholds — surfaced as warnings so users see how close
        // they are before they hit the wall.
        private const int WarnConditionsThreshold = (int)(EngineMaxConditions * 0.9);  // 460
        private const int WarnActionsThreshold    = (int)(EngineMaxActions    * 0.9);  // 921
        private const int WarnTriggersThreshold   = (int)(EngineMaxTriggers   * 0.9);  // 288

        public int DiagPoolConditions => _conditions.Count;
        public int DiagPoolActions    => _actions.Count;
        public int DiagPoolTriggers   => _triggers.Count;

        private string GetBinaryString()
        {
            // Count the number of conditions, actions, and triggers
            int conditionCount = _conditions.Count;
            int actionCount = _actions.Count;
            int triggerCount = _triggers.Count;

            if (conditionCount > EngineMaxConditions)
                throw new InvalidOperationException(
                    $"Compiled {conditionCount} conditions; engine cap is {EngineMaxConditions}. Exceeding crashes the game (no bounds check). Reduce conditions or split across gametypes.");
            if (actionCount > EngineMaxActions)
                throw new InvalidOperationException(
                    $"Compiled {actionCount} actions; engine cap is {EngineMaxActions}. Exceeding crashes the game (no bounds check). Reduce actions, inline-fold, or split across gametypes.");
            if (triggerCount > EngineMaxTriggers)
                throw new InvalidOperationException(
                    $"Compiled {triggerCount} triggers; engine cap is {EngineMaxTriggers}. Exceeding crashes the game (no bounds check). Reduce triggers or split across gametypes.");

            // Approaching-cap warnings.
            if (actionCount > WarnActionsThreshold)
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"Action pool: {actionCount}/{EngineMaxActions} ({100 * actionCount / EngineMaxActions}%).",
                    0, 0));
            if (conditionCount > WarnConditionsThreshold)
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"Condition pool: {conditionCount}/{EngineMaxConditions} ({100 * conditionCount / EngineMaxConditions}%).",
                    0, 0));
            if (triggerCount > WarnTriggersThreshold)
                _encoderDiagnostics.Add(new CompilerDiagnostic(
                    CompilerDiagnosticSeverity.Warning,
                    $"Trigger pool: {triggerCount}/{EngineMaxTriggers} ({100 * triggerCount / EngineMaxTriggers}%).",
                    0, 0));

            // Convert the counts to binary strings
            string conditionCountBinary = Convert.ToString(conditionCount, 2).PadLeft(10, '0');
            string actionCountBinary = Convert.ToString(actionCount, 2).PadLeft(11, '0');
            string triggerCountBinary = Convert.ToString(triggerCount, 2).PadLeft(9, '0');

            // Concatenate the binary strings for the conditions, actions, and triggers
            string conditionsBinary = string.Join("", _conditions.Select(c => c.Parameters.First()));
            string actionsBinary = string.Join("", _actions.Select(a => a.Parameters.First()));
            string triggersBinary = string.Join("", _triggers.Select(t => t.Parameters.First()));

            // Process global variables
            string globalVariablesBinary = ProcessGlobalVariables();

            // Combine the counts and the binary strings for the conditions, actions, triggers, and global variables
            string finalBinaryString = conditionCountBinary + conditionsBinary + actionCountBinary + actionsBinary + triggerCountBinary + triggersBinary + "000" + globalVariablesBinary;

            // Calculate the total number of bits up to WeaponTunings
            int totalBitsUpToWeaponTunings = 3 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 224 + 1824 + 5 + 7 + 1 + 16 + 1 + 4 + 1 + 12 + 7 + 35;

            // Append that many zeros to the final binary string
            finalBinaryString += new string('0', totalBitsUpToWeaponTunings);

            // Set all WeaponTunings bits to 225 in binary
            string weaponTuningsBinary = Convert.ToString(255, 2).PadLeft(1, '0') + // FullAutomagnum
                                         Convert.ToString(225, 2).PadLeft(1, '0') + // MustHaveEnergySwordToBlock
                                         Convert.ToString(225, 2).PadLeft(1, '0') + // EnableActiveCamoModifiers
                                         Convert.ToString(225, 2).PadLeft(1, '0') + // ArmorLockCanBeStuck
                                         Convert.ToString(225, 2).PadLeft(1, '0') + // Armorlockdoesntshednades
                                         Convert.ToString(225, 2).PadLeft(1, '0') + // ShieldBleedthrough
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // PrecisionWeaponBloom
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // ArmorLockDamageDrain
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // ArmorLockDamageDrainLimit
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // ActivecamoEnergyDraincurveMax
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // ActivecamoEnergyDraincurveMin
                                         Convert.ToString(225, 2).PadLeft(8, '0') + // MagnumDamage
                                         Convert.ToString(225, 2).PadLeft(8, '0');  // MagnumFireDelayModifier

            // Append the WeaponTunings binary string to the final binary string
            finalBinaryString += weaponTuningsBinary;

            finalBinaryString += new string('0', totalBitsUpToWeaponTunings);

            return finalBinaryString;
        }

        private string TranslateVariableName(string type, int index)
        {
            if (type == "Object")
            {
                return $"GlobalObject{index}";
            }
            else if (type == "Player")
            {
                return $"GlobalPlayer{index}";
            }
            else if (type == "Team")
            {
                return $"GlobalTeam{index}";
            }
            else if (type == "Timer")
            {
                return $"GlobalTimer{index}";
            }
            else if (type == "Number")
            {
                return $"GlobalNumber{index}";
            }
            else
            {
                throw new ArgumentException($"Unsupported variable type: {type}");
            }
        }

        private string ProcessGlobalVariables()
        {
            // Initialize binary string for global variables
            string globalVariablesBinary = "";

            // Process global numbers
            var globalNumbers = _allDeclaredVariables.Where(kvp => kvp.Value.Type == "Number").ToList();
            globalVariablesBinary += Convert.ToString(globalNumbers.Count, 2).PadLeft(4, '0');
            foreach (var kvp in globalNumbers)
            {
                string translatedName = TranslateVariableName(kvp.Value.Type, kvp.Value.Index);
                globalVariablesBinary += ConvertToBinary(0, 6); // Number
                globalVariablesBinary += ConvertToBinary(0, 16);
                globalVariablesBinary += ConvertToBinary(kvp.Value.Priority, 2); // Locality
            }

            // Process global timers
            var globalTimers = _allDeclaredVariables.Where(kvp => kvp.Value.Type == "Timer").ToList();
            globalVariablesBinary += Convert.ToString(globalTimers.Count, 2).PadLeft(4, '0');
            foreach (var kvp in globalTimers)
            {
                string translatedName = TranslateVariableName(kvp.Value.Type, kvp.Value.Index);
                globalVariablesBinary += ConvertToBinary(0, 0); // Number
            }

            // Process global teams
            var globalTeams = _allDeclaredVariables.Where(kvp => kvp.Value.Type == "Team").ToList();
            globalVariablesBinary += Convert.ToString(globalTeams.Count, 2).PadLeft(4, '0');
            foreach (var kvp in globalTeams)
            {
                string translatedName = TranslateVariableName(kvp.Value.Type, kvp.Value.Index);
                globalVariablesBinary += ConvertToBinary(0, 4); // Team
                globalVariablesBinary += ConvertToBinary(kvp.Value.Priority, 2); // Locality
            }

            // Process global players
            var globalPlayers = _allDeclaredVariables.Where(kvp => kvp.Value.Type == "Player").ToList();
            globalVariablesBinary += Convert.ToString(globalPlayers.Count, 2).PadLeft(4, '0');
            foreach (var kvp in globalPlayers)
            {
                string translatedName = TranslateVariableName(kvp.Value.Type, kvp.Value.Index);
                globalVariablesBinary += ConvertToBinary(kvp.Value.Priority, 2); // Locality
            }

            // Process global objects
            var globalObjects = _allDeclaredVariables.Where(kvp => kvp.Value.Type == "Object").ToList();
            globalVariablesBinary += Convert.ToString(globalObjects.Count, 2).PadLeft(5, '0');
            foreach (var kvp in globalObjects)
            {
                string translatedName = TranslateVariableName(kvp.Value.Type, kvp.Value.Index);
                globalVariablesBinary += ConvertToBinary(kvp.Value.Priority, 2); // Locality
            }

            // Initialize counts for PlayerVars, ObjectVars, and TeamVars to zero
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(4, '0'); // playernumbers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // playertimers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // playerteams count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // playerplayers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // playerobjects count

            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(4, '0'); // objectnumbers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // objecttimers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(2, '0'); // objectteams count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // objectplayers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // objectobjects count

            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(4, '0'); // teamnumbers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // teamtimers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // teamteams count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // teamplayers count
            globalVariablesBinary += Convert.ToString(0, 2).PadLeft(3, '0'); // teamobjects count

            return globalVariablesBinary;
        }

        // Replace the ProcessBinaryCondition method with this improved version
        // Encode a Megl.If operand as a (VarType tag, payload) pair so the
        // condition record matches the megalo VarType variant layout (3-bit
        // tag + variant payload). Picks the right variant from the
        // expression shape: dotted member access → ObjectVar; player/team/
        // timer/no_object identifiers → matching variant; numeric literals
        // and number-typed identifiers → NumericVar.
        private (string tag, string payload) EncodeConditionOperand(ExpressionSyntax expr)
        {
            // Strip parentheses.
            if (expr is ParenthesizedExpressionSyntax paren)
                return EncodeConditionOperand(paren.Expression);

            // Logical NOT: descend (cond record's NOT bit is set elsewhere
            // by ProcessCondition; for the operand-level encoding we just
            // unwrap and emit the inner operand).
            if (expr is PrefixUnaryExpressionSyntax unary
                && unary.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
                return EncodeConditionOperand(unary.Operand);

            // Unary minus literal: `-12345` → NumericVar Int16 with signed
            // 16-bit payload. Roslyn parses this as PrefixUnaryExpression
            // (kind=UnaryMinusExpression) wrapping a numeric literal.
            if (expr is PrefixUnaryExpressionSyntax neg
                && neg.OperatorToken.IsKind(SyntaxKind.MinusToken)
                && neg.Operand is LiteralExpressionSyntax negLit)
            {
                int v = 0;
                int.TryParse(negLit.Token.ValueText, out v);
                v = -v;
                string nt = Convert.ToString((int)NumericTypeRefEnum.Int16, 2).PadLeft(6, '0');
                return ("000", nt + ToSigned16Binary(v));
            }

            // ElementAccessExpression — the decompiler emits things like
            // `script_option[N]`. Stringify and route through NumericTypeRef
            // (which knows the script_option variant).
            if (expr is ElementAccessExpressionSyntax eae)
            {
                string text = eae.ToString();
                try
                {
                    return ("000", ConvertNumericTypeRefToBinary(text, 0));
                }
                catch (Exception ex)
                {
                    _encoderDiagnostics.Add(new CompilerDiagnostic(
                        CompilerDiagnosticSeverity.Warning,
                        $"Condition operand '{text}' (ElementAccessExpression) — no encoder ({ex.Message}); payload empty.",
                        1, 1));
                    return ("000", string.Empty);
                }
            }

            // Member access: current_player.playerobject0, current_object.foo, etc.
            // The receiver tells us the variable kind:
            //   .playerobjectN / .objectobjectN / .teamobjectN / .biped → ObjectTypeRef
            //   .playernumberN / .objectnumberN / .teamnumberN         → NumericTypeRef
            //   .score / .money / .rating                              → NumericTypeRef
            //   .playerteam* / .teamteam*                              → TeamTypeRef
            //   .playertimer* / etc.                                   → TimerTypeRef
            //   .playerplayer*                                         → PlayerTypeRef
            if (expr is MemberAccessExpressionSyntax ma)
            {
                string text = ma.ToString();
                string memberName = ma.Name.Identifier.Text;

                // Numeric variants (player/object/team-scoped numbers + score/money/rating)
                if (memberName.StartsWith("playernumber", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("objectnumber", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("teamnumber", StringComparison.OrdinalIgnoreCase)
                    || memberName.Equals("score", StringComparison.OrdinalIgnoreCase)
                    || memberName.Equals("money", StringComparison.OrdinalIgnoreCase)
                    || memberName.Equals("rating", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return ("000", ConvertNumericTypeRefToBinary(text, 0));
                    }
                    catch (Exception ex)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"Condition operand '{text}' couldn't encode as NumericTypeRef ({ex.Message}); payload empty.",
                            1, 1));
                        return ("000", string.Empty);
                    }
                }

                // Player variants (player/object/team-scoped player slots).
                // `current_object.objectplayer0`, `globalplayer0.playerplayer1`,
                // `current_team.teamplayer2` → PlayerTypeRef variants 1/2/3.
                if (memberName.StartsWith("playerplayer", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("objectplayer", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("teamplayer", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return ("001", ConvertPlayerTypeRefToBinary(text, 0));
                    }
                    catch (Exception ex)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"Condition operand '{text}' couldn't encode as PlayerTypeRef ({ex.Message}); payload empty.",
                            1, 1));
                        return ("001", string.Empty);
                    }
                }
                // Team-scoped sub-slots → TeamTypeRef variants.
                if (memberName.StartsWith("playerteam", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("objectteam", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("teamteam", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return ("011", ConvertTeamTypeRefToBinary(text, 0));
                    }
                    catch (Exception ex)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"Condition operand '{text}' couldn't encode as TeamTypeRef ({ex.Message}); payload empty.",
                            1, 1));
                        return ("011", string.Empty);
                    }
                }
                // Timer-scoped sub-slots → TimerTypeRef variants.
                if (memberName.StartsWith("playertimer", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("objecttimer", StringComparison.OrdinalIgnoreCase)
                    || memberName.StartsWith("teamtimer", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return ("100", ConvertTimerTypeRefToBinary(text, 0));
                    }
                    catch (Exception ex)
                    {
                        _encoderDiagnostics.Add(new CompilerDiagnostic(
                            CompilerDiagnosticSeverity.Warning,
                            $"Condition operand '{text}' couldn't encode as TimerTypeRef ({ex.Message}); payload empty.",
                            1, 1));
                        return ("100", string.Empty);
                    }
                }

                // Default: try ObjectTypeRef (handles .biped, .playerobjectN,
                // .objectobjectN, .teamobjectN, .playerplayerN.biped, etc.).
                try
                {
                    return ("010", ConvertObjectTypeRefToBinary(text, 0, 1));
                }
                catch
                {
                    _encoderDiagnostics.Add(new CompilerDiagnostic(
                        CompilerDiagnosticSeverity.Warning,
                        $"Condition operand '{text}' couldn't encode as ObjectTypeRef — emitting empty payload (will desync).",
                        1, 1));
                    return ("010", string.Empty);
                }
            }

            if (expr is IdentifierNameSyntax id)
            {
                string name = id.Identifier.Text;

                // Resolved variable from the decl table.
                if (_variableToIndexMap.TryGetValue(name, out VariableInfo varInfo))
                {
                    switch (varInfo.Type)
                    {
                        case "Number":
                            return ("000", ConvertNumericTypeRefToBinary($"GlobalNumber{varInfo.Index}", 0));
                        case "Object":
                            return ("010", ConvertObjectTypeRefToBinary($"GlobalObject{varInfo.Index}", 0, 1));
                        case "Player":
                            return ("001", ConvertPlayerTypeRefToBinary($"GlobalPlayer{varInfo.Index}", 0));
                        case "Team":
                            return ("011", ConvertTeamTypeRefToBinary($"GlobalTeam{varInfo.Index}", 0));
                        case "Timer":
                            return ("100", ConvertTimerTypeRefToBinary($"GlobalTimer{varInfo.Index}", 0));
                    }
                }

                // Bare globalnumberN / globalplayerN / globalobjectN /
                // globalteamN / globaltimerN identifiers — common when the
                // decompiler emits without going through the decl table.
                if (name.StartsWith("globalnumber", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("GlobalNumber", StringComparison.OrdinalIgnoreCase))
                    return ("000", ConvertNumericTypeRefToBinary(name, 0));

                // Decompiler-synthesized temp identifiers used as condition
                // operands. Route to the matching VarType variant.
                if (name.StartsWith("temp_num_", StringComparison.OrdinalIgnoreCase))
                    return ("000", ConvertNumericTypeRefToBinary(name, 0));
                if (name.StartsWith("temp_obj_", StringComparison.OrdinalIgnoreCase))
                    return ("010", ConvertObjectTypeRefToBinary(name, 0, 1));
                if (name.StartsWith("temp_player_", StringComparison.OrdinalIgnoreCase))
                    return ("001", ConvertPlayerTypeRefToBinary(name, 0));
                if (name.StartsWith("temp_team_", StringComparison.OrdinalIgnoreCase))
                    return ("011", ConvertTeamTypeRefToBinary(name, 0));

                // Built-in NumericTypeRef no-payload variants (script_option /
                // round-settings) — score_to_win, teams_enabled, round_time_limit,
                // perfection_enabled, etc. ConvertNumericTypeRefToBinary
                // accepts both `teams_enabled` and `teams__enabled`.
                try
                {
                    return ("000", ConvertNumericTypeRefToBinary(name, 0));
                }
                catch
                {
                    // not a numeric — fall through to object/player/team checks.
                }

                // Built-in object/player/team identifiers.
                if (name.Equals("no_object", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("NoObject", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("current_object", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("globalobject", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("GlobalObject", StringComparison.OrdinalIgnoreCase))
                {
                    return ("010", ConvertObjectTypeRefToBinary(name, 0, 1));
                }
                if (name.Equals("current_player", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("no_player", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("NoPlayer", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("GlobalPlayer", StringComparison.OrdinalIgnoreCase))
                {
                    return ("001", ConvertPlayerTypeRefToBinary(name, 0));
                }
                if (name.Equals("current_team", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("no_team", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("NoTeam", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("GlobalTeam", StringComparison.OrdinalIgnoreCase))
                {
                    return ("011", ConvertTeamTypeRefToBinary(name, 0));
                }
            }

            // Literal — numeric Int16.
            if (expr is LiteralExpressionSyntax lit)
            {
                int v = 0;
                int.TryParse(lit.Token.ValueText, out v);
                string nt = Convert.ToString((int)NumericTypeRefEnum.Int16, 2).PadLeft(6, '0');
                return ("000", nt + ToSigned16Binary(v));
            }

            // Default: empty payload (decoder will desync — but we emit a
            // diagnostic so it's visible).
            _encoderDiagnostics.Add(new CompilerDiagnostic(
                CompilerDiagnosticSeverity.Warning,
                $"Condition operand '{expr}' (kind={expr.Kind()}) — no encoder; payload empty.",
                1, 1));
            return ("000", string.Empty);
        }

        private void ProcessBinaryCondition(BinaryExpressionSyntax binaryExpression, ref int conditionCount, ref int actionOffset, ref int conditionOffset, int orSequence = 0, bool isNot = false)
        {
            // Determine operator type
            string operatorSymbol = binaryExpression.OperatorToken.ValueText;
            string operatorBinary = "";

            switch (operatorSymbol)
            {
                case "<": operatorBinary = "000"; break;    // LessThan
                case ">": operatorBinary = "001"; break;    // GreaterThan
                case "==": operatorBinary = "010"; break;   // Equals
                case "<=": operatorBinary = "011"; break;   // LessThanEquals
                case ">=": operatorBinary = "100"; break;   // GreaterThanEquals
                case "!=": operatorBinary = "101"; break;   // NotEquals
                default:
                    Debug.WriteLine($"Unsupported operator: {operatorSymbol}");
                    return;
            }

            // The decompiler emits megalo NOT-flagged comparisons as `!a OP b`
            // (which C# parses as `(!a) OP b`). Peel a leading `!` off the
            // LHS and apply it as the cond record's NOT flag — this is
            // semantically `!(a OP b)`, not `(!a) OP b`.
            ExpressionSyntax leftExpr  = binaryExpression.Left;
            ExpressionSyntax rightExpr = binaryExpression.Right;
            while (leftExpr is ParenthesizedExpressionSyntax lp) leftExpr = lp.Expression;
            if (leftExpr is PrefixUnaryExpressionSyntax leftUnary
                && leftUnary.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            {
                isNot = !isNot;
                leftExpr = leftUnary.Operand;
                while (leftExpr is ParenthesizedExpressionSyntax lp2) leftExpr = lp2.Expression;
            }

            // Process LHS / RHS — pick VarType variant by operand shape:
            //   NumericVar (000) — number variable or numeric literal
            //   ObjectVar  (010) — member-access (current_player.playerobject0)
            //                       or identifier resolving to ObjectTypeRef
            //                       (current_object, no_object, GlobalObjectN)
            //   PlayerVar  (001) — current_player / GlobalPlayerN / no_player
            //   TeamVar    (011) — current_team / GlobalTeamN / no_team
            //   TimerVar   (100) — GlobalTimerN
            (string leftVarType, string leftVarBinary) = EncodeConditionOperand(leftExpr);
            (string rightVarType, string rightVarBinary) = EncodeConditionOperand(rightExpr);

            // Convert the condition number to a 5-bit binary string (Megl.If = 1)
            string conditionNumberBinary = ConvertToBinary(1, 5);

            // Convert NOT and ORSequence to binary strings
            string notBinary = ConvertToBinary(isNot ? 1 : 0, 1);
            string orSequenceBinary = ConvertToBinary(orSequence, 9);

            // Use the current actions count as the target for this condition
            // This is critical for correctly pointing to the first action that should run if this condition is true
            int currentActionOffset = actionOffset; // global cursor (next action in this container)
            int localActionOffset = GetLocalActionOffset(currentActionOffset);
            string actionOffsetBinary = ConvertToBinary(localActionOffset, 10);
            // Construct the full binary condition
            string binaryCondition = conditionNumberBinary + notBinary + orSequenceBinary + actionOffsetBinary
                                   + leftVarType + leftVarBinary
                                   + rightVarType + rightVarBinary
                                   + operatorBinary;

            // Add the condition to the conditions list
            _conditions.Add(new ConditionObject("Megl.If", new List<string> { binaryCondition }));
            Debug.WriteLine($"Added binary condition: Megl.If({binaryCondition}) pointing to LOCAL action {localActionOffset} (base={CurrentActionBase}, global={currentActionOffset})");

            // Encoder-side trace: per-Megl_If payload widths so we can
            // diff against the decoder's --diff-trace output.
            if (CompilerCondTrace != null)
            {
                int hdrBits = conditionNumberBinary.Length + notBinary.Length + orSequenceBinary.Length + actionOffsetBinary.Length;
                int payBits = leftVarType.Length + leftVarBinary.Length + rightVarType.Length + rightVarBinary.Length + operatorBinary.Length;
                CompilerCondTrace.AppendLine(
                    $"  enc cond #{_conditions.Count - 1} Megl.If hdr={hdrBits}b" +
                    $" L=[tag={leftVarType} ({leftVarType.Length}b) pay={leftVarBinary.Length}b ({leftExpr})]" +
                    $" R=[tag={rightVarType} ({rightVarType.Length}b) pay={rightVarBinary.Length}b ({rightExpr})]" +
                    $" op={operatorBinary.Length}b total={hdrBits + payBits}b not={(isNot?1:0)}");
            }

            // Increment condition count
            conditionCount++;
        }

        // Per-Megl_If encoder trace (collects per-call widths). Set by the
        // CLI harness, ignored otherwise.
        public static System.Text.StringBuilder? CompilerCondTrace { get; set; }




        private void ProcessCondition(
            ExpressionSyntax condition,
            ref int conditionCount,
            ref int actionOffset,
            ref int conditionOffset,
            int orSequence = 0,
            bool isNot = false)
        {
            // Parentheses
            if (condition is ParenthesizedExpressionSyntax paren)
            {
                ProcessCondition(paren.Expression, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                return;
            }

            // Handle NOT
            if (condition is PrefixUnaryExpressionSyntax unary
                && unary.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            {
                ProcessCondition(unary.Operand, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot: true);
                return;
            }

            // Handle binary expressions
            if (condition is BinaryExpressionSyntax bin)
            {
                if (bin.IsKind(SyntaxKind.LogicalOrExpression))
                {
                    // `a || b` — both halves share the SAME orSequence so the
                    // decoder groups them as one OR-set. (Decoder logic at
                    // FormatConditionsCSharp groups conditions by OrSequence
                    // and OR's within a group, AND's across groups.)
                    ProcessCondition(bin.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                    ProcessCondition(bin.Right, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                    return;
                }

                if (bin.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    // `a && b` — each half is its own AND-group; allocate
                    // fresh orSequences so the decoder treats them as
                    // distinct groups (AND'd across).
                    ProcessCondition(bin.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                    ProcessCondition(bin.Right, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence + 1, isNot);
                    return;
                }

                // Comparison / arithmetic-based condition -> Megl.If
                ProcessBinaryCondition(bin, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                return;
            }

            // Invocation condition (e.g., Obj.IsOfType(obj, needle_rifle))
            if (condition is InvocationExpressionSyntax invocation)
            {
                string? scriptName = GetInvokedName(invocation.Expression);
                var args = invocation.ArgumentList.Arguments.Select(a => TrimArg(a.Expression)).ToList();

                // Member-access form: receiver.method(args) — try the
                // trailing method name and pass the receiver as arg[0].
                string? memberMethod = null;
                string? memberReceiver = null;
                if (invocation.Expression is MemberAccessExpressionSyntax ma)
                {
                    memberMethod = ma.Name.Identifier.Text;
                    memberReceiver = ma.Expression.ToString();
                }

                MegaloCondition cond = default;
                bool resolved = false;
                bool receiverInjected = false;
                if (!string.IsNullOrWhiteSpace(scriptName))
                {
                    try { cond = ResolveConditionByName(scriptName); resolved = true; }
                    catch { resolved = false; }
                }
                if (!resolved && !string.IsNullOrEmpty(memberMethod))
                {
                    try
                    {
                        cond = ResolveConditionByName(memberMethod);
                        resolved = true;
                        if (memberReceiver != null) { args.Insert(0, memberReceiver); receiverInjected = true; }
                    }
                    catch { resolved = false; }
                }
                // Member-access call (`current_object.is_of_type(monitor)`)
                // resolved via the tail name (`is_of_type`) — the receiver
                // (`current_object`) must be injected as arg[0] because
                // most condition schemas have the receiver as their first
                // (or near-first) param. Mirror ProcessInvocation's
                // member-receiver injection so Conditions encode arg-shifted
                // identically to Actions.
                if (resolved && !receiverInjected && !string.IsNullOrEmpty(memberReceiver))
                {
                    args.Insert(0, memberReceiver);
                    receiverInjected = true;
                }

                if (!resolved)
                {
                    Debug.WriteLine($"Condition '{scriptName}' not found — emitting placeholder.");
                    EmitPlaceholderCondition(scriptName ?? "<unknown>", actionOffset, orSequence, isNot);
                    conditionOffset++;
                    conditionCount++;
                    return;
                }

                List<string> paramBits;
                try
                {
                    paramBits = EncodeMegaloConditionParams(cond, args);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Condition '{cond.Name}' param encoding failed ({ex.Message}); emitting placeholder.");
                    EmitPlaceholderCondition($"{cond.Name}: {ex.Message}", actionOffset, orSequence, isNot);
                    conditionOffset++;
                    conditionCount++;
                    return;
                }

                string conditionNumberBinary = ConvertToBinary(cond.Id, 5);
                string notBinary = ConvertToBinary(isNot ? 1 : 0, 1);
                string orSequenceBinary = ConvertToBinary(orSequence, 9);

                // LOCAL action offset in current container (trigger or inline)
                int localActionOffset = GetLocalActionOffset(actionOffset);
                string actionOffsetBinary = ConvertToBinary(localActionOffset, 10);

                string binaryCondition = conditionNumberBinary
                                       + notBinary
                                       + orSequenceBinary
                                       + actionOffsetBinary
                                       + string.Join("", paramBits);

                _conditions.Add(new ConditionObject(cond.Name, new List<string> { binaryCondition }));
                Debug.WriteLine($"Added condition: {scriptName}({binaryCondition}) at conditionOffset={conditionOffset} (base={CurrentActionBase}, global={actionOffset})");

                if (CompilerCondTrace != null)
                {
                    int hdrBits = conditionNumberBinary.Length + notBinary.Length + orSequenceBinary.Length + actionOffsetBinary.Length;
                    int payBits = paramBits.Sum(p => p?.Length ?? 0);
                    var pbDetail = string.Join(",",
                        cond.Params.Select((p, idx) => $"{p.Name}:{p.TypeRef}({(idx < paramBits.Count ? paramBits[idx]?.Length ?? 0 : -1)}b)"));
                    CompilerCondTrace.AppendLine(
                        $"  enc cond #{_conditions.Count - 1} {cond.Name}(id={cond.Id}) hdr={hdrBits}b pay={payBits}b total={hdrBits + payBits}b" +
                        $" args=[{string.Join(", ", args)}] params=[{pbDetail}]");
                }

                conditionOffset++;
                conditionCount++;

                return;
            }

            // Receiver-only condition with no args, e.g. `current_player.is_elite`
            // (rare — usually invoked, but Roslyn may parse it as bare access).
            if (condition is MemberAccessExpressionSyntax bareMa)
            {
                string method = bareMa.Name.Identifier.Text;
                MegaloCondition cond = default;
                bool ok = false;
                try { cond = ResolveConditionByName(method); ok = true; } catch { }
                if (ok)
                {
                    var args = new List<string> { bareMa.Expression.ToString() };
                    List<string> paramBits;
                    try { paramBits = EncodeMegaloConditionParams(cond, args); }
                    catch { paramBits = new List<string>(); ok = false; }

                    if (ok)
                    {
                        string conditionNumberBinary = ConvertToBinary(cond.Id, 5);
                        string notBinary = ConvertToBinary(isNot ? 1 : 0, 1);
                        string orSequenceBinary = ConvertToBinary(orSequence, 9);
                        int localActionOffset = GetLocalActionOffset(actionOffset);
                        string actionOffsetBinary = ConvertToBinary(localActionOffset, 10);
                        string binaryCondition = conditionNumberBinary
                                               + notBinary + orSequenceBinary + actionOffsetBinary
                                               + string.Join("", paramBits);
                        _conditions.Add(new ConditionObject(cond.Name, new List<string> { binaryCondition }));
                        conditionOffset++;
                        conditionCount++;
                        return;
                    }
                }

                EmitPlaceholderCondition(method, actionOffset, orSequence, isNot);
                conditionOffset++;
                conditionCount++;
                return;
            }

            // Identifier-only condition (e.g. a flag variable used as a
            // boolean). Emit a placeholder so counts stay aligned.
            if (condition is IdentifierNameSyntax || condition is ElementAccessExpressionSyntax)
            {
                EmitPlaceholderCondition(condition.ToString(), actionOffset, orSequence, isNot);
                conditionOffset++;
                conditionCount++;
                return;
            }

            Debug.WriteLine($"Unsupported condition type: {condition.Kind()} — emitting placeholder.");
            EmitPlaceholderCondition(condition.ToString(), actionOffset, orSequence, isNot);
            conditionOffset++;
            conditionCount++;
        }

        // Emit a placeholder condition with a 25-bit header (id=0/None,
        // not, orSeq, actionOffset). Keeps condition counts aligned when
        // we can't resolve the script-side condition name/args. Surfaces
        // a Warning diagnostic so callers can see WHICH condition silently
        // degraded (otherwise the encoder reports zero warnings while the
        // bit stream rolls forward as None placeholders, breaking re-
        // decompile alignment).
        private void EmitPlaceholderCondition(string sourceName, int actionOffset, int orSequence, bool isNot)
        {
            string conditionNumberBinary = ConvertToBinary(0, 5); // condition id 0 == "None"
            string notBinary = ConvertToBinary(isNot ? 1 : 0, 1);
            string orSequenceBinary = ConvertToBinary(orSequence, 9);
            int localActionOffset = GetLocalActionOffset(actionOffset);
            string actionOffsetBinary = ConvertToBinary(localActionOffset, 10);
            string binaryCondition = conditionNumberBinary + notBinary + orSequenceBinary + actionOffsetBinary;
            _conditions.Add(new ConditionObject($"None /* {sourceName} */", new List<string> { binaryCondition }));
            _encoderDiagnostics.Add(new CompilerDiagnostic(
                CompilerDiagnosticSeverity.Warning,
                $"Condition '{sourceName}' could not be resolved/encoded — emitted None placeholder. Re-decompile will mis-align here.",
                1, 1));
        }


        private static string ToSigned16Binary(int v)
        {
            int b = v & 0xFFFF; // two's complement
            return Convert.ToString(b, 2).PadLeft(16, '0');
        }

        private string ConvertNumericTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();
            string Tag6(int t) => Convert.ToString(t, 2).PadLeft(6, '0');

            // Allow literals directly: NumericTypeRef(Int16) + 16-bit payload.
            if (int.TryParse(value, out int literal))
            {
                return Tag6((int)NumericTypeRefEnum.Int16) + ToSigned16Binary(literal);
            }

            // No-payload built-in variants (script-option / round-settings).
            // The decompiler renders these as snake_case identifiers; the
            // schema variant names are space-separated. Normalize both.
            // Decompiler also doubles underscores when the source had a
            // space (so "Teams Enabled" → "teams__enabled"). Strip extras.
            string norm = value
                .Replace("__", "_")
                .Replace(" ", "_")
                .ToLowerInvariant();
            var noPayload = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["current_round"]                = 13,
                ["symmetric_mode"]               = 14,
                ["symmetric_mode_writable"]      = 15,
                ["score_to_win"]                 = 16,
                ["fireteams_enabled"]            = 17,
                ["teams_enabled"]                = 18,
                ["round_time_limit"]             = 19,
                ["round_limit"]                  = 20,
                ["perfection_enabled"]           = 21,
                ["early_victory_win_count"]      = 22,
                ["sudden_death_time_limit"]      = 23,
            };
            if (noPayload.TryGetValue(norm, out int npVariant))
                return Tag6(npVariant);

            // script_option[N]: variant 5, 4-bit option index.
            var soMatch = System.Text.RegularExpressions.Regex.Match(value,
                @"^script_option\s*\[\s*(\d+)\s*\]$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (soMatch.Success && int.TryParse(soMatch.Groups[1].Value, out int soIdx))
                return Tag6(5) + Convert.ToString(soIdx & 0xF, 2).PadLeft(4, '0');

            // Global.Number[N]: variant 4, 4-bit index. Accept both
            // PascalCase (`GlobalNumber3`) and snake_case (`globalnumber3`).
            if (value.StartsWith("GlobalNumber", StringComparison.OrdinalIgnoreCase))
            {
                int gnIdx = int.Parse(value.Substring("GlobalNumber".Length));
                if (gnIdx < 0 || gnIdx > 15)
                    throw new ArgumentException($"Invalid GlobalNumber index: {gnIdx}");
                return Tag6(4) + Convert.ToString(gnIdx, 2).PadLeft(4, '0');
            }

            // temp_num_N — decompiler-synthesized name for a scratch-number
            // slot. The Reach decoder reads NumericTypeRef variants 44..63
            // (the "Unlabelled" range) as 6-bit tag + 4-bit extra-index
            // (controlled by ScriptDecompiler._tempNumberExtraBits, default 4).
            // Mirror that: tag=44 (canonical Unlabelled), extra=N as 4 bits.
            if (value.StartsWith("temp_num_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value.Substring("temp_num_".Length), out int tempNum))
                return Tag6(44) + Convert.ToString(tempNum & 0xF, 2).PadLeft(4, '0');

            // Player.Number — `current_player.playernumberN` or
            // `globalplayerM.playernumberN`. Variant 1: PlayerRef (5b) +
            // slot (3b).
            //
            // Object.Number — `current_object.objectnumberN`. Variant 2:
            // ObjectRef (5b) + slot (3b).
            //
            // Team.Number — `current_team.teamnumberN`. Variant 3:
            // TeamRef (3b) + slot (3b).
            int firstDot = value.IndexOf('.');
            if (firstDot > 0)
            {
                string lhs = value.Substring(0, firstDot).Trim();
                string rhs = value.Substring(firstDot + 1).Trim();

                if (rhs.StartsWith("playernumber", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("playernumber".Length), out int pSlot))
                {
                    string playerRef = EncodePlayerRef5(lhs);
                    return Tag6(1) + playerRef + Convert.ToString(pSlot, 2).PadLeft(3, '0');
                }
                if (rhs.StartsWith("objectnumber", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("objectnumber".Length), out int oSlot))
                {
                    string objRef = EncodeObjectRef5(lhs);
                    return Tag6(2) + objRef + Convert.ToString(oSlot, 2).PadLeft(3, '0');
                }
                if (rhs.StartsWith("teamnumber", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("teamnumber".Length), out int tSlot))
                {
                    string teamRef = EncodeTeamRef3(lhs);
                    return Tag6(3) + teamRef + Convert.ToString(tSlot, 2).PadLeft(3, '0');
                }

                // Player.Score / Player.Money / Team.Score variants.
                if (rhs.Equals("score", StringComparison.OrdinalIgnoreCase))
                {
                    if (lhs.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase) || lhs.Equals("current_player", StringComparison.OrdinalIgnoreCase))
                        return Tag6(8) + EncodePlayerRef5(lhs);
                    return Tag6(7) + EncodeTeamRef3(lhs);
                }
                if (rhs.Equals("money", StringComparison.OrdinalIgnoreCase))
                    return Tag6(9) + EncodePlayerRef5(lhs);
                if (rhs.Equals("rating", StringComparison.OrdinalIgnoreCase))
                    return Tag6(10) + EncodePlayerRef5(lhs);

                // Player.Stat (variant 11) — `<player>.playerstats[N]`
                //   tag(6) + PlayerRef(5b) + Statistic(2b)
                // Team.Stat  (variant 12) — `<team>.playerstats[N]`
                //   tag(6) + TeamRef(3b) + Statistic(2b)
                var psM = System.Text.RegularExpressions.Regex.Match(rhs,
                    @"^playerstats\s*\[\s*(\d+)\s*\]$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (psM.Success && int.TryParse(psM.Groups[1].Value, out int psIdx))
                {
                    if (lhs.Equals("current_team", StringComparison.OrdinalIgnoreCase)
                        || lhs.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase))
                        return Tag6(12) + EncodeTeamRef3(lhs) + Convert.ToString(psIdx & 0x3, 2).PadLeft(2, '0');
                    return Tag6(11) + EncodePlayerRef5(lhs) + Convert.ToString(psIdx & 0x3, 2).PadLeft(2, '0');
                }
            }

            // NoNumber / Int16 token: emit Int16 variant + 16-bit zero.
            if (value.Equals("Int16", StringComparison.OrdinalIgnoreCase)
                || value.Equals("NoNumber", StringComparison.OrdinalIgnoreCase)
                || value.Equals("no_number", StringComparison.OrdinalIgnoreCase))
            {
                return Tag6((int)NumericTypeRefEnum.Int16) + ToSigned16Binary(0);
            }

            throw new ArgumentException($"Unsupported NumericTypeRef: {value}");
        }

        // Shared helpers used by NumericTypeRef and ObjectTypeRef encoders.
        private static string EncodePlayerRef5(string r)
        {
            // PlayerRef variants (5b): 0=NoPlayer, 1..16=Player0..Player15,
            // 17..24=GlobalPlayer[0..7], 25=CurrentPlayer, 26=HudPlayer,
            // 27=HudTargetPlayer, 28=ObjectKiller, 29..31=Unlabelled.
            if (Enum.TryParse(typeof(PlayerRefEnum), r, true, out var pe) && pe != null)
                return Convert.ToString(Convert.ToInt32(pe), 2).PadLeft(5, '0');
            if (r.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("globalplayer".Length), out int gp))
                return Convert.ToString(17 + (gp & 0x7), 2).PadLeft(5, '0');
            if (r.StartsWith("GlobalPlayer", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("GlobalPlayer".Length), out int gP))
                return Convert.ToString(17 + (gP & 0x7), 2).PadLeft(5, '0');
            if (r.Equals("current_player", StringComparison.OrdinalIgnoreCase))
                return Convert.ToString((int)PlayerRefEnum.CurrentPlayer, 2).PadLeft(5, '0');
            if (r.StartsWith("temp_player_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("temp_player_".Length), out int tp))
                return Convert.ToString(29 + (tp & 0x3), 2).PadLeft(5, '0');
            return "00000";
        }
        private static string EncodeObjectRef5(string r)
        {
            if (r.Equals("current_object", StringComparison.OrdinalIgnoreCase))
                return Convert.ToString((int)ObjectRef.CurrentObject, 2).PadLeft(5, '0');
            if (r.StartsWith("GlobalObject", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("GlobalObject".Length), out int gi))
                return Convert.ToString(gi + 1, 2).PadLeft(5, '0');
            if (r.StartsWith("globalobject", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("globalobject".Length), out int gI))
                return Convert.ToString(gI + 1, 2).PadLeft(5, '0');
            if (r.StartsWith("temp_obj_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("temp_obj_".Length), out int to))
                return Convert.ToString(22 + (to & 0x7), 2).PadLeft(5, '0');
            return "00000";
        }
        private static string EncodeTeamRef3(string r)
        {
            if (r.Equals("current_team", StringComparison.OrdinalIgnoreCase)) return "000";
            if (r.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("globalteam".Length), out int gt))
                return Convert.ToString(gt + 1, 2).PadLeft(3, '0');
            if (r.StartsWith("GlobalTeam", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(r.Substring("GlobalTeam".Length), out int gT))
                return Convert.ToString(gT + 1, 2).PadLeft(3, '0');
            return "000";
        }

        private string ConvertPlayerTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // GlobalPlayer[N] maps to PlayerRef variants 17..24 — see
            // MegaloSchema PlayerRef table. (Variants 1..16 are Player0..Player15
            // session slots; 0=NoPlayer; 25=CurrentPlayer.)
            if (value.StartsWith("GlobalPlayer", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Substring("GlobalPlayer".Length));
                if (idx < 0 || idx > 7) throw new ArgumentException($"Invalid GlobalPlayer index: {idx}");

                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString(17 + idx, 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }

            if (value.Equals("NoPlayer", StringComparison.OrdinalIgnoreCase)
                || value.Equals("no_player", StringComparison.OrdinalIgnoreCase))
            {
                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString(0, 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }

            // temp_player_N — decompiler-synthesized scratch player slot.
            // Decompiler renders PlayerRef variants 29..31 (the "Unlabelled"
            // range) as `temp_player_{tag-29}`. Mirror by emitting
            // PlayerTypeRef variant 0 (Player) + PlayerRef tag = 29+N.
            if (value.StartsWith("temp_player_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value.Substring("temp_player_".Length), out int tempPl))
            {
                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString(29 + (tempPl & 0x3), 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }

            // Existing support: current_player
            if (value.Equals("current_player", StringComparison.OrdinalIgnoreCase))
            {
                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString((int)PlayerRefEnum.CurrentPlayer, 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }

            // Dotted: receiver.member — PlayerTypeRef variants 1/2/3.
            //   <player>.playerplayerN  → variant 1 Player.Player (5b PlayerRef + 2b slot)
            //   <object>.objectplayerN  → variant 2 Object.Player  (5b ObjectRef + 2b slot)
            //   <team>.teamplayerN      → variant 3 Team.Player    (3b TeamRef  + 2b slot)
            int dot = value.IndexOf('.');
            if (dot > 0)
            {
                string lhs = value.Substring(0, dot).Trim();
                string rhs = value.Substring(dot + 1).Trim();
                if (rhs.StartsWith("playerplayer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("playerplayer".Length), out int sPP))
                    return "01" + EncodePlayerRef5(lhs) + Convert.ToString(sPP, 2).PadLeft(2, '0');
                if (rhs.StartsWith("objectplayer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("objectplayer".Length), out int sOP))
                    return "10" + EncodeObjectRef5(lhs) + Convert.ToString(sOP, 2).PadLeft(2, '0');
                if (rhs.StartsWith("teamplayer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("teamplayer".Length), out int sTP))
                    return "11" + EncodeTeamRef3(lhs) + Convert.ToString(sTP, 2).PadLeft(2, '0');
            }

            // Try parsing as an enum directly (if you have names like "SomePlayerRef")
            try
            {
                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                int refVal = (int)Enum.Parse(typeof(PlayerRefEnum), value, ignoreCase: true);
                string refBits = Convert.ToString(refVal, 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }
            catch
            {
                throw new ArgumentException($"Unsupported PlayerTypeRef: {value}");
            }
        }

        private string ConvertTeamTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // Fallback encoding: [2-bit type][5-bit ref]
            // If your real schema differs, adjust this and/or wire into your Enums.Enums definitions.
            int typeVal = 0;
            int refVal = 0;

            // TeamRef variants (5b): 0=NoTeam, 1..8=Team0..Team7,
            // 9=NeutralTeam, 10..17=GlobalTeam[0..7], 18=CurrentTeam,
            // 23..28=Unlabelled (temp_team_0..5).
            if (value.StartsWith("GlobalTeam", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Substring("GlobalTeam".Length));
                refVal = 10 + (idx & 0x7);
            }
            else if (value.Equals("current_team", StringComparison.OrdinalIgnoreCase))
            {
                refVal = 18;
            }
            else if (value.Equals("neutral_team", StringComparison.OrdinalIgnoreCase)
                  || value.Equals("NeutralTeam", StringComparison.OrdinalIgnoreCase))
            {
                refVal = 9;
            }
            else if (value.Equals("NoTeam", StringComparison.OrdinalIgnoreCase)
                     || value.Equals("no_team", StringComparison.OrdinalIgnoreCase))
            {
                refVal = 0;
            }
            // temp_team_N — decompiler-synthesized scratch team slot.
            // Decompiler renders TeamRef variants 23..28 as `temp_team_{tag-23}`.
            else if (value.StartsWith("temp_team_", StringComparison.OrdinalIgnoreCase)
                     && int.TryParse(value.Substring("temp_team_".Length), out int tempTm))
            {
                refVal = 23 + (tempTm & 0x7);
            }
            else if (value.StartsWith("Team", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Replace("Team", ""), out int t))
            {
                refVal = 1 + (t & 0x7);
            }
            else
            {
                // Try parsing against a TeamRefEnum if present; otherwise 0
                try
                {
                    refVal = (int)Enum.Parse(typeof(TeamRef), value, ignoreCase: true);
                }
                catch { refVal = 0; }
            }

            // TeamTypeRef per MegaloSchema: 3-bit variant tag + payload.
            string typeBits = ConvertToBinary(typeVal, 3);
            string refBits = ConvertToBinary(refVal, 5);
            string final = typeBits + refBits;
            return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
        }

        private string ConvertTimerTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // TimerTypeRef per MegaloSchema: 3-bit variant tag + payload.
            //   0=Timer            : 3-bit globaltimer slot
            //   1=Player.Timer     : PlayerRef(5) + 2-bit playertimer slot
            //   2=Team.Timer       : TeamRef(3) + 2-bit teamtimer slot
            //   3=Object.Timer     : ObjectRef(5) + 2-bit objecttimer slot
            //   4=GameRoundTimer   : (no payload)
            //   5=SuddenDeathTimer : (no payload)
            //   6=OvertimeTimer    : (no payload)
            //   7=Unlabelled       : (no payload)
            string Tag3(int t) => Convert.ToString(t, 2).PadLeft(3, '0');

            // Dotted forms: <ref>.playertimerN / .teamtimerN / .objecttimerN
            int firstDot = value.IndexOf('.');
            if (firstDot > 0)
            {
                string lhs = value.Substring(0, firstDot).Trim();
                string rhs = value.Substring(firstDot + 1).Trim();
                if (rhs.StartsWith("playertimer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("playertimer".Length), out int pSlot))
                {
                    string playerRef = EncodePlayerRef5(lhs);
                    string final = Tag3(1) + playerRef + Convert.ToString(pSlot & 0x3, 2).PadLeft(2, '0');
                    return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
                }
                if (rhs.StartsWith("teamtimer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("teamtimer".Length), out int tSlot))
                {
                    string teamRef = EncodeTeamRef3(lhs);
                    string final = Tag3(2) + teamRef + Convert.ToString(tSlot & 0x3, 2).PadLeft(2, '0');
                    return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
                }
                if (rhs.StartsWith("objecttimer", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("objecttimer".Length), out int oSlot))
                {
                    string objRef = EncodeObjectRef5(lhs);
                    string final = Tag3(3) + objRef + Convert.ToString(oSlot & 0x3, 2).PadLeft(2, '0');
                    return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
                }
            }

            // No-payload built-in variants.
            string norm = value.Replace("__", "_").Replace(" ", "_").ToLowerInvariant();
            switch (norm)
            {
                case "round_timer":
                case "game_round_timer":
                case "gameroundtimer":
                    return Tag3(4);
                case "sudden_death_timer":
                case "suddendeathtimer":
                    return Tag3(5);
                case "overtime_timer":
                case "overtimetimer":
                    return Tag3(6);
            }

            // Variant 0 (Timer): 3-bit globaltimer slot.
            int refVal = 0;
            if (value.StartsWith("GlobalTimer", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Substring("GlobalTimer".Length));
                refVal = idx + 1; // reserve 0 for NoTimer
            }
            else if (value.StartsWith("globaltimer", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Substring("globaltimer".Length));
                refVal = idx + 1;
            }
            else if (!value.Equals("NoTimer", StringComparison.OrdinalIgnoreCase)
                  && int.TryParse(value, out int n))
            {
                refVal = n;
            }

            string finalDefault = Tag3(0) + Convert.ToString(refVal & 0x7, 2).PadLeft(3, '0');
            return bitSize > 0 ? finalDefault.PadLeft(bitSize, '0') : finalDefault;
        }

        private string ConvertVarTypeToBinary(string token, int bitSize)
        {
            token = (token ?? string.Empty).Trim();

            // VarType header is 3 bits in your existing Megl.If encoding.
            // 000 = numeric, 001 = player, 010 = object, 011 = team, 100 = timer (best-effort).
            string kindBits;
            string payloadBits;

            if (int.TryParse(token, out int literal))
            {
                kindBits = ConvertToBinary(0, 3);
                payloadBits = ConvertNumericTypeRefToBinary(literal.ToString(), 0);
            }
            else if (_variableToIndexMap.TryGetValue(token, out var info))
            {
                if (info.Type.Equals("Number", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(0, 3);
                    payloadBits = ConvertNumericTypeRefToBinary($"GlobalNumber{info.Index}", 0);
                }
                else if (info.Type.Equals("Object", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(2, 3);
                    payloadBits = ConvertObjectTypeRefToBinary($"GlobalObject{info.Index}", 0, 1);
                }
                else if (info.Type.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(1, 3);
                    payloadBits = ConvertPlayerTypeRefToBinary($"GlobalPlayer{info.Index}", 0);
                }
                else if (info.Type.Equals("Team", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(3, 3);
                    payloadBits = ConvertTeamTypeRefToBinary($"GlobalTeam{info.Index}", 0);
                }
                else if (info.Type.Equals("Timer", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(4, 3);
                    payloadBits = ConvertTimerTypeRefToBinary($"GlobalTimer{info.Index}", 0);
                }
                else
                {
                    kindBits = ConvertToBinary(0, 3);
                    payloadBits = ConvertNumericTypeRefToBinary("0", 0);
                }
            }
            else
            {
                // Dotted member access: route by trailing member-name.
                int dot = token.IndexOf('.');
                if (dot > 0)
                {
                    string rhs = token.Substring(dot + 1);
                    if (rhs.StartsWith("playernumber", StringComparison.OrdinalIgnoreCase)
                        || rhs.StartsWith("objectnumber", StringComparison.OrdinalIgnoreCase)
                        || rhs.StartsWith("teamnumber", StringComparison.OrdinalIgnoreCase)
                        || rhs.Equals("score", StringComparison.OrdinalIgnoreCase)
                        || rhs.Equals("money", StringComparison.OrdinalIgnoreCase)
                        || rhs.Equals("rating", StringComparison.OrdinalIgnoreCase)
                        || System.Text.RegularExpressions.Regex.IsMatch(rhs,
                            @"^playerstats\s*\[\s*\d+\s*\]$",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        kindBits = ConvertToBinary(0, 3);
                        payloadBits = ConvertNumericTypeRefToBinary(token, 0);
                    }
                    else if (rhs.StartsWith("playerplayer", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("objectplayer", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("teamplayer", StringComparison.OrdinalIgnoreCase))
                    {
                        kindBits = ConvertToBinary(1, 3);
                        payloadBits = ConvertPlayerTypeRefToBinary(token, 0);
                    }
                    else if (rhs.StartsWith("playerteam", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("objectteam", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("teamteam", StringComparison.OrdinalIgnoreCase))
                    {
                        kindBits = ConvertToBinary(3, 3);
                        payloadBits = ConvertTeamTypeRefToBinary(token, 0);
                    }
                    else if (rhs.StartsWith("playertimer", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("objecttimer", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("teamtimer", StringComparison.OrdinalIgnoreCase))
                    {
                        kindBits = ConvertToBinary(4, 3);
                        payloadBits = ConvertTimerTypeRefToBinary(token, 0);
                    }
                    else if (rhs.StartsWith("playerobject", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("objectobject", StringComparison.OrdinalIgnoreCase)
                          || rhs.StartsWith("teamobject", StringComparison.OrdinalIgnoreCase)
                          || rhs.Equals("biped", StringComparison.OrdinalIgnoreCase))
                    {
                        kindBits = ConvertToBinary(2, 3);
                        payloadBits = ConvertObjectTypeRefToBinary(token, 0, 1);
                    }
                    else
                    {
                        kindBits = ConvertToBinary(0, 3);
                        payloadBits = ConvertNumericTypeRefToBinary("0", 0);
                    }
                }
                // temp_X_N synthesized identifiers — route by prefix.
                else if (token.StartsWith("temp_num_", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(0, 3);
                    payloadBits = ConvertNumericTypeRefToBinary(token, 0);
                }
                else if (token.StartsWith("temp_obj_", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(2, 3);
                    payloadBits = ConvertObjectTypeRefToBinary(token, 0, 1);
                }
                else if (token.StartsWith("temp_player_", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(1, 3);
                    payloadBits = ConvertPlayerTypeRefToBinary(token, 0);
                }
                else if (token.StartsWith("temp_team_", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(3, 3);
                    payloadBits = ConvertTeamTypeRefToBinary(token, 0);
                }
                // Bare built-in identifiers.
                else if (token.Equals("current_player", StringComparison.OrdinalIgnoreCase)
                      || token.Equals("no_player", StringComparison.OrdinalIgnoreCase)
                      || token.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(1, 3);
                    payloadBits = ConvertPlayerTypeRefToBinary(token, 0);
                }
                else if (token.Equals("current_object", StringComparison.OrdinalIgnoreCase)
                      || token.Equals("no_object", StringComparison.OrdinalIgnoreCase)
                      || token.StartsWith("globalobject", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(2, 3);
                    payloadBits = ConvertObjectTypeRefToBinary(token, 0, 1);
                }
                else if (token.Equals("current_team", StringComparison.OrdinalIgnoreCase)
                      || token.Equals("no_team", StringComparison.OrdinalIgnoreCase)
                      || token.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(3, 3);
                    payloadBits = ConvertTeamTypeRefToBinary(token, 0);
                }
                else if (token.Equals("no_timer", StringComparison.OrdinalIgnoreCase)
                      || token.StartsWith("globaltimer", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(4, 3);
                    payloadBits = ConvertTimerTypeRefToBinary(token, 0);
                }
                else
                {
                    // Try to encode as a NumericTypeRef built-in / script_option.
                    try
                    {
                        kindBits = ConvertToBinary(0, 3);
                        payloadBits = ConvertNumericTypeRefToBinary(token, 0);
                    }
                    catch
                    {
                        kindBits = ConvertToBinary(0, 3);
                        payloadBits = ConvertNumericTypeRefToBinary("0", 0);
                    }
                }
            }

            string final = kindBits + payloadBits;
            return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
        }

        private string ConvertObjectTypeRefToBinary(string value, int bitSize, int locality)
        {
            // ObjectTypeRef per MegaloSchema (3-bit tag + variant payload):
            //   0=ObjectRef           : ObjectRef (5b)
            //   1=Player.Object       : PlayerRef (5b) + slot (2b)
            //   2=Object.Object       : ObjectRef (5b) + slot (2b)
            //   3=Team.Object         : TeamRef  (3b) + slot (3b)
            //   4=Player.Biped        : PlayerRef (5b)
            //   5=Player.Player.Biped : PlayerRef (5b) + slot (2b)
            //   6=Object.Player.Biped : ObjectRef (5b) + slot (2b)
            //   7=Team.Player.Biped   : TeamRef  (3b) + slot (2b)
            //
            // We accept the script forms the decompiler emits.
            string v = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(v)) v = "NoObject";

            // Patterns:
            //   "GlobalObjectN" or just "globalobjectN" → variant 0, ObjectRef slot = N+1 (NoObject is slot 0)
            //   "current_object"                       → variant 0, ObjectRef = CurrentObject
            //   "NoObject"                             → variant 0, ObjectRef = NoObject (slot 0)
            //   "current_player.biped"                 → variant 4, PlayerRef = CurrentPlayer
            //   "current_player.playerobjectN"         → variant 1, PlayerRef = CurrentPlayer, slot = N
            //   "current_player.playerplayerN.biped"   → variant 5, PlayerRef = CurrentPlayer, slot = N
            //   "current_object.objectobjectN"         → variant 2, ObjectRef = CurrentObject, slot = N
            //   "playerobjectN" / "objectobjectN" / "teamobjectN" — scope-implicit form
            //                                          → bare slot — fall back to current_player/current_object/current_team
            string Tag(int t) => Convert.ToString(t, 2).PadLeft(3, '0');
            string PlayerRef5(string r)
            {
                // PlayerRef variants (5b): 0=NoPlayer, 1..16=Player0..Player15,
                // 17..24=GlobalPlayer[0..7], 25=CurrentPlayer, 26..28 (HUD/killer),
                // 29..31 = Unlabelled (temp_player_0..2).
                if (Enum.TryParse(typeof(PlayerRefEnum), r, true, out var pe) && pe != null)
                    return Convert.ToString(Convert.ToInt32(pe), 2).PadLeft(5, '0');
                if (r.StartsWith("globalplayer", StringComparison.OrdinalIgnoreCase) && int.TryParse(r.Substring("globalplayer".Length), out int gp))
                    return Convert.ToString(17 + (gp & 0x7), 2).PadLeft(5, '0');
                if (r.Equals("current_player", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToString((int)PlayerRefEnum.CurrentPlayer, 2).PadLeft(5, '0');
                if (r.StartsWith("temp_player_", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(r.Substring("temp_player_".Length), out int tp))
                    return Convert.ToString(29 + (tp & 0x3), 2).PadLeft(5, '0');
                return "00000";
            }
            string ObjRef5(string r)
            {
                if (r.Equals("current_object", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToString((int)ObjectRef.CurrentObject, 2).PadLeft(5, '0');
                if (r.StartsWith("GlobalObject", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(r.Substring("GlobalObject".Length), out int gi);
                    return Convert.ToString(gi + 1, 2).PadLeft(5, '0');
                }
                if (r.StartsWith("globalobject", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(r.Substring("globalobject".Length), out int gi);
                    return Convert.ToString(gi + 1, 2).PadLeft(5, '0');
                }
                if (r.StartsWith("temp_obj_", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(r.Substring("temp_obj_".Length), out int to))
                    return Convert.ToString(22 + (to & 0x7), 2).PadLeft(5, '0'); // Unlabelled range 22-29
                if (r.Equals("NoObject", StringComparison.OrdinalIgnoreCase)) return "00000";
                return "00000";
            }
            string TeamRef3(string r)
            {
                if (r.Equals("current_team", StringComparison.OrdinalIgnoreCase)) return "000";
                if (r.StartsWith("globalteam", StringComparison.OrdinalIgnoreCase) && int.TryParse(r.Substring("globalteam".Length), out int gt))
                    return Convert.ToString(gt + 1, 2).PadLeft(3, '0');
                if (r.StartsWith("temp_team_", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(r.Substring("temp_team_".Length), out int tt))
                    return Convert.ToString(tt & 0x7, 2).PadLeft(3, '0'); // best-effort: low 3 bits
                return "000";
            }

            // Dotted: receiver.member[.suffix]?
            int firstDot = v.IndexOf('.');
            if (firstDot >= 0)
            {
                string lhs = v.Substring(0, firstDot).Trim();
                string rhs = v.Substring(firstDot + 1).Trim();

                // current_player.biped (variant 4)
                if (rhs.Equals("biped", StringComparison.OrdinalIgnoreCase))
                {
                    return Tag(4) + PlayerRef5(lhs);
                }

                // playerN.playerN.biped (variant 5)
                int dot2 = rhs.IndexOf('.');
                if (dot2 > 0 && rhs.Substring(dot2 + 1).Trim().Equals("biped", StringComparison.OrdinalIgnoreCase))
                {
                    string inner = rhs.Substring(0, dot2).Trim();
                    if (inner.StartsWith("playerplayer", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(inner.Substring("playerplayer".Length), out int slotPP))
                    {
                        return Tag(5) + PlayerRef5(lhs) + Convert.ToString(slotPP, 2).PadLeft(2, '0');
                    }
                    if (inner.StartsWith("objectplayer", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(inner.Substring("objectplayer".Length), out int slotOP))
                    {
                        return Tag(6) + ObjRef5(lhs) + Convert.ToString(slotOP, 2).PadLeft(2, '0');
                    }
                    if (inner.StartsWith("teamplayer", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(inner.Substring("teamplayer".Length), out int slotTP))
                    {
                        return Tag(7) + TeamRef3(lhs) + Convert.ToString(slotTP, 2).PadLeft(2, '0');
                    }
                }

                // current_player.playerobjectN (variant 1)
                if (rhs.StartsWith("playerobject", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("playerobject".Length), out int slotPO))
                {
                    return Tag(1) + PlayerRef5(lhs) + Convert.ToString(slotPO, 2).PadLeft(2, '0');
                }
                // current_object.objectobjectN (variant 2)
                if (rhs.StartsWith("objectobject", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("objectobject".Length), out int slotOO))
                {
                    return Tag(2) + ObjRef5(lhs) + Convert.ToString(slotOO, 2).PadLeft(2, '0');
                }
                // current_team.teamobjectN (variant 3)
                if (rhs.StartsWith("teamobject", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(rhs.Substring("teamobject".Length), out int slotTO))
                {
                    return Tag(3) + TeamRef3(lhs) + Convert.ToString(slotTO, 2).PadLeft(3, '0');
                }
            }

            // Bare names — accept both Pascal and snake_case forms.
            if (v.Equals("NoObject", StringComparison.OrdinalIgnoreCase)
                || v.Equals("no_object", StringComparison.OrdinalIgnoreCase))
                return Tag(0) + "00000";
            if (v.Equals("current_object", StringComparison.OrdinalIgnoreCase)
                || v.Equals("CurrentObject", StringComparison.OrdinalIgnoreCase))
                return Tag(0) + Convert.ToString((int)ObjectRef.CurrentObject, 2).PadLeft(5, '0');

            // temp_obj_N — decompiler-synthesized name for a scratch object
            // slot. Decompiler renders ObjectRef variants 22..29 (the
            // "Unlabelled" range) as `temp_obj_{tag-22}`. Mirror by emitting
            // ObjectTypeRef variant 0 (ObjectRef) + ObjectRef tag = 22+N.
            if (v.StartsWith("temp_obj_", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(v.Substring("temp_obj_".Length), out int tempObj))
                return Tag(0) + Convert.ToString(22 + (tempObj & 0x7), 2).PadLeft(5, '0');

            // GlobalObjectN — variant 0 (ObjectRef + slot)
            if (v.StartsWith("GlobalObject", StringComparison.OrdinalIgnoreCase))
                return Tag(0) + ObjRef5(v);
            if (v.StartsWith("globalobject", StringComparison.OrdinalIgnoreCase))
                return Tag(0) + ObjRef5(v);

            // Scope-implicit shorthand the decompiler sometimes emits
            // when the receiver is current_*: `playerobjectN` etc.
            if (v.StartsWith("playerobject", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(v.Substring("playerobject".Length), out int implicitPO))
                return Tag(1) + PlayerRef5("current_player") + Convert.ToString(implicitPO, 2).PadLeft(2, '0');
            if (v.StartsWith("objectobject", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(v.Substring("objectobject".Length), out int implicitOO))
                return Tag(2) + ObjRef5("current_object") + Convert.ToString(implicitOO, 2).PadLeft(2, '0');
            if (v.StartsWith("teamobject", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(v.Substring("teamobject".Length), out int implicitTO))
                return Tag(3) + TeamRef3("current_team") + Convert.ToString(implicitTO, 2).PadLeft(3, '0');

            throw new ArgumentException($"Unsupported ObjectTypeRef: {value}");
        }


        private string ConvertToBinary(object value, int bitSize)
        {
            if (value is Enum e)
            {
                // Convert any enum (byte/short/int/etc) safely to an int
                int n = Convert.ToInt32(e);
                return Convert.ToString(n, 2).PadLeft(bitSize, '0');
            }

            if (value is int intValue)
                return Convert.ToString(intValue, 2).PadLeft(bitSize, '0');

            if (value is bool boolValue)
                return (boolValue ? "1" : "0").PadLeft(bitSize, '0');

            if (value is string strValue)
            {
                if (Enum.TryParse(typeof(NameIndex), strValue, true, out var enumValue) && enumValue != null)
                    return Convert.ToString(Convert.ToInt32(enumValue), 2).PadLeft(bitSize, '0');

                return string.Join("", strValue.Select(c => Convert.ToString(c, 2).PadLeft(8, '0')));
            }

            if (value is ObjectRef objectRefValue)
                return Convert.ToString((int)objectRefValue, 2).PadLeft(bitSize, '0');

            if (value is ObjectType objectTypeValue)
                return Convert.ToString((int)objectTypeValue, 2).PadLeft(bitSize, '0');

            throw new InvalidOperationException($"Unsupported type for binary conversion: {value?.GetType().FullName}");
        }




        private void EndScope()
        {
            if (_scopeStack.Count > 0)
            {
                var scope = _scopeStack.Pop();
                foreach (var kvp in scope)
                {
                    if (_variableToIndexMap.ContainsKey(kvp.Key))
                    {
                        var variableInfo = _variableToIndexMap[kvp.Key];
                        if (variableInfo.Type == "Player" && variableInfo.Index < _entityManager.GetPlayerIndex(new Player("")))
                        {
                            _entityManager.RemovePlayer(variableInfo.Index);
                        }
                        else if (variableInfo.Type == "Object" && variableInfo.Index < _entityManager.GetObjectIndex(new GameObject("")))
                        {
                            _entityManager.RemoveObject(variableInfo.Index);
                        }
                        _variableToIndexMap.Remove(kvp.Key);
                    }
                }
            }
        }



    }



    public enum ObjectRef
    {
        NoObject = 0b00000,
        GlobalObject0 = 0b00001,
        GlobalObject1 = 0b00010,
        GlobalObject2 = 0b00011,
        GlobalObject3 = 0b00100,
        GlobalObject4 = 0b00101,
        GlobalObject5 = 0b00110,
        GlobalObject6 = 0b00111,
        GlobalObject7 = 0b01000,
        GlobalObject8 = 0b01001,
        GlobalObject9 = 0b01010,
        GlobalObject10 = 0b01011,
        GlobalObject11 = 0b01100,
        GlobalObject12 = 0b01101,
        GlobalObject13 = 0b01110,
        GlobalObject14 = 0b01111,
        GlobalObject15 = 0b10000,
        CurrentObject = 0b10001,
        TargetObject = 0b10010,
        KilledObject = 0b10011,
        KillerObject = 0b10100,
        Unlabelled1 = 0b10101,
        Unlabelled2 = 0b10110,
        Unlabelled3 = 0b10111,
        Unlabelled4 = 0b11000,
        Unlabelled5 = 0b11001,
        Unlabelled6 = 0b11010,
        Unlabelled7 = 0b11011,
        Unlabelled8 = 0b11100,
        Unlabelled9 = 0b11101,
        Unlabelled10 = 0b11110,
        Unlabelled11 = 0b11111
    }

    public enum TeamRef
    {
        NoTeam = 0b00000,
        Team0 = 0b00001,
        Team1 = 0b00010,
        Team2 = 0b00011,
        Team3 = 0b00100,
        Team4 = 0b00101,
        Team5 = 0b00110,
        Team6 = 0b00111,
        Team7 = 0b01000,
        NeutralTeam = 0b01001,
        GlobalTeam0 = 0b01010,
        GlobalTeam1 = 0b01011,
        GlobalTeam2 = 0b01100,
        GlobalTeam3 = 0b01101,
        GlobalTeam4 = 0b01110,
        GlobalTeam5 = 0b01111,
        GlobalTeam6 = 0b10000,
        GlobalTeam7 = 0b10001,
        CurrentTeam = 0b10010,
        HudPlayerTeam = 0b10011,
        HudTargetTeam = 0b10100,
        UnkTeam21 = 0b10101,
        UnkTeam22 = 0b10110,
        Unlabelled1 = 0b10111,
        Unlabelled2 = 0b11000,
        Unlabelled3 = 0b11001,
        Unlabelled4 = 0b11010,
        Unlabelled5 = 0b11011,
        Unlabelled6 = 0b11100,
        Unlabelled7 = 0b11101,
        Unlabelled8 = 0b11110,
        Unlabelled9 = 0b11111
    }

    public class ObjectTypeRef
    {
        public string Name { get; set; }
        public int Bits { get; set; }
        public List<ObjectTypeRefParameter> Parameters { get; set; }

        public ObjectTypeRef(string name, int bits, List<ObjectTypeRefParameter> parameters)
        {
            Name = name;
            Bits = bits;
            Parameters = parameters;
        }
    }

    public class NumericTypeRef
    {
        public string Name { get; set; }
        public int Bits { get; set; }
        public List<NumericTypeRefParameter> Parameters { get; set; }

        public NumericTypeRef(string name, int bits, List<NumericTypeRefParameter> parameters)
        {
            Name = name;
            Bits = bits;
            Parameters = parameters;
        }
    }

    public class NumericTypeRefParameter
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public int Bits { get; set; }

        public NumericTypeRefParameter(string name, Type parameterType, int bits)
        {
            Name = name;
            ParameterType = parameterType;
            Bits = bits;
        }
    }


    public class PlayerTypeRef
    {
        public string Name { get; set; }
        public int Bits { get; set; }
        public List<PlayerTypeRefParameter> Parameters { get; set; } = new List<PlayerTypeRefParameter>();

        public PlayerTypeRef(string name, int bits)
        {
            Name = name;
            Bits = bits;
        }
    }

    public class PlayerTypeRefParameter
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public int Bits { get; set; }

        public PlayerTypeRefParameter(string name, Type parameterType, int bits)
        {
            Name = name;
            ParameterType = parameterType;
            Bits = bits;
        }
    }

    public class ObjectTypeRefParameter
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public int Bits { get; set; }

        public ObjectTypeRefParameter(string name, Type parameterType, int bits)
        {
            Name = name;
            ParameterType = parameterType;
            Bits = bits;
        }
    }


    public class BitEncoder
    {
        // Function to get binary representation for "current_player.biped"
        public static string EncodeCurrentPlayerBiped()
        {
            // Step 1: Get the ObjectTypeRef value for Player.Biped
            int objectTypeBits = (int)ObjectTypeRefEnum.PlayerBiped; // 0b100

            // Step 2: Get the PlayerRef value for CurrentPlayer
            int playerRefBits = (int)PlayerRefEnum.CurrentPlayer; // 0b11001

            // Step 3: Combine the binary values into a final representation
            string objectTypeBitsStr = Convert.ToString(objectTypeBits, 2).PadLeft(3, '0');
            string playerRefBitsStr = Convert.ToString(playerRefBits, 2).PadLeft(5, '0');

            // Concatenate to form the final binary string
            string finalBinaryString = objectTypeBitsStr + playerRefBitsStr;

            return finalBinaryString;
        }

        // General method to encode object type references
        public static string EncodeObjectTypeRef(string objectTypeRef)
        {
            // Add logic to handle different object type references
            if (objectTypeRef == "current_player.biped")
            {
                return EncodeCurrentPlayerBiped();
            }

            // Add more cases as needed
            throw new ArgumentException($"Unsupported object type reference: {objectTypeRef}");
        }
    }


    public class ActionObject
    {
        public string ActionType { get; set; }
        public List<string> Parameters { get; set; }

        public ActionObject(string actionType, List<string> parameters)
        {
            ActionType = actionType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return $"{ActionType}({string.Join(", ", Parameters)})";
        }
    }

    public class ConditionObject
    {
        public string ConditionType { get; set; }
        public List<string> Parameters { get; set; }

        public ConditionObject(string conditionType, List<string> parameters)
        {
            ConditionType = conditionType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return $"{ConditionType}({string.Join(", ", Parameters)})";
        }
    }


    public class GameObject
    {
        public string Name { get; set; }

        public GameObject(string name)
        {
            Name = name;
        }
    }
    public class TriggerObject
    {
        public string TriggerType { get; set; }
        public List<string> Parameters { get; set; }

        public TriggerObject(string triggerType, List<string> parameters)
        {
            TriggerType = triggerType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return $"{TriggerType}({string.Join(", ", Parameters)})";
        }
    }
    public class VariableInfo
    {
        public string Type { get; set; }
        public int Index { get; set; }
        public int Priority { get; set; }

        public VariableInfo(string type, int index, int priority)
        {
            Type = type;
            Index = index;
            Priority = priority;
        }
    }
}
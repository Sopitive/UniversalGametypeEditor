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
        public CompileResult TryCompileScript(string script)
        {
            var diags = new List<CompilerDiagnostic>();

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
        }

        private static MegaloAction ResolveActionByName(string name)
        {
            var tried = new List<string>();

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
                    => expr.ToString().Trim().Trim('"');

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

            Prepass_RegisterInlineFunctions(root);


            // Traverse the syntax tree
            foreach (var member in root.Members)
            {
                ProcessMember(member);
            }

            // Compile any nested triggers reserved during statement processing.
            DrainPendingTriggers();

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
                    foreach (var variable in localDeclaration.Declaration.Variables)
                    {
                        string varName = variable.Identifier.Text;
                        string type = localDeclaration.Declaration.Type.ToString();
                        if (type == "Object")
                        {
                            var gameObject = _entityManager.CreateObject(varName);
                            int index = _entityManager.GetObjectIndex(gameObject);
                            var variableInfo = new VariableInfo(type, index, 1); // Default to low priority
                            _variableToIndexMap[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized global Object variable '{varName}' at global.object[{index}]");
                        }
                        else
                        {
                            Debug.WriteLine($"Unhandled global variable type: {type}");
                        }
                    }
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
                ProcessAssignment(assignment, ref actionOffset);
            }
            else if (expression is InvocationExpressionSyntax invocation)
            {
                Debug.WriteLine($"Invocation: {invocation}");
                ProcessInvocation(invocation, ref actionOffset);
                actionCount++;
            }
            else if (expression is BinaryExpressionSyntax binaryExpression)
            {
                Debug.WriteLine($"Binary Expression: {binaryExpression}");
                int conditionCount = 0;
                ProcessCondition(binaryExpression, ref conditionCount, ref actionOffset, ref actionCount);
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

            // If this is a top-level "if", compile it as an Inline action
            if (forceInline)
            {
                // Remember the start indices before compiling this if
                int condStart = _conditions.Count;
                int actStart = _actions.Count;


                // Base for LOCAL condition action offsets inside this inline.
                _actionBaseStack.Push(actStart);
                try
                {
                    int ifCondCount = 0;
                    int bodyActionCount = 0;
                    int bodyConditionCount = 0;

                    // Compile condition into the global pool (temporarily)
                    ProcessCondition(ifStatement.Condition, ref ifCondCount, ref actionOffset, ref conditionOffset, 0);

                    // Compile body into the global pool (temporarily)
                    if (ifStatement.Statement is BlockSyntax block)
                    {
                        ProcessStatementList(block.Statements, ref bodyActionCount, ref bodyConditionCount, ref actionOffset, ref conditionOffset, false);
                    }
                    else
                    {
                        // Single statement body counts as last-in-block by definition.
                        ProcessStatement(ifStatement.Statement, ref bodyActionCount, ref bodyConditionCount, ref actionOffset, ref conditionOffset, false, true);
                    }
                }
                finally
                {
                    _actionBaseStack.Pop();
                }



                // Extract the newly-added conditions/actions from the pools
                var extractedConds = _conditions.GetRange(condStart, _conditions.Count - condStart);
                _conditions.RemoveRange(condStart, _conditions.Count - condStart);

                var extractedActs = _actions.GetRange(actStart, _actions.Count - actStart);
                _actions.RemoveRange(actStart, _actions.Count - actStart);

                // Roll back cursors because we removed what we just compiled
                conditionOffset = condStart;
                actionOffset = actStart;

                // Insert a placeholder Inline action into the TOP-LEVEL action stream
                // (we'll patch offsets after we append the extracted pools later)
                string placeholder = BuildInlineBinary(0, 0, 0, 0);
                _actions.Insert(actStart, new ActionObject("Inline", new List<string> { placeholder }));

                int inlineIndex = actStart;

                // This Inline is one top-level action
                actionCount += 1;
                actionOffset += 1;

                // Stash the extracted pools for later, and remember where to patch the Inline
                _deferredInlines.Add(new InlinePatch
                {
                    InlineActionIndex = inlineIndex,
                    Conditions = extractedConds,
                    Actions = extractedActs
                });

                Debug.WriteLine($"Top-level if compiled as Inline placeholder at action index {inlineIndex}");
                return;
            }

            // ---- Non-top-level if: keep your existing behavior (nested-if semantics) ----
            int nestedIfCondCount = 0;
            ProcessCondition(ifStatement.Condition, ref nestedIfCondCount, ref actionOffset, ref conditionOffset, 0);
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
                bool isLast = (i == statements.Count - 1);
                ProcessStatement(statements[i], ref a, ref c, ref actionOffset, ref conditionOffset, isTopLevel, isLast);
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
                        // IMPORTANT RULE:
                        // - Plain (non-inline) if conditions in this format will keep gating actions that follow them
                        //   in the same container, because there is no explicit "end if".
                        // - Therefore: only emit a plain if when it is the LAST statement in its block.
                        //   Otherwise emit an Inline.
                        bool mustInline = !isLastInBlock || ifStatement.Else != null;
                        ProcessIfStatement(ifStatement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, isTopLevel, mustInline);
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

                default:
                    Debug.WriteLine($"Unhandled statement type: {statement.GetType().Name}");
                    break;
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

                if (_variableToIndexMap.TryGetValue(varName, out var existingVariable))
                {
                    if (existingVariable.Type == actualType && existingVariable.Priority != priorityValue)
                    {
                        throw new InvalidOperationException($"Variable '{varName}' of type '{actualType}' already exists with a different priority.");
                    }
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
                            _scopeStack.Peek()[varName] = variableInfo;
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
                            _scopeStack.Peek()[varName] = variableInfo;
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
                            _scopeStack.Peek()[varName] = variableInfo;
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
                            _scopeStack.Peek()[varName] = variableInfo;
                            _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                            Debug.WriteLine($"Initialized Number variable '{varName}' at global.number[{numberIndex}] with networking priority {priorityValue}");
                        }
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
            // Support: var = SomeActionThatHasOutParam(...)
            // Example: weap = GetWeapon(current_player, GetPrimary);
            if (assignment.Left is IdentifierNameSyntax leftId &&
                assignment.Right is InvocationExpressionSyntax rightInv)
            {
                string varName = leftId.Identifier.Text;
                Debug.WriteLine($"Processing assignment-as-out: {varName} = {rightInv}");
                ProcessInvocation(rightInv, ref actionOffset, varName);
                return;
            }

            // Support: var = some_var_type_expression;
            // Example: newBip = current_player.biped;
            // This compiles to the Megalo "Set" action (formerly Megl.SET):
            //   Set(Base=<left>, Operand=<right>, Operator=Set)
            if (assignment.Left is IdentifierNameSyntax leftVar)
            {
                string baseTok = leftVar.ToString();
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
                    MegaloAction setAction = ResolveActionByName("Set");
                    var args = new List<string> { baseTok, operandTok, opTok };

                    List<string> paramBits = EncodeMegaloActionParams(setAction, args, varOut: null);
                    string actionIdBits = ConvertToBinary(setAction.Id, 7);
                    string binaryAction = actionIdBits + string.Join("", paramBits);

                    _actions.Add(new ActionObject(setAction.Name, new List<string> { binaryAction }));
                    actionOffset++;

                    Debug.WriteLine($"Compiled assignment via Set(): {baseTok} {assignment.OperatorToken} {operandTok}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to compile assignment '{assignment}': {ex.Message}");
                }
                return;
            }

            Debug.WriteLine($"Unhandled assignment form: {assignment}");
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
                return;
            }

            var args = invocation.ArgumentList.Arguments.Select(a => TrimArg(a.Expression)).ToList();

            MegaloAction action;
            try
            {
                action = ResolveActionByName(scriptName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            // Encode action id + params based on MegaloTables schema
            var paramBits = EncodeMegaloActionParams(action, args, varOut);
            string actionNumberBinary = ConvertToBinary(action.Id, 7);
            string binaryAction = actionNumberBinary + string.Join("", paramBits);

            _actions.Add(new ActionObject(action.Name, new List<string> { binaryAction }));
            Debug.WriteLine($"Added action: {scriptName}({binaryAction})");
            actionOffset++;
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
                // This project historically encodes ObjectType as 12 bits.
                if (token == null) return ConvertToBinary(0, 12);
                if (Enum.TryParse(typeof(ObjectType), token, true, out var objEnum) && objEnum != null)
                    return ConvertToBinary((int)objEnum, 12);
                if (int.TryParse(token, out int n))
                    return ConvertToBinary(n, 12);
                return ConvertToBinary(0, 12);
            }

            if (typeRef.Equals("Enumref:LabelRef", StringComparison.OrdinalIgnoreCase))
            {
                // Keep your existing label encoding (1-bit "no label" flag + optional 4-bit label id)
                return EncodeLabelRef(token);
            }

            // Fallback: encode as plain enum (bit-width inferred from max value)
            if (typeRef.StartsWith("Enumref:", StringComparison.OrdinalIgnoreCase))
            {
                string enumName = typeRef.Substring("Enumref:".Length);
                return EncodeEnumByName(enumName, token);
            }

            throw new InvalidOperationException($"Unsupported param typeRef: '{typeRef}'.");
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
                // Unknown enum: safest is zero-width -> return empty
                // but we must still emit something. Use 0 bits (empty) so we don't silently corrupt layouts.
                // If this trips, add a concrete encoder for this TypeRef.
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

        private string EncodeLabelRef(string label)
        {
            // Preserve prior behavior: 1 bit meaning "none/default", otherwise 0 + 4 bits label
            if (string.IsNullOrWhiteSpace(label) || label.Equals("none", StringComparison.OrdinalIgnoreCase))
                return ConvertToBinary(1, 1);

            if (int.TryParse(label, out int labelId))
                return ConvertToBinary(0, 1) + ConvertToBinary(labelId, 4);

            // Without the LabelRef enum table embedded here yet, fallback to 15.
            return ConvertToBinary(0, 1) + ConvertToBinary(15, 4);
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
            switch (name)
            {
                case "Local": attribute = "OnLocal"; return true;
                case "Init": attribute = "OnInit"; return true;
                case "LocalInit": attribute = "OnLocalInit"; return true;
                case "HostMigration": attribute = "OnHostMigration"; return true;
                case "ObjectDeath": attribute = "OnObjectDeath"; return true;
                case "Pregame": attribute = "OnPregame"; return true;
                case "Call": attribute = "OnCall"; return true;
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
            _deferredInlines.Clear();

            // Default trigger type and attribute
            string triggerType = method.Identifier.Text;
            string triggerAttribute = defaultAttribute ?? "OnTick";

            // Wrapper triggers (Local/Init/etc) compile as Do + mapped attribute.
            if (TryMapEventWrapper(method.Identifier.Text, out var wrapperAttr))
            {
                triggerType = "Do";
                triggerAttribute = wrapperAttr;

                if (!_eventWrapperAttributes.Add(wrapperAttr))
                    throw new InvalidOperationException($"Only one root trigger is allowed for event '{wrapperAttr}'.");
            }


            // Check for specific trigger attribute
            if (method.AttributeLists.Count > 0)
            {
                var attribute = method.AttributeLists[0].Attributes[0];
                if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0)
                    triggerAttribute = attribute.ArgumentList.Arguments[0].ToString().Trim('"');
            }

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

            if (method.Body != null)
            {
                ProcessStatementList(method.Body.Statements, ref actionCount, ref conditionCount, ref actionOffsetCursor, ref conditionOffsetCursor, true);
            }

            // Compute counts from the *start offsets*, not the mutated cursors
            int finalConditionCount = _conditions.Count - startConditionOffset;
            int finalActionCount = _actions.Count - startActionOffset;

            EndScope();

            // Create the trigger binary representation
            string conditionOffsetBinary = ConvertToBinary(startConditionOffset, 9);
            string conditionCountBinary = ConvertToBinary(finalConditionCount, 10);
            string actionOffsetBinary = ConvertToBinary(startActionOffset, 10);
            string actionCountBinary = ConvertToBinary(finalActionCount, 11);

            string triggerTypeBinary = ConvertToBinary(Enum.Parse(typeof(TriggerTypeEnum), triggerType), 3);
            string triggerAttributeBinary = ConvertToBinary(Enum.Parse(typeof(TriggerAttributeEnum), triggerAttribute), 3);

            string binaryTrigger =
                triggerTypeBinary +
                triggerAttributeBinary +
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




        private string GetBinaryString()
        {
            // Count the number of conditions, actions, and triggers
            int conditionCount = _conditions.Count;
            int actionCount = _actions.Count;
            int triggerCount = _triggers.Count;

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

            // Process left-hand side (typically a variable)
            string leftVarType = "000"; // Default to NumericVar
            string leftVarBinary = "";

            if (binaryExpression.Left is IdentifierNameSyntax leftIdentifier)
            {
                string varName = leftIdentifier.Identifier.Text;
                if (_variableToIndexMap.TryGetValue(varName, out VariableInfo varInfo))
                {
                    // Handle based on variable type
                    if (varInfo.Type == "Number")
                    {
                        // NumericVar (000) + NumericTypeRef (GlobalNumber = 4)
                        string numericTypeRefBinary = Convert.ToString((int)NumericTypeRefEnum.GlobalNumber, 2).PadLeft(6, '0');
                        string globalNumberIndexBinary = Convert.ToString(varInfo.Index, 2).PadLeft(4, '0');
                        leftVarBinary = numericTypeRefBinary + globalNumberIndexBinary;
                        Debug.WriteLine($"Processing variable {varName} as global.number[{varInfo.Index}]");
                    }
                    // Add handling for other variable types if needed
                }
            }

            // Process right-hand side (typically a literal)
            string rightVarType = "000"; // Default to NumericVar
            string rightVarBinary = "";

            if (binaryExpression.Right is LiteralExpressionSyntax rightLiteral)
            {
                // For numeric literals, use Int16 NumericTypeRef (0)
                string numericTypeRefBinary = Convert.ToString((int)NumericTypeRefEnum.Int16, 2).PadLeft(6, '0');
                int literalValue = int.Parse(rightLiteral.Token.ValueText);
                string literalValueBinary = Convert.ToString(literalValue, 2).PadLeft(16, '0');
                rightVarBinary = numericTypeRefBinary + literalValueBinary;
                Debug.WriteLine($"Processing literal value {literalValue}");
            }

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

            // Increment condition count
            conditionCount++;
        }




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
                    // OR chains use ORSequence slots
                    ProcessCondition(bin.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                    ProcessCondition(bin.Right, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence + 1, isNot);
                    return;
                }

                if (bin.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    // AND is implicit: just emit both conditions with same ORSequence
                    ProcessCondition(bin.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
                    ProcessCondition(bin.Right, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot);
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
                if (string.IsNullOrWhiteSpace(scriptName))
                {
                    Debug.WriteLine("Unsupported condition invocation form.");
                    return;
                }

                MegaloCondition cond;
                try
                {
                    cond = ResolveConditionByName(scriptName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return;
                }

                var args = invocation.ArgumentList.Arguments.Select(a => TrimArg(a.Expression)).ToList();
                var paramBits = EncodeMegaloConditionParams(cond, args);

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
                conditionOffset++;
                conditionCount++;

                return;
            }

            Debug.WriteLine($"Unsupported condition type: {condition.Kind()}");
        }


        private static string ToSigned16Binary(int v)
        {
            int b = v & 0xFFFF; // two's complement
            return Convert.ToString(b, 2).PadLeft(16, '0');
        }

        private string ConvertNumericTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // Allow literals directly: NumericTypeRef(Int16) + 16-bit payload
            if (int.TryParse(value, out int literal))
            {
                string typeBits = Convert.ToString((int)NumericTypeRefEnum.Int16, 2).PadLeft(6, '0');
                string payload = ToSigned16Binary(literal);
                string finalLit = typeBits + payload;
                return bitSize > 0 ? finalLit.PadLeft(bitSize, '0') : finalLit;
            }

            // Define a dictionary to map input strings to their corresponding NumericTypeRefEnum values
            var numericTypeRefMap = new Dictionary<string, NumericTypeRefEnum>(StringComparer.OrdinalIgnoreCase)
    {
        { "Int16", NumericTypeRefEnum.Int16 },
        { "NoNumber", NumericTypeRefEnum.Int16 },
        // You can add more explicit mappings here if you have "RoundTime" etc.
    };

            // Check if the input value is a GlobalNumber
            if (value.StartsWith("GlobalNumber", StringComparison.OrdinalIgnoreCase))
            {
                int globalNumberIndex = int.Parse(value.Replace("GlobalNumber", ""));
                // Index 0 maps to GlobalNumber0, etc.
                if (globalNumberIndex < 0 || globalNumberIndex > 15)
                    throw new ArgumentException($"Invalid GlobalNumber index: {globalNumberIndex}");

                string numericTypeRefBinary = Convert.ToString((int)NumericTypeRefEnum.GlobalNumber, 2).PadLeft(6, '0');
                string globalNumberIndexBinary = Convert.ToString(globalNumberIndex, 2).PadLeft(4, '0');
                string finalBinaryString = numericTypeRefBinary + globalNumberIndexBinary;
                return bitSize > 0 ? finalBinaryString.PadLeft(bitSize, '0') : finalBinaryString;
            }

            // Convert NumericTypeRef to its binary representation
            if (numericTypeRefMap.TryGetValue(value, out var numericTypeRefEnum))
            {
                string numericTypeRefBinary = Convert.ToString((int)numericTypeRefEnum, 2).PadLeft(6, '0');
            }

            throw new ArgumentException($"Unsupported NumericTypeRef: {value}");
        }

        private string ConvertPlayerTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // Support GlobalPlayerN (similar to GlobalObject)
            if (value.StartsWith("GlobalPlayer", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Replace("GlobalPlayer", ""));
                idx += 1; // reserve 0 for NoPlayer
                if (idx < 0 || idx > 31) throw new ArgumentException($"Invalid GlobalPlayer index: {idx}");

                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString(idx, 2).PadLeft(5, '0');
                string final = typeBits + refBits;
                return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
            }

            if (value.Equals("NoPlayer", StringComparison.OrdinalIgnoreCase))
            {
                string typeBits = Convert.ToString((int)PlayerTypeRefEnum.Player, 2).PadLeft(2, '0');
                string refBits = Convert.ToString(0, 2).PadLeft(5, '0');
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

            if (value.StartsWith("GlobalTeam", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Replace("GlobalTeam", ""));
                refVal = idx + 1;
            }
            else if (value.Equals("current_team", StringComparison.OrdinalIgnoreCase))
            {
                // Best-effort: treat as Team0 for now (adjust if you have an enum)
                refVal = 1;
            }
            else if (value.Equals("NoTeam", StringComparison.OrdinalIgnoreCase))
            {
                refVal = 0;
            }
            else if (value.StartsWith("Team", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Replace("Team", ""), out int t))
            {
                refVal = t + 1;
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

            string typeBits = ConvertToBinary(typeVal, 2);
            string refBits = ConvertToBinary(refVal, 5);
            string final = typeBits + refBits;
            return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
        }

        private string ConvertTimerTypeRefToBinary(string value, int bitSize)
        {
            value = (value ?? string.Empty).Trim();

            // Fallback encoding: [2-bit type][5-bit ref]
            int typeVal = 0;
            int refVal = 0;

            if (value.StartsWith("GlobalTimer", StringComparison.OrdinalIgnoreCase))
            {
                int idx = int.Parse(value.Replace("GlobalTimer", ""));
                refVal = idx + 1;
            }
            else if (value.Equals("NoTimer", StringComparison.OrdinalIgnoreCase))
            {
                refVal = 0;
            }
            else
            {
                // Try numeric token
                if (int.TryParse(value, out int n))
                    refVal = n;
            }

            string typeBits = ConvertToBinary(typeVal, 2);
            string refBits = ConvertToBinary(refVal, 5);
            string final = typeBits + refBits;
            return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
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
                // Support direct object/player tokens for VarType:
                // - current_player.biped -> object
                // - current_player -> player
                if (token.Equals("current_player", StringComparison.OrdinalIgnoreCase))
                {
                    kindBits = ConvertToBinary(1, 3);
                    payloadBits = ConvertPlayerTypeRefToBinary("current_player", 0);
                }
                else if (token.Equals("current_player.biped", StringComparison.OrdinalIgnoreCase)
                      || token.Equals("current_object", StringComparison.OrdinalIgnoreCase))
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

            string final = kindBits + payloadBits;
            return bitSize > 0 ? final.PadLeft(bitSize, '0') : final;
        }

        private string ConvertObjectTypeRefToBinary(string value, int bitSize, int locality)
        {
            // Define a dictionary to map input strings to their corresponding ObjectTypeRefEnum values
            var objectTypeRefMap = new Dictionary<string, ObjectTypeRefEnum>
            {
                { "current_player.biped", ObjectTypeRefEnum.PlayerBiped },
                { "NoObject", ObjectTypeRefEnum.ObjectRef },
                {"current_object", ObjectTypeRefEnum.ObjectRef }
                // Add more mappings as needed
            };

            // Check if the input value is a GlobalObject
            if (value.StartsWith("GlobalObject"))
            {
                int globalObjectIndex = int.Parse(value.Replace("GlobalObject", ""));
                globalObjectIndex += 1; // Increment the index by 1 to account for the NoObject case
                if (globalObjectIndex < 0 || globalObjectIndex > 15)
                {
                    throw new ArgumentException($"Invalid GlobalObject index: {globalObjectIndex}");
                }

                // Convert the ObjectTypeRef to its binary representation
                string objectTypeRefBinary = Convert.ToString((int)ObjectTypeRefEnum.ObjectRef, 2).PadLeft(3, '0');

                // Convert the global object index to its binary representation
                string globalObjectIndexBinary = Convert.ToString(globalObjectIndex, 2).PadLeft(5, '0');

                // Concatenate the binary representations to form the final binary string
                string finalBinaryString = objectTypeRefBinary + globalObjectIndexBinary;
                return finalBinaryString;
            }
            else if (objectTypeRefMap.TryGetValue(value, out var objectTypeRefEnum))
            {
                // Convert the ObjectTypeRef to its binary representation
                string objectTypeRefBinary = Convert.ToString((int)objectTypeRefEnum, 2).PadLeft(3, '0');

                // Handle specific cases for parameters
                List<string> parameterBinaries = new List<string>();
                if (value == "current_player.biped")
                {
                    parameterBinaries.Add(Convert.ToString((int)PlayerRefEnum.CurrentPlayer, 2).PadLeft(5, '0'));
                }
                else if (value == "current_object")
                {
                    parameterBinaries.Add(Convert.ToString((int)ObjectRef.CurrentObject, 2).PadLeft(5, '0'));
                }

                // Concatenate the binary representations to form the final binary string
                string finalBinaryString = objectTypeRefBinary + string.Join("", parameterBinaries);
                return finalBinaryString;
            }
            else
            {
                throw new ArgumentException($"Unsupported ObjectTypeRef: {value}");
            }
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
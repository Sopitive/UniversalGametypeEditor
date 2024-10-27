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


namespace UniversalGametypeEditor
{

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
        private Stack<int> _availablePlayerIndices = new Stack<int>();
        private Stack<int> _availableObjectIndices = new Stack<int>();

        public EntityManager()
        {
            // Initialize with some default players or objects if needed
        }

        public Player CreatePlayer(string name)
        {
            int index;
            if (_availablePlayerIndices.Count > 0)
            {
                index = _availablePlayerIndices.Pop();
                Console.WriteLine($"Recycled player index: {index}");
            }
            else
            {
                index = _players.Count;
                Console.WriteLine($"New player index: {index}");
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
                Console.WriteLine($"Recycled object index: {index}");
            }
            else
            {
                index = _objects.Count;
                Console.WriteLine($"New object index: {index}");
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

        public void RemovePlayer(int index)
        {
            if (index >= 0 && index < _players.Count)
            {
                _players[index] = null!;
                _availablePlayerIndices.Push(index);
                Console.WriteLine($"Recycled player index: {index}");
            }
        }

        public void RemoveObject(int index)
        {
            if (index >= 0 && index < _objects.Count)
            {
                _objects[index] = null!;
                _availableObjectIndices.Push(index);
                Console.WriteLine($"Recycled object index: {index}");
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

        public int GetPlayerIndex(Player player)
        {
            return _players.IndexOf(player);
        }

        public int GetObjectIndex(GameObject gameObject)
        {
            return _objects.IndexOf(gameObject);
        }
    }


    internal class ScriptCompiler
    {
        private EntityManager _entityManager = new EntityManager();
        private Dictionary<string, VariableInfo> _variableToIndexMap = new Dictionary<string, VariableInfo>();
        private Stack<Dictionary<string, VariableInfo>> _scopeStack = new Stack<Dictionary<string, VariableInfo>>();
        private List<ActionObject> _actions = new List<ActionObject>();
        private List<ConditionObject> _conditions = new List<ConditionObject>();
        private List<TriggerObject> _triggers = new List<TriggerObject>();
        private Dictionary<string, VariableInfo> _allDeclaredVariables = new Dictionary<string, VariableInfo>();
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

            // Traverse the syntax tree
            foreach (var member in root.Members)
            {
                ProcessMember(member);
            }
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
                Console.WriteLine(diagnostic);
            }
        }


        private void ProcessMember(MemberDeclarationSyntax member)
        {
            if (member is ClassDeclarationSyntax classDecl)
            {
                Console.WriteLine($"Class: {classDecl.Identifier.Text}");
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
                        Console.WriteLine($"Initialized Player variable '{varName}' at global.player[{index}] with networking priority {networkingPriority}");
                    }
                    else if (type == "Object")
                    {
                        var gameObject = _entityManager.CreateObject(varName);
                        int index = _entityManager.GetObjectIndex(gameObject);
                        var variableInfo = new VariableInfo(type, index, networkingPriority);
                        _variableToIndexMap[varName] = variableInfo;
                        _allDeclaredVariables[varName] = variableInfo; // Track all declared variables
                        Console.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {networkingPriority}");
                    }
                    else
                    {
                        Console.WriteLine($"Unhandled global variable type: {type}");
                    }
                }
            }
            else if (member is MethodDeclarationSyntax method)
            {
                Console.WriteLine($"Method: {method.Identifier.Text}");
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
                Console.WriteLine($"Property: {property.Identifier.Text}");
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
                            ProcessExpression(accessor.ExpressionBody.Expression, ref actionCount, actionCount);
                        }
                    }
                }
                EndScope();
            }
            else if (member is ConstructorDeclarationSyntax constructor)
            {
                Console.WriteLine($"Constructor: {constructor.Identifier.Text}");
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
                Console.WriteLine("Global Statement");
                _scopeStack.Push(new Dictionary<string, VariableInfo>()); // Ensure a new scope is pushed
                int actionCount = 0;
                if (globalStatement.Statement is LocalFunctionStatementSyntax localFunction)
                {
                    Console.WriteLine($"Local Function: {localFunction.Identifier.Text}");
                    if (localFunction.Body != null)
                    {
                        // Call ProcessTrigger for top-level local function declarations
                        ProcessTrigger(localFunction);
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
                            Console.WriteLine($"Initialized global Object variable '{varName}' at global.object[{index}]");
                        }
                        else
                        {
                            Console.WriteLine($"Unhandled global variable type: {type}");
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
                Console.WriteLine($"Unhandled member type: {member.GetType().Name}");
            }
        }

        private void ProcessExpression(ExpressionSyntax expression, ref int actionCount, int actionOffset)
        {
            if (expression is AssignmentExpressionSyntax assignment)
            {
                // Handle assignment expressions
                Console.WriteLine($"Assignment: {assignment}");
                ProcessAssignment(assignment, ref actionOffset);
                actionCount++;
            }
            else if (expression is InvocationExpressionSyntax invocation)
            {
                // Handle method invocations
                Console.WriteLine($"Invocation: {invocation}");
                ProcessInvocation(invocation, ref actionOffset);
                actionCount++;
            }
            else if (expression is BinaryExpressionSyntax binaryExpression)
            {
                // Handle binary expressions (conditions)
                Console.WriteLine($"Binary Expression: {binaryExpression}");
                int conditionCount = 0; // Initialize conditionCount
                ProcessCondition(binaryExpression, ref conditionCount, ref actionOffset, ref actionCount);
            }
            // Add more cases for other expression types if needed
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
            _actions.Insert(triggerActionOffset, new ActionObject("Inline", new List<string> { binaryInlineAction }));
            Console.WriteLine($"Added inline action: Inline({binaryInlineAction})");
            int finalConditionCount = conditionCount;
            // Return the number of actions added (1 in this case)
            return (1 - actionCount, finalConditionCount);
        }





        private int actionsAdded = 0;
        private int inlineActionOffset = 1;
        private int inlineActionsOffsetDiff = 0;
        private void ProcessStatement(StatementSyntax statement, ref int actionCount, ref int conditionCount, ref int actionOffset, ref int conditionOffset, bool isTopLevel = true)
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

            // Refactored to use a switch statement with pattern matching
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    // Process local variable declarations
                    ProcessLocalDeclaration(localDeclaration, ref actionCount, ref actionOffset);
                    break;

                case ExpressionStatementSyntax expressionStatement:
                    // Process expressions
                    ProcessExpression(expressionStatement.Expression, ref actionCount, actionOffset);
                    Console.WriteLine($"Action Count after Expression: {actionCount}");
                    //inlineActionOffset++; // Update action offset after processing the expression
                    break;

                case IfStatementSyntax ifStatement:
                    Console.WriteLine($"If Statement: {ifStatement.Condition}");

                    // Record current condition and action offsets before processing
                    int conditionStartOffset = _conditions.Count;
                    int actionStartOffset = inlineActionOffset;

                    // Track conditions in the current block
                    List<int> conditionIndices = new List<int>();
                    int conditionActionOffset = _actions.Count;

                    // Process the condition and update condition count
                    ProcessCondition(ifStatement.Condition, ref conditionCount, ref conditionActionOffset, ref conditionOffset);
                    conditionIndices.Add(_conditions.Count - 1); // Track the index of the added condition
                    Console.WriteLine($"Condition Count after If Condition: {conditionCount}");

                    // Process the body of the if statement
                    if (ifStatement.Statement is BlockSyntax block)
                    {
                        foreach (var blockStatement in block.Statements)
                        {
                            ProcessStatement(blockStatement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                        }
                    }
                    else
                    {
                        ProcessStatement(ifStatement.Statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                    }

                    //// Track inline actions without compiling them
                    if (!IsBottomLevelBlock(ifStatement))
                    {
                        //inlineActionOffset++;
                        //inlineActionOffset = inlineActionOffset == 0 ? 1 : inlineActionOffset;
                        int inlineConditionCount = _conditions.Count - conditionStartOffset;
                        int inlineActionCount = inlineActionOffset - actionStartOffset;
                        //inlineActionOffset += inlineActionCount;
                        int firstConditionOffset = -1;

                        // Cache the parameters for inline actions
                        _inlineActionCaches.Add(new InlineActionCache
                        {
                            ConditionStartOffset = conditionStartOffset,
                            InlineConditionCount = inlineConditionCount,
                            ActionStartOffset = actionStartOffset,
                            InlineActionCount = inlineActionCount
                        });

                        // Update the actionOffset for each condition in the inline action
                        //for (int i = 0; i < inlineConditionCount; i++)
                        //{
                        //    int conditionIndex = conditionStartOffset + i;
                        //    var condition = _conditions[conditionIndex];
                        //    string binaryCondition = condition.Parameters.First();

                        //    // Get the current action offset
                        //    if (firstConditionOffset < 0)
                        //    {
                        //        firstConditionOffset = Convert.ToInt32(binaryCondition.Substring(15, 10), 2);
                        //    }

                        //    string updatedBinaryCondition = UpdateConditionActionOffset(binaryCondition, firstConditionOffset, false);
                        //    _conditions[conditionIndex] = new ConditionObject(condition.ConditionType, new List<string> { updatedBinaryCondition });
                        //}
                    }

                    // Check for else or else-if statements and compile inline actions
                    if (ifStatement.Else != null)
                    {
                        ProcessStatement(ifStatement.Else.Statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                    }
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    Console.WriteLine($"Local Function: {localFunction.Identifier.Text}");
                    if (localFunction.Body != null)
                    {
                        _scopeStack.Push(new Dictionary<string, VariableInfo>());
                        foreach (var localStatement in localFunction.Body.Statements)
                        {
                            ProcessStatement(localStatement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                        }
                        EndScope();
                    }
                    break;

                case ReturnStatementSyntax returnStatement:
                    Console.WriteLine($"Return Statement: {returnStatement.Expression}");
                    break;

                case BlockSyntax block1:
                    foreach (var blockStatement in block1.Statements)
                    {
                        ProcessStatement(blockStatement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset, false);
                    }
                    break;

                default:
                    Console.WriteLine($"Unhandled statement type: {statement.GetType().Name}");
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

                Console.WriteLine($"Variable '{varName}' of type '{actualType}' with priority '{networkingPriority}'");

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
                            Console.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {priorityValue}");

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
                            Console.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}] with networking priority {priorityValue}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unhandled local variable type: {actualType}");
                    }
                }
            }
        }


        


        private void ProcessAssignment(AssignmentExpressionSyntax assignment, ref int actionOffset)
        {
            var left = assignment.Left as IdentifierNameSyntax;
            var right = assignment.Right as InvocationExpressionSyntax;

            if (left != null && right != null)
            {
                string varName = left.Identifier.Text;
                Console.WriteLine($"Processing assignment: {varName} = {right}");
                ProcessInvocation(right, ref actionOffset, varName);
            }
        }

        private void ProcessInvocation(InvocationExpressionSyntax invocation, ref int actionOffset, string? varOut = "NoObject", int inlineActionOffset2 = -1)
        {
            var identifier = invocation.Expression as IdentifierNameSyntax;
            if (identifier != null)
            {
                string actionName = identifier.Identifier.Text;
                Console.WriteLine($"Processing invocation: {actionName}");
                var actionDefinition = ActionDefinitions.ValidActions.FirstOrDefault(a => a.Name == actionName);
                if (actionDefinition != null)
                {
                    var arguments = invocation.ArgumentList.Arguments;
                    List<string> parameters = new List<string>();
                    int hasVarOut = 0;
                    for (int i = 0; i < actionDefinition.Parameters.Count; i++)
                    {
                        var param = actionDefinition.Parameters[i];
                        int bitSize = param.Bits; // Use the Bits property from ActionParameter

                        if (param.Name == "var_out")
                        {
                            hasVarOut = 1;
                            if (varOut != null && _variableToIndexMap.TryGetValue(varOut, out VariableInfo varInfo))
                            {
                                // Translate the index to a global number and then to ObjectRef
                                string varOutValue = $"GlobalObject{varInfo.Index}";
                                string binaryRepresentation = ConvertObjectTypeRefToBinary(varOutValue, bitSize, varInfo.Priority);
                                parameters.Add(binaryRepresentation);
                            }
                            else
                            {
                                //use the default
                                parameters.Add(ConvertObjectTypeRefToBinary(param.DefaultValue?.ToString() ?? string.Empty, bitSize, 1));
                            }
                        }
                        else if (i < arguments.Count + hasVarOut)
                        {
                            string? argumentValue = arguments[i - hasVarOut].ToString().Trim('"');
                            if (argumentValue != null && _variableToIndexMap.TryGetValue(argumentValue, out VariableInfo varInfo))
                            {
                                // Translate the variable to its corresponding ObjectTypeRef
                                argumentValue = $"GlobalObject{varInfo.Index}";
                            }

                            if (param.ParameterType == typeof(ObjectTypeRef))
                            {
                                string binaryRepresentation = ConvertObjectTypeRefToBinary(argumentValue ?? string.Empty, bitSize, 1);
                                parameters.Add(binaryRepresentation);
                            }
                            else if (param.ParameterType == typeof(PlayerTypeRef))
                            {
                                string binaryRepresentation = ConvertPlayerTypeRefToBinary(argumentValue ?? string.Empty, bitSize);
                                parameters.Add(binaryRepresentation);
                            }
                            else if (param.Name == "type")
                            {
                                int typeValue = ConvertToObjectType(argumentValue ?? string.Empty);
                                parameters.Add(ConvertToBinary(typeValue, bitSize));
                            }
                            else if (param.Name == "label")
                            {
                                if (string.IsNullOrEmpty(argumentValue) || argumentValue.Equals("none", StringComparison.OrdinalIgnoreCase))
                                {
                                    parameters.Add(ConvertToBinary(1, 1)); // Default to 1
                                }
                                else
                                {
                                    parameters.Add(ConvertToBinary(0, 1)); // Indicate that a label is specified
                                    parameters.Add(ConvertToBinary(15, 4)); // Convert the label to a 4-bit number
                                }
                            }
                            else if (param.ParameterType == typeof(bool))
                            {
                                bool boolValue = argumentValue == "1" ? true : argumentValue == "0" ? false : bool.Parse(argumentValue ?? "false");
                                parameters.Add(ConvertToBinary(boolValue, bitSize));
                            }
                            else
                            {
                                parameters.Add(ConvertToBinary(argumentValue ?? string.Empty, bitSize));
                            }
                        }
                        else
                        {
                            parameters.Add(ConvertToBinary(param.DefaultValue, bitSize));
                        }
                    }

                    // Convert the action number to a 7-bit binary string
                    string actionNumberBinary = ConvertToBinary(actionDefinition.Id, 7);

                    // Concatenate the action number and all binary parameters into a single binary string
                    string binaryAction = actionNumberBinary + string.Join("", parameters);

                    // Check if the action already exists in the actions list
                    if (!_actions.Any(a => a.Parameters.Contains(binaryAction)))
                    {
                        //if (inlineActionOffset >= 0)
                        //{
                        //    _actions.Insert(inlineActionOffset, new ActionObject(actionName, new List<string> { binaryAction }));
                        //    Console.WriteLine($"Added inline action: {actionName}({binaryAction})");
                        //    actionOffset++;
                        //}
                        //else if (actionOffset >= 0)
                        //{
                        //    _actions.Insert(actionOffset, new ActionObject(actionName, new List<string> { binaryAction }));
                        //    Console.WriteLine($"Added action at offset: {actionName}({binaryAction})");
                        //    actionOffset++; // Increment the action offset
                        //}
                        //else
                        //{
                            _actions.Add(new ActionObject(actionName, new List<string> { binaryAction }));
                            Console.WriteLine($"Added action: {actionName}({binaryAction})");
                            inlineActionOffset++;
                        //}
                    }
                }
                else
                {
                    Console.WriteLine($"Action definition not found for: {actionName}");
                }
            }
        }


        private void ProcessTrigger(LocalFunctionStatementSyntax method)
        {
            // Default trigger type and attribute
            string triggerType = "Do";
            string triggerAttribute = "OnTick";

            // Use the identifier to set the trigger type
            triggerType = method.Identifier.Text;

            // Check for specific trigger attribute
            if (method.AttributeLists.Count > 0)
            {
                var attribute = method.AttributeLists[0].Attributes[0];
                if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0)
                {
                    triggerAttribute = attribute.ArgumentList.Arguments[0].ToString().Trim('"');
                }
            }

            // Count the number of conditions and actions for this trigger
            int conditionOffset = _conditions.Count;
            int actionOffset = _actions.Count;
            int conditionCount = 0;
            int actionCount = 0;
            int inlineActionCount = 0; // Track the number of inline actions created
            triggerActionOffset = actionOffset;
            // Push a new scope onto the stack
            _scopeStack.Push(new Dictionary<string, VariableInfo>());

            if (method.Body != null)
            {
                foreach (var statement in method.Body.Statements)
                {
                    ProcessStatement(statement, ref actionCount, ref conditionCount, ref actionOffset, ref conditionOffset);
                }
            }
            int actionDifference = 0;
            // Compile the cached inline actions
            int totalInlineActions = _inlineActionCaches.Count;
            int lastOffset = 0;
            foreach (var cache in _inlineActionCaches)
            {
                int actionsAdded = 0;
                int conditionsAdded = 0;
                (actionsAdded, conditionsAdded) = CompileInlineAction(cache.ConditionStartOffset, cache.InlineConditionCount, cache.ActionStartOffset + _inlineActionCaches.Count, cache.InlineActionCount);
                inlineActionCount += actionsAdded;
                conditionCount -= conditionsAdded;
                conditionOffset += conditionsAdded;
                int processedCount = 0;
                actionDifference += actionsAdded;
                // Update the actionOffset for each condition in the inline action
                for (int i = 0; i < cache.InlineConditionCount; i++)
                {
                    int conditionIndex = cache.ConditionStartOffset + i;
                    var condition = _conditions[conditionIndex];
                    string binaryCondition = condition.Parameters.First();

                    // Get the current action offset
                    int currentActionOffset = Convert.ToInt32(binaryCondition.Substring(15, 10), 2);
                    processedCount = currentActionOffset - lastOffset;
                    processedCount = i == 0 ? 0 : processedCount;
                    // Subtract the number of conditions already processed
                    int updatedActionOffset = processedCount;
                    lastOffset = currentActionOffset;

                    // Convert the updated action offset back to binary
                    string updatedBinaryCondition = binaryCondition.Substring(0, 15) + ConvertToBinary(updatedActionOffset, 10) + binaryCondition.Substring(25);
                    _conditions[conditionIndex] = new ConditionObject(condition.ConditionType, new List<string> { updatedBinaryCondition });
                }
            }

            // Adjust actionOffset for conditions not part of inline actions
            foreach (var condition in _conditions.Skip(conditionOffset))
            {
                string binaryCondition = condition.Parameters.First();
                int currentActionOffset = Convert.ToInt32(binaryCondition.Substring(15, 10), 2);
                int updatedActionOffset = Math.Max(currentActionOffset + actionDifference, 0);
                string updatedBinaryCondition = binaryCondition.Substring(0, 15) + ConvertToBinary(updatedActionOffset, 10) + binaryCondition.Substring(25);
                condition.Parameters[0] = updatedBinaryCondition;
            }
            int inlineActionCount2 = _inlineActionCaches.Count;
            // Clear the cache after processing
            _inlineActionCaches.Clear();

            EndScope();

            int totalActionCount = actionCount + inlineActionCount;
            actionCount += inlineActionCount;

            // Calculate the number of actions to move
            int actionsToMoveCount = actionCount - inlineActionCount2;

            // Extract the actions to move from the bottom of the list
            var actionsToMove = _actions.Skip(_actions.Count - actionsToMoveCount).ToList();

            // Remove the extracted actions from the bottom of the list
            _actions.RemoveRange(_actions.Count - actionsToMoveCount, actionsToMoveCount);

            // Insert the extracted actions just underneath the inline actions
            _actions.InsertRange(triggerActionOffset + inlineActionCount2, actionsToMove);

            string conditionOffsetBinary = ConvertToBinary(conditionOffset, 9);
            string conditionCountBinary = ConvertToBinary(conditionCount, 10);
            string actionOffsetBinary = ConvertToBinary(Math.Max(actionOffset - 1, 0), 10);
            string actionCountBinary = ConvertToBinary(totalActionCount, 11); // Use the total action count

            string triggerTypeBinary = ConvertToBinary((int)Enum.Parse(typeof(TriggerTypeEnum), triggerType), 3);
            string triggerAttributeBinary = ConvertToBinary((int)Enum.Parse(typeof(TriggerAttributeEnum), triggerAttribute), 3);

            string binaryTrigger = triggerTypeBinary + triggerAttributeBinary + conditionOffsetBinary + conditionCountBinary + actionOffsetBinary + actionCountBinary;
            _triggers.Add(new TriggerObject(triggerType, new List<string> { binaryTrigger }));
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
                globalVariablesBinary += ConvertToBinary(0, 0); // Number
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




        private void ProcessCondition(ExpressionSyntax condition, ref int conditionCount, ref int actionOffset, ref int conditionOffset, int orSequence = 0, bool isNot = false, int localActionOffset = 0)
        {
            if (condition is PrefixUnaryExpressionSyntax unaryExpression && unaryExpression.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            {
                // Handle negation
                ProcessCondition(unaryExpression.Operand, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, true, localActionOffset);
                return;
            }

            if (condition is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken))
                {
                    // Handle logical AND
                    ProcessCondition(binaryExpression.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot, localActionOffset);
                    ProcessCondition(binaryExpression.Right, ref conditionCount, ref actionOffset, ref conditionOffset, ++orSequence, isNot, localActionOffset);
                    return;
                }
                else if (binaryExpression.OperatorToken.IsKind(SyntaxKind.BarBarToken))
                {
                    // Handle logical OR
                    ProcessCondition(binaryExpression.Left, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot, localActionOffset);
                    ProcessCondition(binaryExpression.Right, ref conditionCount, ref actionOffset, ref conditionOffset, orSequence, isNot, localActionOffset);
                    return;
                }
            }

            if (condition is InvocationExpressionSyntax invocation)
            {
                var identifier = invocation.Expression as IdentifierNameSyntax;
                if (identifier != null)
                {
                    string conditionName = identifier.Identifier.Text;
                    Console.WriteLine($"Processing condition: {conditionName}");
                    var conditionDefinition = ConditionDefinitions.ValidConditions.FirstOrDefault(c => c.Name == conditionName);
                    if (conditionDefinition != null)
                    {
                        var arguments = invocation.ArgumentList.Arguments;
                        List<string> parameters = new List<string>();
                        for (int i = 0; i < conditionDefinition.Parameters.Count; i++)
                        {
                            var param = conditionDefinition.Parameters[i];
                            int bitSize = param.Bits; // Use the Bits property from ConditionParameter

                            if (i < arguments.Count)
                            {
                                // Convert parameters to binary
                                string paramValue = arguments[i].ToString().Trim('"');
                                if (param.ParameterType == typeof(ObjectTypeRef))
                                {
                                    // Handle ObjectTypeRef conversion
                                    int priority = 1; // Default to low priority
                                    if (paramValue != null && _variableToIndexMap.TryGetValue(paramValue, out VariableInfo index))
                                    {
                                        paramValue = $"GlobalObject{index.Index}";
                                        priority = index.Priority;
                                    }
                                    parameters.Add(ConvertObjectTypeRefToBinary(paramValue ?? string.Empty, bitSize, priority));
                                }
                                else if (param.ParameterType == typeof(ObjectType))
                                {
                                    ObjectType objectType = (ObjectType)Enum.Parse(typeof(ObjectType), paramValue, true);
                                    parameters.Add(ConvertToBinary(objectType, bitSize));
                                }
                                else
                                {
                                    parameters.Add(ConvertToBinary(paramValue, bitSize));
                                }
                            }
                            else
                            {
                                parameters.Add(ConvertToBinary(param.DefaultValue, bitSize));
                            }
                        }

                        // Convert the condition number to a 5-bit binary string
                        string conditionNumberBinary = ConvertToBinary(conditionDefinition.Id, 5);

                        // Convert the NOT, ORSequence, and ActionOffset to binary strings
                        string notBinary = ConvertToBinary(isNot ? 1 : 0, 1);
                        string orSequenceBinary = ConvertToBinary(orSequence, 9);
                        string actionOffsetBinary = ConvertToBinary(actionOffset, 10); // Use localActionOffset for inline actions

                        // Concatenate all binary parts into a single binary string
                        string binaryCondition = conditionNumberBinary + notBinary + orSequenceBinary + actionOffsetBinary + string.Join("", parameters);

                        // Add the condition to the conditions list
                        _conditions.Add(new ConditionObject(conditionName, new List<string> { binaryCondition }));
                        Console.WriteLine($"Added condition: {conditionName}({binaryCondition})");

                        // Increment the condition count and condition offset
                        conditionCount++;
                        //conditionOffset++;

                        // Increment the local action offset for the next condition in the inline action
                        localActionOffset++;
                    }
                    else
                    {
                        Console.WriteLine($"Condition definition not found for: {conditionName}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Unhandled condition type: {condition.GetType().Name}");
            }
        }

        




        // Method to determine if a condition is standalone
        private bool IsStandaloneCondition(ExpressionSyntax condition)
        {
            // Get the parent of the condition
            var parent = condition.Parent;

            // Check if the parent is an IfStatementSyntax
            if (parent is IfStatementSyntax ifStatement)
            {
                // Check if the IfStatement's parent is a BlockSyntax
                if (ifStatement.Parent is BlockSyntax blockParent)
                {
                    // Check if the BlockSyntax's parent is a MethodDeclarationSyntax or LocalFunctionStatementSyntax
                    if (blockParent.Parent is MethodDeclarationSyntax || blockParent.Parent is LocalFunctionStatementSyntax)
                    {
                        // This indicates that the condition is directly inside a method or local function and not nested
                        return true;
                    }
                }
            }

            // If the parent is another conditional construct, it is nested
            return false;
        }


        // Method to create a new trigger for a standalone condition
        private int CreateNewTriggerForStandaloneCondition(ExpressionSyntax condition, string conditionName, string binaryCondition)
        {
            // Default trigger type and attribute
            string triggerType = "Do";
            string triggerAttribute = "OnCall";

            // Count the number of conditions and actions for this trigger
            int conditionOffset = _conditions.Count;  // Start where the current conditions end
            int actionOffset = _actions.Count;        // Start where the current actions end
            int conditionCount = 1;                   // We are creating a new trigger with a single condition
            int actionCount = 0;                      // Initialize action count

            // Push a new scope onto the stack
            _scopeStack.Push(new Dictionary<string, VariableInfo>());

            // Add the condition to the conditions list
            _conditions.Add(new ConditionObject(conditionName, new List<string> { binaryCondition }));
            Console.WriteLine($"Added standalone condition as part of new trigger: {conditionName}({binaryCondition})");

            // Process the actions under the standalone condition
            var parentIfStatement = condition.Parent as IfStatementSyntax;
            if (parentIfStatement != null && parentIfStatement.Statement is BlockSyntax block)
            {
                foreach (var statement in block.Statements)
                {
                    ProcessStatement(statement, ref actionCount, ref conditionCount, ref actionOffset, ref actionOffset);
                }
            }

            // Pop the scope from the stack
            EndScope();

            // Convert the counts and offsets to binary strings
            string conditionOffsetBinary = ConvertToBinary(conditionOffset, 9);
            string conditionCountBinary = ConvertToBinary(conditionCount, 10);
            string actionOffsetBinary = ConvertToBinary(actionOffset, 10);
            string actionCountBinary = ConvertToBinary(actionCount, 11);
            string triggerTypeBinary = ConvertToBinary((int)Enum.Parse(typeof(TriggerTypeEnum), triggerType), 3);
            string triggerAttributeBinary = ConvertToBinary((int)Enum.Parse(typeof(TriggerAttributeEnum), triggerAttribute), 3);

            // Concatenate the binary strings for the trigger
            string binaryTrigger = triggerTypeBinary + triggerAttributeBinary + conditionOffsetBinary + conditionCountBinary + actionOffsetBinary + actionCountBinary;
            _triggers.Add(new TriggerObject(triggerType, new List<string> { binaryTrigger }));
            Console.WriteLine($"Created new trigger for standalone condition: {triggerType} with binary data: {binaryTrigger}");

            // Return the index of the newly created trigger
            return _triggers.Count - 1;
        }



        private string ConvertPlayerTypeRefToBinary(string value, int bitSize)
        {
            // Define a dictionary to map input strings to their corresponding PlayerTypeRefEnum values
            var playerTypeRefMap = new Dictionary<string, PlayerTypeRefEnum>
            {
                { "Player", PlayerTypeRefEnum.Player },
                { "Player.Player", PlayerTypeRefEnum.PlayerPlayer },
                { "Object.Player", PlayerTypeRefEnum.ObjectPlayer },
                { "Team.Player", PlayerTypeRefEnum.TeamPlayer },
                { "current_player", PlayerTypeRefEnum.Player }
            };

            // Split the value to handle nested structures
            var parts = value.Split('.');

            // Initialize the final binary string
            string finalBinaryString = "";

            //Convert PlayerTypeRef to its binary representation
            if (playerTypeRefMap.TryGetValue(parts[0], out var playerTypeRefEnum))
            {
                finalBinaryString += Convert.ToString((int)playerTypeRefEnum, 2).PadLeft(2, '0');
                finalBinaryString += Convert.ToString((int)PlayerRefEnum.CurrentPlayer, 2).PadLeft(5, '0');
            }
            else
            {
                throw new ArgumentException($"Unsupported PlayerTypeRef: {parts[0]}");
            }


            // Ensure the final binary string is padded to the specified bit size
            return finalBinaryString.PadLeft(bitSize, '0');
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
            if (value is int intValue)
            {
                return Convert.ToString(intValue, 2).PadLeft(bitSize, '0');
            }
            else if (value is bool boolValue)
            {
                return boolValue ? "1".PadLeft(bitSize, '0') : "0".PadLeft(bitSize, '0');
            }
            else if (value is string strValue)
            {
                // Assuming string values are converted to their corresponding enum values
                if (Enum.TryParse(typeof(NameIndex), strValue, true, out var enumValue) && enumValue != null)
                {
                    return Convert.ToString((int)enumValue, 2).PadLeft(bitSize, '0');
                }
                return string.Join("", strValue.Select(c => Convert.ToString(c, 2).PadLeft(8, '0')));
            }
            else if (value is ObjectRef objectRefValue)
            {
                return Convert.ToString((int)objectRefValue, 2).PadLeft(bitSize, '0');
            }
            else if (value is ObjectType objectTypeValue)
            {
                return Convert.ToString((int)objectTypeValue, 2).PadLeft(bitSize, '0');
            }
            throw new InvalidOperationException("Unsupported type for binary conversion.");
        }

        public enum TriggerTypeEnum
        {
            Do = 0b000,
            Player = 0b001,
            RandomPlayer = 0b010,
            Team = 0b011,
            Object = 0b100,
            Labeled = 0b101,
            Unlabelled1 = 0b110,
            Unlabelled2 = 0b111
        }

        public enum TriggerAttributeEnum
        {
            OnTick = 0b000,
            OnCall = 0b001,
            OnInit = 0b010,
            OnLocalInit = 0b011,
            OnHostMigration = 0b100,
            OnObjectDeath = 0b101,
            OnLocal = 0b110,
            OnPregame = 0b111
        }


        public enum NameIndex
        {
            None = 0b00000000,
            MpBoneyardAIdleStart = 0b00000001,
            MpBoneyardAFlyIn = 0b00000010,
            MpBoneyardAIdleMid = 0b00000011,
            MpBoneyardAFlyOut = 0b00000100,
            MpBoneyardBFlyIn = 0b00000101,
            MpBoneyardBIdleMid = 0b00000110,
            MpBoneyardBFlyOut = 0b00000111,
            MpBoneyardBIdleStart = 0b00001000,
            MpBoneyardALeave1 = 0b00001001,
            MpBoneyardBLeave1 = 0b00001010,
            MpBoneyardBPickup = 0b00001011,
            MpBoneyardBIdlePickup = 0b00001100,
            MpBoneyardA = 0b00001101,
            MpBoneyardB = 0b00001110,
            Default = 0b00001111,
            Carter = 0b00010000,
            Jun = 0b00010001,
            Female = 0b00010010,
            Male = 0b00010011,
            Emile = 0b00010100,
            PlayerSkull = 0b00010101,
            Kat = 0b00010110,
            Minor = 0b00010111,
            Officer = 0b00011000,
            Ultra = 0b00011001,
            Space = 0b00011010,
            SpecOps = 0b00011011,
            General = 0b00011100,
            Zealot = 0b00011101,
            Mp = 0b00011110,
            Jetpack = 0b00011111,
            Gauss = 0b00100000,
            Troop = 0b00100001,
            Rocket = 0b00100010,
            Fr = 0b00100011,
            Pl = 0b00100100,
            Spire35Fp = 0b00100101,
            MpSpireFp = 0b00100110,
            MinusOne = 0b11111111
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

        private void ListActions()
        {
            foreach (var action in _actions)
            {
                Console.WriteLine(action);
            }
        }

        
    }



    public enum ObjectTypeRefEnum
    {
        ObjectRef = 0b000,
        PlayerObject = 0b001,
        ObjectObject = 0b010,
        TeamObject = 0b011,
        PlayerBiped = 0b100,
        PlayerPlayerBiped = 0b101,
        ObjectPlayerBiped = 0b110,
        TeamPlayerBiped = 0b111
    }

    public enum PlayerRefEnum
    {
        NoPlayer = 0b00000,
        Player0 = 0b00001,
        Player1 = 0b00010,
        Player2 = 0b00011,
        Player3 = 0b00100,
        Player4 = 0b00101,
        Player5 = 0b00110,
        Player6 = 0b00111,
        Player7 = 0b01000,
        Player8 = 0b01001,
        Player9 = 0b01010,
        Player10 = 0b01011,
        Player11 = 0b01100,
        Player12 = 0b01101,
        Player13 = 0b01110,
        Player14 = 0b01111,
        Player15 = 0b10000,
        GlobalPlayer0 = 0b10001,
        GlobalPlayer1 = 0b10010,
        GlobalPlayer2 = 0b10011,
        GlobalPlayer3 = 0b10100,
        GlobalPlayer4 = 0b10101,
        GlobalPlayer5 = 0b10110,
        GlobalPlayer6 = 0b10111,
        GlobalPlayer7 = 0b11000,
        CurrentPlayer = 0b11001,
        HudPlayer = 0b11010,
        HudTargetPlayer = 0b11011,
        ObjectKiller = 0b11100,
        Unlabelled1 = 0b11101,
        Unlabelled2 = 0b11110,
        Unlabelled3 = 0b11111
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

    public class  PlayerTypeRefParameter 
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
    internal static class CustomSyntaxKind
    {
        public static readonly SyntaxKind LocalKeyword = (SyntaxKind)1000; // Example value, replace with actual value
        public static readonly SyntaxKind HighKeyword = (SyntaxKind)1001; // Example value, replace with actual value
        public static readonly SyntaxKind LowKeyword = (SyntaxKind)1002; // Example value, replace with actual value
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
    public enum PlayerTypeRefEnum
    {
        Player = 0b00,
        PlayerPlayer = 0b01,
        ObjectPlayer = 0b10,
        TeamPlayer = 0b11
    }

    public enum ObjectType
    {
        Spartan = 0x000,
        Elite = 0x001,
        Monitor = 0x002,
        Flag = 0x003,
        Bomb = 0x004,
        Ball = 0x005,
        Area = 0x006,
        Stand = 0x007,
        Destination = 0x008,
        FragGrenade = 0x009,
        PlasmaGrenade = 0x00A,
        SpikeGrenade = 0x00B,
        FirebombGrenade = 0x00C,
        Dmr = 0x00D,
        AssaultRifle = 0x00E,
        PlasmaPistol = 0x00F,
        SpikeRifle = 0x010,
        Smg = 0x011,
        NeedleRifle = 0x012,
        PlasmaRepeater = 0x013,
        EnergySword = 0x014,
        Magnum = 0x015,
        Needler = 0x016,
        PlasmaRifle = 0x017,
        RocketLauncher = 0x018,
        Shotgun = 0x019,
        SniperRifle = 0x01A,
        BruteShot = 0x01B,
        BeamRifle = 0x01C,
        SpartanLaser = 0x01D,
        GravityHammer = 0x01E,
        Mauler = 0x01F,
        Flamethrower = 0x020,
        MissilePod = 0x021,
        Warthog = 0x022,
        Ghost = 0x023,
        Scorpion = 0x024,
        Wraith = 0x025,
        Banshee = 0x026,
        Mongoose = 0x027,
        Chopper = 0x028,
        Prowler = 0x029,
        Hornet = 0x02A,
        Stingray = 0x02B,
        HeavyWraith = 0x02C,
        Falcon = 0x02D,
        Sabre = 0x02E,
        SprintEquipment = 0x02F,
        JetPackEquipment = 0x030,
        ArmorLockEquipment = 0x031,
        PowerFistEquipment = 0x032,
        ActiveCamoEquipment = 0x033,
        AmmoPackEquipment = 0x034,
        SensorPackEquipment = 0x035,
        Revenant = 0x036,
        Pickup = 0x037,
        PrototypeCoveySniper = 0x038,
        TerritoryStatic = 0x039,
        CtfFlagReturnArea = 0x03A,
        CtfFlagSpawnPoint = 0x03B,
        RespawnZone = 0x03C,
        InvasionEliteBuy = 0x03D,
        InvasionEliteDrop = 0x03E,
        InvasionSlayer = 0x03F,
        InvasionSpartanBuy = 0x040,
        InvasionSpartanDrop = 0x041,
        InvasionSpawnController = 0x042,
        OddballBallSpawnPoint = 0x043,
        PlasmaLauncher = 0x044,
        FusionCoil = 0x045,
        UnscShieldGenerator = 0x046,
        CovShieldGenerator = 0x047,
        InitialSpawnPoint = 0x048,
        InvasionVehicleReq = 0x049,
        VehicleReqFloor = 0x04A,
        WallSwitch = 0x04B,
        HealthStation = 0x04C,
        ReqUnscLaser = 0x04D,
        ReqUnscDmr = 0x04E,
        ReqUnscRocket = 0x04F,
        ReqUnscShotgun = 0x050,
        ReqUnscSniper = 0x051,
        ReqCovyLauncher = 0x052,
        ReqCovyNeedler = 0x053,
        ReqCovySniper = 0x054,
        ReqCovySword = 0x055,
        ShockLoadout = 0x056,
        SpecialistLoadout = 0x057,
        AssassinLoadout = 0x058,
        InfiltratorLoadout = 0x059,
        WarriorLoadout = 0x05A,
        CombatantLoadout = 0x05B,
        EngineerLoadout = 0x05C,
        InfantryLoadout = 0x05D,
        OperatorLoadout = 0x05E,
        ReconLoadout = 0x05F,
        ScoutLoadout = 0x060,
        SeekerLoadout = 0x061,
        AirborneLoadout = 0x062,
        RangerLoadout = 0x063,
        ReqBuyBanshee = 0x064,
        ReqBuyFalcon = 0x065,
        ReqBuyGhost = 0x066,
        ReqBuyMongoose = 0x067,
        ReqBuyRevenant = 0x068,
        ReqBuyScorpion = 0x069,
        ReqBuyWarthog = 0x06A,
        ReqBuyWraith = 0x06B,
        Fireteam1RespawnZone = 0x06C,
        Fireteam2RespawnZone = 0x06D,
        Fireteam3RespawnZone = 0x06E,
        Fireteam4RespawnZone = 0x06F,
        Semi = 0x070,
        SoccerBall = 0x071,
        GolfBall = 0x072,
        GolfBallBlue = 0x073,
        GolfBallRed = 0x074,
        GolfClub = 0x075,
        GolfCup = 0x076,
        GolfTee = 0x077,
        Dice = 0x078,
        SpaceCrate = 0x079,
        EradicatorLoadout = 0x07A,
        SaboteurLoadout = 0x07B,
        GrenadierLoadout = 0x07C,
        MarksmanLoadout = 0x07D,
        Flare = 0x07E,
        GlowStick = 0x07F,
        EliteShot = 0x080,
        GrenadeLauncher = 0x081,
        PhantomApproach = 0x082,
        HologramEquipment = 0x083,
        EvadeEquipment = 0x084,
        UnscDataCore = 0x085,
        DangerZone = 0x086,
        TeleporterSender = 0x087,
        TeleporterReceiver = 0x088,
        Teleporter2Way = 0x089,
        DataCoreBeam = 0x08A,
        PhantomOverwatch = 0x08B,
        Longsword = 0x08C,
        InvisibleCubeOfDerek = 0x08D,
        PhantomScenery = 0x08E,
        PelicanScenery = 0x08F,
        Phantom = 0x090,
        Pelican = 0x091,
        ArmoryShelf = 0x092,
        CovResupplyCapsule = 0x093,
        CovyDropPod = 0x094,
        InvisibleMarker = 0x095,
        WeakRespawnZone = 0x096,
        WeakAntiRespawnZone = 0x097,
        PhantomDevice = 0x098,
        ResupplyCapsule = 0x099,
        ResupplyCapsuleOpen = 0x09A,
        WeaponBox = 0x09B,
        TechConsoleStationary = 0x09C,
        TechConsoleWall = 0x09D,
        MpCinematicCamera = 0x09E,
        InvisCovResupplyCapsule = 0x09F,
        CovPowerModule = 0x0A0,
        FlakCannon = 0x0A1,
        DropzoneBoundary = 0x0A2,
        ShieldDoorSmall = 0x0A3,
        ShieldDoorMedium = 0x0A4,
        ShieldDoorLarge = 0x0A5,
        DropShieldEquipment = 0x0A6,
        Machinegun = 0x0A7,
        MachinegunTurret = 0x0A8,
        PlasmaTurretWeapon = 0x0A9,
        MountedPlasmaTurret = 0x0AA,
        ShadeTurret = 0x0AB,
        CargoTruck = 0x0AC,
        CartElectric = 0x0AD,
        Forklift = 0x0AE,
        MilitaryTruck = 0x0AF,
        OniVan = 0x0B0,
        WarthogGunner = 0x0B1,
        WarthogGaussTurret = 0x0B2,
        WarthogRocketTurret = 0x0B3,
        ScorpionInfantryGunner = 0x0B4,
        FalconGrenadierLeft = 0x0B5,
        FalconGrenadierRight = 0x0B6,
        WraithInfantryTurret = 0x0B7,
        LandMine = 0x0B8,
        TargetLaser = 0x0B9,
        FfKillZone = 0x0BA,
        FfPlat1x1Flat = 0x0BB,
        ShadeAntiAir = 0x0BC,
        ShadeFlak = 0x0BD,
        ShadePlasma = 0x0BE,
        Killball = 0x0BF,
        FfLightRed = 0x0C0,
        FfLightBlue = 0x0C1,
        FfLightGreen = 0x0C2,
        FfLightOrange = 0x0C3,
        FfLightPurple = 0x0C4,
        FfLightYellow = 0x0C5,
        FfLightWhite = 0x0C6,
        FfLightFlashRed = 0x0C7,
        FfLightFlashYellow = 0x0C8,
        FxColorblind = 0x0C9,
        FxGloomy = 0x0CA,
        FxJuicy = 0x0CB,
        FxNova = 0x0CC,
        FxOldeTimey = 0x0CD,
        FxPenAndInk = 0x0CE,
        FxDusk = 0x0CF,
        FxGoldenHour = 0x0D0,
        FxEerie = 0x0D1,
        FfGrid = 0x0D2,
        InvisibleCubeOfAlarming1 = 0x0D3,
        InvisibleCubeOfAlarming2 = 0x0D4,
        SpawningSafe = 0x0D5,
        SpawningSafeSoft = 0x0D6,
        SpawningKill = 0x0D7,
        SpawningKillSoft = 0x0D8,
        PackageCabinet = 0x0D9,
        CovPowermoduleStand = 0x0DA,
        DlcCovenantBomb = 0x0DB,
        DlcInvasionHeavyShield = 0x0DC,
        DlcInvasionBombDoor = 0x0DD,
        LanAMf = 0x104
    }


}


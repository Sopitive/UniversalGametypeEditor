using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static UniversalGametypeEditor.ReadGametype;


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
                _players[index] = null;
                _availablePlayerIndices.Push(index);
                Console.WriteLine($"Recycled player index: {index}");
            }
        }

        public void RemoveObject(int index)
        {
            if (index >= 0 && index < _objects.Count)
            {
                _objects[index] = null;
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
        private Dictionary<string, int> _variableToIndexMap = new Dictionary<string, int>();
        private Stack<Dictionary<string, int>> _scopeStack = new Stack<Dictionary<string, int>>();
        private List<ActionObject> _actions = new List<ActionObject>();

        public ScriptCompiler() { }

        public void CompileScript(string script)
        {
            Compile(script);
            ListActions();
        }

        private int ConvertToObjectType(string typeName)
        {
            if (Enum.TryParse(typeof(ObjectType), typeName, true, out var result))
            {
                return (int)result;
            }
            throw new ArgumentException($"Invalid object type: {typeName}");
        }


        public void Compile(string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // Traverse the syntax tree
            foreach (var member in root.Members)
            {
                ProcessMember(member);
            }
        }

        private void ProcessMember(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax method)
            {
                Console.WriteLine($"Method: {method.Identifier.Text}");
                if (method.Body != null)
                {
                    _scopeStack.Push(new Dictionary<string, int>());
                    foreach (var statement in method.Body.Statements)
                    {
                        ProcessStatement(statement);
                    }
                    EndScope();
                }
            }
            else if (member is ClassDeclarationSyntax classDecl)
            {
                Console.WriteLine($"Class: {classDecl.Identifier.Text}");
                foreach (var classMember in classDecl.Members)
                {
                    ProcessMember(classMember);
                }
            }
            else if (member is FieldDeclarationSyntax field)
            {
                Console.WriteLine($"Field: {field.Declaration.Variables.First().Identifier.Text}");
                // Handle field declarations if needed
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                Console.WriteLine($"Property: {property.Identifier.Text}");
                _scopeStack.Push(new Dictionary<string, int>());
                foreach (var accessor in property.AccessorList.Accessors)
                {
                    if (accessor.Body != null)
                    {
                        foreach (var statement in accessor.Body.Statements)
                        {
                            ProcessStatement(statement);
                        }
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        ProcessExpression(accessor.ExpressionBody.Expression);
                    }
                }
                EndScope();
            }
            else if (member is ConstructorDeclarationSyntax constructor)
            {
                Console.WriteLine($"Constructor: {constructor.Identifier.Text}");
                if (constructor.Body != null)
                {
                    _scopeStack.Push(new Dictionary<string, int>());
                    foreach (var statement in constructor.Body.Statements)
                    {
                        ProcessStatement(statement);
                    }
                    EndScope();
                }
            }
            else if (member is GlobalStatementSyntax globalStatement)
            {
                Console.WriteLine("Global Statement");
                _scopeStack.Push(new Dictionary<string, int>()); // Ensure a new scope is pushed
                ProcessStatement(globalStatement.Statement);
                EndScope();
            }
            else
            {
                Console.WriteLine($"Unhandled member type: {member.GetType().Name}");
            }
        }

        private void ProcessExpression(ExpressionSyntax expression)
        {
            if (expression is AssignmentExpressionSyntax assignment)
            {
                // Handle assignment expressions
                Console.WriteLine($"Assignment: {assignment}");
                ProcessAssignment(assignment);
            }
            else if (expression is InvocationExpressionSyntax invocation)
            {
                // Handle method invocations
                Console.WriteLine($"Invocation: {invocation}");
                ProcessInvocation(invocation);
            }
            // Add more cases for other expression types if needed
        }

        private void ProcessStatement(StatementSyntax statement)
        {
            if (_scopeStack.Count == 0)
            {
                throw new InvalidOperationException("Scope stack is empty.");
            }

            if (statement is LocalDeclarationStatementSyntax localDeclaration)
            {
                foreach (var variable in localDeclaration.Declaration.Variables)
                {
                    string varName = variable.Identifier.Text;
                    string type = localDeclaration.Declaration.Type.ToString();
                    if (type == "Player")
                    {
                        var player = _entityManager.CreatePlayer(varName);
                        int index = _entityManager.GetPlayerIndex(player);
                        _variableToIndexMap[varName] = index;
                        _scopeStack.Peek()[varName] = index;
                        Console.WriteLine($"Initialized Player variable '{varName}' at global.player[{index}]");
                    }
                    else if (type == "Object")
                    {
                        var initializer = variable.Initializer?.Value as InvocationExpressionSyntax;
                        if (initializer != null)
                        {
                            // Add the variable to the map before processing the invocation
                            var gameObject = _entityManager.CreateObject(varName);
                            int index = _entityManager.GetObjectIndex(gameObject);
                            _variableToIndexMap[varName] = index;
                            _scopeStack.Peek()[varName] = index;
                            Console.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}]");

                            // Process the invocation with the varOut parameter
                            ProcessInvocation(initializer, varName);
                        }
                        else
                        {
                            var gameObject = _entityManager.CreateObject(varName);
                            int index = _entityManager.GetObjectIndex(gameObject);
                            _variableToIndexMap[varName] = index;
                            _scopeStack.Peek()[varName] = index;
                            Console.WriteLine($"Initialized Object variable '{varName}' at global.object[{index}]");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unhandled local variable type: {type}");
                    }
                }
            }
            else if (statement is ExpressionStatementSyntax expressionStatement)
            {
                ProcessExpression(expressionStatement.Expression);
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                Console.WriteLine($"If Statement: {ifStatement.Condition}");
                // Process the condition and the body of the if statement
            }
            else if (statement is LocalFunctionStatementSyntax localFunction)
            {
                Console.WriteLine($"Local Function: {localFunction.Identifier.Text}");
                if (localFunction.Body != null)
                {
                    _scopeStack.Push(new Dictionary<string, int>());
                    foreach (var localStatement in localFunction.Body.Statements)
                    {
                        ProcessStatement(localStatement);
                    }
                    EndScope();
                }
            }
            else if (statement is ReturnStatementSyntax returnStatement)
            {
                Console.WriteLine($"Return Statement: {returnStatement.Expression}");
                // Process the return statement if needed
            }
            else if (statement is BlockSyntax block)
            {
                foreach (var blockStatement in block.Statements)
                {
                    ProcessStatement(blockStatement);
                }
            }
            else
            {
                Console.WriteLine($"Unhandled statement type: {statement.GetType().Name}");
            }
        }



        private void ProcessAssignment(AssignmentExpressionSyntax assignment)
        {
            var left = assignment.Left as IdentifierNameSyntax;
            var right = assignment.Right as InvocationExpressionSyntax;

            if (left != null && right != null)
            {
                string varName = left.Identifier.Text;
                Console.WriteLine($"Processing assignment: {varName} = {right}");
                ProcessInvocation(right, varName);
            }
        }

        private void ProcessInvocation(InvocationExpressionSyntax invocation, string varOut = null)
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

                        if (param.Name == "var_out" && varOut != null)
                        {
                            hasVarOut = 1;
                            if (_variableToIndexMap.TryGetValue(varOut, out int index))
                            {
                                // Translate the index to a global number and then to ObjectRef
                                ObjectRef objectRef = (ObjectRef)Enum.Parse(typeof(ObjectRef), $"GlobalObject{index}");
                                parameters.Add(ConvertToBinary(objectRef, bitSize));
                            }
                            else
                            {
                                throw new Exception($"Variable '{varOut}' not found in the variable map.");
                            }
                        }
                        else if (i < arguments.Count + hasVarOut)
                        {
                            if (param.Name == "type")
                            {
                                string typeName = arguments[i].ToString().Trim('"');
                                int typeValue = ConvertToObjectType(typeName);
                                parameters.Add(ConvertToBinary(typeValue, bitSize));
                            }
                            else if (param.Name == "at")
                            {
                                string atValue = arguments[i - hasVarOut].ToString().Trim('"');
                                string binaryRepresentation = BitEncoder.EncodeObjectTypeRef(atValue);
                                parameters.Add(binaryRepresentation);
                            }
                            else if (param.Name == "label")
                            {
                                string labelValue = arguments[i - hasVarOut].ToString().Trim('"');
                                if (string.IsNullOrEmpty(labelValue) || labelValue.Equals("none", StringComparison.OrdinalIgnoreCase))
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
                                bool boolValue = bool.Parse(arguments[i - hasVarOut].ToString());
                                parameters.Add(ConvertToBinary(boolValue, bitSize));
                            }
                            else
                            {
                                // Convert other parameters to binary
                                string paramValue = arguments[i - hasVarOut].ToString().Trim('"');
                                parameters.Add(ConvertToBinary(paramValue, bitSize));
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
                    _actions.Add(new ActionObject(actionName, new List<string> { binaryAction }));
                    Console.WriteLine($"Added action: {actionName}({binaryAction})");
                }
                else
                {
                    Console.WriteLine($"Action definition not found for: {actionName}");
                }
            }
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
                        if (_variableToIndexMap[kvp.Key] < _entityManager.GetPlayerIndex(new Player("")))
                        {
                            _entityManager.RemovePlayer(kvp.Value);
                        }
                        else
                        {
                            _entityManager.RemoveObject(kvp.Value);
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
                if (Enum.TryParse(typeof(NameIndex), strValue, true, out var enumValue))
                {
                    return Convert.ToString((int)enumValue, 2).PadLeft(bitSize, '0');
                }
                return string.Join("", strValue.Select(c => Convert.ToString(c, 2).PadLeft(8, '0')));
            }
            else if (value is ObjectRef objectRefValue)
            {
                return Convert.ToString((int)objectRefValue, 2).PadLeft(bitSize, '0');
            }
            throw new InvalidOperationException("Unsupported type for binary conversion.");
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

    public class GameObject
    {
        public string Name { get; set; }

        public GameObject(string name)
        {
            Name = name;
        }
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


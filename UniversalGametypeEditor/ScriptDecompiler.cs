
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
        public static string Decompile(string scriptBits)
        {
            if (string.IsNullOrWhiteSpace(scriptBits))
                return string.Empty;

            var br = new BitReader(scriptBits);

            // Header counts (Reach)
            int conditionCount = br.ReadUInt(10);
            int actionCount = br.ReadUInt(11);
            int triggerCount = br.ReadUInt(8);

            // Parse Conditions
            var conditions = new List<ConditionEntry>(conditionCount);
            for (int i = 0; i < conditionCount; i++)
                conditions.Add(ParseCondition(br));

            // Parse Actions
            var actions = new List<ActionEntry>(actionCount);
            for (int i = 0; i < actionCount; i++)
                actions.Add(ParseAction(br));

            // Parse Triggers
            var triggers = new List<TriggerEntry>(triggerCount);
            for (int i = 0; i < triggerCount; i++)
                triggers.Add(ParseTrigger(br));

            // Remaining bits after triggers belong to other Reach sections (globals, options, etc.) bits then global vars, etc. We don't decode that (yet).
            int remaining = br.RemainingBits;

            // Render
            var sb = new StringBuilder(64 * 1024);
            sb.AppendLine($"// Decompiled Megalo Script (Reach)");
            sb.AppendLine($"// Conditions: {conditionCount}, Actions: {actionCount}, Triggers: {triggerCount}");
            sb.AppendLine($"// Remaining bits after triggers: {remaining}");
            sb.AppendLine();

            for (int i = 0; i < triggers.Count; i++)
            {
                RenderTrigger(sb, triggers[i], i, conditions, actions);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ---- Parsing -------------------------------------------------------

        private static ConditionEntry ParseCondition(BitReader br)
        {
            // Header is fixed (per your compiler):
            //  - conditionId: 5
            //  - not:         1
            //  - orSequence:  9
            //  - actionOff:  10
            int id = br.ReadUInt(5);
            bool isNot = br.ReadUInt(1) != 0;
            int orSeq = br.ReadUInt(9);
            int actionOffset = br.ReadUInt(10);

            var def = MegaloLookup.GetCondition(id);

            var args = new List<string>();
            if (def.HasValue)
            {
                foreach (var p in def.Value.Params)
                    args.Add(ReadParam(br, p.TypeRef));
            }

            return new ConditionEntry(id, def?.Name ?? $"Cond{id}", isNot, orSeq, actionOffset, args);
        }

        private static ActionEntry ParseAction(BitReader br)
        {
            int id = br.ReadUInt(7);

            var def = MegaloLookup.GetAction(id);
            var args = new List<string>();

            if (def.HasValue)
            {
                foreach (var p in def.Value.Params)
                    args.Add(ReadParam(br, p.TypeRef));
            }

            return new ActionEntry(id, def?.Name ?? $"Act{id}", args);
        }

        private static TriggerEntry ParseTrigger(BitReader br)
        {
            // Reach trigger record (older reader + mpvr):
            //  - TriggerTypeEnum:      3
            //  - TriggerAttributeEnum: 4
            //  - conditionOffset:     10
            //  - conditionCount:      10
            //  - actionOffset:        11
            //  - actionCount:         11
            //  - unknown1:             8
            //  - unknown2:             8
            int typeVal = br.ReadUInt(3);
            int attrVal = br.ReadUInt(4);
            int condOffset = br.ReadUInt(10);
            int condCount = br.ReadUInt(10);
            int actOffset = br.ReadUInt(11);
            int actCount = br.ReadUInt(11);
            int unk1 = br.ReadUInt(8);
            int unk2 = br.ReadUInt(8);

            string typeName = EnumNameOrValue("TriggerType", typeVal) ?? typeVal.ToString();
            string attrName = EnumNameOrValue("TriggerAttribute", attrVal) ?? attrVal.ToString();

            return new TriggerEntry(typeVal, attrVal, typeName, attrName, condOffset, condCount, actOffset, actCount, unk1, unk2);
        }

        // ---- Rendering -----------------------------------------------------

        private static void RenderTrigger(StringBuilder sb, TriggerEntry t, int index, List<ConditionEntry> allConds, List<ActionEntry> allActs)
        {
            sb.AppendLine($"Trigger {t.TypeName}()  // attr: {t.AttributeName}");
            sb.AppendLine("{");
            sb.AppendLine($"    // cond: off={t.ConditionOffset}, count={t.ConditionCount}");
            sb.AppendLine($"    // act : off={t.ActionOffset}, count={t.ActionCount}");
            sb.AppendLine($"    // unk : {t.Unknown1}, {t.Unknown2}");
            sb.AppendLine();

            // Conditions block
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
                    sb.Append("(").Append(string.Join(", ", c.Args)).Append(")");
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
                RenderActionStatement(sb, a, indent: 1);
                sb.AppendLine();
            }

            sb.AppendLine("}");
        }

        private static void RenderActionStatement(StringBuilder sb, ActionEntry a, int indent)
        {
            if (a.IsInline)
            {
                sb.AppendLine($"// inline (id={a.Id}) condOff={a.InlineCondOffset} condCount={a.InlineCondCount} actOff={a.InlineActOffset} actCount={a.InlineActCount}");
                return;
            }

            sb.Append(a.Name);
            sb.Append("(");
            sb.Append(string.Join(", ", a.Args));
            sb.Append(");");
        }

        // ---- Param decoding ------------------------------------------------

        private static string ReadParam(BitReader br, string typeRef)
        {
            if (string.IsNullOrWhiteSpace(typeRef))
                return "0";

            // Normalize prefixes
            string tr = typeRef.Trim();

            // Common primitive/composite "Enumref:" types used by your compiler.
            if (Eq(tr, "Enumref:Bool")) return br.ReadUInt(1) != 0 ? "true" : "false";
            if (Eq(tr, "Enumref:Vector3")) return ReadVector3(br);
            if (Eq(tr, "Enumref:LabelRef")) return ReadLabelRef(br);

            if (Eq(tr, "Enumref:TeamTypeRef")) return ReadTeamTypeRef(br);
            if (Eq(tr, "Enumref:TimerTypeRef")) return ReadTimerTypeRef(br);
            if (Eq(tr, "Enumref:PlayerTypeRef")) return ReadPlayerTypeRef(br);
            if (Eq(tr, "Enumref:ObjectTypeRef")) return ReadObjectTypeRef(br);
            if (Eq(tr, "Enumref:NumericTypeRef")) return ReadNumericTypeRef(br);

            if (Eq(tr, "Enumref:VarType")) return ReadVarType(br);

            // Generic Enumref:<EnumName> (bit-size derived from enum max value)
            if (tr.StartsWith("Enumref:", StringComparison.OrdinalIgnoreCase))
            {
                string enumName = tr.Substring("Enumref:".Length).Trim();
                int bits = EnumBitsByName(enumName);
                int val = br.ReadUInt(bits);

                string? name = EnumNameOrValue(enumName, val);
                return name ?? val.ToString();
            }

            // Unknown: fall back to 1 bit so we don't infinite-loop, but also mark it.
            // NOTE: You can extend this when you hit new TypeRef strings.
            int fallback = br.ReadUInt(1);
            return $"{fallback}/*{tr}*/";
        }

        private static string ReadVector3(BitReader br)
        {
            // Your compiler uses 8-bit signed X/Y/Z (ConvertVector3ToBinary).
            int x = br.ReadSInt(8);
            int y = br.ReadSInt(8);
            int z = br.ReadSInt(8);
            return $"{x},{y},{z}";
        }

        private static string ReadLabelRef(BitReader br)
        {
            // 1 bit => none/default, else 0 + 4 bits label id
            int noneBit = br.ReadUInt(1);
            if (noneBit == 1)
                return "none";
            int id = br.ReadUInt(4);
            return id.ToString();
        }

        private static string ReadTeamTypeRef(BitReader br)
        {
            // 2-bit type + 5-bit ref
            _ = br.ReadUInt(2); // type (unused in your current compiler)
            int refVal = br.ReadUInt(5);

            if (refVal == 0) return "NoTeam";

            // Prefer TeamRef enum name if available; else treat as GlobalTeam
            string? n = EnumNameOrValue("TeamRef", refVal);
            if (!string.IsNullOrEmpty(n))
                return n!;

            return $"GlobalTeam{refVal - 1}";
        }

        private static string ReadTimerTypeRef(BitReader br)
        {
            _ = br.ReadUInt(2); // type (unused)
            int refVal = br.ReadUInt(5);
            if (refVal == 0) return "NoTimer";
            // Your compiler accepts numeric timer refs; emitting numeric is safest.
            return refVal.ToString();
        }

        private static string ReadPlayerTypeRef(BitReader br)
        {
            _ = br.ReadUInt(2); // type
            int refVal = br.ReadUInt(5);
            if (refVal == 0) return "NoPlayer";

            string? n = EnumNameOrValue("PlayerRefEnum", refVal);
            if (!string.IsNullOrEmpty(n))
            {
                // Your compiler expects "current_player" token (not "CurrentPlayer")
                if (n!.Equals("CurrentPlayer", StringComparison.OrdinalIgnoreCase))
                    return "current_player";
                return n!;
            }

            return $"GlobalPlayer{refVal - 1}";
        }

        private static string ReadObjectTypeRef(BitReader br)
        {
            int typeVal = br.ReadUInt(3);
            int param = br.ReadUInt(5);

            string? typeName = EnumNameOrValue("ObjectTypeRefEnum", typeVal) ?? EnumNameOrValue("ObjectTypeRef", typeVal);
            typeName ??= $"ObjType{typeVal}";

            // Handle common cases used by your compiler.
            if (typeName.Equals("ObjectRef", StringComparison.OrdinalIgnoreCase))
            {
                if (param == 0) return "NoObject";

                // If this equals ObjectRef.CurrentObject, return current_object
                string? objRefName = EnumNameOrValue("ObjectRef", param);
                if (!string.IsNullOrEmpty(objRefName) && objRefName!.Equals("CurrentObject", StringComparison.OrdinalIgnoreCase))
                    return "current_object";

                // Otherwise treat as GlobalObject (compiler encodes index+1)
                return $"GlobalObject{param - 1}";
            }

            if (typeName.Equals("PlayerBiped", StringComparison.OrdinalIgnoreCase))
            {
                // Param is a PlayerRefEnum value.
                string p = ReadPlayerRefToken(param);
                return $"{p}.biped";
            }

            // Fallback
            return $"{typeName}({param})";
        }

        private static string ReadPlayerRefToken(int refVal)
        {
            if (refVal == 0) return "NoPlayer";
            string? n = EnumNameOrValue("PlayerRefEnum", refVal);
            if (!string.IsNullOrEmpty(n))
            {
                if (n!.Equals("CurrentPlayer", StringComparison.OrdinalIgnoreCase))
                    return "current_player";
                return n!;
            }
            return $"GlobalPlayer{refVal - 1}";
        }

        private static string ReadNumericTypeRef(BitReader br)
        {
            int typeVal = br.ReadUInt(6);

            int globalNumberEnumVal = EnumValueByName("NumericTypeRefEnum", "GlobalNumber") ?? 4;
            int int16EnumVal = EnumValueByName("NumericTypeRefEnum", "Int16") ?? 0;

            if (typeVal == globalNumberEnumVal)
            {
                int idx = br.ReadUInt(4); // matches your compiler's GlobalNumber encoding
                return $"GlobalNumber{idx}";
            }

            // Default: signed int16 literal
            int v = br.ReadSInt(16);
            return v.ToString();
        }

        private static string ReadVarType(BitReader br)
        {
            // Your compiler's VarType is a 3-bit kind + a payload depending on kind.
            int kind = br.ReadUInt(3);

            switch (kind)
            {
                case 0: // NumericVar
                    return ReadNumericTypeRef(br);
                case 1: // PlayerVar
                    return ReadPlayerTypeRef(br);
                case 2: // ObjectVar
                    return ReadObjectTypeRef(br);
                case 3: // TeamVar
                    return ReadTeamTypeRef(br);
                case 4: // TimerVar
                    return ReadTimerTypeRef(br);
                default:
                    // Unknown kind; attempt to avoid desync by returning a marker.
                    return $"VarKind{kind}";
            }
        }

        // ---- Lookup helpers ------------------------------------------------

        private static class MegaloLookup
        {
            private static readonly Dictionary<int, MegaloAction> _actionsById;
            private static readonly Dictionary<int, MegaloCondition> _conditionsById;

            static MegaloLookup()
            {
                _actionsById = MegaloTables.Actions.ToDictionary(a => a.Id, a => a);
                _conditionsById = MegaloTables.Conditions.ToDictionary(c => c.Id, c => c);
            }

            public static MegaloAction? GetAction(int id)
                => _actionsById.TryGetValue(id, out var a) ? a : null;

            public static MegaloCondition? GetCondition(int id)
                => _conditionsById.TryGetValue(id, out var c) ? c : null;
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
            public readonly List<string> Args;

            public ConditionEntry(int id, string name, bool isNot, int orSequence, int actionOffset, List<string> args)
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
            public readonly List<string> Args;

            public readonly bool IsInline;
            public readonly int InlineCondOffset;
            public readonly int InlineCondCount;
            public readonly int InlineActOffset;
            public readonly int InlineActCount;

            public ActionEntry(int id, string name, List<string> args)
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
                Args = new List<string>();

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
                int unk2)
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
            }
        }


        // ---- Bit reader ----------------------------------------------------

        private sealed class BitReader
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

using System;
using System.Collections.Generic;
using System.Text;
using static UniversalGametypeEditor.ScriptCompiler;

namespace UniversalGametypeEditor
{
    public class ActionParameter
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public int Bits { get; set; }
        public object DefaultValue { get; set; }

        public ActionParameter(string name, Type parameterType, object defaultValue = null, int bits = 0)
        {
            Name = name;
            ParameterType = parameterType;
            Bits = bits;
            DefaultValue = defaultValue;
        }
    }

    public class ActionDefinition
    {
        public int Id { get; set; } // Add Id property
        public string Name { get; set; }
        public List<ActionParameter> Parameters { get; set; }

        public ActionDefinition(int id, string name, List<ActionParameter> parameters)
        {
            Id = id; // Assign Id
            Name = name;
            Parameters = parameters;
        }
    }

    public static class ActionDefinitions
    {
        public static List<ActionDefinition> ValidActions = new List<ActionDefinition>
            {
                new ActionDefinition(2, "CreateObject", new List<ActionParameter>
                {
                    new ActionParameter("type", typeof(string), null, 12),
                    new ActionParameter("var_out", typeof(string), "NoObject", 5),
                    new ActionParameter("placeat", typeof(ObjectTypeRef)),
                    new ActionParameter("label", typeof(string), 1, 1),
                    new ActionParameter("suppress", typeof(bool), 0, 1), // Default value
                    new ActionParameter("garbage_collect", typeof(bool), 0, 1), // Default value
                    new ActionParameter("absolute_orientation", typeof(bool), 0, 1), // Default value
                    new ActionParameter("x", typeof(int), 0, 8), // Default value
                    new ActionParameter("y", typeof(int), 0, 8), // Default value
                    new ActionParameter("z", typeof(int), 0, 8), // Default value
                    new ActionParameter("variant", typeof(string), "none", 8) // Default value
                }),
                new ActionDefinition(33, "Attach", new List<ActionParameter>
                {
                    new ActionParameter("child", typeof(ObjectTypeRef)),
                    new ActionParameter("parent", typeof(ObjectTypeRef)),
                    new ActionParameter("x", typeof(int), 0, 8), // Default value
                    new ActionParameter("y", typeof(int), 0, 8), // Default value
                    new ActionParameter("z", typeof(int), 0, 8), // Default value
                    new ActionParameter("reference", typeof(bool), 1, 1)
                }),
                new ActionDefinition(34, "Detach", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(ObjectTypeRef))
                }),
                new ActionDefinition(23, "Kill", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(ObjectTypeRef), null, 5),
                    new ActionParameter("delete", typeof(bool), 0, 1)
                }),
                new ActionDefinition(28, "GetSpeed", new List<ActionParameter>
                {
                    new ActionParameter("var_out", typeof(NumericTypeRef)),
                    new ActionParameter("object", typeof(ObjectTypeRef))
                }),
                new ActionDefinition(5, "GetDistanceTo", new List<ActionParameter>
                {
                    new ActionParameter("object1", typeof(ObjectTypeRef)),
                    new ActionParameter("object2", typeof(ObjectTypeRef)),
                    new ActionParameter("var_out", typeof(string))
                }),
                new ActionDefinition(6, "set", new List<ActionParameter>
                {
                    new ActionParameter("leftHandSide", typeof(string)),
                    new ActionParameter("operator", typeof(string)),
                    new ActionParameter("rightHandSide", typeof(string))
                }),
                new ActionDefinition(94, "DropWeapon", new List<ActionParameter>
                {
                    new ActionParameter("biped", typeof(ObjectTypeRef)),
                    new ActionParameter("slot", typeof(bool), 1, 1),
                    new ActionParameter("delete", typeof(bool), 1, 0)
                }),
                new ActionDefinition(8, "SetInvincible", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("invincible", typeof(bool), 0, 1),
                }),
                new ActionDefinition(9, "Scale", new List<ActionParameter>
                {
                    new ActionParameter("target", typeof(ObjectTypeRef)),
                    new ActionParameter("scale", typeof(int))
                }),
                new ActionDefinition(10, "set_waypoint_visibility", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(string)),
                    new ActionParameter("player_set", typeof(string)),
                    new ActionParameter("is_visible", typeof(bool))
                }),
                new ActionDefinition(11, "apply_traits", new List<ActionParameter>
                {
                    new ActionParameter("player", typeof(string)),
                    new ActionParameter("traits", typeof(string))
                }),
                new ActionDefinition(42, "SetUnit", new List<ActionParameter>
                {
                    new ActionParameter("player", typeof(PlayerTypeRef)),
                    new ActionParameter("unit", typeof(ObjectTypeRef))
                }),
                new ActionDefinition(13, "CopyRotation", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(ObjectTypeRef)),
                    new ActionParameter("object", typeof(ObjectTypeRef)),
                    new ActionParameter("all_axes", typeof(bool), true, 1)
                }),
                new ActionDefinition(14, "Lookat", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(ObjectTypeRef)),
                    new ActionParameter("object", typeof(ObjectTypeRef)),
                    new ActionParameter("x", typeof(int), 0, 8),
                    new ActionParameter("y", typeof(int), 0, 8),
                    new ActionParameter("z", typeof(int), 0, 8)
                }),
                new ActionDefinition(3, "Delete", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(ObjectTypeRef))
                }),
                new ActionDefinition(20, "CallTrigger", new List<ActionParameter>
                {
                    new ActionParameter("trigger", typeof(int), 0, 9)
                }),
                new ActionDefinition(99, "Inline", new List<ActionParameter>
                {
                    new ActionParameter("condition_offset", typeof(int), 0, 9),
                    new ActionParameter("condition_count", typeof(int), 0, 10),
                    new ActionParameter("action_offset", typeof(int), 0, 10),
                    new ActionParameter("action_count", typeof(int), 0, 11)
                }),
                new ActionDefinition(83, "GetWeapon", new List<ActionParameter>
                {
                    new ActionParameter("player", typeof(PlayerTypeRef)),
                    new ActionParameter("slot", typeof(bool), 1, 1),
                    new ActionParameter("var_out", typeof(ObjectTypeRef)),
                }),
                new ActionDefinition(83, "EndRound", new List<ActionParameter>
                {

                }),
            };
    }

}

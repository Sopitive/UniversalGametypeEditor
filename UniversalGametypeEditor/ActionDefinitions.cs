using System;
using System.Collections.Generic;
using System.Text;

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
                new ActionDefinition(2, "create_object", new List<ActionParameter>
                {
                    new ActionParameter("type", typeof(string), null, 12),
                    new ActionParameter("var_out", typeof(string), "NoObject", 5),
                    new ActionParameter("at", typeof(string)),
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
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("x", typeof(int), 0, 8), // Default value
                    new ActionParameter("y", typeof(int), 0, 8), // Default value
                    new ActionParameter("z", typeof(int), 0, 8), // Default value
                    new ActionParameter("reference", typeof(bool), 0, 1)
                }),
                new ActionDefinition(2, "detach", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(string))
                }),
                new ActionDefinition(23, "Kill", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string), null, 5),
                    new ActionParameter("delete", typeof(bool), 0, 1)
                }),
                new ActionDefinition(4, "get_speed", new List<ActionParameter>
                {
                    new ActionParameter("var_out", typeof(string)),
                    new ActionParameter("object", typeof(string))
                }),
                new ActionDefinition(5, "get_distance", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("object2", typeof(string))
                }),
                new ActionDefinition(6, "set", new List<ActionParameter>
                {
                    new ActionParameter("leftHandSide", typeof(string)),
                    new ActionParameter("operator", typeof(string)),
                    new ActionParameter("rightHandSide", typeof(string))
                }),
                new ActionDefinition(7, "DropWeapon", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("slot", typeof(bool), 0, 1),
                    new ActionParameter("delete", typeof(bool), 0, 0)
                }),
                new ActionDefinition(8, "SetInvincible", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string)),
                    new ActionParameter("invincible", typeof(bool), 0, 1),
                }),
                new ActionDefinition(9, "Scale", new List<ActionParameter>
                {
                    new ActionParameter("at", typeof(string)),
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
                new ActionDefinition(12, "set_biped", new List<ActionParameter>
                {
                    new ActionParameter("player", typeof(string)),
                    new ActionParameter("biped", typeof(string))
                }),
                new ActionDefinition(13, "copy_rotation_from", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(string)),
                    new ActionParameter("object", typeof(string)),
                    new ActionParameter("all_axes", typeof(bool), true)
                }),
                new ActionDefinition(14, "face_toward", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(string)),
                    new ActionParameter("object", typeof(string)),
                    new ActionParameter("x", typeof(int), 0),
                    new ActionParameter("y", typeof(int), 0),
                    new ActionParameter("z", typeof(int), 0)
                }),
                new ActionDefinition(15, "delete", new List<ActionParameter>
                {
                    new ActionParameter("object", typeof(string))
                }),
            };
    }

}

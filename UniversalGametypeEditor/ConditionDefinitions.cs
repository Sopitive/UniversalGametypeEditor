using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    public class ConditionParameter
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public int Bits { get; set; }
        public object DefaultValue { get; set; }

        public ConditionParameter(string name, Type parameterType, object defaultValue = null, int bits = 0)
        {
            Name = name;
            ParameterType = parameterType;
            Bits = bits;
            DefaultValue = defaultValue;
        }
    }

    public class ConditionDefinition
    {
        public int Id { get; set; } // Add Id property
        public string Name { get; set; }
        public List<ConditionParameter> Parameters { get; set; }

        public ConditionDefinition(int id, string name, List<ConditionParameter> parameters)
        {
            Id = id; // Assign Id
            Name = name;
            Parameters = parameters;
        }
    }

    public static class ConditionDefinitions
    {
        public static List<ConditionDefinition> ValidConditions = new List<ConditionDefinition>
        {
            new ConditionDefinition(0, "None", new List<ConditionParameter>()),
            new ConditionDefinition(1, "Megl.If", new List<ConditionParameter>
            {
                new ConditionParameter("Var1", typeof(string)),
                new ConditionParameter("Var2", typeof(string)),
                new ConditionParameter("Operator", typeof(string))
            }),
            new ConditionDefinition(17, "Game.IsForge", new List<ConditionParameter>()),
            new ConditionDefinition(3, "Player.WasKilled", new List<ConditionParameter>
            {
                new ConditionParameter("PlayerRef", typeof(string)),
                new ConditionParameter("DeathFlags", typeof(string))
            }),
            new ConditionDefinition(9, "Player.IsLeader", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string))
            }),
            new ConditionDefinition(10, "Player.AssistedKill", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string)),
                new ConditionParameter("KilledPlayer", typeof(string))
            }),
            new ConditionDefinition(12, "Player.IsAlive", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string))
            }),
            new ConditionDefinition(14, "Player.IsSpartan", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string))
            }),
            new ConditionDefinition(15, "Player.IsElite", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string))
            }),
            new ConditionDefinition(16, "Player.IsMonitor", new List<ConditionParameter>
            {
                new ConditionParameter("Player", typeof(string))
            }),
            new ConditionDefinition(2, "Obj.IsInBoundary", new List<ConditionParameter>
            {
                new ConditionParameter("Object", typeof(string)),
                new ConditionParameter("Boundary", typeof(string))
            }),
            new ConditionDefinition(6, "ObjectIsType", new List<ConditionParameter>
            {
                new ConditionParameter("Object", typeof(ObjectTypeRef)),
                new ConditionParameter("Type1", typeof(ObjectType), null, 12)
            }),
            new ConditionDefinition(8, "Obj.IsOuttaBounds", new List<ConditionParameter>
            {
                new ConditionParameter("Object", typeof(string))
            }),
            new ConditionDefinition(11, "Obj.HasLabel", new List<ConditionParameter>
            {
                new ConditionParameter("Object", typeof(string)),
                new ConditionParameter("Label", typeof(string))
            }),
            new ConditionDefinition(13, "Obj.EquipInUse", new List<ConditionParameter>
            {
                new ConditionParameter("Equipment", typeof(string))
            }),
            new ConditionDefinition(4, "Team.Disposition", new List<ConditionParameter>
            {
                new ConditionParameter("Team1", typeof(string)),
                new ConditionParameter("Team2", typeof(string)),
                new ConditionParameter("Allegiance", typeof(string))
            }),
            new ConditionDefinition(7, "Team.HasPlayers", new List<ConditionParameter>
            {
                new ConditionParameter("Team", typeof(string))
            }),
            new ConditionDefinition(5, "Timer.IsZero", new List<ConditionParameter>
            {
                new ConditionParameter("Timer", typeof(string))
            }),
            // Add more conditions as needed
        };
    }

}

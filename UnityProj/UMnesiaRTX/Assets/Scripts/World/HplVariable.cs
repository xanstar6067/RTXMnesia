using System;
using System.Collections.Generic;

namespace RTXMnesia.World
{
    /// <summary>A user variable from a .map or .ent file (&lt;Var Name="..." Value="..."/&gt;), kept as text like in HPL2.</summary>
    [Serializable]
    public struct HplVariable
    {
        public string name;
        public string value;

        public HplVariable(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public static string Find(List<HplVariable> list, string name)
        {
            if (list == null) return null;
            foreach (var v in list)
                if (string.Equals(v.name, name, StringComparison.OrdinalIgnoreCase)) return v.value;
            return null;
        }
    }
}

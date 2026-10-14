using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// An entity placed in a level (.ent file instance). Holds what level scripts and gameplay need:
    /// the HPL2 name is the GameObject name; type and variables come from the .ent and the map instance.
    /// </summary>
    public class AmnesiaEntity : MonoBehaviour
    {
        [Tooltip("Entity file relative to the game folder.")]
        public string file;
        public int hplId;
        [Tooltip("UserDefinedVariables EntityType, e.g. SwingDoor, Lamp, Item.")]
        public string entityType;
        public string entitySubType;
        public Vector3 scale = Vector3.one;
        [Tooltip("Defaults from the .ent file.")]
        public List<HplVariable> variables = new();
        [Tooltip("Overrides set on this instance in the map.")]
        public List<HplVariable> instanceVariables = new();

        /// <summary>Instance value if set in the map, otherwise the .ent default.</summary>
        public string GetVariable(string name) =>
            HplVariable.Find(instanceVariables, name) ?? HplVariable.Find(variables, name);
    }
}

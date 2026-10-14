using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// A level area (&lt;Area AreaType="..."&gt;): Script areas (collision callbacks), PlayerStart, Ladder, Liquid, etc.
    /// The box size is the HPL2 Scale; non-PlayerStart areas get a trigger BoxCollider of that size.
    /// </summary>
    public class AmnesiaArea : MonoBehaviour
    {
        public string areaType;
        public int hplId;
        public Vector3 size = Vector3.one;
        public List<HplVariable> variables = new();

        public string GetVariable(string name) => HplVariable.Find(variables, name);

        void OnDrawGizmos()
        {
            Gizmos.color = areaType == "PlayerStart" ? new Color(0.2f, 1f, 0.2f, 0.8f) : new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (areaType == "PlayerStart")
            {
                Gizmos.DrawWireSphere(Vector3.up * 0.9f, 0.3f);
                // The player looks along the area's +Z (cLuxAreaNodeLoader_PlayerStart).
                Gizmos.DrawLine(Vector3.up * 0.9f, Vector3.up * 0.9f + Vector3.forward * 0.6f);
            }
            else Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}

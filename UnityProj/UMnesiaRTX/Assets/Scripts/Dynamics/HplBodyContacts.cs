using System;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// Body collision callbacks (iPhysicsBodyCallback::OnBodyCollide) for props that react to impacts. Unity reports
    /// contacts after the step; <see cref="HplPhysicsBody.PreviousVelocityAt"/> gives the velocity before it, which
    /// is what Newton's contact callback saw.
    /// </summary>
    [ExecuteAlways] // also when the simulation tools step the physics in edit mode
    public sealed class HplBodyContacts : MonoBehaviour
    {
        [NonSerialized] public HplPhysicsBody Body;
        public event Action<HplPhysicsBody, Collision> Collided;

        void OnCollisionEnter(Collision collision)
        {
            if (Body != null) Collided?.Invoke(Body, collision);
        }
    }
}

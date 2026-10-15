using System;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// Contact messages of a body: iPhysicsBodyCallback::OnBodyCollide for props that react to impacts (first
    /// contact) and the surface sounds (every step of contact, like Newton's contact callback). Unity reports
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
            if (Body == null) return;
            Collided?.Invoke(Body, collision);
            Body.World.SurfaceSounds.OnContact(Body, collision);
        }

        void OnCollisionStay(Collision collision)
        {
            if (Body != null) Body.World.SurfaceSounds.OnContact(Body, collision);
        }
    }
}

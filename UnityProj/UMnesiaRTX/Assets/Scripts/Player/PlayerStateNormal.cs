using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerState_Normal + iLuxPlayerState_DefaultBase (LuxPlayerState_DefaultBase.cpp): every step the entity
    /// under the screen center (cLuxMapHelper::GetClosestEntity, a 20 m ray) is the focus; the interact button
    /// calls its OnInteract and interact callback when it is close enough and allows it. The crosshair shows what
    /// the interaction would be. Focused items get the outline of the effect renderer. Holster → hand object (4.4).
    /// </summary>
    public class PlayerStateNormal : PlayerState
    {
        const float RayLength = 20f;
        static readonly RaycastHit[] Hits = new RaycastHit[64];

        public PlayerStateNormal(AmnesiaPlayer player) : base(player, PlayerStateType.Normal) { }
        protected PlayerStateNormal(AmnesiaPlayer player, PlayerStateType type) : base(player, type) { }
        protected virtual string FocusItemName => null;

        public override void OnEnterState(PlayerStateType previous)
        {
            Player.EntityInFocus = null;
            Player.BodyInFocus = null;
        }



        public override void Update(float timeStep)
        {
            Vector3 start = Player.LogicCameraPosition;
            Vector3 dir = Player.LogicCameraRotation * Vector3.forward;
            GetClosestEntity(start, dir, RayLength, out float distance, out var body, out var entity, FocusItemName);
            Player.FocusDistance = distance;
            Player.BodyInFocus = body;
            Player.EntityInFocus = entity;
            Player.FocusPos = HplSpace.Position(start + dir * distance);
            AddOutlineObjects(body, entity);
            Player.SetFocusText(entity != null && CanInteractWithEntity() ? entity.GetFocusText() : "");
        }

        public override void OnLeaveState(PlayerStateType next) => HplMap.Current?.effects?.Renderer.ClearOutlineObjects();

        /// <summary>
        /// iLuxPlayerState_DefaultBase::AddOutlineObjects: a focused item within its focus distance that can be
        /// interacted with gets the outline, on the meshes of the focused body and the bodies joined to it
        /// (ShowOutlinesOnConnectedBodies; grab objects too, other objects only the focused body).
        /// </summary>
        void AddOutlineObjects(HplPhysicsBody body, HplEntity entity)
        {
            var renderer = HplMap.Current != null && HplMap.Current.effects != null ? HplMap.Current.effects.Renderer : null;
            if (renderer == null) return;
            renderer.ClearOutlineObjects();
            if (entity is not HplProp prop || entity.Type != HplEntityType.Prop || body == null ||
                Player.FocusDistance > entity.MaxFocusDistance || !ShowOutlineOnEntity(entity, body))
                return;

            var bodies = new List<HplPhysicsBody> { body };
            if (prop.ShowOutlinesOnConnectedBodies)
                for (int i = 0; i < bodies.Count; i++)
                    foreach (var joint in prop.Joints)
                    {
                        if (joint.Parent == bodies[i] && joint.Child != null && !bodies.Contains(joint.Child)) bodies.Add(joint.Child);
                        if (joint.Child == bodies[i] && joint.Parent != null && !bodies.Contains(joint.Parent)) bodies.Add(joint.Parent);
                    }
            foreach (var b in bodies)
                if (b.Data != null)
                    foreach (var r in Effects.HplEffectRenderer.MeshRenderers(b.Data.transform)) renderer.AddOutlineObject(r);
        }

        /// <summary>iLuxPlayerState_DefaultBase::ShowOutlineOnEntity: items that can be picked up.</summary>
        protected virtual bool ShowOutlineOnEntity(HplEntity entity, HplPhysicsBody body) =>
            entity is Props.PropItem && entity.CanInteract(body) && !entity.InteractionDisabled;

        /// <summary>
        /// cLuxClosestEntityCallback: the nearest body along the ray. Inactive entities, characters and areas or
        /// non-colliding bodies that cannot be interacted with are skipped; level geometry blocks (entity null).
        /// </summary>
        public static bool GetClosestEntity(Vector3 start, Vector3 dir, float length, out float distance, out HplPhysicsBody body, out HplEntity entity, string useItem = null)
        {
            distance = 9999999f;
            body = null;
            entity = null;
            var map = HplMap.Current;
            int mask = ~(1 << HplPhysics.CharacterLayer);
            int count = Physics.RaycastNonAlloc(start, dir, Hits, length, mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.distance >= distance) continue;
                var c = hit.collider;
                HplPhysicsBody hitBody = map != null ? map.PhysicsWorld?.BodyOf(c) : null;
                HplEntity hitEntity = hitBody?.UserData as HplEntity;
                if (hitEntity == null && c.isTrigger)
                {
                    var area = c.GetComponent<AmnesiaArea>();
                    hitEntity = area != null && map != null ? map.GetEntityByName(area.name) : null;
                    if (hitEntity == null) continue;
                }

                if (hitEntity != null)
                {
                    if (!hitEntity.Active) continue;
                    bool collides = hitBody != null && hitBody.Collide && !c.isTrigger;
                    if (!collides && !hitEntity.CanInteract(hitBody) && !(useItem != null && map?.Script?.HasUseItemCallback(useItem, hitEntity.Name) == true)) continue;
                }
                distance = hit.distance;
                body = hitBody;
                entity = hitEntity;
            }
            return entity != null;
        }

        bool CanInteractWithEntity()
        {
            var e = Player.EntityInFocus;
            return e != null && Player.FocusDistance < e.MaxFocusDistance && e.CanInteract(Player.BodyInFocus) && !e.InteractionDisabled;
        }

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (action == PlayerAction.Interact && pressed)
            {
                if (CanInteractWithEntity())
                {
                    var e = Player.EntityInFocus;
                    e.OnInteract(Player.BodyInFocus, Player.FocusPos);
                    e.RunInteractCallback();
                }
                return false;
            }
            return true;
        }

        public override FocusCrosshair? GetCrosshair()
        {
            var e = Player.EntityInFocus;
            if (e == null || Player.FocusDistance >= e.MaxFocusDistance) return FocusCrosshair.Default;
            var crosshair = e.GetFocusCrosshair(Player.BodyInFocus, Player.FocusPos);
            if (e.InteractionDisabled) crosshair = FocusCrosshair.Default;
            return crosshair == FocusCrosshair.LastEnum ? FocusCrosshair.Default : crosshair;
        }

        public override string Describe()
        {
            var e = Player.EntityInFocus;
            return e == null ? "Normal" : $"Normal focus {e.Name}{(Player.BodyInFocus != null ? "/" + Player.BodyInFocus.Name : "")} {Player.FocusDistance:0.00}m";
        }
    }
}

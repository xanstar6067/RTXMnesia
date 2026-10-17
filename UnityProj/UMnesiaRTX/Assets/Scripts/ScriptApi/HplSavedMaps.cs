using System;
using System.Collections.Generic;
using RTXMnesia.Props;
using RTXMnesia.Save;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// cLuxSavedGameMapCollection / cLuxSavedGameMap (LuxSavedGame.cpp): the state of the maps visited in this game,
    /// by map name. cLuxMapHandler::CheckMapChange saves the map being left (FromMap) and loads it into the freshly
    /// loaded map when the player comes back (ToMap); a map not in the collection is entered for the first time
    /// (OnStart runs). ClearSavedMaps and a new game empty it. A save file holds the collection (<see cref="Export"/>).
    ///
    /// Kept as in HPL2: settings, fog, local variables, use item callbacks, timers, the player's collide callbacks,
    /// the map's lights, saved sounds and particle systems, entity collide callbacks and connections, the look /
    /// interact / callback / state change functions, which entities are inactive, which doors and level doors are
    /// locked, which items were not picked up, which containers still hold an item, which lamps are unlit. Everything
    /// else of an ordinary entity comes back as the map file has it (a thrown box is back in its place). Entities
    /// with FullGameSave are kept whole: their bodies, joints and own fields.
    ///
    /// The current map of a save file (cLuxSavedMap, <see cref="CaptureCurrent"/>) keeps every entity whole, like
    /// HPL2 that loads a saved map without its entities and creates them from the save.
    /// </summary>
    public static class HplSavedMaps
    {
        static readonly Dictionary<string, SavedMap> s_maps = new(StringComparer.Ordinal);

        public static bool MapExists(string name) => s_maps.ContainsKey(name);

        public static int Count => s_maps.Count;

        /// <summary>cLuxMapHandler::ClearSaveMapCollection (ClearSavedMaps, new game).</summary>
        public static void Reset() => s_maps.Clear();

        /// <summary>The collection for a save file (cLuxSaveGame_SaveData::mpSavedMaps).</summary>
        public static List<SavedMap> Export() => new(s_maps.Values);

        /// <summary>cLuxMapHandler::SetSavedMapCollection after loading a save.</summary>
        public static void Import(List<SavedMap> maps)
        {
            s_maps.Clear();
            if (maps == null) return;
            foreach (var m in maps) if (m != null && !string.IsNullOrEmpty(m.name)) s_maps[m.name] = m;
        }

        /// <summary>The key of an entity in the save: the map's unique ID like HPL2, the name for created entities.</summary>
        static string Key(HplEntity e) => e.Id >= 0 ? "#" + e.Id : e.Name.ToLowerInvariant();

        static Dictionary<string, HplEntity> ByKey(HplMap map)
        {
            var dict = new Dictionary<string, HplEntity>(StringComparer.Ordinal);
            foreach (var e in map.Entities) dict.TryAdd(Key(e), e);
            return dict;
        }

        static HplEntity Find(Dictionary<string, HplEntity> byKey, string key) => key != null && byKey.TryGetValue(key, out var e) ? e : null;

        // ================================================================== FromMap

        /// <summary>cLuxSavedGameMapCollection::SaveMap → cLuxSavedGameMap::FromMap.</summary>
        public static void SaveMap(HplMap map) => s_maps[map.MapName] = Capture(map, false);

        /// <summary>cLuxSavedMap::FromMap: the current map of a save file.</summary>
        public static SavedMap CaptureCurrent(HplMap map) => Capture(map, true);

        static SavedMap Capture(HplMap map, bool full)
        {
            var s = new SavedMap
            {
                name = map.MapName,
                full = full,
                displayNameEntry = map.DisplayNameEntry,
                numberOfQuests = map.NumberOfQuests,
                fogActive = map.FogActive,
                fogColor = map.FogColor,
                fogStart = map.FogStart,
                fogEnd = map.FogEnd,
                fogFalloffExp = map.FogFalloffExp,
            };
            foreach (var pair in map.Vars) s.vars.Add(new SavedVar { name = pair.Key, value = pair.Value });
            if (map.Script != null)
                foreach (var (name, item, entity, function, autoDestroy) in map.Script.ExportUseItemCallbacks())
                    s.useItemCallbacks.Add(new SavedUseItem { name = name, item = item, entity = entity, function = function, autoDestroy = autoDestroy });
            foreach (var (name, function, count) in map.ExportTimers()) s.timers.Add(new SavedTimer { name = name, function = function, count = count });
            s.playerCollideCallbacks = ExportCallbacks(map.PlayerCollideCallbacks);
            if (full)
            {
                s.lanternLitCallback = map.LanternLitCallback;
                s.checkPointName = map.CheckPointName;
                s.checkPointStartPos = map.CheckPointStartPos;
                s.checkPointCallback = map.CheckPointCallback;
                s.checkPointCount = map.CheckPointCount;
                s.checkPointMusic = map.CheckPointMusic;
                s.checkPointMusicPrio = map.CheckPointMusicPrio;
                s.checkPointMusicResume = map.CheckPointMusicResume;
                s.checkPointMusicVolume = map.CheckPointMusicVolume;
            }

            foreach (var light in map.MapLights) s.lights.Add(new SavedLight { name = light.name, data = light.Save() });

            // Sounds: placed ones and those made by the script with abSaveSound, not following an entity.
            if (map.sound != null)
                foreach (var sound in map.sound.SoundEntities)
                {
                    if (!sound.IsSaved || sound.Parent != null && !map.IsPlacedSound(sound)) continue;
                    bool placed = map.IsPlacedSound(sound);
                    if (placed && sound.Parent != null && sound.Parent.GetComponentInParent<AmnesiaEntity>(true) != null) continue;
                    s.sounds.Add(new SavedSound
                    {
                        name = sound.Name, file = sound.Data != null ? sound.Data.name : "", placed = placed,
                        active = sound.Active, stopped = sound.IsStopped() || sound.IsFadingOut, removeWhenOver = sound.RemoveWhenOver,
                        minDistance = sound.MinDistance, maxDistance = sound.MaxDistance, volume = sound.Volume, position = sound.Position,
                    });
                }

            // Particle systems: placed ones and saved script ones, not following an entity, not dying.
            if (map.ParticleWorld != null)
                foreach (var ps in map.ParticleWorld.Systems)
                {
                    bool placed = map.IsPlacedParticleSystem(ps);
                    if (!ps.IsSaved || ps.IsDying) continue;
                    if (ps.Parent != null && (!placed || ps.Parent.GetComponentInParent<AmnesiaEntity>(true) != null)) continue;
                    var emitters = new bool[ps.Emitters.Count];
                    for (int i = 0; i < emitters.Length; i++) emitters[i] = !(ps.Emitters[i].IsDying || ps.Emitters[i].IsDead);
                    s.particleSystems.Add(new SavedParticleSystem
                    {
                        name = ps.Name, file = ps.DataName, placed = placed, active = ps.Active, visible = ps.Visible,
                        localMatrix = ps.LocalMatrix, color = ps.Color, fadeAtDistance = ps.FadeAtDistance,
                        minFadeStart = ps.MinFadeDistanceStart, minFadeEnd = ps.MinFadeDistanceEnd,
                        maxFadeStart = ps.MaxFadeDistanceStart, maxFadeEnd = ps.MaxFadeDistanceEnd, emittersActive = emitters,
                    });
                }

            foreach (var e in map.Entities)
            {
                string key = Key(e);
                // cLuxSavedMap: every saved entity that is not being destroyed.
                if (full)
                {
                    if (!map.IsDestroyPending(e)) s.fullEntities.Add(SaveFull(e, key));
                    continue;
                }
                if (e.FullGameSave)
                {
                    s.fullEntities.Add(SaveFull(e, key));
                    continue;
                }

                if (e.CollideCallbacks.Any || e.Connections.Count > 0 || e is HplProp { InteractConnections: { Count: > 0 } })
                    s.links.Add(SaveLinks(e, key));
                if (e.LookAtCallback != "") s.look.Add(new SavedEntityFunc { key = key, function = e.LookAtCallback, autoRemove = e.LookAtCallbackRemove });
                if (e.InteractCallback != "") s.interact.Add(new SavedEntityFunc { key = key, function = e.InteractCallback, autoRemove = e.InteractCallbackRemove });
                if (e.CallbackFunc != "") s.callback.Add(new SavedEntityFunc { key = key, function = e.CallbackFunc });
                if (e.ConnectionStateChangeCallback != "") s.stateChange.Add(new SavedEntityFunc { key = key, function = e.ConnectionStateChangeCallback });
                if (!e.Active) s.disabled.Add(key);

                switch (e)
                {
                    case PropSwingDoor door:
                        if (door.Locked) s.lockedDoors.Add(key);
                        break;
                    case PropLevelDoor levelDoor:
                        if (levelDoor.Locked) s.lockedLevelDoors.Add(key);
                        break;
                    case PropItem item:
                        // An item a broken container spawned is kept as "the container has an active item".
                        if (item.SpawnContainer != "") s.brokenContainersWithActiveItem.Add(item.SpawnContainer);
                        else s.activeItems.Add(key);
                        break;
                    case PropObject obj:
                        if (obj.ContainedItem != "") s.unbrokenContainers.Add(key);
                        break;
                    case PropLamp lamp:
                        if (!lamp.Lit) s.unlitLamps.Add(key);
                        break;
                    case Enemies.HplEnemy enemy:
                        var patrol = new SavedEnemyPatrol { key = key };
                        foreach (var p in enemy.Patrol)
                            patrol.nodes.Add(new SavedPatrolNode { nodeId = p.node.Id, waitTime = p.waitTime, animation = p.animation, loopAnimation = p.loopAnimation });
                        s.enemies.Add(patrol);
                        break;
                }
            }
            return s;
        }

        static List<SavedCollideCallback> ExportCallbacks(HplCollideCallbacks callbacks)
        {
            var list = new List<SavedCollideCallback>();
            foreach (var (entity, function, delete, states) in callbacks.Export())
                list.Add(new SavedCollideCallback { entity = entity, function = function, delete = delete, states = states });
            return list;
        }

        static void ImportCallbacks(HplMap map, HplCollideCallbacks callbacks, List<SavedCollideCallback> saved)
        {
            var list = new List<(string, string, bool, int)>();
            if (saved != null) foreach (var c in saved) list.Add((c.entity, c.function, c.delete, c.states));
            callbacks.Import(map, list);
        }

        static SavedEntityLinks SaveLinks(HplEntity e, string key)
        {
            var links = new SavedEntityLinks { key = key, collideCallbacks = ExportCallbacks(e.CollideCallbacks) };
            foreach (var (name, entity, invert, states, callback) in e.Connections)
                if (entity != null) links.connections.Add(new SavedConnection { name = name, entity = entity.Name, invert = invert, states = states, callback = callback });
            if (e is HplProp prop)
                foreach (var c in prop.InteractConnections) links.interactConnections.Add(c.CaptureSave());
            return links;
        }

        // Restored through their setters, so the scene objects follow (activity, effects, physics, lamps), or not state.
        static readonly HashSet<string> ExplicitFields = new(StringComparer.Ordinal)
        {
            "<Active>k__BackingField", "_effectsActive", "_effectAlphaFading", "<EffectsAlpha>k__BackingField",
            "<StaticPhysics>k__BackingField", "<Lit>k__BackingField", "<DestroyMe>k__BackingField", "IsInteractedWith",
            "<SoundState>k__BackingField",
        };

        static SavedFullEntity SaveFull(HplEntity e, string key)
        {
            var f = new SavedFullEntity
            {
                key = key,
                name = e.Name,
                active = e.Active,
                interactionDisabled = e.InteractionDisabled,
                maxFocusDistance = e.MaxFocusDistance,
                callbackFunc = e.CallbackFunc,
                stateChangeCallback = e.ConnectionStateChangeCallback,
                interactCallback = e.InteractCallback,
                interactRemove = e.InteractCallbackRemove,
                lookAtCallback = e.LookAtCallback,
                lookAtRemove = e.LookAtCallbackRemove,
                position = e.GameObject.transform.position,
                rotation = e.GameObject.transform.rotation,
                links = SaveLinks(e, key),
                // The entity's own state (iLuxProp_SaveData, iLuxArea_SaveData, iLuxEnemy_SaveData and the types' save
                // data): the plain value fields of its class and its base classes.
                fields = HplFieldBag.Capture(e, typeof(HplEntity), ExplicitFields),
            };
            if (e is HplProp prop)
            {
                f.file = prop.Component != null ? prop.Component.file : "";
                f.effectsActive = prop.EffectsActive;
                f.staticPhysics = prop.StaticPhysics;
                f.lit = prop is PropLamp lamp && lamp.Lit;
                foreach (var body in prop.PhysicsBodies)
                    f.bodies.Add(new SavedBody
                    {
                        position = body.Position, rotation = body.Rotation,
                        linearVelocity = body.LinearVelocity, angularVelocity = body.AngularVelocity,
                    });
                foreach (var joint in prop.Joints)
                    f.joints.Add(new SavedJoint { minAngle = joint.MinAngle, maxAngle = joint.MaxAngle, broken = joint.Broken });
                prop.CaptureAnimations(f.animations);
            }
            else if (e is Enemies.HplEnemy enemy)
            {
                f.isEnemy = true;
                f.file = enemy.FileName ?? "";
                f.enemy = enemy.CaptureSave();
            }
            else if (e is HplRope rope) f.rope = rope.CaptureSave();
            else if (e is HplArea { Sticky: { } sticky }) f.sticky = sticky.CaptureSave();
            return f;
        }

        // ================================================================== ToMap

        /// <summary>cLuxSavedGameMapCollection::LoadMap → cLuxSavedGameMap::ToMap: nothing for a first visit.</summary>
        public static void LoadMap(HplMap map)
        {
            if (s_maps.TryGetValue(map.MapName, out var s)) Apply(map, s);
        }

        /// <summary>cLuxSavedMap::ToMap / cLuxSavedGameMap::ToMap on the freshly loaded map scene.</summary>
        public static void Apply(HplMap map, SavedMap s)
        {
            var byKey = ByKey(map);

            map.DisplayNameEntry = s.displayNameEntry;
            map.NumberOfQuests = s.numberOfQuests;
            map.SetFog(s.fogActive, s.fogColor, s.fogStart, s.fogEnd, s.fogFalloffExp);
            if (s.full)
            {
                map.LanternLitCallback = s.lanternLitCallback;
                map.CheckPointName = s.checkPointName;
                map.CheckPointStartPos = s.checkPointStartPos;
                map.CheckPointCallback = s.checkPointCallback;
                map.CheckPointCount = s.checkPointCount;
                map.CheckPointMusic = s.checkPointMusic;
                map.CheckPointMusicPrio = s.checkPointMusicPrio;
                map.CheckPointMusicResume = s.checkPointMusicResume;
                map.CheckPointMusicVolume = s.checkPointMusicVolume;
            }

            map.Vars.Clear();
            foreach (var v in s.vars) map.Vars[v.name] = v.value;
            var useItems = new List<(string, string, string, string, bool)>();
            foreach (var c in s.useItemCallbacks) useItems.Add((c.name, c.item, c.entity, c.function, c.autoDestroy));
            map.Script?.ImportUseItemCallbacks(useItems);
            var timers = new List<(string, string, float)>();
            foreach (var t in s.timers) timers.Add((t.name, t.function, t.count));
            map.ImportTimers(timers);

            // Saved connections and collide callbacks.
            foreach (var links in s.links)
            {
                var e = Find(byKey, links.key);
                if (e == null)
                {
                    HplLog.Error($"Saved entity with ID {links.key} does not exist!");
                    continue;
                }
                LoadLinks(map, e, links);
            }

            map.PlayerCollideCallbacks.Clear();
            ImportCallbacks(map, map.PlayerCollideCallbacks, s.playerCollideCallbacks);

            // Functions: cleared on all entities that are not fully saved, then set as saved.
            if (!s.full)
            {
                foreach (var e in map.Entities)
                {
                    if (e.FullGameSave) continue;
                    e.SetPlayerLookAtCallback("", false);
                    e.SetPlayerInteractCallback("", false);
                    e.CallbackFunc = "";
                    e.ConnectionStateChangeCallback = "";
                }
                foreach (var f in s.look) Find(byKey, f.key)?.SetPlayerLookAtCallback(f.function, f.autoRemove);
                foreach (var f in s.interact) Find(byKey, f.key)?.SetPlayerInteractCallback(f.function, f.autoRemove);
                foreach (var f in s.callback) { var e = Find(byKey, f.key); if (e != null) e.CallbackFunc = f.function; }
                foreach (var f in s.stateChange) { var e = Find(byKey, f.key); if (e != null) e.ConnectionStateChangeCallback = f.function; }
            }

            // Lights.
            var lights = new Dictionary<string, AmnesiaLight.SaveData>(StringComparer.Ordinal);
            foreach (var l in s.lights) lights[l.name] = l.data;
            foreach (var light in map.MapLights)
            {
                if (lights.TryGetValue(light.name, out var data)) light.Restore(data);
                else HplLog.Warn($"Could not find saved light '{light.name}'");
            }

            LoadSounds(map, s);
            LoadParticleSystems(map, s);

            // Full entities: created if needed and loaded. Of the current map of a save every entity is saved, so an
            // entity without save data is gone (picked up, broken, destroyed); in the cache only full ones.
            var saved = new HashSet<string>(StringComparer.Ordinal);
            var loaded = new List<(HplEntity entity, SavedFullEntity data)>();
            foreach (var data in s.fullEntities)
            {
                saved.Add(data.key);
                var e = Find(byKey, data.key);
                if (e == null && !data.isEnemy && !string.IsNullOrEmpty(data.file))
                {
                    e = map.CreateEntity(data.name, data.file, data.position, data.rotation, Vector3.one);
                    if (e != null && !s.full) e.FullGameSave = true;
                }
                if (e != null) loaded.Add((e, data));
                else HplLog.Error($"Saved entity '{data.name}' ({data.key}) could not be created!");
            }
            foreach (var e in new List<HplEntity>(map.Entities))
            {
                if ((s.full || e.FullGameSave) && !saved.Contains(Key(e)) && !loaded.Exists(l => l.entity == e)) map.DestroyEntity(e);
            }
            map.DestroyPendingEntitiesNow();

            if (!s.full)
            {
                // Entity settings of the other entities.
                // Enemies (cLuxSavedGameEnemy::ToEnemy): inactive (FromEnemy saves false), the saved patrol nodes.
                foreach (var patrol in s.enemies)
                {
                    if (Find(byKey, patrol.key) is not Enemies.HplEnemy enemy)
                    {
                        HplLog.Error($"Saved enemy with ID {patrol.key} does not exist!");
                        continue;
                    }
                    enemy.SetActive(false);
                    enemy.ClearPatrolNodes();
                    var container = enemy.Pathfinder?.NodeContainer;
                    if (container == null) continue;
                    foreach (var p in patrol.nodes)
                    {
                        var node = container.GetNodeFromId(p.nodeId);
                        if (node != null) enemy.AddPatrolNode(node, p.waitTime, p.animation, p.loopAnimation);
                    }
                }

                var disabled = new HashSet<string>(s.disabled, StringComparer.Ordinal);
                var lockedDoors = new HashSet<string>(s.lockedDoors, StringComparer.Ordinal);
                var lockedLevelDoors = new HashSet<string>(s.lockedLevelDoors, StringComparer.Ordinal);
                var activeItems = new HashSet<string>(s.activeItems, StringComparer.Ordinal);
                var unbroken = new HashSet<string>(s.unbrokenContainers, StringComparer.Ordinal);
                var brokenWithItem = new HashSet<string>(s.brokenContainersWithActiveItem, StringComparer.Ordinal);
                var unlit = new HashSet<string>(s.unlitLamps, StringComparer.Ordinal);
                foreach (var e in new List<HplEntity>(map.Entities))
                {
                    if (e.FullGameSave || e is Enemies.HplEnemy) continue;
                    string key = Key(e);
                    e.SetActive(!disabled.Contains(key));
                    switch (e)
                    {
                        case PropSwingDoor door:
                            door.SetLocked(lockedDoors.Contains(key), false);
                            break;
                        case PropLevelDoor levelDoor:
                            levelDoor.Locked = lockedLevelDoors.Contains(key);
                            break;
                        case PropItem:
                            if (!activeItems.Contains(key)) map.DestroyEntity(e);
                            break;
                        case PropObject obj:
                            // A broken container whose item was picked up holds nothing any more.
                            if (obj.ContainedItem != "" && !unbroken.Contains(key) && !brokenWithItem.Contains(obj.Name))
                                obj.ContainedItem = "";
                            break;
                        case PropLamp lamp:
                            lamp.SetLit(!unlit.Contains(key), false);
                            break;
                    }
                }
                map.DestroyPendingEntitiesNow();
            }

            // Entities setup.
            foreach (var (e, data) in loaded) LoadFull(map, e, data);
            // After the props: a sticky area makes its attached body static again; the ropes (cLuxSavedMap: after
            // the entities) take their saved particles and bodies.
            foreach (var (e, data) in loaded)
            {
                if (e is HplArea { Sticky: { } sticky } && data.sticky != null) sticky.RestoreSave(data.sticky);
                if (e is HplRope rope && s.full) rope.RestoreSave(data.rope, map.transform);
            }
        }

        static void LoadLinks(HplMap map, HplEntity e, SavedEntityLinks links)
        {
            e.CollideCallbacks.Clear();
            ImportCallbacks(map, e.CollideCallbacks, links.collideCallbacks);
            e.Connections.Clear();
            foreach (var c in links.connections)
            {
                var other = map.GetEntityByName(c.entity);
                if (other != null) e.Connections.Add((c.name, other, c.invert, c.states, c.callback));
            }
            if (e is not HplProp prop) return;
            prop.InteractConnections.Clear();
            if (links.interactConnections == null) return;
            foreach (var saved in links.interactConnections)
            {
                var c = HplInteractConnection.FromSave(map, prop, saved);
                if (c != null) prop.InteractConnections.Add(c);
            }
        }

        static void LoadFull(HplMap map, HplEntity e, SavedFullEntity f)
        {
            HplFieldBag.Restore(e, typeof(HplEntity), f.fields, ExplicitFields);
            e.SetActive(f.active);
            e.InteractionDisabled = f.interactionDisabled;
            e.MaxFocusDistance = f.maxFocusDistance;
            e.CallbackFunc = f.callbackFunc;
            e.ConnectionStateChangeCallback = f.stateChangeCallback;
            e.SetPlayerInteractCallback(f.interactCallback, f.interactRemove);
            e.SetPlayerLookAtCallback(f.lookAtCallback, f.lookAtRemove);
            LoadLinks(map, e, f.links);
            if (e is Enemies.HplEnemy enemy)
            {
                enemy.RestoreSave(f.enemy);
                return;
            }
            if (e is not HplProp prop) return;

            prop.SetStaticPhysics(f.staticPhysics);
            for (int i = 0; i < prop.PhysicsBodies.Count && i < f.bodies.Count; i++)
            {
                var body = prop.PhysicsBodies[i];
                var b = f.bodies[i];
                body.SetPose(b.position, b.rotation);
                body.LinearVelocity = b.linearVelocity;
                body.AngularVelocity = b.angularVelocity;
            }
            for (int i = 0; i < prop.Joints.Count && i < f.joints.Count; i++)
            {
                var joint = prop.Joints[i];
                var j = f.joints[i];
                joint.SetMinAngle(j.minAngle);
                joint.SetMaxAngle(j.maxAngle);
                if (j.broken && !joint.Broken) joint.Break();
            }
            prop.SetEffectsActive(f.effectsActive, false);
            if (prop is PropLamp lamp) lamp.SetLit(f.lit, false);
            prop.RestoreAnimations(f.animations);
            prop.AfterStateRestore();
        }

        static void LoadSounds(HplMap map, SavedMap s)
        {
            if (map.sound == null) return;
            var placed = new Dictionary<string, Sound.HplSoundEntity>(StringComparer.Ordinal);
            foreach (var (name, sound) in map.PlacedMapSounds) placed.TryAdd(name, sound);
            foreach (var saved in s.sounds)
            {
                Sound.HplSoundEntity sound;
                if (saved.placed) placed.TryGetValue(saved.name, out sound);
                else sound = map.sound.CreateSoundEntity(saved.name, saved.file, saved.removeWhenOver);
                if (sound == null)
                {
                    HplLog.Warn($"Could not create or find sound '{saved.name}' of file '{saved.file}'");
                    continue;
                }
                // cEngineSound_SaveData::ToSound
                sound.Active = saved.active;
                if (saved.stopped) sound.Stop(false);
                sound.MinDistance = saved.minDistance;
                sound.MaxDistance = saved.maxDistance;
                sound.Volume = saved.volume;
                sound.Position = saved.position;
            }
        }

        static void LoadParticleSystems(HplMap map, SavedMap s)
        {
            if (map.ParticleWorld == null) return;
            var placed = new Dictionary<string, Particles.HplParticleSystem>(StringComparer.Ordinal);
            foreach (var (name, ps) in map.PlacedMapParticleSystems) placed.TryAdd(name, ps);
            var savedPlaced = new HashSet<string>(StringComparer.Ordinal);
            foreach (var saved in s.particleSystems)
            {
                Particles.HplParticleSystem ps;
                if (saved.placed)
                {
                    placed.TryGetValue(saved.name, out ps);
                    savedPlaced.Add(saved.name);
                }
                else ps = map.ParticleWorld.Create(saved.name, saved.file);
                if (ps == null)
                {
                    if (!saved.placed) HplLog.Warn($"Could not create particle system '{saved.file}'");
                    continue;
                }
                // cEnginePS_SaveData::ToPS
                ps.Active = saved.active;
                ps.SetVisible(saved.visible);
                if (!saved.placed) ps.LocalMatrix = saved.localMatrix;
                ps.Color = saved.color;
                ps.FadeAtDistance = saved.fadeAtDistance;
                ps.MinFadeDistanceStart = saved.minFadeStart;
                ps.MinFadeDistanceEnd = saved.minFadeEnd;
                ps.MaxFadeDistanceStart = saved.maxFadeStart;
                ps.MaxFadeDistanceEnd = saved.maxFadeEnd;
                var emitters = saved.emittersActive ?? Array.Empty<bool>();
                for (int i = 0; i < emitters.Length && i < ps.Emitters.Count; i++)
                    if (!emitters[i]) ps.Emitters[i].KillInstantly();
            }
            // Placed systems that were not saved had been destroyed.
            foreach (var pair in placed)
                if (!savedPlaced.Contains(pair.Key)) pair.Value.KillInstantly();
        }
    }
}

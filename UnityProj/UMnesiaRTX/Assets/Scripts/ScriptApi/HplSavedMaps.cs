using System;
using System.Collections.Generic;
using System.Reflection;
using RTXMnesia.Dynamics;
using RTXMnesia.Props;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// cLuxSavedGameMapCollection / cLuxSavedGameMap (LuxSavedGame.cpp): the state of the maps visited in this game,
    /// by map name. cLuxMapHandler::CheckMapChange saves the map being left (FromMap) and loads it into the freshly
    /// loaded map when the player comes back (ToMap); a map not in the collection is entered for the first time
    /// (OnStart runs). ClearSavedMaps and a new game empty it.
    ///
    /// Kept as in HPL2: settings, fog, local variables, use item callbacks, timers, the player's collide callbacks,
    /// the map's lights, saved sounds and particle systems, entity collide callbacks and connections, the look /
    /// interact / callback / state change functions, which entities are inactive, which doors and level doors are
    /// locked, which items were not picked up, which containers still hold an item, which lamps are unlit. Everything
    /// else of an ordinary entity comes back as the map file has it (a thrown box is back in its place). Entities
    /// with FullGameSave are kept whole: their bodies, joints and the prop's own state.
    /// </summary>
    public static class HplSavedMaps
    {
        sealed class SavedEntityLinks
        {
            public List<(string entity, string function, bool delete, int states)> CollideCallbacks;
            public List<(string name, string entity, bool invert, int states, string callback)> Connections;
        }

        sealed class SavedBody
        {
            public Vector3 position, linearVelocity, angularVelocity;
            public Quaternion rotation;
        }

        sealed class SavedJoint
        {
            public float minAngle, maxAngle;
            public bool broken;
        }

        sealed class SavedFullEntity
        {
            public string name, file;
            public bool active, interactionDisabled, effectsActive, staticPhysics, lit;
            public float maxFocusDistance;
            public string callbackFunc, stateChangeCallback, interactCallback, lookAtCallback;
            public bool interactRemove, lookAtRemove;
            public Vector3 position;
            public Quaternion rotation;
            public SavedEntityLinks links;
            public List<SavedBody> bodies = new();
            public List<SavedJoint> joints = new();
            public Dictionary<FieldInfo, object> fields = new();
        }

        sealed class SavedSound
        {
            public string name, file;
            public bool placed, active, stopped, removeWhenOver;
            public float minDistance, maxDistance, volume;
            public Vector3 position;
        }

        sealed class SavedParticleSystem
        {
            public string name, file;
            public bool placed, active, visible, fadeAtDistance;
            public Matrix4x4 localMatrix;
            public Color color;
            public float minFadeStart, minFadeEnd, maxFadeStart, maxFadeEnd;
            public bool[] emittersActive;
        }

        sealed class SavedMap
        {
            public string name, displayNameEntry;
            public int numberOfQuests;
            public bool fogActive;
            public Color fogColor;
            public float fogStart, fogEnd, fogFalloffExp;
            public Dictionary<string, string> vars;
            public List<(string name, string item, string entity, string function, bool autoDestroy)> useItemCallbacks;
            public List<(string name, string function, float count)> timers;
            public List<(string entity, string function, bool delete, int states)> playerCollideCallbacks;
            public readonly Dictionary<string, AmnesiaLight.SaveData> lights = new(StringComparer.Ordinal);
            public readonly List<SavedSound> sounds = new();
            public readonly List<SavedParticleSystem> particleSystems = new();
            public readonly Dictionary<string, SavedFullEntity> fullEntities = new(StringComparer.Ordinal);
            public readonly Dictionary<string, SavedEntityLinks> links = new(StringComparer.Ordinal);
            public readonly Dictionary<string, (string function, bool remove)> look = new(StringComparer.Ordinal), interact = new(StringComparer.Ordinal);
            public readonly Dictionary<string, string> callback = new(StringComparer.Ordinal), stateChange = new(StringComparer.Ordinal);
            public readonly HashSet<string> disabled = new(StringComparer.Ordinal), lockedDoors = new(StringComparer.Ordinal),
                lockedLevelDoors = new(StringComparer.Ordinal), activeItems = new(StringComparer.Ordinal),
                unbrokenContainers = new(StringComparer.Ordinal), brokenContainersWithActiveItem = new(StringComparer.Ordinal),
                unlitLamps = new(StringComparer.Ordinal);
        }

        static readonly Dictionary<string, SavedMap> s_maps = new(StringComparer.Ordinal);

        public static bool MapExists(string name) => s_maps.ContainsKey(name);

        public static int Count => s_maps.Count;

        /// <summary>cLuxMapHandler::ClearSaveMapCollection (ClearSavedMaps, new game).</summary>
        public static void Reset() => s_maps.Clear();

        /// <summary>The key of an entity in the save: the map's unique ID like HPL2, the name for created entities.</summary>
        static string Key(HplEntity e) => e.Id >= 0 ? "#" + e.Id : e.Name.ToLowerInvariant();

        static HplEntity Find(HplMap map, Dictionary<string, HplEntity> byKey, string key) => byKey.TryGetValue(key, out var e) ? e : null;

        static Dictionary<string, HplEntity> ByKey(HplMap map)
        {
            var dict = new Dictionary<string, HplEntity>(StringComparer.Ordinal);
            foreach (var e in map.Entities) dict.TryAdd(Key(e), e);
            return dict;
        }

        // ================================================================== FromMap

        /// <summary>cLuxSavedGameMapCollection::SaveMap → cLuxSavedGameMap::FromMap.</summary>
        public static void SaveMap(HplMap map)
        {
            var s = new SavedMap
            {
                name = map.MapName,
                displayNameEntry = map.DisplayNameEntry,
                numberOfQuests = map.NumberOfQuests,
                fogActive = map.FogActive,
                fogColor = map.FogColor,
                fogStart = map.FogStart,
                fogEnd = map.FogEnd,
                fogFalloffExp = map.FogFalloffExp,
                vars = new Dictionary<string, string>(map.Vars, StringComparer.Ordinal),
                useItemCallbacks = map.Script != null ? map.Script.ExportUseItemCallbacks() : new(),
                timers = map.ExportTimers(),
                playerCollideCallbacks = map.PlayerCollideCallbacks.Export(),
            };

            foreach (var light in map.MapLights) s.lights[light.name] = light.Save();

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
                if (e.FullGameSave)
                {
                    s.fullEntities[key] = SaveFull(e);
                    continue;
                }

                bool linked = false;
                if (e.CollideCallbacks.Any || e.Connections.Count > 0)
                {
                    s.links[key] = SaveLinks(e);
                    linked = true;
                }
                if (e.LookAtCallback != "") s.look[key] = (e.LookAtCallback, e.LookAtCallbackRemove);
                if (e.InteractCallback != "") s.interact[key] = (e.InteractCallback, e.InteractCallbackRemove);
                if (e.CallbackFunc != "") s.callback[key] = e.CallbackFunc;
                if (e.ConnectionStateChangeCallback != "") s.stateChange[key] = e.ConnectionStateChangeCallback;
                if (!e.Active) s.disabled.Add(key);
                _ = linked;

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
                }
            }

            s_maps[s.name] = s;
        }

        static SavedEntityLinks SaveLinks(HplEntity e)
        {
            var links = new SavedEntityLinks
            {
                CollideCallbacks = e.CollideCallbacks.Export(),
                Connections = new List<(string, string, bool, int, string)>(),
            };
            foreach (var (name, entity, invert, states, callback) in e.Connections)
                if (entity != null) links.Connections.Add((name, entity.Name, invert, states, callback));
            return links;
        }

        static SavedFullEntity SaveFull(HplEntity e)
        {
            var f = new SavedFullEntity
            {
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
                links = SaveLinks(e),
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
                foreach (var field in StateFields(prop.GetType())) f.fields[field] = field.GetValue(prop);
            }
            return f;
        }

        // ------------------------------------------------------------------ the prop's own state

        static readonly Dictionary<Type, List<FieldInfo>> s_stateFields = new();

        // Restored through their setters, so the scene objects follow (effects, physics, lamps), or not state.
        static readonly HashSet<string> ExplicitFields = new(StringComparer.Ordinal)
        {
            "_effectsActive", "_effectAlphaFading", "<EffectsAlpha>k__BackingField", "<StaticPhysics>k__BackingField",
            "<Lit>k__BackingField", "<DestroyMe>k__BackingField", "IsInteractedWith",
        };

        /// <summary>
        /// The prop's own state (iLuxProp_SaveData and the type's save data): the plain value fields of its class and
        /// of HplProp (health, door / lever / wheel / move object state, static movement...). References and
        /// read-only data are not state.
        /// </summary>
        static List<FieldInfo> StateFields(Type type)
        {
            if (s_stateFields.TryGetValue(type, out var list)) return list;
            list = new List<FieldInfo>();
            for (var t = type; t != null && typeof(HplProp).IsAssignableFrom(t); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsInitOnly && !field.IsLiteral && IsPlain(field.FieldType) && !ExplicitFields.Contains(field.Name)) list.Add(field);
            s_stateFields[type] = list;
            return list;
        }

        static bool IsPlain(Type t) =>
            t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) ||
            t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Matrix4x4);

        // ================================================================== ToMap

        /// <summary>cLuxSavedGameMapCollection::LoadMap → cLuxSavedGameMap::ToMap: nothing for a first visit.</summary>
        public static void LoadMap(HplMap map)
        {
            if (!s_maps.TryGetValue(map.MapName, out var s)) return;
            var byKey = ByKey(map);

            map.DisplayNameEntry = s.displayNameEntry;
            map.NumberOfQuests = s.numberOfQuests;
            map.SetFog(s.fogActive, s.fogColor, s.fogStart, s.fogEnd, s.fogFalloffExp);

            map.Vars.Clear();
            foreach (var pair in s.vars) map.Vars[pair.Key] = pair.Value;
            map.Script?.ImportUseItemCallbacks(s.useItemCallbacks);
            map.ImportTimers(s.timers);

            // Saved connections and collide callbacks.
            foreach (var pair in s.links)
            {
                var e = Find(map, byKey, pair.Key);
                if (e == null)
                {
                    HplLog.Error($"Saved entity with ID {pair.Key} does not exist!");
                    continue;
                }
                LoadLinks(map, e, pair.Value);
            }

            map.PlayerCollideCallbacks.Import(map, s.playerCollideCallbacks);

            // Functions: cleared on all entities that are not fully saved, then set as saved.
            foreach (var e in map.Entities)
            {
                if (e.FullGameSave) continue;
                e.SetPlayerLookAtCallback("", false);
                e.SetPlayerInteractCallback("", false);
                e.CallbackFunc = "";
                e.ConnectionStateChangeCallback = "";
            }
            foreach (var pair in s.look) Find(map, byKey, pair.Key)?.SetPlayerLookAtCallback(pair.Value.function, pair.Value.remove);
            foreach (var pair in s.interact) Find(map, byKey, pair.Key)?.SetPlayerInteractCallback(pair.Value.function, pair.Value.remove);
            foreach (var pair in s.callback) { var e = Find(map, byKey, pair.Key); if (e != null) e.CallbackFunc = pair.Value; }
            foreach (var pair in s.stateChange) { var e = Find(map, byKey, pair.Key); if (e != null) e.ConnectionStateChangeCallback = pair.Value; }

            // Lights.
            foreach (var light in map.MapLights)
            {
                if (s.lights.TryGetValue(light.name, out var data)) light.Restore(data);
                else HplLog.Warn($"Could not find saved light '{light.name}'");
            }

            LoadSounds(map, s);
            LoadParticleSystems(map, s);

            // Full game entities: created if needed and loaded; full ones without save data are gone.
            var loaded = new List<(HplEntity entity, SavedFullEntity data)>();
            foreach (var pair in s.fullEntities)
            {
                var e = Find(map, byKey, pair.Key);
                if (e == null && !string.IsNullOrEmpty(pair.Value.file))
                {
                    e = map.CreateEntity(pair.Value.name, pair.Value.file, pair.Value.position, pair.Value.rotation, Vector3.one);
                    if (e != null) e.FullGameSave = true;
                }
                if (e != null) loaded.Add((e, pair.Value));
            }
            foreach (var e in new List<HplEntity>(map.Entities))
                if (e.FullGameSave && !s.fullEntities.ContainsKey(Key(e)) && !loaded.Exists(l => l.entity == e)) map.DestroyEntity(e);
            map.DestroyPendingEntitiesNow();

            // Entity settings of the other entities.
            foreach (var e in new List<HplEntity>(map.Entities))
            {
                if (e.FullGameSave) continue;
                string key = Key(e);
                e.SetActive(!s.disabled.Contains(key));
                switch (e)
                {
                    case PropSwingDoor door:
                        door.SetLocked(s.lockedDoors.Contains(key), false);
                        break;
                    case PropLevelDoor levelDoor:
                        levelDoor.Locked = s.lockedLevelDoors.Contains(key);
                        break;
                    case PropItem:
                        if (!s.activeItems.Contains(key)) map.DestroyEntity(e);
                        break;
                    case PropObject obj:
                        // A broken container whose item was picked up holds nothing any more.
                        if (obj.ContainedItem != "" && !s.unbrokenContainers.Contains(key) && !s.brokenContainersWithActiveItem.Contains(obj.Name))
                            obj.ContainedItem = "";
                        break;
                    case PropLamp lamp:
                        lamp.SetLit(!s.unlitLamps.Contains(key), false);
                        break;
                }
            }
            map.DestroyPendingEntitiesNow();

            // Entities setup.
            foreach (var (e, data) in loaded) LoadFull(map, e, data);
        }

        static void LoadLinks(HplMap map, HplEntity e, SavedEntityLinks links)
        {
            e.CollideCallbacks.Clear();
            e.CollideCallbacks.Import(map, links.CollideCallbacks);
            e.Connections.Clear();
            foreach (var (name, entity, invert, states, callback) in links.Connections)
            {
                var other = map.GetEntityByName(entity);
                if (other != null) e.Connections.Add((name, other, invert, states, callback));
            }
        }

        static void LoadFull(HplMap map, HplEntity e, SavedFullEntity f)
        {
            e.SetActive(f.active);
            e.InteractionDisabled = f.interactionDisabled;
            e.MaxFocusDistance = f.maxFocusDistance;
            e.CallbackFunc = f.callbackFunc;
            e.ConnectionStateChangeCallback = f.stateChangeCallback;
            e.SetPlayerInteractCallback(f.interactCallback, f.interactRemove);
            e.SetPlayerLookAtCallback(f.lookAtCallback, f.lookAtRemove);
            LoadLinks(map, e, f.links);
            if (e is not HplProp prop) return;

            foreach (var pair in f.fields) pair.Key.SetValue(prop, pair.Value);
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
                for (int i = 0; i < saved.emittersActive.Length && i < ps.Emitters.Count; i++)
                    if (!saved.emittersActive[i]) ps.Emitters[i].KillInstantly();
            }
            // Placed systems that were not saved had been destroyed.
            foreach (var pair in placed)
                if (!savedPlaced.Contains(pair.Key)) pair.Value.KillInstantly();
        }
    }
}

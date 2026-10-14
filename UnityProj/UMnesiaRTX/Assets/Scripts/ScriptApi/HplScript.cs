using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using RTXMnesia.Effects;
using RTXMnesia.Player;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>Marks the C# port of a level script (.hps): the map file it belongs to, relative to the game folder.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class HplMapScriptAttribute : Attribute
    {
        public readonly string MapFile;

        public HplMapScriptAttribute(string mapFile) => MapFile = mapFile;
    }

    /// <summary>
    /// Base of the level script ports. The engine functions of LuxScriptHandler.cpp are here with their original
    /// names and parameters (partial files by topic), so a port reads like the .hps it comes from.
    ///
    /// Callbacks are named by string like in AngelScript (timers, collide callbacks, ...) and found by reflection
    /// among the methods of the level class; write them with nameof() so the compiler checks them. AngelScript
    /// conversions to keep in a port: float → int truncates ((int)RandFloat(2, 3)), "or"/"and" are ||/&&.
    /// Engine parts that do not exist yet log once that they are skipped (see docs/ScriptPort/api.md).
    /// </summary>
    public abstract partial class HplScript
    {
        public HplMap Map { get; internal set; }

        protected AmnesiaPlayer Player => Map.player;
        protected HplEffects Effects => Map.effects;
        protected HplHud Hud => Map.hud;

        /// <summary>Runs the first time the map is entered (before <see cref="OnEnter"/>).</summary>
        public virtual void OnStart() { }

        /// <summary>Runs every time the map is entered.</summary>
        public virtual void OnEnter() { }

        /// <summary>Runs when the map is left.</summary>
        public virtual void OnLeave() { }

        // ------------------------------------------------------------------ level script lookup

        static Dictionary<string, Type> _scriptTypes;

        static string NormalizeMap(string mapFile) => (mapFile ?? "").Replace('\\', '/').Trim('/').ToLowerInvariant();

        /// <summary>The <see cref="HplScript"/> class marked for this map, or null.</summary>
        public static Type FindScriptType(string mapFile)
        {
            if (_scriptTypes == null)
            {
                _scriptTypes = new Dictionary<string, Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = assembly.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { types = Array.FindAll(e.Types, t => t != null); }
                    foreach (var type in types)
                    {
                        if (type.IsAbstract || !typeof(HplScript).IsAssignableFrom(type)) continue;
                        var attribute = type.GetCustomAttribute<HplMapScriptAttribute>();
                        if (attribute == null) continue;
                        string key = NormalizeMap(attribute.MapFile);
                        if (!_scriptTypes.TryAdd(key, type))
                            Debug.LogError($"[RTXMnesia] Two level scripts for '{key}': {_scriptTypes[key].Name} and {type.Name}");
                    }
                }
            }
            return _scriptTypes.TryGetValue(NormalizeMap(mapFile), out var found) ? found : null;
        }

        // ------------------------------------------------------------------ calls from the engine

        Dictionary<string, MethodInfo> _methods;

        void CollectMethods()
        {
            _methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var type = GetType(); type != null && type != typeof(HplScript); type = type.BaseType)
            {
                foreach (var method in type.GetMethods(flags))
                {
                    if (method.IsSpecialName) continue;
                    if (!_methods.TryAdd(method.Name, method) && _methods[method.Name].DeclaringType == type)
                        HplLog.Warn($"{type.Name}: several functions named '{method.Name}', callbacks use the first");
                }
            }
        }

        /// <summary>A script function by name with the arguments the engine passes (cLuxMap::RunScript).</summary>
        internal void Invoke(string function, object[] args)
        {
            if (_methods == null) CollectMethods();
            if (string.IsNullOrEmpty(function))
            {
                HplLog.Error("Script call without a function name");
                return;
            }
            if (!_methods.TryGetValue(function, out var method))
            {
                HplLog.Error($"Script function '{function}' does not exist in {GetType().Name}");
                return;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != args.Length)
            {
                HplLog.PortError($"Script function '{function}' takes {parameters.Length} arguments, the engine passes {args.Length}");
                return;
            }
            var converted = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
                converted[i] = Convert.ChangeType(args[i], parameters[i].ParameterType, CultureInfo.InvariantCulture);

            try
            {
                method.Invoke(this, converted);
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
        }

        internal void Run(Action action, string name)
        {
            try { action(); }
            catch (Exception e)
            {
                Debug.LogError($"[Script] {GetType().Name}.{name} failed");
                Debug.LogException(e);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Logs once that an engine part this function needs is not ported yet.</summary>
        protected static void NotPorted(string function, string missing) => HplLog.Warn($"{function}: skipped, needs {missing}");

        static bool IsCorrectType(HplEntity entity, HplEntityType? type, string subType)
        {
            if (type == null) return true;
            if (entity.Type != type) return false;
            if (subType == null) return true;
            return entity switch
            {
                HplArea area => area.AreaType == subType,
                HplProp prop => subType == "Critter" ? prop.PropType.StartsWith("Critter", StringComparison.Ordinal) : prop.PropType == subType,
                _ => false,
            };
        }

        /// <summary>cLuxScriptHandler::GetEntity: by name (any case), warns if missing or of another type.</summary>
        protected HplEntity GetEntity(string name, HplEntityType? type = null, string subType = null)
        {
            var entity = Map.GetEntityByName(name);
            if (entity == null)
            {
                HplLog.Warn($"Entity '{name}' does not exist!");
                return null;
            }
            if (!IsCorrectType(entity, type, subType))
            {
                HplLog.Warn($"Entity '{name}' is not correct type! ({type} {subType})");
                return null;
            }
            return entity;
        }

        /// <summary>
        /// cLuxScriptHandler::GetEntities: one entity by name, or all entities containing every part of a "*"
        /// pattern (as the original, the parts are only tested for being contained, not their order).
        /// </summary>
        protected List<HplEntity> GetEntities(string name, HplEntityType? type = null, string subType = null)
        {
            var list = new List<HplEntity>();
            if (name == null) return list;
            if (name.IndexOf('*') < 0)
            {
                var entity = Map.GetEntityByName(name);
                if (entity == null || !IsCorrectType(entity, type, subType))
                    HplLog.Warn($"Entity '{name}' with type {type?.ToString() ?? "any"} and subtype {subType ?? "any"} does not exist!");
                else
                    list.Add(entity);
                return list;
            }

            string[] parts = name.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entity in Map.Entities)
            {
                if (!IsCorrectType(entity, type, subType)) continue;
                if (Array.TrueForAll(parts, p => entity.Name.IndexOf(p, StringComparison.Ordinal) >= 0)) list.Add(entity);
            }
            if (list.Count == 0) HplLog.Warn($"Could not find any entities with string '{name}'");
            return list;
        }

        /// <summary>Props (and enemies, as the HPL2 casts allow) of a name or pattern, optionally of one prop type.</summary>
        protected List<HplProp> GetProps(string name, string propType = null)
        {
            var list = new List<HplProp>();
            foreach (var e in GetEntities(name, HplEntityType.Prop, propType))
                if (e is HplProp p) list.Add(p);
            return list;
        }

        protected string Translate(string category, string entry) => Hud.Translate(category, entry);
    }
}

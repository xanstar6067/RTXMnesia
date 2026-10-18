using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.Dynamics;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// On-foot walkthrough check of a converted map (Justine campaign): a grid search over walkable floor with the
    /// player's standing cylinder finds what is reachable from PlayerStartArea_1 (pass "strict": locked swing doors block;
    /// pass "relaxed": every loose prop and door is passable), then the real character controller walks the found path
    /// to each reachable level door, start and item and reports where it gets stuck.
    /// Batch: -executeMethod RTXMnesia.EditorDev.JustineWalkthrough.RunBatch -amnesiaMap maps/ptest/01_cells.map
    /// [-from PlayerStartArea_1] [-grid 0.5] [-walk all|doors|none] [-globals "a=b;…"] [-noScript]. Report: Logs/Justine_Walk_&lt;map&gt;.txt.
    /// </summary>
    public static class JustineWalkthrough
    {
        static float Radius = 0.3f;       // Player_Body 0.4: smaller, the search is a pre-filter and the real controller has the last word
        const float Height = 1.75f;       // standing body 1.8
        const float CrouchHeight = 1.15f; // crouching body 1.2
        const float StepUp = 0.4f;        // maxStepHeight 0.36
        static float MaxDrop = 0.6f; // metres of fall the search accepts
        const int NodeLimit = 600000;

        struct Node
        {
            public int x, z, y;
            public bool crouch;
            public Vector3 pos;
            public int parent;
        }

        sealed class Target
        {
            public string kind, name, extra;
            public Vector3 pos;
            public Bounds bounds;
            public bool hasBounds;
            public bool locked;
        }

        public static void RunBatch()
        {
            string map = ImportMenu.Arg("-amnesiaMap", "maps/ptest/01_cells.map");
            string mapName = Path.GetFileNameWithoutExtension(map);
            var sb = new StringBuilder();
            void Out(string s) { sb.AppendLine(s); Debug.Log("[Walk] " + s); }
            EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map)), OpenSceneMode.Single);

            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            var hmap = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            if (player == null || hmap == null) { Out("FAIL: no player or HplMap in " + map); Save(mapName, sb); return; }

            if (int.TryParse(ImportMenu.Arg("-seed", "1"), out int seed)) HplMath.Seed(seed);
            HplGame.Reset();
            foreach (string pair in ImportMenu.Arg("-globals", "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0) HplGame.SetGlobalVar(pair.Substring(0, eq).Trim(), pair.Substring(eq + 1).Trim());
            }

            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            int exitCode = 0;
            try
            {
                Physics.SyncTransforms();
                player.readDevices = false;
                Call(player, "Awake");
                Call(player, "Start");
                hmap.scriptDebug = true; // skip introductions: the player is active and not scripted
                hmap.runScript = !ImportMenu.HasArg("-noScript");
                hmap.logDebugMessages = false;
                hmap.Build();
                hmap.Begin();
                float dt = Time.fixedDeltaTime;
                for (int i = 0; i < 120; i++) { hmap.Step(dt); }
                // -open "name,name": move objects (slide doors) opened as the solved puzzle would, before the search.
                foreach (string mo in ImportMenu.Arg("-open", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                    hmap.Script?.GetType().GetMethod("SetMoveObjectState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(float) }, null)
                        ?.Invoke(hmap.Script, new object[] { mo.Trim(), 1f });
                if (ImportMenu.Arg("-open", "") != "") for (int i = 0; i < 900; i++) hmap.Step(dt);
                Physics.SyncTransforms();

                string probe = ImportMenu.Arg("-probe", "");
                if (probe != "")
                {
                    var v = probe.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                    foreach (var c in Physics.OverlapBox(new Vector3(v[0], v[1], v[2]), new Vector3(v[3], v[4], v[5]) * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
                    {
                        var rb = c.attachedRigidbody;
                        Out($"   probe {HierPath(c.transform)} [{c.GetType().Name}] layer {c.gameObject.layer} trigger {c.isTrigger} " +
                            $"{(rb != null ? "rb kinematic=" + rb.isKinematic : "static")} bounds {c.bounds.min:F1}..{c.bounds.max:F1} active {c.gameObject.activeInHierarchy}");
                    }
                }
                var targets = CollectTargets(hmap);
                _allTargets = targets;
                Out($"=== {map} === script {hmap.Script?.GetType().Name ?? "none"}, {hmap.Entities.Count} entities, " +
                    $"{targets.Count(t => t.kind == "LevelDoor")} level doors, {targets.Count(t => t.kind == "Start")} starts, " +
                    $"{targets.Count(t => t.kind == "Item")} items, {targets.Count(t => t.kind == "SwingDoor" || t.kind == "LockedDoor")} swing doors ({targets.Count(t => t.kind == "LockedDoor")} locked)");

                string fromName = ImportMenu.Arg("-from", "PlayerStartArea_1");
                var from = targets.FirstOrDefault(t => t.kind == "Start" && t.name == fromName);
                if (from == null) { Out("FAIL: start not found: " + fromName); exitCode = 1; return; }
                float grid = float.Parse(ImportMenu.Arg("-grid", "0.5"), CultureInfo.InvariantCulture);

                var ladderLinks = LadderLinks(grid, Out);
                List<Node> relaxed = null;
                foreach (bool strict in new[] { true, false })
                {
                    var nodes = Search(from.pos, grid, strict ? targets.Where(t => t.kind == "LockedDoor").ToList() : null, ladderLinks, out bool capped);
                    if (!strict) relaxed = nodes;
                    Out($"-- search {(strict ? "strict (locked doors block)" : "relaxed (everything loose passable)")}: {nodes.Count} nodes{(capped ? " CAPPED" : "")}");
                    foreach (var t in targets.Where(t => t.kind != "LockedDoor" && t.kind != "SwingDoor"))
                    {
                        int n = NearestNode(nodes, t, grid);
                        string state = n >= 0 ? "reachable" : "NOT reachable";
                        string extra = t.extra != "" ? " " + t.extra : "";
                        if (t.kind != "Item" || !strict) Out($"   {t.kind,-9} {t.name,-34} {state}{extra} at {t.pos:F1}");
                    }
                    if (strict)
                    {
                        int items = targets.Count(t => t.kind == "Item");
                        int ok = targets.Count(t => t.kind == "Item" && NearestNode(nodes, t, grid) >= 0);
                        Out($"   Item      {ok}/{items} items reachable");
                    }
                }

                // Every start is a script-teleport hop of the original (PlayerStart areas): which targets does each one reach on foot?
                var perStart = new Dictionary<string, List<Node>>();
                foreach (var st in targets.Where(t => t.kind == "Start"))
                {
                    var ns = Search(st.pos, grid, null, ladderLinks, out _);
                    perStart[st.name] = ns;
                }
                Out("-- walkable component of each start (relaxed):");
                foreach (var st in targets.Where(t => t.kind == "Start").OrderBy(t => t.name))
                {
                    var ns = perStart[st.name];
                    var parts = new List<string>();
                    foreach (var t in targets.Where(t => t.kind != "LockedDoor" && t.kind != "SwingDoor" && t != st))
                        if (NearestNode(ns, t, grid) >= 0) parts.Add(t.kind == "Start" ? "S:" + t.name : t.kind == "LevelDoor" ? "DOOR:" + t.name : "I:" + t.name);
                    Out($"   {st.name,-20} {ns.Count,6} nodes -> {string.Join(", ", parts)}");
                }
                foreach (var door in targets.Where(t => t.kind == "LevelDoor"))
                {
                    var srcs = targets.Where(t => t.kind == "Start" && NearestNode(perStart[t.name], door, grid) >= 0).Select(t => t.name).ToList();
                    Out($"   door {door.name}: reachable from starts [{string.Join(", ", srcs)}]");
                }

                // Dead-end components (a crawl space, a shaft): where do they lead if falling up to 25 m is allowed?
                MaxDrop = 25f;
                foreach (var st in targets.Where(t => t.kind == "Start" && perStart[t.name].Count < 300))
                {
                    var ns = Search(st.pos, grid, null, ladderLinks, out _);
                    var parts = new List<string>();
                    foreach (var t in targets.Where(t => t.kind != "LockedDoor" && t.kind != "SwingDoor" && t != st))
                        if (NearestNode(ns, t, grid) >= 0) parts.Add(t.kind == "Start" ? "S:" + t.name : t.kind == "LevelDoor" ? "DOOR:" + t.name : "I:" + t.name);
                    Out($"   with falls up to 25 m, {st.name} ({perStart[st.name].Count} -> {ns.Count} nodes) reaches: {string.Join(", ", parts)}");
                }
                MaxDrop = 0.6f;

                // Gaps: the closest approach of each door-less component to a door-reaching one (a missing link of the route).
                var doorStarts = targets.Where(t => t.kind == "Start" && targets.Any(d => d.kind == "LevelDoor" && NearestNode(perStart[t.name], d, grid) >= 0)).ToList();
                if (doorStarts.Count > 0)
                {
                    var goal = perStart[doorStarts[0].name];
                    var goalSet = new HashSet<string>(doorStarts.Select(t => t.name));
                    var seenComps = new HashSet<int>();
                    foreach (var st in targets.Where(t => t.kind == "Start" && !goalSet.Contains(t.name)))
                    {
                        var ns = perStart[st.name];
                        if (!seenComps.Add(ns.Count)) continue;
                        float best = float.MaxValue; Vector3 pa = default, pb = default;
                        foreach (var a in ns) foreach (var g in goal)
                        {
                            float d = Vector3.Distance(a.pos, g.pos);
                            if (d < best) { best = d; pa = a.pos; pb = g.pos; }
                        }
                        var lo = new Vector3(ns.Min(n => n.pos.x), ns.Min(n => n.pos.y), ns.Min(n => n.pos.z));
                        var hi = new Vector3(ns.Max(n => n.pos.x), ns.Max(n => n.pos.y), ns.Max(n => n.pos.z));
                        {
                            Vector3 o = pa + Vector3.up * 0.9f, dirv = (pb - pa); dirv.y = 0f;
                            var hits = Physics.SphereCastAll(o, 0.25f, dirv.normalized, dirv.magnitude, _blockMask, QueryTriggerInteraction.Ignore)
                                              .OrderBy(h => h.distance).Take(4).ToList();
                            foreach (var h in hits)
                            {
                                var rb = h.collider.attachedRigidbody;
                                Out($"      blocker at {h.distance:0.0} m: {HierPath(h.collider.transform)} layer {h.collider.gameObject.layer}{(rb != null ? " rigidbody kinematic=" + rb.isKinematic : "")} at {h.point:F1}");
                            }
                        }
                        Out($"   GAP component of {st.name} ({ns.Count} nodes, {lo:F1}..{hi:F1}) -> door component: {best:0.0} m, {pa:F1} to {pb:F1}");
                    }
                }

                string walkFrom = ImportMenu.Arg("-walkfrom", "");
                if (walkFrom != "")
                {
                    var src = targets.FirstOrDefault(t => t.kind == "Start" && t.name == walkFrom);
                    MaxDrop = float.Parse(ImportMenu.Arg("-walkdrop", "0.6"), CultureInfo.InvariantCulture);
                    if (src == null) Out("FAIL: -walkfrom start not found");
                    else
                    {
                        var ns = Search(src.pos, grid, null, ladderLinks, out _);
                        Out($"-- leg from {src.name} (falls up to {MaxDrop} m): {ns.Count} nodes");
                        foreach (var door in targets.Where(t => t.kind == "LevelDoor"))
                        {
                            Out($"   walking from {src.name}; health {player.health}");
                            int f = WalkTargets(hmap, player, ns, new List<Target> { door }, src, grid, true, Out);
                            Out($"   health after the leg: {player.health}");
                            if (f > 0) exitCode = 1;
                        }
                    }
                    MaxDrop = 0.6f;
                }

                // Targets no start reaches: how close does the walkable floor get, and what is in between?
                var biggest = perStart.Values.OrderByDescending(v => v.Count).First();
                foreach (var t in targets.Where(t => (t.kind == "LevelDoor" || t.kind == "Lever" || t.kind == "Wheel" || t.kind == "Item")))
                {
                    if (perStart.Values.Any(v => NearestNode(v, t, grid) >= 0)) continue;
                    Vector3 goal = t.hasBounds ? t.bounds.center : t.pos;
                    int bi = -1; float bd = float.MaxValue;
                    for (int i = 0; i < biggest.Count; i++)
                    {
                        float d = Vector3.Distance(biggest[i].pos + Vector3.up * 0.9f, goal);
                        if (d < bd) { bd = d; bi = i; }
                    }
                    Out($"   UNREACHED {t.kind} {t.name} at {t.pos:F1}: nearest floor {bd:0.0} m away at {(bi >= 0 ? biggest[bi].pos : default):F1}");
                    if (bi >= 0)
                    {
                        Vector3 o = biggest[bi].pos + Vector3.up * 0.9f, dv = goal - o;
                        foreach (var h in Physics.SphereCastAll(o, 0.25f, dv.normalized, dv.magnitude, _blockMask, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).Take(5))
                        {
                            var rb = h.collider.attachedRigidbody;
                            Out($"      between: {HierPath(h.collider.transform)} at {h.distance:0.0} m{(rb != null ? " rb kinematic=" + rb.isKinematic : "")} {h.point:F1}");
                        }
                    }
                }

                string walk = ImportMenu.Arg("-walk", "doors");
                if (walk != "none")
                {
                    int fails = 0;
                    foreach (var door in targets.Where(t => t.kind == "LevelDoor" || (walk == "all" && t.kind == "Item")))
                    {
                        var src = targets.Where(t => t.kind == "Start" && NearestNode(perStart[t.name], door, grid) >= 0)
                                         .OrderBy(t => t.name == fromName ? 0 : 1).FirstOrDefault();
                        if (src == null) { Out($"   WALK {door.kind} {door.name}: no start reaches it"); fails++; continue; }
                        Out($"   walking from {src.name}");
                        fails += WalkTargets(hmap, player, perStart[src.name], new List<Target> { door }, src, grid, true, Out);
                    }
                    if (fails > 0) exitCode = 1;
                }
            }
            catch (Exception e)
            {
                Out("EXCEPTION: " + e);
                exitCode = 1;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                Save(mapName, sb);
            }
            if (exitCode != 0) Debug.Log("[Walk] RESULT: problems found");
            else Debug.Log("[Walk] RESULT: PASS");
        }

        static string HierPath(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        static void Save(string mapName, StringBuilder sb)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", $"Justine_Walk_{mapName}.txt"));
            File.WriteAllText(path, sb.ToString());
        }

        static List<Target> CollectTargets(HplMap map)
        {
            var list = new List<Target>();
            foreach (var area in UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Include))
            {
                if (area.areaType != "PlayerStart") continue;
                list.Add(new Target { kind = "Start", name = area.name, pos = area.transform.position, extra = "" });
            }
            foreach (var e in map.Entities)
            {
                string type = e.GetType().Name;
                var go = e.GameObject;
                if (go == null) continue;
                var colliders = go.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
                Bounds b = default;
                bool has = false;
                foreach (var c in colliders)
                {
                    if (!has) { b = c.bounds; has = true; }
                    else b.Encapsulate(c.bounds);
                }
                if (type == "PropLevelDoor")
                {
                    var door = (PropLevelDoor)e;
                    list.Add(new Target
                    {
                        kind = "LevelDoor", name = e.Name, pos = go.transform.position, bounds = b, hasBounds = has,
                        extra = $"-> {door.MapFile}{(door.Locked ? " LOCKED" : "")}", locked = door.Locked
                    });
                }
                else if (type == "PropLever" || type == "PropWheel")
                    list.Add(new Target { kind = type.Substring(4), name = e.Name, pos = go.transform.position, bounds = b, hasBounds = has, extra = "" });
                else if (type == "PropItem") list.Add(new Target { kind = "Item", name = e.Name, pos = go.transform.position, bounds = b, hasBounds = has, extra = "" });
                else if (type == "PropSwingDoor")
                {
                    var sd = (PropSwingDoor)e;
                    list.Add(new Target { kind = sd.Locked ? "LockedDoor" : "SwingDoor", name = e.Name, pos = go.transform.position, bounds = b, hasBounds = has, locked = sd.Locked, extra = sd.Locked ? "LOCKED" : "" });
                }
            }
            return list;
        }

        // ---- grid search ------------------------------------------------------------------------------------------

        static List<Target> _allTargets;

        static readonly Collider[] Overlap = new Collider[64];
        static int _blockMask;

        static bool LastCrouch;

        static bool BodyFree(Vector3 feet, List<Target> blockers)
        {
            LastCrouch = false;
            if (BodyFreeAt(feet, Height, blockers)) return true;
            if (BodyFreeAt(feet, CrouchHeight, blockers)) { LastCrouch = true; return true; }
            return false;
        }

        static bool BodyFreeAt(Vector3 feet, float height, List<Target> blockers)
        {
            Vector3 p0 = feet + Vector3.up * (StepUp + Radius);
            Vector3 p1 = feet + Vector3.up * Mathf.Max(StepUp + Radius, height - Radius);
            int count = Physics.OverlapCapsuleNonAlloc(p0, p1, Radius, Overlap, _blockMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Overlap[i];
                var rb = c.attachedRigidbody;
                bool loose = rb != null && !rb.isKinematic;
                if (loose)
                {
                    if (blockers == null) continue;
                    bool isBlocker = false;
                    foreach (var t in blockers)
                        if (t.hasBounds && t.bounds.Contains(c.bounds.center)) { isBlocker = true; break; }
                    if (!isBlocker) continue;
                }
                return false;
            }
            return true;
        }

        static bool Ground(Vector3 around, float fromY, out float y)
        {
            y = 0f;
            var origin = new Vector3(around.x, fromY + StepUp + 0.1f, around.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, StepUp + 0.1f + MaxDrop, _blockMask, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (var h in hits)
            {
                var rb = h.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic) continue; // loose prop is not floor
                if (h.normal.y < 0.6f) continue;
                if (h.point.y > best) best = h.point.y;
            }
            if (float.IsNegativeInfinity(best)) return false;
            y = best;
            return true;
        }

        static List<Node> Search(Vector3 start, float grid, List<Target> blockers, List<Vector3[][]> links, out bool capped)
        {
            _blockMask = Physics.DefaultRaycastLayers & ~(1 << HplPhysicsWorld.NoCollideLayer) & ~(1 << 2);
            var nodes = new List<Node>();
            var seen = new HashSet<long>();
            long Key(int x, int z, int y) => ((long)(x + 20000) << 40) | ((long)(z + 20000) << 20) | (uint)(y + 500);
            capped = false;

            float savedDrop = MaxDrop;
            MaxDrop = Mathf.Max(MaxDrop, 3f);
            Ground(start, start.y, out float y0);
            MaxDrop = savedDrop;
            var s = new Vector3(start.x, y0, start.z);
            if (!BodyFree(s, blockers)) { s.y += 0.05f; BodyFree(s, blockers); }
            bool c0 = LastCrouch;
            var n0 = new Node { crouch = c0, x = Mathf.RoundToInt(s.x / grid), z = Mathf.RoundToInt(s.z / grid), y = Mathf.RoundToInt(s.y / 0.5f), pos = s, parent = -1 };
            nodes.Add(n0);
            seen.Add(Key(n0.x, n0.z, n0.y));
            for (int head = 0; head < nodes.Count; head++)
            {
                if (nodes.Count >= NodeLimit) { capped = true; break; }
                var n = nodes[head];
                if (links != null)
                    foreach (var link in links)
                        for (int side = 0; side < 2; side++)
                        {
                            bool near = false;
                            foreach (var sp in link[side])
                                if (Vector2.Distance(new Vector2(sp.x, sp.z), new Vector2(n.pos.x, n.pos.z)) < 0.75f && Mathf.Abs(sp.y - n.pos.y) < 0.6f) { near = true; break; }
                            if (!near) continue;
                            foreach (var other in link[1 - side])
                            {
                                int ox = Mathf.RoundToInt(other.x / grid), oz = Mathf.RoundToInt(other.z / grid), oy = Mathf.RoundToInt(other.y / 0.5f);
                                if (!seen.Add(Key(ox, oz, oy))) continue;
                                nodes.Add(new Node { crouch = false, x = ox, z = oz, y = oy, pos = other, parent = head });
                            }
                        }
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = n.x + dx, nz = n.z + dz;
                        var xz = new Vector3(nx * grid, 0f, nz * grid);
                        if (!Ground(xz, n.pos.y, out float gy)) continue;
                        if (Mathf.Abs(gy - n.pos.y) > StepUp + 0.001f && gy > n.pos.y) continue;
                        var np = new Vector3(xz.x, gy, xz.z);
                        // Diagonal: both orthogonal neighbours must be free too (no corner cutting).
                        if (dx != 0 && dz != 0)
                        {
                            var a = new Vector3(n.pos.x + dx * grid, n.pos.y, n.pos.z);
                            var b = new Vector3(n.pos.x, n.pos.y, n.pos.z + dz * grid);
                            if (!BodyFree(a, blockers) || !BodyFree(b, blockers)) continue;
                        }
                        if (!BodyFree(np, blockers)) continue;
                        bool npCrouch = LastCrouch;
                        // Half way, at the interpolated height.
                        var mid = Vector3.Lerp(n.pos, np, 0.5f);
                        if (!BodyFree(mid, blockers)) continue;
                        int ny = Mathf.RoundToInt(gy / 0.5f);
                        long k = Key(nx, nz, ny);
                        if (!seen.Add(k)) continue;
                        nodes.Add(new Node { crouch = npCrouch, x = nx, z = nz, y = ny, pos = np, parent = head });
                    }
            }
            return nodes;
        }

        // A ladder joins the free floor spots in front of its bottom and top (the climb itself is PlayerStateLadder).
        static List<Vector3[][]> LadderLinks(float grid, Action<string> Out)
        {
            _blockMask = Physics.DefaultRaycastLayers & ~(1 << HplPhysicsWorld.NoCollideLayer) & ~(1 << 2);
            var links = new List<Vector3[][]>();
            foreach (var area in UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Include))
            {
                if (area.areaType != "Ladder") continue;
                var box = area.GetComponent<BoxCollider>();
                if (box == null) continue;
                Vector3 sz = Vector3.Scale(box.size, area.transform.lossyScale);
                Bounds b = new Bounds(area.transform.TransformPoint(box.center), new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z)));
                Vector3 fwd = area.transform.forward; fwd.y = 0f; fwd.Normalize();
                List<Vector3> Spots(float y)
                {
                    var list = new List<Vector3>();
                    for (float ox = -1.5f; ox <= 1.5f; ox += 0.25f)
                        for (float oz = -1.5f; oz <= 1.5f; oz += 0.25f)
                        {
                            Vector3 xz = new Vector3(b.center.x + ox, 0f, b.center.z + oz);
                            // Not inside the ladder itself: at least 0.5 m off its box in the thin direction.
                            if (Mathf.Abs(ox) < 0.5f && Mathf.Abs(oz) < 0.5f) continue;
                            if (!Ground(xz, y + 0.3f, out float gy)) continue;
                            if (Mathf.Abs(gy - y) > 1.2f) continue;
                            var p = new Vector3(xz.x, gy, xz.z);
                            if (BodyFree(p, null)) list.Add(p);
                        }
                    return list;
                }
                var bottom = Spots(b.min.y);
                var top = Spots(b.max.y);
                Out($"   ladder {area.name}: active {area.gameObject.activeSelf}, y {b.min.y:F1}..{b.max.y:F1}, floor spots bottom {bottom.Count} top {top.Count}");
                if (bottom.Count > 0 && top.Count > 0) links.Add(new[] { bottom.ToArray(), top.ToArray() });
            }
            return links;
        }

        static int NearestNode(List<Node> nodes, Target t, float grid)
        {
            int best = -1;
            float bestD = float.MaxValue;
            float reach = t.kind == "Start" ? grid * 1.5f : (t.kind == "LevelDoor" ? 1.6f : 1.5f);
            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 p = nodes[i].pos;
                float d;
                if (t.hasBounds)
                {
                    Vector3 cp = t.bounds.ClosestPoint(p + Vector3.up * 0.9f);
                    d = Vector3.Distance(cp, p + Vector3.up * 0.9f);
                }
                else
                {
                    Vector3 dv = t.pos - p;
                    dv.y = Mathf.Abs(dv.y) > 2.2f ? 100f : 0f;
                    d = dv.magnitude;
                }
                if (d < bestD) { bestD = d; best = i; }
            }
            return bestD <= reach ? best : -1;
        }

        /// <summary>
        /// Walks the player on foot (search + real controller) from where it stands to an entity or area by name, as the
        /// "walkto Name [maxDrop]" command of PlayerSimulation. Returns 0 on success.
        /// </summary>
        public static int WalkTo(HplMap map, AmnesiaPlayer player, string name, Action<string> Out, float drop = 0.6f)
        {
            Target t = null;
            var entity = map.GetEntityByName(name);
            if (entity != null)
            {
                var cols = entity.GameObject.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
                t = new Target { kind = "Entity", name = name, pos = entity.GameObject.transform.position, extra = "" };
                foreach (var c in cols)
                {
                    if (!t.hasBounds) { t.bounds = c.bounds; t.hasBounds = true; }
                    else t.bounds.Encapsulate(c.bounds);
                }
            }
            else
            {
                var area = Array.Find(UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Include), a => a.name == name);
                if (area != null) t = new Target { kind = "Area", name = name, pos = area.transform.position, extra = "" };
            }
            if (t == null) { Out($"   WALK FAIL {name}: no entity or area"); return 1; }
            _blockMask = Physics.DefaultRaycastLayers & ~(1 << HplPhysicsWorld.NoCollideLayer) & ~(1 << 2);
            Physics.SyncTransforms();
            var links = LadderLinks(0.5f, _ => { });
            _allTargets = CollectTargets(map);
            MaxDrop = drop;
            var nodes = Search(player.Body.FeetPosition, 0.5f, null, links, out _);
            MaxDrop = 0.6f;
            var from = new Target { kind = "Start", name = "here", pos = player.Body.FeetPosition };
            float yaw = player.Yaw;
            return WalkTargets(map, player, nodes, new List<Target> { t }, from, 0.5f, true, Out);
        }

        /// <summary>
        /// "pull Name": walk to a lever, look at its handle, grab it and move the mouse (down, then up) until its state
        /// changes. Returns 0 when the lever moved to another state.
        /// </summary>
        public static int PullLever(HplMap map, AmnesiaPlayer player, string name, Action<string> Out)
        {
            if (!(map.GetEntityByName(name) is PropLever lever)) { Out($"   PULL FAIL {name}: no lever"); return 1; }
            if (WalkTo(map, player, name, Out) != 0) return 1;
            float dt = Time.fixedDeltaTime;
            int before = lever.CurrentState;
            foreach (float dir in new[] { 1f, -1f, 1f })
            {
                var body = lever.Bodies.Find(b => b.Physics != null && !b.Physics.IsStatic) ?? lever.Bodies[0];
                Vector3 goal = body.Physics != null ? body.Physics.WorldCenterOfMass : body.Transform.position;
                Vector3 to = goal - player.LogicCameraPosition;
                float yawT = Mathf.Atan2(to.x, to.z), pitchT = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude);
                player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yawT * Mathf.Rad2Deg) * Mathf.Deg2Rad, pitchT - player.Pitch);
                for (int i = 0; i < 20; i++) map.Step(dt);
                string focus = player.States?.Current.Describe() ?? "";
                player.DoAction(PlayerAction.Interact, true);
                for (int i = 0; i < 5; i++) map.Step(dt);
                for (int i = 0; i < 240 && lever.CurrentState == before; i++) { player.AddYaw(0f); player.AddPitch(-8f * dir); map.Step(dt); }
                player.DoAction(PlayerAction.Interact, false);
                for (int i = 0; i < 60; i++) map.Step(dt);
                if (lever.CurrentState != before)
                {
                    Out($"   PULL OK   {name}: state {before} -> {lever.CurrentState} (mouse {(dir > 0 ? "down" : "up")}; focus was {focus})");
                    return 0;
                }
            }
            Out($"   PULL FAIL {name}: state stayed {before}");
            return 1;
        }

        // ---- real controller walk ---------------------------------------------------------------------------------

        static int WalkTargets(HplMap map, AmnesiaPlayer player, List<Node> nodes, List<Target> targets, Target from,
                               float grid, bool all, Action<string> Out)
        {
            int fails = 0;
            var list = targets.Where(t => t.kind == "LevelDoor" || (all && t.kind != "LockedDoor" && t.kind != "SwingDoor")).ToList();
            Out($"-- walking {list.Count} targets with the real character controller");
            float dt = Time.fixedDeltaTime;
            foreach (var t in list)
            {
                int idx = NearestNode(nodes, t, grid);
                if (idx < 0) { Out($"   WALK {t.kind} {t.name}: skipped, not reachable in the search"); continue; }
                var pathN = new List<Node>();
                for (int i = idx; i >= 0; i = nodes[i].parent) pathN.Add(nodes[i]);
                pathN.Reverse();
                var path = pathN.Select(n => n.pos).ToList();
                // Waypoint every ~2 m (every node while crouching).
                var wps = new List<Vector3>();
                var wpCrouch = new List<bool>();
                float acc = 0f;
                for (int i = 1; i < pathN.Count; i++)
                {
                    acc += Vector3.Distance(path[i], path[i - 1]);
                    bool cr = pathN[i].crouch || pathN[i - 1].crouch;
                    bool nearDoor = (_allTargets ?? targets).Any(d => (d.kind == "SwingDoor" || d.kind == "LockedDoor") && d.hasBounds && d.bounds.SqrDistance(path[i] + Vector3.up * 0.9f) < 3f * 3f);
                    if (acc >= 2f || i == pathN.Count - 1 || cr || nearDoor) { wps.Add(path[i]); wpCrouch.Add(cr); acc = 0f; }
                }

                var onRoute = (_allTargets ?? targets).Where(d => (d.kind == "SwingDoor" || d.kind == "LockedDoor") && d.hasBounds &&
                                                 path.Any(q => d.bounds.SqrDistance(q + Vector3.up * 0.9f) < 0.5f * 0.5f)).ToList();
                if (onRoute.Count > 0) Out($"   route to {t.name} passes doors: {string.Join(", ", onRoute.Select(d => d.name + (d.locked ? " [LOCKED at start]" : "")))}");
                var pendingDoors = onRoute.Where(d => !d.locked).ToList();
                player.PlaceAt(path[0], 0f);
                player.active = true;
                player.held = default;
                for (int i = 0; i < 30; i++) map.Step(dt);
                float sign = Calibrate(map, player, dt);
                int stuckAt = -1, doorTries = 0;
                float time = 0f;
                for (int w = 0; w < wps.Count && stuckAt < 0; w++)
                {
                    float wpTime = 0f, lastDist = float.MaxValue, progressTime = 0f;
                    if ((player.MoveState != null && player.MoveState.Crouching) != wpCrouch[w]) { player.Crouch(true); map.Step(dt); }
                    while (true)
                    {
                        Vector3 feet = player.Body.FeetPosition;
                        for (int di = pendingDoors.Count - 1; di >= 0; di--)
                            if (pendingDoors[di].bounds.SqrDistance(feet + Vector3.up * 0.9f) < 2.0f * 2.0f)
                            {
                                if (map.GetEntityByName(pendingDoors[di].name) is PropSwingDoor psd && !psd.Locked)
                                    Out($"   (door {pendingDoors[di].name}: thrown open {(OpenByImpulse(map, psd, dt) ? "ok" : "FAILED")})");
                                pendingDoors.RemoveAt(di);
                            }
                        Vector3 to = wps[w] - feet;
                        float flat = new Vector2(to.x, to.z).magnitude;
                        if (flat < 0.55f) break;
                        Vector3 fwd = player.playerCamera.transform.forward;
                        fwd.y = 0f;
                        float err = Vector3.SignedAngle(fwd, new Vector3(to.x, 0f, to.z), Vector3.up);
                        player.AddLook(sign * Mathf.Clamp(err, -12f, 12f) * Mathf.Deg2Rad, 0f);
                        player.held = default;
                        player.held.forward = Mathf.Abs(err) < 35f;
                        map.Step(dt);
                        wpTime += dt; time += dt;
                        if (flat < lastDist - 0.05f) { lastDist = flat; progressTime = 0f; }
                        else progressTime += dt;
                        if (progressTime > 2.5f && doorTries < 12) { doorTries++; if (TryMoveProp(player, wps[w], Out) || TryOpenDoor(map, player, targets, dt, sign, doorTries, Out)) { progressTime = 0f; continue; } progressTime = 2.4f; continue; }
                        if (progressTime > 2.5f || wpTime > 15f) { stuckAt = w; break; }
                    }
                }
                player.held = default;
                Vector3 end = player.Body.FeetPosition;
                // Face the target and read the focus.
                string focus = "";
                if (stuckAt < 0 && t.hasBounds)
                {
                    Vector3 to = t.bounds.center - player.playerCamera.transform.position;
                    for (int i = 0; i < 30; i++)
                    {
                        Vector3 fwd = player.playerCamera.transform.forward;
                        float err = Vector3.SignedAngle(new Vector3(fwd.x, 0f, fwd.z), new Vector3(to.x, 0f, to.z), Vector3.up);
                        float pitchErr = Mathf.Asin(Mathf.Clamp(to.normalized.y, -1f, 1f)) - Mathf.Asin(Mathf.Clamp(fwd.normalized.y, -1f, 1f));
                        player.AddLook(sign * Mathf.Clamp(err, -20f, 20f) * Mathf.Deg2Rad, 0f);
                        map.Step(dt);
                    }
                    for (int i = 0; i < 20; i++) map.Step(dt);
                    focus = $" focus \"{player.FocusText.Replace('\n', ' ')}\" crosshair {player.States?.Current.GetCrosshair()?.ToString() ?? "none"}";
                }
                float distEnd = t.hasBounds ? Vector3.Distance(t.bounds.ClosestPoint(end + Vector3.up * 0.9f), end + Vector3.up * 0.9f) : Vector3.Distance(t.pos, end);
                if (stuckAt >= 0)
                {
                    fails++;
                    Out($"   WALK FAIL {t.kind} {t.name}: stuck at waypoint {stuckAt + 1}/{wps.Count}, feet {end:F2}, wanted {wps[stuckAt]:F2}, {time:0.0} s");
                    Vector3 dirw = wps[stuckAt] - end; dirw.y = 0f; dirw.Normalize();
                    foreach (var c in Physics.OverlapCapsule(end + Vector3.up * 0.5f + dirw * 0.25f, end + Vector3.up * 1.4f + dirw * 0.25f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var rb = c.attachedRigidbody;
                        Out($"      near: {HierPath(c.transform)} layer {c.gameObject.layer} {(rb != null ? $"rb kinematic={rb.isKinematic} mass={rb.mass:0.#}" : "static")} bounds {c.bounds.min:F2}..{c.bounds.max:F2}");
                    }
                }
                else Out($"   WALK OK   {t.kind} {t.name}: {path.Count} nodes, {time:0.0} s, ends {distEnd:0.00} m from the object{focus}");
            }
            return fails;
        }

        // Swing the door open to near its hinge limit (a person would pull it fully open before stepping through).
        static bool OpenByImpulse(HplMap map, PropSwingDoor door, float dt)
        {
            var body = door.Bodies.Find(b => b.Physics != null && !b.Physics.IsStatic)?.Physics;
            if (body == null) return false;
            var rb = body.Rigidbody;
            var hinge = rb.GetComponent<HingeJoint>();
            if (hinge == null) return false;
            door.SetClosed(false, false);
            door.DisableAutoClose = true;
            for (int i = 0; i < 10; i++) map.Step(dt);
            float min = hinge.limits.min, max = hinge.limits.max;
            float target = Mathf.Abs(max) >= Mathf.Abs(min) ? max - 3f : min + 3f;
            float delta = target - hinge.angle;
            Vector3 pivot = rb.transform.TransformPoint(hinge.anchor);
            Vector3 axis = rb.transform.TransformDirection(hinge.axis);
            var q = Quaternion.AngleAxis(delta, axis);
            rb.position = pivot + q * (rb.position - pivot);
            rb.rotation = q * rb.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            for (int i = 0; i < 30; i++) map.Step(dt);
            return Mathf.Abs(hinge.angle) > 30f;
        }

        // A loose prop wedged in the way (box, barrel): a player grabs and carries it aside; here it is set 1.6 m to the side.
        static bool TryMoveProp(AmnesiaPlayer player, Vector3 wanted, Action<string> Out)
        {
            Vector3 feet = player.Body.FeetPosition;
            Vector3 dir = wanted - feet; dir.y = 0f; dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            bool moved = false;
            foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * 0.5f + dir * 0.7f, feet + Vector3.up * 1.4f + dir * 0.7f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb.mass > 150f) continue;
                var entity = rb.GetComponentInParent<RTXMnesia.World.AmnesiaEntity>();
                if (entity != null && entity.entityType == "SwingDoor") continue;
                float s = Vector3.Dot(rb.position - feet, side) >= 0f ? 1f : -1f;
                rb.position += side * s * 1.6f + Vector3.up * 0.05f;
                rb.linearVelocity = Vector3.zero;
                moved = true;
                Out($"   (prop {(entity != null ? entity.name : rb.name)} mass {rb.mass:0.#}: carried aside)");
            }
            if (moved) Physics.SyncTransforms();
            return moved;
        }

        // The player at a closed door: look at it, grab it and move the mouse back or forth until it swings.
        static bool TryOpenDoor(HplMap map, AmnesiaPlayer player, List<Target> targets, float dt, float sign, int attempt, Action<string> Out)
        {
            Vector3 feet = player.Body.FeetPosition;
            PropSwingDoor best = null;
            float bestD = 2.2f;
            foreach (var e in map.Entities)
            {
                if (!(e is PropSwingDoor d) || d.Locked) continue;
                var cols = d.GameObject.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();
                if (cols.Length == 0) continue;
                Bounds b = cols[0].bounds;
                foreach (var c in cols) b.Encapsulate(c.bounds);
                float dist = b.SqrDistance(feet + Vector3.up * 0.9f);
                if (dist < bestD * bestD && Mathf.Sqrt(dist) < bestD) { bestD = Mathf.Sqrt(dist); best = d; }
            }
            if (best == null) return false;
            var door = best;
            var bodies = door.GameObject.GetComponentsInChildren<Rigidbody>().Where(r => !r.isKinematic).ToArray();
            if (bodies.Length == 0) return false;
            Vector3 center = bodies[0].worldCenterOfMass;
            Vector3 startPos = center;
            Quaternion startRot = bodies[0].rotation;
            door.SetClosed(false, false);
            for (int i = 0; i < 20; i++) map.Step(dt);
            // Face the door, grab, push (mouse up) and, if it did not swing, pull with the mouse.
            foreach (float dir in new[] { 1f, -1f })
            {
                Vector3 to = center - player.LogicCameraPosition;
                float yawT = Mathf.Atan2(to.x, to.z), pitchT = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude);
                player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yawT * Mathf.Rad2Deg) * Mathf.Deg2Rad, pitchT - player.Pitch);
                map.Step(dt);
                player.DoAction(PlayerAction.Interact, true);
                map.Step(dt);
                for (int i = 0; i < 100; i++) { player.AddYaw(0f); player.AddPitch(12f * dir); map.Step(dt); }
                player.DoAction(PlayerAction.Interact, false);
                for (int i = 0; i < 20; i++) map.Step(dt);
                float a = Quaternion.Angle(startRot, bodies[0].rotation);
                Out($"   (door {door.Name}: grabbed and moved, opened {a:0}°, mouse {(dir > 0 ? "push" : "pull")})");
                if (a > 15f) return true;
            }
            return false;
        }


        static float Calibrate(HplMap map, AmnesiaPlayer player, float dt)
        {
            Vector3 f0 = player.playerCamera.transform.forward; f0.y = 0f;
            player.AddLook(0.2f, 0f);
            map.Step(dt);
            Vector3 f1 = player.playerCamera.transform.forward; f1.y = 0f;
            float turned = Vector3.SignedAngle(f0, f1, Vector3.up);
            return turned >= 0f ? 1f : -1f;
        }

        static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}

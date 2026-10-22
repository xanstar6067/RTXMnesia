"""Lists what lights a map of Amnesia, for the enhanced lighting overlay (docs/EnhancedLighting/README.md).

    python tools/List-MapLights.py maps/main/ch01/01_old_archives.map [--box xmin zmin xmax zmax] [--csv out.csv]

Reads source/<map> (HPL2 coordinates, as the overlay uses them) and prints:
  - every PointLight / SpotLight / BoxLight: position, radius, colour, shadows, gobo, flicker;
    "fill?" marks a map light without CastShadows (the enhanced mode treats it as a fill light);
  - lamp entities (EntityType="Lamp" in their .ent): the general lamp rule already moves and rescales their lights;
  - static objects with "window" in their path (where sky / moon light comes from);
  - which light names the map's .hps script uses (keep their names: the script fades / hides them).
--box limits the output to a floor rectangle (HPL x / z).
"""
import argparse
import csv
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "source")
ATTR = re.compile(r'(\w+)="([^"]*)"')


def attrs(text):
    return dict(ATTR.findall(text))


def vec(s):
    try:
        return [float(v) for v in s.split()]
    except ValueError:
        return [0.0, 0.0, 0.0]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map")
    ap.add_argument("--box", nargs=4, type=float)
    ap.add_argument("--csv")
    a = ap.parse_args()
    path = os.path.join(ROOT, a.map)
    text = open(path, encoding="latin-1").read()

    def inside(p):
        if not a.box:
            return True
        x0, z0, x1, z1 = a.box
        return min(x0, x1) <= p[0] <= max(x0, x1) and min(z0, z1) <= p[2] <= max(z0, z1)

    def files(section):
        m = re.search(r"<%s[^>]*>(.*?)</%s>" % (section, section), text, re.S)
        return {int(f["Id"]): f["Path"] for f in map(attrs, re.findall(r"<File ([^>]*)/>", m.group(1)))} if m else {}

    static_files, entity_files = files("FileIndex_StaticObjects"), files("FileIndex_Entities")
    rows = []
    for kind, body in re.findall(r"<(PointLight|SpotLight|BoxLight) ([^>]*)>", text):
        e = attrs(body)
        p = vec(e.get("WorldPos", "0 0 0"))
        if not inside(p):
            continue
        rows.append({
            "kind": kind, "name": e.get("Name"), "pos": e.get("WorldPos"), "rot": e.get("Rotation"),
            "radius": e.get("Radius"), "scale": e.get("Scale") if kind == "BoxLight" else "",
            "color": e.get("DiffuseColor"), "shadows": e.get("CastShadows"),
            "fov": e.get("FOV", ""), "gobo": e.get("Gobo", ""), "flicker": e.get("FlickerActive"),
            "active": e.get("Active"), "fill?": "fill?" if kind != "BoxLight" and e.get("CastShadows") != "true" else "",
        })

    lamps, windows = [], []
    lamp_cache = {}
    for body in re.findall(r"<Entity ([^>]*)>", text):
        e = attrs(body)
        p = vec(e.get("WorldPos", "0 0 0"))
        f = entity_files.get(int(e.get("FileIndex", "-1")), "")
        if not inside(p) or not f:
            continue
        if f not in lamp_cache:
            ent = os.path.join(ROOT, f)
            lamp_cache[f] = os.path.isfile(ent) and 'EntityType="Lamp"' in open(ent, encoding="latin-1").read()
        if lamp_cache[f]:
            lamps.append((e.get("Name"), f, e.get("WorldPos")))
    for body in re.findall(r"<StaticObject ([^>]*)>", text):
        e = attrs(body)
        p = vec(e.get("WorldPos", "0 0 0"))
        f = static_files.get(int(e.get("FileIndex", "-1")), "")
        if inside(p) and "window" in f.lower():
            windows.append((e.get("Name"), f, e.get("WorldPos"), e.get("Rotation"), e.get("Scale")))

    hps = os.path.splitext(path)[0] + ".hps"
    script = open(hps, encoding="latin-1").read() if os.path.isfile(hps) else ""
    # Light functions of the script get a literal name or a prefix completed in a loop ("LightOff_"+i); comments skipped.
    code = re.sub(r"//[^\n]*|/\*.*?\*/", "", script, flags=re.S)
    literals = set(re.findall(r'"([^"\n]+)"', code))
    used = sorted({r["name"] for r in rows if r["name"] and any(r["name"] == s or (r["name"].startswith(s) and s.endswith("_")) for s in literals)})

    cols = ["kind", "name", "pos", "rot", "radius", "scale", "color", "shadows", "fov", "gobo", "flicker", "active", "fill?"]
    print("== Lights (%d)" % len(rows))
    for r in rows:
        print("  " + " | ".join("%s=%s" % (c, r[c]) for c in cols if r[c] not in ("", None)))
    print("== Lamp entities (%d): their lights follow the general lamp rule" % len(lamps))
    for n, f, p in lamps:
        print("  %s  %s  %s" % (n, f, p))
    print("== Window static objects (%d)" % len(windows))
    for n, f, p, rot, s in windows:
        print("  %s  %s  pos=%s rot=%s scale=%s" % (n, f, p, rot, s))
    print("== Lights named in %s (%d): keep them, change only how they draw" % (os.path.basename(hps), len(used)))
    print("  " + ", ".join(used))
    if a.csv:
        with open(a.csv, "w", newline="", encoding="utf-8") as out:
            w = csv.DictWriter(out, cols)
            w.writeheader()
            w.writerows(rows)


if __name__ == "__main__":
    sys.exit(main())

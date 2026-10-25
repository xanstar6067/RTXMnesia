"""Relief maps from the hand-authored HPL sources instead of the PBRify height.

The PBRify height and roughness of these materials follow albedo brightness (painted pits become
holes, dark spots become glossy). The original game files carry the shapes the artists intended:

  wall    castlebase_wall_nrm.dds (no height map): the normal map of the brick island is
          integrated into a height field, then segmented into stone / mortar.
  floor   castlebase_floor_nrm.dds alpha = HPL depth map of the paving joints (tiling).
  boards  cellarbase_wood_floor_height.tga: bed joints; the end joints are found in the albedo.
  dungeon dungeonbase atlas (PBRify x2, 4096): stone islands from dungeonbase_height.tga scaled x2.

Stones get a clean profile from the distance to the joint: faces at the original plane, rounded
shoulders, joints recessed behind them, so map decals stay visible. Boards are modelled as
separate planks by the Unity side (Assets/Editor/Dev/ManualReliefDevRoomStand.cs); here they get
their joint layout, normal and mask maps.

Outputs go to sourceRTXRemix/<material>/manual-relief-v1/ (derived from game resources -> not in git):
  stone_mask.png  white = stone (wall: gray 128 outside the brick island). May be edited by hand
                  and passed back with --mask.
  height.png      L, metres = lo + value / 255 * (hi - lo), see relief.json (stones only)
  normal.png      RGB, Unity tangent space (G up)
  mask.png        RGBA, HDRP mask map: R metallic 0, G occlusion, B detail 1, A smoothness
  specular.png    RGB, dungeon only: specular coverage, F0 = 0.04 x value (LitSpecular)
  relief.json     parameters and encodings (boards: rows / end joints in image coordinates)
  preview_*.png   inspection images

Usage: python Build-ManualRelief.py wall|floor|boards|dungeon [options]
Requires numpy, scipy, Pillow (ComfyUI's Python has them).
"""
import argparse
import json
import os

import numpy as np
from PIL import Image
from scipy import fft, ndimage, signal

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MATERIALS = {
    'wall': 'static_objects/castlebase/wall/castlebase_wall',
    'floor': 'static_objects/castlebase/castlebase_floor',
    'boards': 'static_objects/cellarbase/floor/cellarbase_wood_floor',
    'dungeon': 'static_objects/dungeonbase/dungeonbase',
}
LO, HI = (0.025, 0.66), (0.68, 0.975)       # wall atlas UV of the brick island (v up)
ISLAND_METRES = 2.2                          # island width on the wall, as on the dev_room stand
TILE_METRES = 2.2                            # floor / boards: one texture repeat, as on the stand
HEIGHT_RANGE = (-0.032, 0.004)               # height.png encoding, metres


def load(path, mode):
    return np.asarray(Image.open(path).convert(mode), dtype=np.float64) / 255.0


def island(size):
    """Pixel box (row0, row1, col0, col1) of the wall island in an image with v pointing down."""
    return (int(round((1 - HI[1]) * size)), int(round((1 - LO[1]) * size)),
            int(round(LO[0] * size)), int(round(HI[0] * size)))


def integrate(p, q):
    """Least-squares height from slopes (x right, y down) with Neumann borders (DCT Poisson)."""
    h, w = p.shape
    div = np.zeros_like(p)
    div[:, 1:] += p[:, 1:] - p[:, :-1]
    div[:, 0] += p[:, 0]
    div[1:, :] += q[1:, :] - q[:-1, :]
    div[0, :] += q[0, :]
    d = fft.dctn(div, norm='ortho')
    yy, xx = np.meshgrid(np.arange(h), np.arange(w), indexing='ij')
    denom = (2 * np.cos(np.pi * xx / w) - 2) + (2 * np.cos(np.pi * yy / h) - 2)
    denom[0, 0] = 1
    d /= denom
    d[0, 0] = 0
    return fft.idctn(d, norm='ortho')


def residual(z, p, q):
    gx = np.zeros_like(z); gx[:, :-1] = z[:, 1:] - z[:, :-1]
    gy = np.zeros_like(z); gy[:-1, :] = z[1:, :] - z[:-1, :]
    return float(np.mean((gx[:, :-1] - p[:, 1:]) ** 2) + np.mean((gy[:-1, :] - q[1:, :]) ** 2))


def disk(radius):
    r = int(np.ceil(radius))
    y, x = np.ogrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= radius * radius


def pbrify_slopes(a, name):
    """PBRify normal as Unity tangent slopes (its G points down the image: flipped once)."""
    pbn = load(os.path.join(a.pbrify, name + '__normal_ogl.png'), 'RGB') * 2 - 1
    pz = np.maximum(pbn[..., 2], 0.15)
    return pbn[..., 0] / pz, -pbn[..., 1] / pz


def fine(slope, radius):
    return slope - ndimage.uniform_filter(slope, 2 * int(radius) + 1, mode='wrap')


def normal_from(sx, sy):
    normal = np.dstack([sx, sy, np.ones_like(sx)])
    return normal / np.linalg.norm(normal, axis=2, keepdims=True)


def roughness_variation(a, name):
    r = load(os.path.join(a.pbrify, name + '__roughness.png'), 'RGB')[..., 0]
    return r, (r - r.mean()) / (r.std() + 1e-6)


def profile(inside, bevel):
    t = np.clip(inside / bevel, 0, 1)
    return 1 - (1 - t) ** 2.2


def save(out, name, array, mode):
    Image.fromarray(np.clip(np.round(array * 255), 0, 255).astype(np.uint8), mode).save(os.path.join(out, name))


def encode_height(height):
    return (height - HEIGHT_RANGE[0]) / (HEIGHT_RANGE[1] - HEIGHT_RANGE[0])


def previews(out, albedo, mask, height, normal, step=2):
    overlay = albedo * 0.7 + np.dstack([~mask * 0.3, mask * 0.15, np.zeros(mask.shape)])
    save(out, 'preview_mask_overlay.png', overlay[::step, ::step], 'RGB')
    if height is not None:
        save(out, 'preview_height.png', ((height - height.min()) / (np.ptp(height) + 1e-9))[::step, ::step], 'L')
    light = np.array([-0.6, 0.5, 0.62]); light /= np.linalg.norm(light)
    shade = np.clip((normal * light).sum(-1), 0, 1)
    save(out, 'preview_shading.png', (albedo * shade[..., None] * 1.6)[::step, ::step], 'RGB')


def wall(a, rng):
    name = 'castlebase_wall'
    albedo = load(os.path.join(a.pbrify, name + '__albedo.png'), 'RGB')
    size = albedo.shape[0]
    r0, r1, c0, c1 = island(size)
    metres_per_px = ISLAND_METRES / (c1 - c0)

    # 1. Integrate the original normal map of the island (1024 px atlas).
    nrm = load(os.path.join(a.source, os.path.dirname(MATERIALS['wall']), name + '_nrm.dds'), 'RGB')
    s0, s1, t0, t1 = island(nrm.shape[0])
    n = nrm[s0:s1, t0:t1] * 2 - 1
    nz = np.maximum(n[..., 2], 0.2)
    p = -n[..., 0] / nz
    best = None
    for sign in (1, -1):   # G convention is verified numerically, not assumed
        q = sign * -n[..., 1] / nz
        z = integrate(p, q)
        err = residual(z, p, q)
        if best is None or err < best[0]:
            best = (err, sign, z)
    _, g_sign, z = best
    z *= ISLAND_METRES / (t1 - t0)          # slope per pixel -> metres
    # Remove broad drift of the integration (sigma ~1/4 stone); the bevels stay. The sign
    # follows from the slope definition: stones come out high, joints low.
    hp = z - ndimage.gaussian_filter(z, (t1 - t0) / 27.0, mode='reflect')
    zoom = (r1 - r0) / hp.shape[0], (c1 - c0) / hp.shape[1]
    hp_big = ndimage.zoom(hp, zoom, order=3)[: r1 - r0, : c1 - c0]
    shape_px = hp_big.shape

    # 2. Stone / mortar mask.
    if a.mask:
        mask = load(a.mask, 'L')[r0:r1, c0:c1] > 0.75
    else:
        smooth = ndimage.gaussian_filter(hp_big, 3)
        level = np.percentile(smooth, 30)    # joints are the lowest ~quarter of the island
        mask = smooth > level
        px = 0.005 / metres_per_px
        mask = ndimage.binary_opening(mask, disk(px))
        mask = ndimage.binary_closing(mask, disk(px * 0.6))
        mask = ndimage.binary_fill_holes(mask)
        labels, count = ndimage.label(mask)
        areas = ndimage.sum(mask, labels, range(1, count + 1))
        keep = np.zeros(count + 1, bool)
        keep[1:] = areas > (0.06 / metres_per_px) ** 2   # drop specks below ~6x6 cm
        mask = keep[labels]
        # Thin head joints merge neighbours at that level. Split them: one core per stone
        # from the upper part of each bevel, every mask pixel goes to its nearest core; a cut
        # line between different stones becomes a joint again.
        core = ndimage.binary_opening(smooth > np.percentile(smooth, 60), disk(0.01 / metres_per_px))
        markers, _ = ndimage.label(core)
        nearest = ndimage.distance_transform_edt(markers == 0, return_distances=False, return_indices=True)
        grown = markers[nearest[0], nearest[1]]
        grown[~mask] = 0
        cut = np.zeros_like(mask)
        for axis in (0, 1):
            a_, b_ = np.take(grown, range(grown.shape[axis] - 1), axis), np.take(grown, range(1, grown.shape[axis]), axis)
            diff = (a_ != b_) & (a_ > 0) & (b_ > 0)
            pad = [(0, 0), (0, 0)]; pad[axis] = (0, 1)
            cut |= np.pad(diff, pad)
        mask &= ~ndimage.binary_dilation(cut, disk(0.004 / metres_per_px))
    labels, count = ndimage.label(mask)

    # 3. Height: faces at the wall plane, rounded shoulders, recessed mortar.
    shoulder = profile(ndimage.distance_transform_edt(mask) * metres_per_px, a.bevel)
    offsets = np.concatenate([[0], rng.uniform(-0.003, 0.001, count)])
    tilts = np.concatenate([[[0, 0]], rng.uniform(-0.012, 0.012, (count, 2))])
    yy, xx = np.indices(shape_px)
    com = np.array(ndimage.center_of_mass(mask, labels, range(1, count + 1))) if count else np.zeros((0, 2))
    cy = np.concatenate([[0], com[:, 0]])[labels]
    cx = np.concatenate([[0], com[:, 1]])[labels]
    tilt = (tilts[labels, 0] * (yy - cy) + tilts[labels, 1] * (xx - cx)) * metres_per_px
    face_detail = np.clip(hp_big / (np.std(hp_big) * 3), -1, 1) * 0.0025
    face = offsets[labels] + tilt + face_detail
    mortar = -a.joint_depth + np.clip(hp_big / (np.std(hp_big) * 3), -1, 1) * 0.0015
    height = np.where(mask, mortar + (face - mortar) * shoulder, mortar)
    height = ndimage.gaussian_filter(height, 1.2)
    height = np.clip(height, HEIGHT_RANGE[0], HEIGHT_RANGE[1])

    # 4. Normal: slopes of the new height + damped PBRify fine detail.
    sx, sy = pbrify_slopes(a, name)
    radius = 0.006 / metres_per_px
    fine_x, fine_y = fine(sx, radius), fine(sy, radius)
    gy, gx = np.gradient(height, metres_per_px)            # gy along rows (v down)
    keep = np.where(mask, a.detail, a.detail * 0.6)
    isl = (slice(r0, r1), slice(c0, c1))
    sx[isl] = -gx + fine_x[isl] * keep                     # normal x = -dh/du
    sy[isl] = gy + fine_y[isl] * keep                      # normal y = -dh/dv, v up = -row
    normal = normal_from(sx, sy)

    # 5. HDRP mask map. Faces 0.76-0.9 roughness with PBRify variation, mortar 0.93.
    rough, _ = roughness_variation(a, name)
    rv = rough[isl]
    rv = (rv - rv.mean()) / (rv.std() + 1e-6)              # variation within the island
    rough[isl] = np.clip(0.83 + rv * 0.03, 0.76, 0.9) * shoulder + 0.93 * (1 - shoulder)
    occlusion = np.ones_like(rough)
    occlusion[isl] = 1 - 0.45 * np.clip(-height / a.joint_depth, 0, 1) ** 1.5
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])

    full_mask = np.full((size, size), 0.5)
    full_mask[isl] = mask
    save(a.out, 'stone_mask.png', full_mask, 'L')
    full_h = np.full((size, size), encode_height(0.0))
    full_h[isl] = encode_height(height)
    save(a.out, 'height.png', full_h, 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    previews(a.out, albedo[isl], mask, height, normal[isl])
    return {'region_uv': {'lo': LO, 'hi': HI}, 'island_metres': ISLAND_METRES,
            'source_normal_g_sign': g_sign, 'stones': int(count), 'stone_share': float(mask.mean())}


def periodic_labels(mask):
    """Connected stones of a tiling texture: components touching across opposite edges merge."""
    labels, count = ndimage.label(mask)
    parent = list(range(count + 1))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for first, last in ((labels[:, 0], labels[:, -1]), (labels[0, :], labels[-1, :])):
        for x, y in zip(first, last):
            if x and y:
                parent[find(x)] = find(y)
    roots = np.array([find(i) for i in range(count + 1)])
    unique, compact = np.unique(roots, return_inverse=True)
    return compact.reshape(-1)[labels], len(unique) - 1


def floor(a, rng):
    name = 'castlebase_floor'
    albedo = load(os.path.join(a.pbrify, name + '__albedo.png'), 'RGB')
    size = albedo.shape[0]
    metres_per_px = TILE_METRES / size
    # HPL height maps are depths (white = deeper); this one is the alpha of the normal map.
    depth = np.asarray(Image.open(os.path.join(a.source, os.path.dirname(MATERIALS['floor']), name + '_nrm.dds')).convert('RGBA'),
                       dtype=np.float64)[..., 3] / 255.0
    depth = ndimage.zoom(depth, size / depth.shape[0], order=3, mode='grid-wrap')
    pad = 160                                   # every filter below works on the tiling texture
    padded = np.pad(depth, pad, mode='wrap')

    if a.mask:
        mask = load(a.mask, 'L') > 0.75
        mask_padded = np.pad(mask, pad, mode='wrap')
    else:
        smooth = ndimage.gaussian_filter(padded, 2)
        stones = smooth < a.threshold
        px = 0.004 / metres_per_px
        stones = ndimage.binary_opening(stones, disk(px))
        stones = ndimage.binary_closing(stones, disk(px * 0.6))
        labels, count = ndimage.label(stones)
        areas = ndimage.sum(stones, labels, range(1, count + 1))
        keep = np.zeros(count + 1, bool)
        keep[1:] = areas > (0.03 / metres_per_px) ** 2   # drop specks below ~3x3 cm
        stones = ndimage.binary_fill_holes(keep[labels])
        mask_padded = stones
        mask = stones[pad:-pad, pad:-pad]
    labels, count = periodic_labels(mask)

    inside = (ndimage.distance_transform_edt(mask_padded) * metres_per_px)[pad:-pad, pad:-pad]
    shoulder = profile(inside, a.bevel)
    dome = np.clip(inside / 0.06, 0, 1) ** 0.7 * 0.002          # paving stones are slightly crowned
    offsets = np.concatenate([[0], rng.uniform(-0.002, 0.001, count)])
    joint = -a.joint_depth + (depth - 0.5) * 0.002              # joints follow the original depth a little
    height = np.where(mask, joint + (offsets[labels] + dome - joint) * shoulder, joint)
    # Crown tops at the original plane (max offset 1 mm + crown 2 mm): flat map decals stay visible.
    height -= 0.003
    height = ndimage.gaussian_filter(height, 1.0, mode='wrap')
    height = np.clip(height, HEIGHT_RANGE[0], HEIGHT_RANGE[1])

    sx, sy = pbrify_slopes(a, name)
    radius = 0.006 / metres_per_px
    gx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) / (2 * metres_per_px)
    gy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) / (2 * metres_per_px)
    keep = np.where(mask, a.detail, a.detail * 0.6)
    normal = normal_from(-gx + fine(sx, radius) * keep, gy + fine(sy, radius) * keep)

    rough, rv = roughness_variation(a, name)
    rough = np.clip(0.82 + rv * 0.03, 0.74, 0.9) * shoulder + 0.95 * (1 - shoulder)
    occlusion = 1 - 0.5 * np.clip(-height / a.joint_depth, 0, 1) ** 1.5
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])

    save(a.out, 'stone_mask.png', mask.astype(np.float64), 'L')
    save(a.out, 'height.png', encode_height(height), 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    previews(a.out, albedo, mask, height, normal)
    return {'tile_metres': TILE_METRES, 'threshold': a.threshold, 'stones': int(count), 'stone_share': float(mask.mean())}


def boards(a, rng):
    name = 'cellarbase_wood_floor'
    albedo = load(os.path.join(a.pbrify, name + '__albedo.png'), 'RGB')
    size = albedo.shape[0]
    metres_per_px = TILE_METRES / size
    # Bed joints: bright (deep) lines of the original HPL depth map.
    depth = load(os.path.join(a.source, os.path.dirname(MATERIALS['boards']), name + '_height.tga'), 'L')
    profile_rows = depth.mean(1)
    peaks, _ = signal.find_peaks(profile_rows, distance=depth.shape[0] // 12, prominence=0.02)
    rows = [0.0] + [float(p / depth.shape[0]) for p in peaks] + [1.0]
    # End joints: the darkest column of each course in the albedo (one per course).
    gray = albedo.mean(-1)
    ends = []
    for r in range(len(rows) - 1):
        y0, y1 = int((rows[r] + 0.02) * size), int((rows[r + 1] - 0.02) * size)
        column = ndimage.uniform_filter1d(gray[y0:y1].mean(0), 5, mode='wrap')
        ends.append(float(np.argmin(column) / size))

    # Distance (metres) to the nearest joint of the layout, in image coordinates.
    v = (np.arange(size) + 0.5) / size
    course = np.clip(np.searchsorted(rows, v, side='right') - 1, 0, len(ends) - 1)
    to_bed = np.minimum(v - np.array(rows)[course], np.array(rows)[course + 1] - v) * TILE_METRES
    u = (np.arange(size) + 0.5) / size
    to_end = np.abs(u[None, :] - np.array(ends)[course][:, None])
    to_end = np.minimum(to_end, 1 - to_end) * TILE_METRES
    to_joint = np.minimum(to_bed[:, None], to_end)
    edge = np.clip(to_joint / 0.012, 0, 1)                   # 0 at the gap, 1 from 12 mm inward

    # The geometry carries plank faces, chamfers and gaps. The normal keeps the wood grain:
    # PBRify detail at reduced strength, its broad slopes (painted gaps) removed.
    sx, sy = pbrify_slopes(a, name)
    radius = 0.008 / metres_per_px
    normal = normal_from(fine(sx, radius) * a.detail, fine(sy, radius) * a.detail)

    rough, rv = roughness_variation(a, name)
    rough = np.clip(0.76 + rv * 0.05, 0.64, 0.88) * edge + 0.9 * (1 - edge)
    occlusion = 0.8 + 0.2 * edge
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])
    mask = to_joint > a.gap / 2

    save(a.out, 'stone_mask.png', mask.astype(np.float64), 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    previews(a.out, albedo, mask, None, normal)
    return {'tile_metres': TILE_METRES, 'rows_from_top': rows, 'ends': ends,
            'gap': a.gap, 'chamfer': a.chamfer, 'board_variation': 0.0008}


# dungeonbase atlas: stone islands as pixel boxes of the 2048 px original (x0, y0, x1, y1, image rows down), the metres per
# UV unit the dungeon modules give them (wall/default, default_short, corridor, welder_stone), the joint depth, the
# longest dead-end joint tail removed (small blobs and tails of the HPL depth map), the depth below which a pixel is stone
# (share of the island's 99th percentile) and how much the faces follow the depth (rubble: rounded cobbles, m).
# Wood (the beam strip under the rubble, the planks right of it) and the carved panels keep the PBRify shape.
DUNGEON_ISLANDS = {
    'rubble': (0, 0, 608, 608, 11.7, 0.026, 0.06, 0.45, 0.010),       # wall/default field
    'ashlar': (612, 0, 1024, 728, 8.8, 0.020, 0.08, 0.3, 0.002),      # frame pillars (unwrapped: side, front, side)
    'band': (0, 728, 1024, 1024, 8.6, 0.020, 0.08, 0.3, 0.002),       # wide courses: wall tops; pillars continue here
    'brick': (1024, 612, 1460, 1030, 7.9, 0.020, 0.08, 0.3, 0.002),
    'lower': (0, 1028, 645, 1525, 11.7, 0.024, 0.08, 0.3, 0.002),     # corridor walls
    'rubble2': (1024, 0, 1360, 608, 11.3, 0.026, 0.06, 0.45, 0.010),  # default_short, corridor
}
DUNGEON_HEIGHT_RANGE = (-0.036, 0.004)
# Columns of the 2048 atlas on the side faces of the frame pillars (wall/default, default_short: x 740-880 is the
# pillar front), rows 0-1024 (ashlar and band). The depth map has blobs and broken joint ends there ("drops" on the
# frame sides): its joints become straight courses across the strip, from the row profile.
DUNGEON_PILLAR_SIDES = ((608, 740), (880, 1024))
DUNGEON_PILLAR_ROWS = 1024


def thin(image):
    """Zhang-Suen thinning of a boolean image to 8-connected lines one pixel wide."""
    img = np.pad(image, 1).astype(np.uint8)
    while True:
        changed = False
        for step in (0, 1):
            p2, p3, p4 = img[:-2, 1:-1], img[:-2, 2:], img[1:-1, 2:]
            p5, p6, p7 = img[2:, 2:], img[2:, 1:-1], img[2:, :-2]
            p8, p9 = img[1:-1, :-2], img[:-2, :-2]
            ring = [p2, p3, p4, p5, p6, p7, p8, p9, p2]
            b = sum(ring[:8])
            a = sum(((ring[i] == 0) & (ring[i + 1] == 1)).astype(np.uint8) for i in range(8))
            if step == 0:
                c = (p2 * p4 * p6 == 0) & (p4 * p6 * p8 == 0)
            else:
                c = (p2 * p4 * p8 == 0) & (p2 * p6 * p8 == 0)
            remove = (img[1:-1, 1:-1] == 1) & (b >= 2) & (b <= 6) & (a == 1) & c
            if remove.any():
                img[1:-1, 1:-1][remove] = 0
                changed = True
        if not changed:
            return img[1:-1, 1:-1].astype(bool)


def endpoints(line):
    """Line pixels with a single run of neighbours around them (one or two adjacent ones) and isolated pixels."""
    img = np.pad(line, 1).astype(np.uint8)
    ring = [img[:-2, 1:-1], img[:-2, 2:], img[1:-1, 2:], img[2:, 2:], img[2:, 1:-1], img[2:, :-2], img[1:-1, :-2], img[:-2, :-2]]
    ring.append(ring[0])
    b = sum(ring[:8])
    a = sum(((ring[i] == 0) & (ring[i + 1] == 1)).astype(np.uint8) for i in range(8))
    return line & ((b == 0) | (a == 1) & (b <= 3))


def prune_tails(joints, length_px):
    """Joints without their dead-end tails shorter than length_px: the depth map's "drops" hanging from a joint into a
    stone. Skeleton, endpoints removed length_px times, long branches grown back along the skeleton, then the joint
    rebuilt around the kept skeleton with its own width. Returns the removed joint pixels."""
    skeleton = thin(joints)
    kept = skeleton.copy()
    order = np.full(joints.shape, -1, np.int32)               # pruning step that removed a skeleton pixel
    for step in range(length_px):
        ends = endpoints(kept)
        order[ends] = step
        kept &= ~ends
    # Branches still cut in the last step are longer than length_px: grow them back step by step in reverse order.
    # A tail that vanished earlier is not on such a path.
    front = order == length_px - 1
    grown = front.copy()
    for step in range(length_px - 2, -1, -1):
        front = ndimage.binary_dilation(front, np.ones((3, 3), bool)) & (order == step)
        grown |= front
    kept |= grown
    radius = ndimage.distance_transform_edt(joints)
    distance, (iy, ix) = ndimage.distance_transform_edt(~kept, return_indices=True)
    rebuilt = joints & (distance <= radius[iy, ix] + 1.5)
    return joints & ~rebuilt


def luminance(rgb):
    return rgb[..., 0] * 0.2126 + rgb[..., 1] * 0.7152 + rgb[..., 2] * 0.0722


def matte(rough, lum, radius_px):
    """Roughness variation that does not follow albedo brightness (PBRify's does, ~0.9), and how much darker than its
    surroundings a pixel is (dark spots and pits must not be glossier than the face)."""
    z = (rough - rough.mean()) / (rough.std() + 1e-6)
    zl = (lum - lum.mean()) / (lum.std() + 1e-6)
    resid = z - np.mean(z * zl) * zl
    resid = ndimage.gaussian_filter(resid / (resid.std() + 1e-6), 1.0)
    mu = ndimage.gaussian_filter(lum, radius_px)
    sd = np.sqrt(ndimage.gaussian_filter((lum - mu) ** 2, radius_px)) + 1e-3
    dark = np.clip((mu - lum) / sd, 0, 2) / 2
    return resid, dark


def dungeon(a, rng):
    """dungeonbase, the 4096 PBRify x2 set: albedo and fine normal detail from PBRify, stones and joints from the original
    HPL depth map (white = deeper joint) scaled x2, roughness from our own matte range (PBRify's is not used directly)."""
    name = 'dungeonbase'
    albedo = load(os.path.join(a.pbrify, name + '__albedo.png'), 'RGB').astype(np.float32)
    size = albedo.shape[0]
    scale = size / 2048
    depth = load(os.path.join(a.source, 'static_objects', 'dungeonbase', 'dungeonbase_height.tga'), 'L')
    depth = np.clip(ndimage.zoom(depth, size / depth.shape[0], order=3), 0, 1).astype(np.float32)
    rows = int(DUNGEON_PILLAR_ROWS * scale)
    for c0, c1 in DUNGEON_PILLAR_SIDES:
        strip = depth[:rows, int(c0 * scale):int(c1 * scale)]
        # A row is a joint where most of the strip is: joints crossing the side span it, blobs and tails vanish.
        course = ndimage.gaussian_filter1d(np.median(strip, axis=1), 1.5 * scale)
        strip[:] = course[:, None]
    sx, sy = pbrify_slopes(a, name)                  # whole atlas: the enhanced normal outside the islands
    sx, sy = sx.astype(np.float32), sy.astype(np.float32)
    pbr_rough = load(os.path.join(a.pbrify, name + '__roughness.png'), 'RGB')[..., 0].astype(np.float32)
    lum = luminance(albedo)

    # Outside the islands (wood, carved panels): the PBRify shape, matte range 0.72-0.92.
    resid, dark = matte(pbr_rough, lum, 0.02 / (8.8 / size))
    rough = np.clip(0.81 + resid * 0.035 + dark * 0.05, 0.72, 0.92).astype(np.float32)
    del resid, dark
    occlusion = np.ones((size, size), np.float32)
    # Specular coverage (F0 = 0.04 x value, LitSpecular): this stone is far darker than the castle wall, and at F0 0.04
    # its rough sheen is as bright as the stone itself (a grey veil). Faces 0.45-0.6, joints 0.3; outside the islands 1.
    reflectance = np.ones((size, size), np.float32)
    full_h = np.zeros((size, size), np.float32)
    full_mask = np.full((size, size), 0.5, np.float32)
    islands, stones_total = {}, 0
    pbr_height = load(os.path.join(a.pbrify, name + '__height.png'), 'RGB')[..., 0].astype(np.float32)
    for key, (x0, y0, x1, y1, metres_per_uv, joint_depth, tail, threshold, dome) in DUNGEON_ISLANDS.items():
        threshold = a.threshold or threshold
        x0, y0, x1, y1 = (int(round(v * scale)) for v in (x0, y0, x1, y1))
        isl = (slice(y0, y1), slice(x0, x1))
        mpp = metres_per_uv / size
        d = depth[isl]
        dn = np.clip(d / max(np.percentile(d, 99), 1e-3), 0, 1)
        smooth = ndimage.gaussian_filter(dn, 1.5 * scale)
        if a.mask:
            mask = load(a.mask, 'L')[isl] > 0.75
            labels, count = ndimage.label(mask)
        else:
            mask = smooth < threshold
            mask = ndimage.binary_opening(mask, disk(0.004 / mpp))
            mask = ndimage.binary_closing(mask, disk(0.0025 / mpp))
            labels, count = ndimage.label(mask)
            areas = ndimage.sum(mask, labels, range(1, count + 1))
            keep = np.zeros(count + 1, bool)
            keep[1:] = areas > (0.05 / mpp) ** 2           # drop specks below ~5x5 cm
            mask = keep[labels]
            # Only small holes: the depth map has no joints along the island border, so the whole
            # joint network is itself a "hole".
            holes, hole_count = ndimage.label(~mask)
            hole_areas = ndimage.sum(~mask, holes, range(1, hole_count + 1))
            small = np.zeros(hole_count + 1, bool)
            small[1:] = hole_areas < (0.03 / mpp) ** 2
            mask |= small[holes]
            # Faint joints merge neighbours: one core per stone from its deepest-free middle, every
            # stone pixel goes to its nearest core, a line between different stones becomes a joint.
            core = ndimage.binary_erosion(smooth < threshold * 0.35, disk(0.012 / mpp)) & mask
            markers, _ = ndimage.label(core)
            nearest = ndimage.distance_transform_edt(markers == 0, return_distances=False, return_indices=True)
            grown = markers[nearest[0], nearest[1]]
            grown[~mask] = 0
            del nearest
            cut = np.zeros_like(mask)
            for axis in (0, 1):
                a_, b_ = np.take(grown, range(grown.shape[axis] - 1), axis), np.take(grown, range(1, grown.shape[axis]), axis)
                diff = (a_ != b_) & (a_ > 0) & (b_ > 0)
                pad = [(0, 0), (0, 0)]; pad[axis] = (0, 1)
                cut |= np.pad(diff, pad)
            cut &= smooth > threshold * 0.4               # only along a faint joint, not across the plain border
            mask &= ~ndimage.binary_dilation(cut, disk(0.004 / mpp))
            # Tails of the depth map hanging into a stone ("drops" on the frame sides) become stone.
            tails = prune_tails(~mask, int(round(tail / mpp)))
            mask |= tails
            dangling = float(tails.mean())
            labels, count = ndimage.label(mask)
        sizes = ndimage.sum(mask, labels, range(1, count + 1)) * mpp * mpp
        # The plain border of an island is one big "stone": it stays flat at the plane.
        plain = np.concatenate([[False], sizes > 0.35])

        # Faces at the original plane, rounded shoulders, recessed joints (as the castle wall).
        shoulder = profile(ndimage.distance_transform_edt(mask) * mpp, a.bevel).astype(np.float32)
        offsets = np.concatenate([[0], rng.uniform(-0.003, 0.001, count)])
        tilts = np.concatenate([[[0, 0]], rng.uniform(-0.012, 0.012, (count, 2))])
        offsets[plain] = 0
        tilts[plain] = 0
        yy, xx = np.indices(mask.shape)
        com = np.array(ndimage.center_of_mass(mask, labels, range(1, count + 1))) if count else np.zeros((0, 2))
        cy = np.concatenate([[0], com[:, 0]])[labels]
        cx = np.concatenate([[0], com[:, 1]])[labels]
        tilt = (tilts[labels, 0] * (yy - cy) + tilts[labels, 1] * (xx - cx)) * mpp
        pits = np.where(mask, smooth, 0)                                # shallow pits where the depth map has them
        face = offsets[labels] + tilt - pits * dome
        mortar = 0.006 if key.startswith('rubble') else 0.003            # rubble mortar is lumpy, cut joints are not
        # Joints follow the original depth a little, plus the small lumps of the PBRify height (mortar grain).
        grain = pbr_height[isl] - ndimage.gaussian_filter(pbr_height[isl], 0.01 / mpp)
        grain = np.clip(grain / (grain.std() * 3 + 1e-6), -1, 1) * mortar * 0.5
        joint = -joint_depth - (smooth - 0.5) * mortar + grain
        height = np.where(mask, joint + (face - joint) * shoulder, joint)
        height = ndimage.gaussian_filter(height, 1.2 * scale)
        height = np.clip(height, DUNGEON_HEIGHT_RANGE[0], DUNGEON_HEIGHT_RANGE[1])
        # Stay at the plane along the island border: neighbouring islands share module edges.
        border = np.minimum.reduce([yy, xx, mask.shape[0] - 1 - yy, mask.shape[1] - 1 - xx]) * mpp
        height = (height * np.clip(border / 0.01, 0, 1)).astype(np.float32)
        del yy, xx, cy, cx, tilt

        gy, gx = np.gradient(height, mpp)                              # gy along rows (v down)
        radius = 0.006 / mpp
        keep = np.where(mask, a.detail, a.detail * 0.8)
        sx[isl] = -gx + fine(sx[isl], radius) * keep                  # normal x = -dh/du
        sy[isl] = gy + fine(sy[isl], radius) * keep                   # normal y = -dh/dv, v up = -row
        # Matte faces 0.77-0.92 (the approved castle wall: 0.76-0.9): variation not tied to brightness, dark spots and
        # pits rougher; joints 0.95.
        resid, dark = matte(pbr_rough[isl], lum[isl], 0.02 / mpp)
        face_rough = np.clip(0.84 + resid * 0.025 + dark * 0.06 + np.clip(pits / threshold, 0, 1) * 0.03, 0.77, 0.92)
        rough[isl] = face_rough * shoulder + 0.95 * (1 - shoulder)
        reflectance[isl] = (0.6 - 0.15 * dark) * shoulder + 0.3 * (1 - shoulder)
        occlusion[isl] = 1 - 0.45 * np.clip(-height / joint_depth, 0, 1) ** 1.5
        full_h[isl] = height
        full_mask[isl] = mask
        info = {'uv_lo': [x0 / size, 1 - y1 / size], 'uv_hi': [x1 / size, 1 - y0 / size], 'metres_per_uv': metres_per_uv,
                'joint_depth': joint_depth, 'threshold': threshold, 'dome': dome, 'stones': int(count), 'stone_share': float(mask.mean()),
                'face_roughness': [float(np.percentile(face_rough[mask], p)) for p in (5, 50, 95)]}
        if not a.mask:
            info.update({'tail_removed_up_to': tail, 'tails_share': dangling})
        islands[key] = info
        stones_total += count
        previews_out = os.path.join(a.out, 'preview_' + key)
        os.makedirs(previews_out, exist_ok=True)
        previews(previews_out, albedo[isl], mask, height, normal_from(sx[isl], sy[isl]), step=2)

    normal = normal_from(sx, sy)
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])
    save(a.out, 'stone_mask.png', full_mask, 'L')
    save(a.out, 'height.png', (full_h - DUNGEON_HEIGHT_RANGE[0]) / (DUNGEON_HEIGHT_RANGE[1] - DUNGEON_HEIGHT_RANGE[0]), 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    save(a.out, 'specular.png', np.dstack([reflectance] * 3), 'RGB')     # RGB: a one-channel PNG imports as R8 / Alpha8
    previews(a.out, albedo, full_mask > 0.75, full_h, normal, step=4)
    return {'height_encoding_metres': DUNGEON_HEIGHT_RANGE, 'size': size, 'islands': islands, 'stones': int(stones_total),
            'pillar_sides_2048': DUNGEON_PILLAR_SIDES, 'pillar_rows_2048': DUNGEON_PILLAR_ROWS}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('material', choices=sorted(MATERIALS))
    ap.add_argument('--source', default=os.path.join(ROOT, 'source'))
    ap.add_argument('--pbrify', help='PBRify result folder (default: <material>/pbrify-x4-batch-v1, dungeon: pbrify-x2-batch-v1)')
    ap.add_argument('--out', help='output folder (default: <material>/manual-relief-v1)')
    ap.add_argument('--mask', help='edited stone_mask.png to use instead of the automatic one')
    ap.add_argument('--joint-depth', type=float, help='joint depth behind the faces, m (wall 0.020, floor 0.014, boards 0.018; dungeon: per island)')
    ap.add_argument('--bevel', type=float, help='width of the rounded stone shoulder, m (wall 0.016, floor 0.018)')
    ap.add_argument('--detail', type=float, help='share of PBRify fine normal detail (stones 0.5, boards 0.6)')
    ap.add_argument('--threshold', type=float, help='floor / dungeon: HPL depth below which a pixel is stone (floor 0.2; dungeon: per island, 0.3 / rubble 0.45 of the island)')
    ap.add_argument('--gap', type=float, default=0.006, help='boards: gap between planks, m')
    ap.add_argument('--chamfer', type=float, default=0.0025, help='boards: plank edge chamfer, m')
    ap.add_argument('--seed', type=int, default=7)
    a = ap.parse_args()
    defaults = {'wall': (0.020, 0.016, 0.5), 'floor': (0.014, 0.018, 0.5), 'boards': (0.018, 0.0, 0.6), 'dungeon': (0.020, 0.016, 0.5)}[a.material]
    if a.threshold is None and a.material != 'dungeon': a.threshold = 0.2     # dungeon: per island
    a.joint_depth = defaults[0] if a.joint_depth is None else a.joint_depth
    a.bevel = defaults[1] if a.bevel is None else a.bevel
    a.detail = defaults[2] if a.detail is None else a.detail
    folder = os.path.join(ROOT, 'sourceRTXRemix', MATERIALS[a.material])
    a.pbrify = a.pbrify or os.path.join(folder, 'pbrify-x2-batch-v1' if a.material == 'dungeon' else 'pbrify-x4-batch-v1')
    a.out = a.out or os.path.join(folder, 'manual-relief-v1')
    os.makedirs(a.out, exist_ok=True)
    info = {'material': MATERIALS[a.material] + '.mat', 'kind': a.material,
            'height_encoding_metres': HEIGHT_RANGE, 'joint_depth': a.joint_depth, 'bevel': a.bevel,
            'detail': a.detail, 'seed': a.seed, 'mask': 'edited: ' + a.mask if a.mask else 'automatic'}
    info.update(globals()[a.material](a, np.random.default_rng(a.seed)))
    with open(os.path.join(a.out, 'relief.json'), 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, ensure_ascii=False)
    print(json.dumps(info, ensure_ascii=False))


if __name__ == '__main__':
    main()

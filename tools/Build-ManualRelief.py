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


def integrate_periodic(p, q):
    """Least-squares height from slopes of a tiling texture (FFT Poisson, wrapped borders): the Neumann solution of
    integrate() piles the slope of a tile seam up into a broad slope along the edges."""
    h, w = p.shape
    div = (p - np.roll(p, 1, 1)) + (q - np.roll(q, 1, 0))
    d = np.fft.fft2(div)
    ky = np.fft.fftfreq(h)[:, None]
    kx = np.fft.fftfreq(w)[None, :]
    denom = (2 * np.cos(2 * np.pi * kx) - 2) + (2 * np.cos(2 * np.pi * ky) - 2)
    denom[0, 0] = 1
    d /= denom
    d[0, 0] = 0
    return np.real(np.fft.ifft2(d))


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
    'ashlar': (612, 0, 1024, 728, 8.8, 0.020, 0.08, 0.3, 0.006),      # frame pillars (unwrapped: side, front, side)
    'band': (0, 728, 1024, 1024, 8.6, 0.020, 0.08, 0.3, 0.006),       # wide courses: wall tops; pillars continue here
    'brick': (1024, 612, 1460, 1030, 7.9, 0.020, 0.08, 0.3, 0.006),
    'lower': (0, 1028, 645, 1525, 11.7, 0.024, 0.08, 0.3, 0.006),     # corridor walls
    'rubble2': (1024, 0, 1360, 608, 11.3, 0.026, 0.06, 0.45, 0.010),  # default_short, corridor
}
DUNGEON_HEIGHT_RANGE = (-0.036, 0.008)
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
    # its rough sheen is as bright as the stone itself (a grey veil). Faces 0.22-0.3, joints 0.15; outside the islands 1.
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
        inside = (ndimage.distance_transform_edt(mask) * mpp).astype(np.float32)
        rubble = key.startswith('rubble')
        # Wider shoulders than the castle wall (16 mm): with 20-26 mm joints a narrow steep bevel facing an overhead
        # light drew a thin crisp light line along every joint.
        shoulder = profile(inside, a.bevel * (1.5 if rubble else 1.25)).astype(np.float32)
        # Cut stones: hewn faces, not tiles. A crown toward the middle (pillowed ashlar), more height and tilt between
        # stones, and chiselled grain from the PBRify height (band 2-12 mm: a broad smooth undulation read as polished);
        # everything about the original plane, so flat map decals stay visible.
        offsets = np.concatenate([[0], rng.uniform(-0.003, 0.001, count) if rubble else rng.uniform(-0.004, 0.002, count)])
        tilts = np.concatenate([[[0, 0]], rng.uniform(-0.012, 0.012, (count, 2)) if rubble else rng.uniform(-0.02, 0.02, (count, 2))])
        offsets[plain] = 0
        tilts[plain] = 0
        stone_crown = np.where(plain, 0.0, 0.002 if rubble else 0.003)
        crown = stone_crown[labels] * np.clip(inside / 0.05, 0, 1) ** 0.7 - (0 if rubble else 0.002) * ~plain[labels]
        hewn = (ndimage.gaussian_filter(pbr_height[isl], 0.002 / mpp) - ndimage.gaussian_filter(pbr_height[isl], 0.012 / mpp))
        hewn = np.clip(hewn / (hewn.std() * 2.5 + 1e-6), -1, 1) * 0.0015
        yy, xx = np.indices(mask.shape)
        com = np.array(ndimage.center_of_mass(mask, labels, range(1, count + 1))) if count else np.zeros((0, 2))
        cy = np.concatenate([[0], com[:, 0]])[labels]
        cx = np.concatenate([[0], com[:, 1]])[labels]
        tilt = (tilts[labels, 0] * (yy - cy) + tilts[labels, 1] * (xx - cx)) * mpp
        pits = np.where(mask, smooth, 0)                                # shallow pits where the depth map has them
        face = offsets[labels] + tilt + crown + hewn - pits * dome
        del inside, crown, hewn
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
        # Softly limited slope (~42 deg): the shoulders reach 80+ deg, and under a light from above or a lantern their
        # upper walls lit up as bright grey rims (the mortar colour of the albedo) along every joint. The mesh keeps the
        # full depth; only the shading normal is gentler.
        limit = 1 / np.sqrt(1 + (np.hypot(gx, gy) / 0.9) ** 2)
        gx, gy = gx * limit, gy * limit
        radius = 0.006 / mpp
        keep = np.where(mask, a.detail, a.detail * 0.8)
        sx[isl] = -gx + fine(sx[isl], radius) * keep                  # normal x = -dh/du
        sy[isl] = gy + fine(sy[isl], radius) * keep                   # normal y = -dh/dv, v up = -row
        # Matte faces 0.77-0.92 (the approved castle wall: 0.76-0.9): variation not tied to brightness, dark spots and
        # pits rougher; joints 0.95. The shoulders turn matte early (weight shoulder^3): their steep normals caught
        # the light as thin glassy lines along the joints.
        resid, dark = matte(pbr_rough[isl], lum[isl], 0.02 / mpp)
        face_rough = np.clip(0.84 + resid * 0.025 + dark * 0.06 + np.clip(pits / threshold, 0, 1) * 0.03, 0.77, 0.92)
        face_weight = shoulder ** 3
        rough[isl] = face_rough * face_weight + 0.95 * (1 - face_weight)
        reflectance[isl] = (0.3 - 0.08 * dark) * face_weight + 0.15 * (1 - face_weight)
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
    # Normal variance into roughness (Toksvig-like), on the islands: a normal that turns within a few texels sparkled as
    # thin bright lines under a close light (HDRP's geometric specular AA sees only the flat vertex normals).
    mean = np.dstack([ndimage.gaussian_filter(normal[..., i], 1.5 * scale) for i in range(3)])
    spread = 1 - np.linalg.norm(mean, axis=2)
    del mean
    stone = full_mask != 0.5
    rough[stone] = np.minimum(np.sqrt(rough[stone] ** 2 + 3 * spread[stone]), 1)
    del spread
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])
    save(a.out, 'stone_mask.png', full_mask, 'L')
    save(a.out, 'height.png', (full_h - DUNGEON_HEIGHT_RANGE[0]) / (DUNGEON_HEIGHT_RANGE[1] - DUNGEON_HEIGHT_RANGE[0]), 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    save(a.out, 'specular.png', np.dstack([reflectance] * 3), 'RGB')     # RGB: a one-channel PNG imports as R8 / Alpha8
    previews(a.out, albedo, full_mask > 0.75, full_h, normal, step=4)
    # over_creases: the mesh relief continues over corners and UV seams (HplReliefMeshBuilder.Surface.fadeAtCreases off).
    return {'height_encoding_metres': DUNGEON_HEIGHT_RANGE, 'size': size, 'islands': islands, 'stones': int(stones_total),
            'over_creases': True,
            # Game mesh: adaptive subdivision, an edge splits where the height leaves the straight line by > 2 mm
            # (HplReliefMeshBuilder.Surface.tolerance): 1 cm in joints, coarse faces, within the 120k budget.
            'mesh_tolerance_metres': 0.002,
            'pillar_sides_2048': DUNGEON_PILLAR_SIDES, 'pillar_rows_2048': DUNGEON_PILLAR_ROWS}


# The other stone walls and floors of chapters 2-3 and Justine (area ranking 2026-10-24), one generic recipe: the shape
# from the original HPL depth (its height map, else the alpha of its normal map, else its normal map integrated), no
# stone segmentation (atlases mix stone, wood and metal; the depth map already holds what the artists' POM showed).
# Faces stay at the plane, the deep part of the depth map becomes recessed joints, matte faces and dimmed specular as
# the approved dungeonbase. Values: material (static_objects/...), depth source ('height', 'alpha' or 'normal'), metres
# per UV unit (median of the modules, planes 1 / TileAmount), joint depth (m), the kind and optionally the stem of the
# original textures when the .mat takes another material's:
#   stone  plateau faces, joints from the deep part (walls, paving, pillars)
#   rock   continuous depth without plateau (cave-ins, rock, earth)
GENERIC = {
    'torture_chancel': ('static_objects/dungeonbase/special/torture_chancel', 'height', 26.3, 0.030, 'stone'),
    'stone_round': ('static_objects/dungeonbase/pillar/stone_round', 'height', 7.4, 0.028, 'stone'),
    'gigantic': ('static_objects/dungeonbase/pillar/gigantic', 'height', 8.2, 0.030, 'stone'),
    'castlebase_wall_extension': ('static_objects/castlebase/wall/castlebase_wall_extension', 'alpha', 5.6, 0.020, 'stone'),
    'castlebase_wall_win': ('static_objects/castlebase/wall/castlebase_wall_win', 'normal', 4.2, 0.020, 'stone'),
    'castlebase_wall_worn': ('static_objects/castlebase/wall/castlebase_wall_worn', 'alpha', 4.2, 0.018, 'stone'),
    'castlebase_wall_welder_convex': ('static_objects/castlebase/wall/castlebase_wall_welder_convex', 'normal', 3.7, 0.018, 'stone'),
    'castlebase_wall_door_straight': ('static_objects/castlebase/wall/castlebase_wall_door_straight', 'normal', 4.9, 0.018, 'stone'),
    'tall_stone': ('static_objects/castlebase/pillar/tall_stone', 'alpha', 5.2, 0.018, 'stone'),
    'murky_castlebase': ('static_objects/murky_castlebase/murky_castlebase', 'height', 5.4, 0.020, 'stone'),
    'cellarbase': ('static_objects/cellarbase/cellarbase', 'height', 9.5, 0.018, 'stone'),
    'dungeonbase_lab_wall': ('static_objects/dungeonbase/dungeonbase_lab_wall', 'height', 2.5, 0.035, 'stone'),
    # Its .mat takes the normal map (with a depth alpha) of cisternentr_wall_tile.
    'dungeon_wall_variant': ('static_objects/dungeonbase/wall/dungeon_wall_variant', 'alpha', 2.0, 0.025, 'stone',
                             'static_objects/castlebase/special/cisternentr_wall_tile'),
    'welder_sewer_stone': ('static_objects/dungeonbase/special/welder_sewer_stone', 'normal', 3.1, 0.030, 'stone'),
    'cistern_stone': ('static_objects/cisternbase/pillars/cistern_stone', 'height', 6.5, 0.030, 'stone'),
    'stone_arched': ('static_objects/cisternbase/ceiling/stone_arched', 'normal', 5.4, 0.030, 'stone'),
    'dungeonbase_wall_tileable': ('static_objects/dungeonbase/dungeonbase_wall_tileable', 'height', 2.5, 0.030, 'stone'),
    'cobbled_stone': ('static_objects/dungeonbase/floor/cobbled_stone', 'height', 4.0, 0.022, 'stone'),
    'dungeonbase_floor': ('static_objects/dungeonbase/dungeonbase_floor', 'height', 2.0, 0.018, 'stone'),
    'gigantic_stones': ('static_objects/dungeonbase/floor/gigantic_stones', 'height', 13.3, 0.030, 'stone'),
    'cisternbase': ('static_objects/cisternbase/cisternbase', 'normal', 13.4, 0.025, 'stone'),
    'stone_dirty': ('static_objects/cisternbase/floor/stone_dirty', 'normal', 1.33, 0.020, 'stone'),
    'church_floor': ('static_objects/castlebase/church_wall/church_floor', 'alpha', 5.0, 0.014, 'stone'),
    'cellarbase_details': ('static_objects/cellarbase/cellarbase_details', 'normal', 2.5, 0.015, 'stone'),
    'dungeon_cave_in': ('static_objects/debris/dungeon_cave_in/dungeon_cave_in', 'normal', 3.8, 0.040, 'rock'),
    'rock_tileable': ('static_objects/sanctumbase/rock_tileable', 'normal', 4.0, 0.035, 'rock'),
    'dirt_mould': ('static_objects/dungeonbase/floor/dirt_mould', 'normal', 4.0, 0.020, 'rock'),
}
# Dark stone dims its specular (dungeonbase: linear albedo luminance 0.039, coverage 0.3 of F0 0.04); stone as light as the
# approved castle wall (0.064) keeps the full F0 0.04.
DARK_LUM, DARK_COVERAGE, LIGHT_LUM = 0.039, 0.3, 0.064
# Second pass 2026-10-27 (user: "deeper joints, crisper stone edges"): the table depths x1.5 (classic POM shows 5-15 cm),
# a narrower face -> joint transition, less smoothing, a steeper shading limit.
DEPTH_GAIN = 1.5


def hpl_texture(a, rel, suffix, mode):
    """An original texture of the material (<name><suffix>.tga|.dds|.png) as float32, or None."""
    for ext in ('.tga', '.dds', '.png', '.jpg'):
        path = os.path.join(a.source, rel + suffix + ext)
        if os.path.exists(path):
            return np.asarray(Image.open(path).convert(mode), dtype=np.float32) / 255.0
    return None


def seam_fade(depth, axis):
    """Tiling HPL depth maps fade to 0 over the last few texels of each edge (cobbled_stone: 5 of 512): the tile seam
    would become a flat ridge across the joints. The faded rows of both edges are bridged by a linear blend between the
    first interior rows across the seam. Returns the bridged width (texels)."""
    d = np.moveaxis(depth, axis, 0)
    n = d.shape[0]
    interior = d[n // 10: n - n // 10].mean()
    means = d.mean(axis=1)
    limit = max(2, n * 4 // 100)
    top = next((i for i in range(limit) if means[i] >= 0.9 * interior), limit)
    bottom = next((i for i in range(limit) if means[n - 1 - i] >= 0.9 * interior), limit)
    # A fade on one edge only, or one that does not end within the limit, is a plain part of an atlas, not a tile seam.
    if top == 0 or bottom == 0 or top >= limit or bottom >= limit:
        return 0
    first, last = d[top].copy(), d[n - 1 - bottom].copy()
    gap = top + bottom
    for k in range(gap):
        row = (n - bottom + k) % n
        d[row] = last + (first - last) * (k + 1) / (gap + 1)
    return gap


def generic(a, rng):
    rel, source_kind, metres_per_uv, joint_depth, kind = GENERIC[a.material][:5]
    original = GENERIC[a.material][5] if len(GENERIC[a.material]) > 5 else rel
    joint_depth = a.joint_depth_override or joint_depth * DEPTH_GAIN
    name = os.path.basename(rel)
    albedo = load(os.path.join(a.pbrify, name + '__albedo.png'), 'RGB').astype(np.float32)
    rows, cols = albedo.shape[:2]
    mpp = metres_per_uv / cols
    zoom = lambda img: np.clip(ndimage.zoom(img, (rows / img.shape[0], cols / img.shape[1]), order=3, mode='grid-wrap'), 0, 1).astype(np.float32)

    # 1. Original depth, white = deeper (HPL relief convention).
    if source_kind == 'height':
        depth = hpl_texture(a, original, '_height', 'L')
    elif source_kind == 'alpha':
        depth = hpl_texture(a, original, '_nrm', 'RGBA')[..., 3]
    else:
        depth = None
    # Only along an axis where the texture tiles (the albedo continues across the opposite edges); atlas borders are
    # plain parts, not seams.
    shape = depth.shape if depth is not None else hpl_texture(a, original, '_nrm', 'L').shape
    small = ndimage.zoom(luminance(albedo), (shape[0] / rows, shape[1] / cols), order=1)
    def tiles(axis):
        img = np.moveaxis(small, axis, 0)
        across = np.abs(img[0] - img[-1]).mean()
        inside = np.median([np.abs(img[k] - img[k + 1]).mean() for k in range(1, img.shape[0] - 1, max(1, img.shape[0] // 64))])
        return across < 2.5 * inside
    tiling = tiles(0) and tiles(1)
    if depth is None:
        nrm = hpl_texture(a, original, '_nrm', 'RGB') * 2 - 1
        nz = np.maximum(nrm[..., 2], 0.2)
        p = -nrm[..., 0] / nz
        best = None
        for sign in (1, -1):    # G convention verified numerically, as for the castle wall
            q = sign * -nrm[..., 1] / nz
            z = integrate_periodic(p, q) if tiling else integrate(p, q)
            err = residual(z, p, q)
            if best is None or err < best[0]:
                best = (err, z)
        z = best[1]
        # Broad drift of the integration goes (sigma 6 cm; 12 cm left 4 cm deep troughs along atlas borders and tilted
        # whole slabs of stone_dirty), reflected at atlas borders, wrapped only for a tiling texture.
        z = z - ndimage.gaussian_filter(z, 0.06 / (metres_per_uv / nrm.shape[1]), mode='wrap' if tiling else 'reflect')
        depth = -z
        if not tiling:
            # The rest of the drift at atlas borders (a dark line along the edge = a groove on modules there): faded to
            # the face level (30th percentile of the depth) within 1.5 % of each edge.
            yy, xx = np.indices(depth.shape)
            edge = np.minimum.reduce([yy, xx, depth.shape[0] - 1 - yy, depth.shape[1] - 1 - xx]) / (0.015 * max(depth.shape))
            face_level = np.percentile(depth, 30)
            depth = face_level + (depth - face_level) * np.clip(edge, 0, 1)
    depth = depth.astype(np.float32)
    # The deep part is darker in the albedo (all 16 HPL depth maps: correlation -0.26...-0.73). The integration's sign
    # is not fixed by the residual (stone_dirty came out inverted): orient it by the albedo.
    flipped = False
    if source_kind == 'normal':
        dz = (depth - depth.mean()) / (depth.std() + 1e-6)
        lz = (small - small.mean()) / (small.std() + 1e-6)
        if float(np.mean(dz * lz)) > 0:
            depth = -depth
            flipped = True
    seams = [seam_fade(depth, axis) if source_kind != 'normal' and tiles(axis) else 0 for axis in (0, 1)]
    depth = zoom(depth)
    lo_, hi_ = np.percentile(depth, (1, 99))
    d = np.clip((depth - lo_) / max(hi_ - lo_, 1e-4), 0, 1)
    d = ndimage.gaussian_filter(d, max(0.001 / mpp, 0.5), mode='wrap')

    # 2. Height. Stone: faces (shallow part of the depth) at the plane with a little of their own relief, the deep part
    # recessed to the joint depth over a soft shoulder. Rock: the depth as it is.
    pbr_height = load(os.path.join(a.pbrify, name + '__height.png'), 'RGB')[..., 0].astype(np.float32)
    hewn = ndimage.gaussian_filter(pbr_height, 0.002 / mpp, mode='wrap') - ndimage.gaussian_filter(pbr_height, 0.012 / mpp, mode='wrap')
    hewn = np.clip(hewn / (hewn.std() * 2.5 + 1e-6), -1, 1) * 0.0015
    if kind == 'stone':
        # Integrated normals give rounded stones: a steeper, later step keeps a flat face and a crisp edge.
        t0, t1 = (0.45, 0.62) if source_kind == 'normal' else (0.35, 0.60)
        x = np.clip((d - t0) / (t1 - t0), 0, 1)
        s = x * x * (3 - 2 * x)                                  # 0 face, 1 joint
        # Small isolated deep spots of the depth map are pits in a face, not joints ("drops" of dungeonbase): they stay
        # a few millimetres deep. Joints are long connected networks.
        deep = s > 0.3
        labels, count = ndimage.label(deep)
        if count:
            objects = ndimage.find_objects(labels)
            extent = np.array([0] + [max(sl[0].stop - sl[0].start, sl[1].stop - sl[1].start) for sl in objects]) * mpp
            pit = extent < 0.2
            pit[0] = False
            damp = np.where(pit[labels], 0.12, 1.0).astype(np.float32)
            damp = np.minimum(damp, ndimage.minimum_filter(damp, 3))
            s = s * ndimage.gaussian_filter(damp, 1.0, mode='wrap')
        face = -0.005 * np.clip(d / t0, 0, 1) + hewn             # faces: shallow pits of the depth map, chiselled grain
        height = face * (1 - s) - joint_depth * s
    else:
        s = d
        height = -joint_depth * d ** 0.8 + hewn
    height = ndimage.gaussian_filter(height, max(0.0007 / mpp, 0.5), mode='wrap')
    height -= np.percentile(height, 99.5)                        # the highest faces at the original plane: decals stay visible
    lo, hi = -(joint_depth + 0.008), 0.003
    height = np.clip(height, lo, hi).astype(np.float32)

    # 3. Normal: slopes of the height, softly limited (~51 deg) against bright rims along joints, plus PBRify fine detail.
    gx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) / (2 * mpp)
    gy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) / (2 * mpp)
    limit = 1 / np.sqrt(1 + (np.hypot(gx, gy) / 1.25) ** 2)
    gx, gy = gx * limit, gy * limit
    sx, sy = pbrify_slopes(a, name)
    radius = 0.006 / mpp
    keep = a.detail * (1 - 0.4 * s)
    normal = normal_from(-gx + fine(sx, radius) * keep, gy + fine(sy, radius) * keep).astype(np.float32)

    # 4. Matte: faces 0.77-0.92 not tied to albedo brightness, darker spots rougher, joints 0.95; normal variance into
    # roughness (Toksvig-like, against sparkling lines under a close light).
    lum = luminance(albedo)
    pbr_rough = load(os.path.join(a.pbrify, name + '__roughness.png'), 'RGB')[..., 0].astype(np.float32)
    resid, dark = matte(pbr_rough, lum, 0.02 / mpp)
    face_rough = np.clip(0.84 + resid * 0.025 + dark * 0.06, 0.77, 0.92)
    face_weight = (1 - s) ** 3
    rough = face_rough * face_weight + 0.95 * (1 - face_weight)
    mean = np.dstack([ndimage.gaussian_filter(normal[..., i], 1.5, mode='wrap') for i in range(3)])
    spread = 1 - np.linalg.norm(mean, axis=2)
    rough = np.minimum(np.sqrt(rough ** 2 + 3 * spread), 1)
    occlusion = 1 - 0.45 * np.clip(-height / joint_depth, 0, 1) ** 1.5
    maskmap = np.dstack([np.zeros_like(rough), occlusion, np.ones_like(rough), 1 - rough])

    # 5. Specular coverage (F0 = 0.04 x value): dark stone dims its sheen, as dungeonbase.
    linear = float(np.mean(albedo ** 2.2 @ np.array([0.2126, 0.7152, 0.0722], np.float32)))
    face_coverage = float(np.clip(DARK_COVERAGE + (linear - DARK_LUM) / (LIGHT_LUM - DARK_LUM) * (1 - DARK_COVERAGE), 0.25, 1))
    reflectance = (face_coverage - 0.08 * dark) * face_weight + face_coverage * 0.5 * (1 - face_weight)

    save(a.out, 'stone_mask.png', (s < 0.5).astype(np.float64), 'L')
    save(a.out, 'height.png', (height - lo) / (hi - lo), 'L')
    save(a.out, 'normal.png', normal * 0.5 + 0.5, 'RGB')
    save(a.out, 'mask.png', maskmap, 'RGBA')
    save(a.out, 'specular.png', np.dstack([reflectance] * 3), 'RGB')     # RGB: a one-channel PNG imports as R8 / Alpha8
    previews(a.out, albedo, s < 0.5, height, normal, step=max(1, cols // 1024))
    return {'height_encoding_metres': [lo, hi], 'source': source_kind, 'metres_per_uv': metres_per_uv, 'joint_depth': joint_depth,
            'relief_kind': kind, 'size': [cols, rows], 'seam_fade_bridged_texels': seams, 'tiling': bool(tiling),
            'integrated_depth_flipped': flipped, 'face_share': float((s < 0.5).mean()), 'linear_albedo': linear,
            'face_specular_coverage': face_coverage,
            'face_roughness': [float(np.percentile(rough[s < 0.5], q)) for q in (5, 50, 95)],
            # Atlases: relief fades at UV seams and creases (generic shapes are not checked across them).
            'over_creases': False, 'mesh_tolerance_metres': 0.0025}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('material', choices=sorted(MATERIALS) + sorted(GENERIC))
    ap.add_argument('--joint-depth-override', type=float, help='generic: joint depth, m (default per material)')
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
    is_generic = a.material in GENERIC
    defaults = {'wall': (0.020, 0.016, 0.5), 'floor': (0.014, 0.018, 0.5), 'boards': (0.018, 0.0, 0.6),
                'dungeon': (0.020, 0.016, 0.5)}.get(a.material, (0.0, 0.0, 0.5))
    if a.threshold is None and a.material != 'dungeon': a.threshold = 0.2     # dungeon: per island
    a.joint_depth = defaults[0] if a.joint_depth is None else a.joint_depth
    a.bevel = defaults[1] if a.bevel is None else a.bevel
    a.detail = defaults[2] if a.detail is None else a.detail
    rel = GENERIC[a.material][0] if is_generic else MATERIALS[a.material]
    folder = os.path.join(ROOT, 'sourceRTXRemix', rel)
    if not a.pbrify:
        # dungeon and the generic materials: the x2 set where there is one (4096 atlases), else x4.
        sets = [s for s in ('pbrify-x2-batch-v1', 'pbrify-x4-batch-v1') if os.path.isdir(os.path.join(folder, s))]
        a.pbrify = os.path.join(folder, sets[0] if (is_generic or a.material == 'dungeon') and sets else 'pbrify-x4-batch-v1')
    a.out = a.out or os.path.join(folder, 'manual-relief-v1')
    os.makedirs(a.out, exist_ok=True)
    info = {'material': rel + '.mat', 'kind': 'generic' if is_generic else a.material,
            'height_encoding_metres': HEIGHT_RANGE, 'joint_depth': a.joint_depth, 'bevel': a.bevel,
            'detail': a.detail, 'seed': a.seed, 'mask': 'edited: ' + a.mask if a.mask else 'automatic',
            'pbrify': os.path.basename(a.pbrify)}
    info.update((generic if is_generic else globals()[a.material])(a, np.random.default_rng(a.seed)))
    with open(os.path.join(a.out, 'relief.json'), 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, ensure_ascii=False)
    print(json.dumps(info, ensure_ascii=False))


if __name__ == '__main__':
    main()

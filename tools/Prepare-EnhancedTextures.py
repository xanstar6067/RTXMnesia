"""Validate finished PBRify results and GPU-compress native DDS for Unity (no source changes)."""
import argparse
import hashlib
import json
import re
import struct
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageChops, ImageOps

VERSION = 'dds-pbrify-v3'
# x4 sets, and x2 sets of 2K originals (Remix-Textures.py --scale 2).
RESULT_FOLDERS = ('pbrify-x4-batch-v1', 'pbrify-alpha-x4-batch-v1', 'pbrify-x2-batch-v1', 'pbrify-alpha-x2-batch-v1')
DEFAULT_NVTT = Path('C:/Program Files/NVIDIA Corporation/RTX Remix/exts/omni.flux.resources/deps/tools/nvtt/nvtt_export.exe')


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def inside(root, relative):
    path = (root / relative).resolve()
    if not path.is_relative_to(root.resolve()):
        raise ValueError(f'Path outside resource root: {relative}')
    return path


def dds_info(path):
    with path.open('rb') as stream:
        header = stream.read(148)
    if header[:4] != b'DDS ' or header[84:88] != b'DX10':
        raise ValueError(f'Expected DX10 DDS: {path}')
    height, width = struct.unpack_from('<II', header, 12)
    mips = struct.unpack_from('<I', header, 28)[0]
    fmt = struct.unpack_from('<I', header, 128)[0]
    blocks = 8 if fmt == 80 else 16
    expected = 148
    w, h = width, height
    for _ in range(mips):
        expected += max(1, (w + 3)//4) * max(1, (h + 3)//4) * blocks
        w, h = max(1, w//2), max(1, h//2)
    if path.stat().st_size != expected:
        raise ValueError(f'Incomplete DDS: {path}')
    return {'width': width, 'height': height, 'mips': mips, 'format': fmt, 'bytes': expected}


def flip_bc45(path):
    """Lossless vertical flip, including index rows inside BC4/5 blocks and partial final mips."""
    import numpy as np
    info = dds_info(path)
    if info['format'] not in (80, 83):
        raise ValueError(f'Expected BC4/5 for native orientation upgrade: {path}')
    channels = 2 if info['format'] == 83 else 1
    content = bytearray(path.read_bytes())
    offset, width, height = 148, info['width'], info['height']
    for _ in range(info['mips']):
        bw, bh = max(1, (width+3)//4), max(1, (height+3)//4)
        size = bw * bh * channels * 8
        values = np.frombuffer(content, dtype='<u8', count=bw*bh*channels, offset=offset).reshape(bh,bw,channels)[::-1].copy()
        indices = values >> np.uint64(16)
        flipped = indices.copy()
        rows = min(height, 4)
        for row in range(rows):
            mask = np.uint64(4095) << np.uint64(12*row)
            flipped = (flipped & ~mask) | (((indices >> np.uint64(12*(rows-1-row))) & np.uint64(4095)) << np.uint64(12*row))
        values = (values & np.uint64(65535)) | (flipped << np.uint64(16))
        content[offset:offset+size] = values.tobytes()
        offset += size
        width, height = max(1,width//2), max(1,height//2)
    temporary = path.with_suffix('.dds.tmp')
    temporary.write_bytes(content)
    temporary.replace(path)


def upgrade_native_orientation(root):
    output_root = (root/'UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Enhanced').resolve()
    changed = 0
    for stamp in output_root.rglob('*__prepared.json'):
        record = json.loads(stamp.read_text())
        result = Path(record['result'])
        if record['signature'] == sha(result)+'|'+VERSION:
            continue
        if record['signature'] != sha(result)+'|dds-pbrify-v2':
            raise ValueError(f'Cannot migrate stale DDS: {stamp}')
        for index in (1,3):
            if not record['files'][index]:
                continue
            path = Path(record['files'][index]).resolve()
            if not path.is_relative_to(output_root):
                raise ValueError(f'DDS outside generated root: {path}')
            flip_bc45(path)
            changed += 1
        record['signature'] = sha(result)+'|'+VERSION
        stamp.write_text(json.dumps(record,indent=2),encoding='utf-8')
        if changed % 50 == 0:
            print(f'Losslessly corrected {changed} BC4/5 files',flush=True)
    print(f'Native DDS orientation upgraded: {changed} files',flush=True)


def clean_packing(unity):
    """Remove only this tool's disposable packed mask/normal PNGs, after validated DDS success."""
    packing = (unity/'Logs/EnhancedFull/Packing').resolve()
    if not packing.is_relative_to(unity.resolve()):
        raise ValueError('Scratch directory outside Unity project')
    removed = 0
    if packing.exists():
        for file in packing.iterdir():
            if file.is_file() and re.fullmatch(r'[a-f0-9]{64}(?:-normal)?\.png',file.name):
                removed += file.stat().st_size
                file.unlink()
    return removed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--nvtt', type=Path, default=DEFAULT_NVTT)
    parser.add_argument('--limit', type=int)
    parser.add_argument('--upgrade-native-orientation', action='store_true', help='One-time lossless upgrade of validated v2 DDS (NumPy, Unity closed).')
    args = parser.parse_args()
    root = args.project.resolve()
    if args.upgrade_native_orientation:
        upgrade_native_orientation(root)
        return
    source = root/'sourceRTXRemix'
    unity = root/'UnityProj/UMnesiaRTX'
    imported = unity/'Assets/AmnesiaImported'
    logs = unity/'Logs/EnhancedFull'
    logs.mkdir(parents=True, exist_ok=True)
    temporary = logs/'Packing'
    temporary.mkdir(exist_ok=True)
    results = sorted(p for p in source.rglob('result.json') if p.parent.name in RESULT_FOLDERS)
    if args.limit:
        results = results[:args.limit]
    cutout_inputs = {d['texture'] for p in results if (d := json.loads(p.read_text(encoding='utf-8-sig'))).get('alpha_mode') == 'source-nearest'}
    commands, prepared = [], []
    for index, file in enumerate(results):
        data = json.loads(file.read_text(encoding='utf-8-sig'))
        if data['version'] != 1 or not data['normal_convention'].startswith('PBRify_NormalV3'):
            raise ValueError(f'Unsupported result: {file}')
        cutout = data.get('alpha_mode') == 'source-nearest'
        input_path = inside(imported/'Textures', data['texture'])
        if sha(input_path) != data['input_sha256']:
            raise ValueError(f'Classic input changed: {file}')
        sizes = set()
        for role, entry in data['files'].items():
            image_path = inside(file.parent, entry['file'])
            if sha(image_path) != entry['sha256']:
                raise ValueError(f'Bad source SHA256: {image_path}')
            with Image.open(image_path) as image:
                if list(image.size) != entry['size']:
                    raise ValueError(f'Bad dimensions: {image_path}')
            sizes.add(tuple(entry['size']))
        if len(sizes) != 1:
            raise ValueError(f'PBR dimensions differ: {file}')
        size = next(iter(sizes))
        if max(size) > 4096:
            raise ValueError(f'Resolution above 4K cap: {file}')
        use_height = False
        for material in data['materials']:
            xml = ET.parse(inside(root/'source', material)).getroot()
            main, units = xml.find('Main'), xml.find('TextureUnits')
            if main.get('Type', '').lower() != 'soliddiffuse':
                raise ValueError(f'Special material outside supported batch: {material}')
            # Physics "Glass" marks opaque window walls too (castlebase_wall_win: stone around the panes); see-through glass is not SolidDiffuse.
            use_height |= units.find('Height') is not None and not cutout
        if cutout:
            if any(v['alpha_cutoff'] != 0.5 for v in data['material_settings'].values()):
                raise ValueError(f'Unsupported alpha coverage threshold: {file}')
            with Image.open(input_path) as original, Image.open(file.parent/data['files']['albedo']['file']) as output:
                expected = original.getchannel('A').resize(size, Image.Resampling.NEAREST)
                if ImageChops.difference(expected, output.getchannel('A')).getbbox():
                    raise ValueError(f'Generated cutout alpha differs from source: {file}')
        texture_stem = data['texture'].removesuffix('.png').replace('__albedo', '')
        if not cutout and data['texture'] in cutout_inputs:
            texture_stem += '__opaque'
        output_stem = imported/'Enhanced'/texture_stem
        output_stem.parent.mkdir(parents=True, exist_ok=True)
        signature = sha(file) + '|' + VERSION
        stamp = Path(str(output_stem)+'__prepared.json')
        old = json.loads(stamp.read_text()) if stamp.exists() else {}
        roles = ['albedo', 'normal', 'mask'] + (['height'] if use_height else [])
        formats = {'albedo': 99, 'normal': 83, 'mask': 98, 'height': 80}
        cached = old.get('signature') == signature
        if cached:
            try:
                cached = all(dds_info(Path(str(output_stem)+'__'+role+'.dds'))['format'] == formats[role] for role in roles)
            except (OSError, ValueError):
                cached = False
        record = {'signature': signature, 'result': str(file), 'texture': data['texture'], 'materials': data['materials'],
                  'sourceSignature': '|'.join(data['files'][r]['sha256'] for r in ['albedo', 'normal_ogl', 'roughness', 'height'] + (['alpha'] if cutout else [])),
                  'width': size[0], 'height': size[1], 'cutout': cutout, 'useHeight': use_height,
                  'files': [str(Path(str(output_stem)+'__'+r+'.dds')) if r in roles else '' for r in ['albedo', 'normal', 'mask', 'height']]}
        prepared.append((stamp, record, formats, roles))
        if not cached:
            mask_path = temporary/(hashlib.sha256(data['texture'].encode()).hexdigest()+'.png')
            with Image.open(file.parent/data['files']['roughness']['file']) as rough:
                smoothness = ImageOps.invert(rough.getchannel('R'))
                Image.merge('RGBA', (Image.new('L', size, 0), Image.new('L', size, 255), Image.new('L', size, 255), smoothness)).save(mask_path, compress_level=1)
            normal_path = temporary/(hashlib.sha256(data['texture'].encode()).hexdigest()+'-normal.png')
            with Image.open(file.parent/data['files']['normal_ogl']['file']) as normal:
                red, green, blue = normal.convert('RGB').split()
                Image.merge('RGB', (red, ImageOps.invert(green), blue)).save(normal_path, compress_level=1)
            for role in roles:
                input_file = mask_path if role == 'mask' else normal_path if role == 'normal' else file.parent/data['files'][role]['file']
                output = Path(str(output_stem)+'__'+role+'.dds')
                command = [str(input_file), '--format', {'normal': 'bc5', 'height': 'bc4'}.get(role, 'bc7'), '--quality', 'normal',
                           '--dx10', '--mip-filter', 'kaiser', '--export-transfer-function', 'srgb' if role == 'albedo' else 'linear', '-o', str(output)]
                # Unity flips BC4/5 itself. Its BC7 importer cannot do so: export only BC7 flipped.
                if role in ('albedo', 'mask'):
                    command += ['--save-flip-y']
                if role != 'albedo':
                    command += ['--no-mip-gamma-correct', '--no-mip-pre-alpha']
                if role == 'albedo':
                    if cutout:
                        command += ['--scale-alpha', '--alpha-threshold', '127']
                commands.append(subprocess.list2cmdline(command))
        if (index + 1) % 25 == 0:
            print(f'Validated {index+1}/{len(results)} sets; {len(commands)} DDS jobs', flush=True)
    snapshot = logs/'results.txt'
    snapshot.write_text('\n'.join(str(p) for p in results)+'\n', encoding='utf-8')
    batch = logs/'nvtt-batch.txt'
    batch.write_text('\n'.join(commands)+'\n', encoding='utf-8')
    print(f'GPU compression: {len(commands)} textures in {len(results)} sets', flush=True)
    if commands:
        with (logs/'nvtt.log').open('w') as stdout, (logs/'nvtt-errors.log').open('w') as stderr:
            result = subprocess.run([str(args.nvtt), '--batch', str(batch)], stdout=stdout, stderr=stderr)
        if result.returncode:
            raise RuntimeError(f'NVTT failed ({result.returncode}); see {logs}/nvtt-errors.log')
    total = 0
    for stamp, record, formats, roles in prepared:
        for role in roles:
            info = dds_info(Path(record['files'][['albedo', 'normal', 'mask', 'height'].index(role)]))
            if info['format'] != formats[role] or (info['width'], info['height']) != (record['width'], record['height']):
                raise ValueError(f'DDS format mismatch: {stamp} {role}')
            total += info['bytes']
        stamp.write_text(json.dumps(record, indent=2), encoding='utf-8')
    removed = clean_packing(unity)
    print(json.dumps({'sets': len(results), 'material_references': sum(len(p[1]['materials']) for p in prepared),
                      'dds_bytes': total, 'dds_gib': total/2**30, 'temporary_bytes_removed': removed}), flush=True)


if __name__ == '__main__':
    main()

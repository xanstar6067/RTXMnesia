"""Batch PBRify via an already running local ComfyUI; never edits source/ or Unity assets.

Requires Pillow (use ComfyUI's Python). Plan is offline; run uses only the local API.
Each completed texture has four losslessly compressed PNGs and a provenance record.
"""
import argparse
from collections import Counter
from contextlib import contextmanager
from datetime import datetime, timezone
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import time
import urllib.parse
import urllib.request
import uuid
import xml.etree.ElementTree as ET

from PIL import Image

VERSION = 1
ROLES = {'178': 'height', '179': 'normal_ogl', '180': 'roughness', '181': 'albedo'}
SAMPLE = 'static_objects/castlebase/wall/castlebase_wall/pbrify-x4-20261019-181639'


def now():
    return datetime.now(timezone.utc).isoformat()


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    os.replace(temporary, path)


def sha256(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def texture_reference(material):
    match = re.search(r'- _BaseColorMap:\s*\n\s*m_Texture: \{[^\n]*guid: ([0-9a-f]{32})', material)
    return match[1] if match else None


def scalar(material, name):
    match = re.search(rf'^\s*- {re.escape(name)}: ([^\r\n]+)', material, re.M)
    return float(match[1]) if match else None


def plan(args):
    root = args.project
    kind = getattr(args, 'kind', 'opaque')
    imported = root / 'UnityProj/UMnesiaRTX/Assets/AmnesiaImported'
    textures = imported / 'Textures'
    materials = imported / 'Materials'
    sample = (materials / 'static_objects/castlebase/wall/castlebase_wall.mat').read_text()
    lit_shader = re.search(r'm_Shader: \{[^\n]*guid: ([0-9a-f]{32})', sample)[1]
    guid_paths = {}
    for path in textures.rglob('*.png.meta'):
        if '__albedo' not in path.name:
            continue
        match = re.search(r'^guid: ([0-9a-f]{32})', path.read_text(), re.M)
        if match:
            guid_paths[match[1]] = Path(str(path)[:-5])
    grouped = {}
    excluded = []
    for path in sorted(materials.rglob('*.mat')):
        rel = path.relative_to(materials).as_posix()
        text = path.read_text(encoding='utf-8-sig')
        original = root / 'source' / rel
        reason = None
        if not original.is_file():
            reason = 'no_source_material'
        else:
            try:
                main = ET.parse(original).getroot().find('Main')
                if main is None or main.get('Type', '').lower() != 'soliddiffuse':
                    reason = 'special_material'
            except ET.ParseError:
                reason = 'invalid_source_material'
        shader = re.search(r'm_Shader: \{[^\n]*guid: ([0-9a-f]{32})', text)
        if reason is None and (not shader or shader[1] != lit_shader):
            reason = 'not_hdrp_lit'
        if reason is None and scalar(text, '_SurfaceType') != 0:
            reason = 'transparent'
        if reason is None:
            has_cutout = scalar(text, '_AlphaCutoffEnable') == 1
            if kind == 'opaque' and has_cutout:
                reason = 'alpha_test'
            elif kind == 'alpha-test' and not has_cutout:
                reason = 'not_alpha_test'
        texture = guid_paths.get(texture_reference(text))
        if reason is None and texture is None:
            reason = 'no_albedo_reference'
        if reason:
            excluded.append({'material': rel, 'reason': reason})
            continue
        texture_rel = texture.relative_to(textures).as_posix()
        if args.folder and not texture_rel.startswith(args.folder.strip('/') + '/'):
            continue
        if texture_rel not in grouped:
            with Image.open(texture) as image:
                size = list(image.size)
                if kind == 'alpha-test' and ('A' not in image.getbands() or image.getchannel('A').getextrema() == (255, 255)):
                    excluded.append({'material': rel, 'reason': 'no_transparency_in_albedo'})
                    continue
            if max(size) <= 16:
                excluded.append({'material': rel, 'reason': 'tiny_technical_texture'})
                continue
            # Large native originals should not be downsampled and regenerated automatically.
            if max(size) >= args.max_size:
                excluded.append({'material': rel, 'reason': 'already_at_resolution_cap'})
                continue
            if max(size) > args.max_size // 4:
                excluded.append({'material': rel, 'reason': 'needs_separate_2x_workflow'})
                continue
            stem = texture_rel.removesuffix('.png').replace('__albedo', '')
            grouped[texture_rel] = {
                'id': hashlib.sha256((texture_rel + (':alpha-test' if kind == 'alpha-test' else '')).encode()).hexdigest()[:16],
                'input': str(texture), 'texture': texture_rel, 'size': size,
                'sha256': sha256(texture), 'materials': [], 'state': 'pending',
                'destination': str(args.output / stem / ('pbrify-alpha-x4-batch-v1' if kind == 'alpha-test' else 'pbrify-x4-batch-v1')),
            }
            if kind == 'alpha-test':
                grouped[texture_rel].update(alpha_mode='source-nearest', material_settings={})
        grouped[texture_rel]['materials'].append(rel)
        if kind == 'alpha-test':
            grouped[texture_rel]['material_settings'][rel] = {
                'alpha_cutoff': scalar(text, '_AlphaCutoff'),
                'double_sided': scalar(text, '_DoubleSidedEnable'),
                'surface_type': 0, 'alpha_test': True,
            }
    entries = sorted(grouped.values(), key=lambda e: (
        0 if e['texture'].startswith('static_objects/castlebase/') else
        1 if e['texture'].startswith('static_objects/') else 2, e['texture']))
    value = {'version': VERSION, 'created': now(), 'project': str(root), 'kind': kind,
             'output': str(args.output), 'max_size': args.max_size,
             'entries': entries, 'excluded': excluded}
    if args.manifest.exists():
        raise RuntimeError('Manifest already exists. Use run to resume, or choose a new --manifest.')
    write_json(args.manifest, value)
    print(json.dumps({'textures': len(entries), 'material_references': sum(len(e['materials']) for e in entries),
                      'sizes': dict(Counter(f"{e['size'][0]}x{e['size'][1]}" for e in entries)),
                      'excluded': dict(Counter(e['reason'] for e in excluded)),
                      'manifest': str(args.manifest)}, ensure_ascii=False, indent=2), flush=True)


def api(url, route, payload=None):
    data = None if payload is None else json.dumps(payload).encode()
    request = urllib.request.Request(url + route, data=data,
                                     headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def upload(url, path, name):
    boundary = uuid.uuid4().hex
    data = (f'--{boundary}\r\nContent-Disposition: form-data; name="image"; filename="{name}"\r\n'
            'Content-Type: image/png\r\n\r\n').encode() + Path(path).read_bytes()
    data += f'\r\n--{boundary}--\r\n'.encode()
    request = urllib.request.Request(url + '/upload/image', data=data,
        headers={'Content-Type': f'multipart/form-data; boundary={boundary}'})
    with urllib.request.urlopen(request, timeout=60) as response:
        result = json.load(response)
    return '/'.join(p for p in (result.get('subfolder'), result['name']) if p)


@contextmanager
def batch_lock(path):
    import msvcrt
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a+b') as stream:
        stream.seek(0)
        if not stream.read(1):
            stream.write(b'0')
            stream.flush()
        stream.seek(0)
        msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        try:
            yield
        finally:
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)


def build_prompt(reference, entry, uploaded):
    prompt = json.loads(json.dumps(reference))
    # Opaque materials: RGB only. The template's extra DAT pass on the alpha mask
    # invents transparency and is not needed for these materials.
    for node in ('28', '33', '34', '27', '35', '37', '38'):
        prompt.pop(node, None)
    prompt['177']['inputs']['image'] = uploaded
    prompt['181']['inputs']['textures'] = ['31', 0]
    return prompt


def status(args):
    data = read_json(args.manifest)
    print(json.dumps({'total': len(data['entries']),
                      'states': dict(Counter(e['state'] for e in data['entries'])),
                      'active': [{k: e.get(k) for k in ('texture', 'prompt_id', 'started', 'error')}
                                 for e in data['entries'] if e['state'] in ('running', 'failed')],
                      'updated': data.get('updated'), 'worker': data.get('worker')},
                     ensure_ascii=False, indent=2), flush=True)


def collect(url, result, entry, workflow_hash):
    destination = Path(entry['destination'])
    destination.mkdir(parents=True, exist_ok=True)
    files = {}
    expected = tuple(v * 4 for v in entry['size'])
    alpha = None
    if entry.get('alpha_mode') == 'source-nearest':
        with Image.open(entry['input']) as source:
            if 'A' not in source.getbands():
                raise RuntimeError('Cutout input lost its alpha channel')
            alpha = upscale_alpha(source.getchannel('A'), expected)
    for node, role in ROLES.items():
        outputs = [i for i in result['outputs'][node]['images'] if i['type'] == 'output']
        if len(outputs) != 1:
            raise RuntimeError(f'Expected one {role} image')
        query = urllib.parse.urlencode({k: outputs[0][k] for k in ('filename', 'subfolder', 'type')})
        with urllib.request.urlopen(url + '/view?' + query, timeout=120) as response:
            pixels = response.read()
        target = destination / (Path(entry['texture']).stem.replace('__albedo', '') + '__' + role + '.png')
        with Image.open(io.BytesIO(pixels)) as image:
            image.load()
            if image.size != expected:
                raise RuntimeError(f'Unexpected {role} size: {image.size}, expected {expected}')
            if image.mode != 'RGB':
                raise RuntimeError(f'Unexpected {role} mode: {image.mode}')
            if role == 'albedo' and alpha is not None:
                image = image.copy()
                image.putalpha(alpha)
            temporary = target.with_suffix('.tmp.png')
            image.save(temporary, compress_level=6)
            with Image.open(temporary) as saved:
                saved.load()
                if saved.size != image.size or saved.tobytes() != image.tobytes():
                    raise RuntimeError('Lossless PNG verification failed')
            os.replace(temporary, target)
        files[role] = {'file': target.name, 'sha256': sha256(target), 'size': list(expected)}
    if alpha is not None:
        target = destination / (Path(entry['texture']).stem.replace('__albedo', '') + '__alpha.png')
        alpha.save(target.with_suffix('.tmp.png'), compress_level=6)
        with Image.open(target.with_suffix('.tmp.png')) as saved:
            if saved.tobytes() != alpha.tobytes():
                raise RuntimeError('Alpha mask verification failed')
        os.replace(target.with_suffix('.tmp.png'), target)
        files['alpha'] = {'file': target.name, 'sha256': sha256(target), 'size': list(expected)}
    record = {'version': VERSION, 'completed': now(), 'texture': entry['texture'],
              'input_sha256': entry['sha256'], 'materials': entry['materials'],
              'workflow_sha256': workflow_hash, 'prompt_id': entry['prompt_id'],
              'normal_convention': 'PBRify_NormalV3 (verify G as in docs/EnhancedTextures.md)',
              'files': files, 'unity_imported': False}
    if alpha is not None:
        record.update(alpha_mode=entry['alpha_mode'], material_settings=entry['material_settings'],
                      alpha_note='Original alpha samples replicated exactly 4x4; cutoff coverage unchanged.')
    write_json(destination / 'result.json', record)
    return files


def upscale_alpha(source, expected):
    """No invented opacity: exact source samples and coverage at every cutoff."""
    if source.mode != 'L' or expected != tuple(v * 4 for v in source.size):
        raise ValueError('Expected a grayscale source mask and exact x4 dimensions')
    return source.resize(expected, Image.Resampling.NEAREST)


def verified(entry, workflow_hash):
    path = Path(entry['destination']) / 'result.json'
    if not path.exists():
        return False
    record = read_json(path)
    if record['input_sha256'] != entry['sha256'] or record['workflow_sha256'] != workflow_hash:
        return False
    return all((path.parent / f['file']).is_file() and sha256(path.parent / f['file']) == f['sha256']
               for f in record['files'].values()) and set(record['files']) == (
                   set(ROLES.values()) | ({'alpha'} if entry.get('alpha_mode') else set()))


def run(args):
    with batch_lock(args.manifest.with_suffix('.lock')):
        data = read_json(args.manifest)
        sample = args.output / SAMPLE
        reference = read_json(sample / 'request.json')['prompt']
        # Hash the effective RGB graph, not sample-specific input path or client id.
        signature = build_prompt(reference, {}, '<input>')
        if data.get('kind') == 'alpha-test':
            signature = {'graph': signature, 'alpha_mode': 'source-nearest', 'version': 1}
        workflow_hash = hashlib.sha256(json.dumps(signature, sort_keys=True).encode()).hexdigest()
        schemas = api(args.url, '/object_info')
        for node in (signature.get('graph', signature)).values():
            if node['class_type'] not in schemas:
                raise RuntimeError('Missing ComfyUI node: ' + node['class_type'])
        data['worker'] = {'pid': os.getpid(), 'started': now(), 'url': args.url, 'state': 'running'}

        def save():
            data['updated'] = now()
            write_json(args.manifest, data)

        processed = 0
        consecutive_failures = 0
        save()
        try:
            for number, entry in enumerate(data['entries'], 1):
                if args.limit and processed >= args.limit:
                    break
                if args.manifest.with_suffix('.stop').exists():
                    print('Stop requested: finishing without submitting another texture.', flush=True)
                    break
                if max(entry['size']) <= 16:
                    entry.update(state='excluded', reason='tiny_technical_texture')
                    save()
                    continue
                if entry['state'] == 'completed' and verified(entry, workflow_hash):
                    continue
                if entry['state'] == 'failed' and not args.retry_failed:
                    continue
                if sha256(entry['input']) != entry['sha256']:
                    raise RuntimeError('Input changed since plan: ' + entry['input'])
                if shutil.disk_usage(args.output).free < 5 * 1024**3:
                    raise RuntimeError('Less than 5 GB free on output disk')
                print(f"[{number}/{len(data['entries'])}] {entry['texture']}", flush=True)
                try:
                    # A restarted worker follows its saved prompt rather than enqueueing a duplicate.
                    if entry['state'] != 'running':
                        if api(args.url, '/queue')['queue_pending']:
                            print('Other pending ComfyUI jobs exist; waiting for the queue.', flush=True)
                            while api(args.url, '/queue')['queue_pending']:
                                time.sleep(5)
                        uploaded = upload(args.url, entry['input'], 'rtxmnesia_' + entry['id'] + '.png')
                        prompt = build_prompt(reference, entry, uploaded)
                        response = api(args.url, '/prompt', {'prompt': prompt, 'client_id': str(uuid.uuid4())})
                        if response.get('node_errors'):
                            raise RuntimeError(json.dumps(response['node_errors']))
                        entry.update(state='running', prompt_id=response['prompt_id'], started=now())
                        save()
                    deadline = time.monotonic() + 7200
                    missing_since = None
                    while True:
                        history = api(args.url, '/history/' + entry['prompt_id'])
                        result = history.get(entry['prompt_id'])
                        if result:
                            if result['status']['status_str'] != 'success':
                                raise RuntimeError(json.dumps(result['status']))
                            break
                        queue = api(args.url, '/queue')
                        known = {item[1] for key in ('queue_running', 'queue_pending') for item in queue[key]}
                        if entry['prompt_id'] not in known:
                            # Completion can occur between the history and queue requests.
                            # Allow history to catch up before declaring the prompt lost.
                            if missing_since is None:
                                missing_since = time.monotonic()
                            elif time.monotonic() - missing_since >= 30:
                                raise RuntimeError('Saved prompt is absent from ComfyUI queue/history; retry explicitly.')
                        else:
                            missing_since = None
                        if time.monotonic() > deadline:
                            raise RuntimeError('Timed out waiting for prompt; prompt_id retained for inspection.')
                        time.sleep(5)
                    files = collect(args.url, result, entry, workflow_hash)
                    entry.update(state='completed', completed=now(), files=files)
                    entry.pop('error', None)
                    processed += 1
                    consecutive_failures = 0
                    print('Completed: ' + entry['destination'], flush=True)
                except (urllib.error.URLError, TimeoutError, ConnectionError) as error:
                    # Keep a running prompt resumable if the connection drops. Do not submit
                    # another image while the outcome of this one is unknown.
                    entry['error'] = str(error)
                    save()
                    raise
                except Exception as error:
                    entry.update(state='failed', error=str(error), failed_at=now())
                    consecutive_failures += 1
                    print('FAILED: ' + str(error), flush=True)
                save()
                if consecutive_failures >= 3:
                    raise RuntimeError('Three consecutive failures; batch stopped for inspection.')
        finally:
            data['worker']['state'] = ('completed' if all(e['state'] in ('completed', 'excluded')
                                                        for e in data['entries']) else 'stopped')
            data['worker']['ended'] = now()
            save()
        status(args)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('plan', 'run', 'status'))
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path)
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--url', default='http://127.0.0.1:8188')
    parser.add_argument('--folder', default='')
    parser.add_argument('--kind', choices=('opaque', 'alpha-test'), default='opaque')
    parser.add_argument('--max-size', type=int, default=4096)
    parser.add_argument('--limit', type=int, default=0)
    parser.add_argument('--retry-failed', action='store_true')
    args = parser.parse_args()
    args.project = args.project.resolve()
    args.output = (args.output or args.project / 'sourceRTXRemix').resolve()
    batch_name = 'pbrify-alpha-v1' if args.kind == 'alpha-test' else 'pbrify-v1'
    args.manifest = (args.manifest or args.output / '_batch' / batch_name / 'manifest.json').resolve()
    if args.url != 'http://127.0.0.1:8188':
        raise ValueError('Only the local ComfyUI endpoint is supported; no external uploads.')
    if not args.output.is_relative_to(args.project) or not args.manifest.is_relative_to(args.output):
        raise ValueError('Output and manifest must stay inside this project.')
    {'plan': plan, 'run': run, 'status': status}[args.action](args)


if __name__ == '__main__':
    main()

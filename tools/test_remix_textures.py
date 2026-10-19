"""Offline checks for batch selection and durable result verification."""
import importlib.util
import io
import json
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import unittest
from unittest.mock import patch

from PIL import Image

TEST_ROOT = Path(__file__).resolve().parents[1] / 'sourceRTXRemix/_batch/tests'
TEST_ROOT.mkdir(parents=True, exist_ok=True)

spec = importlib.util.spec_from_file_location('remix_textures', Path(__file__).with_name('Remix-Textures.py'))
batch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(batch)


class BatchTests(unittest.TestCase):
    def test_plan_uses_guid_references_deduplicates_and_excludes_special_maps(self):
        with TemporaryDirectory(dir=TEST_ROOT) as temp:
            root = Path(temp)
            assert root.resolve().is_relative_to(TEST_ROOT.resolve())
            imported = root / 'UnityProj/UMnesiaRTX/Assets/AmnesiaImported'
            textures = imported / 'Textures'
            materials = imported / 'Materials'
            textures.mkdir(parents=True)
            for name, guid, size in [('stone', '1' * 32, 128), ('large', '2' * 32, 2048), ('tiny', '3' * 32, 8)]:
                image = textures / (name + '__albedo.png')
                Image.new('RGB', (size, size)).save(image)
                Path(str(image) + '.meta').write_text('guid: ' + guid)
            def material(path, guid, alpha=0, surface=0, original_type='SolidDiffuse'):
                output = materials / path
                output.parent.mkdir(parents=True, exist_ok=True)
                output.write_text('m_Shader: {guid: ' + 'a' * 32 + '}\n'
                    '- _BaseColorMap:\n  m_Texture: {guid: ' + guid + '}\n'
                    f'- _SurfaceType: {surface}\n- _AlphaCutoffEnable: {alpha}\n')
                source = root / 'source' / path
                source.parent.mkdir(parents=True, exist_ok=True)
                source.write_text(f'<Material><Main Type="{original_type}"/></Material>')
            material('static_objects/castlebase/wall/castlebase_wall.mat', '1' * 32)
            material('alias.mat', '1' * 32)
            material('leaves.mat', '1' * 32, alpha=1)
            material('glass.mat', '1' * 32, surface=1)
            material('water.mat', '1' * 32, original_type='Water')
            material('large.mat', '2' * 32)
            material('tiny.mat', '3' * 32)
            args = SimpleNamespace(project=root, output=root / 'sourceRTXRemix',
                manifest=root / 'sourceRTXRemix/manifest.json', folder='', max_size=4096)
            batch.plan(args)
            result = batch.read_json(args.manifest)
            self.assertEqual(len(result['entries']), 1)
            self.assertEqual(len(result['entries'][0]['materials']), 2)
            self.assertEqual({e['reason'] for e in result['excluded']},
                {'alpha_test', 'transparent', 'special_material', 'needs_separate_2x_workflow', 'tiny_technical_texture'})
            with self.assertRaises(RuntimeError):
                batch.plan(args)
            # The alpha pass selects cutouts only, including a separate-alpha variant
            # through its exact imported albedo GUID, and carries the material settings.
            stone = textures / 'stone__albedo.png'
            with Image.open(stone) as image:
                rgba = image.convert('RGBA')
                rgba.putpixel((0, 0), (0, 0, 0, 0))
                rgba.save(stone)
            args.kind = 'alpha-test'
            args.manifest = root / 'sourceRTXRemix/alpha-manifest.json'
            batch.plan(args)
            result = batch.read_json(args.manifest)
            self.assertEqual(len(result['entries']), 1)
            self.assertEqual(result['entries'][0]['materials'], ['leaves.mat'])
            self.assertEqual(result['entries'][0]['alpha_mode'], 'source-nearest')
            self.assertTrue(result['entries'][0]['material_settings']['leaves.mat']['alpha_test'])

    def test_resume_rejects_missing_corrupt_or_different_results(self):
        with TemporaryDirectory(dir=TEST_ROOT) as temp:
            root = Path(temp)
            assert root.resolve().is_relative_to(TEST_ROOT.resolve())
            entry = {'destination': str(root), 'sha256': 'source'}
            self.assertFalse(batch.verified(entry, 'workflow'))
            files = {}
            for role in batch.ROLES.values():
                path = root / (role + '.png')
                Image.new('RGB', (16, 16)).save(path)
                files[role] = {'file': path.name, 'sha256': batch.sha256(path)}
            batch.write_json(root / 'result.json', {'input_sha256': 'source', 'workflow_sha256': 'workflow', 'files': files})
            self.assertTrue(batch.verified(entry, 'workflow'))
            self.assertFalse(batch.verified(entry, 'different-workflow'))
            (root / 'albedo.png').write_bytes(b'corrupted')
            self.assertFalse(batch.verified(entry, 'workflow'))

    def test_rgb_workflow_has_no_alpha_upscaler_or_dangling_links(self):
        prompt = {'177': {'inputs': {'image': 'example.png'}},
                  '181': {'inputs': {'textures': ['27', 0]}},
                  '31': {'inputs': {}}, '27': {'inputs': {}}, '33': {'inputs': {}}}
        result = batch.build_prompt(prompt, {}, 'unique.png')
        self.assertEqual(result['177']['inputs']['image'], 'unique.png')
        self.assertEqual(result['181']['inputs']['textures'], ['31', 0])
        self.assertNotIn('27', result)
        self.assertNotIn('33', result)
        self.assertEqual(prompt['177']['inputs']['image'], 'example.png')

    def test_alpha_keeps_holes_and_coverage_for_every_cutoff(self):
        mask = Image.new('L', (2, 2))
        mask.putdata([0, 64, 128, 255])
        enlarged = batch.upscale_alpha(mask, (8, 8))
        for y in range(8):
            for x in range(8):
                self.assertEqual(enlarged.getpixel((x, y)), mask.getpixel((x // 4, y // 4)))
        for threshold in range(256):
            self.assertEqual(sum(v >= threshold for v in enlarged.tobytes()),
                             16 * sum(v >= threshold for v in mask.tobytes()))

    def test_collect_rejoins_source_alpha_and_validates_five_maps(self):
        with TemporaryDirectory(dir=TEST_ROOT) as temp:
            root = Path(temp)
            assert root.resolve().is_relative_to(TEST_ROOT.resolve())
            source = Image.new('RGBA', (2, 2), (80, 100, 120, 255))
            source.putpixel((0, 0), (80, 100, 120, 0))
            source.save(root / 'source.png')
            entry = {'input': str(root / 'source.png'), 'size': [2, 2], 'texture': 'leaf__albedo.png',
                     'destination': str(root / 'result'), 'alpha_mode': 'source-nearest',
                     'sha256': batch.sha256(root / 'source.png'), 'materials': ['leaf.mat'],
                     'material_settings': {'leaf.mat': {'alpha_cutoff': 0.5}}, 'prompt_id': 'test'}
            pixels = io.BytesIO()
            Image.new('RGB', (8, 8), (20, 40, 60)).save(pixels, format='PNG')
            outputs = {key: {'images': [{'type': 'output', 'filename': 'test.png', 'subfolder': 'test'}]}
                       for key in batch.ROLES}
            with patch.object(batch.urllib.request, 'urlopen', side_effect=lambda *a, **k: io.BytesIO(pixels.getvalue())):
                files = batch.collect('http://127.0.0.1:8188', {'outputs': outputs}, entry, 'graph')
            self.assertEqual(set(files), set(batch.ROLES.values()) | {'alpha'})
            self.assertTrue(batch.verified(entry, 'graph'))
            with Image.open(root / 'result/leaf__albedo.png') as saved:
                self.assertEqual(saved.mode, 'RGBA')
                self.assertEqual(saved.getpixel((0, 0)), (20, 40, 60, 0))
                self.assertEqual(saved.getpixel((7, 7)), (20, 40, 60, 255))


if __name__ == '__main__':
    unittest.main()

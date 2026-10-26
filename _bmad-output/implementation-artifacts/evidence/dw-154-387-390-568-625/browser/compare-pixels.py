"""Compare decoded screenshot pixels, independently of PNG compression and metadata (stdlib only)."""
from pathlib import Path
import hashlib
import json
import struct
import sys
import zlib


def normalized_pixels(path):
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('Not a PNG: ' + str(path))
    offset, compressed = 8, bytearray()
    while offset < len(data):
        size = struct.unpack('>I', data[offset:offset + 4])[0]
        kind = data[offset + 4:offset + 8]
        chunk = data[offset + 8:offset + 8 + size]
        crc = struct.unpack('>I', data[offset + 8 + size:offset + 12 + size])[0]
        if zlib.crc32(kind + chunk) & 0xffffffff != crc:
            raise ValueError('Invalid PNG CRC')
        if kind == b'IHDR':
            width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', chunk)
            if depth != 8 or color not in (2, 6) or compression or filtering or interlace:
                raise ValueError('Expected non-interlaced 8-bit RGB/RGBA Chromium screenshots')
        elif kind == b'IDAT':
            compressed.extend(chunk)
        offset += size + 12
    channels = 3 if color == 2 else 4
    stride = width * channels
    raw = zlib.decompress(compressed)
    if len(raw) != (stride + 1) * height:
        raise ValueError('Invalid PNG pixel extent')
    previous = bytearray(stride)
    pixels = bytearray(struct.pack('>II', width, height))
    for y in range(height):
        start = y * (stride + 1)
        filter_kind = raw[start]
        row = bytearray(raw[start + 1:start + 1 + stride])
        if filter_kind > 4:
            raise ValueError('Unknown PNG filter')
        for x in range(stride):
            a = row[x - channels] if x >= channels else 0
            b = previous[x]
            c = previous[x - channels] if x >= channels else 0
            if filter_kind == 0: predictor = 0
            elif filter_kind == 1: predictor = a
            elif filter_kind == 2: predictor = b
            elif filter_kind == 3: predictor = (a + b) // 2
            else:
                p = a + b - c
                distances = (abs(p - a), abs(p - b), abs(p - c))
                predictor = (a, b, c)[distances.index(min(distances))]
            row[x] = (row[x] + predictor) & 255
        for x in range(0, stride, channels):
            alpha = row[x + 3] if channels == 4 else 255
            pixels.extend((row[x + i] * alpha + 255 * (255 - alpha) + 127) // 255 for i in range(3))
        previous = row
    return bytes(pixels), (width, height)


def distinct_crops(paths):
    records = []
    for path in paths:
        pixels, dimensions = normalized_pixels(path)
        records.append({'file': path.name, 'dimensions': dimensions, 'rgb_sha256': hashlib.sha256(pixels).hexdigest()})
    collisions = [(a['file'], b['file']) for i, a in enumerate(records) for b in records[i + 1:]
                  if a['rgb_sha256'] == b['rgb_sha256']]
    return {'distinct': not collisions, 'collisions': collisions, 'crops': records}


def compare(directory):
    positives = []
    for culture in ('en', 'fr'):
        for width in (1440, 390):
            for colors in ('none', 'active'):
                group = distinct_crops([directory / f'annotated-{culture}-{width}-{colors}-{i}.png' for i in range(5)])
                if not group['distinct']:
                    raise AssertionError('Annotated visual collision: ' + repr(group['collisions']))
                positives.append(group)
    # The exact same visual check is applied to actual fixture mutation crops, regardless of its string assertion.
    mutation = distinct_crops([directory / f'literal-mutation-{i}.png' for i in range(5)])
    if mutation['distinct'] or not any(set(pair) == {'literal-mutation-0.png', 'literal-mutation-1.png'} for pair in mutation['collisions']):
        raise AssertionError('The known same-layout literal whitespace collision escaped the visual control')
    known_pair = distinct_crops([directory / 'literal-0.png', directory / 'literal-1.png'])
    if known_pair['distinct']:
        raise AssertionError('The retained initial collision pair no longer compares equal')
    result = {'normalization': 'PNG decode/unfilter, 8-bit RGB; alpha composited over white; dimensions included; PNG metadata ignored',
              'positive_groups': positives, 'literal_fixture_mutation': mutation,
              'initial_known_collision': known_pair, 'literal_mutation_rejected_by_positive_visual_check': True}
    (directory / 'pixel-comparison.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS: eight annotated groups have five distinct decoded pixel crops; the identical visual check rejects literal collisions.')
    return result


if __name__ == '__main__':
    compare(Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent)

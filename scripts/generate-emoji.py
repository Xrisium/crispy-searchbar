"""Generate offline Emoji resources. Install pypinyin==0.55.0, then run this script.

The checked-in source lock is verified on every run. --refresh-lock is only for
an intentional data update. Downloads are cached under ignored artifacts/.
"""
import argparse
import gzip
import hashlib
import io
import importlib.metadata
import json
from pathlib import Path
import re
import tarfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from pypinyin import lazy_pinyin, load_phrases_dict

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / 'artifacts/emoji-sources'
OUTPUT = ROOT / 'data/emoji'
LOCK = ROOT / 'scripts/emoji-sources.lock.json'
SOURCES = {
    'emoji-test.txt': 'https://www.unicode.org/Public/17.0.0/emoji/emoji-test.txt',
    **{f'{kind}-{lang}.xml': f'https://raw.githubusercontent.com/unicode-org/cldr/release-48/common/{kind}/{lang}.xml'
       for kind in ['annotations', 'annotationsDerived'] for lang in ['en', 'zh']},
    'twemoji.tar.gz': 'https://codeload.github.com/jdecked/twemoji/tar.gz/refs/tags/v17.0.3',
}

def canonical(text):
    return text.replace('\ufe0f', '')

def main():
    if importlib.metadata.version('pypinyin') != '0.55.0':
        raise ValueError('Install the pinned pypinyin 0.55.0 before generating data')
    args = argparse.ArgumentParser()
    args.add_argument('--refresh-lock', action='store_true')
    refresh = args.parse_args().refresh_lock
    CACHE.mkdir(parents=True, exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    lock = {} if refresh else json.loads(LOCK.read_text('utf-8'))
    data = {}
    for name, url in SOURCES.items():
        path = CACHE / name
        if not path.exists():
            print(f'Downloading {name}', flush=True)
            with urllib.request.urlopen(url, timeout=120) as response:
                path.write_bytes(response.read())
        content = path.read_bytes()
        checksum = hashlib.sha256(content).hexdigest()
        if not refresh and lock[name] != {'url': url, 'sha256': checksum}:
            raise ValueError(f'Source checksum mismatch: {name}')
        lock[name] = {'url': url, 'sha256': checksum}
        data[name] = content
    if refresh:
        LOCK.write_text(json.dumps(lock, indent=2) + '\n', 'utf-8')
    annotations = {'en': {}, 'zh': {}}
    for lang in annotations:
        for kind in ['annotations', 'annotationsDerived']:
            for node in ET.fromstring(data[f'{kind}-{lang}.xml']).iter('annotation'):
                value = annotations[lang].setdefault(canonical(node.attrib['cp']), {'name': '', 'keywords': []})
                text = ''.join(node.itertext()).strip()
                if node.attrib.get('type') == 'tts': value['name'] = text
                else: value['keywords'].extend(text.split(' | '))
    custom = json.loads((ROOT / 'scripts/emoji-aliases.json').read_text('utf-8'))
    aliases_by_sequence = {}
    for sequence, aliases in custom['aliases'].items():
        aliases_by_sequence.setdefault(canonical(sequence), []).extend(aliases)
    for phrase, syllables in custom['pronunciations'].items():
        load_phrases_dict({phrase: [[syllable] for syllable in syllables.split()]})
    images = {}
    with tarfile.open(fileobj=io.BytesIO(data['twemoji.tar.gz'])) as archive:
        for member in archive.getmembers():
            if '/assets/72x72/' in member.name and member.name.endswith('.png'):
                images[Path(member.name).stem] = archive.extractfile(member).read()
    entries = []
    group = subgroup = ''
    for line in data['emoji-test.txt'].decode('utf-8').splitlines():
        if line.startswith('# group:'): group = line.split(': ', 1)[1]
        if line.startswith('# subgroup:'): subgroup = line.split(': ', 1)[1]
        if '; fully-qualified' not in line: continue
        points = line.split(';')[0].strip().split()
        sequence = ''.join(chr(int(point, 16)) for point in points)
        key = canonical(sequence)
        fallback = re.split(r' E[\d.]+ ', line.split('#', 1)[1], maxsplit=1)[-1]
        en = annotations['en'].get(key, {'name': fallback, 'keywords': []})
        zh = annotations['zh'].get(key, {'name': '', 'keywords': []})
        aliases = aliases_by_sequence.get(key, [])
        terms = list(dict.fromkeys([en['name'], zh['name'], *aliases, *en['keywords'], *zh['keywords']]))
        terms = [term for term in terms if term]
        pinyin = []
        initials = []
        for term in terms:
            if re.search('[\u3400-\u9fff]', term):
                syllables = lazy_pinyin(term, errors='ignore')
                pinyin.append(''.join(syllables))
                initials.append(''.join(s[0] for s in syllables if s))
        image = '-'.join(f'{int(point, 16):x}' for point in points)
        if image not in images: image = '-'.join(p for p in image.split('-') if p != 'fe0f')
        if image not in images: raise ValueError(f'Missing Twemoji image: {sequence} {image}')
        base = ''.join(c for c in key if not 0x1f3fb <= ord(c) <= 0x1f3ff)
        entries.append({'sequence': sequence, 'nameEn': en['name'] or fallback,
                        'nameZh': zh['name'] or en['name'] or fallback,
                        'preferredTerms': list(dict.fromkeys([en['name'], zh['name'], *aliases, *[''.join(lazy_pinyin(t, errors='ignore')) for t in [en['name'], zh['name'], *aliases] if re.search('[\u3400-\u9fff]', t)]])),
                        'terms': terms, 'pinyin': sorted(set(pinyin)), 'initials': sorted(set(initials)),
                        'variantGroup': base, 'image': image, 'order': len(entries),
                        'group': group, 'subgroup': subgroup})
    # Legacy neutral pair glyphs use a different representation from mixed-tone pairs.
    legacy_pairs = {'🫱\u200d🫲': '🤝', '👩\u200d🤝\u200d👩': '👭',
                    '👨\u200d🤝\u200d👨': '👬', '👩\u200d🤝\u200d👨': '👫'}
    for entry in entries:
        entry['variantGroup'] = legacy_pairs.get(entry['variantGroup'], entry['variantGroup'])
    content = json.dumps(entries, ensure_ascii=False, separators=(',', ':')).encode('utf-8')
    buffer = io.BytesIO()
    with gzip.GzipFile(filename='', mode='wb', fileobj=buffer, mtime=0, compresslevel=9) as compressed:
        compressed.write(content)
    (OUTPUT / 'catalog.json.gz').write_bytes(buffer.getvalue())
    with zipfile.ZipFile(OUTPUT / 'twemoji.zip', 'w', compression=zipfile.ZIP_STORED) as archive:
        for name in sorted({entry['image'] for entry in entries}):
            info = zipfile.ZipInfo(name + '.png', (1980, 1, 1, 0, 0, 0))
            archive.writestr(info, images[name])
    manifest = {'unicode': '17.0', 'cldr': '48', 'twemoji': '17.0.3', 'pypinyin': '0.55.0',
                'entries': len(entries), 'images': len({entry['image'] for entry in entries}),
                'outputs': {p.name: {'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
                            for p in [OUTPUT / 'catalog.json.gz', OUTPUT / 'twemoji.zip']}}
    (ROOT / 'scripts/emoji-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', 'utf-8')
    print(json.dumps(manifest), flush=True)

if __name__ == '__main__': main()

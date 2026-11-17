#!/usr/bin/env python3
"""Package real map previews as Panorama CSS backgrounds (no Image panel or JS).

Run against the server's mapmanager config, then compile the workshop directory.
Uses only Python's standard library. Optional --images JSON maps map names/IDs to
local PNG/JPEG files; otherwise Workshop previews are fetched from Steam.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
ENDPOINT = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/'


def read_jsonc(path):
    # Preserve quoted strings, including URLs and comment markers inside them.
    text = Path(path).read_text(encoding='utf-8-sig')
    text = re.sub(r'"(?:\\.|[^"\\])*"|//[^\n]*|/\*[\s\S]*?\*/',
                  lambda m: m[0] if m[0].startswith('"') else ' ', text)
    text = re.sub(r'"(?:\\.|[^"\\])*"|,\s*(?=[}\]])',
                  lambda m: m[0] if m[0].startswith('"') else '', text)
    return json.loads(text)


def get(obj, key, default=None):
    return next((v for k, v in obj.items() if k.lower() == key.lower()), default)


def catalog(document):
    available = get(get(get(document, 'sws2-cs2mapmanager', {}), 'Maps', {}), 'Available', {})
    entries = available.items() if isinstance(available, dict) else ((None, v) for v in available)
    result = {}
    for name, value in entries:
        if isinstance(value, dict):
            name = get(value, 'Name', name)
            local = get(value, 'MapName')
            workshop = get(value, 'WorkshopId')
        else:
            value = str(value).strip()
            local = value[6:] if value.lower().startswith('local:') else None
            workshop = None if local is not None else value
        local = str(local).strip() if local is not None else ''
        workshop = str(workshop).strip() if workshop is not None else ''
        if bool(local) == bool(workshop):
            continue
        if local and re.fullmatch(r'[A-Za-z0-9_-]{1,128}', local):
            map_id = 'map:' + local.lower()
        elif re.fullmatch(r'[0-9]+', workshop) and 0 < int(workshop) < 2**64:
            workshop = str(int(workshop))
            map_id = 'workshop:' + workshop
        else:
            continue
        result.setdefault(map_id, (name or local or workshop, workshop or None))
    return result


def thumbnail_class(map_id):
    return 'map-thumb-' + hashlib.sha256(map_id.lower().encode()).hexdigest()[:16]


def fetch(url, timeout, data=None):
    request = urllib.request.Request(url, data=data, headers={'User-Agent': 'sws2-map-thumbnails/1.0'})
    with urllib.request.urlopen(request, timeout=timeout) as response:
        content = response.read(10 * 1024 * 1024 + 1)
    if len(content) > 10 * 1024 * 1024:
        raise ValueError('response exceeds 10 MiB')
    return content


def write_image(content, path):
    if content.startswith(b'\x89PNG\r\n\x1a\n'):
        extension = '.png'
    elif content.startswith(b'\xff\xd8\xff'):
        extension = '.jpg'
    else:
        raise ValueError('preview must be PNG or JPEG')
    target = path.with_suffix(extension)
    temporary = target.with_suffix(extension + '.tmp')
    temporary.write_bytes(content)
    temporary.replace(target)
    for other in ('.png', '.jpg'):
        if other != extension:
            path.with_suffix(other).unlink(missing_ok=True)
    return target


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', required=True, type=Path, help='mapmanager config.jsonc')
    parser.add_argument('--images', type=Path, help='JSON mapping map names or stable IDs to local image files')
    parser.add_argument('--offline', action='store_true', help='use local images/cache without contacting Steam')
    parser.add_argument('--workshop-dir', type=Path, default=ROOT / 'workshop')
    parser.add_argument('--timeout', type=int, default=30)
    args = parser.parse_args()
    maps = catalog(read_jsonc(args.config))
    overrides = read_jsonc(args.images) if args.images else {}
    folder = args.workshop_dir / 'panorama/images/custom_game/sws2cs2menus/maps'
    folder.mkdir(parents=True, exist_ok=True)
    cached = lambda key: next((p for ext in ('.png', '.jpg') if (p := (folder / key).with_suffix(ext)).is_file()), None)
    previews = {}
    ids = [workshop for map_id, (name, workshop) in maps.items()
           if workshop and not cached(thumbnail_class(map_id)) and not overrides.get(map_id, overrides.get(name))]
    if not args.offline:
        for start in range(0, len(ids), 100):
            batch = ids[start:start + 100]
            form = {'itemcount': str(len(batch)), **{f'publishedfileids[{i}]': v for i, v in enumerate(batch)}}
            try:
                payload = json.loads(fetch(ENDPOINT, args.timeout, urllib.parse.urlencode(form).encode()))
                for entry in payload['response']['publishedfiledetails']:
                    url = entry.get('preview_url', '')
                    if entry.get('result') == 1 and urllib.parse.urlsplit(url).scheme == 'https':
                        previews[str(entry['publishedfileid'])] = url
            except Exception as error:
                print(f'Steam details unavailable: {error}. Keeping local/cached images.', file=sys.stderr)
                break
    rules = ['/* Generated by tools/build_map_thumbnails.py. Local screenshots only. */']
    count = 0
    for map_id, (name, workshop) in maps.items():
        key = thumbnail_class(map_id)
        target = cached(key)
        override = overrides.get(map_id, overrides.get(name))
        try:
            if override:
                source = Path(override)
                if not source.is_absolute():
                    source = args.images.parent / source
                target = write_image(source.read_bytes(), folder / key)
            elif target is None and workshop in previews:
                target = write_image(fetch(previews[workshop], args.timeout), folder / key)
        except Exception as error:
            print(f'{name}: {error}. Using cached image or placeholder.', file=sys.stderr)
        if target:
            rules.append(f'.{key} {{ background-image: url("file://{{images}}/custom_game/sws2cs2menus/maps/{target.name}"); }}')
            count += 1
    css = args.workshop_dir / 'panorama/styles/custom_game/sws2cs2menus/map_thumbnails.css'
    css.parent.mkdir(parents=True, exist_ok=True)
    css.write_text('\n'.join(rules) + '\n')
    print(f'Packaged {count}/{len(maps)} map previews into {args.workshop_dir}. Missing images use a text placeholder.')
    print('Recompile the XML, CSS and image resources in CS2 Workshop Tools, then update the mounted addon.')


if __name__ == '__main__':
    main()

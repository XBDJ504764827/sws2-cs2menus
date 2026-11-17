#!/usr/bin/env python3
"""Validate the menu installation package and its stable base contract."""
from pathlib import PurePosixPath
import sys
import zipfile


def main():
    with zipfile.ZipFile(sys.argv[1]) as package:
        names = set(package.namelist())
        required = ['sws2-cs2menus.dll', 'sws2-cs2menus.deps.json', 'Sws2Cs2Menus.Contracts.dll',
                    'resources/exports/Sws2Cs2Menus.Contracts.dll', 'resources/hud-addon.json',
                    'resources/templates/config.template.jsonc', 'resources/translations/en.jsonc',
                    'resources/translations/zh-CN.jsonc', 'resources/translations/zh-TW.jsonc']
        assert all('sws2-cs2menus/' + name in names for name in required), 'Incomplete menu package'
        assert all(name.startswith('sws2-cs2menus/') and '..' not in PurePosixPath(name).parts for name in names)
        assert not any(PurePosixPath(name).name in ('SwiftlyS2.CS2.dll', 'SwiftlyS2.Shared.dll', 'SwiftlyS2.Core.dll') for name in names)
        contract = package.read('sws2-cs2menus/Sws2Cs2Menus.Contracts.dll')
        assert contract == package.read('sws2-cs2menus/resources/exports/Sws2Cs2Menus.Contracts.dll')
        assert b'get_IsCustomHudAvailable' not in contract, 'Keep HUD capability outside the stable base interface'
        plugin = package.read('sws2-cs2menus/sws2-cs2menus.dll')
        assert 'sws2-cs2menus.custom-hud.v1'.encode('utf-16le') in plugin
        assert package.testzip() is None
    print('Menu package and base-contract checks passed.')


if __name__ == '__main__':
    main()

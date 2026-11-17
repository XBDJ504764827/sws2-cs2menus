#!/usr/bin/env python3
"""Check HUD readiness with both current and preloaded legacy menu exports."""
import argparse
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--plugin', action='append', default=[], help='Also JIT-check a built plugin DLL')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    plugins = [str(Path(path).resolve()) for path in args.plugin]

    def run(*arguments):
        subprocess.run([args.dotnet, *arguments], cwd=root, check=True)

    run('build', 'tests/LegacyMenuContracts/LegacyMenuContracts.csproj', '-c', 'Release', '-warnaserror')
    run('run', '--project', 'tests/HudChecks/HudChecks.csproj', '-c', 'Release')
    run('run', '--project', 'tests/ContractLoadChecks/ContractLoadChecks.csproj', '-c', 'Release', '--',
        'tests/LegacyMenuContracts/bin/Release/net10.0/Sws2Cs2Menus.Contracts.dll',
        'tests/HudChecks/bin/Release/net10.0/HudChecks.dll', *plugins)


if __name__ == '__main__':
    main()

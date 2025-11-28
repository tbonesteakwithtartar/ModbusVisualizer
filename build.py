#!/usr/bin/env python3
"""
Build script for ModbusVisualizer - Creates standalone Windows executables
Run: python build.py [--clean] [--no-trim] [--runtime win-x86]
"""

import subprocess
import os
import sys
import shutil
import argparse
from pathlib import Path

def run_command(cmd, cwd=None):
    """Run a command and return success status"""
    print(f"  Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd, capture_output=False)
    return result.returncode == 0

def main():
    parser = argparse.ArgumentParser(description='Build ModbusVisualizer standalone executable')
    parser.add_argument('--clean', action='store_true', help='Clean build directory before building')
    parser.add_argument('--no-trim', action='store_true', help='Skip publishing trimmed binary')
    parser.add_argument('--runtime', default='win-x64', help='Target runtime (default: win-x64)')
    args = parser.parse_args()

    # Setup paths
    script_dir = Path(__file__).parent
    project_dir = script_dir / 'ModbusVisualizer'
    build_dir = script_dir / 'bin' / 'publish'

    print()
    print("=" * 50)
    print("ModbusVisualizer Build Script")
    print("=" * 50)
    print()
    print(f"Project Directory: {project_dir}")
    print(f"Build Output: {build_dir}")
    print(f"Runtime: {args.runtime}")
    print(f"Trim Binary: {not args.no_trim}")
    print()

    # Clean if requested
    if args.clean or not project_dir.exists():
        if build_dir.exists():
            print("Cleaning previous build...")
            shutil.rmtree(build_dir)

    # Restore dependencies
    print("Restoring NuGet packages...")
    if not run_command(['dotnet', 'restore'], cwd=project_dir):
        print("Error: Failed to restore packages")
        return 1

    # Build Release
    print("Building Release configuration...")
    if not run_command(['dotnet', 'build', '--configuration', 'Release', '--no-restore'], cwd=project_dir):
        print("Error: Failed to build project")
        return 1

    # Publish as self-contained single-file executable
    print("Publishing self-contained executable...")
    publish_args = [
        'dotnet', 'publish',
        '--configuration', 'Release',
        '--runtime', args.runtime,
        '--self-contained',
        '-p:PublishSingleFile=true',
        '-p:PublishReadyToRun=true',
        '--output', str(build_dir)
    ]

    if not args.no_trim:
        publish_args.append('-p:PublishTrimmed=true')

    if not run_command(publish_args, cwd=project_dir):
        print("Error: Failed to publish executable")
        return 1

    # Check output
    exe_path = build_dir / 'ModbusVisualizer.exe'
    if exe_path.exists():
        exe_size = exe_path.stat().st_size
        size_mb = exe_size / (1024 * 1024)
        print()
        print("=" * 50)
        print("Build Complete!")
        print("=" * 50)
        print()
        print(f"Executable: {exe_path}")
        print(f"File Size: {size_mb:.2f} MB ({exe_size:,} bytes)")
        print()
        print(f"To run: {exe_path}")
        print()
        return 0
    else:
        print("Error: Executable not found after build")
        return 1

if __name__ == '__main__':
    sys.exit(main())

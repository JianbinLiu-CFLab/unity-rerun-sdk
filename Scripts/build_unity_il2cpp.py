#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
#
# Unity2Rerun IL2CPP Standalone Player build script.
# Usage: python Scripts/build_unity_il2cpp.py [--unity-path <path>] [--dry-run]

"""Build the Unity2Rerun demo project for IL2CPP standalone.

Examples:
  python Scripts/build_unity_il2cpp.py
  python Scripts/build_unity_il2cpp.py --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3/Editor/Unity.exe"
  python Scripts/build_unity_il2cpp.py --dry-run
"""

from __future__ import annotations

import argparse
import os
import platform
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import List, Optional, Tuple


IMPORTANT_LOG_MARKERS = (
    "[Rerun",
    "Build Finished",
    "Build succeeded",
    "Build failed",
    "Scripts have compiler errors",
    "Script Compilation",
    "Tundra build failed",
    "error CS",
    "Exception",
    "NullReference",
    "IL2CPP",
    "Csc ",
    "Bee",
)


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def unity_version_key(path: Path) -> Tuple[int, ...]:
    for part in reversed(path.parts):
        if re.match(r"^\d+\.\d+\.\d+", part):
            return tuple(int(n) for n in re.findall(r"\d+", part))
    return ()


def newest_existing(paths: List[Path]) -> Optional[Path]:
    existing = [p for p in paths if p.exists()]
    if not existing:
        return None
    return max(existing, key=lambda p: (unity_version_key(p), p.stat().st_mtime))


def find_unity_explicit(path: Optional[str]) -> Optional[Path]:
    if not path:
        return None
    unity = Path(path).expanduser()
    if unity.exists():
        return unity
    raise FileNotFoundError(f"--unity-path path does not exist: {unity}")


def find_unity_from_env() -> Optional[Path]:
    for name in ("UNITY_EXE", "UNITY_PATH"):
        value = os.environ.get(name)
        if value:
            unity = Path(value).expanduser()
            if unity.exists():
                return unity
            raise FileNotFoundError(f"{name} points to a missing file: {unity}")
    return None


def find_unity_from_hub() -> Optional[Path]:
    system = platform.system().lower()
    candidates: List[Path] = []

    if system == "windows":
        for root in (
            Path(os.environ.get("PROGRAMFILES", r"C:\Program Files")),
            Path(os.environ.get("PROGRAMFILES(X86)", r"C:\Program Files (x86)")),
        ):
            candidates.extend(root.glob(r"Unity/Hub/Editor/*/Editor/Unity.exe"))
    elif system == "darwin":
        candidates.extend(Path("/Applications/Unity/Hub/Editor").glob("*/Unity.app/Contents/MacOS/Unity"))
        candidates.append(Path("/Applications/Unity/Unity.app/Contents/MacOS/Unity"))
    elif system == "linux":
        for root in (Path.home() / "Unity" / "Hub" / "Editor", Path("/opt/Unity/Hub/Editor")):
            candidates.extend(root.glob("*/Editor/Unity"))
        candidates.append(Path("/opt/Unity/Editor/Unity"))

    return newest_existing(candidates)


def find_unity(path: Optional[str]) -> Path:
    unity = (
        find_unity_explicit(path)
        or find_unity_from_env()
        or find_unity_from_hub()
    )
    if unity:
        return unity

    msg = "Unity executable was not found."
    if platform.system().lower() == "windows":
        msg += ' Pass --unity-path "C:/Program Files/Unity/Hub/Editor/<version>/Editor/Unity.exe" or set UNITY_EXE.'
    else:
        msg += " Pass --unity-path <path> or set UNITY_EXE/UNITY_PATH."
    raise FileNotFoundError(msg)


def relative_to_root(path: Path, root: Path) -> str:
    try:
        return str(path.resolve().relative_to(root.resolve()))
    except ValueError:
        return str(path)


def build_command(args: argparse.Namespace) -> Tuple[List[str], Path, Path, Path]:
    root = repo_root()
    project_path = (root / args.project).resolve()
    build_dir = (root / args.build_dir).resolve() if args.build_dir else default_build_dir(root)
    log_path = (root / args.log).resolve() if args.log else build_dir / "Unity2Rerun_IL2CPP_build.log"
    output_path = (root / args.output).resolve() if args.output else build_dir / "WindowsIL2CPP" / "Unity2RerunDemo.exe"
    unity = find_unity(args.unity)

    if not project_path.exists():
        raise FileNotFoundError(f"Unity project was not found: {project_path}")

    cmd = [
        str(unity),
        "-batchmode",
        "-quit",
        "-projectPath", str(project_path),
        "-executeMethod", "RerunBuild.BuildIl2CppFromCommandLine",
        "-rerunBuildOutput", str(output_path),
        "-logFile", str(log_path),
    ]

    if args.development:
        cmd.append("-development")

    return cmd, project_path, log_path, output_path


def default_build_dir(root: Path) -> Path:
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    return root / "build" / "Unity" / f"win64-il2cpp-{stamp}"


def format_elapsed(seconds: float) -> str:
    total = int(seconds)
    hours, remainder = divmod(total, 3600)
    minutes, seconds = divmod(remainder, 60)
    if hours:
        return f"{hours:02d}:{minutes:02d}:{seconds:02d}"
    return f"{minutes:02d}:{seconds:02d}"


def is_important_log_line(line: str) -> bool:
    stripped = line.strip()
    if not stripped:
        return False
    return any(marker in stripped for marker in IMPORTANT_LOG_MARKERS)


def read_new_important_lines(log_path: Path, offset: int) -> Tuple[int, List[str]]:
    if not log_path.exists():
        return offset, []

    try:
        with log_path.open("r", encoding="utf-8", errors="replace") as handle:
            handle.seek(offset)
            lines = handle.readlines()
            new_offset = handle.tell()
    except OSError:
        return offset, []

    important = [line.strip() for line in lines if is_important_log_line(line)]
    return new_offset, important


def terminate_process(process: subprocess.Popen) -> None:
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=10)


def run_with_progress(cmd: List[str], root: Path, log_path: Path, interval: int, timeout_seconds: int) -> int:
    started = time.monotonic()
    next_heartbeat = started + interval
    offset = 0

    process = subprocess.Popen(cmd, cwd=root)
    while True:
        offset, lines = read_new_important_lines(log_path, offset)
        for line in lines:
            print(f"[unity-log] {line}", flush=True)

        returncode = process.poll()
        now = time.monotonic()
        if returncode is not None:
            break

        elapsed_seconds = now - started
        if timeout_seconds > 0 and elapsed_seconds >= timeout_seconds:
            elapsed = format_elapsed(elapsed_seconds)
            print(
                f"[build_unity_il2cpp] Timeout after {elapsed}; terminating Unity build.",
                file=sys.stderr,
                flush=True,
            )
            terminate_process(process)
            return 124

        if now >= next_heartbeat:
            elapsed = format_elapsed(elapsed_seconds)
            print(
                f"[build_unity_il2cpp] Elapsed {elapsed}; still building. "
                f"Log: {relative_to_root(log_path, root)}",
                flush=True,
            )
            next_heartbeat = now + interval

        time.sleep(1)

    offset, lines = read_new_important_lines(log_path, offset)
    for line in lines:
        print(f"[unity-log] {line}", flush=True)

    elapsed = format_elapsed(time.monotonic() - started)
    print(f"[build_unity_il2cpp] Unity exited after {elapsed}.", flush=True)
    return returncode


def parse_args(argv: Optional[List[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run Unity batchmode IL2CPP build for Unity2Rerun.")
    parser.add_argument("--unity", "--unity-path", dest="unity", help="Path to Unity.exe. Falls back to UNITY_EXE, Hub scan.")
    parser.add_argument("--project", "--project-path", dest="project", default="Unity2Rerun", help="Unity project path relative to repo root.")
    parser.add_argument("--log", help="Log path. Defaults to <build-dir>/Unity2Rerun_IL2CPP_build.log.")
    parser.add_argument("--build-dir", "--output-dir", dest="build_dir", help="Build run directory. Defaults to build/Unity/<timestamp>/.")
    parser.add_argument("--output", "--output-path", dest="output", help="Player output path. Defaults to <build-dir>/WindowsIL2CPP/Unity2RerunDemo.exe.")
    parser.add_argument("--development", action="store_true", help="Build with development options (profiler, debug).")
    parser.add_argument("--dry-run", action="store_true", help="Print resolved paths without starting Unity.")
    parser.add_argument("--progress-interval", type=int, default=15, help="Seconds between progress heartbeats.")
    parser.add_argument("--timeout-seconds", type=int, default=0, help="Max build time in seconds. 0 = no limit.")
    return parser.parse_args(argv)


def main() -> int:
    args = parse_args()
    root = repo_root()

    try:
        cmd, project_path, log_path, output_path = build_command(args)
    except Exception as exc:
        print(f"[build_unity_il2cpp] {exc}", file=sys.stderr)
        return 2

    print(f"[build_unity_il2cpp] Unity:    {cmd[0]}")
    print(f"[build_unity_il2cpp] Project:  {relative_to_root(project_path, root)}")
    print(f"[build_unity_il2cpp] Log:      {relative_to_root(log_path, root)}")
    print(f"[build_unity_il2cpp] Output:   {relative_to_root(output_path, root)}")

    if args.dry_run:
        print("[build_unity_il2cpp] Dry run only; Unity was not started.")
        return 0

    log_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    print("[build_unity_il2cpp] Starting Unity batchmode build...")

    returncode = run_with_progress(cmd, root, log_path, max(1, args.progress_interval), args.timeout_seconds)
    if returncode == 0:
        print("[build_unity_il2cpp] Build command completed successfully.")
    else:
        print(
            f"[build_unity_il2cpp] Build failed with exit code {returncode}. "
            f"See log: {relative_to_root(log_path, root)}",
            file=sys.stderr,
        )

    return returncode


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Release hygiene checks for the Unity2Rerun UPM package.

This script is intentionally conservative: it checks public package metadata,
sample declarations, Unity .meta sidecars, obvious private/local references,
and third-party notice coverage before a GitHub release is archived by Zenodo.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "Packages" / "dev.unity2rerun.sdk"
SAMPLES = PACKAGE / "Samples~"
DOCS = PACKAGE / "Documentation~"
THIRD_PARTY_NOTICES = PACKAGE / "THIRD_PARTY_NOTICES.md"
CONCEPT_DOI = "10.5281/zenodo.20247512"
VERSION_DOI = "10.5281/zenodo.20247513"

EXPECTED_PACKAGE = {
    "name": "dev.unity2rerun.sdk",
    "displayName": "Unity2Rerun SDK",
    "license": "Apache-2.0",
    "version": "0.4.0",
}

EXPECTED_SAMPLES = {
    "Basic Rrd Recording": "Samples~/BasicRrdRecording",
    "Publisher Components": "Samples~/PublisherComponents",
    "Live Viewer": "Samples~/LiveViewer",
    "Generated RerunLog": "Samples~/GeneratedRerunLog",
    "Interactive 3D Control": "Samples~/Interactive3DControl",
}

REQUIRED_FILES = [
    ROOT / "README.md",
    ROOT / "LICENSE",
    ROOT / "CITATION.cff",
    ROOT / "AI_NOTICE.md",
    ROOT / "CHANGELOG.md",
    ROOT / "docs" / "releases" / "RELEASE_NOTES_v0.4.0.md",
    ROOT / "docs" / "releases" / "ZENODO_RELEASE_CHECKLIST.md",
    PACKAGE / "README.md",
    PACKAGE / "LICENSE",
    PACKAGE / "CHANGELOG.md",
    PACKAGE / "RELEASE_NOTES.md",
    THIRD_PARTY_NOTICES,
    PACKAGE / "Runtime" / "Unity.RerunSDK.asmdef",
    PACKAGE / "Runtime" / "link.xml",
    PACKAGE / "Editor" / "Unity.RerunSDK.Editor.asmdef",
    PACKAGE / "Editor" / "SourceGenerators" / "src" / "Unity.RerunSDK.SourceGenerators.asmdef",
    PACKAGE / "Editor" / "SourceGenerators" / "analyzers" / "dotnet" / "cs" / "RerunLogSourceGenerator.dll",
    DOCS / "README.md",
    DOCS / "en" / "00_Prerequisites.md",
    DOCS / "en" / "02_Publisher_Components.md",
    DOCS / "en" / "07_Interactive_3D_Control.md",
    PACKAGE / "Runtime" / "Plugins" / "Apache.Arrow.dll",
    PACKAGE / "Runtime" / "Plugins" / "Google.Protobuf.dll",
    PACKAGE / "Runtime" / "Plugins" / "Grpc.Net.Client.dll",
]

PUBLIC_TEXT_ROOTS = [
    ROOT / "README.md",
    ROOT / "CHANGELOG.md",
    ROOT / "PAPER.md",
    ROOT / "docs",
    PACKAGE / "README.md",
    PACKAGE / "CHANGELOG.md",
    PACKAGE / "RELEASE_NOTES.md",
    SAMPLES,
    DOCS,
]

TEXT_EXTENSIONS = {".cff", ".cs", ".json", ".md", ".txt", ".xml", ".yml"}
UNITY_META_EXTENSIONS = {".asset", ".cs", ".dll", ".inputactions", ".json", ".md", ".unity", ".xml"}
FORBIDDEN_SAMPLE_PARTS = {"Library", "Logs", "Recordings", "Generated", "Temp", "obj", "bin"}
FORBIDDEN_SAMPLE_NAME_PATTERNS = [
    re.compile(r".*_RerunLog\.g\.cs$", re.IGNORECASE),
    re.compile(r"PerformanceTestRun", re.IGNORECASE),
]

FORBIDDEN_PUBLIC_PATTERNS = [
    ("local Windows path", re.compile(r"\b[A-Za-z]:[\\/]")),
    ("private Developer folder reference", re.compile(r"Developer[\\/]")),
    ("Obsidian pasted image embed", re.compile(r"!\[\[Pasted image", re.IGNORECASE)),
    ("Obsidian wiki embed", re.compile(r"!\[\[")),
    ("TODO marker", re.compile(r"\bTODO\b")),
    ("TBD marker", re.compile(r"\bTBD\b")),
    ("FIXME marker", re.compile(r"\bFIXME\b")),
]

THIRD_PARTY_REQUIREMENTS = {
    "Runtime/Plugins/Apache.Arrow.dll": ("Apache Arrow", "Apache-2.0", "Arrow IPC"),
    "Runtime/Plugins/Google.Protobuf.dll": ("Google.Protobuf", "BSD-3-Clause"),
    "Runtime/Plugins/Grpc.Net.Client.dll": ("grpc-dotnet", "Apache-2.0"),
    "Runtime/Plugins/Microsoft.Bcl.AsyncInterfaces.dll": (
        "Microsoft .NET support libraries",
        "MIT",
        "Microsoft.Bcl.AsyncInterfaces.dll",
    ),
    "Runtime/Plugins/System.Threading.Channels.dll": (
        "Microsoft .NET support libraries",
        "MIT",
        "System.Threading.Channels.dll",
    ),
}


def iter_files(path: Path) -> list[Path]:
    if path.is_file():
        return [path]
    if not path.exists():
        return []
    return [item for item in path.rglob("*") if item.is_file()]


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8-sig")


def validate() -> list[str]:
    errors: list[str] = []
    package_json_path = PACKAGE / "package.json"

    if not package_json_path.exists():
        return [f"Missing package.json: {package_json_path}"]

    package_json = json.loads(read_text(package_json_path))
    for key, expected in EXPECTED_PACKAGE.items():
        actual = package_json.get(key)
        if actual != expected:
            errors.append(f"package.json {key!r} is {actual!r}; expected {expected!r}")

    declared_samples = package_json.get("samples", [])
    if len(declared_samples) != len(EXPECTED_SAMPLES):
        errors.append(
            f"package.json declares {len(declared_samples)} samples; expected {len(EXPECTED_SAMPLES)}"
        )

    sample_map = {sample.get("displayName"): sample.get("path") for sample in declared_samples}
    for display_name, sample_path in EXPECTED_SAMPLES.items():
        actual = sample_map.get(display_name)
        if actual != sample_path:
            errors.append(f"sample {display_name!r} path is {actual!r}; expected {sample_path!r}")
        if not (PACKAGE / sample_path).exists():
            errors.append(f"declared sample path does not exist: {sample_path}")

    for required in REQUIRED_FILES:
        if not required.exists():
            errors.append(f"required release file is missing: {required.relative_to(ROOT)}")

    errors.extend(check_sample_meta_files())
    errors.extend(check_public_text())
    errors.extend(check_sample_artifacts())
    errors.extend(check_package_artifacts())
    errors.extend(check_plugin_identity())
    errors.extend(check_third_party_notices())
    errors.extend(check_doi_metadata())
    return errors


def check_sample_meta_files() -> list[str]:
    errors: list[str] = []
    if not SAMPLES.exists():
        return ["Samples~ folder is missing"]

    for item in SAMPLES.rglob("*"):
        if item.name.endswith(".meta"):
            continue
        needs_meta = item.is_dir() or item.suffix in UNITY_META_EXTENSIONS
        if needs_meta and not item.with_name(item.name + ".meta").exists():
            errors.append(f"missing Unity .meta sidecar for sample asset: {item.relative_to(ROOT)}")
    return errors


def check_public_text() -> list[str]:
    errors: list[str] = []
    for root in PUBLIC_TEXT_ROOTS:
        for path in iter_files(root):
            if path.suffix not in TEXT_EXTENSIONS:
                continue
            text = read_text(path)
            for label, pattern in FORBIDDEN_PUBLIC_PATTERNS:
                match = pattern.search(text)
                if match:
                    errors.append(
                        f"forbidden {label} in {path.relative_to(ROOT)} near {match.group(0)!r}"
                    )
    return errors


def check_sample_artifacts() -> list[str]:
    errors: list[str] = []
    if not SAMPLES.exists():
        return errors

    for path in SAMPLES.rglob("*"):
        if any(part in FORBIDDEN_SAMPLE_PARTS for part in path.parts):
            errors.append(f"forbidden generated/cache path under Samples~: {path.relative_to(ROOT)}")
        if any(pattern.search(path.name) for pattern in FORBIDDEN_SAMPLE_NAME_PATTERNS):
            errors.append(f"forbidden generated sample artifact: {path.relative_to(ROOT)}")
    return errors


def check_package_artifacts() -> list[str]:
    errors: list[str] = []
    forbidden_dirs = {"Library", "Logs", "Temp", "obj", "bin"}
    for path in PACKAGE.rglob("*"):
        if path.is_dir() and path.name in forbidden_dirs:
            errors.append(f"forbidden build/cache directory inside package: {path.relative_to(ROOT)}")
    return errors


def check_plugin_identity() -> list[str]:
    errors: list[str] = []
    plugins = PACKAGE / "Runtime" / "Plugins"
    if not plugins.exists():
        return ["Runtime/Plugins folder is missing"]

    dll_names = {path.stem for path in plugins.glob("*.dll")}
    asmdef_names = {path.stem for path in plugins.glob("*.asmdef")}
    collisions = sorted(dll_names & asmdef_names)
    for name in collisions:
        errors.append(f"Runtime/Plugins has dll/asmdef basename collision: {name}")
    return errors


def check_third_party_notices() -> list[str]:
    errors: list[str] = []
    if not THIRD_PARTY_NOTICES.exists():
        return ["THIRD_PARTY_NOTICES.md is missing"]

    notices = read_text(THIRD_PARTY_NOTICES)
    for relative_binary, tokens in THIRD_PARTY_REQUIREMENTS.items():
        binary = PACKAGE / relative_binary
        if not binary.exists():
            errors.append(f"bundled third-party binary is missing: {relative_binary}")
            continue
        for token in tokens:
            if token not in notices:
                errors.append(
                    f"THIRD_PARTY_NOTICES.md does not mention {token!r} for {relative_binary}"
                )
    return errors


def check_doi_metadata() -> list[str]:
    errors: list[str] = []
    requirements = {
        ROOT / "README.md": (CONCEPT_DOI, VERSION_DOI),
        ROOT / "CITATION.cff": (CONCEPT_DOI,),
        ROOT / "docs" / "releases" / "RELEASE_NOTES_v0.4.0.md": (CONCEPT_DOI, VERSION_DOI),
        PACKAGE / "RELEASE_NOTES.md": (CONCEPT_DOI, VERSION_DOI),
    }

    for path, tokens in requirements.items():
        if not path.exists():
            errors.append(f"DOI metadata file is missing: {path.relative_to(ROOT)}")
            continue
        text = read_text(path)
        for token in tokens:
            if token not in text:
                errors.append(f"DOI metadata missing {token!r} in {path.relative_to(ROOT)}")
    return errors


def main() -> int:
    errors = validate()
    if errors:
        print("[FAIL] Unity2Rerun release package validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("[PASS] Unity2Rerun release package validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
# Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
# SPDX-License-Identifier: Apache-2.0
#
# Purpose: Validate the documented Unity2Rerun coverage of official Rerun SDK types.

"""Check the release-facing Rerun type coverage matrix."""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
RERUN_DEFINITIONS = (
    ROOT
    / "third-party"
    / "rerun"
    / "crates"
    / "store"
    / "re_sdk_types"
    / "definitions"
    / "rerun"
)
ARCHETYPE_DEFINITIONS = RERUN_DEFINITIONS / "archetypes"
COMPONENT_DEFINITIONS = RERUN_DEFINITIONS / "components"
EXCLUDED_DEFINITION_DIRS = ("blueprint", "datatypes", "testing")
ENCODER = (
    ROOT
    / "Packages"
    / "dev.unity2rerun.sdk"
    / "Runtime"
    / "Encoding"
    / "RerunArrowIpcEncoder.cs"
)
MATRIX = ROOT / "docs" / "releases" / "RERUN_TYPE_COVERAGE_MATRIX.md"

STATUS_VALUES = {"Done", "Partial", "Future", "Not Unity-scope"}
DONE_EVIDENCE_PLACEHOLDERS = {"", "none", "n/a", "no encoder output"}
TYPE_DECLARATION = re.compile(
    r"^\s*(?:table|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\b",
    re.MULTILINE,
)
ARCHETYPE_EMIT = re.compile(r"rerun\.archetypes\.([A-Za-z_][A-Za-z0-9_]*)")
COMPONENT_EMIT = re.compile(r"rerun\.components\.([A-Za-z_][A-Za-z0-9_]*)")


@dataclass(frozen=True)
class OfficialType:
    key: str
    name: str
    stem: str
    path: Path


@dataclass(frozen=True)
class MatrixRow:
    key: str
    name: str
    status: str
    evidence: str
    cells: tuple[str, ...]


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8-sig")


def normalize_name(name: str) -> str:
    return re.sub(r"[^A-Za-z0-9]", "", name).lower()


def display_name_from_fbs(path: Path) -> str:
    text = read_text(path)
    match = TYPE_DECLARATION.search(text)
    if match:
        return match.group(1)
    parts = path.stem.split("_")
    return "".join(part[:1].upper() + part[1:] for part in parts)


def list_official_types(path: Path) -> dict[str, OfficialType]:
    types: dict[str, OfficialType] = {}
    for fbs in sorted(path.glob("*.fbs")):
        official = OfficialType(
            key=normalize_name(fbs.stem),
            name=display_name_from_fbs(fbs),
            stem=fbs.stem,
            path=fbs,
        )
        if official.key in types:
            raise ValueError(
                f"duplicate official type key {official.key!r}: "
                f"{types[official.key].path} and {fbs}"
            )
        types[official.key] = official
    return types


def emitted_names(pattern: re.Pattern[str], text: str) -> dict[str, str]:
    names: dict[str, str] = {}
    for match in pattern.finditer(text):
        name = match.group(1)
        names.setdefault(normalize_name(name), name)
    return dict(sorted(names.items(), key=lambda item: item[1].lower()))


def split_markdown_row(line: str) -> tuple[str, ...]:
    cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
    return tuple(cell.strip("`").strip() for cell in cells)


def is_separator_row(cells: tuple[str, ...]) -> bool:
    return all(re.fullmatch(r":?-{3,}:?", cell.strip()) for cell in cells)


def table_lines_after_heading(text: str, heading: str) -> list[str]:
    lines = text.splitlines()
    start = None
    for index, line in enumerate(lines):
        if line.strip() == heading:
            start = index + 1
            break
    if start is None:
        raise ValueError(f"missing heading: {heading}")

    table: list[str] = []
    for line in lines[start:]:
        stripped = line.strip()
        if stripped.startswith("## ") and table:
            break
        if stripped.startswith("|"):
            table.append(line)
        elif table and stripped:
            break
    if not table:
        raise ValueError(f"missing table under heading: {heading}")
    return table


def parse_matrix_table(text: str, heading: str) -> tuple[tuple[str, ...], list[MatrixRow], list[str]]:
    table = table_lines_after_heading(text, heading)
    header = split_markdown_row(table[0])
    rows: list[MatrixRow] = []
    errors: list[str] = []
    seen: dict[str, str] = {}
    status_index = header.index("Status") if "Status" in header else 1
    evidence_index = header.index("Unity2Rerun evidence") if "Unity2Rerun evidence" in header else 1

    for line in table[1:]:
        cells = split_markdown_row(line)
        if not cells or is_separator_row(cells):
            continue
        if len(cells) <= max(status_index, evidence_index):
            errors.append(f"{heading} row has too few cells: {line}")
            continue
        name = cells[0]
        status = cells[status_index]
        evidence = cells[evidence_index]
        key = normalize_name(name)
        if key in seen:
            errors.append(f"{heading} duplicates row for {name!r} and {seen[key]!r}")
        seen[key] = name
        rows.append(MatrixRow(key=key, name=name, status=status, evidence=evidence, cells=cells))

    return header, rows, errors


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def validate_matrix(write_report: Path | None = None) -> int:
    errors: list[str] = []
    if write_report is not None and not write_report.is_absolute():
        write_report = ROOT / write_report

    official_archetypes = list_official_types(ARCHETYPE_DEFINITIONS)
    official_components = list_official_types(COMPONENT_DEFINITIONS)
    encoder_text = read_text(ENCODER)
    emitted_archetypes = emitted_names(ARCHETYPE_EMIT, encoder_text)
    emitted_components = emitted_names(COMPONENT_EMIT, encoder_text)
    matrix_text = read_text(MATRIX)

    archetype_header, archetype_rows, row_errors = parse_matrix_table(
        matrix_text, "## Archetype Matrix"
    )
    errors.extend(row_errors)
    component_header, component_rows, row_errors = parse_matrix_table(
        matrix_text, "## Component Coverage Summary"
    )
    errors.extend(row_errors)

    expected_archetype_header = (
        "Official archetype",
        "Status",
        "Unity2Rerun evidence",
        "User surface",
        "Next decision",
    )
    if archetype_header != expected_archetype_header:
        errors.append(
            "Archetype Matrix header is "
            f"{archetype_header!r}; expected {expected_archetype_header!r}"
        )

    expected_component_header = ("Component", "Used By", "Status", "Note")
    if component_header != expected_component_header:
        errors.append(
            "Component Coverage Summary header is "
            f"{component_header!r}; expected {expected_component_header!r}"
        )

    archetype_row_by_key = {row.key: row for row in archetype_rows}
    component_row_by_key = {row.key: row for row in component_rows}

    missing_archetypes = sorted(set(official_archetypes) - set(archetype_row_by_key))
    extra_archetypes = sorted(set(archetype_row_by_key) - set(official_archetypes))
    if missing_archetypes:
        errors.append("matrix is missing archetypes: " + ", ".join(missing_archetypes))
    if extra_archetypes:
        errors.append("matrix has non-runtime archetype rows: " + ", ".join(extra_archetypes))

    for row in archetype_rows:
        if row.status not in STATUS_VALUES:
            errors.append(f"archetype {row.name} has invalid status {row.status!r}")
        if row.status == "Done" and row.evidence.strip().lower() in DONE_EVIDENCE_PLACEHOLDERS:
            errors.append(f"archetype {row.name} is Done without concrete evidence")

    for key, name in emitted_archetypes.items():
        row = archetype_row_by_key.get(key)
        if row is None:
            errors.append(f"emitted archetype {name} has no matrix row")
        elif row.status not in {"Done", "Partial"}:
            errors.append(f"emitted archetype {name} has status {row.status}; expected Done or Partial")

    for row in component_rows:
        if row.status not in STATUS_VALUES:
            errors.append(f"component {row.name} has invalid status {row.status!r}")

    for key, name in emitted_components.items():
        row = component_row_by_key.get(key)
        if row is None:
            errors.append(f"emitted component {name} has no component summary row")
        elif row.status not in {"Done", "Partial"}:
            errors.append(f"emitted component {name} has status {row.status}; expected Done or Partial")

    excluded_counts = {
        dirname: len(list((RERUN_DEFINITIONS / dirname).glob("*.fbs")))
        for dirname in EXCLUDED_DEFINITION_DIRS
        if (RERUN_DEFINITIONS / dirname).exists()
    }

    report = "\n".join(
        [
            "# Generated Rerun Type Coverage Scan",
            "",
            f"Official runtime archetypes: {len(official_archetypes)}",
            f"Official runtime components: {len(official_components)}",
            f"Emitted archetypes: {len(emitted_archetypes)}",
            f"Emitted components: {len(emitted_components)}",
            "Excluded definition directories: "
            + ", ".join(f"{name}={count}" for name, count in sorted(excluded_counts.items())),
            "",
            "## Emitted Archetypes",
            *[f"- {name}" for name in emitted_archetypes.values()],
            "",
            "## Emitted Components",
            *[f"- {name}" for name in emitted_components.values()],
            "",
            "## Source Files",
            f"- Official definitions: {relative(RERUN_DEFINITIONS)}",
            f"- Encoder scan: {relative(ENCODER)}",
            f"- Matrix: {relative(MATRIX)}",
            "",
        ]
    )

    if write_report is not None:
        write_report.parent.mkdir(parents=True, exist_ok=True)
        write_report.write_text(report, encoding="utf-8", newline="\n")

    if errors:
        print("[FAIL] Rerun type coverage matrix drift detected.")
        for error in errors:
            print(f"- {error}")
        return 1

    done_count = sum(1 for row in archetype_rows if row.status == "Done")
    partial_count = sum(1 for row in archetype_rows if row.status == "Partial")
    future_count = sum(1 for row in archetype_rows if row.status == "Future")
    non_scope_count = sum(1 for row in archetype_rows if row.status == "Not Unity-scope")

    print("[PASS] Rerun type coverage matrix is current.")
    print(f"Official runtime archetypes: {len(official_archetypes)}")
    print(f"Official runtime components: {len(official_components)}")
    print(f"Emitted archetypes: {len(emitted_archetypes)}")
    print(f"Emitted components: {len(emitted_components)}")
    print(
        "Archetype status rows: "
        f"Done={done_count}, Partial={partial_count}, "
        f"Future={future_count}, Not Unity-scope={non_scope_count}"
    )
    if write_report is not None:
        print(f"Report written: {relative(write_report)}")
    return 0


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--write",
        type=Path,
        help="optional generated scan report path, usually under build/reports",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    return validate_matrix(args.write)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

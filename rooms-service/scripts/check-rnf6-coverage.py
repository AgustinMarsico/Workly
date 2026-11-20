#!/usr/bin/env python3
from __future__ import annotations

import glob
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path


TARGET_FILES = [
    "RoomsController.cs",
    "RoomService.cs",
    "RoomRepository.cs",
    "ReservationServiceAuthContext.cs",
    "RoomTypeFactory.cs",
    "RoomType.cs",
    "ApiErrors.cs",
    "CustomException.cs",
]


@dataclass
class FileCoverage:
    lines: dict[int, int] = field(default_factory=dict)
    branches: dict[tuple[int, int], int] = field(default_factory=dict)

    def add_line(self, number: int, hits: int) -> None:
        self.lines[number] = max(self.lines.get(number, 0), hits)

    def add_branch(self, line_number: int, condition_number: int, covered: bool) -> None:
        key = (line_number, condition_number)
        self.branches[key] = max(self.branches.get(key, 0), 1 if covered else 0)

    @property
    def covered_lines(self) -> int:
        return sum(1 for hits in self.lines.values() if hits > 0)

    @property
    def total_lines(self) -> int:
        return len(self.lines)

    @property
    def covered_branches(self) -> int:
        return sum(self.branches.values())

    @property
    def total_branches(self) -> int:
        return len(self.branches)


def find_reports(args: list[str]) -> list[str]:
    if args:
        reports: list[str] = []
        for arg in args:
            matches = glob.glob(arg, recursive=True)
            reports.extend(matches or [arg])
        return sorted(set(reports))

    return sorted(glob.glob("TestResults/**/coverage.cobertura.xml", recursive=True))


def parse_condition_coverage(value: str) -> tuple[int, int] | None:
    if "(" not in value or "/" not in value:
        return None

    fraction = value.split("(", 1)[1].split(")", 1)[0]
    covered, total = fraction.split("/", 1)
    return int(covered), int(total)


def collect_coverage(report_paths: list[str]) -> dict[str, FileCoverage]:
    coverage: dict[str, FileCoverage] = {}

    for report_path in report_paths:
        path = Path(report_path)
        if not path.exists():
            raise FileNotFoundError(f"Coverage report not found: {report_path}")

        root = ET.parse(path).getroot()
        for class_element in root.findall(".//class"):
            filename = class_element.attrib.get("filename", "")
            basename = Path(filename).name
            if basename not in TARGET_FILES:
                continue

            file_coverage = coverage.setdefault(basename, FileCoverage())
            for line_element in class_element.findall("./lines/line"):
                line_number = int(line_element.attrib["number"])
                hits = int(line_element.attrib.get("hits", "0"))
                file_coverage.add_line(line_number, hits)

                if line_element.attrib.get("branch") != "True":
                    continue

                parsed = parse_condition_coverage(line_element.attrib.get("condition-coverage", ""))
                if parsed is None:
                    continue

                covered, total = parsed
                for condition_number in range(total):
                    file_coverage.add_branch(line_number, condition_number, condition_number < covered)

    return coverage


def percent(covered: int, total: int) -> float:
    if total == 0:
        return 100.0

    return covered * 100.0 / total


def main() -> int:
    reports = find_reports(sys.argv[1:])
    if not reports:
        print("No coverage.cobertura.xml files found under TestResults/.", file=sys.stderr)
        print("Run dotnet test with --collect:\"XPlat Code Coverage\" first.", file=sys.stderr)
        return 1

    coverage = collect_coverage(reports)
    failed = False

    print("RNF 6 coverage gate")
    print("Reports:")
    for report in reports:
        print(f"  {report}")
    print()

    for target in TARGET_FILES:
        file_coverage = coverage.get(target)
        if file_coverage is None:
            print(f"FAIL {target}: missing from coverage reports")
            failed = True
            continue

        line_percentage = percent(file_coverage.covered_lines, file_coverage.total_lines)
        branch_percentage = percent(file_coverage.covered_branches, file_coverage.total_branches)

        branch_text = "branches: n/a"
        if file_coverage.total_branches > 0:
            branch_text = (
                f"branches: {file_coverage.covered_branches}/{file_coverage.total_branches} "
                f"({branch_percentage:.2f}%)"
            )

        print(
            f"{target}: lines {file_coverage.covered_lines}/{file_coverage.total_lines} "
            f"({line_percentage:.2f}%), {branch_text}"
        )

        if file_coverage.covered_lines != file_coverage.total_lines:
            failed = True
        if file_coverage.total_branches > 0 and file_coverage.covered_branches != file_coverage.total_branches:
            failed = True

    if failed:
        print()
        print("RNF 6 coverage gate failed.", file=sys.stderr)
        return 1

    print()
    print("RNF 6 coverage gate passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

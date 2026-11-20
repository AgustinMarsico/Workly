#!/usr/bin/env python3
import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


TARGET_FILES = [
    "AuditRecordsController.cs",
    "AuditEventService.cs",
    "AuditRecord.cs",
    "AuditActionTypes.cs",
    "ClaimsPrincipalExtensions.cs",
    "AuditRecordResponse.cs",
]


def find_coverage_file(explicit_path):
    if explicit_path:
        path = Path(explicit_path)
        if path.is_file():
            return path
        raise FileNotFoundError(f"Coverage file not found: {path}")

    matches = list(Path("TestResults").glob("**/coverage.cobertura.xml"))
    if not matches:
        matches = list(Path.cwd().glob("**/TestResults/**/coverage.cobertura.xml"))
    if not matches:
        raise FileNotFoundError("coverage.cobertura.xml not found under TestResults")

    return max(matches, key=lambda path: path.stat().st_mtime)


def local_name(tag):
    return tag.rsplit("}", 1)[-1]


def file_name(path):
    return Path(path.replace("\\", "/")).name


def parse_condition_coverage(value):
    if not value:
        return None

    match = re.search(r"\((\d+)\s*/\s*(\d+)\)", value)
    if not match:
        return None

    return int(match.group(1)), int(match.group(2))


def collect_file_coverage(xml_path):
    tree = ET.parse(xml_path)
    coverage = {
        target: {
            "lines": {},
            "branches": {},
            "found": False,
        }
        for target in TARGET_FILES
    }

    for class_element in tree.iter():
        if local_name(class_element.tag) != "class":
            continue

        target = file_name(class_element.attrib.get("filename", ""))
        if target not in coverage:
            continue

        coverage[target]["found"] = True

        for line_element in class_element.iter():
            if local_name(line_element.tag) != "line":
                continue

            number = line_element.attrib.get("number")
            if number is None:
                continue

            hits = int(line_element.attrib.get("hits", "0"))
            coverage[target]["lines"][number] = max(
                coverage[target]["lines"].get(number, 0),
                hits,
            )

            branch_data = parse_condition_coverage(line_element.attrib.get("condition-coverage"))
            if branch_data is not None:
                previous = coverage[target]["branches"].get(number, (0, 0))
                coverage[target]["branches"][number] = max(previous, branch_data, key=lambda item: (item[1], item[0]))

    return coverage


def percentage(covered, total):
    if total == 0:
        return "N/A"
    return f"{(covered / total) * 100:.2f}%"


def main():
    parser = argparse.ArgumentParser(description="Validate RNF 6 coverage for selected audit-service files.")
    parser.add_argument("coverage_xml", nargs="?", help="Path to coverage.cobertura.xml")
    args = parser.parse_args()

    try:
        coverage_file = find_coverage_file(args.coverage_xml)
        coverage = collect_file_coverage(coverage_file)
    except Exception as error:
        print(f"RNF 6 coverage gate failed: {error}", file=sys.stderr)
        return 1

    print(f"RNF 6 coverage gate: {coverage_file}")
    failed = False

    for target in TARGET_FILES:
        data = coverage[target]
        total_lines = len(data["lines"])
        covered_lines = sum(1 for hits in data["lines"].values() if hits > 0)
        total_branches = sum(total for _, total in data["branches"].values())
        covered_branches = sum(covered for covered, _ in data["branches"].values())

        print(
            f"{target}: "
            f"lines {covered_lines}/{total_lines} ({percentage(covered_lines, total_lines)}), "
            f"branches {covered_branches}/{total_branches} ({percentage(covered_branches, total_branches)})"
        )

        if not data["found"]:
            print(f"  ERROR: target file was not found in coverage report")
            failed = True
        if total_lines == 0 or covered_lines != total_lines:
            print(f"  ERROR: line coverage must be 100% for RNF 6 target file")
            failed = True
        if total_branches > 0 and covered_branches != total_branches:
            print(f"  ERROR: branch coverage must be 100% for RNF 6 target file")
            failed = True

    if failed:
        print("RNF 6 coverage gate failed")
        return 1

    print("RNF 6 coverage gate passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())

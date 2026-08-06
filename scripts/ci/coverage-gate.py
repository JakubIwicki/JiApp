#!/usr/bin/env python3
"""CI coverage gate - fails when combined line coverage drops below the floor.

Sums `lines-covered` / `lines-valid` across every coverage.cobertura.xml under
the repository root and exits non-zero when the resulting line coverage
percentage is below FLOOR. The floor is a regression guard (baseline minus
~12 points), not a quality target: healthy development can move freely, but a
sustained drop in coverage fails CI.

Overrides:
    --floor PERCENT      minimum accepted line coverage; takes precedence
    COVERAGE_FLOOR       same, via environment variable
"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

FLOOR = 60  # backend baseline (73.8%) minus ~14; regression guard, not a target
REPO_ROOT = Path(__file__).resolve().parents[2]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--floor", type=float, help="minimum accepted line coverage percent (0-100)"
    )
    return parser.parse_args()


def resolve_floor(args: argparse.Namespace) -> float:
    if args.floor is not None:
        return args.floor
    env = os.environ.get("COVERAGE_FLOOR")
    if env is not None:
        return float(env)
    return float(FLOOR)


def sum_coverage(root: Path) -> tuple[int, int]:
    """Return (lines-covered, lines-valid) summed over all cobertura files."""
    covered = 0
    valid = 0
    for report in sorted(root.rglob("coverage.cobertura.xml")):
        try:
            coverage = ET.parse(report).getroot()
        except ET.ParseError:
            print(f"COVERAGE GATE: malformed coverage report: {report}", file=sys.stderr)
            raise
        covered += int(coverage.get("lines-covered", 0) or 0)
        valid += int(coverage.get("lines-valid", 0) or 0)
    return covered, valid


def main() -> int:
    floor = resolve_floor(parse_args())
    covered, valid = sum_coverage(REPO_ROOT)
    if valid == 0:
        print(
            f"COVERAGE GATE FAILED: no line coverage data found under {REPO_ROOT}",
            file=sys.stderr,
        )
        return 1
    percent = 100.0 * covered / valid
    print(f"Line coverage: {covered}/{valid} lines ({percent:.1f}%)")
    if percent < floor:
        print(
            f"COVERAGE GATE FAILED: {percent:.1f}% below floor {floor:.1f}% - "
            "new code is not being covered by tests.",
            file=sys.stderr,
        )
        return 1
    print(f"COVERAGE GATE PASSED: {percent:.1f}% >= floor {floor:.1f}%")
    return 0


if __name__ == "__main__":
    sys.exit(main())

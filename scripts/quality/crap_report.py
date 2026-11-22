"""CRAP (Change Risk Anti-Patterns) gate computed from OpenCover coverage reports.

CRAP(m) = CC(m)^2 * (1 - coverage(m))^3 + CC(m), where CC is cyclomatic complexity and coverage is the
sequence coverage ratio. Rule from AGENTS.md ub-mode: every method must stay below the threshold (10).

Pre-existing offenders are recorded in a baseline file (ratchet): they are reported but do not fail the
gate; any method that is new or whose score got worse than its baseline entry fails it.

Usage:
  python crap_report.py "<glob>" [--threshold 10] [--baseline FILE] [--update-baseline]
"""
import argparse
import glob
import json
import pathlib
import sys
import xml.etree.ElementTree as ET

TEST_MODULE_MARKERS = (".Tests", ".UnitTests", ".IntegrationTests", ".ArchTests")


def crap(complexity, coverage_ratio):
    return complexity ** 2 * (1 - coverage_ratio) ** 3 + complexity


def collect_methods(report_paths):
    """Merge reports: same method across test projects keeps its best coverage."""
    methods = {}
    for path in report_paths:
        for module in ET.parse(path).getroot().iter("Module"):
            module_name = (module.findtext("ModuleName") or "").strip()
            if module_name.endswith(TEST_MODULE_MARKERS) or any(m in module_name for m in TEST_MODULE_MARKERS):
                continue
            for klass in module.iter("Class"):
                class_name = (klass.findtext("FullName") or "").strip()
                for method in klass.iter("Method"):
                    if method.get("isGetter") == "true" or method.get("isSetter") == "true":
                        continue
                    name = (method.findtext("Name") or "").strip()
                    complexity = int(method.get("cyclomaticComplexity", "1"))
                    coverage = float(method.get("sequenceCoverage", "0")) / 100
                    key = f"{class_name}::{name}"
                    previous = methods.get(key)
                    if previous is None or coverage > previous[1]:
                        methods[key] = (complexity, coverage)
    return methods


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("report_glob")
    parser.add_argument("--threshold", type=float, default=10.0)
    parser.add_argument("--baseline", default="scripts/quality/crap-baseline.json")
    parser.add_argument("--update-baseline", action="store_true")
    args = parser.parse_args()

    reports = sorted(glob.glob(args.report_glob, recursive=True))
    if not reports:
        print(f"CRAP gate: no coverage reports match '{args.report_glob}'. NOT RUN.")
        return 2

    scores = {key: crap(cc, cov) for key, (cc, cov) in collect_methods(reports).items()}
    offenders = {key: round(score, 1) for key, score in scores.items() if score >= args.threshold}

    baseline_path = pathlib.Path(args.baseline)
    if args.update_baseline:
        baseline_path.write_text(json.dumps(dict(sorted(offenders.items())), indent=2) + "\n", encoding="utf-8")
        print(f"Baseline written with {len(offenders)} method(s): {baseline_path}")
        return 0

    baseline = json.loads(baseline_path.read_text(encoding="utf-8")) if baseline_path.exists() else {}
    regressions = {k: v for k, v in offenders.items() if v > baseline.get(k, args.threshold - 0.001) + 0.05}

    print(f"CRAP gate: {len(scores)} methods analyzed, {len(offenders)} at or above {args.threshold:g}, "
          f"{len(offenders) - len(regressions)} accepted by baseline.")
    for key, value in sorted(regressions.items(), key=lambda item: -item[1])[:25]:
        print(f"  CRAP {value:>7}  {key}")
    if regressions:
        print("FAIL: add tests or reduce complexity. Never raise the baseline to hide a regression.")
        return 1
    print("[OK] No CRAP regressions.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

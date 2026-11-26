"""Machine-verifies two testability rules that were previously only prose (AGENTS.md section 5, rule 7).

1. Ambient clock: `DateTime.Now/UtcNow/Today` and `DateTimeOffset.Now/UtcNow` are forbidden in Domain and
   Application code. Inject `TimeProvider` instead (use `FakeTimeProvider` in tests). Infrastructure adapters at
   the edge may read the system clock.
2. Fixed waits in tests: `Task.Delay(...)` and `Thread.Sleep(...)` under src/backend/tests make tests slow and
   flaky. Existing waits are recorded in scripts/quality/test-hygiene-baseline.json (a ratchet): a file may keep
   at most its baseline count, never more. Lower or delete entries as waits are replaced by polling helpers.

Run: python -I scripts/quality/check_test_hygiene.py
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
BASELINE_FILE = ROOT / "scripts" / "quality" / "test-hygiene-baseline.json"

CLOCK_DIRS = ("src/backend/src/App.Domain", "src/backend/src/App.Application")
TEST_DIR = "src/backend/tests"
IGNORED_PARTS = {"obj", "bin", "TestResults", "StrykerOutput"}

AMBIENT_CLOCK = re.compile(r"\bDateTime(Offset)?\s*\.\s*(UtcNow|Now|Today)\b")
FIXED_WAIT = re.compile(r"\b(Task\s*\.\s*Delay|Thread\s*\.\s*Sleep)\s*\(")


def _source_files(root, relative_dir):
    base = root / relative_dir
    if not base.exists():
        return
    for path in sorted(base.rglob("*.cs")):
        if IGNORED_PARTS.intersection(path.relative_to(root).parts):
            continue
        yield path


def _relative(root, path):
    return path.relative_to(root).as_posix()


def scan(root, baseline):
    """Return a list of human-readable violations for the repository rooted at `root`."""
    violations = []

    for clock_dir in CLOCK_DIRS:
        for path in _source_files(root, clock_dir):
            for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
                if AMBIENT_CLOCK.search(line):
                    violations.append(
                        f"{_relative(root, path)}:{number}: ambient system clock in Domain/Application code; "
                        "inject TimeProvider instead."
                    )

    for path in _source_files(root, TEST_DIR):
        relative = _relative(root, path)
        count = len(FIXED_WAIT.findall(path.read_text(encoding="utf-8")))
        allowed = baseline.get(relative, 0)
        if count > allowed:
            violations.append(
                f"{relative}: {count} fixed wait(s) (Task.Delay/Thread.Sleep) but baseline allows {allowed}; "
                "poll a condition with a timeout instead of sleeping."
            )

    return violations


def main():
    baseline = json.loads(BASELINE_FILE.read_text(encoding="utf-8")) if BASELINE_FILE.exists() else {}
    violations = scan(ROOT, baseline)
    if violations:
        print("Test hygiene gate FAILED:")
        for violation in violations:
            print(f"  - {violation}")
        return 1
    print("Test hygiene gate passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

"""Fails when Portuguese text appears in repository artifacts (AGENTS.md section 1, rule 6).

Scans git-tracked and untracked-but-not-ignored text files. Detection is intentionally simple and
deterministic: Portuguese diacritics plus a short list of unambiguous Portuguese stopwords.

Allowed exceptions live in scripts/quality/english-allowlist.txt, one entry per line:
  <path-glob>              exempt a whole file
  <path-glob>::<substring> exempt only lines containing the substring (legal terms of art, etc.)
"""
import fnmatch
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
ALLOWLIST = ROOT / "scripts" / "quality" / "english-allowlist.txt"

SKIPPED_PATHS = re.compile(
    r"(^|/)(package-lock\.json|skills-lock\.json|LICENSE|node_modules|\.agents/skills/supabase[^/]*)(/|$)"
    r"|\.(png|jpg|jpeg|gif|ico|svg|lock|woff2?|pdf)$"
)
DIACRITICS = re.compile("[áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ]")
STOPWORDS = re.compile(r"\b(não|você|também|então|são|está|será|isso|sobre|nunca commite)\b", re.IGNORECASE)


def load_allowlist():
    entries = []
    if not ALLOWLIST.exists():
        return entries
    for raw in ALLOWLIST.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        glob, _, needle = line.partition("::")
        entries.append((glob.strip(), needle.strip()))
    return entries


def is_allowed(path, line, allowlist):
    for glob, needle in allowlist:
        if fnmatch.fnmatch(path, glob) and (not needle or needle in line):
            return True
    return False


def candidate_files():
    out = subprocess.run(
        ["git", "ls-files", "-c", "-o", "--exclude-standard"],
        cwd=ROOT, capture_output=True, text=True, encoding="utf-8", check=True,
    ).stdout
    return [p for p in out.splitlines() if p and not SKIPPED_PATHS.search(p)]


def main():
    allowlist = load_allowlist()
    violations = []
    for rel in candidate_files():
        try:
            text = (ROOT / rel).read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            violations.append((rel, 0, "file is not valid UTF-8 (possible mojibake from non-English text)"))
            continue
        for number, line in enumerate(text.splitlines(), 1):
            if (DIACRITICS.search(line) or STOPWORDS.search(line)) and not is_allowed(rel, line, allowlist):
                violations.append((rel, number, line.strip()[:100]))

    for path, number, snippet in violations:
        print(f"{path}:{number}: {snippet}")
    if violations:
        print(f"\n{len(violations)} possible non-English line(s). Translate them or add a justified entry to {ALLOWLIST.relative_to(ROOT)}.")
        return 1
    print("[OK] No Portuguese text found in repository artifacts.")
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())

"""Behavioral tests for scripts/quality/check_test_hygiene.py (run: python -I -m unittest discover scripts/quality/tests)."""
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))

import check_test_hygiene as hygiene  # noqa: E402


def write(root, relative, content):
    path = root / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


class AmbientClockRuleTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self._tmp.name)

    def tearDown(self):
        self._tmp.cleanup()

    def test_ambient_clock_in_application_code_is_reported_with_location(self):
        write(self.root, "src/backend/src/App.Application/Handler.cs", "var now = DateTimeOffset.UtcNow;\n")

        violations = hygiene.scan(self.root, baseline={})

        self.assertEqual(1, len(violations))
        self.assertIn("src/backend/src/App.Application/Handler.cs:1", violations[0])
        self.assertIn("TimeProvider", violations[0])

    def test_ambient_clock_in_domain_code_is_reported(self):
        write(self.root, "src/backend/src/App.Domain/Entity.cs", "var today = DateTime.Today;\n")

        self.assertEqual(1, len(hygiene.scan(self.root, baseline={})))

    def test_ambient_clock_in_infrastructure_adapter_is_allowed(self):
        write(self.root, "src/backend/src/App.Infrastructure/Adapter.cs", "var now = DateTimeOffset.UtcNow;\n")

        self.assertEqual([], hygiene.scan(self.root, baseline={}))

    def test_injected_time_provider_is_not_reported(self):
        write(self.root, "src/backend/src/App.Application/Handler.cs", "var now = timeProvider.GetUtcNow();\n")

        self.assertEqual([], hygiene.scan(self.root, baseline={}))


class FixedDelayRuleTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self._tmp.name)

    def tearDown(self):
        self._tmp.cleanup()

    def test_new_fixed_delay_in_tests_is_reported(self):
        write(self.root, "src/backend/tests/App.IntegrationTests/NewTests.cs", "await Task.Delay(500);\n")

        violations = hygiene.scan(self.root, baseline={})

        self.assertEqual(1, len(violations))
        self.assertIn("poll a condition", violations[0])

    def test_thread_sleep_in_tests_is_reported(self):
        write(self.root, "src/backend/tests/App.UnitTests/SlowTests.cs", "Thread.Sleep(100);\n")

        self.assertEqual(1, len(hygiene.scan(self.root, baseline={})))

    def test_delays_up_to_the_baseline_are_tolerated(self):
        relative = "src/backend/tests/App.IntegrationTests/LegacyTests.cs"
        write(self.root, relative, "await Task.Delay(1);\nawait Task.Delay(2);\n")

        self.assertEqual([], hygiene.scan(self.root, baseline={relative: 2}))

    def test_delays_above_the_baseline_are_reported(self):
        relative = "src/backend/tests/App.IntegrationTests/LegacyTests.cs"
        write(self.root, relative, "await Task.Delay(1);\nawait Task.Delay(2);\nawait Task.Delay(3);\n")

        violations = hygiene.scan(self.root, baseline={relative: 2})

        self.assertEqual(1, len(violations))
        self.assertIn("baseline allows 2", violations[0])

    def test_generated_build_output_is_ignored(self):
        write(self.root, "src/backend/tests/App.UnitTests/obj/Generated.cs", "await Task.Delay(500);\n")

        self.assertEqual([], hygiene.scan(self.root, baseline={}))


if __name__ == "__main__":
    unittest.main()

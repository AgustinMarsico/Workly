"""Pruebas del orden de arranque y del bloqueo ante migraciones fallidas."""

import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch, MagicMock


spec = importlib.util.spec_from_file_location("up_local", Path(__file__).with_name("up-local.py"))
up_local = importlib.util.module_from_spec(spec)
spec.loader.exec_module(up_local)


class StartupTests(unittest.TestCase):
    def exercise(self, failed_migration=False):
        calls = []
        secret = "local-test-secret-never-print-this"

        def run(command, **kwargs):
            calls.append(command)
            failed = failed_migration and "run" in command and "company-migrate" in command
            return subprocess.CompletedProcess(command, 1 if failed else 0, secret if failed else "", "")

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / ".env").write_text(f"SUPER_SECRET_KEY={secret}\nCOMPOSE_PROJECT_NAME=workly\n")
            response = MagicMock()
            response.__enter__.return_value.status = 200
            output = io.StringIO()
            with (
                patch.object(up_local, "ROOT", root),
                patch("sys.argv", ["up-local.py"]),
                patch.object(up_local.subprocess, "run", side_effect=run),
                patch.object(up_local.urllib.request, "urlopen", return_value=response),
                contextlib.redirect_stdout(output),
            ):
                if failed_migration:
                    with self.assertRaises(SystemExit):
                        up_local.main()
                else:
                    up_local.main()
            return calls, output.getvalue(), secret

    def test_failed_migration_stops_startup_and_redacts_secret(self):
        calls, output, secret = self.exercise(failed_migration=True)
        self.assertNotIn(secret, output)
        self.assertIn("[REDACTED]", output)
        self.assertFalse(any("up" in call and "company-api" in call for call in calls))
        self.assertFalse(any("up" in call and "reservation-api" in call for call in calls))

    def test_all_migrations_precede_api_start(self):
        calls, output, _ = self.exercise()
        migrations = [index for index, call in enumerate(calls) if "run" in call]
        api_start = next(index for index, call in enumerate(calls) if "up" in call and "company-api" in call)
        self.assertEqual(len(migrations), 3)
        self.assertLess(max(migrations), api_start)
        self.assertIn("Workly listo", output)


if __name__ == "__main__":
    unittest.main()

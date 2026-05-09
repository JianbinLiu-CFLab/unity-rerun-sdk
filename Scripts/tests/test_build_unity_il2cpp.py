import argparse
import sys
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import build_unity_il2cpp as build


class BuildUnityIl2CppTests(unittest.TestCase):
    def test_parse_args_accepts_phase6_aliases(self):
        args = build.parse_args([
            "--unity-path", "C:/Unity/Unity.exe",
            "--project-path", "Unity2Rerun",
            "--output-path", "build/Unity/out/Unity2RerunDemo.exe",
            "--output-dir", "build/Unity/run",
            "--timeout-seconds", "42",
        ])

        self.assertEqual(args.unity, "C:/Unity/Unity.exe")
        self.assertEqual(args.project, "Unity2Rerun")
        self.assertEqual(args.output, "build/Unity/out/Unity2RerunDemo.exe")
        self.assertEqual(args.build_dir, "build/Unity/run")
        self.assertEqual(args.timeout_seconds, 42)

    def test_main_passes_timeout_to_progress_runner(self):
        args = argparse.Namespace(
            dry_run=False,
            progress_interval=15,
            timeout_seconds=123,
        )

        with mock.patch.object(build, "parse_args", return_value=args), \
             mock.patch.object(build, "build_command", return_value=(["Unity.exe"], build.repo_root(), build.repo_root() / "build.log", build.repo_root() / "out.exe")), \
             mock.patch.object(build, "run_with_progress", return_value=0) as run:
            self.assertEqual(build.main(), 0)

        self.assertEqual(run.call_args.args[4], 123)

    def test_run_with_progress_times_out_and_terminates_process(self):
        process = mock.Mock()
        process.poll.return_value = None

        with mock.patch.object(build.subprocess, "Popen", return_value=process), \
             mock.patch.object(build.time, "monotonic", side_effect=[0.0, 2.0]), \
             mock.patch.object(build.time, "sleep") as sleep, \
             mock.patch.object(build, "read_new_important_lines", return_value=(0, [])), \
             mock.patch.object(build, "terminate_process") as terminate:
            result = build.run_with_progress(
                ["Unity.exe"],
                build.repo_root(),
                build.repo_root() / "build.log",
                interval=15,
                timeout_seconds=1,
            )

        self.assertEqual(result, 124)
        terminate.assert_called_once_with(process)
        sleep.assert_not_called()


if __name__ == "__main__":
    unittest.main()

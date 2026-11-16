"""Exercise guard decisions without changing the working repository."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest


DISPATCH = Path(__file__).with_name("dispatch.py")


class GuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.repo = Path(self.temp.name)
        self.git("init", "-b", "main")
        self.git("config", "user.name", "Guard test")
        self.git("config", "user.email", "guard@example.invalid")
        (self.repo / "Widget.cs").write_text("original\n")
        (self.repo / "README.md").write_text("`Widget` is documented here.\n")
        self.git("add", ".")
        self.git("commit", "-m", "initial")
        self.git("branch", "dev")
        self.git("update-ref", "refs/remotes/origin/dev", "HEAD")

    def git(self, *args):
        return subprocess.run(["git", "-C", str(self.repo), *args], check=True,
                              capture_output=True, text=True)

    def decision(self, tool, args):
        event = {"cwd": str(self.repo), "tool_name": tool, "tool_input": args}
        result = subprocess.run(["python3", str(DISPATCH)], input=json.dumps(event),
                                text=True, capture_output=True, check=True, cwd=self.repo)
        return json.loads(result.stdout)["hookSpecificOutput"] if result.stdout.strip() else {}

    def feature(self):
        self.git("switch", "-c", "feature/example", "dev")

    def test_protected_branch_blocks_patch_and_commit(self):
        for tool, args in [("apply_patch", {"patch": "*** Begin Patch\n*** End Patch"}),
                           ("exec_command", {"cmd": "git commit -m test"})]:
            self.assertEqual(self.decision(tool, args).get("permissionDecision"), "deny")

    def test_feature_allows_patch_and_read(self):
        self.feature()
        self.assertEqual(self.decision("apply_patch", {"patch": "example"}), {})
        self.assertEqual(self.decision("exec_command", {"cmd": "git status"}), {})

    def test_branch_names_and_base(self):
        self.git("switch", "dev")
        for command in ["git switch -c fix/example", "git switch -c feature/scrum-99-example",
                        "git switch -c feature/example main"]:
            self.assertEqual(self.decision("Bash", {"command": command}).get("permissionDecision"), "deny")
        self.assertEqual(self.decision("Bash", {"command": "git switch -c feature/example"}), {})

    def test_explicit_secret_paths_and_curl(self):
        self.feature()
        for command in ["cat .env", "cat .env.local", "cat /tmp/secrets/key", "curl https://example.com"]:
            self.assertEqual(self.decision("exec_command", {"cmd": command}).get("permissionDecision"), "deny")
        self.assertEqual(self.decision("exec_command", {"cmd": "cat .env.example"}), {})

    def test_docs_review_checkpoint(self):
        self.feature()
        (self.repo / "Widget.cs").write_text("changed\n")
        self.git("add", "Widget.cs")
        command = {"cmd": "git commit -m test"}
        first = self.decision("exec_command", command)
        self.assertEqual(first.get("permissionDecision"), "deny")
        self.assertIn("README.md", first["permissionDecisionReason"])
        self.assertEqual(self.decision("exec_command", command), {})


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3
"""Normalize Codex shell payloads for the repository's branch/docs guards."""
import json
from pathlib import Path
import subprocess
import sys
import re

event = json.load(sys.stdin)
tool = event.get("tool_name", "")
args = event.get("tool_input", {})
if isinstance(args, str):
    try:
        args = json.loads(args)
    except json.JSONDecodeError:
        args = {"patch": args}
args = dict(args)
if tool in {"exec_command", "shell_command", "shell"}:
    tool = "Bash"
if tool == "Bash":
    command = args.get("command", args.get("cmd", ""))
    if isinstance(command, list):
        command = " ".join(command)
    args["command"] = command
if tool == "spawn_agent":
    tool = "Agent"
    args["subagent_type"] = args.get("agent_type", "")
event["tool_name"] = tool
event["tool_input"] = args

# Catch explicit secret paths and the project's prohibited download command.
# This is a guardrail; shell interpretation and indirect reads require sandbox policy.
payload = json.dumps(args)
secret_path = re.search(r"(?:^|[\s/\\\"'])\.env(?!\.example(?:[\s\\\"']|$))(?:\.[\w.-]+)?(?:[\s\\\"']|$)|(?:^|[\s/\\\"'])secrets/", payload)
curl_command = tool == "Bash" and re.search(r"(?:^|[\s;&|])curl(?:\s|$)", args.get("command", ""))
if secret_path or curl_command:
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "Project policy prohibits real .env/secrets access and curl. Use .env.example and documentation tools."}}))
    sys.exit(0)
root = Path(__file__).resolve().parent
for script in ["git-flow-guard.sh", "docs-sync-check.sh"]:
    result = subprocess.run(["bash", str(root / script)], input=json.dumps(event), text=True, capture_output=True)
    if result.returncode:
        print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "Repository guard failed: " + script}}))
        break
    if result.stdout.strip():
        print(result.stdout.strip())
        break

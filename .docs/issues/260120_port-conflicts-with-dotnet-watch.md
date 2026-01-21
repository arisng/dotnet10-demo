---
date: 2026-01-20
type: Retrospective
severity: N/A
status: Documented
---

# Lesson: Port Conflicts with dotnet-watch in Development

## Context
When running `dotnet watch run` for the demo4 application, the process failed to start with "Failed to bind to address https://[::]:7210: address already in use". This occurred after a previous run was interrupted or crashed, leaving the port bound by a lingering dotnet-watch process.

## What Went Well
- Quickly identified the root cause using `ss -tlnp | grep 7210` to check for processes on the port.
- Located the dotnet-watch PID via `ps aux | grep dotnet`.
- Successfully killed the process and restarted the application.

## What Didn't Go Well
- Initial `ss` check didn't show the process immediately, but killing the known dotnet-watch PID resolved the issue.
- The error message was clear, but required manual investigation to find the culprit.

## Key Lessons Learned
- When encountering "address already in use" errors, always check for lingering processes on the specified port.
- Use `ss -tlnp | grep <port>` or `netstat -tlnp | grep <port>` to identify the PID of the process holding the port.
- `dotnet watch` can leave ports bound if not properly shut down, especially in development environments.
- Killing the dotnet-watch process (PID) frees the port without needing to restart the system.

## Actions Taken
- Terminated the lingering dotnet-watch process (PID 141599).
- Restarted the demo4 application successfully.

## Future Prevention / Improvements
- [ ] Consider using unique ports for each demo to avoid conflicts (e.g., demo1: 7010, demo2: 7020, etc.).
- [ ] Add a cleanup script to kill any dotnet processes on common dev ports before starting.
- [ ] Document port usage conventions in the workshop README.
- [ ] Use `dotnet watch` with `--no-hot-reload` or similar options if available to reduce lingering processes.
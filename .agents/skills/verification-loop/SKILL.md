---
name: verification-loop
description: Run build, test, diff, and security checks before closing agent-platform work.
origin: app_tro_ly canonical catalog
---

# Verification Loop

Run build, test, diff, and security checks before closing agent-platform work.

## Triggers

- after a significant code change
- before updating task status to done
- after regenerating harness surfaces

## Checklist

- run the relevant tests
- check generated surfaces for drift
- record concrete evidence

## Outputs

- verification report
- blocking issues
- ready or not-ready call

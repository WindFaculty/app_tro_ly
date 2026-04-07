---
name: tdd-workflow
description: Add tests before modifying critical lifecycle, export, or verification behavior.
origin: app_tro_ly canonical catalog
---

# TDD Workflow

Add tests before modifying critical lifecycle, export, or verification behavior.

## Triggers

- changing orchestrator lifecycle logic
- adding export or validation code
- fixing regressions in control-plane behavior

## Checklist

- write or update focused failing tests first when practical
- keep fixes small and evidence-backed
- rerun the relevant test target after green

## Outputs

- test plan
- red to green evidence
- coverage notes

# Desktop Non-Goals

Status: Planned work  
Phase: A00 - Freeze desktop product definition  
Last updated: 2026-04-07

## Purpose

This document locks what Workstream A does not need to deliver.

Anything listed here is outside the done criteria for the standalone desktop app unless a later approved phase explicitly reopens it.

## Hard Non-Goals Before Sync

- real runtime sync between the desktop app and Unity
- embedding Unity as a required runtime dependency for desktop app completion
- using Unity as the main shell UI
- adding new business UI screens, panels, overlays, or workflow controls to Unity
- mini assistant mode before the sync workstream
- deep 3D object interaction before the sync workstream

## Product Non-Goals For Desktop v1

- multi-user or team collaboration
- non-Windows-first packaging or support commitments
- full offline AI inference or local model hosting as a release requirement
- wake-word or always-listening voice mode
- silent or approval-free browser automation
- support for non-Google email and calendar providers in v1
- cross-device sync
- final Unity wardrobe preview or live desktop-to-Unity outfit application
- final production room art, avatar art, or Unity presentation polish

## Architectural Non-Goals

- rewriting the existing FastAPI backend into another backend stack
- reintroducing a second competing business UI shell
- coupling desktop feature completion to optional automation subsystems
- treating typed sync contracts as sufficient reason to start S-series early

## Validation Limits

The following are expected follow-up or manual gates, not A-series done criteria by themselves:

- target-machine Google credentials and sign-in validation
- target-machine packaging and installer checks
- Unity standalone build validation
- first React or Tauri or Unity bridge smoke

Use `tasks/task-people.md` for those manual prerequisites when they become active.

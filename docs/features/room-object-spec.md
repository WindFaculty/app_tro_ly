# Room Object Spec

Updated: 2026-04-06
Status: Planned work plus current foundation notes for Character Space room-object metadata, intake, and registry rules

This document is the Phase 0 object-spec baseline for the future room-backed Character Space.

It does not describe final shipped assets or production-ready object interaction. Current implementation now includes a placeholder-safe room-object registry, factory, a richer 12-object placement pipeline for the MVP room foundation, and a first basic room interaction slice, but it still does not have prefab-backed asset intake or advanced interaction behavior in the live shell.

## Current Foundation Note

- `unity-client/Assets/Scripts/World/Objects/` now contains the first repo-side object metadata and factory pipeline.
- `RoomObjectRegistry.CreateFoundationMvp(...)` currently owns the placeholder-safe MVP object definitions used by the room foundation.
- `RoomLayoutDefinition` now carries placement config for those room objects.
- `RoomWorldController` now spawns room objects through the registry and factory instead of hardcoding the desk, sofa, shelf, plant, and lamp clusters inline.
- The current placeholder population now covers 12 runtime-spawned object instances: desk, laptop, chair, sofa, side table, shelf, books, plant, lamp, wall art, cabinet, and storage box.
- `unity-client/Assets/Scripts/World/Interaction/` now provides hover, click-select, selected-object snapshots, and basic camera focus behavior for the room objects that declare interaction capability flags.
- `RoomObjectRegistryValidator` now provides placeholder-safe versus strict-prefab validation modes, and `Tools > TroLy > Validate Room Object Registry` plus `Tools > TroLy > Validate Room Object Prefab Intake` expose that validation from the Unity Editor.

## Purpose

- define one registry-first way to add room objects
- keep prefab naming, metadata, and interaction flags consistent
- avoid scene-authoring drift when new room objects are added later

## Registry Rule

- Every room object used by Character Space should be declared through a registry entry before it is considered part of a layout.
- Layout data should reference object ids or prefab keys from the registry, not arbitrary scene references.
- The registry is the source of truth for object metadata, default scale, category, and interaction capability flags.

## Planned Categories

- `furniture`
- `decor`
- `interactive`
- `lighting`
- `utility`
- `structure`

Recommended meaning:

- `structure`: floor, walls, window frame, built-in room pieces
- `furniture`: desk, chair, bed, sofa, shelf
- `decor`: plant, painting, books, small props
- `interactive`: laptop, lamp switch, bookshelf inspect point, wardrobe point
- `lighting`: lamp fixture or other light-owned objects
- `utility`: helper props or technical objects not meant as focal decor

## Prefab Rules

- Each room object should resolve to one prefab-owned asset path or prefab key.
- Prefabs should have normalized scale and pivot before registry entry.
- Prefabs should not hide required runtime logic inside arbitrary scene-only setup.
- If an object needs colliders, highlight markers, or interactable markers, those should be part of the prefab contract or a predictable factory augmentation step.

## Metadata Schema

Each registry entry should be able to express at least:

- `id`
- `displayName`
- `category`
- `prefabKey`
- `prefabPath`
- `defaultScale`
- `anchorType`
- `interactionType`
- `selectable`
- `hoverable`
- `inspectable`
- `tags`
- `optionalStates`

Example shape:

```yaml
id: desk_main_01
displayName: Ban lam viec
category: furniture
prefabKey: OBJ_Furniture_Desk_Wood_A01
prefabPath: Assets/World/Prefabs/Furniture/OBJ_Furniture_Desk_Wood_A01.prefab
defaultScale: [1.0, 1.0, 1.0]
anchorType: desk_anchor
interactionType: inspect
selectable: true
hoverable: true
inspectable: true
tags: ["work", "desk", "main"]
optionalStates: ["clean", "active"]
```

## Naming Rules

Recommended prefab-key and prefab naming pattern:

- `OBJ_Furniture_Desk_Wood_A01`
- `OBJ_Decor_Plant_Small_A01`
- `OBJ_Interactive_Laptop_A01`
- `OBJ_Lighting_Lamp_Table_A01`

Recommended id pattern inside the registry:

- `desk_main_01`
- `plant_corner_01`
- `lamp_table_01`

Rules:

- use stable English keys for ids and prefab keys
- use display names for localized or player-facing text
- keep category visible in the prefab key
- increment variants with a suffix such as `A01`, `A02`

## Anchor Rules

- Layout placement should target named anchor types such as `desk_anchor`, `shelf_anchor`, `decor_small_anchor`, or `avatar_spawn`.
- Anchors belong to the room layout, not to the object registry.
- Objects can declare which anchor type they expect by default.
- Layout data may still override scale or rotation per placement when needed.

## Interaction Capability Flags

Every object entry should explicitly declare whether it supports:

- selection
- hover
- inspect
- focus camera
- future character intent

Recommended interaction types for the MVP:

- `none`
- `inspect`
- `focus`
- `inspect_and_focus`

## MVP Object List

The first room should stay in the 8 to 12 object range:

- floor
- walls
- window or painting
- rug
- bed or sofa
- desk
- chair
- lamp
- shelf
- laptop
- plant

Current placeholder-safe population in repo now sits at 12 runtime-spawned objects:

- desk
- laptop
- chair
- sofa
- side table
- shelf
- books
- plant
- lamp
- wall art
- cabinet
- storage box

Suggested MVP interactive targets:

- laptop
- desk
- bed or sofa
- lamp
- shelf or bookshelf

## Validation Rules For Later Phases

Before a new room object is accepted later, it should pass:

- import path and prefab path are valid
- scale is normalized
- pivot is usable for placement
- material assignment is intentional
- collider presence matches interaction flags
- registry entry exists
- docs update is included when new categories or rules are introduced

Current repo-side validation now supports two modes:

- `placeholder-safe`: allows missing prefab assets as informational fallback while primitive room-object shapes still own the live foundation
- `strict prefab intake`: treats missing prefab assets as errors before a room object can be called prefab-backed

Use [room-object-intake-checklist.md](room-object-intake-checklist.md) whenever a new room object or prefab-backed replacement enters the repo.

## Room Style Guide

Use these current guardrails to keep the MVP room coherent:

- Keep object density in the current 8 to 12 hero-object range unless the room plan expands deliberately.
- Prefer warm-neutral furniture colors, softer decor accents, and one clear highlight color per object definition.
- Keep silhouettes readable from the fixed stage camera before adding micro-detail.
- Favor anchor-driven grouping over scattered props so desk, rest, and decor zones stay legible.
- Treat extreme scale overrides as exceptions that must be justified in layout config.

## Out Of Scope For This Spec

- production art approval
- final room style guide
- advanced animation binding
- pathfinding and movement rules
- multiplayer or multi-room support

## Phase 0 Deliverables Captured Here

- object categories
- prefab and naming rules
- metadata schema
- interaction capability flags
- MVP object baseline

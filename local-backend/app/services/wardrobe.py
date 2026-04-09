from __future__ import annotations

import json
from collections import Counter
from pathlib import Path
from typing import Any

from pydantic import BaseModel, Field

from app.core.config import Settings
from app.core.logging import get_logger
from app.core.time import iso_datetime, now_local
from app.models.schemas import (
    WardrobeImportRequest,
    WardrobeItemCreateRequest,
    WardrobeItemRecord,
    WardrobeItemUpdateRequest,
    WardrobeOutfitCreateRequest,
    WardrobeOutfitRecord,
    WardrobeOutfitUpdateRequest,
    WardrobeSlotRecord,
    WardrobeSnapshotResponse,
    WardrobeSummary,
    WardrobeValidationIssue,
)

logger = get_logger("wardrobe")

REPO_ROOT = Path(__file__).resolve().parents[3]
SLOT_TAXONOMY_PATH = (
    REPO_ROOT / "ai-dev-system" / "domain" / "customization" / "contracts" / "slot-taxonomy.json"
)
ITEM_SCHEMA_PATH = (
    REPO_ROOT
    / "ai-dev-system"
    / "domain"
    / "customization"
    / "contracts"
    / "item-manifest.schema.json"
)
SAMPLE_CATALOG_PATH = (
    REPO_ROOT
    / "ai-dev-system"
    / "domain"
    / "customization"
    / "sample-data"
    / "current-item-catalog.json"
)


def _read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def _humanize_slot(key: str) -> str:
    parts = [part for part in key.replace("-", "_").split("_") if part]
    if not parts:
        return key
    return " ".join(parts).title()


def _sync_status(issues: list[WardrobeValidationIssue]) -> str:
    if any(issue.severity == "error" for issue in issues):
        return "blocked"
    if issues:
        return "needs_attention"
    return "ready"


def _issue(
    severity: str,
    code: str,
    message: str,
    *,
    slots: list[str] | None = None,
    item_ids: list[str] | None = None,
) -> WardrobeValidationIssue:
    return WardrobeValidationIssue(
        severity=severity,
        code=code,
        message=message,
        slots=slots or [],
        item_ids=item_ids or [],
    )


class _WardrobeItemDocument(WardrobeItemCreateRequest):
    created_at: str
    updated_at: str


class _WardrobeOutfitDocument(WardrobeOutfitCreateRequest):
    created_at: str
    updated_at: str


class _WardrobeRegistryDocument(BaseModel):
    version: int = 1
    updated_at: str
    items: list[_WardrobeItemDocument] = Field(default_factory=list)
    outfits: list[_WardrobeOutfitDocument] = Field(default_factory=list)


class WardrobeService:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._registry_path = settings.data_dir / "wardrobe" / "registry.json"
        self._slot_contract = self._load_slot_contract()
        self._allowed_slots = set(self._slot_contract["order"])
        self._reserved_slots = set(self._slot_contract["reserved"])
        self._allowed_body_regions, self._allowed_anchor_types = self._load_item_schema_contract()

    @property
    def registry_path(self) -> Path:
        return self._registry_path

    def get_snapshot(self) -> WardrobeSnapshotResponse:
        document = self._load_registry_document()
        return self._build_snapshot(document)

    def export_snapshot(self) -> WardrobeSnapshotResponse:
        return self.get_snapshot()

    def create_item(self, payload: WardrobeItemCreateRequest) -> WardrobeItemRecord:
        document = self._load_registry_document()
        if any(item.item_id == payload.item_id for item in document.items):
            raise ValueError(f"Wardrobe item {payload.item_id} already exists")

        item = self._item_document_from_payload(payload)
        document.items.append(item)
        document.updated_at = item.updated_at
        self._save_registry_document(document)
        return self._build_item_record(item)

    def update_item(self, item_id: str, payload: WardrobeItemUpdateRequest) -> WardrobeItemRecord:
        document = self._load_registry_document()
        for index, current in enumerate(document.items):
            if current.item_id != item_id:
                continue

            merged = current.model_dump()
            merged.update(payload.model_dump(exclude_unset=True))
            merged["item_id"] = item_id
            merged["created_at"] = current.created_at
            merged["updated_at"] = iso_datetime(now_local())
            updated = self._item_document_from_payload(_WardrobeItemDocument.model_validate(merged))
            self._assert_item_slot_change_is_safe(document, current, updated)
            document.items[index] = updated
            document.updated_at = updated.updated_at
            self._save_registry_document(document)
            return self._build_item_record(updated)
        raise LookupError(f"Wardrobe item {item_id} not found")

    def delete_item(self, item_id: str) -> WardrobeSnapshotResponse:
        document = self._load_registry_document()
        before = len(document.items)
        document.items = [item for item in document.items if item.item_id != item_id]
        if len(document.items) == before:
            raise LookupError(f"Wardrobe item {item_id} not found")

        for outfit in document.outfits:
            filtered = {
                slot: assigned_item_id
                for slot, assigned_item_id in outfit.slot_assignments.items()
                if assigned_item_id != item_id
            }
            if filtered != outfit.slot_assignments:
                outfit.slot_assignments = filtered
                outfit.updated_at = iso_datetime(now_local())

        document.updated_at = iso_datetime(now_local())
        self._save_registry_document(document)
        return self._build_snapshot(document)

    def create_outfit(self, payload: WardrobeOutfitCreateRequest) -> WardrobeOutfitRecord:
        document = self._load_registry_document()
        if any(outfit.outfit_id == payload.outfit_id for outfit in document.outfits):
            raise ValueError(f"Wardrobe outfit {payload.outfit_id} already exists")

        outfit = self._outfit_document_from_payload(payload, document.items)
        document.outfits.append(outfit)
        document.updated_at = outfit.updated_at
        self._save_registry_document(document)
        return self._build_outfit_record(outfit, document.items)

    def update_outfit(
        self,
        outfit_id: str,
        payload: WardrobeOutfitUpdateRequest,
    ) -> WardrobeOutfitRecord:
        document = self._load_registry_document()
        for index, current in enumerate(document.outfits):
            if current.outfit_id != outfit_id:
                continue

            merged = current.model_dump()
            merged.update(payload.model_dump(exclude_unset=True))
            merged["outfit_id"] = outfit_id
            merged["created_at"] = current.created_at
            merged["updated_at"] = iso_datetime(now_local())
            updated = self._outfit_document_from_payload(
                _WardrobeOutfitDocument.model_validate(merged),
                document.items,
            )
            document.outfits[index] = updated
            document.updated_at = updated.updated_at
            self._save_registry_document(document)
            return self._build_outfit_record(updated, document.items)
        raise LookupError(f"Wardrobe outfit {outfit_id} not found")

    def delete_outfit(self, outfit_id: str) -> WardrobeSnapshotResponse:
        document = self._load_registry_document()
        before = len(document.outfits)
        document.outfits = [outfit for outfit in document.outfits if outfit.outfit_id != outfit_id]
        if len(document.outfits) == before:
            raise LookupError(f"Wardrobe outfit {outfit_id} not found")
        document.updated_at = iso_datetime(now_local())
        self._save_registry_document(document)
        return self._build_snapshot(document)

    def import_snapshot(self, payload: WardrobeImportRequest) -> WardrobeSnapshotResponse:
        document = self._load_registry_document()
        if payload.mode == "replace":
            document = _WardrobeRegistryDocument(updated_at=iso_datetime(now_local()))

        item_by_id = {item.item_id: item for item in document.items}
        for item_payload in payload.items:
            current = item_by_id.get(item_payload.item_id)
            created_at = current.created_at if current else iso_datetime(now_local())
            source = item_payload.source if item_payload.source else "imported"
            item_by_id[item_payload.item_id] = self._item_document_from_payload(
                _WardrobeItemDocument.model_validate(
                    {
                        **item_payload.model_dump(),
                        "source": source,
                        "created_at": created_at,
                        "updated_at": iso_datetime(now_local()),
                    }
                )
            )

        items = sorted(item_by_id.values(), key=lambda item: item.item_id.casefold())

        outfit_by_id = {outfit.outfit_id: outfit for outfit in document.outfits}
        for outfit_payload in payload.outfits:
            current = outfit_by_id.get(outfit_payload.outfit_id)
            created_at = current.created_at if current else iso_datetime(now_local())
            source = outfit_payload.source if outfit_payload.source else "imported"
            outfit_by_id[outfit_payload.outfit_id] = self._outfit_document_from_payload(
                _WardrobeOutfitDocument.model_validate(
                    {
                        **outfit_payload.model_dump(),
                        "source": source,
                        "created_at": created_at,
                        "updated_at": iso_datetime(now_local()),
                    }
                ),
                items,
            )

        document.items = items
        document.outfits = sorted(outfit_by_id.values(), key=lambda outfit: outfit.outfit_id.casefold())
        document.updated_at = iso_datetime(now_local())
        self._save_registry_document(document)
        return self._build_snapshot(document)

    def _load_registry_document(self) -> _WardrobeRegistryDocument:
        self._registry_path.parent.mkdir(parents=True, exist_ok=True)
        if not self._registry_path.exists():
            document = self._default_registry_document()
            self._save_registry_document(document)
            return document

        try:
            payload = _read_json(self._registry_path)
            return _WardrobeRegistryDocument.model_validate(payload)
        except Exception as exc:
            logger.warning("Wardrobe registry was invalid and will be reset: %s", exc)
            quarantine_path = self._registry_path.with_name(
                f"registry.corrupt-{now_local().strftime('%Y%m%dT%H%M%S')}.json"
            )
            self._registry_path.replace(quarantine_path)
            document = self._default_registry_document()
            self._save_registry_document(document)
            return document

    def _save_registry_document(self, document: _WardrobeRegistryDocument) -> None:
        self._registry_path.parent.mkdir(parents=True, exist_ok=True)
        self._registry_path.write_text(
            json.dumps(document.model_dump(mode="json"), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _default_registry_document(self) -> _WardrobeRegistryDocument:
        now_value = iso_datetime(now_local())
        items = [
            self._item_document_from_payload(
                _WardrobeItemDocument.model_validate(
                    {
                        **item,
                        "source": item.get("source") or "unity_snapshot",
                        "created_at": now_value,
                        "updated_at": now_value,
                    }
                )
            )
            for item in self._load_sample_items()
        ]
        return _WardrobeRegistryDocument(
            version=1,
            updated_at=now_value,
            items=items,
            outfits=[],
        )

    def _item_document_from_payload(
        self,
        payload: WardrobeItemCreateRequest | _WardrobeItemDocument,
    ) -> _WardrobeItemDocument:
        material_paths = self._assert_known_slot_list(payload.material_asset_paths, field_name="material_asset_paths", allow_unknown=True)
        occupies_slots = self._assert_known_slot_list(payload.occupies_slots, field_name="occupies_slots")
        blocks_slots = self._assert_known_slot_list(payload.blocks_slots, field_name="blocks_slots")
        requires_slots = self._assert_known_slot_list(payload.requires_slots, field_name="requires_slots")
        self._assert_known_body_regions(payload.hide_body_regions)
        self._assert_known_anchor_type(payload.anchor_type)
        data = payload.model_dump()
        data["material_asset_paths"] = material_paths
        data["occupies_slots"] = occupies_slots
        data["blocks_slots"] = blocks_slots
        data["requires_slots"] = requires_slots
        data["hide_body_regions"] = payload.hide_body_regions
        if isinstance(payload, _WardrobeItemDocument):
            return _WardrobeItemDocument.model_validate(data)
        return _WardrobeItemDocument.model_validate(
            {
                **data,
                "created_at": iso_datetime(now_local()),
                "updated_at": iso_datetime(now_local()),
            }
        )

    def _outfit_document_from_payload(
        self,
        payload: WardrobeOutfitCreateRequest | _WardrobeOutfitDocument,
        items: list[_WardrobeItemDocument],
    ) -> _WardrobeOutfitDocument:
        slot_assignments = self._validate_slot_assignments(payload.slot_assignments, items)
        data = payload.model_dump()
        data["slot_assignments"] = slot_assignments
        if isinstance(payload, _WardrobeOutfitDocument):
            return _WardrobeOutfitDocument.model_validate(data)
        return _WardrobeOutfitDocument.model_validate(
            {
                **data,
                "created_at": iso_datetime(now_local()),
                "updated_at": iso_datetime(now_local()),
            }
        )

    def _assert_item_slot_change_is_safe(
        self,
        document: _WardrobeRegistryDocument,
        current: _WardrobeItemDocument,
        updated: _WardrobeItemDocument,
    ) -> None:
        if current.slot == updated.slot:
            return
        affected_outfits = [
            outfit.outfit_id
            for outfit in document.outfits
            if outfit.slot_assignments.get(current.slot) == current.item_id
            or updated.item_id in outfit.slot_assignments.values()
        ]
        if affected_outfits:
            raise ValueError(
                f"Cannot change slot for {current.item_id} while it is assigned in outfit "
                f"{', '.join(sorted(affected_outfits))}"
            )

    def _validate_slot_assignments(
        self,
        assignments: dict[str, str],
        items: list[_WardrobeItemDocument],
    ) -> dict[str, str]:
        item_lookup = {item.item_id: item for item in items}
        normalized: dict[str, str] = {}
        for slot_key, item_id in assignments.items():
            if slot_key not in self._allowed_slots:
                raise ValueError(f"Unknown wardrobe slot {slot_key}")
            item = item_lookup.get(item_id)
            if item is None:
                raise ValueError(f"Wardrobe item {item_id} was not found for slot {slot_key}")
            if item.slot != slot_key:
                raise ValueError(
                    f"Wardrobe item {item_id} belongs to slot {item.slot}, not {slot_key}"
                )
            normalized[slot_key] = item_id
        return normalized

    def _assert_known_slot_list(
        self,
        values: list[str],
        *,
        field_name: str,
        allow_unknown: bool = False,
    ) -> list[str]:
        normalized: list[str] = []
        for value in values:
            if allow_unknown:
                normalized.append(value)
                continue
            if value not in self._allowed_slots:
                raise ValueError(f"Unknown wardrobe slot {value} in {field_name}")
            normalized.append(value)
        return normalized

    def _assert_known_body_regions(self, regions: list[str]) -> None:
        unknown = sorted(set(regions) - self._allowed_body_regions)
        if unknown:
            raise ValueError(f"Unknown body region(s): {', '.join(unknown)}")

    def _assert_known_anchor_type(self, anchor_type: str) -> None:
        if anchor_type not in self._allowed_anchor_types:
            raise ValueError(f"Unknown anchor type {anchor_type}")

    def _load_slot_contract(self) -> dict[str, Any]:
        payload = _read_json(SLOT_TAXONOMY_PATH)
        slots: list[dict[str, Any]] = []
        reserved_keys: list[str] = []
        notes = payload.get("notes", [])

        for item in payload.get("slots", []):
            slots.append(
                {
                    "key": item["contract_key"],
                    "unity_enum": item["unity_enum"],
                    "reserved": False,
                    "notes": [],
                }
            )
        for item in payload.get("reserved_slots", []):
            reserved_keys.append(item["contract_key"])
            slots.append(
                {
                    "key": item["contract_key"],
                    "unity_enum": item["unity_enum"],
                    "reserved": True,
                    "notes": [],
                }
            )

        for index, note in enumerate(notes):
            if not slots:
                break
            slots[min(index, len(slots) - 1)]["notes"].append(note)

        return {
            "version": int(payload.get("version", 1)),
            "slots": slots,
            "order": [item["key"] for item in slots],
            "reserved": reserved_keys,
        }

    def _load_item_schema_contract(self) -> tuple[set[str], set[str]]:
        payload = _read_json(ITEM_SCHEMA_PATH)
        body_regions = set(payload.get("$defs", {}).get("body_region", {}).get("enum", []))
        anchor_types = set(payload.get("properties", {}).get("anchor_type", {}).get("enum", []))
        return body_regions, anchor_types

    def _load_sample_items(self) -> list[dict[str, Any]]:
        payload = _read_json(SAMPLE_CATALOG_PATH)
        return list(payload.get("items", []))

    def _build_snapshot(self, document: _WardrobeRegistryDocument) -> WardrobeSnapshotResponse:
        item_records = [self._build_item_record(item) for item in document.items]
        outfit_records = [self._build_outfit_record(outfit, document.items) for outfit in document.outfits]
        issue_counter = Counter(
            issue.severity
            for record in [*item_records, *outfit_records]
            for issue in record.validation_issues
        )
        slot_item_counts = Counter(item.slot for item in document.items)
        slot_outfit_counts = Counter(
            slot_key
            for outfit in document.outfits
            for slot_key in outfit.slot_assignments
        )

        slots = [
            WardrobeSlotRecord(
                key=slot["key"],
                unity_enum=slot["unity_enum"],
                display_name=_humanize_slot(slot["key"]),
                reserved=slot["reserved"],
                item_count=slot_item_counts.get(slot["key"], 0),
                outfit_count=slot_outfit_counts.get(slot["key"], 0),
                notes=slot.get("notes", []),
            )
            for slot in self._slot_contract["slots"]
        ]

        summary = WardrobeSummary(
            slot_count=len(slots),
            reserved_slot_count=sum(1 for slot in slots if slot.reserved),
            item_count=len(item_records),
            outfit_count=len(outfit_records),
            warning_count=issue_counter.get("warning", 0),
            error_count=issue_counter.get("error", 0),
            ready_item_count=sum(1 for item in item_records if item.sync_status == "ready"),
            ready_outfit_count=sum(1 for outfit in outfit_records if outfit.sync_status == "ready"),
        )
        return WardrobeSnapshotResponse(
            version=document.version,
            updated_at=document.updated_at,
            registry_path=str(self._registry_path),
            slot_taxonomy_version=self._slot_contract["version"],
            slots=slots,
            items=item_records,
            outfits=outfit_records,
            summary=summary,
        )

    def _build_item_record(self, item: _WardrobeItemDocument) -> WardrobeItemRecord:
        issues = self._validate_item(item)
        return WardrobeItemRecord(
            **item.model_dump(),
            validation_issues=issues,
            sync_status=_sync_status(issues),
        )

    def _build_outfit_record(
        self,
        outfit: _WardrobeOutfitDocument,
        items: list[_WardrobeItemDocument],
    ) -> WardrobeOutfitRecord:
        issues = self._validate_outfit(outfit, items)
        return WardrobeOutfitRecord(
            **outfit.model_dump(),
            validation_issues=issues,
            sync_status=_sync_status(issues),
        )

    def _validate_item(self, item: _WardrobeItemDocument) -> list[WardrobeValidationIssue]:
        issues: list[WardrobeValidationIssue] = []
        if item.slot in item.blocks_slots:
            issues.append(
                _issue(
                    "warning",
                    "blocks_own_slot",
                    f"{item.display_name} blocks its own slot {item.slot}.",
                    slots=[item.slot],
                    item_ids=[item.item_id],
                )
            )
        if item.slot in item.requires_slots:
            issues.append(
                _issue(
                    "error",
                    "requires_own_slot",
                    f"{item.display_name} requires its own slot {item.slot}.",
                    slots=[item.slot],
                    item_ids=[item.item_id],
                )
            )
        if not item.prefab_asset_path:
            issues.append(
                _issue(
                    "warning",
                    "missing_prefab_asset_path",
                    f"{item.display_name} does not yet declare a prefab asset path for export handoff.",
                    slots=[item.slot],
                    item_ids=[item.item_id],
                )
            )
        overlapping_tags = sorted(
            set(tag.casefold() for tag in item.compatible_tags)
            & set(tag.casefold() for tag in item.incompatible_tags)
        )
        if overlapping_tags:
            issues.append(
                _issue(
                    "warning",
                    "self_conflicting_tags",
                    f"{item.display_name} repeats tags across compatible and incompatible sets.",
                    slots=[item.slot],
                    item_ids=[item.item_id],
                )
            )
        if item.anchor_type == "BoneAttach" and not item.anchor_bone_name:
            issues.append(
                _issue(
                    "warning",
                    "missing_anchor_bone",
                    f"{item.display_name} uses BoneAttach without anchor_bone_name.",
                    slots=[item.slot],
                    item_ids=[item.item_id],
                )
            )
        return issues

    def _validate_outfit(
        self,
        outfit: _WardrobeOutfitDocument,
        items: list[_WardrobeItemDocument],
    ) -> list[WardrobeValidationIssue]:
        issues: list[WardrobeValidationIssue] = []
        item_lookup = {item.item_id: item for item in items}
        assigned_items = [item_lookup[item_id] for item_id in outfit.slot_assignments.values() if item_id in item_lookup]
        assigned_slots = set(outfit.slot_assignments)

        if "dress" in assigned_slots and ("top" in assigned_slots or "bottom" in assigned_slots):
            issues.append(
                _issue(
                    "warning",
                    "dress_with_separates",
                    f"{outfit.display_name} combines a dress with top or bottom assignments.",
                    slots=sorted(assigned_slots & {"dress", "top", "bottom"}),
                    item_ids=list(outfit.slot_assignments.values()),
                )
            )

        occupancy: dict[str, list[str]] = {}
        for item in assigned_items:
            occupied_slots = [item.slot, *item.occupies_slots]
            for slot_key in occupied_slots:
                occupancy.setdefault(slot_key, []).append(item.item_id)
            for required_slot in item.requires_slots:
                if required_slot not in assigned_slots:
                    issues.append(
                        _issue(
                            "error",
                            "missing_required_slot",
                            f"{outfit.display_name} equips {item.display_name} without required slot {required_slot}.",
                            slots=[item.slot, required_slot],
                            item_ids=[item.item_id],
                        )
                    )
            for blocked_slot in item.blocks_slots:
                blocked_item_id = outfit.slot_assignments.get(blocked_slot)
                if blocked_item_id:
                    issues.append(
                        _issue(
                            "error",
                            "blocked_slot_conflict",
                            f"{outfit.display_name} keeps blocked slot {blocked_slot} active with {item.display_name}.",
                            slots=[item.slot, blocked_slot],
                            item_ids=[item.item_id, blocked_item_id],
                        )
                    )

        for slot_key, occupant_ids in occupancy.items():
            unique_ids = sorted(set(occupant_ids))
            if len(unique_ids) > 1:
                issues.append(
                    _issue(
                        "error",
                        "occupied_slot_conflict",
                        f"{outfit.display_name} assigns multiple items to occupied slot {slot_key}.",
                        slots=[slot_key],
                        item_ids=unique_ids,
                    )
                )

        for index, item in enumerate(assigned_items):
            incompatible = {tag.casefold() for tag in item.incompatible_tags}
            if not incompatible:
                continue
            for other in assigned_items[index + 1 :]:
                other_tags = {tag.casefold() for tag in other.compatible_tags}
                if incompatible & other_tags:
                    issues.append(
                        _issue(
                            "error",
                            "incompatible_tags",
                            f"{item.display_name} conflicts with {other.display_name} through tag compatibility.",
                            slots=[item.slot, other.slot],
                            item_ids=[item.item_id, other.item_id],
                        )
                    )

        return issues

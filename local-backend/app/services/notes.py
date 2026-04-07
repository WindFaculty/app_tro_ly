from __future__ import annotations

import unicodedata

from app.core.ids import make_id
from app.core.time import iso_datetime, now_local
from app.db.repository import SQLiteRepository
from app.models.schemas import NoteCreateRequest, NoteListResponse, NoteRecord, NoteUpdateRequest


class NoteService:
    def __init__(self, repository: SQLiteRepository) -> None:
        self._repository = repository

    def list_notes(
        self,
        *,
        limit: int = 100,
        tag: str | None = None,
        linked_task_id: str | None = None,
        linked_conversation_id: str | None = None,
    ) -> NoteListResponse:
        normalized_tag = self._normalize(tag) if tag else ""
        items = self._repository.list_notes(limit=limit)
        filtered = []
        for item in items:
            if normalized_tag and normalized_tag not in {self._normalize(note_tag) for note_tag in item["tags"]}:
                continue
            if linked_task_id and item["linked_task_id"] != linked_task_id:
                continue
            if linked_conversation_id and item["linked_conversation_id"] != linked_conversation_id:
                continue
            filtered.append(NoteRecord.model_validate(item))
        return NoteListResponse(items=filtered, count=len(filtered))

    def create_note(self, request: NoteCreateRequest) -> NoteRecord:
        now_iso = iso_datetime(now_local())
        normalized = self._normalize_payload(request.model_dump(), existing=None)
        normalized.update(
            {
                "id": make_id("note"),
                "created_at": now_iso,
                "updated_at": now_iso,
            }
        )
        self._repository.create_note(normalized)
        return NoteRecord.model_validate(self._repository.get_note(normalized["id"]))

    def update_note(self, note_id: str, request: NoteUpdateRequest) -> NoteRecord:
        existing = self._require_note(note_id)
        updates = request.model_dump(exclude_unset=True)
        if not updates:
            return NoteRecord.model_validate(existing)
        merged = {**existing, **updates}
        normalized = self._normalize_payload(merged, existing=existing)
        normalized["updated_at"] = iso_datetime(now_local())
        self._repository.update_note(note_id, normalized)
        return NoteRecord.model_validate(self._require_note(note_id))

    def _require_note(self, note_id: str) -> dict[str, object]:
        note = self._repository.get_note(note_id)
        if note is None:
            raise LookupError(f"Note '{note_id}' not found")
        return note

    def _normalize_payload(
        self,
        payload: dict[str, object],
        existing: dict[str, object] | None,
    ) -> dict[str, object]:
        data = dict(payload)
        title = str(data.get("title") or "").strip()
        if not title:
            raise ValueError("Note title must not be empty")

        linked_task_id = self._normalize_optional_id(data.get("linked_task_id"))
        linked_conversation_id = self._normalize_optional_id(data.get("linked_conversation_id"))
        if linked_task_id and self._repository.get_task(linked_task_id) is None:
            raise ValueError(f"Linked task '{linked_task_id}' not found")
        if linked_conversation_id and self._repository.get_conversation(linked_conversation_id) is None:
            raise ValueError(f"Linked conversation '{linked_conversation_id}' not found")

        data["title"] = title
        data["body"] = str(data.get("body") or "").strip()
        data["tags"] = self._normalize_tags(data.get("tags"))
        data["linked_task_id"] = linked_task_id
        data["linked_conversation_id"] = linked_conversation_id
        data["pinned"] = bool(data.get("pinned"))
        if existing is not None:
            data["id"] = existing["id"]
            data["created_at"] = existing["created_at"]
        return data

    def _normalize_tags(self, value: object) -> list[str]:
        if not isinstance(value, list):
            return []
        unique: list[str] = []
        seen: set[str] = set()
        for item in value:
            if not isinstance(item, str):
                continue
            cleaned = item.strip()
            key = self._normalize(cleaned)
            if not cleaned or key in seen:
                continue
            unique.append(cleaned)
            seen.add(key)
        return unique

    def _normalize_optional_id(self, value: object) -> str | None:
        if not isinstance(value, str):
            return None
        cleaned = value.strip()
        return cleaned or None

    def _normalize(self, value: str | None) -> str:
        normalized = unicodedata.normalize("NFKD", (value or "").casefold())
        return "".join(ch for ch in normalized if not unicodedata.combining(ch)).strip()

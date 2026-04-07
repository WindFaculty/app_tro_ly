from __future__ import annotations

from app.core.ids import make_id
from app.core.time import iso_datetime, now_local


def test_notes_crud_links_memory_and_active_tasks(client) -> None:
    now_iso = iso_datetime(now_local())
    task_response = client.post(
        "/v1/tasks",
        json={
            "title": "Prepare research notes",
            "status": "planned",
            "priority": "medium",
            "repeat_rule": "none",
            "tags": ["notes"],
        },
    )
    assert task_response.status_code == 200
    task = task_response.json()

    repository = client.app.state.container.repository
    conversation_id = make_id("conv")
    repository.create_conversation(
        {
            "id": conversation_id,
            "mode": "text",
            "created_at": now_iso,
            "updated_at": now_iso,
        }
    )
    repository.add_message(
        {
            "id": make_id("msg"),
            "conversation_id": conversation_id,
            "role": "user",
            "content": "Remember that the workshop starts at 4 PM.",
            "emotion": None,
            "animation_hint": None,
            "metadata_json": "{}",
            "created_at": now_iso,
        }
    )
    repository.upsert_memory_item(
        {
            "id": make_id("mem"),
            "category": "routine",
            "normalized_key": "workshop starts at 4 pm",
            "content": "Workshop starts at 4 PM.",
            "confidence": 0.91,
            "status": "active",
            "metadata_json": {"source": "test"},
            "source_conversation_id": conversation_id,
            "created_at": now_iso,
            "updated_at": now_iso,
        }
    )

    create_note = client.post(
        "/v1/notes",
        json={
            "title": "Workshop prep",
            "body": "Bring demo deck and check the projector.",
            "tags": ["event", "prep"],
            "linked_task_id": task["id"],
            "linked_conversation_id": conversation_id,
            "pinned": True,
        },
    )
    assert create_note.status_code == 200
    note = create_note.json()
    assert note["linked_task_id"] == task["id"]
    assert note["linked_conversation_id"] == conversation_id
    assert note["pinned"] is True

    notes_response = client.get("/v1/notes")
    assert notes_response.status_code == 200
    notes_payload = notes_response.json()
    assert notes_payload["count"] == 1
    assert notes_payload["items"][0]["title"] == "Workshop prep"

    update_note = client.put(
        f"/v1/notes/{note['id']}",
        json={
            "body": "Bring demo deck, check projector, and confirm room access.",
            "tags": ["event", "logistics"],
            "pinned": False,
        },
    )
    assert update_note.status_code == 200
    updated = update_note.json()
    assert updated["pinned"] is False
    assert updated["tags"] == ["event", "logistics"]

    memory_response = client.get("/v1/memory/items")
    assert memory_response.status_code == 200
    memory_payload = memory_response.json()
    assert memory_payload["count"] == 1
    assert memory_payload["items"][0]["content"] == "Workshop starts at 4 PM."

    active_response = client.get("/v1/tasks/active")
    assert active_response.status_code == 200
    active_payload = active_response.json()
    assert active_payload["count"] == 1
    assert active_payload["items"][0]["id"] == task["id"]


def test_note_create_rejects_missing_linked_task(client) -> None:
    response = client.post(
        "/v1/notes",
        json={
            "title": "Broken link",
            "body": "This should fail.",
            "linked_task_id": "task_missing",
        },
    )
    assert response.status_code == 400
    assert "Linked task" in response.json()["detail"]

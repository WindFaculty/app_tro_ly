from __future__ import annotations

from datetime import timedelta

from app.core.time import iso_date, now_local


def test_chat_answers_today_summary(client) -> None:
    day = now_local().date()
    client.post(
        "/v1/tasks",
        json={
            "title": "Lam slide demo",
            "status": "planned",
            "priority": "high",
            "scheduled_date": iso_date(day),
            "repeat_rule": "none",
            "tags": [],
        },
    )

    response = client.post(
        "/v1/chat",
        json={
            "message": "Hôm nay tôi có gì?",
            "conversation_id": None,
            "mode": "text",
            "selected_date": iso_date(day),
            "include_voice": False,
        },
    )
    assert response.status_code == 200
    payload = response.json()
    assert payload["emotion"] == "serious"
    assert payload["cards"][0]["type"] == "today_summary"
    assert "Hôm nay" in payload["reply_text"]


def test_chat_can_create_complete_and_reschedule_tasks(client) -> None:
    create_response = client.post(
        "/v1/chat",
        json={
            "message": "Thêm task họp nhóm lúc 2 giờ chiều mai",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert create_response.status_code == 200
    created_payload = create_response.json()
    assert created_payload["task_actions"][0]["type"] == "create_task"
    task_id = created_payload["task_actions"][0]["task_id"]

    complete_response = client.post(
        "/v1/chat",
        json={
            "message": "Đánh dấu họp nhóm là xong",
            "conversation_id": created_payload["conversation_id"],
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert complete_response.status_code == 200
    assert complete_response.json()["task_actions"][0]["type"] == "complete_task"

    reopen = client.put(f"/v1/tasks/{task_id}", json={"status": "planned"})
    assert reopen.status_code == 200

    reschedule_response = client.post(
        "/v1/chat",
        json={
            "message": "Dời họp nhóm sang thứ sáu",
            "conversation_id": created_payload["conversation_id"],
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert reschedule_response.status_code == 200
    assert reschedule_response.json()["task_actions"][0]["type"] == "reschedule_task"


def test_chat_priority_update(client) -> None:
    day = now_local().date() + timedelta(days=1)
    create_response = client.post(
        "/v1/tasks",
        json={
            "title": "Bao cao tuan",
            "status": "planned",
            "priority": "medium",
            "scheduled_date": iso_date(day),
            "repeat_rule": "none",
            "tags": [],
        },
    )
    task_id = create_response.json()["id"]

    response = client.post(
        "/v1/chat",
        json={
            "message": "Tăng ưu tiên báo cáo tuần lên cao",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert response.status_code == 200
    assert response.json()["task_actions"][0]["type"] == "priority_task"

    task = client.get(f"/v1/tasks/week?start_date={iso_date(day)}").json()
    flattened = [item for day_bucket in task["days"] for item in day_bucket["items"]]
    updated = next(item for item in flattened if item["id"] == task_id)
    assert updated["priority"] == "high"


def test_chat_falls_back_to_text_when_tts_fails(client) -> None:
    def fail_synthesize(text: str, voice: str | None = None, cache: bool = True):
        raise RuntimeError("tts offline")

    client.app.state.container.speech_service.synthesize = fail_synthesize

    response = client.post(
        "/v1/chat",
        json={
            "message": "Add task fallback coverage tomorrow",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": True,
        },
    )

    assert response.status_code == 200
    payload = response.json()
    assert payload["reply_text"]
    assert payload["speak"] is False
    assert payload["audio_url"] is None
    assert payload["task_actions"][0]["type"] == "create_task"


def test_chat_conversations_list_returns_recent_threads_with_previews(client) -> None:
    first = client.post(
        "/v1/chat",
        json={
            "message": "Create task finish release note draft tomorrow",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert first.status_code == 200

    second = client.post(
        "/v1/chat",
        json={
            "message": "Summarize my day and call out the biggest risk",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert second.status_code == 200

    response = client.get("/v1/chat/conversations?limit=10")
    assert response.status_code == 200
    payload = response.json()

    assert payload["count"] >= 2
    assert len(payload["items"]) >= 2
    assert payload["items"][0]["updated_at"] >= payload["items"][1]["updated_at"]

    latest_ids = {item["conversation_id"] for item in payload["items"][:2]}
    assert first.json()["conversation_id"] in latest_ids
    assert second.json()["conversation_id"] in latest_ids

    latest_thread = next(
        item
        for item in payload["items"]
        if item["conversation_id"] == second.json()["conversation_id"]
    )
    assert latest_thread["message_count"] == 2
    assert latest_thread["last_message_role"] == "assistant"
    assert latest_thread["last_message_preview"]


def test_chat_conversation_detail_returns_messages_with_metadata(client) -> None:
    chat_response = client.post(
        "/v1/chat",
        json={
            "message": "Create task review planner rebuild on Friday",
            "conversation_id": None,
            "mode": "text",
            "selected_date": None,
            "include_voice": False,
        },
    )
    assert chat_response.status_code == 200
    conversation_id = chat_response.json()["conversation_id"]

    response = client.get(f"/v1/chat/conversations/{conversation_id}")
    assert response.status_code == 200
    payload = response.json()

    assert payload["conversation_id"] == conversation_id
    assert payload["message_count"] == 2
    assert len(payload["messages"]) == 2
    assert payload["messages"][0]["role"] == "user"
    assert payload["messages"][0]["content"] == "Create task review planner rebuild on Friday"
    assert payload["messages"][0]["metadata"]["notes_context"] == ""
    assert payload["messages"][1]["role"] == "assistant"
    assert payload["messages"][1]["metadata"]["route"]
    assert payload["messages"][1]["metadata"]["provider"]


def test_chat_conversation_detail_returns_not_found_for_missing_thread(client) -> None:
    response = client.get("/v1/chat/conversations/conv_missing")
    assert response.status_code == 404

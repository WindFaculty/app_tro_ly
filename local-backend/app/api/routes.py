from __future__ import annotations

import base64
import shutil
from datetime import date
from pathlib import Path
from typing import Any

from fastapi import APIRouter, File, HTTPException, Query, Request, UploadFile, WebSocket, WebSocketDisconnect
from fastapi.responses import FileResponse, HTMLResponse

from app.container import AppContainer
from app.core.ids import make_id
from app.core.time import iso_date
from app.core.health import build_logs_payload, build_recovery_actions
from app.core.logging import get_logger
from app.models.schemas import (
    AssistantStreamMessage,
    BrowserAutomationApprovalRequest,
    BrowserAutomationCancelRequest,
    BrowserAutomationRejectRequest,
    BrowserAutomationRunCreateRequest,
    BrowserAutomationRunDetail,
    BrowserAutomationRunListResponse,
    BrowserAutomationTemplateListResponse,
    ChatConversationDetailResponse,
    ChatConversationListResponse,
    ChatRequest,
    ChatResponse,
    CompleteTaskRequest,
    EmailDraftCreateRequest,
    EmailDraftListResponse,
    EmailDraftRecord,
    EmailDraftUpdateRequest,
    EmailMessageDetail,
    EmailMessageListResponse,
    EmailToTaskRequest,
    GoogleCalendarConnectResponse,
    GoogleCalendarDeleteResponse,
    GoogleCalendarEventCreateRequest,
    GoogleCalendarEventListResponse,
    GoogleCalendarEventRecord,
    GoogleCalendarEventUpdateRequest,
    GoogleCalendarStatusResponse,
    GoogleEmailConnectResponse,
    GoogleEmailStatusResponse,
    HealthResponse,
    MemoryListResponse,
    NoteCreateRequest,
    NoteListResponse,
    NoteRecord,
    NoteUpdateRequest,
    RescheduleTaskRequest,
    SettingsPayload,
    SpeechSttResponse,
    SpeechTtsRequest,
    SpeechTtsResponse,
    TaskCreateRequest,
    TaskUpdateRequest,
    WardrobeImportRequest,
    WardrobeItemCreateRequest,
    WardrobeItemRecord,
    WardrobeItemUpdateRequest,
    WardrobeOutfitCreateRequest,
    WardrobeOutfitRecord,
    WardrobeOutfitUpdateRequest,
    WardrobeSnapshotResponse,
)

router = APIRouter(prefix="/v1")
logger = get_logger("api")


def _container(request: Request) -> AppContainer:
    return request.app.state.container


def _parse_day(day_value: str | None) -> date:
    if day_value:
        return date.fromisoformat(day_value)
    from app.core.time import now_local

    return now_local().date()


def _runtime_is_degraded(runtime_payload: dict[str, Any]) -> bool:
    if not runtime_payload.get("available", False):
        return True
    provider_available = runtime_payload.get("provider_available")
    return provider_available is False


@router.get("/health", response_model=HealthResponse)
async def health(request: Request) -> HealthResponse:
    container = _container(request)
    database = container.repository.health_check()
    llm = container.llm_service.health()
    stt = container.speech_service.stt_health()
    tts = container.speech_service.tts_health()
    recovery_actions = build_recovery_actions(container.settings, database, llm, stt, tts)
    degraded = []
    if not llm["available"]:
        degraded.append("llm")
    if _runtime_is_degraded(stt):
        degraded.append("stt")
    if _runtime_is_degraded(tts):
        degraded.append("tts")
    status = "ready"
    if not database["available"]:
        status = "error"
    elif degraded:
        status = "partial"
    return HealthResponse(
        status=status,
        service=container.settings.app_name,
        version=container.settings.app_version,
        database=database,
        runtimes={"llm": llm, "stt": stt, "tts": tts},
        degraded_features=degraded,
        logs=build_logs_payload(container.settings),
        recovery_actions=recovery_actions,
    )


@router.get("/tasks/today")
async def tasks_today(request: Request, date: str | None = Query(default=None)) -> dict[str, Any]:
    return _container(request).task_service.list_day(_parse_day(date))


@router.get("/tasks/week")
async def tasks_week(request: Request, start_date: str | None = Query(default=None)) -> dict[str, Any]:
    return _container(request).task_service.list_week(_parse_day(start_date))


@router.get("/tasks/overdue")
async def tasks_overdue(request: Request) -> dict[str, Any]:
    return _container(request).task_service.list_overdue()


@router.get("/tasks/inbox")
async def tasks_inbox(request: Request, limit: int = Query(default=50, ge=1, le=200)) -> dict[str, Any]:
    return _container(request).task_service.list_inbox(limit=limit)


@router.get("/tasks/active")
async def tasks_active(request: Request, limit: int = Query(default=100, ge=1, le=200)) -> dict[str, Any]:
    return _container(request).task_service.list_active(limit=limit)


@router.get("/tasks/completed")
async def tasks_completed(request: Request, limit: int = Query(default=50, ge=1, le=200)) -> dict[str, Any]:
    return _container(request).task_service.list_completed(limit=limit)


@router.post("/tasks")
async def create_task(request: Request, payload: TaskCreateRequest) -> dict[str, Any]:
    try:
        task = _container(request).task_service.create_task(payload)
    except ValueError as exc:
        logger.warning("Task creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    await _container(request).event_bus.publish({"type": "task_updated", "task_id": task.id, "change": "created"})
    return task.model_dump()


@router.put("/tasks/{task_id}")
async def update_task(request: Request, task_id: str, payload: TaskUpdateRequest) -> dict[str, Any]:
    try:
        task = _container(request).task_service.update_task(task_id, payload)
    except LookupError as exc:
        logger.warning("Task update failed for %s: %s", task_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Task update rejected for %s: %s", task_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    await _container(request).event_bus.publish({"type": "task_updated", "task_id": task.id, "change": "updated"})
    return task.model_dump()


@router.post("/tasks/{task_id}/complete")
async def complete_task(request: Request, task_id: str, payload: CompleteTaskRequest) -> dict[str, Any]:
    try:
        task = _container(request).task_service.complete_task(task_id, payload)
    except LookupError as exc:
        logger.warning("Task completion failed for %s: %s", task_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    await _container(request).event_bus.publish({"type": "task_updated", "task_id": task.id, "change": "completed"})
    return task.model_dump()


@router.post("/tasks/{task_id}/reschedule")
async def reschedule_task(request: Request, task_id: str, payload: RescheduleTaskRequest) -> dict[str, Any]:
    try:
        task = _container(request).task_service.reschedule_task(task_id, payload)
    except LookupError as exc:
        logger.warning("Task reschedule failed for %s: %s", task_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Task reschedule rejected for %s: %s", task_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    await _container(request).event_bus.publish({"type": "task_updated", "task_id": task.id, "change": "rescheduled"})
    return task.model_dump()


@router.get("/notes", response_model=NoteListResponse)
async def list_notes(
    request: Request,
    limit: int = Query(default=100, ge=1, le=200),
    tag: str | None = Query(default=None),
    linked_task_id: str | None = Query(default=None),
    linked_conversation_id: str | None = Query(default=None),
) -> NoteListResponse:
    return _container(request).note_service.list_notes(
        limit=limit,
        tag=tag,
        linked_task_id=linked_task_id,
        linked_conversation_id=linked_conversation_id,
    )


@router.post("/notes", response_model=NoteRecord)
async def create_note(request: Request, payload: NoteCreateRequest) -> NoteRecord:
    try:
        return _container(request).note_service.create_note(payload)
    except ValueError as exc:
        logger.warning("Note creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.put("/notes/{note_id}", response_model=NoteRecord)
async def update_note(request: Request, note_id: str, payload: NoteUpdateRequest) -> NoteRecord:
    try:
        return _container(request).note_service.update_note(note_id, payload)
    except LookupError as exc:
        logger.warning("Note update failed for %s: %s", note_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Note update rejected for %s: %s", note_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/memory/items", response_model=MemoryListResponse)
async def list_memory_items(
    request: Request,
    limit: int = Query(default=50, ge=1, le=200),
) -> MemoryListResponse:
    return _container(request).memory_service.list_memory_items(limit=limit)


@router.get("/wardrobe", response_model=WardrobeSnapshotResponse)
async def get_wardrobe_snapshot(request: Request) -> WardrobeSnapshotResponse:
    return _container(request).wardrobe_service.get_snapshot()


@router.get("/wardrobe/export", response_model=WardrobeSnapshotResponse)
async def export_wardrobe_snapshot(request: Request) -> WardrobeSnapshotResponse:
    return _container(request).wardrobe_service.export_snapshot()


@router.post("/wardrobe/import", response_model=WardrobeSnapshotResponse)
async def import_wardrobe_snapshot(
    request: Request,
    payload: WardrobeImportRequest,
) -> WardrobeSnapshotResponse:
    try:
        return _container(request).wardrobe_service.import_snapshot(payload)
    except ValueError as exc:
        logger.warning("Wardrobe import rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/wardrobe/items", response_model=WardrobeItemRecord)
async def create_wardrobe_item(
    request: Request,
    payload: WardrobeItemCreateRequest,
) -> WardrobeItemRecord:
    try:
        return _container(request).wardrobe_service.create_item(payload)
    except ValueError as exc:
        logger.warning("Wardrobe item creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.put("/wardrobe/items/{item_id}", response_model=WardrobeItemRecord)
async def update_wardrobe_item(
    request: Request,
    item_id: str,
    payload: WardrobeItemUpdateRequest,
) -> WardrobeItemRecord:
    try:
        return _container(request).wardrobe_service.update_item(item_id, payload)
    except LookupError as exc:
        logger.warning("Wardrobe item update failed for %s: %s", item_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Wardrobe item update rejected for %s: %s", item_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.delete("/wardrobe/items/{item_id}", response_model=WardrobeSnapshotResponse)
async def delete_wardrobe_item(
    request: Request,
    item_id: str,
) -> WardrobeSnapshotResponse:
    try:
        return _container(request).wardrobe_service.delete_item(item_id)
    except LookupError as exc:
        logger.warning("Wardrobe item delete failed for %s: %s", item_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc


@router.post("/wardrobe/outfits", response_model=WardrobeOutfitRecord)
async def create_wardrobe_outfit(
    request: Request,
    payload: WardrobeOutfitCreateRequest,
) -> WardrobeOutfitRecord:
    try:
        return _container(request).wardrobe_service.create_outfit(payload)
    except ValueError as exc:
        logger.warning("Wardrobe outfit creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.put("/wardrobe/outfits/{outfit_id}", response_model=WardrobeOutfitRecord)
async def update_wardrobe_outfit(
    request: Request,
    outfit_id: str,
    payload: WardrobeOutfitUpdateRequest,
) -> WardrobeOutfitRecord:
    try:
        return _container(request).wardrobe_service.update_outfit(outfit_id, payload)
    except LookupError as exc:
        logger.warning("Wardrobe outfit update failed for %s: %s", outfit_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Wardrobe outfit update rejected for %s: %s", outfit_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.delete("/wardrobe/outfits/{outfit_id}", response_model=WardrobeSnapshotResponse)
async def delete_wardrobe_outfit(
    request: Request,
    outfit_id: str,
) -> WardrobeSnapshotResponse:
    try:
        return _container(request).wardrobe_service.delete_outfit(outfit_id)
    except LookupError as exc:
        logger.warning("Wardrobe outfit delete failed for %s: %s", outfit_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc


@router.post("/chat", response_model=ChatResponse)
async def chat(request: Request, payload: ChatRequest) -> ChatResponse:
    try:
        return await _container(request).conversation_service.handle_chat(payload)
    except LookupError as exc:
        logger.warning("Chat task lookup failed: %s", exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Chat validation failed: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/chat/conversations", response_model=ChatConversationListResponse)
async def list_chat_conversations(
    request: Request,
    limit: int = Query(default=20, ge=1, le=100),
) -> ChatConversationListResponse:
    return _container(request).conversation_service.list_conversations(limit=limit)


@router.get("/chat/conversations/{conversation_id}", response_model=ChatConversationDetailResponse)
async def get_chat_conversation(
    request: Request,
    conversation_id: str,
) -> ChatConversationDetailResponse:
    conversation = _container(request).conversation_service.get_conversation_detail(conversation_id)
    if conversation is None:
        raise HTTPException(status_code=404, detail="Conversation not found")
    return conversation


@router.post("/speech/stt", response_model=SpeechSttResponse)
async def speech_stt(
    request: Request,
    audio: UploadFile = File(...),
    language: str | None = None,
) -> SpeechSttResponse:
    container = _container(request)
    original_name = Path(audio.filename or "input.wav").name or "input.wav"
    temp_path = container.settings.audio_dir / f"{make_id('stt')}_{original_name}"
    with temp_path.open("wb") as handle:
        shutil.copyfileobj(audio.file, handle)
    try:
        result = container.speech_service.transcribe(temp_path, language=language)
        return SpeechSttResponse(**result)
    except RuntimeError as exc:
        logger.warning("STT request failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    finally:
        temp_path.unlink(missing_ok=True)


@router.post("/speech/tts", response_model=SpeechTtsResponse)
async def speech_tts(request: Request, payload: SpeechTtsRequest) -> SpeechTtsResponse:
    try:
        result = _container(request).speech_service.synthesize(payload.text, payload.voice, payload.cache)
        return SpeechTtsResponse(
            audio_url=result["audio_url"],
            duration_ms=result["duration_ms"],
            cached=result["cached"],
        )
    except Exception as exc:
        logger.warning("TTS request failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.get("/speech/cache/{filename}")
async def speech_cache(request: Request, filename: str) -> FileResponse:
    audio_path = _container(request).settings.audio_dir / filename
    if not audio_path.exists():
        raise HTTPException(status_code=404, detail="Audio file not found")
    return FileResponse(audio_path)


@router.get("/settings")
async def get_settings(request: Request) -> dict[str, Any]:
    return _container(request).settings_service.get()


@router.put("/settings")
async def put_settings(request: Request, payload: SettingsPayload) -> dict[str, Any]:
    return _container(request).settings_service.update(payload.model_dump(exclude_unset=True))


@router.post("/settings/reset")
async def reset_settings(request: Request) -> dict[str, Any]:
    return _container(request).settings_service.reset()


@router.get("/calendar/status", response_model=GoogleCalendarStatusResponse)
async def get_calendar_status(request: Request) -> GoogleCalendarStatusResponse:
    return _container(request).google_calendar_service.status()


@router.get("/calendar/google/connect", response_model=GoogleCalendarConnectResponse)
async def start_google_calendar_connect(request: Request) -> GoogleCalendarConnectResponse:
    try:
        return _container(request).google_calendar_service.start_google_connect()
    except ValueError as exc:
        logger.warning("Google calendar connect setup rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/calendar/google/callback")
async def google_calendar_callback(
    request: Request,
    code: str | None = Query(default=None),
    state: str | None = Query(default=None),
    error: str | None = Query(default=None),
) -> HTMLResponse:
    if error:
        return HTMLResponse(
            "<html><body><h1>Google calendar connection failed</h1>"
            f"<p>{error}</p><p>You can close this window and return to the desktop app.</p></body></html>",
            status_code=400,
        )
    if not code or not state:
        raise HTTPException(status_code=400, detail="Missing Google OAuth callback parameters")
    try:
        status = _container(request).google_calendar_service.complete_google_connect(
            code=code,
            state=state,
        )
    except ValueError as exc:
        logger.warning("Google calendar callback rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Google calendar callback failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    return HTMLResponse(
        "<html><body><h1>Google calendar connected</h1>"
        f"<p>Status: {status.status}</p>"
        f"<p>Calendar: {status.calendar_id or 'connected'}</p>"
        "<p>You can close this window and return to the desktop app.</p></body></html>"
    )


@router.post("/calendar/google/disconnect", response_model=GoogleCalendarStatusResponse)
async def disconnect_google_calendar(request: Request) -> GoogleCalendarStatusResponse:
    return _container(request).google_calendar_service.disconnect_google_account()


@router.get("/calendar/events", response_model=GoogleCalendarEventListResponse)
async def list_calendar_events(
    request: Request,
    start_date: str | None = Query(default=None),
    days: int = Query(default=7, ge=1, le=31),
    calendar_id: str | None = Query(default=None),
    query: str = Query(default=""),
    limit: int = Query(default=20, ge=1, le=100),
) -> GoogleCalendarEventListResponse:
    try:
        return _container(request).google_calendar_service.list_events(
            start_date=start_date,
            days=days,
            calendar_id=calendar_id,
            query=query,
            limit=limit,
        )
    except RuntimeError as exc:
        logger.warning("Calendar event list failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.post("/calendar/events", response_model=GoogleCalendarEventRecord)
async def create_calendar_event(
    request: Request,
    payload: GoogleCalendarEventCreateRequest,
) -> GoogleCalendarEventRecord:
    try:
        return _container(request).google_calendar_service.create_event(payload)
    except ValueError as exc:
        logger.warning("Calendar event creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Calendar event creation failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.put("/calendar/events/{event_id}", response_model=GoogleCalendarEventRecord)
async def update_calendar_event(
    request: Request,
    event_id: str,
    payload: GoogleCalendarEventUpdateRequest,
) -> GoogleCalendarEventRecord:
    try:
        return _container(request).google_calendar_service.update_event(event_id, payload)
    except ValueError as exc:
        logger.warning("Calendar event update rejected for %s: %s", event_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Calendar event update failed for %s: %s", event_id, exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.delete("/calendar/events/{event_id}", response_model=GoogleCalendarDeleteResponse)
async def delete_calendar_event(
    request: Request,
    event_id: str,
    calendar_id: str | None = Query(default=None),
) -> GoogleCalendarDeleteResponse:
    try:
        return _container(request).google_calendar_service.delete_event(
            event_id,
            calendar_id=calendar_id,
        )
    except RuntimeError as exc:
        logger.warning("Calendar event delete failed for %s: %s", event_id, exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.get("/email/status", response_model=GoogleEmailStatusResponse)
async def get_email_status(request: Request) -> GoogleEmailStatusResponse:
    return _container(request).google_email_service.status()


@router.get("/email/google/connect", response_model=GoogleEmailConnectResponse)
async def start_google_email_connect(request: Request) -> GoogleEmailConnectResponse:
    try:
        return _container(request).google_email_service.start_google_connect()
    except ValueError as exc:
        logger.warning("Google email connect setup rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/email/google/callback")
async def google_email_callback(
    request: Request,
    code: str | None = Query(default=None),
    state: str | None = Query(default=None),
    error: str | None = Query(default=None),
) -> HTMLResponse:
    if error:
        return HTMLResponse(
            "<html><body><h1>Google email connection failed</h1>"
            f"<p>{error}</p><p>You can close this window and return to the desktop app.</p></body></html>",
            status_code=400,
        )
    if not code or not state:
        raise HTTPException(status_code=400, detail="Missing Google OAuth callback parameters")
    try:
        status = _container(request).google_email_service.complete_google_connect(code=code, state=state)
    except ValueError as exc:
        logger.warning("Google email callback rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Google email callback failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    return HTMLResponse(
        "<html><body><h1>Google email connected</h1>"
        f"<p>Status: {status.status}</p>"
        f"<p>Account: {status.email_address or 'connected'}</p>"
        "<p>You can close this window and return to the desktop app.</p></body></html>"
    )


@router.post("/email/google/disconnect", response_model=GoogleEmailStatusResponse)
async def disconnect_google_email(request: Request) -> GoogleEmailStatusResponse:
    return _container(request).google_email_service.disconnect_google_account()


@router.get("/email/messages", response_model=EmailMessageListResponse)
async def list_email_messages(
    request: Request,
    query: str = Query(default=""),
    label: str | None = Query(default=None),
    limit: int = Query(default=20, ge=1, le=50),
) -> EmailMessageListResponse:
    try:
        return _container(request).google_email_service.list_messages(
            query=query,
            label=label,
            limit=limit,
        )
    except RuntimeError as exc:
        logger.warning("Email message list failed: %s", exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.get("/email/messages/{message_id}", response_model=EmailMessageDetail)
async def get_email_message(request: Request, message_id: str) -> EmailMessageDetail:
    try:
        return _container(request).google_email_service.get_message(message_id)
    except RuntimeError as exc:
        logger.warning("Email detail failed for %s: %s", message_id, exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.post("/email/messages/{message_id}/task")
async def convert_email_message_to_task(
    request: Request,
    message_id: str,
    payload: EmailToTaskRequest,
) -> dict[str, Any]:
    try:
        task = _container(request).google_email_service.convert_message_to_task(message_id, payload)
    except ValueError as exc:
        logger.warning("Email task conversion rejected for %s: %s", message_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Email task conversion failed for %s: %s", message_id, exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    await _container(request).event_bus.publish(
        {"type": "task_updated", "task_id": task.id, "change": "created_from_email"}
    )
    return task.model_dump()


@router.get("/email/drafts", response_model=EmailDraftListResponse)
async def list_email_drafts(
    request: Request,
    limit: int = Query(default=50, ge=1, le=100),
) -> EmailDraftListResponse:
    return _container(request).google_email_service.list_drafts(limit=limit)


@router.post("/email/drafts", response_model=EmailDraftRecord)
async def create_email_draft(
    request: Request,
    payload: EmailDraftCreateRequest,
) -> EmailDraftRecord:
    try:
        return _container(request).google_email_service.create_draft(payload)
    except ValueError as exc:
        logger.warning("Email draft creation rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.put("/email/drafts/{draft_id}", response_model=EmailDraftRecord)
async def update_email_draft(
    request: Request,
    draft_id: str,
    payload: EmailDraftUpdateRequest,
) -> EmailDraftRecord:
    try:
        return _container(request).google_email_service.update_draft(draft_id, payload)
    except LookupError as exc:
        logger.warning("Email draft update failed for %s: %s", draft_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Email draft update rejected for %s: %s", draft_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/email/drafts/{draft_id}/send", response_model=EmailDraftRecord)
async def send_email_draft(request: Request, draft_id: str) -> EmailDraftRecord:
    try:
        return _container(request).google_email_service.send_draft(draft_id)
    except LookupError as exc:
        logger.warning("Email draft send failed for %s: %s", draft_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Email draft send rejected for %s: %s", draft_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except RuntimeError as exc:
        logger.warning("Email draft send failed for %s: %s", draft_id, exc)
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.get("/browser-automation/templates", response_model=BrowserAutomationTemplateListResponse)
async def list_browser_automation_templates(request: Request) -> BrowserAutomationTemplateListResponse:
    return _container(request).browser_automation_service.list_templates()


@router.get("/browser-automation/runs", response_model=BrowserAutomationRunListResponse)
async def list_browser_automation_runs(
    request: Request,
    limit: int = Query(default=20, ge=1, le=100),
) -> BrowserAutomationRunListResponse:
    return _container(request).browser_automation_service.list_runs(limit=limit)


@router.post("/browser-automation/runs", response_model=BrowserAutomationRunDetail)
async def create_browser_automation_run(
    request: Request,
    payload: BrowserAutomationRunCreateRequest,
) -> BrowserAutomationRunDetail:
    try:
        return _container(request).browser_automation_service.create_run(payload)
    except ValueError as exc:
        logger.warning("Browser automation run rejected: %s", exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/browser-automation/runs/{run_id}", response_model=BrowserAutomationRunDetail)
async def get_browser_automation_run(request: Request, run_id: str) -> BrowserAutomationRunDetail:
    try:
        return _container(request).browser_automation_service.get_run(run_id)
    except LookupError as exc:
        logger.warning("Browser automation run lookup failed for %s: %s", run_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc


@router.post("/browser-automation/runs/{run_id}/approve", response_model=BrowserAutomationRunDetail)
async def approve_browser_automation_step(
    request: Request,
    run_id: str,
    payload: BrowserAutomationApprovalRequest,
) -> BrowserAutomationRunDetail:
    try:
        return _container(request).browser_automation_service.approve_next_step(run_id, payload)
    except LookupError as exc:
        logger.warning("Browser automation approve failed for %s: %s", run_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Browser automation approve rejected for %s: %s", run_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/browser-automation/runs/{run_id}/reject", response_model=BrowserAutomationRunDetail)
async def reject_browser_automation_step(
    request: Request,
    run_id: str,
    payload: BrowserAutomationRejectRequest,
) -> BrowserAutomationRunDetail:
    try:
        return _container(request).browser_automation_service.reject_next_step(run_id, payload)
    except LookupError as exc:
        logger.warning("Browser automation reject failed for %s: %s", run_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Browser automation reject rejected for %s: %s", run_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/browser-automation/runs/{run_id}/cancel", response_model=BrowserAutomationRunDetail)
async def cancel_browser_automation_run(
    request: Request,
    run_id: str,
    payload: BrowserAutomationCancelRequest,
) -> BrowserAutomationRunDetail:
    try:
        return _container(request).browser_automation_service.cancel_run(run_id, payload)
    except LookupError as exc:
        logger.warning("Browser automation cancel failed for %s: %s", run_id, exc)
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except ValueError as exc:
        logger.warning("Browser automation cancel rejected for %s: %s", run_id, exc)
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.websocket("/events")
async def events_socket(websocket: WebSocket) -> None:
    await websocket.accept()
    container: AppContainer = websocket.app.state.container
    queue = await container.event_bus.subscribe()
    try:
        await websocket.send_json({"type": "assistant_state_changed", "state": "idle", "emotion": "neutral", "animation_hint": "idle"})
        while True:
            event = await queue.get()
            await websocket.send_json(event)
    except WebSocketDisconnect:
        logger.info("Event socket disconnected")
    finally:
        await container.event_bus.unsubscribe(queue)


@router.websocket("/assistant/stream")
async def assistant_stream_socket(websocket: WebSocket) -> None:
    await websocket.accept()
    container: AppContainer = websocket.app.state.container
    session_context: dict[str, dict[str, Any]] = {}
    transcript_parts: dict[str, list[str]] = {}

    async def send_event(payload: dict[str, Any]) -> None:
        await websocket.send_json(payload)

    try:
        while True:
            payload = AssistantStreamMessage.model_validate(await websocket.receive_json())
            session_id = payload.session_id or "default"
            session_meta = session_context.setdefault(
                session_id,
                {
                    "conversation_id": payload.conversation_id,
                    "selected_date": payload.selected_date,
                    "notes_context": payload.notes_context,
                },
            )
            if payload.conversation_id:
                session_meta["conversation_id"] = payload.conversation_id
            if payload.selected_date is not None:
                session_meta["selected_date"] = payload.selected_date
            if payload.notes_context is not None:
                session_meta["notes_context"] = payload.notes_context

            if payload.type == "session_start":
                await send_event(
                    {
                        "type": "assistant_state_changed",
                        "state": "waiting",
                        "emotion": "neutral",
                        "animation_hint": "idle",
                        "session_id": session_id,
                    }
                )
                continue

            if payload.type == "context_update":
                await send_event(
                    {
                        "type": "assistant_state_changed",
                        "state": "listening" if payload.voice_mode else "waiting",
                        "emotion": "neutral",
                        "animation_hint": "listen" if payload.voice_mode else "idle",
                        "session_id": session_id,
                    }
                )
                continue

            if payload.type == "cancel_response":
                await send_event(
                    {
                        "type": "assistant_state_changed",
                        "state": "waiting",
                        "emotion": "neutral",
                        "animation_hint": "idle",
                        "session_id": session_id,
                    }
                )
                continue

            if payload.type == "session_stop":
                transcript_parts.pop(session_id, None)
                session_context.pop(session_id, None)
                container.repository.delete_assistant_session(session_id)
                await send_event(
                    {
                        "type": "assistant_state_changed",
                        "state": "idle",
                        "emotion": "neutral",
                        "animation_hint": "idle",
                        "session_id": session_id,
                    }
                )
                continue

            if payload.type == "text_turn":
                response = await container.assistant_orchestrator.stream_turn(
                    message=payload.message or "",
                    conversation_id=session_meta.get("conversation_id"),
                    session_id=session_id,
                    selected_date=session_meta.get("selected_date"),
                    voice_mode=payload.voice_mode,
                    notes_context=session_meta.get("notes_context"),
                    stream_emitter=send_event,
                )
                session_meta["conversation_id"] = response.conversation_id
                continue

            if payload.type == "voice_chunk":
                if not payload.audio_base64:
                    continue
                try:
                    transcript = container.speech_service.transcribe_bytes(
                        base64.b64decode(payload.audio_base64),
                        language=payload.language,
                    )
                    text = (transcript.get("text") or "").strip()
                    if text:
                        transcript_parts.setdefault(session_id, []).append(text)
                        await send_event(
                            {
                                "type": "transcript_partial",
                                "session_id": session_id,
                                "text": " ".join(transcript_parts.get(session_id, [])),
                            }
                        )
                except Exception as exc:
                    await send_event({"type": "error", "session_id": session_id, "detail": str(exc)})
                continue

            if payload.type == "voice_end":
                if payload.audio_base64:
                    try:
                        transcript = container.speech_service.transcribe_bytes(
                            base64.b64decode(payload.audio_base64),
                            language=payload.language,
                        )
                        text = (transcript.get("text") or "").strip()
                        if text:
                            transcript_parts.setdefault(session_id, []).append(text)
                    except Exception as exc:
                        await send_event({"type": "error", "session_id": session_id, "detail": str(exc)})
                        continue
                final_text = " ".join(transcript_parts.pop(session_id, [])).strip()
                await send_event(
                    {
                        "type": "transcript_final",
                        "session_id": session_id,
                        "text": final_text,
                    }
                )
                if final_text:
                    response = await container.assistant_orchestrator.stream_turn(
                        message=final_text,
                        conversation_id=session_meta.get("conversation_id"),
                        session_id=session_id,
                        selected_date=session_meta.get("selected_date"),
                        voice_mode=True,
                        notes_context=session_meta.get("notes_context"),
                        stream_emitter=send_event,
                    )
                    session_meta["conversation_id"] = response.conversation_id
                continue
    except WebSocketDisconnect:
        logger.info("Assistant stream socket disconnected")

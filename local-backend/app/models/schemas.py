from __future__ import annotations

from typing import Any

from pydantic import BaseModel, ConfigDict, Field, field_validator

from .enums import AnimationHint, AssistantEmotion, RepeatRule, TaskPriority, TaskStatus


class TaskFields(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    title: str = Field(min_length=1, max_length=200)
    description: str | None = None
    status: TaskStatus = TaskStatus.PLANNED
    priority: TaskPriority = TaskPriority.MEDIUM
    category: str | None = None
    scheduled_date: str | None = None
    start_at: str | None = None
    end_at: str | None = None
    due_at: str | None = None
    is_all_day: bool = False
    repeat_rule: RepeatRule = RepeatRule.NONE
    estimated_minutes: int | None = Field(default=None, ge=0)
    actual_minutes: int | None = Field(default=None, ge=0)
    tags: list[str] = Field(default_factory=list)

    @field_validator("title")
    @classmethod
    def normalize_title(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("title must not be empty")
        return cleaned

    @field_validator("tags")
    @classmethod
    def normalize_tags(cls, value: list[str]) -> list[str]:
        return [item.strip() for item in value if item and item.strip()]


class TaskCreateRequest(TaskFields):
    repeat_config_json: dict[str, Any] | None = None


class TaskUpdateRequest(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    title: str | None = None
    description: str | None = None
    status: TaskStatus | None = None
    priority: TaskPriority | None = None
    category: str | None = None
    scheduled_date: str | None = None
    start_at: str | None = None
    end_at: str | None = None
    due_at: str | None = None
    is_all_day: bool | None = None
    repeat_rule: RepeatRule | None = None
    repeat_config_json: dict[str, Any] | None = None
    estimated_minutes: int | None = Field(default=None, ge=0)
    actual_minutes: int | None = Field(default=None, ge=0)
    tags: list[str] | None = None


class TaskRecord(TaskFields):
    id: str
    created_at: str
    updated_at: str
    completed_at: str | None = None


class CompleteTaskRequest(BaseModel):
    completed_at: str | None = None


class RescheduleTaskRequest(BaseModel):
    scheduled_date: str | None = None
    start_at: str | None = None
    end_at: str | None = None
    due_at: str | None = None


class NoteFields(BaseModel):
    title: str = Field(min_length=1, max_length=200)
    body: str = ""
    tags: list[str] = Field(default_factory=list)
    linked_task_id: str | None = None
    linked_conversation_id: str | None = None
    pinned: bool = False

    @field_validator("title")
    @classmethod
    def normalize_note_title(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("title must not be empty")
        return cleaned

    @field_validator("body")
    @classmethod
    def normalize_note_body(cls, value: str) -> str:
        return value.strip()

    @field_validator("tags")
    @classmethod
    def normalize_note_tags(cls, value: list[str]) -> list[str]:
        return [item.strip() for item in value if item and item.strip()]


class NoteCreateRequest(NoteFields):
    pass


class NoteUpdateRequest(BaseModel):
    title: str | None = None
    body: str | None = None
    tags: list[str] | None = None
    linked_task_id: str | None = None
    linked_conversation_id: str | None = None
    pinned: bool | None = None


class NoteRecord(NoteFields):
    id: str
    created_at: str
    updated_at: str


class NoteListResponse(BaseModel):
    items: list[NoteRecord] = Field(default_factory=list)
    count: int = 0


class GoogleEmailStatusResponse(BaseModel):
    provider: str = "gmail"
    status: str
    configured: bool
    connected: bool
    sync_enabled: bool
    email_address: str | None = None
    auth_url_available: bool = False
    redirect_uri: str | None = None
    default_label: str = "INBOX"
    query_limit: int = 20
    scopes: list[str] = Field(default_factory=list)
    last_sync_at: str | None = None
    last_error: str | None = None
    detail: str


class EmailMessageSummary(BaseModel):
    id: str
    thread_id: str
    subject: str
    from_display: str = ""
    from_address: str = ""
    to: list[str] = Field(default_factory=list)
    snippet: str = ""
    labels: list[str] = Field(default_factory=list)
    is_read: bool = False
    starred: bool = False
    has_attachments: bool = False
    received_at: str | None = None
    linked_task_ids: list[str] = Field(default_factory=list)


class EmailMessageDetail(EmailMessageSummary):
    cc: list[str] = Field(default_factory=list)
    body_text: str = ""
    body_html: str | None = None


class EmailMessageListResponse(BaseModel):
    account: GoogleEmailStatusResponse
    query: str = ""
    label: str = "INBOX"
    items: list[EmailMessageSummary] = Field(default_factory=list)
    count: int = 0
    draft_count: int = 0


class EmailDraftFields(BaseModel):
    to: list[str] = Field(default_factory=list)
    cc: list[str] = Field(default_factory=list)
    bcc: list[str] = Field(default_factory=list)
    subject: str = ""
    body_text: str = ""
    linked_message_id: str | None = None

    @field_validator("to", "cc", "bcc")
    @classmethod
    def normalize_recipients(cls, value: list[str]) -> list[str]:
        normalized: list[str] = []
        seen: set[str] = set()
        for item in value:
            cleaned = item.strip()
            key = cleaned.casefold()
            if cleaned and key not in seen:
                normalized.append(cleaned)
                seen.add(key)
        return normalized

    @field_validator("subject", "body_text")
    @classmethod
    def normalize_email_text(cls, value: str) -> str:
        return value.strip()


class EmailDraftCreateRequest(EmailDraftFields):
    thread_id: str | None = None


class EmailDraftUpdateRequest(BaseModel):
    to: list[str] | None = None
    cc: list[str] | None = None
    bcc: list[str] | None = None
    subject: str | None = None
    body_text: str | None = None
    linked_message_id: str | None = None
    thread_id: str | None = None


class EmailDraftRecord(EmailDraftFields):
    id: str
    provider: str
    thread_id: str | None = None
    status: str
    gmail_message_id: str | None = None
    created_at: str
    updated_at: str
    sent_at: str | None = None


class EmailDraftListResponse(BaseModel):
    items: list[EmailDraftRecord] = Field(default_factory=list)
    count: int = 0


class GoogleEmailConnectResponse(BaseModel):
    authorization_url: str
    redirect_uri: str
    state: str


class GoogleCalendarStatusResponse(BaseModel):
    provider: str = "google_calendar"
    status: str
    configured: bool
    connected: bool
    sync_enabled: bool
    calendar_id: str | None = None
    auth_url_available: bool = False
    redirect_uri: str | None = None
    default_calendar_id: str = "primary"
    agenda_days: int = 7
    event_limit: int = 20
    scopes: list[str] = Field(default_factory=list)
    last_sync_at: str | None = None
    last_error: str | None = None
    detail: str


class GoogleCalendarEventRecord(BaseModel):
    id: str
    calendar_id: str
    status: str
    summary: str
    description: str | None = None
    location: str | None = None
    html_link: str | None = None
    conference_link: str | None = None
    organizer_email: str | None = None
    creator_email: str | None = None
    attendees: list[str] = Field(default_factory=list)
    start_at: str | None = None
    end_at: str | None = None
    start_date: str | None = None
    end_date: str | None = None
    is_all_day: bool = False
    created_at: str | None = None
    updated_at: str | None = None


class GoogleCalendarEventFields(BaseModel):
    summary: str
    description: str | None = None
    location: str | None = None
    attendees: list[str] = Field(default_factory=list)
    calendar_id: str | None = None
    is_all_day: bool = False
    start_at: str | None = None
    end_at: str | None = None
    start_date: str | None = None
    end_date: str | None = None

    @field_validator("summary")
    @classmethod
    def normalize_calendar_summary(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("summary must not be empty")
        return cleaned

    @field_validator(
        "description",
        "location",
        "calendar_id",
        "start_at",
        "end_at",
        "start_date",
        "end_date",
    )
    @classmethod
    def normalize_optional_calendar_text(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        return cleaned or None

    @field_validator("attendees")
    @classmethod
    def normalize_calendar_attendees(cls, value: list[str]) -> list[str]:
        normalized: list[str] = []
        seen: set[str] = set()
        for item in value:
            cleaned = item.strip()
            key = cleaned.casefold()
            if cleaned and key not in seen:
                normalized.append(cleaned)
                seen.add(key)
        return normalized


class GoogleCalendarEventCreateRequest(GoogleCalendarEventFields):
    pass


class GoogleCalendarEventUpdateRequest(BaseModel):
    summary: str | None = None
    description: str | None = None
    location: str | None = None
    attendees: list[str] | None = None
    calendar_id: str | None = None
    is_all_day: bool = False
    start_at: str | None = None
    end_at: str | None = None
    start_date: str | None = None
    end_date: str | None = None

    @field_validator("summary")
    @classmethod
    def normalize_optional_calendar_summary(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("summary must not be empty")
        return cleaned

    @field_validator(
        "description",
        "location",
        "calendar_id",
        "start_at",
        "end_at",
        "start_date",
        "end_date",
    )
    @classmethod
    def normalize_optional_calendar_update_text(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        return cleaned or None

    @field_validator("attendees")
    @classmethod
    def normalize_optional_calendar_update_attendees(
        cls,
        value: list[str] | None,
    ) -> list[str] | None:
        if value is None:
            return None
        normalized: list[str] = []
        seen: set[str] = set()
        for item in value:
            cleaned = item.strip()
            key = cleaned.casefold()
            if cleaned and key not in seen:
                normalized.append(cleaned)
                seen.add(key)
        return normalized


class GoogleCalendarEventListResponse(BaseModel):
    account: GoogleCalendarStatusResponse
    calendar_id: str = "primary"
    start_date: str
    end_date: str
    query: str = ""
    time_zone: str | None = None
    items: list[GoogleCalendarEventRecord] = Field(default_factory=list)
    count: int = 0


class GoogleCalendarConnectResponse(BaseModel):
    authorization_url: str
    redirect_uri: str
    state: str


class GoogleCalendarDeleteResponse(BaseModel):
    status: str
    event_id: str
    calendar_id: str


class EmailToTaskRequest(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    title: str | None = None
    priority: TaskPriority = TaskPriority.MEDIUM
    scheduled_date: str | None = None
    due_at: str | None = None
    tags: list[str] = Field(default_factory=list)

    @field_validator("title")
    @classmethod
    def normalize_optional_title(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        return cleaned or None

    @field_validator("tags")
    @classmethod
    def normalize_email_tags(cls, value: list[str]) -> list[str]:
        return [item.strip() for item in value if item and item.strip()]


class MemoryItemRecord(BaseModel):
    id: str
    category: str
    content: str
    confidence: float
    status: str
    metadata: dict[str, Any] = Field(default_factory=dict)
    source_conversation_id: str | None = None
    created_at: str
    updated_at: str


class MemoryListResponse(BaseModel):
    items: list[MemoryItemRecord] = Field(default_factory=list)
    count: int = 0


class TaskActionReport(BaseModel):
    type: str
    status: str
    task_id: str | None = None
    title: str | None = None
    detail: str | None = None


class ChatCard(BaseModel):
    type: str
    payload: dict[str, Any] = Field(default_factory=dict)


class ChatRequest(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    message: str = Field(min_length=1)
    conversation_id: str | None = None
    session_id: str | None = None
    mode: str = "text"
    selected_date: str | None = None
    include_voice: bool = True
    voice_mode: bool = False
    notes_context: str | None = None


class ChatResponse(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    conversation_id: str
    reply_text: str
    emotion: AssistantEmotion
    animation_hint: AnimationHint
    speak: bool
    audio_url: str | None = None
    task_actions: list[TaskActionReport] = Field(default_factory=list)
    cards: list[ChatCard] = Field(default_factory=list)
    route: str | None = None
    provider: str | None = None
    latency_ms: int | None = None
    token_usage: dict[str, Any] = Field(default_factory=dict)
    fallback_used: bool = False
    plan_id: str | None = None


class ChatConversationSummary(BaseModel):
    conversation_id: str
    mode: str
    created_at: str
    updated_at: str
    message_count: int = 0
    last_message_preview: str = ""
    last_message_role: str | None = None
    last_message_at: str | None = None
    summary_text: str | None = None


class ChatConversationListResponse(BaseModel):
    items: list[ChatConversationSummary] = Field(default_factory=list)
    count: int = 0


class ChatMessageRecord(BaseModel):
    id: str
    conversation_id: str
    role: str
    content: str
    emotion: str | None = None
    animation_hint: str | None = None
    metadata: dict[str, Any] = Field(default_factory=dict)
    created_at: str


class ChatConversationDetailResponse(BaseModel):
    conversation_id: str
    mode: str
    created_at: str
    updated_at: str
    summary_text: str | None = None
    message_count: int = 0
    messages: list[ChatMessageRecord] = Field(default_factory=list)


class SpeechSttResponse(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    text: str
    language: str
    confidence: float = 0.0


class SpeechTtsRequest(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    text: str = Field(min_length=1)
    voice: str | None = None
    cache: bool = True


class SpeechTtsResponse(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    audio_url: str
    duration_ms: int
    cached: bool


class SettingsPayload(BaseModel):
    model_config = ConfigDict(use_enum_values=True)

    voice: dict[str, Any] = Field(default_factory=dict)
    model: dict[str, Any] = Field(default_factory=dict)
    window_mode: dict[str, Any] = Field(default_factory=dict)
    avatar: dict[str, Any] = Field(default_factory=dict)
    reminder: dict[str, Any] = Field(default_factory=dict)
    startup: dict[str, Any] = Field(default_factory=dict)
    memory: dict[str, Any] = Field(default_factory=dict)
    google_email: dict[str, Any] = Field(default_factory=dict)
    google_calendar: dict[str, Any] = Field(default_factory=dict)


class WardrobeValidationIssue(BaseModel):
    severity: str
    code: str
    message: str
    slots: list[str] = Field(default_factory=list)
    item_ids: list[str] = Field(default_factory=list)


def _normalize_unique_text_list(value: list[str] | None) -> list[str]:
    if value is None:
        return []
    normalized: list[str] = []
    seen: set[str] = set()
    for item in value:
        cleaned = item.strip()
        key = cleaned.casefold()
        if cleaned and key not in seen:
            normalized.append(cleaned)
            seen.add(key)
    return normalized


def _normalize_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    cleaned = value.strip()
    return cleaned or None


class WardrobeItemFields(BaseModel):
    display_name: str = Field(min_length=1, max_length=200)
    slot: str = Field(min_length=1, max_length=80)
    source: str = "user"
    source_asset_path: str | None = None
    prefab_asset_path: str | None = None
    material_asset_paths: list[str] = Field(default_factory=list)
    thumbnail_asset_path: str | None = None
    occupies_slots: list[str] = Field(default_factory=list)
    blocks_slots: list[str] = Field(default_factory=list)
    requires_slots: list[str] = Field(default_factory=list)
    compatible_tags: list[str] = Field(default_factory=list)
    incompatible_tags: list[str] = Field(default_factory=list)
    hide_body_regions: list[str] = Field(default_factory=list)
    anchor_type: str = "None"
    anchor_bone_name: str | None = None

    @field_validator("display_name", "slot", "source", "anchor_type")
    @classmethod
    def normalize_required_wardrobe_text(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("value must not be empty")
        return cleaned

    @field_validator(
        "source_asset_path",
        "prefab_asset_path",
        "thumbnail_asset_path",
        "anchor_bone_name",
    )
    @classmethod
    def normalize_optional_wardrobe_text(cls, value: str | None) -> str | None:
        return _normalize_optional_text(value)

    @field_validator(
        "material_asset_paths",
        "occupies_slots",
        "blocks_slots",
        "requires_slots",
        "compatible_tags",
        "incompatible_tags",
        "hide_body_regions",
    )
    @classmethod
    def normalize_wardrobe_lists(cls, value: list[str]) -> list[str]:
        return _normalize_unique_text_list(value)


class WardrobeItemCreateRequest(WardrobeItemFields):
    item_id: str = Field(min_length=1, max_length=120)

    @field_validator("item_id")
    @classmethod
    def normalize_item_id(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("item_id must not be empty")
        return cleaned


class WardrobeItemUpdateRequest(BaseModel):
    display_name: str | None = Field(default=None, min_length=1, max_length=200)
    slot: str | None = Field(default=None, min_length=1, max_length=80)
    source: str | None = None
    source_asset_path: str | None = None
    prefab_asset_path: str | None = None
    material_asset_paths: list[str] | None = None
    thumbnail_asset_path: str | None = None
    occupies_slots: list[str] | None = None
    blocks_slots: list[str] | None = None
    requires_slots: list[str] | None = None
    compatible_tags: list[str] | None = None
    incompatible_tags: list[str] | None = None
    hide_body_regions: list[str] | None = None
    anchor_type: str | None = None
    anchor_bone_name: str | None = None

    @field_validator("display_name", "slot", "source", "anchor_type")
    @classmethod
    def normalize_optional_required_text(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("value must not be empty")
        return cleaned

    @field_validator(
        "source_asset_path",
        "prefab_asset_path",
        "thumbnail_asset_path",
        "anchor_bone_name",
    )
    @classmethod
    def normalize_optional_update_text(cls, value: str | None) -> str | None:
        return _normalize_optional_text(value)

    @field_validator(
        "material_asset_paths",
        "occupies_slots",
        "blocks_slots",
        "requires_slots",
        "compatible_tags",
        "incompatible_tags",
        "hide_body_regions",
    )
    @classmethod
    def normalize_optional_update_lists(cls, value: list[str] | None) -> list[str] | None:
        if value is None:
            return None
        return _normalize_unique_text_list(value)


class WardrobeItemRecord(WardrobeItemCreateRequest):
    created_at: str
    updated_at: str
    validation_issues: list[WardrobeValidationIssue] = Field(default_factory=list)
    sync_status: str = "ready"


class WardrobeOutfitFields(BaseModel):
    display_name: str = Field(min_length=1, max_length=200)
    source: str = "user"
    thumbnail_asset_path: str | None = None
    slot_assignments: dict[str, str] = Field(default_factory=dict)

    @field_validator("display_name", "source")
    @classmethod
    def normalize_required_outfit_text(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("value must not be empty")
        return cleaned

    @field_validator("thumbnail_asset_path")
    @classmethod
    def normalize_optional_outfit_text(cls, value: str | None) -> str | None:
        return _normalize_optional_text(value)

    @field_validator("slot_assignments")
    @classmethod
    def normalize_slot_assignments(cls, value: dict[str, str]) -> dict[str, str]:
        normalized: dict[str, str] = {}
        for slot_key, item_id in value.items():
            clean_slot = slot_key.strip()
            clean_item = item_id.strip()
            if clean_slot and clean_item:
                normalized[clean_slot] = clean_item
        return normalized


class WardrobeOutfitCreateRequest(WardrobeOutfitFields):
    outfit_id: str = Field(min_length=1, max_length=120)

    @field_validator("outfit_id")
    @classmethod
    def normalize_outfit_id(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("outfit_id must not be empty")
        return cleaned


class WardrobeOutfitUpdateRequest(BaseModel):
    display_name: str | None = Field(default=None, min_length=1, max_length=200)
    source: str | None = None
    thumbnail_asset_path: str | None = None
    slot_assignments: dict[str, str] | None = None

    @field_validator("display_name", "source")
    @classmethod
    def normalize_optional_outfit_required_text(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("value must not be empty")
        return cleaned

    @field_validator("thumbnail_asset_path")
    @classmethod
    def normalize_optional_outfit_update_text(cls, value: str | None) -> str | None:
        return _normalize_optional_text(value)

    @field_validator("slot_assignments")
    @classmethod
    def normalize_optional_slot_assignments(
        cls,
        value: dict[str, str] | None,
    ) -> dict[str, str] | None:
        if value is None:
            return None
        normalized: dict[str, str] = {}
        for slot_key, item_id in value.items():
            clean_slot = slot_key.strip()
            clean_item = item_id.strip()
            if clean_slot and clean_item:
                normalized[clean_slot] = clean_item
        return normalized


class WardrobeOutfitRecord(WardrobeOutfitCreateRequest):
    created_at: str
    updated_at: str
    validation_issues: list[WardrobeValidationIssue] = Field(default_factory=list)
    sync_status: str = "ready"


class WardrobeSlotRecord(BaseModel):
    key: str
    unity_enum: str
    display_name: str
    reserved: bool = False
    item_count: int = 0
    outfit_count: int = 0
    notes: list[str] = Field(default_factory=list)


class WardrobeSummary(BaseModel):
    slot_count: int = 0
    reserved_slot_count: int = 0
    item_count: int = 0
    outfit_count: int = 0
    warning_count: int = 0
    error_count: int = 0
    ready_item_count: int = 0
    ready_outfit_count: int = 0


class WardrobeSnapshotResponse(BaseModel):
    version: int = 1
    updated_at: str
    registry_path: str
    slot_taxonomy_version: int = 1
    slots: list[WardrobeSlotRecord] = Field(default_factory=list)
    items: list[WardrobeItemRecord] = Field(default_factory=list)
    outfits: list[WardrobeOutfitRecord] = Field(default_factory=list)
    summary: WardrobeSummary = Field(default_factory=WardrobeSummary)


class WardrobeImportRequest(BaseModel):
    mode: str = "merge"
    items: list[WardrobeItemCreateRequest] = Field(default_factory=list)
    outfits: list[WardrobeOutfitCreateRequest] = Field(default_factory=list)

    @field_validator("mode")
    @classmethod
    def normalize_import_mode(cls, value: str) -> str:
        cleaned = value.strip().lower()
        if cleaned not in {"merge", "replace"}:
            raise ValueError("mode must be either 'merge' or 'replace'")
        return cleaned


class HealthResponse(BaseModel):
    model_config = ConfigDict(use_enum_values=True)
    status: str
    service: str
    version: str
    database: dict[str, Any]
    runtimes: dict[str, Any]
    degraded_features: list[str]
    logs: dict[str, Any] = Field(default_factory=dict)
    recovery_actions: list[str] = Field(default_factory=list)


class AssistantStreamMessage(BaseModel):
    type: str
    session_id: str | None = None
    conversation_id: str | None = None
    message: str | None = None
    selected_date: str | None = None
    voice_mode: bool = False
    notes_context: str | None = None
    audio_base64: str | None = None
    language: str | None = None


class AssistantPlanPayload(BaseModel):
    intent: str
    task_type: str
    reasoning_summary: str
    actionable_plan: list[str] = Field(default_factory=list)
    task_actions: list[dict[str, Any]] = Field(default_factory=list)
    spoken_brief: str
    ui_cards: list[dict[str, Any]] = Field(default_factory=list)
    memory_candidates: list[dict[str, Any]] = Field(default_factory=list)


class BrowserAutomationTemplateField(BaseModel):
    key: str
    label: str
    required: bool = True
    placeholder: str | None = None
    help_text: str | None = None


class BrowserAutomationTemplateRecord(BaseModel):
    template_id: str
    title: str
    description: str
    step_count: int
    fields: list[BrowserAutomationTemplateField] = Field(default_factory=list)


class BrowserAutomationTemplateListResponse(BaseModel):
    items: list[BrowserAutomationTemplateRecord] = Field(default_factory=list)
    count: int = 0


class BrowserAutomationRunCreateRequest(BaseModel):
    template_id: str = Field(min_length=1, max_length=80)
    title: str = Field(min_length=1, max_length=200)
    goal: str = Field(min_length=1, max_length=500)
    inputs: dict[str, Any] = Field(default_factory=dict)

    @field_validator("template_id", "title", "goal")
    @classmethod
    def normalize_browser_automation_text(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("value must not be empty")
        return cleaned


class BrowserAutomationApprovalRequest(BaseModel):
    approval_note: str | None = Field(default=None, max_length=500)

    @field_validator("approval_note")
    @classmethod
    def normalize_optional_approval_note(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        return cleaned or None


class BrowserAutomationRejectRequest(BaseModel):
    reason: str = Field(min_length=1, max_length=500)

    @field_validator("reason")
    @classmethod
    def normalize_reject_reason(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("reason must not be empty")
        return cleaned


class BrowserAutomationCancelRequest(BaseModel):
    reason: str | None = Field(default=None, max_length=500)

    @field_validator("reason")
    @classmethod
    def normalize_optional_cancel_reason(cls, value: str | None) -> str | None:
        if value is None:
            return None
        cleaned = value.strip()
        return cleaned or None


class BrowserAutomationStepRecord(BaseModel):
    id: str
    position: int
    action_type: str
    title: str
    description: str
    status: str
    requires_approval: bool = True
    url: str | None = None
    approval_note: str | None = None
    recovery_notes: str = ""
    result: dict[str, Any] = Field(default_factory=dict)
    updated_at: str
    completed_at: str | None = None


class BrowserAutomationLogRecord(BaseModel):
    id: str
    run_id: str
    step_id: str | None = None
    level: str
    code: str
    message: str
    payload: dict[str, Any] = Field(default_factory=dict)
    created_at: str


class BrowserAutomationRunSummary(BaseModel):
    id: str
    template_id: str
    title: str
    goal: str
    status: str
    current_step_index: int = 0
    step_count: int = 0
    pending_step_title: str | None = None
    last_log_message: str | None = None
    created_at: str
    updated_at: str
    completed_at: str | None = None
    cancelled_at: str | None = None


class BrowserAutomationRunDetail(BrowserAutomationRunSummary):
    start_url: str | None = None
    inputs: dict[str, Any] = Field(default_factory=dict)
    steps: list[BrowserAutomationStepRecord] = Field(default_factory=list)
    logs: list[BrowserAutomationLogRecord] = Field(default_factory=list)


class BrowserAutomationRunListResponse(BaseModel):
    items: list[BrowserAutomationRunSummary] = Field(default_factory=list)
    count: int = 0

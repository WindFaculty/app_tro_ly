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
    mode: str = "text"
    selected_date: str | None = None
    include_voice: bool = True


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

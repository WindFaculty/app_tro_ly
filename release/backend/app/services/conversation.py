from __future__ import annotations

import re
from dataclasses import dataclass, field
from datetime import date, datetime, timedelta
from typing import Any

from app.core.events import EventBus
from app.core.ids import make_id
from app.core.time import iso_date, iso_datetime, now_local, parse_date
from app.db.repository import SQLiteRepository
from app.models.enums import AnimationHint, AssistantEmotion, TaskPriority, TaskStatus
from app.models.schemas import (
    ChatCard,
    ChatRequest,
    ChatResponse,
    CompleteTaskRequest,
    RescheduleTaskRequest,
    TaskActionReport,
    TaskCreateRequest,
    TaskUpdateRequest,
)
from app.services.llm import LlmService
from app.services.planner import PlannerService
from app.services.settings import SettingsService
from app.services.speech import SpeechService
from app.services.tasks import TaskService


@dataclass
class IntentResult:
    kind: str
    title: str | None = None
    date_value: date | None = None
    start_at: str | None = None
    due_at: str | None = None
    repeat_rule: str = "none"
    priority: str | None = None
    cards: list[ChatCard] = field(default_factory=list)


class ConversationService:
    def __init__(
        self,
        repository: SQLiteRepository,
        task_service: TaskService,
        planner_service: PlannerService,
        speech_service: SpeechService,
        settings_service: SettingsService,
        llm_service: LlmService,
        event_bus: EventBus,
    ) -> None:
        self._repository = repository
        self._task_service = task_service
        self._planner_service = planner_service
        self._speech_service = speech_service
        self._settings_service = settings_service
        self._llm_service = llm_service
        self._event_bus = event_bus

    async def handle_chat(self, request: ChatRequest) -> ChatResponse:
        now = now_local()
        conversation_id = self._ensure_conversation(request.conversation_id, request.mode, now)
        self._save_message(conversation_id, "user", request.message, None, None)
        self._repository.touch_conversation(conversation_id, iso_datetime(now))
        if request.selected_date:
            self._repository.set_session_state("selected_date", request.selected_date)

        await self._event_bus.publish(
            {
                "type": "assistant_state_changed",
                "state": "thinking",
                "emotion": AssistantEmotion.THINKING.value,
                "animation_hint": AnimationHint.THINK.value,
            }
        )

        intent = self._parse_intent(request.message, request.selected_date)
        reply_text, emotion, hint, actions, cards = self._apply_intent(intent)
        refined = self._llm_service.refine_reply(
            request.message,
            {"reply": reply_text, "actions": [action.model_dump() for action in actions]},
        )
        if refined:
            reply_text = refined

        audio_url = None
        speak = bool(request.include_voice and self._settings_service.get()["voice"].get("speak_replies", True))
        utterance_id = make_id("utt")
        if speak:
            try:
                await self._event_bus.publish({"type": "speech_started", "utterance_id": utterance_id})
                speech = self._speech_service.synthesize(
                    reply_text,
                    self._settings_service.get()["voice"].get("tts_voice"),
                )
                audio_url = speech["audio_url"]
            except RuntimeError:
                speak = False
            finally:
                await self._event_bus.publish({"type": "speech_finished", "utterance_id": utterance_id})

        self._save_message(conversation_id, "assistant", reply_text, emotion.value, hint.value)
        self._repository.touch_conversation(conversation_id, iso_datetime(now_local()))
        await self._event_bus.publish(
            {
                "type": "assistant_state_changed",
                "state": "idle",
                "emotion": emotion.value,
                "animation_hint": hint.value,
            }
        )

        return ChatResponse(
            conversation_id=conversation_id,
            reply_text=reply_text,
            emotion=emotion,
            animation_hint=hint,
            speak=speak,
            audio_url=audio_url,
            task_actions=actions,
            cards=cards,
        )

    def _apply_intent(
        self,
        intent: IntentResult,
    ) -> tuple[str, AssistantEmotion, AnimationHint, list[TaskActionReport], list[ChatCard]]:
        actions: list[TaskActionReport] = []
        cards = list(intent.cards)

        if intent.kind == "lookup_day":
            summary = self._planner_service.daily_summary(intent.date_value or now_local().date())
            cards.append(ChatCard(type="today_summary", payload=summary))
            return summary["text"], AssistantEmotion.SERIOUS, AnimationHint.EXPLAIN, actions, cards

        if intent.kind == "lookup_week":
            summary = self._planner_service.weekly_summary(intent.date_value or now_local().date())
            cards.append(ChatCard(type="week_summary", payload=summary))
            return summary["text"], AssistantEmotion.SERIOUS, AnimationHint.EXPLAIN, actions, cards

        if intent.kind == "lookup_overdue":
            summary = self._planner_service.overdue_summary()
            cards.append(ChatCard(type="overdue_summary", payload=summary))
            return summary["text"], AssistantEmotion.WARNING, AnimationHint.ALERT, actions, cards

        if intent.kind == "lookup_free_time":
            summary = self._planner_service.free_slots(intent.date_value or now_local().date())
            cards.append(ChatCard(type="free_time", payload=summary))
            return summary["text"], AssistantEmotion.NEUTRAL, AnimationHint.EXPLAIN, actions, cards

        if intent.kind == "lookup_urgency":
            summary = self._planner_service.urgency_summary()
            cards.append(ChatCard(type="urgency_summary", payload=summary))
            return summary["text"], AssistantEmotion.SERIOUS, AnimationHint.EXPLAIN, actions, cards

        if intent.kind == "create_task":
            task = self._task_service.create_task(
                TaskCreateRequest(
                    title=intent.title or "Việc mới",
                    status=TaskStatus.PLANNED if intent.date_value else TaskStatus.INBOX,
                    priority=intent.priority or TaskPriority.MEDIUM.value,
                    scheduled_date=iso_date(intent.date_value),
                    start_at=intent.start_at,
                    due_at=intent.due_at,
                    repeat_rule=intent.repeat_rule,
                )
            )
            actions.append(
                TaskActionReport(
                    type="create_task",
                    status="applied",
                    task_id=task.id,
                    title=task.title,
                    detail="Task created from natural language",
                )
            )
            cards.append(ChatCard(type="task_action", payload={"task": task.model_dump()}))
            reply = f"Đã tạo việc '{task.title}'"
            reply += f" cho ngày {task.scheduled_date}." if task.scheduled_date else " vào Inbox."
            return reply, AssistantEmotion.HAPPY, AnimationHint.CONFIRM, actions, cards

        if intent.kind == "complete_task":
            task = self._match_task_or_fail(intent.title)
            updated = self._task_service.complete_task(
                task["id"],
                CompleteTaskRequest(completed_at=iso_datetime(now_local())),
            )
            actions.append(
                TaskActionReport(
                    type="complete_task",
                    status="applied",
                    task_id=updated.id,
                    title=updated.title,
                )
            )
            cards.append(ChatCard(type="task_action", payload={"task": updated.model_dump()}))
            return (
                f"Đã đánh dấu xong việc '{updated.title}'.",
                AssistantEmotion.HAPPY,
                AnimationHint.CONFIRM,
                actions,
                cards,
            )

        if intent.kind == "reschedule_task":
            task = self._match_task_or_fail(intent.title)
            updated = self._task_service.reschedule_task(
                task["id"],
                RescheduleTaskRequest(
                    scheduled_date=iso_date(intent.date_value),
                    start_at=intent.start_at,
                ),
            )
            actions.append(
                TaskActionReport(
                    type="reschedule_task",
                    status="applied",
                    task_id=updated.id,
                    title=updated.title,
                )
            )
            cards.append(ChatCard(type="task_action", payload={"task": updated.model_dump()}))
            return (
                f"Đã dời '{updated.title}' sang {updated.scheduled_date}.",
                AssistantEmotion.HAPPY,
                AnimationHint.CONFIRM,
                actions,
                cards,
            )

        if intent.kind == "priority_task":
            task = self._match_task_or_fail(intent.title)
            updated = self._task_service.update_task(
                task["id"],
                TaskUpdateRequest(priority=intent.priority),
            )
            actions.append(
                TaskActionReport(
                    type="priority_task",
                    status="applied",
                    task_id=updated.id,
                    title=updated.title,
                )
            )
            cards.append(ChatCard(type="task_action", payload={"task": updated.model_dump()}))
            return (
                f"Đã tăng ưu tiên cho '{updated.title}' lên mức {updated.priority}.",
                AssistantEmotion.HAPPY,
                AnimationHint.CONFIRM,
                actions,
                cards,
            )

        summary = self._planner_service.daily_summary(parse_date(self._selected_date()) or now_local().date())
        cards.append(ChatCard(type="today_summary", payload=summary))
        return summary["text"], AssistantEmotion.NEUTRAL, AnimationHint.EXPLAIN, actions, cards

    def _parse_intent(self, message: str, selected_date: str | None) -> IntentResult:
        lowered = self._sanitize(message)
        if self._looks_like_create(lowered):
            return self._parse_create(lowered)
        if self._looks_like_complete(lowered):
            return self._parse_complete(lowered)
        if self._looks_like_reschedule(lowered):
            return self._parse_reschedule(lowered)
        if self._looks_like_priority(lowered):
            return self._parse_priority(lowered)
        if any(token in lowered for token in ("quá hạn", "overdue")):
            return IntentResult(kind="lookup_overdue")
        if any(token in lowered for token in ("gấp nhất", "urgent", "quan trọng nhất")):
            return IntentResult(kind="lookup_urgency")
        if any(token in lowered for token in ("rảnh lúc nào", "free when", "free time")):
            return IntentResult(kind="lookup_free_time", date_value=self._extract_date(lowered, selected_date))
        if any(token in lowered for token in ("tuần này", "this week", "7 ngày")):
            return IntentResult(kind="lookup_week", date_value=self._extract_date(lowered, selected_date))
        if any(token in lowered for token in ("hôm nay", "today", "ngày mai", "tomorrow")):
            return IntentResult(kind="lookup_day", date_value=self._extract_date(lowered, selected_date))
        return IntentResult(kind="lookup_day", date_value=self._extract_date(lowered, selected_date))

    def _looks_like_create(self, text: str) -> bool:
        return any(text.startswith(prefix) for prefix in ("thêm", "tạo", "nhắc tôi", "add ", "create "))

    def _looks_like_complete(self, text: str) -> bool:
        return any(token in text for token in ("đánh dấu", "xong", "hoàn thành", "complete"))

    def _looks_like_reschedule(self, text: str) -> bool:
        return any(token in text for token in ("dời", "chuyển", "move", "reschedule"))

    def _looks_like_priority(self, text: str) -> bool:
        return "ưu tiên" in text or "priority" in text

    def _parse_create(self, text: str) -> IntentResult:
        title = self._extract_title(text, prefixes=("thêm task", "thêm việc", "thêm", "tạo task", "tạo việc", "tạo", "nhắc tôi", "add task", "add", "create task", "create"))
        target_date = self._extract_date(text, None, default_to_today=False)
        start_at = self._extract_datetime(text, target_date)
        repeat_rule = self._extract_repeat_rule(text)
        priority = self._extract_priority(text)
        due_at = start_at if "deadline" in text or "nộp" in text else None
        return IntentResult(
            kind="create_task",
            title=title,
            date_value=target_date,
            start_at=iso_datetime(start_at) if start_at else None,
            due_at=iso_datetime(due_at) if due_at else None,
            repeat_rule=repeat_rule,
            priority=priority,
        )

    def _parse_complete(self, text: str) -> IntentResult:
        title = self._extract_title(
            text,
            prefixes=("đánh dấu", "hoàn thành", "complete", "xong"),
            stops=("là xong", "là done", "hoàn thành"),
        )
        return IntentResult(kind="complete_task", title=title)

    def _parse_reschedule(self, text: str) -> IntentResult:
        title_match = re.search(r"(?:dời|chuyển|move|reschedule)\s+(.*?)\s+(?:sang|to)\s+", text)
        title = title_match.group(1).strip() if title_match else self._extract_title(text, prefixes=("dời", "chuyển", "move", "reschedule"))
        target_date = self._extract_date(text, None, default_to_today=False)
        start_at = self._extract_datetime(text, target_date)
        return IntentResult(
            kind="reschedule_task",
            title=title,
            date_value=target_date,
            start_at=iso_datetime(start_at) if start_at else None,
        )

    def _parse_priority(self, text: str) -> IntentResult:
        title_match = re.search(r"(?:tăng ưu tiên|priority)\s+(.*?)\s+(?:lên|to)\s+", text)
        title = title_match.group(1).strip() if title_match else self._extract_title(text, prefixes=("tăng ưu tiên", "priority"))
        return IntentResult(kind="priority_task", title=title, priority=self._extract_priority(text) or TaskPriority.HIGH.value)

    def _extract_title(
        self,
        text: str,
        *,
        prefixes: tuple[str, ...],
        stops: tuple[str, ...] = ("lúc", "vao", "vào", "sang", "to", "ngày", "mai", "hôm nay", "thứ"),
    ) -> str:
        cleaned = text
        for prefix in prefixes:
            if cleaned.startswith(prefix):
                cleaned = cleaned[len(prefix) :].strip()
                break
        for stop in stops:
            match = re.search(rf"\s+{re.escape(stop)}(?:\s+|$)", cleaned)
            if match:
                return cleaned[: match.start()].strip(" .")
        return cleaned.strip(" .")

    def _extract_priority(self, text: str) -> str | None:
        mapping = {
            "critical": TaskPriority.CRITICAL.value,
            "khẩn cấp": TaskPriority.CRITICAL.value,
            "cao": TaskPriority.HIGH.value,
            "high": TaskPriority.HIGH.value,
            "trung bình": TaskPriority.MEDIUM.value,
            "medium": TaskPriority.MEDIUM.value,
            "thấp": TaskPriority.LOW.value,
            "low": TaskPriority.LOW.value,
        }
        for key, value in mapping.items():
            if key in text:
                return value
        return None

    def _extract_repeat_rule(self, text: str) -> str:
        if "mỗi tối" in text or "mỗi ngày" in text or "daily" in text:
            return "daily"
        if "ngày trong tuần" in text or "weekdays" in text:
            return "weekdays"
        if "mỗi tuần" in text or "weekly" in text:
            return "weekly"
        if "mỗi tháng" in text or "monthly" in text:
            return "monthly"
        return "none"

    def _extract_date(self, text: str, selected_date: str | None, *, default_to_today: bool = True) -> date | None:
        today = now_local().date()
        if "ngày mai" in text or re.search(r"\bmai\b", text) or "tomorrow" in text:
            return today + timedelta(days=1)
        if "hôm nay" in text or "today" in text:
            return today

        weekdays = {
            "thứ hai": 0,
            "thứ ba": 1,
            "thứ tư": 2,
            "thứ năm": 3,
            "thứ sáu": 4,
            "thứ bảy": 5,
            "chủ nhật": 6,
            "monday": 0,
            "tuesday": 1,
            "wednesday": 2,
            "thursday": 3,
            "friday": 4,
            "saturday": 5,
            "sunday": 6,
        }
        for label, weekday in weekdays.items():
            if label in text:
                delta = (weekday - today.weekday()) % 7
                delta = 7 if delta == 0 else delta
                return today + timedelta(days=delta)

        iso_match = re.search(r"\b(20\d{2}-\d{2}-\d{2})\b", text)
        if iso_match:
            return date.fromisoformat(iso_match.group(1))
        if selected_date:
            return parse_date(selected_date) or today
        return today if default_to_today else None

    def _extract_datetime(self, text: str, target_date: date | None) -> datetime | None:
        if target_date is None:
            target_date = now_local().date()
        hour = None
        minute = 0
        match = re.search(r"(\d{1,2})(?::(\d{2}))?", text)
        if match:
            hour = int(match.group(1))
            if match.group(2):
                minute = int(match.group(2))
        elif "chiều" in text:
            hour = 15
        elif "tối" in text:
            hour = 19
        elif "sáng" in text:
            hour = 9

        if hour is None:
            return None
        if ("chiều" in text or "pm" in text or "tối" in text) and hour < 12:
            hour += 12
        return datetime.combine(target_date, datetime.min.time()).replace(hour=hour, minute=minute)

    def _sanitize(self, value: str) -> str:
        return " ".join(value.casefold().replace("?", " ").replace("!", " ").split())

    def _match_task_or_fail(self, title: str | None) -> dict[str, Any]:
        task = self._task_service.search_task(title or "")
        if task is None:
            raise LookupError(f"Could not find task matching '{title}'")
        return task

    def _selected_date(self) -> str | None:
        return self._repository.get_session_state().get("selected_date")

    def _ensure_conversation(self, conversation_id: str | None, mode: str, now: datetime) -> str:
        if conversation_id and self._repository.get_conversation(conversation_id):
            return conversation_id
        new_id = make_id("conv")
        self._repository.create_conversation(
            {
                "id": new_id,
                "mode": mode,
                "created_at": iso_datetime(now),
                "updated_at": iso_datetime(now),
            }
        )
        return new_id

    def _save_message(
        self,
        conversation_id: str,
        role: str,
        content: str,
        emotion: str | None,
        animation_hint: str | None,
    ) -> None:
        self._repository.add_message(
            {
                "id": make_id("msg"),
                "conversation_id": conversation_id,
                "role": role,
                "content": content,
                "emotion": emotion,
                "animation_hint": animation_hint,
                "metadata_json": "{}",
                "created_at": iso_datetime(now_local()),
            }
        )

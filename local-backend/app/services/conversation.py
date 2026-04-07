from __future__ import annotations

from app.db.repository import SQLiteRepository
from app.models.schemas import (
    ChatConversationDetailResponse,
    ChatConversationListResponse,
    ChatConversationSummary,
    ChatMessageRecord,
    ChatRequest,
    ChatResponse,
)
from app.services.assistant_orchestrator import AssistantOrchestrator


class ConversationService:
    def __init__(
        self,
        assistant_orchestrator: AssistantOrchestrator,
        repository: SQLiteRepository,
    ) -> None:
        self._assistant_orchestrator = assistant_orchestrator
        self._repository = repository

    async def handle_chat(self, request: ChatRequest) -> ChatResponse:
        return await self._assistant_orchestrator.handle_chat(request)

    def list_conversations(self, limit: int = 20) -> ChatConversationListResponse:
        items = [
            ChatConversationSummary(
                conversation_id=item["id"],
                mode=item["mode"],
                created_at=item["created_at"],
                updated_at=item["updated_at"],
                message_count=int(item.get("message_count") or 0),
                last_message_preview=self._preview_text(item.get("last_message_content")),
                last_message_role=item.get("last_message_role"),
                last_message_at=item.get("last_message_at"),
                summary_text=item.get("summary_text"),
            )
            for item in self._repository.list_conversations(limit)
        ]
        return ChatConversationListResponse(items=items, count=len(items))

    def get_conversation_detail(self, conversation_id: str) -> ChatConversationDetailResponse | None:
        conversation = self._repository.get_conversation(conversation_id)
        if conversation is None:
            return None

        summary = self._repository.get_conversation_summary(conversation_id)
        messages = [
            ChatMessageRecord(
                id=item["id"],
                conversation_id=item["conversation_id"],
                role=item["role"],
                content=item["content"],
                emotion=item.get("emotion"),
                animation_hint=item.get("animation_hint"),
                metadata=item.get("metadata") or {},
                created_at=item["created_at"],
            )
            for item in self._repository.list_messages(conversation_id)
        ]
        return ChatConversationDetailResponse(
            conversation_id=conversation["id"],
            mode=conversation["mode"],
            created_at=conversation["created_at"],
            updated_at=conversation["updated_at"],
            summary_text=summary["summary_text"] if summary else None,
            message_count=len(messages),
            messages=messages,
        )

    @staticmethod
    def _preview_text(value: object, limit: int = 180) -> str:
        text = str(value or "").strip()
        if len(text) <= limit:
            return text
        return f"{text[: limit - 1].rstrip()}..."

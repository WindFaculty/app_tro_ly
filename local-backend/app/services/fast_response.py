from __future__ import annotations

import json
from typing import Any

from app.services.llm import LlmService


class FastResponseService:
    def __init__(self, llm_service: LlmService) -> None:
        self._llm_service = llm_service

    def compose(
        self,
        *,
        provider: str,
        user_message: str,
        factual_context: dict[str, Any],
        spoken_brief: str | None = None,
    ) -> tuple[str, dict[str, Any]]:
        reply = self._llm_service.complete(
            provider=provider,
            system_prompt=self._system_prompt(),
            user_prompt=self._build_prompt(user_message, factual_context, spoken_brief),
        )
        return reply.text, reply.token_usage

    def fallback_compose(self, *, spoken_brief: str | None, factual_context: dict[str, Any]) -> str:
        if spoken_brief:
            return spoken_brief
        summary = factual_context.get("summary") or factual_context.get("daily") or {}
        return summary.get("text") or "Minh da tong hop xong va san sang noi ngan gon lai cho ban."

    def _system_prompt(self) -> str:
        return (
            "You are the fast-response layer of a Vietnamese virtual assistant. "
            "Reply in short, natural, voice-friendly Vietnamese. "
            "If a spoken_brief exists, stay close to it."
        )

    def _build_prompt(self, user_message: str, factual_context: dict[str, Any], spoken_brief: str | None) -> str:
        return json.dumps(
            {
                "user_message": user_message,
                "spoken_brief": spoken_brief,
                "factual_context": factual_context,
            },
            ensure_ascii=False,
        )

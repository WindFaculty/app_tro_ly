from __future__ import annotations

import json
from typing import Any

import httpx

from app.core.config import Settings
from app.core.logging import get_logger

logger = get_logger("llm")


class LlmService:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    def health(self) -> dict[str, Any]:
        if self._settings.llm_provider == "gemini":
            return self._openai_compatible_health(
                provider="gemini",
                api_key=self._settings.gemini_api_key,
                base_url=self._settings.gemini_base_url,
                model=self._settings.gemini_model,
                timeout_sec=self._settings.gemini_timeout_sec,
            )
        if self._settings.llm_provider == "groq":
            return self._openai_compatible_health(
                provider="groq",
                api_key=self._settings.groq_api_key,
                base_url=self._settings.groq_base_url,
                model=self._settings.groq_model,
                timeout_sec=self._settings.groq_timeout_sec,
            )
        return self._ollama_health()

    def refine_reply(self, message: str, facts: dict[str, Any]) -> str | None:
        if self._settings.llm_provider == "gemini":
            return self._openai_compatible_refine_reply(
                provider="gemini",
                api_key=self._settings.gemini_api_key,
                base_url=self._settings.gemini_base_url,
                model=self._settings.gemini_model,
                timeout_sec=self._settings.gemini_timeout_sec,
                temperature=self._settings.gemini_temperature,
                message=message,
                facts=facts,
            )
        if self._settings.llm_provider == "groq":
            return self._openai_compatible_refine_reply(
                provider="groq",
                api_key=self._settings.groq_api_key,
                base_url=self._settings.groq_base_url,
                model=self._settings.groq_model,
                timeout_sec=self._settings.groq_timeout_sec,
                temperature=self._settings.groq_temperature,
                message=message,
                facts=facts,
            )
        return self._ollama_refine_reply(message, facts)

    def _ollama_health(self) -> dict[str, Any]:
        if not self._settings.enable_ollama:
            return {
                "available": False,
                "provider": "ollama",
                "base_url": self._settings.ollama_base_url,
                "model": self._settings.ollama_model,
                "reason": "disabled",
            }
        try:
            with httpx.Client(timeout=self._settings.ollama_timeout_sec) as client:
                response = client.get(f"{self._settings.ollama_base_url}/api/tags")
                response.raise_for_status()
            return {
                "available": True,
                "provider": "ollama",
                "base_url": self._settings.ollama_base_url,
                "model": self._settings.ollama_model,
            }
        except Exception as exc:  # pragma: no cover - network branch
            logger.warning("Ollama health check failed for %s: %s", self._settings.ollama_base_url, exc)
            return {
                "available": False,
                "provider": "ollama",
                "base_url": self._settings.ollama_base_url,
                "model": self._settings.ollama_model,
                "reason": str(exc),
            }

    def _ollama_refine_reply(self, message: str, facts: dict[str, Any]) -> str | None:
        if not self._settings.enable_ollama:
            return None

        try:
            with httpx.Client(timeout=self._settings.ollama_timeout_sec) as client:
                response = client.post(
                    f"{self._settings.ollama_base_url}/api/generate",
                    json={
                        "model": self._settings.ollama_model,
                        "prompt": self._build_prompt(message, facts),
                        "stream": False,
                    },
                )
                response.raise_for_status()
                data = response.json()
        except Exception as exc:
            logger.warning("Reply refinement fell back to rule-based response: %s", exc)
            return None
        result = (data.get("response") or "").strip()
        return result or None

    def _openai_compatible_health(
        self,
        *,
        provider: str,
        api_key: str | None,
        base_url: str,
        model: str,
        timeout_sec: float,
    ) -> dict[str, Any]:
        if not api_key:
            return {
                "available": False,
                "provider": provider,
                "base_url": base_url,
                "model": model,
                "reason": "missing_api_key",
            }
        try:
            with httpx.Client(timeout=timeout_sec) as client:
                response = client.get(
                    f"{base_url}/models",
                    headers=self._bearer_headers(api_key),
                )
                response.raise_for_status()
            return {
                "available": True,
                "provider": provider,
                "base_url": base_url,
                "model": model,
            }
        except Exception as exc:  # pragma: no cover - network branch
            logger.warning("%s health check failed for %s: %s", provider.title(), base_url, exc)
            return {
                "available": False,
                "provider": provider,
                "base_url": base_url,
                "model": model,
                "reason": str(exc),
            }

    def _openai_compatible_refine_reply(
        self,
        *,
        provider: str,
        api_key: str | None,
        base_url: str,
        model: str,
        timeout_sec: float,
        temperature: float,
        message: str,
        facts: dict[str, Any],
    ) -> str | None:
        if not api_key:
            return None

        try:
            with httpx.Client(timeout=timeout_sec) as client:
                response = client.post(
                    f"{base_url}/chat/completions",
                    headers=self._bearer_headers(api_key),
                    json={
                        "model": model,
                        "temperature": temperature,
                        "stream": False,
                        "messages": [
                            {"role": "system", "content": self._system_prompt()},
                            {"role": "user", "content": self._build_prompt(message, facts)},
                        ],
                    },
                )
                response.raise_for_status()
                data = response.json()
        except Exception as exc:
            logger.warning("Reply refinement fell back to rule-based response: %s", exc)
            return None

        choices = data.get("choices") or []
        if not choices:
            logger.warning("%s reply refinement returned no choices.", provider.title())
            return None
        result = ((((choices[0] or {}).get("message") or {}).get("content")) or "").strip()
        return result or None

    def _system_prompt(self) -> str:
        return (
            "Ban la tro ly lap ke hoach cong viec noi bo. "
            "Hay viet lai phan tra loi ngan gon, ro rang, uu tien tieng Viet, "
            "khong them thong tin ngoai facts."
        )

    def _build_prompt(self, message: str, facts: dict[str, Any]) -> str:
        return (
            f"User message: {message}\n"
            f"Facts: {json.dumps(facts, ensure_ascii=False)}\n"
            "Response:"
        )

    def _bearer_headers(self, api_key: str) -> dict[str, str]:
        return {
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
        }


OllamaService = LlmService

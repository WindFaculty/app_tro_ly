from __future__ import annotations

from typing import Any

from app.core.config import Settings
from app.core.logging import app_log_path


def build_logs_payload(settings: Settings) -> dict[str, Any]:
    return {
        "directory": str(settings.log_dir),
        "app_log": str(app_log_path(settings)),
    }


def build_recovery_actions(
    settings: Settings,
    database: dict[str, Any],
    llm: dict[str, Any],
    stt: dict[str, Any],
    tts: dict[str, Any],
) -> list[str]:
    actions: list[str] = []
    if not database.get("available", False):
        actions.append(f"Check SQLite path and write permissions for {settings.db_path}.")
    if not llm.get("available", False):
        provider = str(llm.get("provider") or settings.llm_provider).lower()
        reason = str(llm.get("reason", "")).lower()
        if provider == "hybrid":
            fast_provider = settings.fast_provider
            deep_provider = settings.deep_provider
            actions.append(
                f"Check API keys and connectivity for fast provider {fast_provider} and deep provider {deep_provider}."
            )
            return actions
        if provider == "groq":
            if reason == "missing_api_key":
                actions.append("Set assistant_llm_provider=groq and configure assistant_groq_api_key for Groq replies.")
            else:
                actions.append(
                    f"Check Groq connectivity at {settings.groq_base_url} and ensure model {settings.groq_model} is available."
                )
        elif provider == "gemini":
            if reason == "missing_api_key":
                actions.append("Set assistant_llm_provider=gemini and configure assistant_gemini_api_key for Gemini replies.")
            else:
                actions.append(
                    f"Check Gemini connectivity at {settings.gemini_base_url} and ensure model {settings.gemini_model} is available."
                )
        elif reason in {"disabled", "disabled_for_this_phase"}:
            actions.append(
                "Set assistant_enable_ollama=true or switch assistant_llm_provider to groq or gemini to enable reply refinement."
            )
        else:
            actions.append(
                f"Start Ollama at {settings.ollama_base_url} and ensure model {settings.ollama_model} is available."
            )
    if not stt.get("available", False):
        if stt.get("provider") == "faster-whisper":
            actions.append(
                "Install faster-whisper or switch assistant_stt_provider=whisper_cpp and configure assistant_whisper_command."
            )
        else:
            actions.append("Configure assistant_whisper_command and assistant_whisper_model_path for speech-to-text.")
    if not tts.get("available", False):
        actions.append("Configure assistant_piper_command and assistant_piper_model_path for speech output.")
    return actions

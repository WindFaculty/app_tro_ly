from __future__ import annotations

from pathlib import Path
from typing import Any

from app.core.config import Settings
from app.services.stt import SttService
from app.services.tts import TtsService


class SpeechService:
    def __init__(self, settings: Settings) -> None:
        self.stt = SttService(settings)
        self.tts = TtsService(settings)

    def stt_health(self) -> dict[str, Any]:
        return self.stt.health()

    def tts_health(self) -> dict[str, Any]:
        return self.tts.health()

    def transcribe(self, audio_path: Path, language: str | None = None) -> dict[str, Any]:
        return self.stt.transcribe(audio_path, language)

    def transcribe_bytes(self, wav_bytes: bytes, language: str | None = None) -> dict[str, Any]:
        return self.stt.transcribe_bytes(wav_bytes, language)

    def synthesize(self, text: str, voice: str | None = None, cache: bool = True) -> dict[str, Any]:
        return self.tts.synthesize(text, voice, cache)

    def synthesize_sentences(self, text: str, voice: str | None = None, cache: bool = True) -> list[dict[str, Any]]:
        return self.tts.synthesize_sentences(text, voice, cache)


SttService = SttService
TtsService = TtsService

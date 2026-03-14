from __future__ import annotations

import hashlib
import re
import shutil
import subprocess
import wave
from pathlib import Path
from typing import Any

from app.core.config import Settings
from app.core.logging import get_logger

logger = get_logger("tts")


class TtsService:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    def health(self) -> dict[str, Any]:
        command = self._resolve_command(self._settings.piper_command)
        payload: dict[str, Any] = {
            "available": command is not None,
            "provider": "piper",
        }
        if self._settings.piper_command:
            payload["command"] = self._settings.piper_command
        if self._settings.piper_model_path:
            payload["model_path"] = self._settings.piper_model_path
        if command is None:
            payload["reason"] = "command not configured or not found"
        return payload

    def synthesize(self, text: str, voice: str | None = None, cache: bool = True) -> dict[str, Any]:
        command = self._resolve_command(self._settings.piper_command)
        if command is None:
            logger.warning("TTS requested but Piper runtime is not configured")
            raise RuntimeError("Piper runtime is not configured")

        key = hashlib.sha256(f"{voice or self._settings.default_tts_voice}:{text}".encode("utf-8")).hexdigest()
        output_path = self._settings.audio_dir / f"{key}.wav"
        cached = cache and output_path.exists()
        if not cached:
            process = subprocess.run(
                [
                    command,
                    "-m",
                    self._settings.piper_model_path or "",
                    "-f",
                    str(output_path),
                ],
                input=text,
                capture_output=True,
                text=True,
                timeout=self._settings.piper_timeout_sec,
                check=False,
            )
            if process.returncode != 0:
                logger.error(
                    "Piper synthesis failed for voice %s: %s",
                    voice or self._settings.default_tts_voice,
                    process.stderr.strip(),
                )
                raise RuntimeError(process.stderr.strip() or "Piper synthesis failed")

        return {
            "audio_path": output_path,
            "audio_url": f"/v1/speech/cache/{output_path.name}",
            "duration_ms": self._audio_duration_ms(output_path),
            "cached": cached,
        }

    def split_sentences(self, text: str) -> list[str]:
        sentences = [item.strip() for item in re.split(r"(?<=[.!?])\s+", text.strip()) if item.strip()]
        return sentences or ([text.strip()] if text.strip() else [])

    def synthesize_sentences(self, text: str, voice: str | None = None, cache: bool = True) -> list[dict[str, Any]]:
        results = []
        for sentence in self.split_sentences(text):
            synthesis = self.synthesize(sentence, voice=voice, cache=cache)
            synthesis["text"] = sentence
            results.append(synthesis)
        return results

    def _resolve_command(self, value: str | None) -> str | None:
        if not value:
            return None
        path = Path(value)
        if path.exists():
            return str(path)
        return shutil.which(value)

    def _audio_duration_ms(self, output_path: Path) -> int:
        with wave.open(str(output_path), "rb") as handle:
            frames = handle.getnframes()
            rate = handle.getframerate()
        return int((frames / rate) * 1000)

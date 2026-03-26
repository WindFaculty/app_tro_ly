from __future__ import annotations

import shutil
import subprocess
from pathlib import Path
from typing import Any

from app.core.config import Settings
from app.core.ids import make_id
from app.core.logging import get_logger

logger = get_logger("stt")


class SttService:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._faster_whisper_model: Any | None = None

    def health(self) -> dict[str, Any]:
        if self._settings.stt_provider == "faster_whisper":
            try:
                import faster_whisper  # noqa: F401
            except (ModuleNotFoundError, ImportError):
                payload = {
                    "available": False,
                    "provider": "faster-whisper",
                    "model_path": self._settings.faster_whisper_model_path or self._settings.faster_whisper_model_size,
                    "reason": "module_not_available",
                }
                fallback = self._whisper_cpp_health()
                if fallback.get("available"):
                    payload["fallback"] = fallback
                return payload
            return {
                "available": True,
                "provider": "faster-whisper",
                "model_path": self._settings.faster_whisper_model_path or self._settings.faster_whisper_model_size,
                "device": self._settings.faster_whisper_device,
            }
        return self._whisper_cpp_health()

    def transcribe(self, audio_path: Path, language: str | None = None) -> dict[str, Any]:
        if self._settings.stt_provider == "faster_whisper":
            try:
                return self._transcribe_with_faster_whisper(audio_path, language)
            except (ModuleNotFoundError, ImportError):
                logger.warning("faster-whisper is not installed, falling back to whisper.cpp")
            except Exception as exc:
                logger.warning("faster-whisper transcription failed, falling back to whisper.cpp: %s", exc)
        return self._transcribe_with_whisper_cpp(audio_path, language)

    def transcribe_bytes(self, wav_bytes: bytes, language: str | None = None) -> dict[str, Any]:
        temp_path = self._settings.audio_dir / f"{make_id('sttchunk')}.wav"
        temp_path.write_bytes(wav_bytes)
        try:
            return self.transcribe(temp_path, language)
        finally:
            temp_path.unlink(missing_ok=True)

    def _transcribe_with_faster_whisper(self, audio_path: Path, language: str | None = None) -> dict[str, Any]:
        from faster_whisper import WhisperModel

        if self._faster_whisper_model is None:
            model_name = self._settings.faster_whisper_model_path or self._settings.faster_whisper_model_size
            self._faster_whisper_model = WhisperModel(
                model_name,
                device=self._settings.faster_whisper_device,
                compute_type=self._settings.faster_whisper_compute_type,
            )
        segments, _ = self._faster_whisper_model.transcribe(
            str(audio_path),
            language=language or self._settings.default_language,
        )
        text = " ".join(segment.text.strip() for segment in segments).strip()
        return {
            "text": text,
            "language": language or self._settings.default_language,
            "confidence": 0.9 if text else 0.0,
        }

    def _whisper_cpp_health(self) -> dict[str, Any]:
        command = self._resolve_command(self._settings.whisper_command)
        model_path = self._resolve_model_path(self._settings.whisper_model_path)
        issues: list[str] = []
        if command is None:
            issues.append("command_not_configured_or_not_found")
        if model_path is None:
            issues.append("model_path_not_configured_or_not_found")
        payload: dict[str, Any] = {
            "available": len(issues) == 0,
            "provider": "whisper.cpp",
        }
        if self._settings.whisper_command:
            payload["command"] = self._settings.whisper_command
        if self._settings.whisper_model_path:
            payload["model_path"] = self._settings.whisper_model_path
        if issues:
            payload["reason"] = issues[0]
            payload["issues"] = issues
        return payload

    def _transcribe_with_whisper_cpp(self, audio_path: Path, language: str | None = None) -> dict[str, Any]:
        command = self._resolve_command(self._settings.whisper_command)
        if command is None:
            logger.warning("STT requested but whisper.cpp runtime is not configured")
            raise RuntimeError("whisper.cpp runtime is not configured")
        model_path = self._resolve_model_path(self._settings.whisper_model_path)
        if model_path is None:
            logger.warning("STT requested but whisper.cpp model path is not configured")
            raise RuntimeError("whisper.cpp model path is not configured")
        output_base = audio_path.with_suffix("")
        process = subprocess.run(
            [
                command,
                "-m",
                model_path,
                "-f",
                str(audio_path),
                "-otxt",
                "-of",
                str(output_base),
            ],
            capture_output=True,
            text=True,
            timeout=self._settings.whisper_timeout_sec,
            check=False,
        )
        if process.returncode != 0:
            logger.error("whisper.cpp transcription failed for %s: %s", audio_path, process.stderr.strip())
            raise RuntimeError(process.stderr.strip() or "whisper.cpp transcription failed")
        transcript_file = output_base.with_suffix(".txt")
        if not transcript_file.exists():
            logger.error("whisper.cpp completed without transcript output for %s", audio_path)
            raise RuntimeError("whisper.cpp did not produce a transcript file")
        text = transcript_file.read_text(encoding="utf-8").strip()
        transcript_file.unlink(missing_ok=True)
        return {"text": text, "language": language or self._settings.default_language, "confidence": 0.8}

    def _resolve_command(self, value: str | None) -> str | None:
        if not value:
            return None
        path = Path(value)
        if path.exists():
            return str(path) if path.is_file() else None
        return shutil.which(value)

    def _resolve_model_path(self, value: str | None) -> str | None:
        if not value:
            return None
        path = Path(value)
        if not path.exists() or not path.is_file():
            return None
        return str(path)

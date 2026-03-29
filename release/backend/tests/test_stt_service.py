from __future__ import annotations

from pathlib import Path

from app.core.config import Settings
from app.services.stt import SttService


def _settings(tmp_path: Path, **overrides: object) -> Settings:
    return Settings(
        _env_file=None,
        base_dir=tmp_path,
        data_dir=tmp_path / "data",
        db_path=tmp_path / "data" / "app.db",
        audio_dir=tmp_path / "data" / "audio",
        cache_dir=tmp_path / "data" / "cache",
        log_dir=tmp_path / "data" / "logs",
        reminder_poll_seconds=1,
        **overrides,
    )


def test_whisper_cpp_health_requires_command_and_model_file(tmp_path: Path) -> None:
    settings = _settings(
        tmp_path,
        stt_provider="whisper_cpp",
        whisper_command=str(tmp_path / "whisper-cli.exe"),
        whisper_model_path=str(tmp_path / "models"),
    )
    settings.ensure_directories()
    Path(settings.whisper_command).write_text("stub", encoding="utf-8")
    Path(settings.whisper_model_path).mkdir(parents=True)
    service = SttService(settings)

    payload = service.health()

    assert payload["available"] is False
    assert payload["provider"] == "whisper.cpp"
    assert payload["reason"] == "model_path_not_configured_or_not_found"
    assert "model_path_not_configured_or_not_found" in payload["issues"]


def test_whisper_cpp_transcribe_raises_when_model_path_is_missing(tmp_path: Path) -> None:
    settings = _settings(
        tmp_path,
        stt_provider="whisper_cpp",
        whisper_command=str(tmp_path / "whisper-cli.exe"),
        whisper_model_path=str(tmp_path / "missing.bin"),
    )
    settings.ensure_directories()
    Path(settings.whisper_command).write_text("stub", encoding="utf-8")
    audio_path = tmp_path / "sample.wav"
    audio_path.write_bytes(b"stub")
    service = SttService(settings)

    try:
        service.transcribe(audio_path)
    except RuntimeError as exc:
        assert "whisper.cpp model path is not configured" in str(exc)
    else:
        raise AssertionError("Expected missing whisper.cpp model path to raise RuntimeError")

from __future__ import annotations

from pathlib import Path
from types import SimpleNamespace

import numpy as np
import torch

from app.core.config import Settings
from app.services.tts import TtsService, _apply_chattts_torch_compat


def _settings(tmp_path: Path, **overrides: object) -> Settings:
    return Settings(
        _env_file=None,
        base_dir=tmp_path,
        data_dir=tmp_path / "data",
        db_path=tmp_path / "data" / "app.db",
        audio_dir=tmp_path / "data" / "audio",
        cache_dir=tmp_path / "data" / "cache",
        log_dir=tmp_path / "data" / "logs",
        enable_ollama=False,
        reminder_poll_seconds=1,
        **overrides,
    )


def test_chattts_health_reports_missing_module(tmp_path: Path) -> None:
    settings = _settings(tmp_path, tts_provider="chattts")
    settings.ensure_directories()
    service = TtsService(settings)
    service._import_chattts = lambda: (_ for _ in ()).throw(ModuleNotFoundError())  # type: ignore[method-assign]

    payload = service.health()

    assert payload["available"] is False
    assert payload["provider"] == "chattts"
    assert payload["reason"] == "module_not_installed"


def test_chattts_health_reports_import_failure(tmp_path: Path) -> None:
    settings = _settings(tmp_path, tts_provider="chattts")
    settings.ensure_directories()
    service = TtsService(settings)
    service._import_chattts = lambda: (_ for _ in ()).throw(ImportError("transformers mismatch"))  # type: ignore[method-assign]

    payload = service.health()

    assert payload["available"] is False
    assert payload["provider"] == "chattts"
    assert payload["reason"] == "import_failed"
    assert "transformers mismatch" in payload["error"]


def test_chattts_synthesize_writes_cached_wav(tmp_path: Path) -> None:
    settings = _settings(tmp_path, tts_provider="chattts")
    settings.ensure_directories()
    service = TtsService(settings)

    class _FakeChat:
        def __init__(self) -> None:
            self.load_calls: list[bool] = []

        def load(self, compile: bool = False) -> None:
            self.load_calls.append(compile)

        def sample_random_speaker(self) -> str:
            return "speaker-1"

        def infer(self, texts: list[str], **_: object) -> list[np.ndarray]:
            assert texts == ["Xin chao"]
            return [np.linspace(-0.25, 0.25, 2400, dtype=np.float32)]

    fake_chat = _FakeChat()

    class _FakeChatFactory:
        InferCodeParams = staticmethod(lambda **kwargs: kwargs)

        def __call__(self) -> _FakeChat:
            return fake_chat

    service._import_chattts = lambda: SimpleNamespace(Chat=_FakeChatFactory())  # type: ignore[method-assign]

    first = service.synthesize("Xin chao", voice="demo")
    second = service.synthesize("Xin chao", voice="demo")

    assert first["cached"] is False
    assert second["cached"] is True
    assert first["audio_path"].exists()
    assert first["duration_ms"] > 0
    assert fake_chat.load_calls == [False]


def test_chattts_synthesize_raises_runtime_error_when_import_fails(tmp_path: Path) -> None:
    settings = _settings(tmp_path, tts_provider="chattts")
    settings.ensure_directories()
    service = TtsService(settings)
    service._import_chattts = lambda: (_ for _ in ()).throw(ImportError("transformers mismatch"))  # type: ignore[method-assign]

    try:
        service.synthesize("Xin chao")
    except RuntimeError as exc:
        assert "ChatTTS import failed" in str(exc)
        assert "transformers mismatch" in str(exc)
    else:
        raise AssertionError("Expected ChatTTS import failure to raise RuntimeError")


def test_apply_chattts_torch_compat_restores_file_like_symbol() -> None:
    original = getattr(torch.serialization, "FILE_LIKE", None)
    had_original = hasattr(torch.serialization, "FILE_LIKE")
    if had_original:
        delattr(torch.serialization, "FILE_LIKE")

    try:
        _apply_chattts_torch_compat()
        assert hasattr(torch.serialization, "FILE_LIKE")
    finally:
        if had_original:
            torch.serialization.FILE_LIKE = original
        elif hasattr(torch.serialization, "FILE_LIKE"):
            delattr(torch.serialization, "FILE_LIKE")

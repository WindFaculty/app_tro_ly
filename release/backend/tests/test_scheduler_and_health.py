from __future__ import annotations

import asyncio
from datetime import timedelta
import wave
from pathlib import Path

from app.core.time import iso_date, iso_datetime, now_local


def _write_wav(path: Path) -> None:
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(16000)
        handle.writeframes(b"\x00\x00" * 1600)


def test_scheduler_emits_due_event(client) -> None:
    start_at = now_local().replace(second=0)  # reminder is immediately due because lead time is 15m
    end_at = start_at.replace(microsecond=0) + timedelta(minutes=30)
    response = client.post(
        "/v1/tasks",
        json={
            "title": "Nhap standup",
            "status": "planned",
            "priority": "high",
            "scheduled_date": iso_date(start_at.date()),
            "start_at": iso_datetime(start_at),
            "end_at": iso_datetime(end_at),
            "repeat_rule": "none",
            "tags": [],
        },
    )
    assert response.status_code == 200
    delivered = asyncio.run(client.app.state.container.scheduler_service.tick())
    assert delivered >= 1


def test_tts_endpoint_can_be_stubbed_for_smoke(client) -> None:
    audio_path = client.app.state.container.settings.audio_dir / "stub.wav"
    _write_wav(audio_path)

    def fake_synthesize(text: str, voice: str | None = None, cache: bool = True):
        return {
            "audio_path": audio_path,
            "audio_url": f"/v1/speech/cache/{audio_path.name}",
            "duration_ms": 100,
            "cached": False,
        }

    client.app.state.container.speech_service.synthesize = fake_synthesize
    response = client.post("/v1/speech/tts", json={"text": "Xin chao", "voice": "stub", "cache": True})
    assert response.status_code == 200
    assert response.json()["audio_url"].endswith("stub.wav")


def test_settings_update_roundtrip(client) -> None:
    update = client.put(
        "/v1/settings",
        json={
            "voice": {"speak_replies": False},
            "window_mode": {"mini_assistant_enabled": False},
        },
    )
    assert update.status_code == 200
    current = client.get("/v1/settings")
    assert current.status_code == 200
    payload = current.json()
    assert payload["voice"]["speak_replies"] is False


def test_scheduler_run_recovers_after_tick_failure(client) -> None:
    scheduler = client.app.state.container.scheduler_service
    scheduler._settings.reminder_poll_seconds = 0
    stop_event = asyncio.Event()
    calls = {"count": 0}

    async def flaky_tick() -> int:
        calls["count"] += 1
        if calls["count"] == 1:
            raise RuntimeError("boom")
        stop_event.set()
        return 0

    scheduler.tick = flaky_tick  # type: ignore[method-assign]
    asyncio.run(scheduler.run(stop_event))
    assert calls["count"] >= 2

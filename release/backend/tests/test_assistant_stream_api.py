from __future__ import annotations


def test_assistant_stream_text_turn_emits_route_and_final(client) -> None:
    with client.websocket_connect("/v1/assistant/stream") as websocket:
        websocket.send_json(
            {
                "type": "session_start",
                "session_id": "sess_test",
                "selected_date": None,
                "voice_mode": False,
            }
        )
        first = websocket.receive_json()
        assert first["type"] == "assistant_state_changed"

        websocket.send_json(
            {
                "type": "text_turn",
                "session_id": "sess_test",
                "conversation_id": None,
                "message": "Hom nay toi co gi?",
                "voice_mode": False,
            }
        )

        seen_types: list[str] = []
        final_payload = None
        for _ in range(8):
            event = websocket.receive_json()
            seen_types.append(event["type"])
            if event["type"] == "assistant_final":
                final_payload = event
                break

        assert "route_selected" in seen_types
        assert final_payload is not None
        assert final_payload["reply_text"]
        assert final_payload["route"] in {"groq_fast", "gemini_deep", "hybrid_plan_then_groq"}


def test_assistant_stream_voice_end_emits_transcript_and_final(client) -> None:
    client.app.state.container.speech_service.transcribe_bytes = lambda wav_bytes, language=None: {
        "text": "Them task demo ngay mai",
        "language": language or "vi",
        "confidence": 1.0,
    }

    with client.websocket_connect("/v1/assistant/stream") as websocket:
        websocket.send_json(
            {
                "type": "session_start",
                "session_id": "sess_voice",
                "selected_date": None,
                "voice_mode": True,
            }
        )
        websocket.receive_json()

        websocket.send_json(
            {
                "type": "voice_end",
                "session_id": "sess_voice",
                "conversation_id": None,
                "voice_mode": True,
                "audio_base64": "c3R1Yi13YXY=",
            }
        )

        seen_types: list[str] = []
        final_payload = None
        for _ in range(10):
            event = websocket.receive_json()
            seen_types.append(event["type"])
            if event["type"] == "assistant_final":
                final_payload = event
                break

        assert "transcript_final" in seen_types
        assert final_payload is not None
        assert final_payload["reply_text"]
